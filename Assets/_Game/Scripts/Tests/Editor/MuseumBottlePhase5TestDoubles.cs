//=============================================================================
// File:        MuseumBottlePhase5TestDoubles.cs
// Description: Phase 5 술병 테스트용 인벤토리·Progress 대역이다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

internal sealed class MuseumBottleTestInventory : IInventoryService, IInventoryStateTransactionService
{
    private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
    internal bool BlockReward { get; set; }
    internal int ConsumeCalls { get; private set; }
    internal int ExchangeCalls { get; private set; }
    internal Action OnBeforeConsume { get; set; }
    internal Action OnBeforeAdd { get; set; }
    internal MuseumBottleTestProgress TransactionProgress { get; set; }
    public event Action<string, int> ItemCountChanged;

    internal void SetCount(string id, int count) => _counts[id] = count;
    public bool TryApplyWithSystemState(string removeId, int removeAmount, string addId, int addAmount,
        string stateId, string stateJson, out InventoryStateChangeFailure failure)
    {
        failure = InventoryStateChangeFailure.INVENTORY_STATE_CHANGE_INVALID;
        if (removeAmount > 0) OnBeforeConsume?.Invoke();
        if (addAmount > 0) OnBeforeAdd?.Invoke();
        if ((removeAmount > 0 && GetItemCount(removeId) < removeAmount)
            || (addAmount > 0 && !CanAddItem(addId, addAmount))) return false;
        failure = InventoryStateChangeFailure.INVENTORY_STATE_CHANGE_APPLY_FAILED;
        if (TransactionProgress == null || !TransactionProgress.TrySetSystemStates(
                "inventory.test", "{}", stateId, stateJson)) return false;
        if (removeAmount > 0) SetCount(removeId, GetItemCount(removeId) - removeAmount);
        if (addAmount > 0) SetCount(addId, GetItemCount(addId) + addAmount);
        failure = InventoryStateChangeFailure.INVENTORY_STATE_CHANGE_NONE;
        if (removeAmount > 0) ItemCountChanged?.Invoke(removeId, GetItemCount(removeId));
        if (addAmount > 0) ItemCountChanged?.Invoke(addId, GetItemCount(addId));
        return true;
    }
    public int GetItemCount(string id) => _counts.TryGetValue(id, out int count) ? count : 0;
    public bool CanAddItem(string id, int amount) => !BlockReward && amount > 0
        && GetItemCount(id) <= int.MaxValue - amount;
    public bool TryAddItem(string id, int amount)
    {
        OnBeforeAdd?.Invoke();
        if (!CanAddItem(id, amount)) return false;
        SetCount(id, GetItemCount(id) + amount);
        ItemCountChanged?.Invoke(id, GetItemCount(id));
        return true;
    }
    public bool TryConsumeItem(string id, int amount)
    {
        ConsumeCalls++;
        OnBeforeConsume?.Invoke();
        if (amount <= 0 || GetItemCount(id) < amount) return false;
        SetCount(id, GetItemCount(id) - amount);
        ItemCountChanged?.Invoke(id, GetItemCount(id));
        return true;
    }
    public bool TryExchangeItem(string removeId, int removeAmount, string addId, int addAmount)
    {
        ExchangeCalls++;
        if (GetItemCount(removeId) < removeAmount || !CanAddItem(addId, addAmount)) return false;
        SetCount(removeId, GetItemCount(removeId) - removeAmount);
        SetCount(addId, GetItemCount(addId) + addAmount);
        ItemCountChanged?.Invoke(removeId, GetItemCount(removeId));
        ItemCountChanged?.Invoke(addId, GetItemCount(addId));
        return true;
    }
    public bool TryGetItem(string id, out ItemDefinition item) { item = null; return false; }
    public bool TryGetItemDisplay(string id, out InventoryItemDisplayData display)
    { display = null; return false; }
    public IReadOnlyList<InventorySlotData> GetSlots(InventoryCategory category) =>
        Array.Empty<InventorySlotData>();
    public IReadOnlyList<InventorySlotData> GetSlotsByAcquisitionOrder() =>
        Array.Empty<InventorySlotData>();
    public bool TryMoveSlot(InventoryCategory category, int sourceIndex, int targetIndex) => false;
    public bool TryMarkItemChecked(string id) => false;
    public void Reset() => _counts.Clear();
}

internal sealed class MuseumBottleTestProgress : IProgressService, IProgressStateTransactionService
{
    private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();
    internal bool ThrowOnFlagWrite { get; set; }
    private readonly Dictionary<string, string> _states = new Dictionary<string, string>();
    internal int StateWrites { get; private set; }
    internal int SaveCalls { get; private set; }
    internal bool ThrowOnStateWrite { get; set; }
    internal bool SaveSucceeds { get; set; } = true;
    private bool _isDirty;
    public bool IsDirty => _isDirty;
    public UniTask<bool> LoadAsync(CancellationToken token = default) => UniTask.FromResult(true);
    public UniTask<bool> SaveAsync(CancellationToken token = default)
    {
        SaveCalls++;
        if (SaveSucceeds) _isDirty = false;
        return UniTask.FromResult(SaveSucceeds);
    }
    public void SetFlag(string id, bool value)
    {
        if (ThrowOnFlagWrite) throw new InvalidOperationException("flag write failed");
        _flags[id] = value;
        _isDirty = true;
    }
    public bool TrySetSystemStates(string firstId, string firstJson, string secondId, string secondJson)
    {
        if (ThrowOnStateWrite) return false;
        SetSystemState(firstId, firstJson);
        SetSystemState(secondId, secondJson);
        return true;
    }
    public bool GetFlag(string id) => _flags.TryGetValue(id, out bool value) && value;
    public void SetSystemState(string id, string json)
    {
        if (ThrowOnStateWrite) throw new InvalidOperationException("state write failed");
        StateWrites++;
        _isDirty = true;
        _states[id] = json;
    }
    public bool TryGetSystemState(string id, out string json) => _states.TryGetValue(id, out json);
    public void SetObjectState(int stage, string id, string json) { }
    public bool TryGetObjectState(int stage, string id, out string json)
    { json = null; return false; }
    public void Reset()
    {
        _states.Clear();
        _flags.Clear();
        ThrowOnFlagWrite = false;
        StateWrites = 0;
        SaveCalls = 0;
        ThrowOnStateWrite = false;
        SaveSucceeds = true;
        _isDirty = false;
    }
}

internal sealed class MuseumBottleTestReferences : IMuseumBottleReferenceData
{
    private readonly Dictionary<string, ItemDefinition> _items =
        new Dictionary<string, ItemDefinition>();
    private readonly Dictionary<string, CodexEntryDefinition> _entries =
        new Dictionary<string, CodexEntryDefinition>();
    private readonly HashSet<string> _keys = new HashSet<string>();

    internal void Add(string id, int chapter, ItemType type = ItemType.ITEM_ALCOHOL_COLLECTIBLE)
    {
        _items[id] = new ItemDefinition(id, "item.name", "item.desc", string.Empty,
            0, false, type, 99);
        _entries[id] = new CodexEntryDefinition(
            "CODEX_" + id, CodexCategory.CODEX_CATEGORY_ALCOHOL, 0, id, chapter);
    }
    internal void AddReward(string id) => _items[id] = new ItemDefinition(
        id, "reward.name", "reward.desc", string.Empty, 0, false, ItemType.ITEM_MISC, 99);
    internal void AddKey(string key) => _keys.Add(key);
    public bool TryGetItem(string id, out ItemDefinition item) => _items.TryGetValue(id, out item);
    public bool TryGetAlcoholCodexEntry(string id, out CodexEntryDefinition entry) =>
        _entries.TryGetValue(id, out entry);
    public bool HasLocalization(string key) => _keys.Contains(key);
}
