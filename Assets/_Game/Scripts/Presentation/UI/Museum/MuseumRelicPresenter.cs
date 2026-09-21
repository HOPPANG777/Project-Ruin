//=============================================================================
// File:        MuseumRelicPresenter.cs
// Description: 유물 전용 선택 UI와 배치·회수 및 플레이어 잠금을 중개한다.
// History:     08/30/2026 + Created -- Codex
//=============================================================================
//========================================
// System Includes
//========================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>전용 UI의 선택 처리와 플레이어·외부 메뉴 입력 잠금을 소유합니다.</summary>
[DisallowMultipleComponent]
public sealed class MuseumRelicPresenter : MonoBehaviour
{
    [Tooltip("유물 전용 선택 목록을 표시하는 View입니다.")]
    [SerializeField] private MuseumRelicSelectionView _view;

    // UI를 갱신할 때 재사용하는 배치 가능 유물 목록입니다.
    private readonly List<string> _relicIds = new List<string>();
    // 전시 상태 조회·변경 및 이벤트를 제공하는 서비스입니다.
    private IMuseumRelicDisplayService _displays;
    // 보유 수량 조회와 원자적 아이템 변경을 수행하는 서비스입니다.
    private IInventoryService _inventory;
    // 전용 UI 동안 일반 인벤토리 입력을 차단할 대상입니다.
    private InventoryPresenter _inventoryPresenter;
    // 실제 상호작용 호출자가 전달한 플레이어 참조입니다.
    private PlayerInteractionController _playerInteractionController;
    // 실제 상호작용 호출자의 이동 잠금 대상입니다.
    private PlayerMoveController _playerMoveController;
    // 현재 선택 UI가 대상으로 삼는 전시대의 안정적인 ID입니다.
    private string _pedestalId;
    // 배치·회수 처리 중 중복 선택을 차단합니다.
    private bool _isBusy;
    // 이 Presenter가 입력 잠금의 복원 책임을 갖는지 나타냅니다.
    private bool _ownsPlayerLock;
    // 전용 UI를 열기 전 상호작용 허용 상태입니다.
    private bool _previousCanInteract = true;
    // 전용 UI를 열기 전 이동 허용 상태입니다.
    private bool _previousCanMove = true;

    /// <summary>유물 선택 UI가 현재 열려 있는지 여부입니다.</summary>
    public bool IsOpen => _view != null && _view.IsOpen;
    /// <summary>다른 인벤토리·도감·지도 화면으로 이동을 막아야 하는지 여부입니다.</summary>
    public bool BlocksExternalNavigation => IsOpen;

    private void OnDisable()
    {
        Close();
    }

    private void OnDestroy()
    {
        if (_displays != null) _displays.ExhibitChanged -= HandleExhibitChanged;
        Close();
    }

    //=============================================================================
    // Init
    // Description: 전시 도메인과 인벤토리 및 일반 인벤토리 UI를 연결합니다.
    // Parameters: IMuseumRelicDisplayService displays, IInventoryService inventory, InventoryPresenter inventoryPresenter
    // Return: void
    //=============================================================================
    /// <summary>전시 도메인과 인벤토리 및 일반 인벤토리 UI를 연결합니다.</summary>
    public void Init(IMuseumRelicDisplayService displays, IInventoryService inventory,
        InventoryPresenter inventoryPresenter)
    {
        Close();
        if (_displays != null) _displays.ExhibitChanged -= HandleExhibitChanged;
        _displays = displays;
        _inventory = inventory;
        _inventoryPresenter = inventoryPresenter;
        _pedestalId = string.Empty;
        _isBusy = false;
        if (_displays != null) _displays.ExhibitChanged += HandleExhibitChanged;
        if (_view == null)
        {
            Debug.LogWarning("[MuseumRelicPresenter] View가 연결되지 않았습니다.", this);
            return;
        }
        _view.Init(SelectRelic, Recover, Close);
    }

    //=============================================================================
    // Open
    // Description: 지정 전시대의 선택 UI를 열고 전달받은 플레이어 입력을 잠급니다.
    // Parameters: string pedestalId, PlayerInteractionController interactionController, PlayerMoveController moveController
    // Return: void
    //=============================================================================
    /// <summary>지정 전시대의 선택 UI를 열고 전달받은 플레이어 입력을 잠급니다.</summary>
    public void Open(string pedestalId, PlayerInteractionController interactionController,
        PlayerMoveController moveController)
    {
        if (_displays == null || _inventory == null || _view == null
            || _inventoryPresenter == null || interactionController == null || moveController == null
            || !isActiveAndEnabled
            || _inventoryPresenter.IsOpen || !interactionController.CanInteract || !moveController.CanMove
            || _view.IsOpen || _isBusy || string.IsNullOrWhiteSpace(pedestalId)) return;
        _displays.RegisterPedestal(pedestalId);
        _displays.RetryPendingImmediateRewards();
        _playerInteractionController = interactionController;
        _playerMoveController = moveController;
        _pedestalId = pedestalId;
        Refresh();
        if (_view.TryShow())
        {
            _inventoryPresenter?.SetExternalInputBlocked(true);
            AcquirePlayerLock();
        }
        else
        {
            _playerInteractionController = null;
            _playerMoveController = null;
        }
    }

    //=============================================================================
    // SelectRelic
    // Description: 배치 결과에 따라 UI를 닫거나 재시도 가능 상태로 복원합니다.
    // Parameters: int index
    // Return: void
    //=============================================================================
    private void SelectRelic(int index)
    {
        if (_isBusy || index < 0 || index >= _relicIds.Count) return;
        BeginInput();
        if (_displays.TryPlace(_pedestalId, _relicIds[index], out var failure)) Close();
        else
        {
            Debug.LogWarning($"[MuseumRelicPresenter] 배치 실패: {failure}", this);
            Refresh();
            EndInput();
        }
    }

    //=============================================================================
    // Recover
    // Description: 전시 유물을 회수하고 실패 시 UI를 복원합니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
    private void Recover()
    {
        if (_isBusy) return;
        BeginInput();
        if (_displays.TryRecover(_pedestalId, out var failure)) Close();
        else
        {
            Debug.LogWarning($"[MuseumRelicPresenter] 회수 실패: {failure}", this);
            Refresh();
            EndInput();
        }
    }

    private void Refresh()
    {
        _displays.FillPlaceableRelics(_relicIds);
        string currentName = string.Empty;
        if (_displays.TryGetDisplayedRelic(_pedestalId, out string currentId)
            && _inventory.TryGetItemDisplay(currentId, out InventoryItemDisplayData display))
            currentName = display.DisplayName;
        _view.Refresh(_displays.DisplayedRelicCount, _displays.RelicTypeCount,
            currentName, _relicIds, _inventory);
    }

    private void HandleExhibitChanged(string pedestalId)
    {
        if (IsOpen && pedestalId == _pedestalId && !_isBusy) Refresh();
    }

    private void BeginInput()
    {
        _isBusy = true;
        _view.SetInputEnabled(false);
    }

    private void EndInput()
    {
        _isBusy = false;
        _view.SetInputEnabled(true);
    }

    //=============================================================================
    // Close
    // Description: UI와 선택 버퍼를 정리하고 입력 잠금을 해제합니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
    private void Close()
    {
        _view?.Hide();
        _pedestalId = string.Empty;
        _relicIds.Clear();
        _isBusy = false;
        if (_ownsPlayerLock) _inventoryPresenter?.SetExternalInputBlocked(false);
        ReleasePlayerLock();
    }

    //=============================================================================
    // AcquirePlayerLock
    // Description: 이전 입력 상태를 기억하고 전용 UI가 잠금을 소유합니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
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

    //=============================================================================
    // ReleasePlayerLock
    // Description: 자신이 소유한 입력 잠금만 이전 상태로 복원합니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
    private void ReleasePlayerLock()
    {
        if (!_ownsPlayerLock) return;
        _playerInteractionController?.SetCanInteract(_previousCanInteract);
        _playerMoveController?.SetCanMove(_previousCanMove);
        _playerInteractionController = null;
        _playerMoveController = null;
        _ownsPlayerLock = false;
    }
}
