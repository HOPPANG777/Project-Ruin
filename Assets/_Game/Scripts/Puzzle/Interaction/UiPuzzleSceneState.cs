//=============================================================================
// File:        UiPuzzleSceneState.cs
// Description: 배치 인스턴스의 완료 여부와 자기 문을 씬 상태 계약에 연결합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System;
using UnityEngine;

/// <summary>퍼즐 데이터 ID와 독립된 배치 ID로 완료 상태만 저장·복원합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UiPuzzleInteractable))]
public sealed class UiPuzzleSceneState : MonoBehaviour, ISceneStateContributor
{
    /// <summary>미완성 조각 배치를 포함하지 않는 저장 형식입니다.</summary>
    [Serializable]
    private sealed class SavedState
    {
        /// <summary>해당 배치 인스턴스의 완료 여부입니다.</summary>
        public bool Solved;
    }

    [Tooltip("배치마다 지정하는 고정 저장 ID입니다. CSV puzzle_id를 사용하지 않습니다.")]
    [SerializeField] private string _objectId;
    [Tooltip("기존 SceneStateBridge와 같은 저장 스테이지 번호입니다.")]
    [SerializeField] private int _stageNumber;
    [Tooltip("이 퍼즐만 여는 문입니다. 조작대와 패드를 자식으로 두지 않습니다.")]
    [SerializeField] private GameObject _closedDoor;

    // Bootstrap 주입 이전이나 중복 ID에는 저장 키를 노출하지 않습니다.
    private bool _canPersist;
    private UiPuzzleInteractable _puzzle;
    private Action<UiPuzzleSceneState> _requestSave;

    /// <summary>유효한 배치만 SceneStateBridge의 Capture/Apply에 참여합니다.</summary>
    public string ObjectId => _canPersist ? _objectId : string.Empty;
    /// <summary>배치가 속한 기존 저장 스테이지 번호입니다.</summary>
    public int StageNumber => _stageNumber;
    /// <summary>저장 복구와 유효한 배치 연결이 끝나 상호작용할 수 있는지 여부입니다.</summary>
    public bool IsReady => _canPersist;
    /// <summary>초기화 검증에 사용하는 직렬화된 고정 ID입니다.</summary>
    internal string ConfiguredId => _objectId;

    //=============================================================================
    // Description: 비활성 배치도 구독하고 저장 상태를 즉시 복원합니다.
    // Parameters: progress, requestSave, canPersist — 주입된 연결. Return: void.
    //=============================================================================
    /// <summary>Bootstrap 연결 객체가 저장 서비스와 최초 클리어 콜백을 주입합니다.</summary>
    public void Init(IProgressService progress, Action<UiPuzzleSceneState> requestSave, bool canPersist)
    {
        if (_puzzle == null) TryGetComponent(out _puzzle);
        _canPersist = canPersist && progress != null;
        _requestSave = requestSave;
        if (_puzzle == null) return;
        _puzzle.PuzzleCleared -= HandleFirstClear;
        _puzzle.PuzzleCleared += HandleFirstClear;
        if (_canPersist && progress.TryGetObjectState(_stageNumber, _objectId, out string json))
        {
            ApplyState(json);
        }
        else if (_canPersist)
        {
            ApplySolved(false);
        }
    }

    /// <summary>완료 여부만 기존 저장 서비스가 직렬화할 JSON으로 반환합니다.</summary>
    public string CaptureState()
    {
        if (_puzzle == null) TryGetComponent(out _puzzle);
        return JsonUtility.ToJson(new SavedState { Solved = _puzzle != null && _puzzle.IsSolved });
    }

    /// <summary>반복 복원해도 최초 클리어 이벤트나 저장 요청을 발생시키지 않습니다.</summary>
    public void ApplyState(string stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson)) return;
        try
        {
            SavedState state = JsonUtility.FromJson<SavedState>(stateJson);
            if (state != null) ApplySolved(state.Solved);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"[UiPuzzleSceneState] 상태 복원 실패: {_objectId}, {exception.Message}", this);
        }
    }

    // 최초 클리어 경로는 향후 개방 연출을 연결할 UnityEvent와 별도로 유지합니다.
    private void HandleFirstClear()
    {
        ApplySolved(true);
        if (_canPersist) _requestSave?.Invoke(this);
    }

    private void ApplySolved(bool solved)
    {
        if (_puzzle == null) TryGetComponent(out _puzzle);
        _puzzle?.RestoreSolvedState(solved);
        if (_closedDoor != null) _closedDoor.SetActive(!solved);
    }

    /// <summary>저장 연결의 수명이 끝나면 이전 완료 콜백과 구독을 해제합니다.</summary>
    public void Disconnect()
    {
        if (_puzzle != null) _puzzle.PuzzleCleared -= HandleFirstClear;
        _requestSave = null;
        _canPersist = false;
    }

    private void OnDestroy() => Disconnect();
}
