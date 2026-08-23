//=============================================================================
// File:        CodexCsvDefinitionLoader.cs
//
// Description: 도감 항목과 술 제조 재료 CSV를 불변 Definition으로 변환한다.
//
// History:     08/19/2026 + Created -- Codex
//=============================================================================

using System.Collections.Generic;

public static class CodexCsvDefinitionLoader
{
    private static readonly string[] ENTRY_HEADERS =
    {
        "entry_id", "category", "display_order", "source_id", "chapter"
    };

    private static readonly string[] INGREDIENT_HEADERS =
    {
        "entry_id", "ingredient_order", "item_id", "location_text_key"
    };

    /// <summary>도감 항목과 술 재료 CSV를 읽어 Definition 목록을 생성합니다.</summary>
    public static bool TryLoad(
        GameDataCsvFile entriesCsv,
        GameDataCsvFile ingredientsCsv,
        out List<CodexEntryDefinition> entries,
        out List<CodexIngredientDefinition> ingredients,
        out GameDataCsvLoadError loadError)
    {
        entries = null;
        ingredients = null;
        loadError = null;

        if (!CsvDefinitionReadUtility.TryParseOptionalDocument(
                entriesCsv, ENTRY_HEADERS, out CsvDocument entryDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseOptionalDocument(
                ingredientsCsv, INGREDIENT_HEADERS, out CsvDocument ingredientDocument, out loadError))
        {
            return false;
        }

        entries = new List<CodexEntryDefinition>();
        ingredients = new List<CodexIngredientDefinition>();
        return TryReadEntries(entryDocument, entries, out loadError)
            && TryReadIngredients(ingredientDocument, ingredients, out loadError);
    }

    private static bool TryReadEntries(
        CsvDocument document,
        List<CodexEntryDefinition> entries,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (document == null) return true;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "entry_id", out string entryId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(
                    row, document.SourceName, "category", out string rawCategory, out loadError)
                || !TryParseCategory(row, document.SourceName, rawCategory, out CodexCategory category, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "display_order", out int displayOrder, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "source_id", out string sourceId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "chapter", out int chapter, out loadError))
            {
                return false;
            }
            entries.Add(new CodexEntryDefinition(entryId, category, displayOrder, sourceId, chapter));
        }
        return true;
    }

    private static bool TryReadIngredients(
        CsvDocument document,
        List<CodexIngredientDefinition> ingredients,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (document == null) return true;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "entry_id", out string entryId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "ingredient_order", out int order, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "item_id", out string itemId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row, document.SourceName, "location_text_key", out string locationKey, out loadError))
            {
                return false;
            }
            ingredients.Add(new CodexIngredientDefinition(entryId, order, itemId, locationKey));
        }
        return true;
    }

    private static bool TryParseCategory(
        CsvRow row,
        string sourceName,
        string rawCategory,
        out CodexCategory category,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;
        switch (rawCategory)
        {
            case "RELIC":
                category = CodexCategory.CODEX_CATEGORY_RELIC;
                return true;
            case "ALCOHOL":
                category = CodexCategory.CODEX_CATEGORY_ALCOHOL;
                return true;
            case "TECHNOLOGY":
                category = CodexCategory.CODEX_CATEGORY_TECHNOLOGY;
                return true;
            default:
                category = default;
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    sourceName, row, "category", "RELIC, ALCOHOL, TECHNOLOGY만 입력할 수 있습니다.");
                return false;
        }
    }
}

