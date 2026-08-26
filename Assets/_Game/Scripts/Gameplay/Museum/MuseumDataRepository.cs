//=============================================================================
// File:        MuseumDataRepository.cs
// Description: 박물관 벽화와 제단 목적지 정의를 검증하고 ID로 조회한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;

/// <summary>벽화 한 개와 반환 기술의 연결 정의입니다.</summary>
public sealed class MuseumMuralDefinition
{
    public MuseumMuralDefinition(string muralId, int chapter, string technologyId)
    {
        MuralId = muralId ?? string.Empty;
        Chapter = chapter;
        TechnologyId = technologyId ?? string.Empty;
    }

    public string MuralId { get; }
    public int Chapter { get; }
    public string TechnologyId { get; }
}

/// <summary>박물관 제단에서 이동할 스테이지와 SpawnPoint 정의입니다.</summary>
public sealed class AltarDestinationDefinition
{
    public AltarDestinationDefinition(string altarId, string displayNameKey,
        int stageNumber, string spawnPointId, bool isMainAltar)
    {
        AltarId = altarId ?? string.Empty;
        DisplayNameKey = displayNameKey ?? string.Empty;
        StageNumber = stageNumber;
        SpawnPointId = spawnPointId ?? string.Empty;
        IsMainAltar = isMainAltar;
    }

    public string AltarId { get; }
    public string DisplayNameKey { get; }
    public int StageNumber { get; }
    public string SpawnPointId { get; }
    public bool IsMainAltar { get; }
}

/// <summary>박물관 CSV가 참조하는 기술과 현지화 Key의 최소 조회 계약입니다.</summary>
public interface IMuseumReferenceData
{
    bool TryGetTechnology(string technologyId, out TechnologyDefinition technology);
    bool HasLocalization(string textKey);
}

/// <summary>검증된 박물관 Phase 4 정의의 읽기 전용 저장소입니다.</summary>
public sealed class MuseumDataRepository
{
    private readonly Dictionary<string, MuseumMuralDefinition> _muralById;
    private readonly Dictionary<string, AltarDestinationDefinition> _altarById;
    private readonly MuseumMuralDefinition[] _murals;
    private readonly AltarDestinationDefinition[] _altars;

    private MuseumDataRepository(IReadOnlyList<MuseumMuralDefinition> murals,
        IReadOnlyList<AltarDestinationDefinition> altars)
    {
        _murals = Copy(murals);
        _altars = Copy(altars);
        _muralById = new Dictionary<string, MuseumMuralDefinition>(StringComparer.Ordinal);
        _altarById = new Dictionary<string, AltarDestinationDefinition>(StringComparer.Ordinal);
        for (int i = 0; i < _murals.Length; i++) _muralById.Add(_murals[i].MuralId, _murals[i]);
        for (int i = 0; i < _altars.Length; i++) _altarById.Add(_altars[i].AltarId, _altars[i]);
    }

    public int MuralCount => _murals.Length;
    public int AltarDestinationCount => _altars.Length;
    public IReadOnlyList<MuseumMuralDefinition> Murals => _murals;
    public IReadOnlyList<AltarDestinationDefinition> AltarDestinations => _altars;

    public bool TryGetMural(string muralId, out MuseumMuralDefinition mural) =>
        _muralById.TryGetValue(muralId ?? string.Empty, out mural);

    public bool TryGetAltarDestination(string altarId, out AltarDestinationDefinition altar) =>
        _altarById.TryGetValue(altarId ?? string.Empty, out altar);

    public static bool TryCreate(IReadOnlyList<MuseumMuralDefinition> murals,
        IReadOnlyList<AltarDestinationDefinition> altars, IMuseumReferenceData references,
        out MuseumDataRepository repository, out GameDataRepositoryBuildError error)
    {
        repository = null;
        error = null;
        if (murals == null || altars == null || references == null)
            return Fail("Museum", string.Empty, "박물관 검증 입력이 없습니다.", out error);

        HashSet<string> muralIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> technologyIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < murals.Count; i++)
        {
            MuseumMuralDefinition mural = murals[i];
            if (mural == null || !muralIds.Add(mural.MuralId))
                return Fail("MuseumMural", mural?.MuralId, "벽화 ID가 비었거나 중복됩니다.", out error);
            if (!technologyIds.Add(mural.TechnologyId)
                || !references.TryGetTechnology(mural.TechnologyId, out TechnologyDefinition technology))
                return Fail("MuseumMural", mural.MuralId, "기술 ID가 없거나 중복 연결됐습니다.", out error);
            if (mural.Chapter < 0 || mural.Chapter > 3 || technology.Chapter != mural.Chapter)
                return Fail("MuseumMural", mural.MuralId, "벽화와 기술의 챕터가 0~3 범위에서 일치해야 합니다.", out error);
        }

        HashSet<string> altarIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> spawnIds = new HashSet<string>(StringComparer.Ordinal);
        int mainAltarCount = 0;
        for (int i = 0; i < altars.Count; i++)
        {
            AltarDestinationDefinition altar = altars[i];
            if (altar == null || !altarIds.Add(altar.AltarId) || !spawnIds.Add(altar.SpawnPointId))
                return Fail("AltarDestination", altar?.AltarId, "제단 또는 SpawnPoint ID가 비었거나 중복됩니다.", out error);
            if (altar.StageNumber < 0 || !references.HasLocalization(altar.DisplayNameKey))
                return Fail("AltarDestination", altar.AltarId, "스테이지 번호 또는 이름 Key가 유효하지 않습니다.", out error);
            if (altar.IsMainAltar) mainAltarCount++;
        }
        if (altars.Count > 0 && mainAltarCount != 1)
            return Fail("AltarDestination", string.Empty, "작성된 목적지에는 메인 제단이 정확히 한 개여야 합니다.", out error);

        repository = new MuseumDataRepository(murals, altars);
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
