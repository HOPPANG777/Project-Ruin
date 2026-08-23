//=============================================================================
// File:        CodexNavigationPresenter.cs
// Description: 도감 입력, 창 전환과 플레이어 입력 잠금 생명주기를 관리한다.
// History:     08/22/2026 + Created -- Codex
//              08/23/2026 + 실제 인벤토리 표시 상태와 양방향 전환 연결 -- Codex
//=============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class CodexNavigationPresenter : MonoBehaviour
{
    [Header("Presenters")]
    [Tooltip("열기와 닫기를 수행할 도감 Presenter입니다.")]
    [SerializeField] private CodexPresenter _codexPresenter;
    [Tooltip("연결됐을 때 도감과 전환할 인벤토리 Presenter입니다.")]
    [SerializeField] private InventoryPresenter _inventoryPresenter;
    [Tooltip("인벤토리의 실제 표시 상태를 제공하는 최상위 CanvasGroup입니다.")]
    [SerializeField] private CanvasGroup _inventoryRootCanvasGroup;
    [Tooltip("씬에 배치된 인벤토리 전환 참조 묶음입니다.")]
    [SerializeField] private InventoryNavigationBridge _inventoryBridge;
    [Header("Buttons")]
    [Tooltip("HUD에서 도감을 여는 버튼입니다.")]
    [SerializeField] private Button _hudOpenButton;
    [Tooltip("도감 우측 상단 X 닫기 버튼입니다.")]
    [SerializeField] private Button _closeButton;
    [Tooltip("도감에서 인벤토리로 이동하는 왼쪽 버튼입니다.")]
    [SerializeField] private Button _inventoryButton;
    [Tooltip("인벤토리에서 도감으로 이동하는 오른쪽 버튼입니다.")]
    [SerializeField] private Button _inventoryCodexButton;
    [Tooltip("지도 미구현 시 비활성화할 오른쪽 버튼입니다.")]
    [SerializeField] private Button _mapButton;
    [Header("Player Lock")]
    [Tooltip("메뉴가 열린 동안 상호작용을 막을 플레이어 컨트롤러입니다.")]
    [SerializeField] private PlayerInteractionController _playerInteractionController;
    [Tooltip("메뉴가 열린 동안 이동을 막을 플레이어 컨트롤러입니다.")]
    [SerializeField] private PlayerMoveController _playerMoveController;

    private bool _isInitialized;
    private bool _ownsPlayerLock;
    private bool _attemptedPlayerResolution;
    private bool _previousCanInteract = true;
    private bool _previousCanMove = true;
    private int _transitionFrame = -1;

    public bool IsInitialized => _isInitialized;

    private void Update()
    {
        if (!_isInitialized) TryInitializeFromGameRoot();
        if (!_isInitialized) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.capsLockKey.wasPressedThisFrame) ToggleCodex();
        SyncPlayerLock();
    }

    private void OnDisable()
    {
        _codexPresenter?.Close();
        _inventoryPresenter?.Close();
        ReleasePlayerLock();
    }

    private void OnDestroy()
    {
        UnregisterListeners();
        ReleasePlayerLock();
    }

    /// <summary>인벤토리 전환 대상과 버튼 이벤트를 연결합니다.</summary>
    public bool Init(InventoryPresenter inventoryPresenter,
        CanvasGroup inventoryRootCanvasGroup = null, Button inventoryCodexButton = null)
    {
        UnregisterListeners();
        ReleasePlayerLock();
        _inventoryPresenter = inventoryPresenter;
        if (inventoryRootCanvasGroup != null) _inventoryRootCanvasGroup = inventoryRootCanvasGroup;
        if (inventoryCodexButton != null) _inventoryCodexButton = inventoryCodexButton;
        _isInitialized = _codexPresenter != null && _hudOpenButton != null
            && _closeButton != null && _inventoryButton != null;
        _transitionFrame = -1;
        bool canNavigateInventory = _inventoryPresenter != null
            && _inventoryRootCanvasGroup != null && _inventoryCodexButton != null;
        if (_mapButton != null) _mapButton.interactable = false;
        if (_inventoryButton != null) _inventoryButton.interactable = canNavigateInventory;
        if (_inventoryCodexButton != null) _inventoryCodexButton.interactable = canNavigateInventory;
        if (!_isInitialized) return false;
        RegisterListeners();
        _codexPresenter.Close();
        _inventoryPresenter?.Close();
        return true;
    }

    public void OpenCodex()
    {
        if (!TryBeginTransition() || _codexPresenter.IsOpen) return;
        _inventoryPresenter?.Close();
        _codexPresenter.Open();
        SyncPlayerLock();
    }

    public void CloseCodex()
    {
        if (!TryBeginTransition()) return;
        _codexPresenter.Close();
        SyncPlayerLock();
    }

    public void ToggleCodex()
    {
        if (!TryBeginTransition()) return;
        if (_codexPresenter.IsOpen) _codexPresenter.Close();
        else
        {
            _inventoryPresenter?.Close();
            _codexPresenter.Open();
        }
        SyncPlayerLock();
    }

    public void OpenInventory()
    {
        if (!TryBeginTransition() || _inventoryPresenter == null
            || _inventoryRootCanvasGroup == null) return;
        _codexPresenter.Close();
        _inventoryPresenter.Open();
        SyncPlayerLock();
    }

    private void TryInitializeFromGameRoot()
    {
        GameRoot root = GameRoot.Instance;
        if (root == null || root.GameData?.CodexData == null || root.Codex == null
            || root.Inventory == null || root.Localization == null || _codexPresenter == null) return;
        ResolveInventoryBridge();
        if (!_codexPresenter.Init(root.GameData.CodexData, root.Codex,
            root.Inventory, root.Localization)) return;
        _inventoryPresenter?.Init(root.Inventory);
        Init(_inventoryPresenter, _inventoryRootCanvasGroup, _inventoryCodexButton);
    }

    private void ResolveInventoryBridge()
    {
        _inventoryBridge ??= Object.FindFirstObjectByType<InventoryNavigationBridge>(
            FindObjectsInactive.Include);
        if (_inventoryBridge == null) return;
        _inventoryPresenter ??= _inventoryBridge.Presenter;
        _inventoryRootCanvasGroup ??= _inventoryBridge.RootCanvasGroup;
        _inventoryCodexButton ??= _inventoryBridge.CodexButton;
    }

    private bool TryBeginTransition()
    {
        if (!_isInitialized || _transitionFrame == Time.frameCount) return false;
        _transitionFrame = Time.frameCount;
        return true;
    }

    private bool IsInventoryOpen() => _inventoryRootCanvasGroup != null
        && _inventoryRootCanvasGroup.gameObject.activeInHierarchy
        && _inventoryRootCanvasGroup.interactable && _inventoryRootCanvasGroup.alpha > 0.5f;

    private void SyncPlayerLock()
    {
        bool inventoryOpen = IsInventoryOpen();
        if (_codexPresenter != null && _codexPresenter.IsOpen && inventoryOpen)
        {
            _inventoryPresenter?.Close();
            inventoryOpen = false;
        }
        if ((_codexPresenter != null && _codexPresenter.IsOpen) || inventoryOpen)
            AcquirePlayerLock();
        else ReleasePlayerLock();
    }

    private void AcquirePlayerLock()
    {
        if (_ownsPlayerLock) return;
        ResolvePlayerControllers();
        if (_playerInteractionController == null && _playerMoveController == null) return;
        _previousCanInteract = _playerInteractionController == null
            || _playerInteractionController.CanInteract;
        _previousCanMove = _playerMoveController == null || _playerMoveController.CanMove;
        _playerInteractionController?.SetCanInteract(false);
        _playerMoveController?.SetCanMove(false);
        _ownsPlayerLock = true;
    }

    private void ResolvePlayerControllers()
    {
        if (_attemptedPlayerResolution) return;
        _attemptedPlayerResolution = true;
        _playerInteractionController ??= Object.FindFirstObjectByType<PlayerInteractionController>();
        _playerMoveController ??= Object.FindFirstObjectByType<PlayerMoveController>();
    }

    private void ReleasePlayerLock()
    {
        if (_ownsPlayerLock)
        {
            _playerInteractionController?.SetCanInteract(_previousCanInteract);
            _playerMoveController?.SetCanMove(_previousCanMove);
            _ownsPlayerLock = false;
        }
        _attemptedPlayerResolution = false;
    }

    private void RegisterListeners()
    {
        _hudOpenButton.onClick.AddListener(OpenCodex);
        _closeButton.onClick.AddListener(CloseCodex);
        _inventoryButton.onClick.AddListener(OpenInventory);
        _inventoryCodexButton?.onClick.AddListener(OpenCodex);
    }

    private void UnregisterListeners()
    {
        _hudOpenButton?.onClick.RemoveListener(OpenCodex);
        _closeButton?.onClick.RemoveListener(CloseCodex);
        _inventoryButton?.onClick.RemoveListener(OpenInventory);
        _inventoryCodexButton?.onClick.RemoveListener(OpenCodex);
    }
}
