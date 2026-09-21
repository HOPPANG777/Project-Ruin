//=============================================================================
// File:        MuseumRelicMilestoneData.cs
// Description: 유물 전시 마일스톤의 CSV 정의·검증·조회 구조를 제공한다.
// History:     08/30/2026 + Created -- Codex
//              08/30/2026 + enum 접두어와 공개 API 문서화 보완 -- Codex
//=============================================================================
//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;

/// <summary>유물 전시 마일스톤의 보상 수령 방식입니다.</summary>
public enum MuseumRelicRewardMode
{
    MUSEUM_RELIC_REWARD_GRANT_IMMEDIATELY = 0,
    MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP
}

/// <summary>서로 다른 실제 전시 유물 수에 따른 한 번성 보상 정의입니다.</summary>
public sealed class MuseumRelicMilestoneDefinition
{
    //=============================================================================
    // MuseumRelicMilestoneDefinition
    // Description: 검증 전 유물 전시 마일스톤 정의를 생성합니다.
    // Parameters: string id, int requiredCount, MuseumRelicRewardMode mode, string rewardItemId, int rewardAmount
    // Return: N/A.
    //=============================================================================
    /// <summary>검증 전 유물 전시 마일스톤 정의를 생성합니다.</summary>
    public MuseumRelicMilestoneDefinition(string id, int requiredCount,
        MuseumRelicRewardMode mode, string rewardItemId, int rewardAmount)
    {
        MilestoneId = id ?? string.Empty;
        RequiredExhibitCount = requiredCount;
        RewardMode = mode;
        RewardItemId = rewardItemId ?? string.Empty;
        RewardAmount = rewardAmount;
    }
    /// <summary>저장과 씬 연결에 사용하는 마일스톤 ID입니다.</summary>
    public string MilestoneId { get; }
    /// <summary>보상 조건에 필요한 서로 다른 전시 유물 수입니다.</summary>
    public int RequiredExhibitCount { get; }
    /// <summary>조건 충족 후 보상을 수령하는 방식입니다.</summary>
    public MuseumRelicRewardMode RewardMode { get; }
    /// <summary>지급할 보상 Item ID입니다.</summary>
    public string RewardItemId { get; }
    /// <summary>지급할 보상 수량입니다.</summary>
    public int RewardAmount { get; }
}

/// <summary>마일스톤이 참조하는 유물 수와 아이템의 검증 계약입니다.</summary>
public interface IMuseumRelicReferenceData
{
    /// <summary>도감에 정의된 수집형 유물 종류 수입니다.</summary>
    int RelicTypeCount { get; }
    /// <summary>Item ID로 아이템 정의를 조회합니다.</summary>
    bool TryGetItem(string itemId, out ItemDefinition item);
}

/// <summary>기존 게임 데이터를 마일스톤 참조 검증과 배치 후보 조회에 연결합니다.</summary>
internal sealed class MuseumRelicGameDataReferences : IMuseumRelicReferenceData
{
    //=============================================================================
    // FillPlaceable
    // Description: 아직 전시되지 않은 보유 유물 목록을 수집합니다.
    // Parameters: IGameDataRepository data, IInventoryService inventory, HashSet<string> displayed, List<string> buffer
    // Return: int
    //=============================================================================
    internal static int FillPlaceable(IGameDataRepository data, IInventoryService inventory,
        HashSet<string> displayed, List<string> buffer)
    {
        IReadOnlyList<CodexEntryDefinition> entries = data.CodexData.CodexEntries;
        for (int i = 0; i < entries.Count; i++)
        {
            CodexEntryDefinition entry = entries[i];
            if (entry.Category == CodexCategory.CODEX_CATEGORY_RELIC
                && data.TryGetItem(entry.SourceId, out ItemDefinition item)
                && item.ItemType == ItemType.ITEM_RELIC_COLLECTIBLE
                && !displayed.Contains(entry.SourceId)
                && inventory.GetItemCount(entry.SourceId) > 0) buffer.Add(entry.SourceId);
        }
        return buffer.Count;
    }

    // 게임 데이터 조회를 위임할 원본 저장소입니다.
    private readonly IGameDataRepository _data;
    internal MuseumRelicGameDataReferences(IGameDataRepository data) => _data = data;
    /// <summary>게임 데이터에 정의된 유물 종류 수입니다.</summary>
    public int RelicTypeCount => CountRelics(_data.CodexData.CodexEntries);
    /// <summary>검증에 사용할 아이템 원본을 조회합니다.</summary>
    public bool TryGetItem(string id, out ItemDefinition item) => _data.TryGetItem(id, out item);

    internal static int CountRelics(IReadOnlyList<CodexEntryDefinition> entries)
    {
        int count = 0;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Category == CodexCategory.CODEX_CATEGORY_RELIC) count++;
        return count;
    }
}

/// <summary>검증된 유물 전시 마일스톤을 ID와 요구 개수 순서로 제공합니다.</summary>
public sealed class MuseumRelicMilestoneRepository
{
    // 요구 개수 순으로 검증된 마일스톤 정의 배열입니다.
    private readonly MuseumRelicMilestoneDefinition[] _values;
    // 안정적인 마일스톤 ID를 위한 조회 인덱스입니다.
    private readonly Dictionary<string, MuseumRelicMilestoneDefinition> _byId;

    private MuseumRelicMilestoneRepository(IReadOnlyList<MuseumRelicMilestoneDefinition> values)
    {
        _values = new MuseumRelicMilestoneDefinition[values.Count];
        _byId = new Dictionary<string, MuseumRelicMilestoneDefinition>(StringComparer.Ordinal);
        for (int i = 0; i < values.Count; i++)
        {
            _values[i] = values[i];
            _byId.Add(values[i].MilestoneId, values[i]);
        }
    }

    /// <summary>요구 전시 수 오름차순으로 검증된 마일스톤 목록입니다.</summary>
    public IReadOnlyList<MuseumRelicMilestoneDefinition> Milestones => _values;
    //=============================================================================
    // TryGet
    // Description: 안정적인 ID로 마일스톤 정의를 조회합니다.
    // Parameters: string id, out MuseumRelicMilestoneDefinition value
    // Return: bool
    //=============================================================================
    /// <summary>안정적인 ID로 마일스톤 정의를 조회합니다.</summary>
    public bool TryGet(string id, out MuseumRelicMilestoneDefinition value) =>
        _byId.TryGetValue(id ?? string.Empty, out value);

    //=============================================================================
    // TryCreate
    // Description: 마일스톤의 ID·순서·참조와 10종/전체 전시 정책을 검증합니다.
    // Parameters: IReadOnlyList<MuseumRelicMilestoneDefinition> values, IMuseumRelicReferenceData references, out MuseumRelicMilestoneRepository repository, out GameDataRepositoryBuildError error
    // Return: bool
    //=============================================================================
    /// <summary>마일스톤의 ID·순서·참조와 10종/전체 전시 정책을 검증합니다.</summary>
    public static bool TryCreate(IReadOnlyList<MuseumRelicMilestoneDefinition> values,
        IMuseumRelicReferenceData references,
        out MuseumRelicMilestoneRepository repository,
        out GameDataRepositoryBuildError error)
    {
        repository = null;
        error = null;
        if (values == null || references == null)
            return Fail(string.Empty, "마일스톤 검증 입력이 없습니다.", out error);
        if (values.Count != 0 && values.Count != 2)
            return Fail(string.Empty, "마일스톤은 미작성 상태이거나 10종/전체 보상 2개여야 합니다.", out error);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<int> counts = new HashSet<int>();
        int previous = 0;
        for (int i = 0; i < values.Count; i++)
        {
            MuseumRelicMilestoneDefinition value = values[i];
            if (value == null || string.IsNullOrWhiteSpace(value.MilestoneId)
                || !ids.Add(value.MilestoneId))
                return Fail(value?.MilestoneId, "마일스톤 ID가 비었거나 중복됩니다.", out error);
            if (value.RequiredExhibitCount <= previous || !counts.Add(value.RequiredExhibitCount)
                || value.RequiredExhibitCount > references.RelicTypeCount)
                return Fail(value.MilestoneId, "요구 개수가 고유 오름차순이 아니거나 유물 총수를 넘습니다.", out error);
            if (!Enum.IsDefined(typeof(MuseumRelicRewardMode), value.RewardMode)
                || string.IsNullOrWhiteSpace(value.RewardItemId)
                || value.RewardAmount <= 0 || !references.TryGetItem(value.RewardItemId, out _))
                return Fail(value.MilestoneId, "보상 방식·Item 또는 수량이 유효하지 않습니다.", out error);
            previous = value.RequiredExhibitCount;
        }
        if (values.Count == 2 && (values[0].RequiredExhibitCount != 10
            || values[0].RewardMode
                != MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_GRANT_IMMEDIATELY
            || values[1].RequiredExhibitCount != references.RelicTypeCount
            || values[1].RewardMode
                != MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP))
            return Fail(string.Empty, "첫 보상은 10종 즉시 지급, 두 번째는 전체 전시 후 방 수령이어야 합니다.", out error);
        repository = new MuseumRelicMilestoneRepository(values);
        return true;
    }

    private static bool Fail(string id, string message,
        out GameDataRepositoryBuildError error)
    {
        error = new GameDataRepositoryBuildError("MuseumRelicMilestone", id ?? string.Empty, message);
        return false;
    }
}

/// <summary>museum_relic_milestones.csv를 불변 정의 목록으로 변환합니다.</summary>
public static class MuseumRelicMilestoneCsvLoader
{
    private static readonly string[] HEADERS =
    {
        "milestone_id", "required_exhibit_count", "reward_mode",
        "reward_item_id", "reward_amount"
    };

    //=============================================================================
    // TryLoadRepository
    // Description: CSV 파싱과 참조 검증만 수행하며 런타임 상태는 변경하지 않습니다.
    // Parameters: GameDataCsvFile csv, IGameDataRepository gameData, out MuseumRelicMilestoneRepository repository, out GameDataCsvLoadError error
    // Return: bool
    //=============================================================================
    /// <summary>CSV 파싱과 참조 검증만 수행하며 런타임 상태는 변경하지 않습니다.</summary>
    public static bool TryLoadRepository(GameDataCsvFile csv, IGameDataRepository gameData,
        out MuseumRelicMilestoneRepository repository, out GameDataCsvLoadError error)
    {
        repository = null;
        if (!TryLoad(csv, out var values, out error)) return false;
        if (gameData?.CodexData == null)
        {
            error = new GameDataCsvLoadError("(museum relic)", 0, 0,
                string.Empty, "게임 데이터 참조가 없습니다.");
            return false;
        }
        if (MuseumRelicMilestoneRepository.TryCreate(values,
                new MuseumRelicGameDataReferences(gameData), out repository,
                out GameDataRepositoryBuildError buildError)) return true;
        error = new GameDataCsvLoadError("(museum relic)", 0, 0, string.Empty,
            $"{buildError.Message} id={buildError.DataId}");
        return false;
    }

    //=============================================================================
    // TryLoad
    // Description: CSV 문서를 읽어 유물 전시 마일스톤 정의 목록으로 변환합니다.
    // Parameters: GameDataCsvFile csv, out List<MuseumRelicMilestoneDefinition> values, out GameDataCsvLoadError error
    // Return: bool
    //=============================================================================
    /// <summary>CSV 문서를 읽어 유물 전시 마일스톤 정의 목록으로 변환합니다.</summary>
    public static bool TryLoad(GameDataCsvFile csv,
        out List<MuseumRelicMilestoneDefinition> values, out GameDataCsvLoadError error)
    {
        values = null;
        if (!CsvDefinitionReadUtility.TryParseOptionalDocument(
                csv, HEADERS, out CsvDocument document, out error)) return false;
        values = new List<MuseumRelicMilestoneDefinition>();
        if (document == null) return true;
        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName,
                    "milestone_id", out string id, out error)
                || !CsvDefinitionReadUtility.TryGetPositiveInt(row, document.SourceName,
                    "required_exhibit_count", out int count, out error)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(row, document.SourceName,
                    "reward_mode", out string modeText, out error)
                || !TryParseRewardMode(modeText, out MuseumRelicRewardMode mode)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(row, document.SourceName,
                    "reward_item_id", out string itemId, out error)
                || !CsvDefinitionReadUtility.TryGetPositiveInt(row, document.SourceName,
                    "reward_amount", out int amount, out error))
            {
                if (error == null) error = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "reward_mode", "허용되지 않은 보상 방식입니다.");
                return false;
            }
            values.Add(new MuseumRelicMilestoneDefinition(id, count, mode, itemId, amount));
        }
        return true;
    }

    private static bool TryParseRewardMode(string value, out MuseumRelicRewardMode mode)
    {
        if (value == "GRANT_IMMEDIATELY")
        {
            mode = MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_GRANT_IMMEDIATELY;
            return true;
        }
        if (value == "ENABLE_ROOM_PICKUP")
        {
            mode = MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP;
            return true;
        }
        mode = default;
        return false;
    }
}
