//=============================================================================
// File:        MuseumBottleDataRepository.cs
// Description: 술병과 허용 술 정의를 교차 검증하고 ID로 조회한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;

/// <summary>챕터별 술병과 선택적 완성 보상 정의입니다.</summary>
public sealed class MuseumBottleDefinition
{
    public MuseumBottleDefinition(string bottleId, int chapter, string displayNameKey,
        string rewardItemId, int? rewardAmount)
    {
        BottleId = bottleId ?? string.Empty;
        Chapter = chapter;
        DisplayNameKey = displayNameKey ?? string.Empty;
        RewardItemId = rewardItemId ?? string.Empty;
        RewardAmount = rewardAmount;
    }

    public string BottleId { get; }
    public int Chapter { get; }
    public string DisplayNameKey { get; }
    public string RewardItemId { get; }
    public int? RewardAmount { get; }
    public bool HasReward => !string.IsNullOrEmpty(RewardItemId) && RewardAmount.HasValue;
}

/// <summary>술병에 한 번 투입할 수 있는 술과 채움 기여도 정의입니다.</summary>
public sealed class MuseumBottleItemDefinition
{
    public MuseumBottleItemDefinition(string bottleId, int itemOrder,
        string alcoholItemId, int contributionValue)
    {
        BottleId = bottleId ?? string.Empty;
        ItemOrder = itemOrder;
        AlcoholItemId = alcoholItemId ?? string.Empty;
        ContributionValue = contributionValue;
    }

    public string BottleId { get; }
    public int ItemOrder { get; }
    public string AlcoholItemId { get; }
    public int ContributionValue { get; }
}

/// <summary>술병 CSV가 참조하는 Item·도감·문장의 최소 조회 계약입니다.</summary>
public interface IMuseumBottleReferenceData
{
    bool TryGetItem(string itemId, out ItemDefinition item);
    bool TryGetAlcoholCodexEntry(string itemId, out CodexEntryDefinition entry);
    bool HasLocalization(string textKey);
}

/// <summary>검증된 Phase 5 술병 정의 저장소입니다.</summary>
public sealed class MuseumBottleDataRepository
{
    private readonly Dictionary<string, MuseumBottleDefinition> _bottleById;
    private readonly Dictionary<string, MuseumBottleItemDefinition[]> _itemsByBottleId;
    private readonly Dictionary<string, MuseumBottleItemDefinition> _itemByAlcoholId;
    private readonly MuseumBottleDefinition[] _bottles;

    private MuseumBottleDataRepository(IReadOnlyList<MuseumBottleDefinition> bottles,
        Dictionary<string, List<MuseumBottleItemDefinition>> itemsByBottle)
    {
        _bottles = Copy(bottles);
        _bottleById = new Dictionary<string, MuseumBottleDefinition>(StringComparer.Ordinal);
        _itemsByBottleId = new Dictionary<string, MuseumBottleItemDefinition[]>(StringComparer.Ordinal);
        _itemByAlcoholId = new Dictionary<string, MuseumBottleItemDefinition>(StringComparer.Ordinal);
        for (int i = 0; i < _bottles.Length; i++)
        {
            MuseumBottleDefinition bottle = _bottles[i];
            _bottleById.Add(bottle.BottleId, bottle);
            List<MuseumBottleItemDefinition> items = itemsByBottle[bottle.BottleId];
            items.Sort((left, right) => left.ItemOrder.CompareTo(right.ItemOrder));
            MuseumBottleItemDefinition[] itemArray = items.ToArray();
            _itemsByBottleId.Add(bottle.BottleId, itemArray);
            for (int j = 0; j < itemArray.Length; j++)
                _itemByAlcoholId.Add(itemArray[j].AlcoholItemId, itemArray[j]);
        }
    }

    public int BottleCount => _bottles.Length;
    public IReadOnlyList<MuseumBottleDefinition> Bottles => _bottles;
    public bool TryGetBottle(string bottleId, out MuseumBottleDefinition bottle) =>
        _bottleById.TryGetValue(bottleId ?? string.Empty, out bottle);
    public bool TryGetBottleItems(string bottleId,
        out IReadOnlyList<MuseumBottleItemDefinition> items)
    {
        bool found = _itemsByBottleId.TryGetValue(
            bottleId ?? string.Empty, out MuseumBottleItemDefinition[] values);
        items = values;
        return found;
    }
    public bool TryGetBottleItem(string alcoholItemId,
        out MuseumBottleItemDefinition item) =>
        _itemByAlcoholId.TryGetValue(alcoholItemId ?? string.Empty, out item);

    /// <summary>두 CSV의 정책과 외부 참조를 검증해 저장소를 생성합니다.</summary>
    public static bool TryCreate(IReadOnlyList<MuseumBottleDefinition> bottles,
        IReadOnlyList<MuseumBottleItemDefinition> items, IMuseumBottleReferenceData references,
        out MuseumBottleDataRepository repository, out GameDataRepositoryBuildError error)
    {
        repository = null;
        error = null;
        if (bottles == null || items == null || references == null)
            return Fail("MuseumBottle", string.Empty, "술병 검증 입력이 없습니다.", out error);
        if (bottles.Count != 0 && bottles.Count != 3)
            return Fail("MuseumBottle", string.Empty, "술병은 미작성 상태이거나 정확히 3개여야 합니다.", out error);

        Dictionary<string, MuseumBottleDefinition> bottleById =
            new Dictionary<string, MuseumBottleDefinition>(StringComparer.Ordinal);
        Dictionary<string, List<MuseumBottleItemDefinition>> itemsByBottle =
            new Dictionary<string, List<MuseumBottleItemDefinition>>(StringComparer.Ordinal);
        HashSet<int> chapters = new HashSet<int>();
        for (int i = 0; i < bottles.Count; i++)
        {
            MuseumBottleDefinition bottle = bottles[i];
            if (bottle == null || !bottleById.TryAdd(bottle.BottleId, bottle))
                return Fail("MuseumBottle", bottle?.BottleId, "술병 ID가 비었거나 중복됩니다.", out error);
            if (bottle.Chapter < 0 || !chapters.Add(bottle.Chapter)
                || !references.HasLocalization(bottle.DisplayNameKey))
                return Fail("MuseumBottle", bottle.BottleId, "챕터 또는 표시 이름 Key가 유효하지 않습니다.", out error);
            bool hasRewardItemId = !string.IsNullOrEmpty(bottle.RewardItemId);
            if (hasRewardItemId != bottle.RewardAmount.HasValue
                || (bottle.HasReward && !references.TryGetItem(bottle.RewardItemId, out _)))
                return Fail("MuseumBottle", bottle.BottleId, "완성 보상 Item ID와 수량 쌍이 유효하지 않습니다.", out error);
            itemsByBottle.Add(bottle.BottleId, new List<MuseumBottleItemDefinition>());
        }

        HashSet<string> alcoholIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, HashSet<int>> ordersByBottle = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        for (int i = 0; i < bottles.Count; i++) ordersByBottle.Add(bottles[i].BottleId, new HashSet<int>());
        for (int i = 0; i < items.Count; i++)
        {
            MuseumBottleItemDefinition item = items[i];
            if (item == null || !bottleById.TryGetValue(item.BottleId, out MuseumBottleDefinition bottle))
                return Fail("MuseumBottleItem", item?.BottleId, "부모 술병을 찾을 수 없습니다.", out error);
            if (item.ItemOrder < 0 || item.ContributionValue <= 0
                || !ordersByBottle[item.BottleId].Add(item.ItemOrder)
                || !alcoholIds.Add(item.AlcoholItemId))
                return Fail("MuseumBottleItem", item.AlcoholItemId, "순서·기여도 또는 술 중복 규칙을 위반했습니다.", out error);
            if (!references.TryGetItem(item.AlcoholItemId, out ItemDefinition alcohol)
                || alcohol.ItemType != ItemType.ITEM_ALCOHOL_COLLECTIBLE
                || !references.TryGetAlcoholCodexEntry(item.AlcoholItemId, out CodexEntryDefinition entry)
                || entry.Chapter != bottle.Chapter)
                return Fail("MuseumBottleItem", item.AlcoholItemId, "술 Item·도감 항목 또는 챕터가 유효하지 않습니다.", out error);
            itemsByBottle[item.BottleId].Add(item);
        }
        for (int i = 0; i < bottles.Count; i++)
            if (itemsByBottle[bottles[i].BottleId].Count == 0)
                return Fail("MuseumBottle", bottles[i].BottleId, "인정 술이 한 개 이상 필요합니다.", out error);

        repository = new MuseumBottleDataRepository(bottles, itemsByBottle);
        return true;
    }

    private static T[] Copy<T>(IReadOnlyList<T> source)
    {
        T[] result = new T[source.Count];
        for (int i = 0; i < source.Count; i++) result[i] = source[i];
        return result;
    }

    private static bool Fail(string type, string id, string message,
        out GameDataRepositoryBuildError error)
    {
        error = new GameDataRepositoryBuildError(type, id ?? string.Empty, message);
        return false;
    }
}
