//=============================================================================
// File:        CodexNavigationPresenter.cs
// Description: 도감 입력, 창 전환과 플레이어 입력 잠금 생명주기를 관리한다.
// History:     08/22/2026 + Created -- Codex
//              08/23/2026 + 실제 인벤토리 표시 상태와 양방향 전환 연결 -- Codex
//              08/30/2026 + 유물 전용 선택 UI의 외부 이동 차단 -- Codex
//              08/31/2026 + 초기화 및 플레이어 참조를 GameRoot에서 명시적으로 주입 -- Codex
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
    private bool _previousCanInteract = true;
    private bool _previousCanMove = true;
    private int _transitionFrame = -1;

    /// <summary>도감 전환 입력을 받을 준비가 끝났는지 반환합니다.</summary>
    public bool IsInitialized => _isInitialized;

    private void Awake()
    {
        if (_hudOpenButton != null) _hudOpenButton.interactable = false;
    }

    private void Update()
    {
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
        if (_hudOpenButton != null) _hudOpenButton.interactable = _isInitialized;
        if (!_isInitialized) return false;
        RegisterListeners();
        _codexPresenter.Close();
        _inventoryPresenter?.Close();
        return true;
    }

    /// <summary>외부 전용 UI의 잠금을 존중하며 도감을 엽니다.</summary>
    public void OpenCodex()
    {
        if (IsExternalNavigationBlocked || !TryBeginTransition()
            || _codexPresenter.IsOpen) return;
        _inventoryPresenter?.Close();
        _codexPresenter.Open();
        SyncPlayerLock();
    }

    /// <summary>도감을 닫고 보유한 입력 잠금을 해제합니다.</summary>
    public void CloseCodex()
    {
        if (!TryBeginTransition()) return;
        _codexPresenter.Close();
        SyncPlayerLock();
    }

    /// <summary>같은 프레임의 중복 전환을 차단하며 도감을 토글합니다.</summary>
    public void ToggleCodex()
    {
        if (IsExternalNavigationBlocked || !TryBeginTransition()) return;
        if (_codexPresenter.IsOpen) _codexPresenter.Close();
        else
        {
            _inventoryPresenter?.Close();
            _codexPresenter.Open();
        }
        SyncPlayerLock();
    }

    /// <summary>도감을 닫고 연결된 인벤토리로 전환합니다.</summary>
    public void OpenInventory()
    {
        if (IsExternalNavigationBlocked || !TryBeginTransition()
            || _inventoryPresenter == null || _inventoryRootCanvasGroup == null) return;
        _codexPresenter.Close();
        _inventoryPresenter.Open();
        SyncPlayerLock();
    }

    //=============================================================================
    // TryInitialize / BindPlayer / UnbindPlayer
    // Description: 초기화와 씬 플레이어 참조는 소유자가 제공하며 전역 탐색을 하지 않습니다.
    // Parameters: 도감 서비스 또는 플레이어 입력 Controller 쌍
    // Return: bool / void
    //=============================================================================
    /// <summary>GameRoot의 저장·도메인 초기화 완료 후 직렬화된 UI 연결을 초기화합니다.</summary>
    public bool TryInitialize(ICodexDataRepository data, ICodexService codex,
        IInventoryService inventory, ILocalizationService localization)
    {
        OnDisable();
        UnregisterListeners();
        if (_hudOpenButton != null) _hudOpenButton.interactable = false;
        _isInitialized = false;
        if (_inventoryBridge == null || _inventoryBridge.Presenter == null || _codexPresenter == null)
            return false;
        if (_inventoryBridge.RootCanvasGroup == null || _inventoryBridge.CodexButton == null) return false;
        if (!_codexPresenter.Init(data, codex, inventory, localization)) return false;
        return Init(_inventoryBridge.Presenter, _inventoryBridge.RootCanvasGroup, _inventoryBridge.CodexButton);
    }

    /// <summary>플레이어 교체 시 이전 창과 잠금을 정리하고 새 참조를 연결합니다.</summary>
    public void BindPlayer(PlayerInteractionController interaction, PlayerMoveController movement)
    {
        if (_playerInteractionController == interaction && _playerMoveController == movement) return;
        _codexPresenter?.Close();
        _inventoryPresenter?.Close();
        ReleasePlayerLock();
        _playerInteractionController = interaction;
        _playerMoveController = movement;
    }

    /// <summary>현재 플레이어가 비활성화될 때만 연결과 잠금을 해제합니다.</summary>
    public void UnbindPlayer(PlayerInteractionController interaction, PlayerMoveController movement)
    {
        if (_playerInteractionController == interaction && _playerMoveController == movement)
            BindPlayer(null, null);
    }

    private bool TryBeginTransition()
    {
        if (!_isInitialized || !isActiveAndEnabled || _transitionFrame == Time.frameCount) return false;
        _transitionFrame = Time.frameCount;
        return true;
    }

    private bool IsExternalNavigationBlocked =>
        _inventoryPresenter != null && _inventoryPresenter.IsExternalInputBlocked;

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
        if (_playerInteractionController == null && _playerMoveController == null) return;
        _previousCanInteract = _playerInteractionController == null
            || _playerInteractionController.CanInteract;
        _previousCanMove = _playerMoveController == null || _playerMoveController.CanMove;
        _playerInteractionController?.SetCanInteract(false);
        _playerMoveController?.SetCanMove(false);
        _ownsPlayerLock = true;
    }

    private void ReleasePlayerLock()
    {
        if (!_ownsPlayerLock) return;
        _playerInteractionController?.SetCanInteract(_previousCanInteract);
        _playerMoveController?.SetCanMove(_previousCanMove);
        _ownsPlayerLock = false;
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
