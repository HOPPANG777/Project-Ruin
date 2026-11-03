//=============================================================================
// File:        BrewingControllerTests.cs
//
// Description: 술 제조 기획서의 검증 시나리오(T01~T14)와 레시피 카탈로그 규칙을 검증한다.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

//=============================================================================
//
// Synopsis:   BrewingControllerTests;
//                - 가짜 인벤토리로 슬롯 등록·차감·회수 규칙을 검증
//                - UI 점멸은 Presenter 책임이라 이 테스트에서 다루지 않음
//=============================================================================
[TestFixture]
public sealed class BrewingControllerTests
{
    //========================================
    // 상수 (구조 설명용 가상 데이터)
    //========================================

    private const string ITEM_A = "ITEM_A";
    private const string ITEM_B = "ITEM_B";
    private const string ITEM_C = "ITEM_C";
    private const string TOOL_X = "TOOL_X";
    private const string TOOL_Y = "TOOL_Y";
    private const string DRINK_1 = "DRINK_1"; // A,A,B + X
    private const string DRINK_2 = "DRINK_2"; // A,B,C + X
    private const string DRINK_3 = "DRINK_3"; // A,B + X
    private const string DRINK_4 = "DRINK_4"; // A,B + Y

    //========================================
    // 내부 상태
    //========================================

    private FakeInventory _inventory;
    private BrewingRecipeCatalogSO _catalog;
    private GameObject _controllerObject;
    private BrewingController _brewing;

    //******************************************************************************
    //
    // Setup / TearDown
    //
    //******************************************************************************

    [SetUp]
    public void SetUp()
    {
        _inventory = new FakeInventory();
        _catalog = CreateCatalog(
            CreateRecipe("BREW_0001", TOOL_X, DRINK_1, (ITEM_A, 2), (ITEM_B, 1)),
            CreateRecipe("BREW_0002", TOOL_X, DRINK_2, (ITEM_A, 1), (ITEM_B, 1), (ITEM_C, 1)),
            CreateRecipe("BREW_0003", TOOL_X, DRINK_3, (ITEM_A, 1), (ITEM_B, 1)),
            CreateRecipe("BREW_0004", TOOL_Y, DRINK_4, (ITEM_A, 1), (ITEM_B, 1)));
        _controllerObject = new GameObject(nameof(BrewingControllerTests));
        _brewing = _controllerObject.AddComponent<BrewingController>();
        _brewing.Init(_inventory, _catalog);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_controllerObject);
        UnityEngine.Object.DestroyImmediate(_catalog);
    }

    //******************************************************************************
    //
    // 제조 성공 / 판정
    //
    //******************************************************************************

    [Test] // T01
    public void BrewConsumesIngredientsAndKeepsTool()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A, ITEM_B);

        Assert.That(_brewing.TryBrew(), Is.True);

        Assert.That(_brewing.ResultItemId, Is.EqualTo(DRINK_3));
        Assert.That(_inventory.GetItemCount(ITEM_A), Is.Zero);
        Assert.That(_inventory.GetItemCount(ITEM_B), Is.Zero);
        Assert.That(_inventory.GetItemCount(TOOL_X), Is.EqualTo(1));
        Assert.That(_brewing.ToolItemId, Is.EqualTo(TOOL_X));
        Assert.That(_brewing.GetIngredientItemId(0), Is.Null);
    }

    [Test] // T02
    public void IngredientOrderDoesNotMatter()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1), (ITEM_C, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_C, ITEM_A, ITEM_B);

        Assert.That(_brewing.TryBrew(), Is.True);
        Assert.That(_brewing.ResultItemId, Is.EqualTo(DRINK_2));
    }

    [Test] // T03
    public void DuplicateIngredientsAreAllowedWhenOwned()
    {
        _inventory.Own((ITEM_A, 2), (ITEM_B, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A, ITEM_A, ITEM_B);

        Assert.That(_brewing.TryBrew(), Is.True);
        Assert.That(_brewing.ResultItemId, Is.EqualTo(DRINK_1));
        Assert.That(_inventory.GetItemCount(ITEM_A), Is.Zero);
    }

    [Test]
    public void SameIngredientsWithDifferentToolSelectDifferentRecipe()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1), (TOOL_Y, 1));
        Register(TOOL_Y, ITEM_A, ITEM_B);

        Assert.That(_brewing.TryBrew(), Is.True);
        Assert.That(_brewing.ResultItemId, Is.EqualTo(DRINK_4));
    }

    [Test] // T05
    public void SingleIngredientIsRejected()
    {
        _inventory.Own((ITEM_A, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A);

        Assert.That(_brewing.TryBrew(), Is.False);
        Assert.That(_inventory.GetItemCount(ITEM_A), Is.EqualTo(1));
    }

    [Test]
    public void MissingToolIsRejected()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1));
        Register(null, ITEM_A, ITEM_B);

        Assert.That(_brewing.TryBrew(), Is.False);
    }

    [Test] // T06
    public void UnknownCombinationConsumesNothing()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_C, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A, ITEM_C);

        Assert.That(_brewing.TryBrew(), Is.False);
        Assert.That(_brewing.ResultItemId, Is.Null);
        Assert.That(_inventory.GetItemCount(ITEM_A), Is.EqualTo(1));
        Assert.That(_inventory.GetItemCount(ITEM_C), Is.EqualTo(1));
    }

    //******************************************************************************
    //
    // 슬롯 등록 규칙
    //
    //******************************************************************************

    [Test] // T04
    public void SecondSlotIsRejectedWhenOnlyOneOwned()
    {
        _inventory.Own((ITEM_A, 1));

        Assert.That(_brewing.TrySetIngredient(0, ITEM_A), Is.True);
        Assert.That(_brewing.TrySetIngredient(1, ITEM_A), Is.False);
        Assert.That(_brewing.GetIngredientItemId(1), Is.Null);
    }

    [Test]
    public void SameItemCanReplaceItsOwnSlot()
    {
        _inventory.Own((ITEM_A, 1));
        _brewing.TrySetIngredient(0, ITEM_A);

        Assert.That(_brewing.TrySetIngredient(0, ITEM_A), Is.True);
    }

    [Test] // T10
    public void ReplacingIngredientDoesNotChangeInventory()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1));
        _brewing.TrySetIngredient(0, ITEM_A);

        Assert.That(_brewing.TrySetIngredient(0, ITEM_B), Is.True);

        Assert.That(_brewing.GetIngredientItemId(0), Is.EqualTo(ITEM_B));
        Assert.That(_inventory.GetItemCount(ITEM_A), Is.EqualTo(1));
        Assert.That(_inventory.GetItemCount(ITEM_B), Is.EqualTo(1));
    }

    [Test] // T11
    public void ItemsWithZeroCountCannotBeRegistered()
    {
        Assert.That(_brewing.TrySetIngredient(0, ITEM_A), Is.False);
        Assert.That(_brewing.TrySetTool(TOOL_X), Is.False);
    }

    [Test]
    public void WrongSlotTypeAndInvalidIndexAreRejected()
    {
        _inventory.Own((ITEM_A, 1), (TOOL_X, 1));

        Assert.That(_brewing.TrySetIngredient(0, TOOL_X), Is.False);
        Assert.That(_brewing.TrySetTool(ITEM_A), Is.False);
        Assert.That(_brewing.TrySetIngredient(-1, ITEM_A), Is.False);
        Assert.That(_brewing.TrySetIngredient(3, ITEM_A), Is.False);
    }

    //******************************************************************************
    //
    // 결과 슬롯 잠금
    //
    //******************************************************************************

    [Test] // T07
    public void ResultSlotBlocksRegistrationAndReplacement()
    {
        BrewDrink3();
        _inventory.Own((ITEM_A, 1), (TOOL_Y, 1));

        Assert.That(_brewing.TrySetIngredient(0, ITEM_A), Is.False);
        Assert.That(_brewing.TrySetTool(TOOL_Y), Is.False);
        Assert.That(_brewing.ToolItemId, Is.EqualTo(TOOL_X));
    }

    [Test] // T08
    public void ResultSlotStillAllowsRemoval()
    {
        BrewDrink3();
        int toolCount = _inventory.GetItemCount(TOOL_X);

        _brewing.ClearTool();

        Assert.That(_brewing.ToolItemId, Is.Null);
        Assert.That(_inventory.GetItemCount(TOOL_X), Is.EqualTo(toolCount));
    }

    [Test] // T09
    public void ResultSlotBlocksAnotherBrew()
    {
        BrewDrink3();

        Assert.That(_brewing.TryBrew(), Is.False);
        Assert.That(_brewing.ResultItemId, Is.EqualTo(DRINK_3));
    }

    [Test]
    public void CollectResultAddsOneDrinkAndUnlocksSlots()
    {
        BrewDrink3();

        Assert.That(_brewing.TryCollectResult(), Is.True);

        Assert.That(_inventory.GetItemCount(DRINK_3), Is.EqualTo(1));
        Assert.That(_brewing.ResultItemId, Is.Null);
        Assert.That(_brewing.TryCollectResult(), Is.False);
    }

    //******************************************************************************
    //
    // 팝업 종료 / 외부 변경 / 원자성
    //
    //******************************************************************************

    [Test] // T12
    public void CloseWithoutBrewingClearsSlotsAndKeepsInventory()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A, ITEM_B);

        _brewing.CloseSession();

        Assert.That(_brewing.GetIngredientItemId(0), Is.Null);
        Assert.That(_brewing.ToolItemId, Is.Null);
        Assert.That(_inventory.GetItemCount(ITEM_A), Is.EqualTo(1));
        Assert.That(_inventory.GetItemCount(ITEM_B), Is.EqualTo(1));
        Assert.That(_inventory.GetItemCount(TOOL_X), Is.EqualTo(1));
    }

    [Test] // T13
    public void CloseCollectsUncollectedDrink()
    {
        BrewDrink3();

        _brewing.CloseSession();

        Assert.That(_inventory.GetItemCount(DRINK_3), Is.EqualTo(1));
        Assert.That(_brewing.ResultItemId, Is.Null);
        Assert.That(_brewing.ToolItemId, Is.Null);
    }

    [Test] // T14
    public void BrewIsRejectedWhenCountDropsAfterRegistration()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A, ITEM_B);
        _inventory.Own((ITEM_A, 0));

        Assert.That(_brewing.TryBrew(), Is.False);
        Assert.That(_inventory.GetItemCount(ITEM_B), Is.EqualTo(1));
        Assert.That(_brewing.GetIngredientItemId(0), Is.EqualTo(ITEM_A));
    }

    [Test]
    public void ConsumeFailureRollsBackAlreadyConsumedIngredients()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A, ITEM_B);
        _inventory.FailConsumeAtCall = 1; // 두 번째 재료 차감에서 실패시킨다.

        Assert.That(_brewing.TryBrew(), Is.False);

        Assert.That(_brewing.ResultItemId, Is.Null);
        Assert.That(_inventory.GetItemCount(ITEM_A), Is.EqualTo(1));
        Assert.That(_inventory.GetItemCount(ITEM_B), Is.EqualTo(1));
    }

    [Test]
    public void SuccessfulBrewRaisesStateChangedOnce()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A, ITEM_B);
        int raised = 0;
        _brewing.StateChanged += delegate { raised++; };

        _brewing.TryBrew();

        Assert.That(raised, Is.EqualTo(1));
    }

    //******************************************************************************
    //
    // 레시피 카탈로그
    //
    //******************************************************************************

    [Test]
    public void CatalogListsEveryIngredientAndToolOnce()
    {
        Assert.That(_catalog.IngredientItemIds, Is.EquivalentTo(new[] { ITEM_A, ITEM_B, ITEM_C }));
        Assert.That(_catalog.ToolItemIds, Is.EquivalentTo(new[] { TOOL_X, TOOL_Y }));
    }

    [Test]
    public void CatalogRejectsDuplicateCombinationWithDifferentResult()
    {
        BrewingRecipeCatalogSO duplicated = CreateCatalog(
            CreateRecipe("BREW_A", TOOL_X, DRINK_1, (ITEM_A, 1), (ITEM_B, 1)),
            CreateRecipe("BREW_B", TOOL_X, DRINK_2, (ITEM_B, 1), (ITEM_A, 1)));
        LogAssert.Expect(LogType.Error, new Regex(@"\[BrewingRecipeCatalogSO\]"));

        bool found = duplicated.TryFindRecipe(new[] { ITEM_A, ITEM_B }, TOOL_X, out BrewingRecipe recipe);

        Assert.That(found, Is.True);
        Assert.That(recipe.ResultItemId, Is.EqualTo(DRINK_1));
        UnityEngine.Object.DestroyImmediate(duplicated);
    }

    //******************************************************************************
    //
    // Helpers
    //
    //******************************************************************************

    // 도구와 재료를 순서대로 등록한다. tool이 null이면 도구는 등록하지 않는다.
    private void Register(string tool, params string[] ingredients)
    {
        if (tool != null)
        {
            Assert.That(_brewing.TrySetTool(tool), Is.True);
        }

        for (int i = 0; i < ingredients.Length; i++)
        {
            Assert.That(_brewing.TrySetIngredient(i, ingredients[i]), Is.True);
        }
    }

    // A+B+도구X로 DRINK_3을 제조해 결과 슬롯을 채운다.
    private void BrewDrink3()
    {
        _inventory.Own((ITEM_A, 1), (ITEM_B, 1), (TOOL_X, 1));
        Register(TOOL_X, ITEM_A, ITEM_B);
        Assert.That(_brewing.TryBrew(), Is.True);
    }

    private static BrewingRecipeEntry CreateRecipe(
        string recipeId, string toolId, string resultId, params (string id, int count)[] ingredients)
    {
        List<BrewingIngredientEntry> list = new List<BrewingIngredientEntry>();

        for (int i = 0; i < ingredients.Length; i++)
        {
            BrewingIngredientEntry ingredient = new BrewingIngredientEntry();
            SetField(ingredient, "_itemId", ingredients[i].id);
            SetField(ingredient, "_count", ingredients[i].count);
            list.Add(ingredient);
        }

        BrewingRecipeEntry entry = new BrewingRecipeEntry();
        SetField(entry, "_recipeId", recipeId);
        SetField(entry, "_ingredients", list);
        SetField(entry, "_toolId", toolId);
        SetField(entry, "_resultItemId", resultId);
        return entry;
    }

    private static BrewingRecipeCatalogSO CreateCatalog(params BrewingRecipeEntry[] recipes)
    {
        BrewingRecipeCatalogSO catalog = ScriptableObject.CreateInstance<BrewingRecipeCatalogSO>();
        SetField(catalog, "_recipes", new List<BrewingRecipeEntry>(recipes));
        return catalog;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    //=============================================================================
    //
    // Synopsis:   FakeInventory;
    //                - 수량 조회·가산·차감만 구현한 테스트용 인벤토리
    //                - FailConsumeAtCall 번째 차감 호출을 실패시켜 롤백을 검증
    //=============================================================================
    private sealed class FakeInventory : IInventoryService
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private int _consumeCallCount;

        public int FailConsumeAtCall = -1;

        public event Action<string, int> ItemCountChanged;

        public void Own(params (string id, int count)[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                _counts[items[i].id] = items[i].count;
            }
        }

        public int GetItemCount(string itemId)
        {
            return _counts.TryGetValue(itemId, out int count) ? count : 0;
        }

        public bool TryAddItem(string itemId, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            _counts[itemId] = GetItemCount(itemId) + amount;
            ItemCountChanged?.Invoke(itemId, _counts[itemId]);
            return true;
        }

        public bool TryConsumeItem(string itemId, int amount)
        {
            if (_consumeCallCount++ == FailConsumeAtCall || amount <= 0
                || GetItemCount(itemId) < amount)
            {
                return false;
            }

            _counts[itemId] -= amount;
            ItemCountChanged?.Invoke(itemId, _counts[itemId]);
            return true;
        }

        public bool CanAddItem(string itemId, int amount) => amount > 0;
        public bool TryExchangeItem(string removeItemId, int removeAmount, string addItemId, int addAmount) => false;
        public bool TryGetItem(string itemId, out ItemDefinition itemData) { itemData = null; return false; }
        public bool TryGetItemDisplay(string itemId, out InventoryItemDisplayData displayData) { displayData = null; return false; }
        public IReadOnlyList<InventorySlotData> GetSlots(InventoryCategory category) => Array.Empty<InventorySlotData>();
        public IReadOnlyList<InventorySlotData> GetSlotsByAcquisitionOrder() => Array.Empty<InventorySlotData>();
        public bool TryMoveSlot(InventoryCategory category, int sourceIndex, int targetIndex) => false;
        public bool TryMarkItemChecked(string itemId) => false;
        public void Reset() => _counts.Clear();
    }
}
