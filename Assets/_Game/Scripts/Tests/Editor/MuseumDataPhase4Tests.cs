//=============================================================================
// File:        MuseumDataPhase4Tests.cs
// Description: 박물관 Phase 4 CSV 파싱과 교차 참조 검증을 확인한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class MuseumDataPhase4Tests
{
    private const string TECH_ID = "TECH_MURAL_CH1";

    [Test]
    public void LoaderParsesMuralAndAltarRows()
    {
        GameDataCsvFile murals = new GameDataCsvFile("museum_murals.csv",
            "mural_id,chapter,technology_id\nMURAL_CH1,1,TECH_MURAL_CH1\n");
        GameDataCsvFile altars = new GameDataCsvFile("altar_destinations.csv",
            "altar_id,display_name_key,stage_number,spawn_point_id,is_main_altar\n"
            + "ALTAR_MAIN,altar.main.name,0,SPAWN_MUSEUM,true\n");

        bool loaded = MuseumCsvDefinitionLoader.TryLoad(
            murals, altars, out List<MuseumMuralDefinition> muralDefinitions,
            out List<AltarDestinationDefinition> altarDefinitions, out GameDataCsvLoadError error);

        Assert.That(loaded, Is.True, error?.ToString());
        Assert.That(muralDefinitions, Has.Count.EqualTo(1));
        Assert.That(muralDefinitions[0].TechnologyId, Is.EqualTo(TECH_ID));
        Assert.That(altarDefinitions, Has.Count.EqualTo(1));
        Assert.That(altarDefinitions[0].SpawnPointId, Is.EqualTo("SPAWN_MUSEUM"));
        Assert.That(altarDefinitions[0].IsMainAltar, Is.True);
    }

    [Test]
    public void LoaderRejectsUppercaseBoolean()
    {
        GameDataCsvFile altars = new GameDataCsvFile("altar_destinations.csv",
            "altar_id,display_name_key,stage_number,spawn_point_id,is_main_altar\n"
            + "ALTAR_MAIN,altar.main.name,0,SPAWN_MUSEUM,TRUE\n");

        bool loaded = MuseumCsvDefinitionLoader.TryLoad(
            null, altars, out _, out _, out GameDataCsvLoadError error);

        Assert.That(loaded, Is.False);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void RepositoryAcceptsHeaderOnlyPlanningData()
    {
        bool created = MuseumDataRepository.TryCreate(
            new List<MuseumMuralDefinition>(), new List<AltarDestinationDefinition>(),
            new FakeReferences(), out MuseumDataRepository repository,
            out GameDataRepositoryBuildError error);

        Assert.That(created, Is.True, error?.ToString());
        Assert.That(repository.MuralCount, Is.Zero);
        Assert.That(repository.AltarDestinationCount, Is.Zero);
    }

    [Test]
    public void RepositoryRejectsUnknownTechnologyWithoutThrowing()
    {
        List<MuseumMuralDefinition> murals = new List<MuseumMuralDefinition>
        {
            new MuseumMuralDefinition("MURAL_UNKNOWN", 1, "TECH_UNKNOWN")
        };

        bool created = MuseumDataRepository.TryCreate(
            murals, new List<AltarDestinationDefinition>(), new FakeReferences(),
            out _, out GameDataRepositoryBuildError error);

        Assert.That(created, Is.False);
        Assert.That(error.DataType, Is.EqualTo("MuseumMural"));
    }

    [Test]
    public void RepositoryRejectsChapterMismatch()
    {
        FakeReferences references = new FakeReferences();
        references.AddTechnology(new TechnologyDefinition(
            TECH_ID, "tech.name", "tech.description", "tech.operation", string.Empty,
            string.Empty, 1, 10));
        List<MuseumMuralDefinition> murals = new List<MuseumMuralDefinition>
        {
            new MuseumMuralDefinition("MURAL_CH2", 2, TECH_ID)
        };

        bool created = MuseumDataRepository.TryCreate(
            murals, new List<AltarDestinationDefinition>(), references, out _, out _);

        Assert.That(created, Is.False);
    }

    [Test]
    public void WrittenAltarDataRequiresExactlyOneMainAltar()
    {
        FakeReferences references = new FakeReferences();
        references.AddLocalization("altar.main.name");
        List<AltarDestinationDefinition> altars = new List<AltarDestinationDefinition>
        {
            new AltarDestinationDefinition(
                "ALTAR_SIDE", "altar.main.name", 0, "SPAWN_SIDE", false)
        };

        bool created = MuseumDataRepository.TryCreate(
            new List<MuseumMuralDefinition>(), altars, references, out _, out _);

        Assert.That(created, Is.False);
    }

    private sealed class FakeReferences : IMuseumReferenceData
    {
        private readonly Dictionary<string, TechnologyDefinition> _technologies =
            new Dictionary<string, TechnologyDefinition>();
        private readonly HashSet<string> _localization = new HashSet<string>();

        internal void AddTechnology(TechnologyDefinition technology) =>
            _technologies.Add(technology.TechnologyId, technology);
        internal void AddLocalization(string key) => _localization.Add(key);
        public bool TryGetTechnology(string id, out TechnologyDefinition technology) =>
            _technologies.TryGetValue(id, out technology);
        public bool HasLocalization(string key) => _localization.Contains(key);
    }
}
