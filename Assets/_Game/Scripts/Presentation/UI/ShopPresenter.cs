//=============================================================================
// File:        ShopPresenter.cs
//
// Description: 상점 열기, 목록 구성, 선택 상태와 거래 UI 갱신을 중개한다.
//
// History:     08/12/2026 + Created -- Codex
//
//=============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

//=============================================================================
//
// Synopsis:   ShopPresenter;
//                - 구매 목록과 획득 순 판매 목록 Bind
//                - 수량 선택 및 확인 시 ShopController 거래 호출
//=============================================================================
[DisallowMultipleComponent]
public sealed class ShopPresenter : MonoBehaviour
{
    [SerializeField] private ShopView _view;
    [SerializeField] private PlayerMoveController _playerMoveController;
    [SerializeField] private PlayerInteractionController _playerInteractionController;

    private ShopController _shopController;
    private IInventoryService _inventoryService;
    private IDialogueService _dialogueService;
    private ShopInputLock _inputLock;
    private ShopDefinition _currentShop;
    private readonly Dictionary<string, InventoryItemDisplayData> _displayCache = new Dictionary<string, InventoryItemDisplayData>(StringComparer.Ordinal);
    private ShopItemView _hoveredView;
    private InventoryItemDisplayData _selectedData;
    private ShopUiMode _selectedMode;
    private int _selectedQuantity;
    private int _selectedMaximum;
    private bool _isConfirming;

    private void OnDisable()
    {
        Close();
    }

    private void OnDestroy()
    {
        Close();
        Unsubscribe();
    }

    /// <summary>거래 Controller, 인벤토리와 View 입력을 연결합니다.</summary>
    public void Init(
        ShopController shopController,
        IInventoryService inventoryService,
        IDialogueService dialogueService)
    {
        Close();
        Unsubscribe();
        _shopController = shopController;
        _inventoryService = inventoryService;
        _dialogueService = dialogueService;
        _inputLock = new ShopInputLock(_playerMoveController, _playerInteractionController);
        _displayCache.Clear();

        if (_view == null || _shopController == null || _inventoryService == null)
        {
            return;
        }

        _view.Init();
        _view.CloseRequested += Close;
        _view.IncreaseRequested += HandleIncrease;
        _view.DecreaseRequested += HandleDecrease;
        _view.CancelRequested += HandleCancel;
        _view.ConfirmRequested += HandleConfirm;
        _inventoryService.ItemCountChanged += HandleItemCountChanged;

        if (_dialogueService != null)
        {
            _dialogueService.ShopOpenRequested += HandleShopOpenRequested;
        }
    }

    /// <summary>유효한 상점 데이터를 조회해 메인 창과 두 목록을 엽니다.</summary>
    public bool Open(string shopId)
    {
        if (_view == null || !_view.IsConfigured
            || _shopController == null || _inventoryService == null
            || _inputLock == null || !_inputLock.IsConfigured
            || !_shopController.TryGetShop(shopId, out ShopDefinition shop))
        {
            Close();
            return false;
        }

        _currentShop = shop;
        _inputLock.Acquire();
        ResetSelection();
        _view.Show();
        RefreshBuyList();
        RefreshSellList();
        return true;
    }

    /// <summary>상점과 하위 창을 닫고 선택·처리 상태를 초기화합니다.</summary>
    public void Close()
    {
        _isConfirming = false;
        _currentShop = null;
        ResetSelection();
        _view?.Hide();
        _inputLock?.Release();
    }

    private void HandleShopOpenRequested(string shopId) => Open(shopId);

    private void RefreshBuyList()
    {
        if (_currentShop == null)
        {
            return;
        }

        int usedCount = 0;

        for (int i = 0; i < _currentShop.Stock.Count; i++)
        {
            ShopStockDefinition stock = _currentShop.Stock[i];

            if (!TryGetDisplay(stock.ItemId, out InventoryItemDisplayData data))
            {
                continue;
            }

            int currentStock = _shopController.GetStock(_currentShop.ShopId, stock.ItemId);
            _view.BindRow(
                ShopUiMode.SHOP_UI_BUY,
                usedCount++,
                data,
                currentStock,
                _inventoryService.GetItemCount(stock.ItemId),
                currentStock <= 0,
                HandleRowClicked,
                HandlePointerEntered,
                HandlePointerExited);
        }

        _view.CompleteRows(ShopUiMode.SHOP_UI_BUY, usedCount);
    }

    private void RefreshSellList()
    {
        if (_currentShop == null)
        {
            return;
        }

        IReadOnlyList<string> sellItemIds = _shopController.GetSellableItemIds(_currentShop.ShopId);
        int usedCount = 0;

        for (int i = 0; i < sellItemIds.Count; i++)
        {
            string itemId = sellItemIds[i];

            if (!TryGetDisplay(itemId, out InventoryItemDisplayData data))
            {
                continue;
            }

            int ownedQuantity = _inventoryService.GetItemCount(itemId);
            _view.BindRow(
                ShopUiMode.SHOP_UI_SELL,
                usedCount++,
                data,
                ownedQuantity,
                ownedQuantity,
                false,
                HandleRowClicked,
                HandlePointerEntered,
                HandlePointerExited);
        }

        _view.CompleteRows(ShopUiMode.SHOP_UI_SELL, usedCount);
    }

    private bool TryGetDisplay(string itemId, out InventoryItemDisplayData data)
    {
        if (_displayCache.TryGetValue(itemId, out data))
        {
            return true;
        }

        if (!_inventoryService.TryGetItemDisplay(itemId, out data))
        {
            return false;
        }

        _displayCache.Add(itemId, data);
        return true;
    }

    private void HandleRowClicked(ShopItemView itemView)
    {
        if (_currentShop == null || _isConfirming || itemView == null || itemView.DisplayData == null)
        {
            return;
        }

        int maximum = itemView.Mode == ShopUiMode.SHOP_UI_BUY
            ? _shopController.GetMaxPurchaseQuantity(_currentShop.ShopId, itemView.DisplayData.ItemId)
            : _shopController.GetMaxSaleQuantity(_currentShop.ShopId, itemView.DisplayData.ItemId);

        if (maximum <= 0
            || !_shopController.TryGetTotalPrice(itemView.DisplayData.ItemId, maximum, out int totalPrice))
        {
            return;
        }

        _selectedData = itemView.DisplayData;
        _selectedMode = itemView.Mode;
        _selectedQuantity = maximum;
        _selectedMaximum = maximum;
        _view.ShowTransaction(_selectedMode, _selectedData, _selectedQuantity, totalPrice);
    }

    private void HandleIncrease(ShopUiMode mode)
    {
        if (!HasSelection(mode) || _selectedQuantity >= _selectedMaximum)
        {
            return;
        }

        _selectedQuantity++;
        RefreshTransaction();
    }

    private void HandleDecrease(ShopUiMode mode)
    {
        if (!HasSelection(mode) || _selectedQuantity <= 1)
        {
            return;
        }

        _selectedQuantity--;
        RefreshTransaction();
    }

    private void RefreshTransaction()
    {
        if (_shopController.TryGetTotalPrice(_selectedData.ItemId, _selectedQuantity, out int totalPrice))
        {
            _view.UpdateTransaction(_selectedMode, _selectedQuantity, totalPrice);
        }
    }

    private void HandleCancel(ShopUiMode mode)
    {
        if (HasSelection(mode))
        {
            ResetSelection();
        }
    }

    private void HandleConfirm(ShopUiMode mode)
    {
        if (!HasSelection(mode) || _isConfirming)
        {
            return;
        }

        _isConfirming = true;
        string itemId = _selectedData.ItemId;
        if (mode == ShopUiMode.SHOP_UI_BUY)
        {
            _shopController.TryPurchase(_currentShop.ShopId, itemId, _selectedQuantity);
        }
        else
        {
            _shopController.TrySell(_currentShop.ShopId, itemId, _selectedQuantity);
        }

        _isConfirming = false;
        ResetSelection();

        if (mode == ShopUiMode.SHOP_UI_BUY)
        {
            RefreshBuyList();
        }

        RefreshSellList();

    }

    private void HandlePointerEntered(ShopItemView itemView)
    {
        if (itemView == null || itemView.DisplayData == null)
        {
            return;
        }

        _hoveredView = itemView;
        _view.ShowTooltip(itemView.DisplayData, itemView.OwnedQuantity);
    }

    private void HandlePointerExited(ShopItemView itemView)
    {
        if (_hoveredView != itemView)
        {
            return;
        }

        _hoveredView = null;
        _view.HideTooltip();
    }

    private void HandleItemCountChanged(string itemId, int finalCount)
    {
        if (_currentShop == null || _isConfirming)
        {
            return;
        }

        ResetSelection();

        if (IsStockItem(itemId))
        {
            RefreshBuyList();
        }

        if (!string.Equals(itemId, _currentShop.CurrencyItemId, StringComparison.Ordinal))
        {
            RefreshSellList();
        }
    }

    private bool IsStockItem(string itemId)
    {
        for (int i = 0; i < _currentShop.Stock.Count; i++)
        {
            if (string.Equals(_currentShop.Stock[i].ItemId, itemId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSelection(ShopUiMode mode)
    {
        return _currentShop != null && _selectedData != null && _selectedMode == mode;
    }

    private void ResetSelection()
    {
        _hoveredView = null;
        _selectedData = null;
        _selectedQuantity = 0;
        _selectedMaximum = 0;
        _view?.HideTooltip();
        _view?.HideTransactions();
    }

    private void Unsubscribe()
    {
        if (_view != null)
        {
            _view.CloseRequested -= Close;
            _view.IncreaseRequested -= HandleIncrease;
            _view.DecreaseRequested -= HandleDecrease;
            _view.CancelRequested -= HandleCancel;
            _view.ConfirmRequested -= HandleConfirm;
        }

        if (_inventoryService != null)
        {
            _inventoryService.ItemCountChanged -= HandleItemCountChanged;
        }

        if (_dialogueService != null)
        {
            _dialogueService.ShopOpenRequested -= HandleShopOpenRequested;
        }
    }
}
