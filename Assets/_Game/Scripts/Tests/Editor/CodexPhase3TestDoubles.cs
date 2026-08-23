using System;
using System.Collections.Generic;

internal sealed class Phase3CodexServiceStub : ICodexService
{
    public event Action<string> DiscoveryChanged { add { } remove { } }
    public bool IsEntryUnlocked(string entryId) => false;
    public bool IsItemDiscovered(string itemId) => false;
}

internal sealed class Phase3InventoryServiceStub : IInventoryService
{
    private static readonly IReadOnlyList<InventorySlotData> Empty =
        Array.Empty<InventorySlotData>();

    public event Action<string, int> ItemCountChanged { add { } remove { } }
    public bool TryAddItem(string itemId, int amount) => false;
    public bool CanAddItem(string itemId, int amount) => false;
    public bool TryConsumeItem(string itemId, int amount) => false;
    public bool TryExchangeItem(string removeItemId, int removeAmount,
        string addItemId, int addAmount) => false;
    public bool TryGetItem(string itemId, out ItemDefinition itemData)
    {
        itemData = null;
        return false;
    }
    public bool TryGetItemDisplay(string itemId, out InventoryItemDisplayData displayData)
    {
        displayData = null;
        return false;
    }
    public int GetItemCount(string itemId) => 0;
    public IReadOnlyList<InventorySlotData> GetSlots(InventoryCategory category) => Empty;
    public IReadOnlyList<InventorySlotData> GetSlotsByAcquisitionOrder() => Empty;
    public bool TryMoveSlot(InventoryCategory category, int sourceIndex, int targetIndex) => false;
    public bool TryMarkItemChecked(string itemId) => false;
    public void Reset() { }
}
