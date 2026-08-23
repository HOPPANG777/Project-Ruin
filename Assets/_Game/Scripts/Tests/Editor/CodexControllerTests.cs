//=============================================================================
// File:        CodexControllerTests.cs
//
// Description: 도감 영구 발견과 기술 해제 상태 규칙을 검증한다.
//
// History:     08/19/2026 + Created -- Codex
//=============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public sealed class CodexControllerTests
{
    private const string RELIC_ID = "ITEM_TEST_RELIC";
    private const string TECH_ID = "TECH_TEST";
    private GameObject _gameObject;
    private CodexController _controller;
    private FakeInventory _inventory;
    private FakeProgress _progress;
    private FakeRestoration _restoration;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject(nameof(CodexControllerTests));
        _controller = _gameObject.AddComponent<CodexController>();
        _inventory = new FakeInventory();
        _progress = new FakeProgress();
        _restoration = new FakeRestoration();
    }

    [TearDown]
    public void TearDown() => UnityEngine.Object.DestroyImmediate(_gameObject);

    [Test]
    public void ExistingOwnedItemIsDiscoveredDuringInit()
    {
        _inventory.SetCount(RELIC_ID, 1);

        Assert.That(_controller.Init(CreateData(), _inventory, _progress, _restoration), Is.True);

        Assert.That(_controller.IsItemDiscovered(RELIC_ID), Is.True);
        Assert.That(_progress.SetFlagCalls, Is.EqualTo(1));
    }

    [Test]
    public void FirstAcquisitionStaysDiscoveredAfterConsumption()
    {
        _controller.Init(CreateData(), _inventory, _progress, _restoration);
        int discoveryEvents = 0;
        _controller.DiscoveryChanged += _ => discoveryEvents++;

        _inventory.SetCount(RELIC_ID, 1);
        _inventory.SetCount(RELIC_ID, 0);
        _inventory.SetCount(RELIC_ID, 2);

        Assert.That(_controller.IsEntryUnlocked("CODEX_RELIC"), Is.True);
        Assert.That(discoveryEvents, Is.EqualTo(1));
        Assert.That(_progress.SetFlagCalls, Is.EqualTo(1));
    }

    [Test]
    public void UnregisteredItemDoesNotCreateDiscoveryFlag()
    {
        _controller.Init(CreateData(), _inventory, _progress, _restoration);

        _inventory.SetCount("ITEM_NOT_CODEX", 1);

        Assert.That(_progress.SetFlagCalls, Is.Zero);
        Assert.That(_controller.IsItemDiscovered("ITEM_NOT_CODEX"), Is.False);
    }

    [Test]
    public void TechnologyUsesExistingAcquisitionState()
    {
        _controller.Init(CreateData(), _inventory, _progress, _restoration);
        Assert.That(_controller.IsEntryUnlocked("CODEX_TECH"), Is.False);

        _restoration.IsTechnologyAcquired = true;

        Assert.That(_controller.IsEntryUnlocked("CODEX_TECH"), Is.True);
        Assert.That(_controller.IsEntryUnlocked("UNKNOWN"), Is.False);
    }

    private static ICodexDataRepository CreateData()
    {
        return new FakeCodexData(new[]
        {
            new CodexEntryDefinition("CODEX_RELIC", CodexCategory.CODEX_CATEGORY_RELIC, 0, RELIC_ID, 1),
            new CodexEntryDefinition("CODEX_TECH", CodexCategory.CODEX_CATEGORY_TECHNOLOGY, 0, TECH_ID, 1)
        });
    }

    private sealed class FakeCodexData : ICodexDataRepository
    {
        private readonly IReadOnlyList<CodexEntryDefinition> _entries;
        internal FakeCodexData(IReadOnlyList<CodexEntryDefinition> entries) => _entries = entries;
        public int MaxRestore => 100;
        public int TechnologyCount => 0;
        public int CodexEntryCount => _entries.Count;
        public IReadOnlyList<TechnologyDefinition> Technologies => Array.Empty<TechnologyDefinition>();
        public IReadOnlyList<RestorationStageDefinition> RestorationStages => Array.Empty<RestorationStageDefinition>();
        public IReadOnlyList<CodexEntryDefinition> CodexEntries => _entries;
        public bool TryGetTechnology(string id, out TechnologyDefinition value) { value = null; return false; }
        public bool TryGetCodexEntry(string id, out CodexEntryDefinition value)
        {
            for (int i = 0; i < _entries.Count; i++) if (_entries[i].EntryId == id) { value = _entries[i]; return true; }
            value = null; return false;
        }
        public bool TryGetCodexEntryBySource(CodexCategory category, string id, out CodexEntryDefinition value)
        {
            for (int i = 0; i < _entries.Count; i++) if (_entries[i].Category == category && _entries[i].SourceId == id) { value = _entries[i]; return true; }
            value = null; return false;
        }
        public bool TryGetCodexIngredients(string id, out IReadOnlyList<CodexIngredientDefinition> value)
        { value = null; return false; }
    }

    private sealed class FakeInventory : IInventoryService
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        public event Action<string, int> ItemCountChanged;
        internal void SetCount(string id, int count) { _counts[id] = count; ItemCountChanged?.Invoke(id, count); }
        public int GetItemCount(string id) => _counts.TryGetValue(id, out int count) ? count : 0;
        public bool TryAddItem(string id, int amount) { SetCount(id, GetItemCount(id) + amount); return true; }
        public bool CanAddItem(string id, int amount) => amount > 0;
        public bool TryConsumeItem(string id, int amount) { SetCount(id, GetItemCount(id) - amount); return true; }
        public bool TryExchangeItem(string a, int b, string c, int d) => false;
        public bool TryGetItem(string id, out ItemDefinition item) { item = null; return false; }
        public bool TryGetItemDisplay(string id, out InventoryItemDisplayData data) { data = null; return false; }
        public IReadOnlyList<InventorySlotData> GetSlots(InventoryCategory category) => Array.Empty<InventorySlotData>();
        public IReadOnlyList<InventorySlotData> GetSlotsByAcquisitionOrder() => Array.Empty<InventorySlotData>();
        public bool TryMoveSlot(InventoryCategory category, int source, int target) => false;
        public bool TryMarkItemChecked(string id) => false;
        public void Reset() => _counts.Clear();
    }

    private sealed class FakeProgress : IProgressService
    {
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();
        internal int SetFlagCalls { get; private set; }
        public bool IsDirty => false;
        public void SetFlag(string id, bool value) { SetFlagCalls++; _flags[id] = value; }
        public bool GetFlag(string id) => _flags.TryGetValue(id, out bool value) && value;
        public UniTask<bool> LoadAsync(CancellationToken token = default) => UniTask.FromResult(true);
        public UniTask<bool> SaveAsync(CancellationToken token = default) => UniTask.FromResult(true);
        public void SetSystemState(string id, string json) { }
        public bool TryGetSystemState(string id, out string json) { json = null; return false; }
        public void SetObjectState(int stage, string id, string json) { }
        public bool TryGetObjectState(int stage, string id, out string json) { json = null; return false; }
        public void Reset() => _flags.Clear();
    }

    private sealed class FakeRestoration : IRelicRestorationService
    {
        internal bool IsTechnologyAcquired { get; set; }
        public int CurrentRestore => 0;
        public int MaxRestore => 100;
        public int RestoreStage => 0;
        public bool IsKnownTechnology(string id) => id == TECH_ID;
        public bool IsAcquired(string id) => id == TECH_ID && IsTechnologyAcquired;
        public bool IsReturned(string id) => false;
        public int FillReturnableTechnologies(List<TechnologyDefinition> buffer) { buffer?.Clear(); return 0; }
        public bool TryAcquireTechnology(string id, out RelicRestorationFailure failure) { failure = default; return false; }
        public bool TryReturnTechnology(string id, out RelicRestorationResult result, out RelicRestorationFailure failure)
        { result = default; failure = default; return false; }
    }
}

