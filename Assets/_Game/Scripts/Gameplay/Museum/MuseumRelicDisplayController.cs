//=============================================================================
// File:        MuseumRelicDisplayController.cs
// Description: 전시대별 유물 배치와 박물관 마일스톤 보상을 관리한다.
// History:     08/30/2026 + Created -- Codex
//              08/31/2026 + 원자적 상태 적용·재진입 차단·직접 등록 보완 -- Codex
//=============================================================================
//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>전시대별 유물과 박물관 마일스톤 보상을 관리합니다.</summary>
[DisallowMultipleComponent]
public sealed class MuseumRelicDisplayController : MonoBehaviour, IMuseumRelicDisplayService
{
    /// <summary>전시대별 유물과 수령 상태를 저장하는 버전 고정 키입니다.</summary>
    public const string STATE_ID = "museum.relic_displays.v1";
    [Tooltip("유물 전시 개수별 보상을 정의하는 museum_relic_milestones.csv입니다.")]
    [SerializeField] private TextAsset _museumRelicMilestonesCsv;
    // 전시대 ID별 현재 전시 유물 ID입니다.
    private readonly Dictionary<string, string> _exhibits = new Dictionary<string, string>(StringComparer.Ordinal);
    // 재접속 후에도 중복 지급을 막는 수령 완료 ID입니다.
    private readonly HashSet<string> _claimed = new HashSet<string>(StringComparer.Ordinal);
    // 씬 컴포넌트가 직접 등록한 전시대 ID입니다.
    private readonly HashSet<string> _knownPedestals = new HashSet<string>(StringComparer.Ordinal);
    // 동일 유물의 중복 전시를 빠르게 검사하는 캐시입니다.
    private readonly HashSet<string> _displayedIds = new HashSet<string>(StringComparer.Ordinal);
    // 검증에 성공했을 때만 활성화되는 보상 정의 저장소입니다.
    private MuseumRelicMilestoneRepository _milestones;
    // 유물 원본과 도감 관계를 조회하는 주입 데이터입니다.
    private IGameDataRepository _gameData;
    // 보유 수량 조회와 원자적 아이템 변경을 수행하는 서비스입니다.
    private IInventoryService _inventory;
    // 메모리 진행 상태와 ES3 저장을 담당하는 서비스입니다.
    private IProgressService _progress;
    // 트랜잭션 및 알림 도중 재진입을 차단하는 잠금입니다.
    private bool _isChanging;

    /// <summary>전시 상태 적용 후 변경된 전시대 ID를 알립니다.</summary>
    public event Action<string> ExhibitChanged;
    /// <summary>보상과 수령 상태 적용 후 마일스톤 ID를 알립니다.</summary>
    public event Action<string> MilestoneChanged;
    /// <summary>현재 실제 전시 중인 서로 다른 유물 수입니다.</summary>
    public int DisplayedRelicCount => _exhibits.Count;
    /// <summary>현재 도감에 정의된 전체 유물 종류 수입니다.</summary>
    public int RelicTypeCount { get; private set; }

    //=============================================================================
    // TryInit
    // Description: CSV와 GameRoot 서비스로 유물 전시 시스템을 초기화합니다.
    // Parameters: IGameDataRepository gameData, IInventoryService inventory, IProgressService progress, out GameDataCsvLoadError error
    // Return: bool
    //=============================================================================
    /// <summary>CSV와 GameRoot 서비스로 유물 전시 시스템을 초기화합니다.</summary>
    public bool TryInit(IGameDataRepository gameData, IInventoryService inventory,
        IProgressService progress, out GameDataCsvLoadError error)
    {
        error = null;
        _milestones = null;
        if (gameData?.CodexData == null || inventory == null || progress == null)
            return FailLoad("유물 전시 초기화 의존성이 없습니다.", out error);
        GameDataCsvFile csv = _museumRelicMilestonesCsv != null
            ? new GameDataCsvFile(_museumRelicMilestonesCsv.name, _museumRelicMilestonesCsv.text) : null;
        if (!MuseumRelicMilestoneCsvLoader.TryLoadRepository(
                csv, gameData, out var repository, out error)) return false;
        if (Init(repository, gameData, inventory, progress)) return true;
        return FailLoad("유물 전시 상태를 Progress에 적용하지 못했습니다.", out error);
    }

    //=============================================================================
    // Init
    // Description: 검증된 데이터와 테스트 가능한 서비스로 유물 전시 시스템을 초기화합니다.
    // Parameters: MuseumRelicMilestoneRepository milestones, IGameDataRepository gameData, IInventoryService inventory, IProgressService progress
    // Return: bool
    //=============================================================================
    /// <summary>검증된 데이터와 테스트 가능한 서비스로 유물 전시 시스템을 초기화합니다.</summary>
    public bool Init(MuseumRelicMilestoneRepository milestones,
        IGameDataRepository gameData, IInventoryService inventory, IProgressService progress)
    {
        _milestones = null;
        _gameData = gameData;
        _inventory = inventory;
        _progress = progress;
        _isChanging = false;
        if (milestones == null || _gameData?.CodexData == null
            || !(_inventory is IInventoryStateTransactionService) || _progress == null) return false;
        _milestones = milestones;
        RelicTypeCount = MuseumRelicGameDataReferences.CountRelics(_gameData.CodexData.CodexEntries);
        MuseumRelicDisplayStateStore.Load(_progress, _exhibits, _claimed);
        MuseumRelicDisplayStateStore.Sanitize(
            _exhibits, _claimed, _displayedIds, _milestones, IsCollectibleRelic);
        if (!MuseumRelicDisplayStateStore.TryWrite(_progress, _exhibits, _claimed))
        {
            _milestones = null;
            return false;
        }
        RetryPendingImmediateRewards();
        foreach (string id in _knownPedestals)
            MuseumRelicDisplayStateStore.NotifyCommitted(ExhibitChanged, id, this);
        return true;
    }

    //=============================================================================
    // RegisterPedestal
    // Description: 씬 전시대의 안정적인 ID를 등록합니다.
    // Parameters: string pedestalId
    // Return: void
    //=============================================================================
    /// <summary>씬 전시대의 안정적인 ID를 등록합니다.</summary>
    public void RegisterPedestal(string pedestalId)
    {
        if (!string.IsNullOrWhiteSpace(pedestalId)) _knownPedestals.Add(pedestalId);
    }

    //=============================================================================
    // TryGetDisplayedRelic
    // Description: 전시대에 배치된 유물 Item ID를 조회합니다.
    // Parameters: string pedestalId, out string relicItemId
    // Return: bool
    //=============================================================================
    /// <summary>전시대에 배치된 유물 Item ID를 조회합니다.</summary>
    public bool TryGetDisplayedRelic(string pedestalId, out string relicItemId) =>
        _exhibits.TryGetValue(pedestalId ?? string.Empty, out relicItemId);

    //=============================================================================
    // FillPlaceableRelics
    // Description: 현재 배치 가능한 보유 유물 ID를 호출자 버퍼에 채웁니다.
    // Parameters: List<string> buffer
    // Return: int
    //=============================================================================
    /// <summary>현재 배치 가능한 보유 유물 ID를 호출자 버퍼에 채웁니다.</summary>
    public int FillPlaceableRelics(List<string> buffer)
    {
        buffer?.Clear();
        if (buffer == null || _milestones == null) return 0;
        return MuseumRelicGameDataReferences.FillPlaceable(_gameData, _inventory, _displayedIds, buffer);
    }

    //=============================================================================
    // TryPlace
    // Description: 유물을 소비하여 전시대에 배치하거나 기존 유물과 교체합니다.
    // Parameters: string pedestalId, string relicItemId, out MuseumRelicDisplayFailure failure
    // Return: bool
    //=============================================================================
    /// <summary>유물을 소비하여 전시대에 배치하거나 기존 유물과 교체합니다.</summary>
    public bool TryPlace(string pedestalId, string relicItemId,
        out MuseumRelicDisplayFailure failure)
    {
        if (_milestones == null) return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NOT_INITIALIZED, out failure);
        if (_isChanging) return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_BUSY, out failure);
        if (string.IsNullOrWhiteSpace(pedestalId) || !_knownPedestals.Contains(pedestalId))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVALID_PEDESTAL, out failure);
        if (string.IsNullOrWhiteSpace(relicItemId))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVALID_ITEM, out failure);
        if (!IsCollectibleRelic(relicItemId))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ITEM_NOT_RELIC, out failure);
        if (_displayedIds.Contains(relicItemId))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ALREADY_DISPLAYED, out failure);
        if (_inventory.GetItemCount(relicItemId) <= 0)
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ITEM_NOT_OWNED, out failure);
        _exhibits.TryGetValue(pedestalId, out string previous);
        _isChanging = true;
        try
        {
            if (!MuseumRelicDisplayStateStore.TryPlace(
                    _inventory, _exhibits, _displayedIds, _claimed,
                    pedestalId, relicItemId, previous, out failure)) return false;
            MuseumRelicDisplayStateStore.NotifyCommitted(ExhibitChanged, pedestalId, this);
            GrantPendingImmediateRewards();
            Save();
            return true;
        }
        finally { _isChanging = false; }
    }

    //=============================================================================
    // TryRecover
    // Description: 전시 유물을 인벤토리로 회수합니다.
    // Parameters: string pedestalId, out MuseumRelicDisplayFailure failure
    // Return: bool
    //=============================================================================
    /// <summary>전시 유물을 인벤토리로 회수합니다.</summary>
    public bool TryRecover(string pedestalId, out MuseumRelicDisplayFailure failure)
    {
        if (_milestones == null) return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NOT_INITIALIZED, out failure);
        if (_isChanging) return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_BUSY, out failure);
        if (string.IsNullOrWhiteSpace(pedestalId) || !_knownPedestals.Contains(pedestalId))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVALID_PEDESTAL, out failure);
        if (!_exhibits.TryGetValue(pedestalId, out string relicId))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_PEDESTAL_EMPTY, out failure);
        _isChanging = true;
        try
        {
            if (!MuseumRelicDisplayStateStore.TryRecover(
                    _inventory, _exhibits, _displayedIds, _claimed,
                    pedestalId, relicId, out failure)) return false;
            MuseumRelicDisplayStateStore.NotifyCommitted(ExhibitChanged, pedestalId, this);
            Save();
            return true;
        }
        finally { _isChanging = false; }
    }

    //=============================================================================
    // TryGetMilestone
    // Description: 마일스톤 정의와 현재 달성·수령 상태를 조회합니다.
    // Parameters: string id, out MuseumRelicMilestoneDefinition milestone, out MuseumRelicMilestoneStatus status
    // Return: bool
    //=============================================================================
    /// <summary>마일스톤 정의와 현재 달성·수령 상태를 조회합니다.</summary>
    public bool TryGetMilestone(string id, out MuseumRelicMilestoneDefinition milestone,
        out MuseumRelicMilestoneStatus status)
    {
        milestone = null;
        bool found = _milestones != null && _milestones.TryGet(id, out milestone);
        status = found ? new MuseumRelicMilestoneStatus(
            DisplayedRelicCount >= milestone.RequiredExhibitCount,
            _claimed.Contains(milestone.MilestoneId)) : default;
        return found;
    }

    //=============================================================================
    // TryClaimRoomReward
    // Description: 방 안에서 수령하도록 정의된 마일스톤 보상을 한 번만 지급합니다.
    // Parameters: string id, out MuseumRelicDisplayFailure failure
    // Return: bool
    //=============================================================================
    /// <summary>방 안에서 수령하도록 정의된 마일스톤 보상을 한 번만 지급합니다.</summary>
    public bool TryClaimRoomReward(string id, out MuseumRelicDisplayFailure failure)
    {
        if (_isChanging) return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_BUSY, out failure);
        if (!TryGetMilestone(id, out var milestone, out var status))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_UNKNOWN_MILESTONE, out failure);
        if (milestone.RewardMode
            != MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP)
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ROOM_PICKUP, out failure);
        if (!status.IsEligible) return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ELIGIBLE, out failure);
        if (status.IsClaimed) return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_ALREADY_CLAIMED, out failure);
        _isChanging = true;
        try
        {
            if (!TryGrant(milestone, out failure)) return false;
            Save();
            return true;
        }
        finally { _isChanging = false; }
    }

    //=============================================================================
    // RetryPendingImmediateRewards
    // Description: 조건을 충족했지만 지급되지 않은 즉시 보상을 다시 시도합니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
    /// <summary>조건을 충족했지만 지급되지 않은 즉시 보상을 다시 시도합니다.</summary>
    public void RetryPendingImmediateRewards()
    {
        if (_milestones == null || _isChanging) return;
        _isChanging = true;
        try { if (GrantPendingImmediateRewards()) Save(); }
        finally { _isChanging = false; }
    }

    //=============================================================================
    // GrantPendingImmediateRewards
    // Description: 미수령 즉시 보상만 조건을 검사하고 재시도합니다.
    // Parameters: None.
    // Return: bool
    //=============================================================================
    private bool GrantPendingImmediateRewards()
    {
        bool granted = false;
        IReadOnlyList<MuseumRelicMilestoneDefinition> values = _milestones.Milestones;
        for (int i = 0; i < values.Count; i++)
            if (values[i].RewardMode
                == MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_GRANT_IMMEDIATELY
                && DisplayedRelicCount >= values[i].RequiredExhibitCount
                && !_claimed.Contains(values[i].MilestoneId))
            {
                if (TryGrant(values[i], out MuseumRelicDisplayFailure failure)) granted = true;
                else if (failure == MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED)
                    Debug.LogError($"[MuseumRelicDisplayController] 보상 상태 적용 실패: {failure}", this);
            }
        return granted;
    }

    //=============================================================================
    // TryGrant
    // Description: 보상 아이템과 한 번성 수령 상태를 함께 적용합니다.
    // Parameters: MuseumRelicMilestoneDefinition value, out MuseumRelicDisplayFailure failure
    // Return: bool
    //=============================================================================
    private bool TryGrant(MuseumRelicMilestoneDefinition value,
        out MuseumRelicDisplayFailure failure)
    {
        if (!MuseumRelicDisplayStateStore.TryGrant(
                _inventory, _exhibits, _claimed, value, out failure)) return false;
        MuseumRelicDisplayStateStore.NotifyCommitted(MilestoneChanged, value.MilestoneId, this);
        return true;
    }

    // 유물 원본과 도감 관계를 조회하는 주입 데이터입니다.
    private bool IsCollectibleRelic(string id) => _gameData.TryGetItem(id, out ItemDefinition item)
        && item.ItemType == ItemType.ITEM_RELIC_COLLECTIBLE
        && _gameData.CodexData.TryGetCodexEntryBySource(CodexCategory.CODEX_CATEGORY_RELIC, id, out _);

    private void Save() => MuseumRelicDisplayStateStore.SaveToDiskAsync(
        _progress, this, this.GetCancellationTokenOnDestroy()).Forget();

    private static bool Fail(MuseumRelicDisplayFailure value, out MuseumRelicDisplayFailure failure)
    {
        failure = value;
        return false;
    }
    private static bool FailLoad(string message, out GameDataCsvLoadError error)
    {
        error = new GameDataCsvLoadError("(museum relic)", 0, 0, string.Empty, message);
        return false;
    }
}
