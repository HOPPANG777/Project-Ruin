//=============================================================================
// File:        BrewingRecipeCatalogSO.cs
//
// Description: 술 제조 레시피 데이터를 보관하고 조합 키로 판정하는 카탈로그.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using UnityEngine;

//=============================================================================
//
// Synopsis:   BrewingIngredientEntry;
//                - 레시피에 필요한 재료 한 종류와 개수
//=============================================================================
[Serializable]
public sealed class BrewingIngredientEntry
{
    [Tooltip("재료 아이템 ID입니다.")]
    [SerializeField] private string _itemId;

    [Tooltip("레시피에 필요한 해당 재료 개수입니다.")]
    [SerializeField] private int _count = 1;

    /// <summary>재료 아이템 ID입니다.</summary>
    public string ItemId => _itemId;

    /// <summary>레시피에 필요한 해당 재료 개수입니다.</summary>
    public int Count => _count;
}

//=============================================================================
//
// Synopsis:   BrewingRecipeEntry;
//                - 재료 조합 + 도구 → 결과 술 한 건의 원본 데이터
//=============================================================================
[Serializable]
public sealed class BrewingRecipeEntry
{
    [Tooltip("레시피 고유 ID입니다.")]
    [SerializeField] private string _recipeId;

    [Tooltip("필요한 재료 목록입니다. 종류별 개수의 합은 2~3이어야 합니다.")]
    [SerializeField] private List<BrewingIngredientEntry> _ingredients = new List<BrewingIngredientEntry>();

    [Tooltip("필요한 도구 아이템 ID입니다. 제조 시 소모되지 않습니다.")]
    [SerializeField] private string _toolId;

    [Tooltip("제조 결과로 생성되는 술 아이템 ID입니다.")]
    [SerializeField] private string _resultItemId;

    /// <summary>레시피 고유 ID입니다.</summary>
    public string RecipeId => _recipeId;

    /// <summary>필요한 재료 목록입니다.</summary>
    public IReadOnlyList<BrewingIngredientEntry> Ingredients => _ingredients;

    /// <summary>필요한 도구 아이템 ID입니다.</summary>
    public string ToolId => _toolId;

    /// <summary>제조 결과 술 아이템 ID입니다.</summary>
    public string ResultItemId => _resultItemId;
}

//=============================================================================
//
// Synopsis:   BrewingRecipeCatalogSO;
//                - 레시피를 "정렬된 재료 ID 나열 + 도구 ID" 키로 색인해 O(1) 판정
//                - 잘못된 항목과 중복 조합은 색인에서 제외하고 Error 로그를 남김
//=============================================================================
[CreateAssetMenu(fileName = "BrewingRecipeCatalog", menuName = "Ruin Conservator/Brewing/Recipe Catalog")]
public sealed class BrewingRecipeCatalogSO : ScriptableObject, IBrewingRecipeBook
{
    //========================================
    // 직렬화 필드
    //========================================

    [Tooltip("술 제조 레시피 목록입니다. 재료 조합과 도구가 같은 레시피는 중복 등록할 수 없습니다.")]
    [SerializeField] private List<BrewingRecipeEntry> _recipes = new List<BrewingRecipeEntry>();

    //========================================
    // 내부 상태
    //========================================

    // 조합 키 → 레시피 색인입니다.
    private readonly Dictionary<string, BrewingRecipe> _recipeByKey =
        new Dictionary<string, BrewingRecipe>(StringComparer.Ordinal);

    // 재료·도구 ID 중복 등록을 막는 집합입니다.
    private readonly HashSet<string> _ingredientIdSet = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _toolIdSet = new HashSet<string>(StringComparer.Ordinal);

    // 선택 패널 표시 순서를 유지하는 목록입니다.
    private readonly List<string> _ingredientIds = new List<string>();
    private readonly List<string> _toolIds = new List<string>();

    // 키 생성용 재사용 버퍼입니다. 조회마다 리스트를 새로 만들지 않기 위해 둡니다.
    private readonly List<string> _keyBuffer = new List<string>(BrewingRecipe.MAX_INGREDIENT_COUNT);

    private bool _isIndexBuilt;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>선택 패널에 표시할 재료 아이템 ID 목록입니다.</summary>
    public IReadOnlyList<string> IngredientItemIds
    {
        get { EnsureIndex(); return _ingredientIds; }
    }

    /// <summary>선택 패널에 표시할 도구 아이템 ID 목록입니다.</summary>
    public IReadOnlyList<string> ToolItemIds
    {
        get { EnsureIndex(); return _toolIds; }
    }

    //******************************************************************************
    //
    // Unity Lifecycle
    //
    //******************************************************************************

    private void OnEnable()
    {
        _isIndexBuilt = false;
    }

#if UNITY_EDITOR
    // 데이터 검증 단계: 인스펙터 수정 즉시 색인을 다시 만들어 오류를 콘솔에 노출한다.
    private void OnValidate()
    {
        _isIndexBuilt = false;
        EnsureIndex();
    }
#endif

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    /// <summary>레시피에 쓰이는 재료 아이템인지 확인합니다.</summary>
    public bool IsIngredient(string itemId)
    {
        EnsureIndex();
        return !string.IsNullOrWhiteSpace(itemId) && _ingredientIdSet.Contains(itemId);
    }

    /// <summary>레시피에 쓰이는 도구 아이템인지 확인합니다.</summary>
    public bool IsTool(string itemId)
    {
        EnsureIndex();
        return !string.IsNullOrWhiteSpace(itemId) && _toolIdSet.Contains(itemId);
    }

    //=============================================================================
    // BrewingRecipeCatalogSO.TryFindRecipe
    //=============================================================================
    // Description: 재료를 정렬해 키를 만들고 색인에서 정확히 일치하는 레시피를 찾는다.
    //              부분 일치는 허용하지 않는다.
    //
    // Parameters:  ( IReadOnlyList<string> ingredientIds, string toolId,
    //                out BrewingRecipe recipe )
    //
    // Return:      bool — 일치하는 레시피가 있으면 true
    //
    //=============================================================================
    /// <summary>재료 조합과 도구가 정확히 일치하는 레시피를 찾습니다.</summary>
    public bool TryFindRecipe(
        IReadOnlyList<string> ingredientIds, string toolId, out BrewingRecipe recipe)
    {
        EnsureIndex();
        recipe = null;

        if (ingredientIds == null || string.IsNullOrWhiteSpace(toolId))
        {
            return false;
        }

        _keyBuffer.Clear();

        for (int i = 0; i < ingredientIds.Count; i++)
        {
            _keyBuffer.Add(ingredientIds[i]);
        }

        return _recipeByKey.TryGetValue(BuildKey(_keyBuffer, toolId), out recipe);
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    private void EnsureIndex()
    {
        if (_isIndexBuilt)
        {
            return;
        }

        _recipeByKey.Clear();
        _ingredientIdSet.Clear();
        _toolIdSet.Clear();
        _ingredientIds.Clear();
        _toolIds.Clear();
        _isIndexBuilt = true;

        for (int i = 0; _recipes != null && i < _recipes.Count; i++)
        {
            RegisterRecipe(_recipes[i], i);
        }
    }

    private void RegisterRecipe(BrewingRecipeEntry entry, int index)
    {
        if (!TryBuildKey(entry, out string key))
        {
            Debug.LogError($"[BrewingRecipeCatalogSO] 잘못된 레시피를 건너뜁니다. index={index}");
            return;
        }

        // 같은 조합에 다른 결과를 지정하는 중복 레시피는 판정이 모호해지므로 막는다.
        if (!_recipeByKey.TryAdd(key, new BrewingRecipe(entry.RecipeId, entry.ResultItemId)))
        {
            Debug.LogError($"[BrewingRecipeCatalogSO] 재료·도구 조합이 중복된 레시피를 건너뜁니다. recipeId={entry.RecipeId}");
            return;
        }

        for (int i = 0; i < entry.Ingredients.Count; i++)
        {
            AddUnique(_ingredientIdSet, _ingredientIds, entry.Ingredients[i].ItemId);
        }

        AddUnique(_toolIdSet, _toolIds, entry.ToolId);
    }

    // 키 생성 버퍼에 재료를 개수만큼 펼쳐 담고, 규칙 위반이면 false를 반환한다.
    private bool TryBuildKey(BrewingRecipeEntry entry, out string key)
    {
        key = null;

        if (entry == null || entry.Ingredients == null
            || string.IsNullOrWhiteSpace(entry.RecipeId)
            || string.IsNullOrWhiteSpace(entry.ToolId)
            || string.IsNullOrWhiteSpace(entry.ResultItemId))
        {
            return false;
        }

        _keyBuffer.Clear();

        for (int i = 0; i < entry.Ingredients.Count; i++)
        {
            BrewingIngredientEntry ingredient = entry.Ingredients[i];

            if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.ItemId)
                || ingredient.Count <= 0 || ingredient.Count > BrewingRecipe.MAX_INGREDIENT_COUNT)
            {
                return false;
            }

            for (int n = 0; n < ingredient.Count; n++)
            {
                _keyBuffer.Add(ingredient.ItemId);
            }
        }

        if (_keyBuffer.Count < BrewingRecipe.MIN_INGREDIENT_COUNT
            || _keyBuffer.Count > BrewingRecipe.MAX_INGREDIENT_COUNT)
        {
            return false;
        }

        key = BuildKey(_keyBuffer, entry.ToolId);
        return true;
    }

    // 정렬해서 키를 만들기 때문에 등록 순서가 달라도 같은 조합은 같은 키가 된다.
    private static string BuildKey(List<string> ingredientIds, string toolId)
    {
        ingredientIds.Sort(StringComparer.Ordinal);
        return string.Join(",", ingredientIds) + "|" + toolId;
    }

    private static void AddUnique(HashSet<string> set, List<string> list, string itemId)
    {
        if (set.Add(itemId))
        {
            list.Add(itemId);
        }
    }
}
