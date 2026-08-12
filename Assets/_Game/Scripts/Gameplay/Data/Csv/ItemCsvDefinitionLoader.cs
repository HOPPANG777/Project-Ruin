using System;
using System.Collections.Generic;

//==============================================================================
// 기능: class::ItemCsvDefinitionLoader
// 설명: items.csv를 인벤토리 아이템 Definition 목록으로 변환하는 로더
//==============================================================================
public static class ItemCsvDefinitionLoader
{
    private static readonly string[] ITEM_HEADERS =
    {
        "item_id",
        "display_name_key",
        "description_text_key",
        "icon_id",
        "price",
        "can_sell",
        "item_type",
        "max_stack_count"
    };

    public static bool TryLoad(
        GameDataCsvFile csvFile,
        out List<ItemDefinition> items,
        out GameDataCsvLoadError loadError)
    {
        items = null;
        loadError = null;

        if (!CsvDefinitionReadUtility.TryParseRequiredDocument(
            csvFile,
            ITEM_HEADERS,
            out CsvDocument document,
            out loadError))
        {
            return false;
        }

        List<ItemDefinition> loadedItems = new List<ItemDefinition>(document.Rows.Count);
        HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName, "item_id", out string itemId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(row, document.SourceName, "display_name_key", out string displayNameKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(row, document.SourceName, "description_text_key", out string descriptionTextKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(row, document.SourceName, "icon_id", out string iconId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(row, document.SourceName, "price", out int price, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredBool(row, document.SourceName, "can_sell", out bool canSell, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(row, document.SourceName, "item_type", out string itemTypeText, out loadError)
                || !CsvDefinitionReadUtility.TryGetPositiveInt(row, document.SourceName, "max_stack_count", out int maxStackCount, out loadError))
            {
                return false;
            }

            if (!Enum.TryParse(itemTypeText, false, out ItemType itemType)
                || !Enum.IsDefined(typeof(ItemType), itemType))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName,
                    row,
                    "item_type",
                    "지원하지 않는 아이템 타입입니다.");
                return false;
            }

            if (!itemIds.Add(itemId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName,
                    row,
                    "item_id",
                    "중복 아이템 ID가 있습니다.");
                return false;
            }

            loadedItems.Add(new ItemDefinition(
                itemId,
                displayNameKey,
                descriptionTextKey,
                iconId,
                price,
                canSell,
                itemType,
                maxStackCount));
        }

        items = loadedItems;
        return true;
    }
}
