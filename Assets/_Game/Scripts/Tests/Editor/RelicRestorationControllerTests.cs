//=============================================================================
// File:        RelicRestorationControllerTests.cs
// Description: 기술 획득·반환과 재건도 계산 규칙을 검증한다.
// History:     07/22/2026 + Created -- Codex
//              07/31/2026 + Technology 데이터 적용 -- Codex
//=============================================================================
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;
//=============================================================================
// Synopsis:   RelicRestorationControllerTests;
//                - 잘못되거나 중복된 기술 상태 변경 방지
//                - 반환된 기술에 따른 재건도와 단계 계산 검증
//=============================================================================
[TestFixture]
public sealed class RelicRestorationControllerTests
{
    private const string TECHNOLOGY_MOVE = "TECHNOLOGY_MOVE";
    private const string TECHNOLOGY_POWER = "TECHNOLOGY_POWER";
    private const string TECHNOLOGY_CRAFT = "TECHNOLOGY_CRAFT";
    private const string TECHNOLOGY_LIFE = "TECHNOLOGY_LIFE";
    private readonly List<Object> _createdObjects = new List<Object>();
    private RelicRestorationController _controller;
    private FakeProgressService _progress;
    [SetUp]
    public void SetUp()
    {
        RelicRestorationCatalogSO catalog = CreateObject<RelicRestorationCatalogSO>();
        SetField(catalog, "_technologies", new List<TechnologyDataSO>
        {
            CreateTechnology(TECHNOLOGY_MOVE, 0, 0),
            CreateTechnology(TECHNOLOGY_POWER, 1, 30),
            CreateTechnology(TECHNOLOGY_CRAFT, 2, 30),
            CreateTechnology(TECHNOLOGY_LIFE, 3, 40)
        });
        TimelineAsset timeline = CreateObject<TimelineAsset>();
        SetField(catalog, "_stages", new List<RelicRestorationCatalogSO.RestorationStageEntry>
        {
            CreateStage(1, 30, timeline), CreateStage(2, 60, timeline),
            CreateStage(3, 100, timeline)
        });
        SetField(catalog, "_altarDestinations",
            new List<RelicRestorationCatalogSO.AltarDestinationEntry> { CreateAltar() });
        GameObject gameObject = new GameObject(nameof(RelicRestorationControllerTests));
        _createdObjects.Add(gameObject);
        _controller = gameObject.AddComponent<RelicRestorationController>();
        SetField(_controller, "_catalog", catalog);
        _progress = new FakeProgressService();
        Assert.That(_controller.Init(_progress), Is.True);
    }
    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
        {
            Object.DestroyImmediate(_createdObjects[i]);
        }
        _createdObjects.Clear();
    }
    [Test]
    public void AcquiredTechnologyIsReturnableUntilReturned()
    {
        List<TechnologyDataSO> buffer = new List<TechnologyDataSO>();
        bool acquired = _controller.TryAcquireTechnology(TECHNOLOGY_POWER, out _);
        int count = _controller.FillReturnableTechnologies(buffer);
        Assert.That(acquired, Is.True);
        Assert.That(count, Is.EqualTo(1));
        Assert.That(buffer[0].TechnologyId, Is.EqualTo(TECHNOLOGY_POWER));
        Assert.That(_controller.TryReturnTechnology(TECHNOLOGY_POWER, out _, out _), Is.True);
        Assert.That(_controller.FillReturnableTechnologies(buffer), Is.Zero);
    }
    [Test]
    public void TechnologyCannotBeAcquiredTwice()
    {
        Assert.That(_controller.TryAcquireTechnology(TECHNOLOGY_POWER, out _), Is.True);

        bool acquired = _controller.TryAcquireTechnology(TECHNOLOGY_POWER, out RelicRestorationFailure failure);

        Assert.That(acquired, Is.False);
        Assert.That(failure, Is.EqualTo(
            RelicRestorationFailure.RESTORATION_FAILURE_TECHNOLOGY_ALREADY_ACQUIRED));
        Assert.That(_progress.SetFlagCallCount, Is.EqualTo(1));
    }
    [TestCase("", RelicRestorationFailure.RESTORATION_FAILURE_INVALID_TECHNOLOGY_ID)]
    [TestCase("UNKNOWN", RelicRestorationFailure.RESTORATION_FAILURE_TECHNOLOGY_NOT_FOUND)]
    public void InvalidTechnologyIdIsRejected(string technologyId, RelicRestorationFailure expected)
    {
        bool acquired = _controller.TryAcquireTechnology(technologyId, out RelicRestorationFailure failure);
        Assert.That(acquired, Is.False);
        Assert.That(failure, Is.EqualTo(expected));
        Assert.That(_progress.SetFlagCallCount, Is.Zero);
    }
    [Test]
    public void UnacquiredTechnologyCannotBeReturned()
    {
        bool returned = _controller.TryReturnTechnology(TECHNOLOGY_POWER, out _,
            out RelicRestorationFailure failure);
        Assert.That(returned, Is.False);
        Assert.That(failure, Is.EqualTo(
            RelicRestorationFailure.RESTORATION_FAILURE_TECHNOLOGY_NOT_ACQUIRED));
        Assert.That(_controller.CurrentRestore, Is.Zero);
    }
    [Test]
    public void TechnologyCannotBeReturnedTwice()
    {
        ReturnTechnology(TECHNOLOGY_POWER);

        bool returned = _controller.TryReturnTechnology(TECHNOLOGY_POWER, out _,
            out RelicRestorationFailure failure);

        Assert.That(returned, Is.False);
        Assert.That(failure, Is.EqualTo(
            RelicRestorationFailure.RESTORATION_FAILURE_TECHNOLOGY_ALREADY_RETURNED));
        Assert.That(_controller.CurrentRestore, Is.EqualTo(30));
    }
    [Test]
    public void ThreeRestorationTechnologiesReachEnding()
    {
        ReturnTechnology(TECHNOLOGY_POWER);
        ReturnTechnology(TECHNOLOGY_CRAFT);
        _controller.TryAcquireTechnology(TECHNOLOGY_LIFE, out _);

        bool returned = _controller.TryReturnTechnology(TECHNOLOGY_LIFE,
            out RelicRestorationResult result, out _);
        Assert.That(returned, Is.True);
        Assert.That(result.Technology.TechnologyId, Is.EqualTo(TECHNOLOGY_LIFE));
        Assert.That(result.CurrentRestore, Is.EqualTo(100));
        Assert.That(result.CurrentStage, Is.EqualTo(3));
        Assert.That(result.ShouldPlayEnding, Is.True);
    }
    private void ReturnTechnology(string technologyId)
    {
        Assert.That(_controller.TryAcquireTechnology(technologyId, out _), Is.True);
        Assert.That(_controller.TryReturnTechnology(technologyId, out _, out _), Is.True);
    }
    private TechnologyDataSO CreateTechnology(string technologyId, int chapter, int restoreValue)
    {
        TechnologyDataSO technology = CreateObject<TechnologyDataSO>();
        SetField(technology, "_technologyId", technologyId);
        SetField(technology, "_displayNameKey", string.Concat("name.", technologyId));
        SetField(technology, "_descriptionKey", string.Concat("description.", technologyId));
        SetField(technology, "_chapter", chapter);
        SetField(technology, "_restoreValue", restoreValue);
        return technology;
    }
    private static RelicRestorationCatalogSO.RestorationStageEntry CreateStage(
        int stage, int requiredRestore, TimelineAsset timeline)
    {
        var entry = new RelicRestorationCatalogSO.RestorationStageEntry();
        SetField(entry, "_stage", stage);
        SetField(entry, "_requiredRestore", requiredRestore);
        SetField(entry, "_timeline", timeline);
        return entry;
    }
    private static RelicRestorationCatalogSO.AltarDestinationEntry CreateAltar()
    {
        var entry = new RelicRestorationCatalogSO.AltarDestinationEntry();
        SetField(entry, "_altarId", "MAIN_ALTAR");
        SetField(entry, "_displayNameKey", "altar.main");
        SetField(entry, "_spawnPointId", "SPAWN_MAIN_ALTAR");
        return entry;
    }
    private T CreateObject<T>() where T : ScriptableObject
    {
        T createdObject = ScriptableObject.CreateInstance<T>();
        _createdObjects.Add(createdObject);
        return createdObject;
    }
    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
    private sealed class FakeProgressService : IProgressService
    {
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();
        internal int SetFlagCallCount { get; private set; }
        public bool IsDirty => false;
        public UniTask<bool> LoadAsync(CancellationToken cancellationToken = default) => UniTask.FromResult(true);
        public UniTask<bool> SaveAsync(CancellationToken cancellationToken = default) => UniTask.FromResult(true);
        public void SetFlag(string flagId, bool value)
        {
            SetFlagCallCount++;
            _flags[flagId] = value;
        }
        public bool GetFlag(string flagId) => _flags.TryGetValue(flagId, out bool value) && value;
        public void SetObjectState(int stageNumber, string objectId, string stateJson) { }
        public bool TryGetObjectState(int stageNumber, string objectId, out string stateJson)
        {
            stateJson = null;
            return false;
        }
        public void Reset() => _flags.Clear();
    }
}
