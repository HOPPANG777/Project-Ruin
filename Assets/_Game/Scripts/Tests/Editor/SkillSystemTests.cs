//=============================================================================
// File:        SkillSystemTests.cs
// Description: 환경 스킬의 등록, 획득, 중복 방지와 Progress 연동을 검증한다.
// History:     07/31/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

//=============================================================================
// Synopsis:   SkillSystemTests;
//                - 환경 스킬 획득 상태와 Technology 저장 경로 분리 검증
//=============================================================================
[TestFixture]
public sealed class SkillSystemTests
{
    private const string SKILL_PUSH = "SKILL_PUSH";
    private const string SKILL_BREAK = "SKILL_BREAK";

    private readonly List<Object> _createdObjects = new List<Object>();
    private SkillController _controller;
    private FakeProgressService _progress;

    [SetUp]
    public void SetUp()
    {
        SkillDataSO pushSkill = CreateSkill(SKILL_PUSH);
        SkillDataSO breakSkill = CreateSkill(SKILL_BREAK);
        GameObject gameObject = new GameObject(nameof(SkillSystemTests));
        _createdObjects.Add(gameObject);
        _controller = gameObject.AddComponent<SkillController>();
        SetField(_controller, "_skills", new[] { pushSkill, breakSkill });
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
    public void RegisteredSkillCanBeAcquired()
    {
        bool acquired = _controller.TryAcquireSkill(SKILL_PUSH, out SkillAcquireFailure failure);

        Assert.That(acquired, Is.True);
        Assert.That(failure, Is.EqualTo(SkillAcquireFailure.SKILL_FAILURE_NONE));
        Assert.That(_controller.IsAcquired(SKILL_PUSH), Is.True);
    }

    [Test]
    public void SkillCannotBeAcquiredTwice()
    {
        Assert.That(_controller.TryAcquireSkill(SKILL_PUSH, out _), Is.True);

        bool acquired = _controller.TryAcquireSkill(SKILL_PUSH, out SkillAcquireFailure failure);

        Assert.That(acquired, Is.False);
        Assert.That(failure, Is.EqualTo(SkillAcquireFailure.SKILL_FAILURE_ALREADY_ACQUIRED));
        Assert.That(_progress.SetFlagCallCount, Is.EqualTo(1));
    }

    [TestCase("", SkillAcquireFailure.SKILL_FAILURE_INVALID_ID)]
    [TestCase("UNKNOWN", SkillAcquireFailure.SKILL_FAILURE_NOT_FOUND)]
    public void InvalidSkillIdIsRejected(string skillId, SkillAcquireFailure expected)
    {
        bool acquired = _controller.TryAcquireSkill(skillId, out SkillAcquireFailure failure);

        Assert.That(acquired, Is.False);
        Assert.That(failure, Is.EqualTo(expected));
        Assert.That(_progress.SetFlagCallCount, Is.Zero);
    }

    [Test]
    public void ProgressFlagIsReflectedByAcquiredList()
    {
        string flagId = string.Concat(ISkillService.SKILL_ACQUIRED_FLAG_PREFIX, SKILL_BREAK);
        _progress.SetFlag(flagId, true);
        List<SkillDataSO> buffer = new List<SkillDataSO>();

        int count = _controller.FillAcquiredSkills(buffer);

        Assert.That(_controller.IsAcquired(SKILL_BREAK), Is.True);
        Assert.That(count, Is.EqualTo(1));
        Assert.That(buffer[0].SkillId, Is.EqualTo(SKILL_BREAK));
    }

    [Test]
    public void TechnologyFlagDoesNotAcquireEnvironmentSkill()
    {
        string technologyFlagId = string.Concat(
            IRelicRestorationService.TECHNOLOGY_ACQUIRED_FLAG_PREFIX, SKILL_PUSH);
        _progress.SetFlag(technologyFlagId, true);

        Assert.That(ISkillService.SKILL_ACQUIRED_FLAG_PREFIX,
            Is.Not.EqualTo(IRelicRestorationService.TECHNOLOGY_ACQUIRED_FLAG_PREFIX));
        Assert.That(_controller.IsAcquired(SKILL_PUSH), Is.False);
    }

    [Test]
    public void RegisteredSkillCanBeFoundById()
    {
        bool found = _controller.TryGetSkill(SKILL_PUSH, out SkillDataSO skill);

        Assert.That(found, Is.True);
        Assert.That(skill.SkillId, Is.EqualTo(SKILL_PUSH));
    }

    private SkillDataSO CreateSkill(string skillId)
    {
        SkillDataSO skill = ScriptableObject.CreateInstance<SkillDataSO>();
        _createdObjects.Add(skill);
        SetField(skill, "_skillId", skillId);
        return skill;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private sealed class FakeProgressService : IProgressService
    {
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();
        internal int SetFlagCallCount { get; private set; }
        public bool IsDirty => false;

        public UniTask<bool> LoadAsync(CancellationToken cancellationToken = default) =>
            UniTask.FromResult(true);
        public UniTask<bool> SaveAsync(CancellationToken cancellationToken = default) =>
            UniTask.FromResult(true);

        public void SetFlag(string flagId, bool value)
        {
            SetFlagCallCount++;
            _flags[flagId] = value;
        }

        public bool GetFlag(string flagId) =>
            _flags.TryGetValue(flagId, out bool value) && value;
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
