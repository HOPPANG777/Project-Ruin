//=============================================================================
// File:        ShopController.cs
//
// Description: 상점 재고와 원자적인 구매·판매 거래를 관리한다.
//
// History:     08/11/2026 + Created -- Codex
//
//=============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

//=============================================================================
//
// Synopsis:   ShopController;
//                - 상점별 세션 재고와 최대 거래 수량 계산
//                - 확인 시 재검증 및 인벤토리 원자 교환 실행
//=============================================================================
public enum ShopTransactionResult
{
    SHOP_TRANSACTION_SUCCESS = 0,
    SHOP_TRANSACTION_INVALID_SHOP,
    SHOP_TRANSACTION_INVALID_ITEM,
    SHOP_TRANSACTION_INVALID_QUANTITY,
    SHOP_TRANSACTION_NOT_ENOUGH_CURRENCY,
    SHOP_TRANSACTION_NOT_ENOUGH_STOCK,
    SHOP_TRANSACTION_NOT_ENOUGH_ITEMS,
    SHOP_TRANSACTION_OVERFLOW,
    SHOP_TRANSACTION_INVENTORY_FAILED,
    SHOP_TRANSACTION_PROCESSING
}

[DisallowMultipleComponent]
public sealed class ShopController : MonoBehaviour
{
    private const string COIN_ITEM_ID = "ITEM_COIN";

    private IGameDataRepository _gameDataRepository;
    private IInventoryService _inventoryService;
    private readonly Dictionary<string, Dictionary<string, int>> _stockByShopId =
        new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
    private readonly List<string> _sellItemIds = new List<string>();
    private readonly HashSet<string> _sellItemIdSet = new HashSet<string>(StringComparer.Ordinal);
    private bool _isProcessing;

    /// <summary>게임 데이터와 인벤토리를 연결하고 세션 재고를 초기화합니다.</summary>
    public void Init(
        IGameDataRepository gameDataRepository,
        IInventoryService inventoryService)
    {
        _gameDataRepository = gameDataRepository;
        _inventoryService = inventoryService;
        _stockByShopId.Clear();
        _isProcessing = false;
    }

    /// <summary>상점 ID로 정의를 조회하고 세션 재고를 준비합니다.</summary>
    public bool TryGetShop(string shopId, out ShopDefinition shop)
    {
        return TryGetShopState(shopId, out shop, out _);
    }

    /// <summary>상점 상품의 현재 세션 재고를 반환합니다.</summary>
    public int GetStock(string shopId, string itemId)
    {
        if (!TryGetShopState(shopId, out _, out Dictionary<string, int> stock)
            || string.IsNullOrWhiteSpace(itemId)
            || !stock.TryGetValue(itemId, out int currentStock))
        {
            return 0;
        }

        return currentStock;
    }

    /// <summary>재고·화폐·인벤토리 int 수용량을 반영한 최대 구매 수량입니다.</summary>
    public int GetMaxPurchaseQuantity(string shopId, string itemId)
    {
        if (!TryGetShopState(shopId, out ShopDefinition shop, out Dictionary<string, int> stock)
            || string.IsNullOrWhiteSpace(itemId)
            || !stock.TryGetValue(itemId, out int currentStock)
            || currentStock <= 0
            || !TryGetPositivePriceItem(itemId, out ItemDefinition item))
        {
            return 0;
        }

        int currencyCount = _inventoryService.GetItemCount(shop.CurrencyItemId);
        int affordableAmount = currencyCount / item.Price;
        int inventoryCapacity = int.MaxValue - _inventoryService.GetItemCount(itemId);
        return Min(currentStock, affordableAmount, inventoryCapacity);
    }

    /// <summary>보유량·총가격·화폐 int 수용량을 반영한 최대 판매 수량입니다.</summary>
    public int GetMaxSaleQuantity(string shopId, string itemId)
    {
        if (!TryGetShopState(shopId, out ShopDefinition shop, out _)
            || !TryGetPositivePriceItem(itemId, out ItemDefinition item)
            || !item.CanSell || item.ItemId == shop.CurrencyItemId)
        {
            return 0;
        }

        int ownedAmount = _inventoryService.GetItemCount(itemId);
        int totalPriceCapacity = int.MaxValue / item.Price;
        int currencyCapacity = int.MaxValue - _inventoryService.GetItemCount(shop.CurrencyItemId);
        return Min(ownedAmount, totalPriceCapacity, currencyCapacity / item.Price);
    }

    /// <summary>현재 보유한 판매 가능 아이템 ID를 최근 획득 순으로 반환합니다.</summary>
    public IReadOnlyList<string> GetSellableItemIds(string shopId)
    {
        _sellItemIds.Clear();
        _sellItemIdSet.Clear();

        if (!TryGetShopState(shopId, out ShopDefinition shop, out _))
        {
            return _sellItemIds;
        }

        IReadOnlyList<InventorySlotData> slots = _inventoryService.GetSlotsByAcquisitionOrder();

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slot = slots[i];

            if (slot == null || slot.IsEmpty || _sellItemIdSet.Contains(slot.ItemId)
                || string.Equals(slot.ItemId, COIN_ITEM_ID, StringComparison.Ordinal)
                || !TryGetPositivePriceItem(slot.ItemId, out ItemDefinition item)
                || !item.CanSell || item.ItemId == shop.CurrencyItemId)
            {
                continue;
            }

            _sellItemIdSet.Add(item.ItemId);
            _sellItemIds.Add(item.ItemId);
        }

        return _sellItemIds;
    }

    /// <summary>단가와 수량의 안전한 int 총가격을 계산합니다.</summary>
    public bool TryGetTotalPrice(string itemId, int quantity, out int totalPrice)
    {
        totalPrice = 0;
        return TryGetPositivePriceItem(itemId, out ItemDefinition item)
            && TryCalculateTotalPrice(item.Price, quantity, out totalPrice);
    }

    /// <summary>구매 조건을 다시 검증하고 화폐와 상품을 원자적으로 교환합니다.</summary>
    public ShopTransactionResult TryPurchase(string shopId, string itemId, int quantity)
    {
        if (_isProcessing)
        {
            return ShopTransactionResult.SHOP_TRANSACTION_PROCESSING;
        }

        if (!TryGetShopState(shopId, out ShopDefinition shop, out Dictionary<string, int> stock))
        {
            return ShopTransactionResult.SHOP_TRANSACTION_INVALID_SHOP;
        }

        if (string.IsNullOrWhiteSpace(itemId)
            || !stock.TryGetValue(itemId, out int currentStock)
            || !TryGetPositivePriceItem(itemId, out ItemDefinition item))
        {
            return ShopTransactionResult.SHOP_TRANSACTION_INVALID_ITEM;
        }

        if (quantity <= 0)
        {
            return ShopTransactionResult.SHOP_TRANSACTION_INVALID_QUANTITY;
        }

        if (!TryCalculateTotalPrice(item.Price, quantity, out int totalPrice))
        {
            return ShopTransactionResult.SHOP_TRANSACTION_OVERFLOW;
        }

        if (currentStock < quantity)
        {
            return ShopTransactionResult.SHOP_TRANSACTION_NOT_ENOUGH_STOCK;
        }

        if (_inventoryService.GetItemCount(shop.CurrencyItemId) < totalPrice)
        {
            return ShopTransactionResult.SHOP_TRANSACTION_NOT_ENOUGH_CURRENCY;
        }

        if (!_inventoryService.CanAddItem(item.ItemId, quantity))
        {
            return ShopTransactionResult.SHOP_TRANSACTION_OVERFLOW;
        }

        return ExecutePurchase(shop, stock, item.ItemId, quantity, totalPrice);
    }

    /// <summary>판매 조건을 다시 검증하고 아이템을 상점 화폐로 원자 교환합니다.</summary>
    public ShopTransactionResult TrySell(string shopId, string itemId, int quantity)
    {
        if (_isProcessing)
        {
            return ShopTransactionResult.SHOP_TRANSACTION_PROCESSING;
        }

        if (!TryGetShopState(shopId, out ShopDefinition shop, out _))
        {
            return ShopTransactionResult.SHOP_TRANSACTION_INVALID_SHOP;
        }

        if (!TryGetPositivePriceItem(itemId, out ItemDefinition item)
            || !item.CanSell || item.ItemId == shop.CurrencyItemId)
        {
            return ShopTransactionResult.SHOP_TRANSACTION_INVALID_ITEM;
        }

        if (quantity <= 0)
        {
            return ShopTransactionResult.SHOP_TRANSACTION_INVALID_QUANTITY;
        }

        if (_inventoryService.GetItemCount(item.ItemId) < quantity)
        {
            return ShopTransactionResult.SHOP_TRANSACTION_NOT_ENOUGH_ITEMS;
        }

        if (!TryCalculateTotalPrice(item.Price, quantity, out int totalPrice)
            || !_inventoryService.CanAddItem(shop.CurrencyItemId, totalPrice))
        {
            return ShopTransactionResult.SHOP_TRANSACTION_OVERFLOW;
        }

        return ExecuteSale(shop, item.ItemId, quantity, totalPrice);
    }

    private ShopTransactionResult ExecutePurchase(
        ShopDefinition shop,
        Dictionary<string, int> stock,
        string itemId,
        int quantity,
        int totalPrice)
    {
        _isProcessing = true;

        try
        {
            if (!_inventoryService.TryExchangeItem(
                shop.CurrencyItemId, totalPrice, itemId, quantity))
            {
                return ShopTransactionResult.SHOP_TRANSACTION_INVENTORY_FAILED;
            }

            stock[itemId] -= quantity;
            return ShopTransactionResult.SHOP_TRANSACTION_SUCCESS;
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private ShopTransactionResult ExecuteSale(
        ShopDefinition shop,
        string itemId,
        int quantity,
        int totalPrice)
    {
        _isProcessing = true;

        try
        {
            return _inventoryService.TryExchangeItem(
                itemId, quantity, shop.CurrencyItemId, totalPrice)
                ? ShopTransactionResult.SHOP_TRANSACTION_SUCCESS
                : ShopTransactionResult.SHOP_TRANSACTION_INVENTORY_FAILED;
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private bool TryGetShopState(
        string shopId,
        out ShopDefinition shop,
        out Dictionary<string, int> stock)
    {
        shop = null;
        stock = null;

        if (_gameDataRepository == null || _inventoryService == null
            || !_gameDataRepository.TryGetShop(shopId, out shop))
        {
            return false;
        }

        if (_stockByShopId.TryGetValue(shop.ShopId, out stock))
        {
            return true;
        }

        stock = new Dictionary<string, int>(shop.Stock.Count, StringComparer.Ordinal);

        for (int i = 0; i < shop.Stock.Count; i++)
        {
            ShopStockDefinition stockDefinition = shop.Stock[i];
            stock.Add(stockDefinition.ItemId, stockDefinition.InitialStock);
        }

        _stockByShopId.Add(shop.ShopId, stock);
        return true;
    }

    private bool TryGetPositivePriceItem(string itemId, out ItemDefinition item)
    {
        item = null;
        return _gameDataRepository != null
            && _gameDataRepository.TryGetItem(itemId, out item)
            && item.Price > 0;
    }

    private static bool TryCalculateTotalPrice(int unitPrice, int quantity, out int totalPrice)
    {
        totalPrice = 0;

        if (unitPrice <= 0 || quantity <= 0)
        {
            return false;
        }

        long calculatedPrice = (long)unitPrice * quantity;

        if (calculatedPrice > int.MaxValue)
        {
            return false;
        }

        totalPrice = (int)calculatedPrice;
        return true;
    }

    private static int Min(int first, int second, int third)
    {
        int minimum = first < second ? first : second;
        return minimum < third ? minimum : third;
    }
}
