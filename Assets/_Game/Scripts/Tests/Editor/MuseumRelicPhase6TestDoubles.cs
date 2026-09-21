//=============================================================================
// File:        MuseumRelicPhase6TestDoubles.cs
// Description: Phase 6 유물 전시 테스트용 게임 데이터 대역
// History:     08/30/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;

internal sealed class MuseumRelicTestData : IGameDataRepository, ICodexDataRepository,
    IMuseumRelicReferenceData
{
    private readonly Dictionary<string, ItemDefinition> _items =
        new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, CodexEntryDefinition> _entries =
        new Dictionary<string, CodexEntryDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, CodexEntryDefinition> _entriesBySource =
        new Dictionary<string, CodexEntryDefinition>(StringComparer.Ordinal);
    private readonly List<CodexEntryDefinition> _entryList = new List<CodexEntryDefinition>();

    internal void AddRelic(string id, int order)
    {
        AddItem(id, ItemType.ITEM_RELIC_COLLECTIBLE);
        CodexEntryDefinition entry = new CodexEntryDefinition(
            "CODEX_" + id, CodexCategory.CODEX_CATEGORY_RELIC, order, id, 0);
        _entries.Add(entry.EntryId, entry);
        _entriesBySource.Add(SourceKey(entry.Category, id), entry);
        _entryList.Add(entry);
    }

    internal void AddItem(string id, ItemType type = ItemType.ITEM_MISC) =>
        _items[id] = new ItemDefinition(id, "item.name", "item.desc", string.Empty,
            0, false, type, int.MaxValue);

    public int RelicTypeCount => _entryList.Count;
    public int DialogueActorCount => 0;
    public int DialogueGraphCount => 0;
    public int ItemCount => _items.Count;
    public int QuestCount => 0;
    public int LocalizationEntryCount => 0;
    public int PuzzleCount => 0;
    public int ShopCount => 0;
    public ICodexDataRepository CodexData => this;
    public int MaxRestore => 0;
    public int TechnologyCount => 0;
    public int CodexEntryCount => _entryList.Count;
    public IReadOnlyList<TechnologyDefinition> Technologies => Array.Empty<TechnologyDefinition>();
    public IReadOnlyList<RestorationStageDefinition> RestorationStages =>
        Array.Empty<RestorationStageDefinition>();
    public IReadOnlyList<CodexEntryDefinition> CodexEntries => _entryList;

    public bool TryGetItem(string id, out ItemDefinition value) => _items.TryGetValue(id, out value);
    public bool TryGetCodexEntry(string id, out CodexEntryDefinition value) =>
        _entries.TryGetValue(id, out value);
    public bool TryGetCodexEntryBySource(CodexCategory category, string id,
        out CodexEntryDefinition value) => _entriesBySource.TryGetValue(SourceKey(category, id), out value);
    public bool TryGetTechnology(string id, out TechnologyDefinition value)
    { value = null; return false; }
    public bool TryGetCodexIngredients(string id,
        out IReadOnlyList<CodexIngredientDefinition> values)
    { values = null; return false; }
    public bool TryGetDialogueActor(string id, out DialogueActorDefinition value)
    { value = null; return false; }
    public bool TryGetDialoguePortrait(string actorId, string portraitId,
        out DialoguePortraitDefinition value) { value = null; return false; }
    public bool TryGetDialogueGraph(string id, out DialogueGraphDefinition value)
    { value = null; return false; }
    public bool TryGetDialogueNode(string graphId, string nodeId,
        out DialogueNodeDefinition value) { value = null; return false; }
    public bool TryGetQuest(string id, out QuestDefinition value)
    { value = null; return false; }
    public bool TryGetPuzzle(string id, out PuzzleDefinition value)
    { value = null; return false; }
    public bool TryGetShop(string id, out ShopDefinition value)
    { value = null; return false; }
    public bool TryGetLocalizationEntry(string id, out LocalizationEntryDefinition value)
    { value = null; return false; }
    public bool TryGetNpcDialogueBinding(string id, out NpcDialogueBindingDefinition value)
    { value = null; return false; }
    public bool TryGetNpcQuestDialogueRoutes(string id,
        out IReadOnlyList<NpcQuestDialogueRouteDefinition> values)
    { values = null; return false; }

    private static string SourceKey(CodexCategory category, string id) =>
        string.Concat(((int)category).ToString(), ":", id);
}

internal sealed class MuseumRelicTestDisplayService : IMuseumRelicDisplayService
{
    public event Action<string> ExhibitChanged { add { } remove { } }
    public event Action<string> MilestoneChanged { add { } remove { } }
    public int DisplayedRelicCount => 0;
    public int RelicTypeCount => 20;
    public void RegisterPedestal(string pedestalId) { }
    public bool TryGetDisplayedRelic(string pedestalId, out string relicItemId)
    { relicItemId = null; return false; }
    public int FillPlaceableRelics(List<string> buffer)
    { buffer.Clear(); return 0; }
    public bool TryPlace(string pedestalId, string relicItemId,
        out MuseumRelicDisplayFailure failure)
    { failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVALID_ITEM; return false; }
    public bool TryRecover(string pedestalId, out MuseumRelicDisplayFailure failure)
    { failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_PEDESTAL_EMPTY; return false; }
    public bool TryGetMilestone(string milestoneId,
        out MuseumRelicMilestoneDefinition milestone,
        out MuseumRelicMilestoneStatus status)
    { milestone = null; status = default; return false; }
    public bool TryClaimRoomReward(string milestoneId,
        out MuseumRelicDisplayFailure failure)
    { failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_UNKNOWN_MILESTONE; return false; }
    public void RetryPendingImmediateRewards() { }
}
