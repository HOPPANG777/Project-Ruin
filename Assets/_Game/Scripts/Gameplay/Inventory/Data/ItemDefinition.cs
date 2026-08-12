//==============================================================================
// 기능: class::ItemDefinition
// 설명: items.csv에서 읽은 인벤토리 아이템 원본 정의 데이터
//==============================================================================
public sealed class ItemDefinition
{
    public string ItemId { get; }
    public string DisplayNameKey { get; }
    public string DescriptionTextKey { get; }
    public string IconId { get; }
    public int Price { get; }
    public bool CanSell { get; }
    public ItemType ItemType { get; }
    public int MaxStackCount { get; }

    public ItemDefinition(
        string itemId,
        string displayNameKey,
        string descriptionTextKey,
        string iconId,
        int price,
        bool canSell,
        ItemType itemType,
        int maxStackCount)
    {
        ItemId = itemId ?? string.Empty;
        DisplayNameKey = displayNameKey ?? string.Empty;
        DescriptionTextKey = descriptionTextKey ?? string.Empty;
        IconId = iconId ?? string.Empty;
        Price = price;
        CanSell = canSell;
        ItemType = itemType;
        MaxStackCount = maxStackCount;
    }
}
