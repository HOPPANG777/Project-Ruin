//=============================================================================
// File:        IBrewingRecipeBook.cs
//
// Description: 술 제조 레시피 조회에 대한 읽기 전용 계약.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//========================================
// System Includes
//========================================
using System.Collections.Generic;

//=============================================================================
//
// Synopsis:   IBrewingRecipeBook;
//                - 재료 조합(순서 무관 다중집합) + 도구로 레시피를 찾는 계약
//                - 저장 방식(ScriptableObject, CSV 등)을 BrewingController에서 분리
//=============================================================================
public interface IBrewingRecipeBook
{
    /// <summary>선택 패널에 표시할 재료 아이템 ID 목록입니다.</summary>
    IReadOnlyList<string> IngredientItemIds { get; }

    /// <summary>선택 패널에 표시할 도구 아이템 ID 목록입니다.</summary>
    IReadOnlyList<string> ToolItemIds { get; }

    /// <summary>레시피에 쓰이는 재료 아이템인지 확인합니다.</summary>
    /// <param name="itemId">확인할 아이템 ID입니다.</param>
    /// <returns>재료 슬롯에 등록할 수 있는 아이템이면 true입니다.</returns>
    bool IsIngredient(string itemId);

    /// <summary>레시피에 쓰이는 도구 아이템인지 확인합니다.</summary>
    /// <param name="itemId">확인할 아이템 ID입니다.</param>
    /// <returns>도구 슬롯에 등록할 수 있는 아이템이면 true입니다.</returns>
    bool IsTool(string itemId);

    /// <summary>재료 조합과 도구가 정확히 일치하는 레시피를 찾습니다.</summary>
    /// <param name="ingredientIds">등록된 재료 ID입니다. 순서와 무관하며 중복 개수는 유지됩니다.</param>
    /// <param name="toolId">등록된 도구 ID입니다.</param>
    /// <param name="recipe">일치한 레시피입니다.</param>
    /// <returns>일치하는 레시피가 있으면 true입니다.</returns>
    bool TryFindRecipe(
        IReadOnlyList<string> ingredientIds, string toolId, out BrewingRecipe recipe);
}
