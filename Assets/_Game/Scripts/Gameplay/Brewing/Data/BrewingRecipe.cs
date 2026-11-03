//=============================================================================
// File:        BrewingRecipe.cs
//
// Description: 술 제조 레시피 판정 결과와 제조 규칙 상수를 정의한다.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//=============================================================================
//
// Synopsis:   BrewingRecipe;
//                - 재료·도구 조합이 일치했을 때 생성할 결과 술 정보
//                - 재료 슬롯 수와 제조 최소·최대 재료 수를 한곳에서 관리
//=============================================================================
public sealed class BrewingRecipe
{
    //========================================
    // 상수
    //========================================

    // 재료 슬롯 수이자 레시피 재료 개수의 상한입니다.
    public const int MAX_INGREDIENT_COUNT = 3;

    // 제조에 필요한 최소 재료 개수입니다.
    public const int MIN_INGREDIENT_COUNT = 2;

    // 제조 1회에 생성되는 술 개수입니다. (기획서: 현재 고정)
    public const int RESULT_COUNT = 1;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>레시피 고유 ID입니다.</summary>
    public string RecipeId { get; }

    /// <summary>제조 결과로 생성되는 술 아이템 ID입니다.</summary>
    public string ResultItemId { get; }

    //=============================================================================
    // BrewingRecipe.BrewingRecipe
    //=============================================================================
    // Description: Constructor.
    //
    // Parameters:  ( string recipeId, string resultItemId )
    //
    // Return:      N/A.
    //
    //=============================================================================
    public BrewingRecipe(string recipeId, string resultItemId)
    {
        RecipeId = recipeId ?? string.Empty;
        ResultItemId = resultItemId ?? string.Empty;
    }
}
