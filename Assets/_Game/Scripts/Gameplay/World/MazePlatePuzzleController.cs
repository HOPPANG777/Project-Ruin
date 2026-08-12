//=============================================================================
// File:        MazePlatePuzzleController.cs
//
// Description: 여러 미로 발판의 동시 압력과 최종 해결 상태를 관리합니다.
//
// History:     08/06/2026 + Created -- Codex
//
//=============================================================================

using System;
using UnityEngine;
using UnityEngine.Events;

public enum MazePlatePuzzleState
{
    MAZE_PLATE_PUZZLE_INITIAL = 0,
    MAZE_PLATE_PUZZLE_IN_PROGRESS,
    MAZE_PLATE_PUZZLE_COMPLETED
}

//=============================================================================
//
// Synopsis:   MazePlatePuzzleController;
//                - 모든 발판이 동시에 눌린 경우에만 한 번 해결
//                - 최종 해결 상태만 저장하고 보상 없는 복원 지원
//
//=============================================================================
[DisallowMultipleComponent]
public sealed class MazePlatePuzzleController : MonoBehaviour, ISceneStateContributor
{
    private const string SOLVED_STATE_JSON = "true";

    [Header("Identity")]
    [Tooltip("씬 상태 저장에서 이 퍼즐을 구분하는 고유 ID입니다.")]
    [SerializeField] private string _objectId;

    [Header("Puzzle")]
    [Tooltip("동시에 눌러야 하는 모든 미로 발판입니다.")]
    [SerializeField] private MazePuzzlePlate[] _plates;
    [Tooltip("해결 후 이동을 잠글 이 퍼즐 소속 돌입니다.")]
    [SerializeField] private PushableStone[] _stones = new PushableStone[0];

    [Header("Result")]
    [Tooltip("퍼즐이 미해결일 때 표시할 문 또는 입구입니다.")]
    [SerializeField] private GameObject _unsolvedResultObject;
    [Tooltip("퍼즐이 해결된 뒤 표시할 문 또는 입구입니다.")]
    [SerializeField] private GameObject _solvedResultObject;
    [Tooltip("처음 해결될 때 문 개방 또는 보상을 한 번 실행합니다.")]
    [SerializeField] private UnityEvent _onSolved;

    private MazePlatePuzzleState _state = MazePlatePuzzleState.MAZE_PLATE_PUZZLE_INITIAL;
    private bool _isSetupValid;

    /// <summary>씬 상태 저장에서 사용하는 고유 퍼즐 ID입니다.</summary>
    public string ObjectId => _objectId;
    /// <summary>현재 퍼즐의 초기, 진행 또는 종료 상태입니다.</summary>
    public MazePlatePuzzleState State => _state;
    /// <summary>퍼즐 진행 상태가 실제로 바뀌었을 때 전달합니다.</summary>
    public event Action<MazePlatePuzzleState> StateChanged;

    private void Awake()
    {
        _isSetupValid = TryValidateSetup(true);
        if (!_isSetupValid)
        {
            return;
        }

        for (int i = 0; i < _plates.Length; i++)
        {
            _plates[i].Initialize(this);
        }
        SetStonesCompleted(false);
        ApplyResultState(false);
    }

    /// <summary>검증된 초기 퍼즐을 플레이어가 조작 가능한 진행 상태로 전환합니다.</summary>
    public bool TryBeginPuzzle()
    {
        if (!_isSetupValid
            || _state != MazePlatePuzzleState.MAZE_PLATE_PUZZLE_INITIAL)
        {
            return false;
        }

        SetState(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS);
        TryCompleteIfAllPressed();
        return true;
    }

    /// <summary>진행 중인 발판과 돌을 최초 상태로 되돌리고 초기 상태로 전환합니다.</summary>
    public bool TryResetPuzzle()
    {
        if (!_isSetupValid
            || _state != MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS)
        {
            return false;
        }

        SetStonesCompleted(false);
        for (int i = 0; i < _stones.Length; i++)
        {
            if (!_stones[i].TryResetToInitialPosition())
            {
                Debug.LogError(
                    $"[MazePlatePuzzleController] 돌을 최초 위치로 복원할 수 없습니다. index={i}", this);
                return false;
            }
        }
        for (int i = 0; i < _plates.Length; i++)
        {
            _plates[i].ResetPlate();
        }
        SetState(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_INITIAL);
        ApplyResultState(false);
        return true;
    }

    /// <summary>최종 해결 상태만 씬 저장 문자열로 반환합니다.</summary>
    public string CaptureState()
    {
        return _state == MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED
            ? SOLVED_STATE_JSON : string.Empty;
    }

    /// <summary>보상 이벤트 없이 저장된 최종 해결 상태를 복원합니다.</summary>
    public void ApplyState(string stateJson)
    {
        if (!_isSetupValid || string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }
        if (!bool.TryParse(stateJson, out bool isSolved))
        {
            Debug.LogWarning(
                $"[MazePlatePuzzleController] 저장 상태를 읽을 수 없습니다. object={name}", this);
            return;
        }
        if (isSolved && _state != MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED)
        {
            CompletePuzzle(false);
        }
    }

    /// <summary>발판 상태가 변한 진행 퍼즐의 전체 해결 조건을 다시 검사합니다.</summary>
    internal void NotifyPlateStateChanged()
    {
        if (_state == MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS)
        {
            TryCompleteIfAllPressed();
        }
    }

    private void TryCompleteIfAllPressed()
    {
        for (int i = 0; i < _plates.Length; i++)
        {
            if (!_plates[i].IsPressed)
            {
                return;
            }
        }
        CompletePuzzle(true);
    }

    private void CompletePuzzle(bool invokeResult)
    {
        if (_state == MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED)
        {
            return;
        }

        SetState(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED);
        SetStonesCompleted(true);
        ApplyResultState(true);
        if (invokeResult)
        {
            _onSolved?.Invoke();
        }
    }

    private void SetStonesCompleted(bool isCompleted)
    {
        for (int i = 0; i < _stones.Length; i++)
        {
            _stones[i].SetPuzzleCompleted(isCompleted);
        }
    }

    private void ApplyResultState(bool isSolved)
    {
        _unsolvedResultObject.SetActive(!isSolved);
        _solvedResultObject.SetActive(isSolved);
    }

    private void SetState(MazePlatePuzzleState state)
    {
        if (_state == state)
        {
            return;
        }
        _state = state;
        StateChanged?.Invoke(_state);
    }

    private bool TryValidateSetup(bool logIssue)
    {
        string issue = GetSetupIssue();
        if (issue.Length == 0)
        {
            return true;
        }
        if (logIssue)
        {
            Debug.LogError($"[MazePlatePuzzleController] {name}: {issue}", this);
        }
        return false;
    }

    private string GetSetupIssue()
    {
        if (string.IsNullOrWhiteSpace(_objectId) || _plates == null || _plates.Length == 0)
        {
            return "ObjectId 또는 발판 목록이 비어 있습니다.";
        }
        if (_stones == null || _unsolvedResultObject == null || _solvedResultObject == null
            || _unsolvedResultObject == _solvedResultObject)
        {
            return "돌 목록 또는 해결 결과 오브젝트 설정이 올바르지 않습니다.";
        }
        for (int i = 0; i < _plates.Length; i++)
        {
            if (_plates[i] == null)
            {
                return $"발판 참조가 비어 있습니다. index={i}";
            }
            if (!_plates[i].TryValidate(out string issue))
            {
                return $"발판 설정이 올바르지 않습니다. index={i}, reason={issue}";
            }
            for (int otherIndex = 0; otherIndex < i; otherIndex++)
            {
                if (_plates[otherIndex] == _plates[i])
                {
                    return $"같은 발판이 중복 연결되었습니다. index={i}";
                }
            }
        }
        for (int i = 0; i < _stones.Length; i++)
        {
            if (_stones[i] == null || !_stones[i].CanResetToInitialPosition)
            {
                return $"돌 참조가 비어 있거나 초기 위치 복원이 허용되지 않습니다. index={i}";
            }
            for (int otherIndex = 0; otherIndex < i; otherIndex++)
            {
                if (_stones[otherIndex] == _stones[i])
                {
                    return $"같은 돌이 중복 연결되었습니다. index={i}";
                }
            }
        }
        return string.Empty;
    }

#if UNITY_EDITOR
    private void OnValidate() => TryValidateSetup(false);
#endif
}
