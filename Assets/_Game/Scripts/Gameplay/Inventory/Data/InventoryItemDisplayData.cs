using UnityEngine;

//==============================================================================
// 기능: class::InventoryItemDisplayData
// 설명: CSV 아이템 정의에 현재 언어 문장과 Unity 아이콘을 결합한 UI 표시 데이터
//==============================================================================
public sealed class InventoryItemDisplayData
{
    public ItemDefinition Definition { get; }
    public string ItemId => Definition != null ? Definition.ItemId : string.Empty;
    public string DisplayName { get; }
    public string Description { get; }
    public Sprite Icon { get; }
    public int Price => Definition != null ? Definition.Price : 0;
    public ItemType ItemType => Definition != null ? Definition.ItemType : ItemType.ITEM_MISC;
    public int MaxStackCount => Definition != null ? Definition.MaxStackCount : 1;

    public InventoryItemDisplayData(
        ItemDefinition definition,
        string displayName,
        string description,
        Sprite icon)
    {
        Definition = definition;
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        Icon = icon;
    }
}
