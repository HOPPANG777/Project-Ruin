//=============================================================================
// File:        RelicRestorationControllerTests.cs
// Description: 기술 획득·반환과 재건도 계산 규칙을 검증한다.
// History:     07/22/2026 + Created -- Codex
//              07/31/2026 + Technology 데이터 적용 -- Codex
//=============================================================================
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
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
    private RelicRestorationController _controller;
    private GameObject _gameObject;
    private FakeProgressService _progress;
    [SetUp]
    public void SetUp()
    {
        List<TechnologyDefinition> technologies = new List<TechnologyDefinition>
        {
            CreateTechnology(TECHNOLOGY_MOVE, 0, 0),
            CreateTechnology(TECHNOLOGY_POWER, 1, 30),
            CreateTechnology(TECHNOLOGY_CRAFT, 2, 30),
            CreateTechnology(TECHNOLOGY_LIFE, 3, 40)
        };
        List<RestorationStageDefinition> stages = new List<RestorationStageDefinition>
        {
            new RestorationStageDefinition(1, 30),
            new RestorationStageDefinition(2, 60),
            new RestorationStageDefinition(3, 100)
        };
        _gameObject = new GameObject(nameof(RelicRestorationControllerTests));
        _controller = _gameObject.AddComponent<RelicRestorationController>();
        _progress = new FakeProgressService();
        Assert.That(_controller.Init(new FakeCodexDataRepository(technologies, stages), _progress), Is.True);
    }
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameObject);
    }
    [Test]
    public void AcquiredTechnologyIsReturnableUntilReturned()
    {
        List<TechnologyDefinition> buffer = new List<TechnologyDefinition>();
        bool acquired = _controller.TryAcquireTechnology(TECHNOLOGY_POWER, out _);
        int count = _controller.FillReturnableTechnologies(buffer);
        Assert.That(acquired, Is.True);
        Assert.That(count, Is.EqualTo(1));
        Assert.That(buffer[0].TechnologyId, Is.EqualTo(TECHNOLOGY_POWER));
        Assert.That(_controller.TryReturnTechnology(TECHNOLOGY_POWER, out _, out _), Is.True);
        Assert.That(_controller.FillReturnableTechnologies(buffer), Is.Zero);
        Assert.That(_controller.IsAcquired(TECHNOLOGY_POWER), Is.True);
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
    private static TechnologyDefinition CreateTechnology(string technologyId, int chapter, int restoreValue)
    {
        return new TechnologyDefinition(
            technologyId,
            string.Concat("name.", technologyId),
            string.Concat("description.", technologyId),
            string.Concat("operation.", technologyId),
            string.Empty,
            string.Empty,
            chapter,
            restoreValue);
    }
    private sealed class FakeCodexDataRepository : ICodexDataRepository
    {
        private readonly IReadOnlyList<TechnologyDefinition> _technologies;
        private readonly IReadOnlyList<RestorationStageDefinition> _stages;
        public FakeCodexDataRepository(
            IReadOnlyList<TechnologyDefinition> technologies,
            IReadOnlyList<RestorationStageDefinition> stages)
        {
            _technologies = technologies;
            _stages = stages;
        }
        public int MaxRestore => 100;
        public int TechnologyCount => _technologies.Count;
        public int CodexEntryCount => 0;
        public IReadOnlyList<TechnologyDefinition> Technologies => _technologies;
        public IReadOnlyList<RestorationStageDefinition> RestorationStages => _stages;
        public IReadOnlyList<CodexEntryDefinition> CodexEntries => System.Array.Empty<CodexEntryDefinition>();
        public bool TryGetTechnology(string technologyId, out TechnologyDefinition technology)
        {
            for (int i = 0; i < _technologies.Count; i++)
            {
                if (_technologies[i].TechnologyId == technologyId)
                {
                    technology = _technologies[i];
                    return true;
                }
            }
            technology = null;
            return false;
        }
        public bool TryGetCodexEntry(string entryId, out CodexEntryDefinition entry)
        { entry = null; return false; }
        public bool TryGetCodexEntryBySource(CodexCategory category, string sourceId,
            out CodexEntryDefinition entry) { entry = null; return false; }
        public bool TryGetCodexIngredients(string entryId,
            out IReadOnlyList<CodexIngredientDefinition> ingredients)
        { ingredients = null; return false; }
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
        public void SetSystemState(string stateId, string stateJson) { }
        public bool TryGetSystemState(string stateId, out string stateJson)
        {
            stateJson = null;
            return false;
        }
        public void SetObjectState(int stageNumber, string objectId, string stateJson) { }
        public bool TryGetObjectState(int stageNumber, string objectId, out string stateJson)
        {
            stateJson = null;
            return false;
        }
        public void Reset() => _flags.Clear();
    }
}
