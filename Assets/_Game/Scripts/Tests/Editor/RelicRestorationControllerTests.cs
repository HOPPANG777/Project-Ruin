//=============================================================================
// File:        RelicRestorationControllerTests.cs
//
// Description: 기술 획득·반납과 재건도 계산 규칙을 검증한다.
//
// History:     07/22/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;

//=============================================================================
//
// Synopsis:   RelicRestorationControllerTests;
//                - 획득·반납 조건 검증
//                - 재건도와 복원 단계 검증
//                - 순수 로직 300줄 제한 검증
//
//=============================================================================
[TestFixture]
public sealed class RelicRestorationControllerTests
{
    //========================================
    // 상수
    //========================================

    private const string SKILL_TELEPORT = "SKILL_TELEPORT";
    private const string SKILL_TELEKINESIS = "SKILL_TELEKINESIS";
    private const string SKILL_CARVING = "SKILL_CARVING";
    private const string SKILL_REVIVAL = "SKILL_REVIVAL";
    private const int LOGIC_LINE_LIMIT = 300;

    //========================================
    // 내부 상태
    //========================================

    private readonly List<Object> _createdObjects = new List<Object>();
    private readonly List<string> _saveFiles = new List<string>();
    private RelicRestorationController _controller;
    private RelicRestorationCatalogSO _catalog;
    private FakeProgressService _progress;

    //******************************************************************************
    //
    // Test Lifecycle
    //
    //******************************************************************************

    /// <summary>각 테스트에서 사용할 유효한 기술과 Catalog를 생성합니다.</summary>
    [SetUp]
    public void SetUp()
    {
        SkillDataSO teleport = CreateSkill(SKILL_TELEPORT, 0, 0);
        SkillDataSO telekinesis = CreateSkill(SKILL_TELEKINESIS, 1, 30);
        SkillDataSO carving = CreateSkill(SKILL_CARVING, 2, 30);
        SkillDataSO revival = CreateSkill(SKILL_REVIVAL, 3, 40);
        TimelineAsset timeline = CreateObject<TimelineAsset>();
        _catalog = CreateObject<RelicRestorationCatalogSO>();

        SetField(_catalog, "_skills", new List<SkillDataSO>
        {
            teleport,
            telekinesis,
            carving,
            revival
        });
        SetField(_catalog, "_stages", new List<RelicRestorationCatalogSO.RestorationStageEntry>
        {
            CreateStage(1, 30, timeline),
            CreateStage(2, 60, timeline),
            CreateStage(3, 100, timeline)
        });
        SetField(_catalog, "_altarDestinations", new List<RelicRestorationCatalogSO.AltarDestinationEntry>
        {
            CreateAltar()
        });

        GameObject gameObject = new GameObject("RelicRestorationControllerTests");
        _createdObjects.Add(gameObject);
        _controller = gameObject.AddComponent<RelicRestorationController>();
        SetField(_controller, "_catalog", _catalog);

        _progress = new FakeProgressService();
        Assert.That(_controller.Init(_progress), Is.True);
    }

    /// <summary>테스트에서 생성한 Unity 오브젝트를 정리합니다.</summary>
    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
        {
            Object.DestroyImmediate(_createdObjects[i]);
        }

        _createdObjects.Clear();

        for (int i = 0; i < _saveFiles.Count; i++)
        {
            if (ES3.FileExists(_saveFiles[i]))
            {
                ES3.DeleteFile(_saveFiles[i]);
            }
        }

        _saveFiles.Clear();
    }

    //******************************************************************************
    //
    // Domain Tests
    //
    //******************************************************************************

    /// <summary>획득한 기술만 반납 가능 목록에 포함되는지 검증합니다.</summary>
    [Test]
    public void AcquiredSkillBecomesReturnable()
    {
        List<SkillDataSO> buffer = new List<SkillDataSO>();

        bool acquired = _controller.TryAcquireSkill(SKILL_TELEKINESIS, out RelicRestorationFailure failure);
        int count = _controller.FillReturnableSkills(buffer);

        Assert.That(acquired, Is.True);
        Assert.That(failure, Is.EqualTo(RelicRestorationFailure.RESTORATION_FAILURE_NONE));
        Assert.That(count, Is.EqualTo(1));
        Assert.That(buffer[0].SkillId, Is.EqualTo(SKILL_TELEKINESIS));
    }

    /// <summary>퀘스트 기술 보상이 Progress 직접 변경 없이 재건 서비스로 전달되는지 검증합니다.</summary>
    [Test]
    public void QuestSkillRewardUsesRestorationService()
    {
        QuestController questController = new GameObject("QuestSkillRewardTests").AddComponent<QuestController>();
        _createdObjects.Add(questController.gameObject);
        SetField(questController, "_relicRestorationService", _controller);
        string flagId = string.Concat(IRelicRestorationService.SKILL_ACQUIRED_FLAG_PREFIX, SKILL_TELEKINESIS);
        QuestRewardDefinition reward = new QuestRewardDefinition("QUEST", 0, "REWARD", "reward.skill", flagId, true, string.Empty, null);
        MethodInfo method = typeof(QuestController).GetMethod("TryApplyProgressReward", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That((bool)method.Invoke(questController, new object[] { reward }), Is.True);
        Assert.That(_controller.IsAcquired(SKILL_TELEKINESIS), Is.True);
    }

    /// <summary>획득하지 않은 기술은 반납할 수 없는지 검증합니다.</summary>
    [Test]
    public void UnacquiredSkillCannotBeReturned()
    {
        bool returned = _controller.TryReturnSkill(
            SKILL_TELEKINESIS,
            out _,
            out RelicRestorationFailure failure);

        Assert.That(returned, Is.False);
        Assert.That(failure, Is.EqualTo(RelicRestorationFailure.RESTORATION_FAILURE_SKILL_NOT_ACQUIRED));
        Assert.That(_controller.CurrentRestore, Is.Zero);
    }

    /// <summary>같은 기술은 한 번만 반납되고 재건도가 중복 증가하지 않는지 검증합니다.</summary>
    [Test]
    public void ReturnedSkillCannotBeReturnedTwice()
    {
        _controller.TryAcquireSkill(SKILL_TELEKINESIS, out _);

        bool firstReturn = _controller.TryReturnSkill(SKILL_TELEKINESIS, out _, out _);
        bool secondReturn = _controller.TryReturnSkill(
            SKILL_TELEKINESIS,
            out _,
            out RelicRestorationFailure failure);

        Assert.That(firstReturn, Is.True);
        Assert.That(secondReturn, Is.False);
        Assert.That(failure, Is.EqualTo(RelicRestorationFailure.RESTORATION_FAILURE_SKILL_ALREADY_RETURNED));
        Assert.That(_controller.CurrentRestore, Is.EqualTo(30));
        Assert.That(_controller.RestoreStage, Is.EqualTo(1));
    }

    /// <summary>챕터 0 기술 반납은 재건도를 올리지 않는지 검증합니다.</summary>
    [Test]
    public void TeleportSkillDoesNotIncreaseRestore()
    {
        _controller.TryAcquireSkill(SKILL_TELEPORT, out _);
        bool returned = _controller.TryReturnSkill(SKILL_TELEPORT, out RelicRestorationResult result, out _);

        Assert.That(returned, Is.True);
        Assert.That(result.CurrentRestore, Is.Zero);
        Assert.That(result.CurrentStage, Is.Zero);
        Assert.That(result.StageChanged, Is.False);
    }

    /// <summary>세 복원 기술 반납 후 재건도 100과 엔딩 조건을 달성하는지 검증합니다.</summary>
    [Test]
    public void ThreeRestorationSkillsReachEnding()
    {
        ReturnSkill(SKILL_TELEKINESIS);
        ReturnSkill(SKILL_CARVING);

        _controller.TryAcquireSkill(SKILL_REVIVAL, out _);
        bool returned = _controller.TryReturnSkill(SKILL_REVIVAL, out RelicRestorationResult result, out _);

        Assert.That(returned, Is.True);
        Assert.That(result.PreviousRestore, Is.EqualTo(60));
        Assert.That(result.CurrentRestore, Is.EqualTo(100));
        Assert.That(result.CurrentStage, Is.EqualTo(3));
        Assert.That(result.ShouldPlayEnding, Is.True);
    }

    /// <summary>ES3에 저장한 반납 상태가 새 컨트롤러에서 재건도 30으로 복원되는지 검증합니다.</summary>
    [Test]
    public async Task SavedReturnStateRestoresThirtyPercent()
    {
        string saveFilePath = $"RelicRestorationTest_{System.Guid.NewGuid():N}.es3";
        _saveFiles.Add(saveFilePath);
        ProgressController writer = CreateProgressController(saveFilePath);

        Assert.That(await writer.LoadAsync(), Is.True);
        writer.SetFlag(string.Concat(IRelicRestorationService.SKILL_ACQUIRED_FLAG_PREFIX, SKILL_TELEKINESIS), true);
        writer.SetFlag(string.Concat(IRelicRestorationService.SKILL_RETURNED_FLAG_PREFIX, SKILL_TELEKINESIS), true);
        Assert.That(writer.IsDirty, Is.True);
        Assert.That(await writer.SaveAsync(), Is.True);
        Assert.That(writer.IsDirty, Is.False);
        writer.SetFlag(string.Concat(IRelicRestorationService.SKILL_RETURNED_FLAG_PREFIX, SKILL_TELEKINESIS), true);
        Assert.That(writer.IsDirty, Is.False);

        ProgressController reader = CreateProgressController(saveFilePath);
        Assert.That(await reader.LoadAsync(), Is.True);
        RelicRestorationController restoredController = CreateRestorationController(reader);

        Assert.That(restoredController.IsReturned(SKILL_TELEKINESIS), Is.True);
        Assert.That(restoredController.CurrentRestore, Is.EqualTo(30));
        Assert.That(restoredController.RestoreStage, Is.EqualTo(1));
    }

    /// <summary>재건 관련 C# 파일이 순수 로직 300줄을 넘지 않는지 검증합니다.</summary>
    [TestCase("Assets/_Game/Scripts/Gameplay/Skills/Data/SkillDataSO.cs")]
    [TestCase("Assets/_Game/Scripts/Gameplay/Relics/RelicRestorationCatalogSO.cs")]
    [TestCase("Assets/_Game/Scripts/Gameplay/Relics/IRelicRestorationService.cs")]
    [TestCase("Assets/_Game/Scripts/Gameplay/Relics/RelicRestorationController.cs")]
    [TestCase("Assets/_Game/Scripts/Gameplay/Relics/AltarInteractable.cs")]
    [TestCase("Assets/_Game/Scripts/Presentation/UI/Relics/AltarPresenter.cs")]
    [TestCase("Assets/_Game/Scripts/Systems/Progress/Contracts/IProgressService.cs")]
    [TestCase("Assets/_Game/Scripts/Systems/Progress/ProgressController.cs")]
    [TestCase("Assets/_Game/Scripts/Systems/SceneFlow/StageFlowController.cs")]
    [TestCase("Assets/_Game/Scripts/Core/Bootstrap/GameRoot.cs")]
    [TestCase("Assets/_Game/Scripts/Gameplay/Quests/QuestController.cs")]
    public void ProductionScriptStaysUnderLogicLineLimit(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string fullPath = Path.Combine(projectRoot, relativePath);
        int logicLineCount = CountLogicLines(File.ReadAllLines(fullPath));

        Assert.That(logicLineCount, Is.LessThanOrEqualTo(LOGIC_LINE_LIMIT), relativePath);
    }

    //******************************************************************************
    //
    // Test Helpers
    //
    //******************************************************************************

    private void ReturnSkill(string skillId)
    {
        Assert.That(_controller.TryAcquireSkill(skillId, out _), Is.True);
        Assert.That(_controller.TryReturnSkill(skillId, out _, out _), Is.True);
    }

    private ProgressController CreateProgressController(string saveFilePath)
    {
        GameObject gameObject = new GameObject("ProgressControllerTests");
        _createdObjects.Add(gameObject);
        ProgressController controller = gameObject.AddComponent<ProgressController>();
        SetField(controller, "_saveFilePath", saveFilePath);
        return controller;
    }

    private RelicRestorationController CreateRestorationController(IProgressService progressService)
    {
        GameObject gameObject = new GameObject("RestoredRelicControllerTests");
        _createdObjects.Add(gameObject);
        RelicRestorationController controller = gameObject.AddComponent<RelicRestorationController>();
        SetField(controller, "_catalog", _catalog);
        Assert.That(controller.Init(progressService), Is.True);
        return controller;
    }

    private SkillDataSO CreateSkill(string skillId, int chapter, int restoreValue)
    {
        SkillDataSO skill = CreateObject<SkillDataSO>();
        SetField(skill, "_skillId", skillId);
        SetField(skill, "_displayNameKey", string.Concat("name.", skillId));
        SetField(skill, "_descriptionKey", string.Concat("description.", skillId));
        SetField(skill, "_chapter", chapter);
        SetField(skill, "_restoreValue", restoreValue);
        return skill;
    }

    private static RelicRestorationCatalogSO.RestorationStageEntry CreateStage(
        int stage,
        int requiredRestore,
        TimelineAsset timeline)
    {
        RelicRestorationCatalogSO.RestorationStageEntry entry =
            new RelicRestorationCatalogSO.RestorationStageEntry();
        SetField(entry, "_stage", stage);
        SetField(entry, "_requiredRestore", requiredRestore);
        SetField(entry, "_timeline", timeline);
        return entry;
    }

    private static RelicRestorationCatalogSO.AltarDestinationEntry CreateAltar()
    {
        RelicRestorationCatalogSO.AltarDestinationEntry entry =
            new RelicRestorationCatalogSO.AltarDestinationEntry();
        SetField(entry, "_altarId", "MAIN_ALTAR");
        SetField(entry, "_displayNameKey", "altar.main");
        SetField(entry, "_stageNumber", 0);
        SetField(entry, "_spawnPointId", "SPAWN_MAIN_ALTAR");
        SetField(entry, "_isMainAltar", true);
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

    private static int CountLogicLines(string[] lines)
    {
        int count = 0;
        bool inBlockComment = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (line.StartsWith("/*"))
            {
                inBlockComment = true;
            }

            if (!inBlockComment
                && line.Length > 0
                && !line.StartsWith("//")
                && !line.StartsWith("using ")
                && !line.StartsWith("#"))
            {
                count++;
            }

            if (line.EndsWith("*/"))
            {
                inBlockComment = false;
            }
        }

        return count;
    }

    private sealed class FakeProgressService : IProgressService
    {
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();

        public bool IsDirty => false;

        public UniTask<bool> LoadAsync(CancellationToken cancellationToken = default) =>
            UniTask.FromResult(true);

        public UniTask<bool> SaveAsync(CancellationToken cancellationToken = default) =>
            UniTask.FromResult(true);

        public void SetFlag(string flagId, bool value)
        {
            _flags[flagId] = value;
        }

        public bool GetFlag(string flagId)
        {
            return _flags.TryGetValue(flagId, out bool value) && value;
        }

        public void SetObjectState(int stageNumber, string objectId, string stateJson)
        {
        }

        public bool TryGetObjectState(int stageNumber, string objectId, out string stateJson)
        {
            stateJson = null;
            return false;
        }

        public void Reset()
        {
            _flags.Clear();
        }
    }
}
