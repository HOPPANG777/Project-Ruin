using UnityEngine;
using UnityEngine.Events;

//=============================================================================
// File:        UiPuzzleInteractable.cs
//
// Description: 맵 오브젝트의 상호작용을 공용 PuzzleUiRoot에 전달한다.
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

    [Header("Clear")]
    [Tooltip("퍼즐을 해결한 뒤 이 오브젝트와 다시 상호작용할 수 없게 합니다.")]
    [SerializeField] private bool _disableInteractionAfterClear = true;

    [Tooltip("퍼즐을 해결했을 때 문 열기, 보상 지급 등을 연결합니다.")]
    [SerializeField] private UnityEvent _onPuzzleCleared = new UnityEvent();

    [Header("Highlight")]
    [Tooltip("플레이어 상호작용 범위에 들어왔을 때 표시할 선택 오브젝트입니다.")]
    [SerializeField] private GameObject _highlightObject;

    private Collider2D _interactionCollider;

    /// <summary>이 오브젝트가 실행하는 퍼즐 종류입니다.</summary>
    public PuzzleType PuzzleType => _puzzleType;

    /// <summary>이 오브젝트가 실행하는 CSV 퍼즐 ID입니다.</summary>
    public string PuzzleId => _puzzleId;

    /// <summary>현재 플레이 세션에서 퍼즐을 해결했는지 여부입니다.</summary>
    public bool IsSolved { get; private set; }

    private void Awake()
    {
        TryGetComponent(out _interactionCollider);
        SetHighlight(false);
    }

    /// <summary>플레이어 상호작용을 종류와 ID에 맞는 UI 퍼즐 열기로 전달합니다.</summary>
    public void Interact()
    {
        if (IsSolved && _disableInteractionAfterClear)
        {
            return;
        }

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

        IsSolved = true;
        SetHighlight(false);

        if (_disableInteractionAfterClear && _interactionCollider != null)
        {
            _interactionCollider.enabled = false;
        }

        _onPuzzleCleared?.Invoke();
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
