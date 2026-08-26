//=============================================================================
// File:        MuseumBottleCsvDefinitionLoader.cs
// Description: 술병과 허용 술 CSV를 불변 Definition으로 변환한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;

public static class MuseumBottleCsvDefinitionLoader
{
    private static readonly string[] BOTTLE_HEADERS =
    {
        "bottle_id", "chapter", "display_name_key",
        "completion_reward_item_id", "completion_reward_amount"
    };
    private static readonly string[] ITEM_HEADERS =
    {
        "bottle_id", "item_order", "alcohol_item_id", "contribution_value"
    };

    /// <summary>선택 술병 CSV 두 개를 읽어 Definition 목록을 생성합니다.</summary>
    public static bool TryLoad(GameDataCsvFile bottlesCsv, GameDataCsvFile itemsCsv,
        out List<MuseumBottleDefinition> bottles,
        out List<MuseumBottleItemDefinition> items, out GameDataCsvLoadError loadError)
    {
        bottles = null;
        items = null;
        loadError = null;
        if (!CsvDefinitionReadUtility.TryParseOptionalDocument(
                bottlesCsv, BOTTLE_HEADERS, out CsvDocument bottleDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseOptionalDocument(
                itemsCsv, ITEM_HEADERS, out CsvDocument itemDocument, out loadError))
            return false;

        bottles = new List<MuseumBottleDefinition>();
        items = new List<MuseumBottleItemDefinition>();
        return TryReadBottles(bottleDocument, bottles, out loadError)
            && TryReadItems(itemDocument, items, out loadError);
    }

    private static bool TryReadBottles(CsvDocument document,
        List<MuseumBottleDefinition> bottles, out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (document == null) return true;
        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "bottle_id", out string bottleId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "chapter", out int chapter, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row, document.SourceName, "display_name_key", out string nameKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(
                    row, document.SourceName, "completion_reward_item_id", out string rewardId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalPositiveInt(
                    row, document.SourceName, "completion_reward_amount", out int? rewardAmount, out loadError))
                return false;
            bottles.Add(new MuseumBottleDefinition(
                bottleId, chapter, nameKey, rewardId, rewardAmount));
        }
        return true;
    }

    private static bool TryReadItems(CsvDocument document,
        List<MuseumBottleItemDefinition> items, out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (document == null) return true;
        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "bottle_id", out string bottleId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "item_order", out int itemOrder, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "alcohol_item_id", out string alcoholId, out loadError)
                || !CsvDefinitionReadUtility.TryGetPositiveInt(
                    row, document.SourceName, "contribution_value", out int value, out loadError))
                return false;
            items.Add(new MuseumBottleItemDefinition(bottleId, itemOrder, alcoholId, value));
        }
        return true;
    }
}
