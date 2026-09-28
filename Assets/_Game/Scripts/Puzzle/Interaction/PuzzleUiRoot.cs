using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

//=============================================================================
// File:        PuzzleUiRoot.cs
//
// Description: 세 종류의 UI 퍼즐 팝업을 한곳에서 찾고 월드 오브젝트의 요청을
//              현재 플레이어 입력 잠금과 연결한다.
//
//=============================================================================
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class PuzzleUiRoot : MonoBehaviour
{
    [Serializable]
    private struct PuzzlePopupBinding
    {
        [SerializeField] private PuzzleType _puzzleType;
        [SerializeField] private UiPuzzlePopup _puzzlePopup;

        public PuzzleType PuzzleType => _puzzleType;
        public UiPuzzlePopup PuzzlePopup => _puzzlePopup;
    }

    [Header("Puzzle Popups")]
    [Tooltip("퍼즐 종류별 팝업입니다. 파이프, 라이트, 원형을 각각 한 번만 등록합니다.")]
    [SerializeField] private PuzzlePopupBinding[] _puzzlePopups;

    [Header("Input Fallback")]
    [Tooltip("씬에 EventSystem이 없을 때만 사용하는 프리팹 내 기본 EventSystem입니다.")]
    [SerializeField] private GameObject _fallbackEventSystem;

    private UiPuzzlePopup _activePopup;
    private UiPuzzleInteractable _activeRequester;
    private PlayerInteractionController _interactionController;
    private PlayerMoveController _moveController;
    private bool _previousCanInteract;
    private bool _previousCanMove;
    private bool _ownsPlayerInputLock;
    private bool _clearProcessed;

    /// <summary>현재 어떤 퍼즐 팝업이라도 열려 있는지 여부입니다.</summary>
    public bool IsPuzzleOpen => _activePopup != null && _activePopup.IsOpen;

    private void Awake()
    {
        ConfigureFallbackEventSystem();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        ClearActivePuzzle(true);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => ConfigureFallbackEventSystem();

    /// <summary>현재 씬에서 활성화된 공용 퍼즐 UI 루트를 찾습니다.</summary>
    public static bool TryFind(out PuzzleUiRoot puzzleUiRoot)
    {
        puzzleUiRoot = FindFirstObjectByType<PuzzleUiRoot>();
        return puzzleUiRoot != null;
    }

    /// <summary>요청한 종류와 CSV ID에 맞는 퍼즐 팝업을 엽니다.</summary>
    public bool TryOpenPuzzle(
        PuzzleType puzzleType,
        string puzzleId,
        UiPuzzleInteractable requester)
    {
        if (_activePopup != null)
        {
            return false;
        }

        if (requester == null || string.IsNullOrWhiteSpace(puzzleId))
        {
            Debug.LogWarning("[PuzzleUiRoot] 요청 오브젝트 또는 Puzzle ID가 비어 있습니다.", this);
            return false;
        }

        if (!TryGetPopup(puzzleType, out UiPuzzlePopup popup)
            || popup.PuzzleManager == null)
        {
            Debug.LogWarning(
                $"[PuzzleUiRoot] 등록된 퍼즐 팝업을 찾을 수 없습니다. type={puzzleType}", this);
            return false;
        }

        if (!popup.PuzzleManager.TrySetPuzzleId(puzzleId))
        {
            return false;
        }

        popup.PuzzleManager.SetChapterIndex(requester.ChapterIndex);

        _activePopup = popup;
        _activeRequester = requester;
        _clearProcessed = false;
        _activePopup.OnPopupClosedEvent += HandlePopupClosed;
        _activePopup.PuzzleManager.OnPuzzleClearedEvent += HandlePuzzleCleared;

        _activePopup.OpenPuzzlePopup();
        if (!_activePopup.IsOpen)
        {
            ClearActivePuzzle(false);
            return false;
        }

        AcquirePlayerInput();
        return true;
    }

    private bool TryGetPopup(PuzzleType puzzleType, out UiPuzzlePopup popup)
    {
        popup = null;

        if (_puzzlePopups == null)
        {
            return false;
        }

        for (int i = 0; i < _puzzlePopups.Length; i++)
        {
            if (_puzzlePopups[i].PuzzleType != puzzleType)
            {
                continue;
            }

            popup = _puzzlePopups[i].PuzzlePopup;
            return popup != null;
        }

        return false;
    }

    private void HandlePuzzleCleared()
    {
        if (_clearProcessed)
        {
            return;
        }

        _clearProcessed = true;
        _activeRequester?.NotifyPuzzleCleared();
    }

    private void HandlePopupClosed()
    {
        ClearActivePuzzle(false);
    }

    private void ClearActivePuzzle(bool closePopup)
    {
        UiPuzzlePopup popup = _activePopup;
        if (popup != null)
        {
            popup.OnPopupClosedEvent -= HandlePopupClosed;
            if (popup.PuzzleManager != null)
            {
                popup.PuzzleManager.OnPuzzleClearedEvent -= HandlePuzzleCleared;
            }
        }

        _activePopup = null;
        _activeRequester = null;
        _clearProcessed = false;
        ReleasePlayerInput();

        if (closePopup && popup != null && popup.IsOpen)
        {
            popup.ClosePopup();
        }
    }

    private void AcquirePlayerInput()
    {
        _interactionController = FindFirstObjectByType<PlayerInteractionController>();
        _moveController = FindFirstObjectByType<PlayerMoveController>();

        if (_interactionController != null)
        {
            _previousCanInteract = _interactionController.CanInteract;
            _interactionController.SetCanInteract(false);
        }

        if (_moveController != null)
        {
            _previousCanMove = _moveController.CanMove;
            _moveController.SetCanMove(false);
        }

        _ownsPlayerInputLock = _interactionController != null || _moveController != null;
    }

    private void ReleasePlayerInput()
    {
        if (!_ownsPlayerInputLock)
        {
            return;
        }

        _ownsPlayerInputLock = false;

        if (_interactionController != null)
        {
            _interactionController.SetCanInteract(_previousCanInteract);
        }

        if (_moveController != null)
        {
            _moveController.SetCanMove(_previousCanMove);
        }

        _interactionController = null;
        _moveController = null;
    }

    private void ConfigureFallbackEventSystem()
    {
        if (_fallbackEventSystem == null)
        {
            return;
        }

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        bool hasExternalEventSystem = false;

        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem eventSystem = eventSystems[i];
            if (eventSystem != null
                && eventSystem.gameObject != _fallbackEventSystem
                && eventSystem.gameObject.activeInHierarchy
                && eventSystem.enabled)
            {
                hasExternalEventSystem = true;
                break;
            }
        }

        _fallbackEventSystem.SetActive(!hasExternalEventSystem);
    }
}
