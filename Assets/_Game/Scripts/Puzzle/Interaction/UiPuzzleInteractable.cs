using System;
using UnityEngine;
using UnityEngine.Events;

//=============================================================================
// File:        UiPuzzleInteractable.cs
//
// Description: 맵 오브젝트의 상호작용을 공용 PuzzleUiRoot에 전달한다.
// History:     10/07/2026 + 최초 클리어와 무효과 상태 복원 분리 -- Codex
//
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class UiPuzzleInteractable : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Puzzle")]
    [Tooltip("이 오브젝트가 실행할 UI 퍼즐 종류입니다.")]
    [SerializeField] private PuzzleType _puzzleType = PuzzleType.PUZZLE_TYPE_PIPE;

    [Tooltip("puzzles.csv의 puzzle_id와 정확히 일치해야 합니다.")]
    [SerializeField] private string _puzzleId = "PIPE_STAGE_01";

    [Tooltip("퍼즐 UI에 표시할 챕터 배경과 조각 종류입니다. 0~3 중 선택합니다.")]
    [SerializeField, Range(0, 3)] private int _chapterIndex = 1;

    [Header("원형 별자리 (선택)")]
    [Tooltip("이 배치만 사용할 별자리 설정입니다. 비어 있으면 기존 색상 판정을 유지합니다.")]
    [SerializeField] private CircleConstellationSettingsSO _constellationSettings;

    // 공용 UI가 파괴되면 Unity null 검사로 다음 상호작용 때 다시 연결합니다.
    private UiCirclePuzzleManager _circleManager;

    [Header("Clear")]
    [Tooltip("퍼즐을 해결한 뒤 이 오브젝트와 다시 상호작용할 수 없게 합니다.")]
    [SerializeField] private bool _disableInteractionAfterClear = true;

    [Tooltip("퍼즐을 해결했을 때 문 열기, 보상 지급 등을 연결합니다.")]
    [SerializeField] private UnityEvent _onPuzzleCleared = new UnityEvent();

    [Header("Highlight")]
    [Tooltip("플레이어 상호작용 범위에 들어왔을 때 표시할 선택 오브젝트입니다.")]
    [SerializeField] private GameObject _highlightObject;

    private Collider2D _interactionCollider;

    // 비활성 오브젝트의 Awake 전 복원도 원래 충돌 설정을 보존합니다.
    private bool _initialColliderEnabled;

    /// <summary>최초 클리어에만 상태 저장 연결을 알립니다.</summary>
    public event Action PuzzleCleared;

    /// <summary>이 오브젝트가 실행하는 퍼즐 종류입니다.</summary>
    public PuzzleType PuzzleType => _puzzleType;

    /// <summary>이 오브젝트가 실행하는 CSV 퍼즐 ID입니다.</summary>
    public string PuzzleId => _puzzleId;

    /// <summary>현재 퍼즐 UI의 챕터 표시 번호입니다.</summary>
    public int ChapterIndex => _chapterIndex;

    /// <summary>현재 플레이 세션에서 퍼즐을 해결했는지 여부입니다.</summary>
    public bool IsSolved { get; private set; }

    private void Awake()
    {
        RestoreSolvedState(IsSolved);
    }

    /// <summary>플레이어 상호작용을 종류와 ID에 맞는 UI 퍼즐 열기로 전달합니다.</summary>
    public void Interact()
    {
        if (IsSolved && _disableInteractionAfterClear)
        {
            return;
        }

        // 비동기 파일 로드가 끝나기 전에 퍼즐을 풀어 복원 결과를 덮어쓰지 않도록 합니다.
        if (TryGetComponent(out UiPuzzleSceneState state) && !state.IsReady) return;

        if (_puzzleType == PuzzleType.PUZZLE_TYPE_NONE
            || string.IsNullOrWhiteSpace(_puzzleId))
        {
            Debug.LogWarning(
                $"[UiPuzzleInteractable] Puzzle Type 또는 ID가 비어 있습니다. object={name}", this);
            return;
        }

        if (!PuzzleUiRoot.TryFind(out PuzzleUiRoot puzzleUiRoot))
        {
            Debug.LogWarning(
                $"[UiPuzzleInteractable] 씬에서 PuzzleUiRoot를 찾을 수 없습니다. object={name}", this);
            return;
        }

        if (puzzleUiRoot.IsPuzzleOpen) return;
        if (_puzzleType == PuzzleType.PUZZLE_TYPE_CIRCLE)
        {
            if (_circleManager == null) _circleManager = puzzleUiRoot.GetComponentInChildren<UiCirclePuzzleManager>(true);
            _circleManager?.SetConstellationSettings(_constellationSettings);
        }
        puzzleUiRoot.TryOpenPuzzle(_puzzleType, _puzzleId, this);
    }

    /// <summary>플레이어 포커스 여부에 따라 선택 표시를 켜거나 끕니다.</summary>
    public void SetHighlight(bool isActive)
    {
        if (_highlightObject != null)
        {
            _highlightObject.SetActive(isActive);
        }
    }

    /// <summary>현재 오브젝트가 요청한 퍼즐의 클리어 결과를 한 번 적용합니다.</summary>
    internal void NotifyPuzzleCleared()
    {
        if (IsSolved)
        {
            return;
        }

        RestoreSolvedState(true);
        PuzzleCleared?.Invoke();
        _onPuzzleCleared?.Invoke();
    }

    //=============================================================================
    // Description: 최초 클리어 이벤트 없이 저장된 최종 상태를 적용합니다.
    // Parameters: solved — 저장된 완료 여부. Return: void.
    //=============================================================================
    /// <summary>복원 시 이벤트·보상·저장 요청 없이 상호작용 상태를 적용합니다.</summary>
    public void RestoreSolvedState(bool solved)
    {
        if (_interactionCollider == null && TryGetComponent(out _interactionCollider))
        {
            _initialColliderEnabled = _interactionCollider.enabled;
        }
        IsSolved = solved;
        SetHighlight(false);
        if (_interactionCollider != null)
        {
            _interactionCollider.enabled = _initialColliderEnabled
                && !(solved && _disableInteractionAfterClear);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_puzzleType == PuzzleType.PUZZLE_TYPE_NONE)
        {
            Debug.LogWarning(
                $"[UiPuzzleInteractable] Puzzle Type을 지정해야 합니다. object={name}", this);
        }

        if (string.IsNullOrWhiteSpace(_puzzleId))
        {
            Debug.LogWarning(
                $"[UiPuzzleInteractable] Puzzle ID를 지정해야 합니다. object={name}", this);
        }
    }
#endif
}
