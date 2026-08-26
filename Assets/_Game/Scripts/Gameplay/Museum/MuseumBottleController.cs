//=============================================================================
// File:        MuseumBottleController.cs
// Description: 술 소모, 병 진행 상태와 최초 완성 보상을 원자적으로 조정한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MuseumBottleController : MonoBehaviour, IMuseumBottleService
{
    public const string STATE_ID = "museum.bottles.v1";

    [Header("Museum Phase 5 CSV")]
    [Tooltip("챕터별 술병과 선택적 완성 보상을 정의하는 museum_bottles.csv입니다.")]
    [SerializeField] private TextAsset _museumBottlesCsv;
    [Tooltip("술병별 허용 술과 채움 기여도를 정의하는 museum_bottle_items.csv입니다.")]
    [SerializeField] private TextAsset _museumBottleItemsCsv;

    private readonly Dictionary<string, MuseumBottleRuntimeState> _stateByBottleId =
        new Dictionary<string, MuseumBottleRuntimeState>(StringComparer.Ordinal);
    private MuseumBottleDataRepository _repository;
    private IInventoryService _inventory;
    private IProgressService _progress;
    private bool _isChanging;

    public event Action<string> BottleChanged;
    public int BottleCount => _repository != null ? _repository.BottleCount : 0;

    /// <summary>인스펙터 CSV를 검증하고 인벤토리·진행 상태를 연결합니다.</summary>
    public bool TryInit(IGameDataRepository gameData, IInventoryService inventory,
        IProgressService progress, out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (gameData == null || gameData.CodexData == null || inventory == null || progress == null)
            return FailLoad("술병 초기화 의존성이 없습니다.", out loadError);
        if (!MuseumBottleCsvDefinitionLoader.TryLoad(
                CreateCsv(_museumBottlesCsv), CreateCsv(_museumBottleItemsCsv),
                out List<MuseumBottleDefinition> bottles,
                out List<MuseumBottleItemDefinition> items, out loadError))
            return false;
        if (!MuseumBottleDataRepository.TryCreate(bottles, items, new ReferenceData(gameData),
                out MuseumBottleDataRepository repository,
                out GameDataRepositoryBuildError buildError))
            return FailLoad($"{buildError.Message} id={buildError.DataId}", out loadError);
        return Init(repository, inventory, progress);
    }

    /// <summary>검증된 저장소와 런타임 서비스를 연결하고 저장 상태를 복원합니다.</summary>
    public bool Init(MuseumBottleDataRepository repository,
        IInventoryService inventory, IProgressService progress)
    {
        _repository = null;
        _inventory = null;
        _progress = null;
        _stateByBottleId.Clear();
        _isChanging = false;
        if (repository == null || inventory == null || progress == null) return false;
        _repository = repository;
        _inventory = inventory;
        _progress = progress;
        MuseumBottleStateStore.Load(_progress, _repository, _stateByBottleId);
        return true;
    }

    public bool TryGetBottle(string bottleId, out MuseumBottleDefinition bottle)
    {
        bottle = null;
        return _repository != null && _repository.TryGetBottle(bottleId, out bottle);
    }

    public bool TryGetProgress(string bottleId, out MuseumBottleProgress progress)
    {
        progress = default;
        if (_repository == null || !_repository.TryGetBottleItems(bottleId, out var items)
            || !_stateByBottleId.TryGetValue(bottleId, out MuseumBottleRuntimeState state)) return false;
        int contributedValue = 0;
        int requiredValue = 0;
        for (int i = 0; i < items.Count; i++)
        {
            requiredValue += items[i].ContributionValue;
            if (state.ContributedIds.Contains(items[i].AlcoholItemId))
                contributedValue += items[i].ContributionValue;
        }
        progress = new MuseumBottleProgress(state.ContributedIds.Count, items.Count,
            contributedValue, requiredValue, state.Completed, state.RewardGranted);
        return true;
    }

    public bool IsContributed(string bottleId, string alcoholItemId) =>
        !string.IsNullOrWhiteSpace(alcoholItemId)
        && _stateByBottleId.TryGetValue(bottleId ?? string.Empty, out MuseumBottleRuntimeState state)
        && state.ContributedIds.Contains(alcoholItemId);

    public int FillContributableItems(string bottleId, List<MuseumBottleItemDefinition> buffer)
    {
        if (buffer == null) return 0;
        buffer.Clear();
        if (_repository == null || !_repository.TryGetBottleItems(bottleId, out var items)
            || !_stateByBottleId.TryGetValue(bottleId, out MuseumBottleRuntimeState state)
            || state.Completed) return 0;
        for (int i = 0; i < items.Count; i++)
            if (!state.ContributedIds.Contains(items[i].AlcoholItemId)
                && _inventory.GetItemCount(items[i].AlcoholItemId) > 0)
                buffer.Add(items[i]);
        return buffer.Count;
    }

    public bool TryContribute(string bottleId, string alcoholItemId,
        out MuseumBottleContributionFailure failure)
    {
        failure = MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_NONE;
        if (_repository == null || _inventory == null || _progress == null)
            return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_NOT_INITIALIZED, out failure);
        if (_isChanging) return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_BUSY, out failure);
        if (string.IsNullOrWhiteSpace(bottleId) || string.IsNullOrWhiteSpace(alcoholItemId))
            return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_INVALID_ID, out failure);
        if (!_repository.TryGetBottle(bottleId, out MuseumBottleDefinition bottle))
            return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_UNKNOWN_BOTTLE, out failure);
        if (!_repository.TryGetBottleItem(alcoholItemId, out MuseumBottleItemDefinition item)
            || item.BottleId != bottleId)
            return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ITEM_NOT_ALLOWED, out failure);
        MuseumBottleRuntimeState state = _stateByBottleId[bottleId];
        if (state.Completed)
            return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ALREADY_COMPLETED, out failure);
        if (state.ContributedIds.Contains(alcoholItemId))
            return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ALREADY_CONTRIBUTED, out failure);
        if (_inventory.GetItemCount(alcoholItemId) < 1)
            return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ITEM_NOT_OWNED, out failure);

        _repository.TryGetBottleItems(bottleId, out var requiredItems);
        bool completes = state.ContributedIds.Count + 1 == requiredItems.Count;
        if (completes && bottle.HasReward
            && !_inventory.CanAddItem(bottle.RewardItemId, bottle.RewardAmount.Value))
            return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_REWARD_UNAVAILABLE, out failure);
        _isChanging = true;
        try
        {
            bool changed = completes && bottle.HasReward
                ? _inventory.TryExchangeItem(alcoholItemId, 1,
                    bottle.RewardItemId, bottle.RewardAmount.Value)
                : _inventory.TryConsumeItem(alcoholItemId, 1);
            if (!changed)
                return Fail(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_INVENTORY_CHANGE_FAILED, out failure);
            state.ContributedIds.Add(alcoholItemId);
            state.Completed = completes;
            if (completes) state.RewardGranted = true;
            MuseumBottleStateStore.Write(_progress, _stateByBottleId);
            BottleChanged?.Invoke(bottleId);
            SaveProgressAsync(this.GetCancellationTokenOnDestroy()).Forget();
            return true;
        }
        finally { _isChanging = false; }
    }

    private async UniTaskVoid SaveProgressAsync(CancellationToken token)
    {
        try { if (!await _progress.SaveAsync(token)) Debug.LogWarning("[MuseumBottle] Save deferred.", this); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    private static GameDataCsvFile CreateCsv(TextAsset asset) =>
        asset != null ? new GameDataCsvFile(asset.name, asset.text) : null;
    private static bool Fail(MuseumBottleContributionFailure value,
        out MuseumBottleContributionFailure failure) { failure = value; return false; }
    private static bool FailLoad(string message, out GameDataCsvLoadError error)
    {
        error = new GameDataCsvLoadError("(museum bottles)", 0, 0, string.Empty, message);
        return false;
    }

    private sealed class ReferenceData : IMuseumBottleReferenceData
    {
        private readonly IGameDataRepository _data;
        internal ReferenceData(IGameDataRepository data) => _data = data;
        public bool TryGetItem(string id, out ItemDefinition item) => _data.TryGetItem(id, out item);
        public bool TryGetAlcoholCodexEntry(string id, out CodexEntryDefinition entry) =>
            _data.CodexData.TryGetCodexEntryBySource(CodexCategory.CODEX_CATEGORY_ALCOHOL, id, out entry);
        public bool HasLocalization(string key) => _data.TryGetLocalizationEntry(key, out _);
    }
}
