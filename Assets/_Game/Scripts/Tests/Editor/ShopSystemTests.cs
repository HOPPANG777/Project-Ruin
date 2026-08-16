//=============================================================================
// File:        ShopSystemTests.cs
//
// Description: 재화 기반 인벤토리 변경, 원자적 교환, HUD 갱신을 검증한다.
//
// History:     08/11/2026 + Created -- Codex
//
//=============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

//=============================================================================
//
// Synopsis:   ShopSystemTests;
//                - Phase 1 코인 수량과 교환 불변식 검증
//=============================================================================
[TestFixture]
public sealed class ShopSystemTests
{
    private const string ITEM_COIN = "ITEM_COIN";
    private const string ITEM_TOOL = "ITEM_TOOL";
    private const string ITEM_EXPENSIVE = "ITEM_EXPENSIVE";
    private const string SHOP_ID = "SHOP_TEST";

    private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
    private InventoryController _inventory;
    private ShopController _shop;
    private ProgressController _progress;

    [SetUp]
    public void SetUp()
    {
        GameDataRepository repository = CreateRepository();
        GameObject inventoryObject = new GameObject(nameof(ShopSystemTests));
        _createdObjects.Add(inventoryObject);
        _progress = inventoryObject.AddComponent<ProgressController>();
        _inventory = inventoryObject.AddComponent<InventoryController>();
        _inventory.Init(repository, null, _progress);
        GameObject shopObject = new GameObject("ShopController");
        _createdObjects.Add(shopObject);
        _shop = shopObject.AddComponent<ShopController>();
        _shop.Init(repository, _inventory, _progress);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
        {
            UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
        }

        _createdObjects.Clear();
    }

    [Test]
    public void CoinCanBeAddedAndConsumed()
    {
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.Zero);
        Assert.That(_inventory.TryAddItem(ITEM_COIN, 10), Is.True);
        Assert.That(_inventory.TryConsumeItem(ITEM_COIN, 4), Is.True);
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(6));
        Assert.That(_inventory.TryConsumeItem(ITEM_COIN, 7), Is.False);
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(6));
        _inventory.Init(CreateRepository(), null, _progress);
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(6));
    }

    [Test]
    public void AddAndConsumeRaiseOneFinalCountEvent()
    {
        int eventCount = 0;
        int finalCount = -1;
        _inventory.ItemCountChanged += delegate(string itemId, int count)
        {
            if (itemId == ITEM_COIN)
            {
                eventCount++;
                finalCount = count;
            }
        };

        Assert.That(_inventory.TryAddItem(ITEM_COIN, 5), Is.True);
        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(finalCount, Is.EqualTo(5));
        Assert.That(_inventory.TryConsumeItem(ITEM_COIN, 2), Is.True);
        Assert.That(eventCount, Is.EqualTo(2));
        Assert.That(finalCount, Is.EqualTo(3));
    }

    [Test]
    public void ExchangeChangesBothItemsOnce()
    {
        Assert.That(_inventory.TryAddItem(ITEM_COIN, 20), Is.True);
        int eventCount = 0;
        _inventory.ItemCountChanged += delegate { eventCount++; };

        bool exchanged = _inventory.TryExchangeItem(ITEM_COIN, 12, ITEM_TOOL, 3);

        Assert.That(exchanged, Is.True);
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(8));
        Assert.That(_inventory.GetItemCount(ITEM_TOOL), Is.EqualTo(3));
        Assert.That(eventCount, Is.EqualTo(2));
    }

    [TestCase(21, 1), TestCase(1, 0), TestCase(0, 1)]
    public void InvalidExchangeLeavesInventoryUnchanged(int removeAmount, int addAmount)
    {
        Assert.That(_inventory.TryAddItem(ITEM_COIN, 20), Is.True);

        bool exchanged = _inventory.TryExchangeItem(ITEM_COIN, removeAmount, ITEM_TOOL, addAmount);

        Assert.That(exchanged, Is.False);
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(20));
        Assert.That(_inventory.GetItemCount(ITEM_TOOL), Is.Zero);
    }

    [Test]
    public void AdditionBeyondIntRangeIsRejected()
    {
        Assert.That(_inventory.TryAddItem(ITEM_COIN, int.MaxValue), Is.True);
        Assert.That(_inventory.CanAddItem(ITEM_COIN, 1), Is.False);
        Assert.That(_inventory.TryAddItem(ITEM_COIN, 1), Is.False);
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void CurrencyHudIgnoresOtherItems()
    {
        GameObject textObject = new GameObject("CoinCount", typeof(RectTransform), typeof(CanvasRenderer));
        _createdObjects.Add(textObject);
        TextMeshProUGUI countText = textObject.AddComponent<TextMeshProUGUI>();
        GameObject hudObject = new GameObject("CurrencyHud");
        _createdObjects.Add(hudObject);
        CurrencyHudPresenter hud = hudObject.AddComponent<CurrencyHudPresenter>();
        SetField(hud, "_coinCountText", countText);
        hud.Init(_inventory);

        Assert.That(countText.text, Is.EqualTo("0"));
        Assert.That(_inventory.TryAddItem(ITEM_TOOL, 2), Is.True);
        Assert.That(countText.text, Is.EqualTo("0"));
        Assert.That(_inventory.TryAddItem(ITEM_COIN, 7), Is.True);
        Assert.That(countText.text, Is.EqualTo("7"));
    }

    [Test]
    public void PurchaseUsesCurrentLimitsAndChangesAllState()
    {
        Assert.That(_inventory.TryAddItem(ITEM_COIN, 50), Is.True);
        Assert.That(_shop.GetMaxPurchaseQuantity(SHOP_ID, ITEM_TOOL), Is.EqualTo(5));
        Assert.That(_shop.TryGetTotalPrice(ITEM_TOOL, 5, out int totalPrice), Is.True);
        Assert.That(totalPrice, Is.EqualTo(50));

        ShopTransactionResult result = _shop.TryPurchase(SHOP_ID, ITEM_TOOL, 5);

        Assert.That(result, Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_SUCCESS));
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.Zero);
        Assert.That(_inventory.GetItemCount(ITEM_TOOL), Is.EqualTo(5));
        Assert.That(_shop.GetStock(SHOP_ID, ITEM_TOOL), Is.Zero);
        _shop.Init(CreateRepository(), _inventory, _progress);
        Assert.That(_shop.GetStock(SHOP_ID, ITEM_TOOL), Is.Zero);
    }

    [Test]
    public void PurchaseRevalidatesAndRejectsOverflow()
    {
        Assert.That(_inventory.TryAddItem(ITEM_COIN, 100), Is.True);
        Assert.That(_shop.GetMaxPurchaseQuantity(SHOP_ID, ITEM_TOOL), Is.EqualTo(5));
        Assert.That(_inventory.TryConsumeItem(ITEM_COIN, 90), Is.True);

        Assert.That(_shop.TryPurchase(SHOP_ID, ITEM_TOOL, 2),
            Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_NOT_ENOUGH_CURRENCY));
        Assert.That(_shop.TryPurchase(SHOP_ID, ITEM_TOOL, 6),
            Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_NOT_ENOUGH_STOCK));
        Assert.That(_shop.TryPurchase(SHOP_ID, ITEM_TOOL, 0),
            Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_INVALID_QUANTITY));
        Assert.That(_shop.TryGetTotalPrice(ITEM_EXPENSIVE, 2, out _), Is.False);
        Assert.That(_shop.TryPurchase(SHOP_ID, ITEM_EXPENSIVE, 2),
            Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_OVERFLOW));
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(10));
        Assert.That(_shop.GetStock(SHOP_ID, ITEM_TOOL), Is.EqualTo(5));
    }

    [Test]
    public void PurchaseRejectsReentrantConfirmation()
    {
        Assert.That(_inventory.TryAddItem(ITEM_COIN, 100), Is.True);
        ShopTransactionResult nestedResult = ShopTransactionResult.SHOP_TRANSACTION_SUCCESS;
        _inventory.ItemCountChanged += delegate(string itemId, int count)
        {
            if (itemId == ITEM_COIN)
            {
                nestedResult = _shop.TryPurchase(SHOP_ID, ITEM_TOOL, 1);
            }
        };

        ShopTransactionResult result = _shop.TryPurchase(SHOP_ID, ITEM_TOOL, 1);

        Assert.That(result, Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_SUCCESS));
        Assert.That(nestedResult, Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_PROCESSING));
        Assert.That(_inventory.GetItemCount(ITEM_TOOL), Is.EqualTo(1));
        Assert.That(_shop.GetStock(SHOP_ID, ITEM_TOOL), Is.EqualTo(4));
    }

    [Test]
    public void SellableQuestItemUsesExchangeAndRejectsInvalidRequests()
    {
        Assert.That(_inventory.TryAddItem(ITEM_TOOL, 3), Is.True);
        Assert.That(_shop.GetMaxSaleQuantity(SHOP_ID, ITEM_TOOL), Is.EqualTo(3));
        Assert.That(_shop.TrySell(SHOP_ID, ITEM_TOOL, 2),
            Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_SUCCESS));
        Assert.That(_inventory.GetItemCount(ITEM_TOOL), Is.EqualTo(1));
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(20));
        Assert.That(_shop.TrySell(SHOP_ID, ITEM_COIN, 1),
            Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_INVALID_ITEM));
        Assert.That(_shop.TrySell(SHOP_ID, ITEM_TOOL, 2),
            Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_NOT_ENOUGH_ITEMS));
    }

    [Test]
    public void SaleOverflowLeavesInventoryUnchanged()
    {
        Assert.That(_inventory.TryAddItem(ITEM_COIN, int.MaxValue - 5), Is.True);
        Assert.That(_inventory.TryAddItem(ITEM_TOOL, 1), Is.True);
        Assert.That(_shop.GetMaxSaleQuantity(SHOP_ID, ITEM_TOOL), Is.Zero);
        Assert.That(_shop.TrySell(SHOP_ID, ITEM_TOOL, 1),
            Is.EqualTo(ShopTransactionResult.SHOP_TRANSACTION_OVERFLOW));
        Assert.That(_inventory.GetItemCount(ITEM_TOOL), Is.EqualTo(1));
        Assert.That(_inventory.GetItemCount(ITEM_COIN), Is.EqualTo(int.MaxValue - 5));
    }

    [Test]
    public void SaleItemsFollowRecentAcquisitionOrder()
    {
        Assert.That(_inventory.TryAddItem(ITEM_TOOL, 1), Is.True);
        Assert.That(_inventory.TryAddItem(ITEM_EXPENSIVE, 1), Is.True);

        IReadOnlyList<string> itemIds = _shop.GetSellableItemIds(SHOP_ID);

        Assert.That(itemIds, Is.EqualTo(new[] { ITEM_EXPENSIVE, ITEM_TOOL }));
    }

    [Test]
    public void DialogueShopActionForwardsOnlyKnownShop()
    {
        DialogueController dialogue = _shop.gameObject.AddComponent<DialogueController>();
        SetField(dialogue, "_gameDataRepository", CreateRepository());
        string requestedShopId = null;
        dialogue.ShopOpenRequested += delegate(string shopId) { requestedShopId = shopId; };
        MethodInfo method = typeof(DialogueController).GetMethod("TryOpenShop", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        Assert.That(method.Invoke(dialogue, new object[] { SHOP_ID }), Is.True);
        Assert.That(requestedShopId, Is.EqualTo(SHOP_ID));
        Assert.That(method.Invoke(dialogue, new object[] { "SHOP_UNKNOWN" }), Is.False);
        Assert.That(requestedShopId, Is.EqualTo(SHOP_ID));
    }

    private static GameDataRepository CreateRepository()
    {
        ItemDefinition[] items =
        {
            new ItemDefinition(ITEM_COIN, "item.coin.name", "item.coin.description", string.Empty, 0, false, ItemType.ITEM_QUEST, int.MaxValue),
            new ItemDefinition(ITEM_TOOL, "item.tool.name", "item.tool.description", string.Empty, 10, true, ItemType.ITEM_QUEST, int.MaxValue),
            new ItemDefinition(ITEM_EXPENSIVE, "item.tool.name", "item.tool.description", string.Empty, int.MaxValue, true, ItemType.ITEM_MISC, int.MaxValue)
        };
        ShopStockDefinition[] stock =
        {
            new ShopStockDefinition(SHOP_ID, ITEM_TOOL, 0, 5),
            new ShopStockDefinition(SHOP_ID, ITEM_EXPENSIVE, 1, 2)
        };
        ShopDefinition[] shops =
        {
            new ShopDefinition(SHOP_ID, ITEM_COIN, stock)
        };
        LocalizationEntryDefinition[] localization =
        {
            new LocalizationEntryDefinition("item.coin.name", "코인", "Coin", "Coin"),
            new LocalizationEntryDefinition("item.coin.description", "재화", "Currency", "Currency"),
            new LocalizationEntryDefinition("item.tool.name", "도구", "Tool", "Tool"),
            new LocalizationEntryDefinition("item.tool.description", "도구", "Tool", "Tool")
        };
        bool created = GameDataRepository.TryCreate(
            Array.Empty<DialogueActorDefinition>(), Array.Empty<DialogueGraphDefinition>(), items, Array.Empty<QuestDefinition>(),
            localization, Array.Empty<NpcDialogueBindingDefinition>(), Array.Empty<NpcQuestDialogueRouteDefinition>(),
            Array.Empty<PuzzleDefinition>(), shops, out GameDataRepository repository, out GameDataRepositoryBuildError error);
        Assert.That(created, Is.True, error?.ToString());
        return repository;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
