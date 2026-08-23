//=============================================================================
// File:        CodexEntryDefinition.cs
//
// Description: 도감 항목과 술 제조 재료의 불변 런타임 정의
//
// History:     08/19/2026 + Created -- Codex
//=============================================================================

public enum CodexCategory
{
    CODEX_CATEGORY_RELIC = 0,
    CODEX_CATEGORY_ALCOHOL,
    CODEX_CATEGORY_TECHNOLOGY
}

public sealed class CodexEntryDefinition
{
    public string EntryId { get; }
    public CodexCategory Category { get; }
    public int DisplayOrder { get; }
    public string SourceId { get; }
    public int Chapter { get; }

    /// <summary>검증이 끝난 도감 항목 정의를 생성합니다.</summary>
    public CodexEntryDefinition(
        string entryId,
        CodexCategory category,
        int displayOrder,
        string sourceId,
        int chapter)
    {
        EntryId = entryId ?? string.Empty;
        Category = category;
        DisplayOrder = displayOrder;
        SourceId = sourceId ?? string.Empty;
        Chapter = chapter;
    }
}

public sealed class CodexIngredientDefinition
{
    public string EntryId { get; }
    public int IngredientOrder { get; }
    public string ItemId { get; }
    public string LocationTextKey { get; }

    /// <summary>검증이 끝난 술 도감 재료 정의를 생성합니다.</summary>
    public CodexIngredientDefinition(
        string entryId,
        int ingredientOrder,
        string itemId,
        string locationTextKey)
    {
        EntryId = entryId ?? string.Empty;
        IngredientOrder = ingredientOrder;
        ItemId = itemId ?? string.Empty;
        LocationTextKey = locationTextKey ?? string.Empty;
    }
}

