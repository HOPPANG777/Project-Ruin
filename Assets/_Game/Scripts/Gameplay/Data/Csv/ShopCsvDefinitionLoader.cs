//=============================================================================
// File:        ShopCsvDefinitionLoader.cs
//
// Description: 상점과 초기 재고 CSV를 불변 Definition 목록으로 변환한다.
//
// History:     08/11/2026 + Created -- Codex
//
//=============================================================================

using System;
using System.Collections.Generic;

//=============================================================================
//
// Synopsis:   ShopStockDefinition, ShopDefinition, ShopCsvDefinitionLoader;
//                - 상점별 화폐와 정렬된 초기 재고 정의
//                - 중복 ID, 상품, 표시 순서 및 수량 검증
//=============================================================================
public sealed class ShopStockDefinition
{
    public string ShopId { get; }
    public string ItemId { get; }
    public int DisplayOrder { get; }
    public int InitialStock { get; }

    public ShopStockDefinition(string shopId, string itemId, int displayOrder, int initialStock)
    {
        ShopId = shopId ?? string.Empty;
        ItemId = itemId ?? string.Empty;
        DisplayOrder = displayOrder;
        InitialStock = initialStock;
    }
}

public sealed class ShopDefinition
{
    private readonly ShopStockDefinition[] _stock;

    public string ShopId { get; }
    public string CurrencyItemId { get; }
    public IReadOnlyList<ShopStockDefinition> Stock => _stock;

    public ShopDefinition(
        string shopId,
        string currencyItemId,
        IReadOnlyList<ShopStockDefinition> stock)
    {
        ShopId = shopId ?? string.Empty;
        CurrencyItemId = currencyItemId ?? string.Empty;
        int stockCount = stock != null ? stock.Count : 0;
        _stock = new ShopStockDefinition[stockCount];

        for (int i = 0; i < stockCount; i++)
        {
            _stock[i] = stock[i];
        }
    }
}

public static class ShopCsvDefinitionLoader
{
    private static readonly string[] SHOP_HEADERS =
    {
        "shop_id",
        "currency_item_id"
    };

    private static readonly string[] STOCK_HEADERS =
    {
        "shop_id",
        "item_id",
        "display_order",
        "initial_stock"
    };

    public static bool TryLoad(
        GameDataCsvFile shopsCsv,
        GameDataCsvFile shopStockCsv,
        out List<ShopDefinition> shops,
        out GameDataCsvLoadError loadError)
    {
        shops = null;
        loadError = null;

        if (!CsvDefinitionReadUtility.TryParseOptionalDocument(
            shopsCsv, SHOP_HEADERS, out CsvDocument shopDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseOptionalDocument(
                shopStockCsv, STOCK_HEADERS, out CsvDocument stockDocument, out loadError))
        {
            return false;
        }

        List<string> shopIds = new List<string>();
        Dictionary<string, string> currencyByShopId = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, List<ShopStockDefinition>> stockByShopId = new Dictionary<string, List<ShopStockDefinition>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> itemIdsByShopId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<int>> ordersByShopId = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        if (!TryReadShops(
            shopDocument, shopIds, currencyByShopId, stockByShopId,
            itemIdsByShopId, ordersByShopId, out loadError)
            || !TryReadStock(
                stockDocument, stockByShopId, itemIdsByShopId, ordersByShopId, out loadError))
        {
            return false;
        }

        shops = BuildDefinitions(shopIds, currencyByShopId, stockByShopId);
        return true;
    }

    private static bool TryReadShops(
        CsvDocument document,
        List<string> shopIds,
        Dictionary<string, string> currencyByShopId,
        Dictionary<string, List<ShopStockDefinition>> stockByShopId,
        Dictionary<string, HashSet<string>> itemIdsByShopId,
        Dictionary<string, HashSet<int>> ordersByShopId,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (document == null)
        {
            return true;
        }

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                row, document.SourceName, "shop_id", out string shopId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "currency_item_id", out string currencyItemId, out loadError))
            {
                return false;
            }

            if (!currencyByShopId.TryAdd(shopId, currencyItemId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "shop_id", "중복 상점 ID가 있습니다.");
                return false;
            }

            shopIds.Add(shopId);
            stockByShopId.Add(shopId, new List<ShopStockDefinition>());
            itemIdsByShopId.Add(shopId, new HashSet<string>(StringComparer.Ordinal));
            ordersByShopId.Add(shopId, new HashSet<int>());
        }

        return true;
    }

    private static bool TryReadStock(
        CsvDocument document,
        Dictionary<string, List<ShopStockDefinition>> stockByShopId,
        Dictionary<string, HashSet<string>> itemIdsByShopId,
        Dictionary<string, HashSet<int>> ordersByShopId,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (document == null)
        {
            return true;
        }

        for (int i = 0; i < document.Rows.Count; i++)
        {
            if (!TryReadStockRow(
                document, document.Rows[i], stockByShopId,
                itemIdsByShopId, ordersByShopId, out loadError))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryReadStockRow(
        CsvDocument document,
        CsvRow row,
        Dictionary<string, List<ShopStockDefinition>> stockByShopId,
        Dictionary<string, HashSet<string>> itemIdsByShopId,
        Dictionary<string, HashSet<int>> ordersByShopId,
        out GameDataCsvLoadError loadError)
    {
        if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
            row, document.SourceName, "shop_id", out string shopId, out loadError)
            || !CsvDefinitionReadUtility.TryGetRequiredSystemId(
                row, document.SourceName, "item_id", out string itemId, out loadError)
            || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                row, document.SourceName, "display_order", out int displayOrder, out loadError)
            || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                row, document.SourceName, "initial_stock", out int initialStock, out loadError))
        {
            return false;
        }

        if (!stockByShopId.TryGetValue(shopId, out List<ShopStockDefinition> stock))
        {
            loadError = CsvDefinitionReadUtility.CreateRowError(
                document.SourceName, row, "shop_id", "상점 ID를 찾을 수 없습니다.");
            return false;
        }

        if (!itemIdsByShopId[shopId].Add(itemId))
        {
            loadError = CsvDefinitionReadUtility.CreateRowError(
                document.SourceName, row, "item_id", "상점에 중복 상품이 있습니다.");
            return false;
        }

        if (!ordersByShopId[shopId].Add(displayOrder))
        {
            loadError = CsvDefinitionReadUtility.CreateRowError(
                document.SourceName, row, "display_order", "상점에 중복 표시 순서가 있습니다.");
            return false;
        }

        stock.Add(new ShopStockDefinition(shopId, itemId, displayOrder, initialStock));
        return true;
    }

    private static List<ShopDefinition> BuildDefinitions(
        List<string> shopIds,
        Dictionary<string, string> currencyByShopId,
        Dictionary<string, List<ShopStockDefinition>> stockByShopId)
    {
        List<ShopDefinition> shops = new List<ShopDefinition>(shopIds.Count);

        for (int i = 0; i < shopIds.Count; i++)
        {
            string shopId = shopIds[i];
            List<ShopStockDefinition> stock = stockByShopId[shopId];
            stock.Sort(CompareStock);
            shops.Add(new ShopDefinition(shopId, currencyByShopId[shopId], stock));
        }

        return shops;
    }

    private static int CompareStock(ShopStockDefinition left, ShopStockDefinition right)
    {
        return left.DisplayOrder.CompareTo(right.DisplayOrder);
    }
}
