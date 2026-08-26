//=============================================================================
// File:        MuseumController.cs
// Description: 박물관 CSV 조회와 벽화별 영구 개봉 상태 변경을 관리한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum MuseumRevealFailure
{
    MUSEUM_REVEAL_FAILURE_NONE = 0,
    MUSEUM_REVEAL_FAILURE_NOT_INITIALIZED,
    MUSEUM_REVEAL_FAILURE_INVALID_MURAL_ID,
    MUSEUM_REVEAL_FAILURE_UNKNOWN_MURAL,
    MUSEUM_REVEAL_FAILURE_UNKNOWN_TECHNOLOGY,
    MUSEUM_REVEAL_FAILURE_TECHNOLOGY_NOT_RETURNED,
    MUSEUM_REVEAL_FAILURE_ALREADY_REVEALED
}

/// <summary>박물관 벽화 상태와 제단 목적지의 외부 공개 계약입니다.</summary>
public interface IMuseumService
{
    int MuralCount { get; }
    int AltarDestinationCount { get; }
    bool TryGetMural(string muralId, out MuseumMuralDefinition mural);
    bool TryGetAltarDestination(string altarId, out AltarDestinationDefinition altar);
    bool TryCreateAltarTransferData(string altarId, out SceneTransferData transferData);
    bool IsMuralRevealed(string muralId);
    bool TryRevealMural(string muralId, out MuseumRevealFailure failure);
}

[DisallowMultipleComponent]
public sealed class MuseumController : MonoBehaviour, IMuseumService
{
    public const string MURAL_REVEALED_FLAG_PREFIX = "museum.mural.revealed.";

    [Header("Museum Phase 4 CSV")]
    [Tooltip("벽화와 반환 기술 연결을 정의하는 museum_murals.csv입니다.")]
    [SerializeField] private TextAsset _museumMuralsCsv;
    [Tooltip("제단 이동 목적지를 정의하는 altar_destinations.csv입니다.")]
    [SerializeField] private TextAsset _altarDestinationsCsv;

    private readonly Dictionary<string, string> _revealFlagByMuralId =
        new Dictionary<string, string>(StringComparer.Ordinal);
    private MuseumDataRepository _repository;
    private IProgressService _progress;
    private IRelicRestorationService _restoration;

    public int MuralCount => _repository != null ? _repository.MuralCount : 0;
    public int AltarDestinationCount => _repository != null ? _repository.AltarDestinationCount : 0;

    /// <summary>인스펙터 CSV를 검증하고 기존 Progress·기술 반환 서비스를 연결합니다.</summary>
    public bool TryInit(IGameDataRepository gameData, IProgressService progress,
        IRelicRestorationService restoration, out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (gameData == null || gameData.CodexData == null || progress == null || restoration == null)
        {
            loadError = new GameDataCsvLoadError(
                "(museum)", 0, 0, string.Empty, "박물관 초기화 의존성이 없습니다.");
            return false;
        }

        if (!MuseumCsvDefinitionLoader.TryLoad(
                CreateOptionalCsvFile(_museumMuralsCsv), CreateOptionalCsvFile(_altarDestinationsCsv),
                out List<MuseumMuralDefinition> murals,
                out List<AltarDestinationDefinition> altars, out loadError))
            return false;

        ReferenceData references = new ReferenceData(gameData);
        if (!MuseumDataRepository.TryCreate(
                murals, altars, references, out MuseumDataRepository repository,
                out GameDataRepositoryBuildError buildError))
        {
            loadError = new GameDataCsvLoadError(
                "(museum repository)", 0, 0, buildError.DataType,
                $"{buildError.Message} id={buildError.DataId}");
            return false;
        }

        return Init(repository, progress, restoration);
    }

    /// <summary>검증된 박물관 저장소와 런타임 상태 서비스를 연결합니다.</summary>
    public bool Init(MuseumDataRepository repository, IProgressService progress,
        IRelicRestorationService restoration)
    {
        _repository = null;
        _progress = null;
        _restoration = null;
        _revealFlagByMuralId.Clear();
        if (repository == null || progress == null || restoration == null)
            return false;

        _repository = repository;
        _progress = progress;
        _restoration = restoration;
        IReadOnlyList<MuseumMuralDefinition> murals = repository.Murals;
        for (int i = 0; i < murals.Count; i++)
        {
            string muralId = murals[i].MuralId;
            _revealFlagByMuralId.Add(muralId, MURAL_REVEALED_FLAG_PREFIX + muralId);
        }
        return true;
    }

    public bool TryGetMural(string muralId, out MuseumMuralDefinition mural)
    {
        mural = null;
        return _repository != null && _repository.TryGetMural(muralId, out mural);
    }

    public bool TryGetAltarDestination(string altarId, out AltarDestinationDefinition altar)
    {
        altar = null;
        return _repository != null && _repository.TryGetAltarDestination(altarId, out altar);
    }

    public bool TryCreateAltarTransferData(string altarId, out SceneTransferData transferData)
    {
        transferData = null;
        if (!TryGetAltarDestination(altarId, out AltarDestinationDefinition altar))
            return false;

        transferData = new SceneTransferData(altar.SpawnPointId, altar.StageNumber);
        return true;
    }

    public bool IsMuralRevealed(string muralId) =>
        TryGetRevealFlag(muralId, out string flagId) && _progress.GetFlag(flagId);

    public bool TryRevealMural(string muralId, out MuseumRevealFailure failure)
    {
        failure = MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_NONE;
        if (_repository == null || _progress == null || _restoration == null)
            return Fail(MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_NOT_INITIALIZED, out failure);
        if (string.IsNullOrWhiteSpace(muralId))
            return Fail(MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_INVALID_MURAL_ID, out failure);
        if (!_repository.TryGetMural(muralId, out MuseumMuralDefinition mural))
            return Fail(MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_UNKNOWN_MURAL, out failure);
        if (!_restoration.IsKnownTechnology(mural.TechnologyId))
            return Fail(MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_UNKNOWN_TECHNOLOGY, out failure);
        if (!_restoration.IsReturned(mural.TechnologyId))
            return Fail(MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_TECHNOLOGY_NOT_RETURNED, out failure);
        if (IsMuralRevealed(muralId))
            return Fail(MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_ALREADY_REVEALED, out failure);

        _progress.SetFlag(_revealFlagByMuralId[muralId], true);
        SaveProgressAsync(this.GetCancellationTokenOnDestroy()).Forget();
        return true;
    }

    private async UniTaskVoid SaveProgressAsync(CancellationToken token)
    {
        try
        {
            if (!await _progress.SaveAsync(token))
                Debug.LogWarning("[MuseumController] Mural reveal save was deferred.", this);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    private bool TryGetRevealFlag(string muralId, out string flagId)
    {
        flagId = null;
        return _progress != null && !string.IsNullOrWhiteSpace(muralId)
            && _revealFlagByMuralId.TryGetValue(muralId, out flagId);
    }

    private static GameDataCsvFile CreateOptionalCsvFile(TextAsset asset) =>
        asset != null ? new GameDataCsvFile(asset.name, asset.text) : null;

    private static bool Fail(MuseumRevealFailure value, out MuseumRevealFailure failure)
    {
        failure = value;
        return false;
    }

    private sealed class ReferenceData : IMuseumReferenceData
    {
        private readonly IGameDataRepository _gameData;
        internal ReferenceData(IGameDataRepository gameData) => _gameData = gameData;
        public bool TryGetTechnology(string id, out TechnologyDefinition technology) =>
            _gameData.CodexData.TryGetTechnology(id, out technology);
        public bool HasLocalization(string key) => _gameData.TryGetLocalizationEntry(key, out _);
    }
}
