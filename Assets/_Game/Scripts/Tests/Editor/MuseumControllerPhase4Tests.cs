//=============================================================================
// File:        MuseumControllerPhase4Tests.cs
// Description: 기술 반환, 벽화 저장, 제단 이동 데이터 변환을 검증한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public sealed class MuseumControllerPhase4Tests
{
    private const string MURAL_ID = "MURAL_CH1";
    private const string TECH_ID = "TECH_CH1";
    private const string ALTAR_ID = "ALTAR_MAIN";
    private const string SPAWN_ID = "SPAWN_MUSEUM";
    private GameObject _root;
    private MuseumController _controller;
    private FakeProgress _progress;
    private FakeRestoration _restoration;

    [SetUp]
    public void SetUp()
    {
        FakeReferences references = new FakeReferences();
        references.Technology = new TechnologyDefinition(
            TECH_ID, "tech.name", "tech.description", "tech.operation",
            string.Empty, string.Empty, 1, 10);
        Assert.That(MuseumDataRepository.TryCreate(
            new[] { new MuseumMuralDefinition(MURAL_ID, 1, TECH_ID) },
            new[] { new AltarDestinationDefinition(ALTAR_ID, "altar.main.name", 4, SPAWN_ID, true) },
            references, out MuseumDataRepository repository, out _), Is.True);
        _root = new GameObject(nameof(MuseumControllerPhase4Tests));
        _controller = _root.AddComponent<MuseumController>();
        _progress = new FakeProgress();
        _restoration = new FakeRestoration();
        Assert.That(_controller.Init(repository, _progress, _restoration), Is.True);
    }

    [TearDown]
    public void TearDown() => UnityEngine.Object.DestroyImmediate(_root);

    [Test]
    public void UnreturnedTechnologyCannotRevealOrSaveMural()
    {
        bool revealed = _controller.TryRevealMural(MURAL_ID, out MuseumRevealFailure failure);
        Assert.That(revealed, Is.False);
        Assert.That(failure, Is.EqualTo(
            MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_TECHNOLOGY_NOT_RETURNED));
        Assert.That(_progress.SetFlagCalls, Is.Zero);
        Assert.That(_progress.SaveCalls, Is.Zero);
    }

    [Test]
    public void ReturnedTechnologyRevealsAndSavesMuralOnlyOnce()
    {
        _restoration.IsReturnedValue = true;
        Assert.That(_controller.TryRevealMural(MURAL_ID, out _), Is.True);
        Assert.That(_controller.TryRevealMural(MURAL_ID, out MuseumRevealFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(
            MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_ALREADY_REVEALED));
        Assert.That(_progress.SetFlagCalls, Is.EqualTo(1));
        Assert.That(_progress.SaveCalls, Is.EqualTo(1));
    }

    [Test]
    public void LoadedRevealFlagRestoresFinalStateWithoutNewWriteOrSave()
    {
        _progress.SetFlag(MuseumController.MURAL_REVEALED_FLAG_PREFIX + MURAL_ID, true);
        int callsAfterLoad = _progress.SetFlagCalls;
        Assert.That(_controller.IsMuralRevealed(MURAL_ID), Is.True);
        Assert.That(_progress.SetFlagCalls, Is.EqualTo(callsAfterLoad));
        Assert.That(_progress.SaveCalls, Is.Zero);
    }

    [Test]
    public void AltarDestinationCreatesExactSceneTransferData()
    {
        Assert.That(_controller.TryCreateAltarTransferData(
            ALTAR_ID, out SceneTransferData transferData), Is.True);
        Assert.That(transferData.enteringStageNumber, Is.EqualTo(4));
        Assert.That(transferData.targetSpawnPointId, Is.EqualTo(SPAWN_ID));
        Assert.That(_controller.TryCreateAltarTransferData("UNKNOWN", out _), Is.False);
    }

    [Test]
    public void UnknownMuralFailsSafely()
    {
        _restoration.IsReturnedValue = true;
        Assert.That(_controller.TryRevealMural("MURAL_UNKNOWN", out MuseumRevealFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_UNKNOWN_MURAL));
    }

    private sealed class FakeReferences : IMuseumReferenceData
    {
        internal TechnologyDefinition Technology { get; set; }
        public bool TryGetTechnology(string id, out TechnologyDefinition technology)
        {
            technology = id == Technology?.TechnologyId ? Technology : null;
            return technology != null;
        }
        public bool HasLocalization(string key) => key == "altar.main.name";
    }

    private sealed class FakeProgress : IProgressService
    {
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();
        internal int SetFlagCalls { get; private set; }
        internal int SaveCalls { get; private set; }
        public bool IsDirty => false;
        public void SetFlag(string id, bool value) { SetFlagCalls++; _flags[id] = value; }
        public bool GetFlag(string id) => _flags.TryGetValue(id, out bool value) && value;
        public UniTask<bool> LoadAsync(CancellationToken token = default) => UniTask.FromResult(true);
        public UniTask<bool> SaveAsync(CancellationToken token = default)
        { SaveCalls++; return UniTask.FromResult(true); }
        public void SetSystemState(string id, string json) { }
        public bool TryGetSystemState(string id, out string json) { json = null; return false; }
        public void SetObjectState(int stage, string id, string json) { }
        public bool TryGetObjectState(int stage, string id, out string json) { json = null; return false; }
        public void Reset() => _flags.Clear();
    }

    private sealed class FakeRestoration : IRelicRestorationService
    {
        internal bool IsReturnedValue { get; set; }
        public int CurrentRestore => 0;
        public int MaxRestore => 100;
        public int RestoreStage => 0;
        public bool IsKnownTechnology(string id) => id == TECH_ID;
        public bool IsAcquired(string id) => id == TECH_ID;
        public bool IsReturned(string id) => id == TECH_ID && IsReturnedValue;
        public int FillReturnableTechnologies(List<TechnologyDefinition> buffer)
        { buffer?.Clear(); return 0; }
        public bool TryAcquireTechnology(string id, out RelicRestorationFailure failure)
        { failure = default; return false; }
        public bool TryReturnTechnology(string id, out RelicRestorationResult result,
            out RelicRestorationFailure failure)
        { result = default; failure = default; return false; }
    }
}
