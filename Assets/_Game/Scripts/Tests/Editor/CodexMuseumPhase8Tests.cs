//=============================================================================
// File:        CodexMuseumPhase8Tests.cs
// Description: 실제 GameRoot 초기화·독립 ES3 저장과 반복 조회 할당·원본 참조 회귀 검사
// History:     08/31/2026 + Created -- Codex
//              09/23/2026 + Bootstrap Module 직렬화와 Nested Prefab 검증 반영 -- Codex
//=============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class CodexMuseumPhase8Tests
{
    private const string ITEM = "ITEM_PHASE8_RELIC";
    private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> _created = new List<Object>();

    /// <summary>테스트 인스턴스만 파괴하며 원본 에셋이나 사용자 저장을 변경하지 않습니다.</summary>
    [TearDown]
    public void TearDown()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
            if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
    }

    /// <summary>실제 프리팹 연결은 통과하고 인스턴스의 누락·중복은 차단해야 합니다.</summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void RootValidationRejectsMissingAndDuplicateControllers(int fault)
    {
        var root = CreateRoot();
        if (fault == 1)
        {
            var serializedRoot = new SerializedObject(root);
            serializedRoot.FindProperty("_gameplayModule._codexController").objectReferenceValue = null;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }
        if (fault == 3) root.gameObject.SetActive(false);
        if (fault == 4) Set(root.GetComponentInChildren<CodexNavigationPresenter>(true), "_inventoryBridge", null);
        if (fault == 2)
        {
            var duplicate = new GameObject("Duplicate");
            duplicate.transform.SetParent(root.transform);
            duplicate.AddComponent<CodexController>();
        }
        Assert.That(MuseumPhase8EditorValidator.TryValidateRoot(root.gameObject, out string message),
            Is.EqualTo(fault == 0), message);
    }

    /// <summary>Preview 로드가 숨길 수 있는 누락 참조를 저장하지 않고 원문에서 검출합니다.</summary>
    [TestCase("8", true)]
    [TestCase("9", false)]
    [TestCase("0", false)]
    public void OriginalYamlDetectsDanglingComponent(string id, bool expected)
    {
        string yaml = "--- !u!1 &7\nGameObject:\n  m_Component:\n  - component: {fileID: "
            + id + "}\n--- !u!4 &8\nTransform:\n  m_GameObject: {fileID: 7}\n";
        Assert.That(MuseumPhase8EditorValidator.TryValidateComponentReferences(yaml, out _),
            Is.EqualTo(expected));
    }

    /// <summary>기존 보유만 저장된 파일을 실제 InitializeAsync로 읽고 발견·문 개방 순서를 확인합니다.</summary>
    [UnityTest]
    public IEnumerator RealGameRootLoadsLegacyInventoryBeforeCodexAndRewardRoom()
    {
        string directory = Path.GetFullPath("Logs/Phase8Validation");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "root-" + Guid.NewGuid().ToString("N") + ".es3");
        var root = CreateRoot();
        ConfigureRuntimeCsv(root);
        var progress = root.GetComponentInChildren<ProgressController>(true);
        Set(progress, "_saveFilePath", path);
        yield return progress.LoadAsync().ToCoroutine(loaded => Assert.That(loaded, Is.True));
        var data = root.GetComponentInChildren<GameDataController>(true);
        Assert.That(data.TryInit(out var error), Is.True, error?.ToString());
        var inventory = root.GetComponentInChildren<InventoryController>(true);
        inventory.Init(data.Repository, data, progress);
        Assert.That(inventory.TryAddItem(ITEM, 1), Is.True);
        Assert.That(progress.GetFlag(ICodexService.ITEM_DISCOVERY_FLAG_PREFIX + ITEM), Is.False);
        yield return progress.SaveAsync().ToCoroutine(saved => Assert.That(saved, Is.True));
        Object.DestroyImmediate(root.gameObject);

        // 같은 프리팹의 새 인스턴스에서 실제 GameRoot 초기화 본문을 호출합니다.
        root = CreateRoot();
        ConfigureRuntimeCsv(root);
        progress = root.GetComponentInChildren<ProgressController>(true);
        Set(progress, "_saveFilePath", path);
        var initialize = (UniTask)typeof(GameRoot).GetMethod("InitializeAsync", PRIVATE)
            .Invoke(root, new object[] { CancellationToken.None });
        yield return initialize.ToCoroutine();
        Assert.That(root.Inventory.GetItemCount(ITEM), Is.EqualTo(1));
        Assert.That(root.Codex.IsItemDiscovered(ITEM), Is.True);
        Assert.That(root.MuseumRewardRoom.IsOpen, Is.True);
        Assert.That(root.MuseumRelicDisplays.DisplayedRelicCount, Is.Zero);
        var presenter = root.GetComponentInChildren<MuseumRelicPresenter>(true);
        Assert.That(typeof(MuseumRelicPresenter).GetField("_inventoryPresenter", PRIVATE).GetValue(presenter),
            Is.SameAs(root.GetComponentInChildren<InventoryPresenter>(true)));
        bool savedLatest = false;
        float deadline = Time.realtimeSinceStartup + 10;
        while (!savedLatest && Time.realtimeSinceStartup < deadline)
        {
            yield return progress.SaveAsync().ToCoroutine(saved => savedLatest = saved);
            if (!savedLatest) yield return null;
        }
        Assert.That(savedLatest, Is.True);
        Assert.That(progress.IsDirty, Is.False);
        Assert.That(File.Exists(path), Is.True);
    }

    /// <summary>LoadAsync의 실패 반환 후 진행 상태를 수정하거나 후속 시스템을 초기화하지 않습니다.</summary>
    [UnityTest]
    public IEnumerator FailedLoadStopsBeforeMuseumStateWrites()
    {
        var root = CreateRoot();
        var progress = root.GetComponentInChildren<ProgressController>(true);
        Set(progress, "_isIoRunning", true);
        LogAssert.Expect(LogType.Error,
            "[GameBootstrapper] 진행 상태 불러오기에 실패하여 초기화를 중단합니다.");
        var initialize = (UniTask)typeof(GameRoot).GetMethod("InitializeAsync", PRIVATE)
            .Invoke(root, new object[] { CancellationToken.None });
        yield return initialize.ToCoroutine();
        Assert.That(progress.IsDirty, Is.False);
        Assert.That(progress.TryGetSystemState(MuseumRelicDisplayController.STATE_ID, out _), Is.False);
    }

    /// <summary>초기화·워밍업을 제외한 발견 조회와 호출자 버퍼 재사용을 측정합니다.</summary>
    [Test]
    public void WarmDiscoveryAndPlaceableQueriesAllocateNoManagedMemory()
    {
        var go = new GameObject("AllocationTest");
        _created.Add(go);
        var data = new MuseumRelicTestData();
        for (int i = 0; i < 20; i++) data.AddRelic(ITEM + i, i);
        var progress = new MuseumBottleTestProgress();
        var inventory = go.AddComponent<InventoryController>();
        inventory.Init(data, null, progress);
        string itemId = ITEM + "0";
        Assert.That(inventory.TryAddItem(itemId, 1), Is.True);
        var codex = go.AddComponent<CodexController>();
        Assert.That(codex.Init(data, inventory, progress, null), Is.True);
        Assert.That(MuseumRelicMilestoneRepository.TryCreate(
            Array.Empty<MuseumRelicMilestoneDefinition>(), data, out var milestones, out _), Is.True);
        var displays = go.AddComponent<MuseumRelicDisplayController>();
        Assert.That(displays.Init(milestones, data, inventory, progress), Is.True);
        var buffer = new List<string>(20);
        for (int i = 0; i < 100; i++)
        {
            codex.IsItemDiscovered(itemId);
            displays.FillPlaceableRelics(buffer);
        }
        int hits = 0;
        using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 10000,
            ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
        GC.KeepAlive(new byte[4096]);
        recorder.Stop();
        Assert.That(recorder.Count, Is.GreaterThan(0), "실제 할당을 감지한 계측기만 사용합니다.");
        TestContext.WriteLine($"[Phase8] GC.Alloc unit={recorder.UnitType}, control samples={recorder.Count}");
        recorder.Reset();
        recorder.Start();
        for (int i = 0; i < 1000; i++)
        {
            if (codex.IsItemDiscovered(itemId)) hits++;
            displays.FillPlaceableRelics(buffer);
        }
        recorder.Stop();
        int allocationSamples = recorder.Count;
        TestContext.WriteLine($"[Phase8] warmed discovery + 20-item buffer, 1000 iterations: {allocationSamples} GC.Alloc samples");
        Assert.That(hits, Is.EqualTo(1000));
        Assert.That(buffer.Count, Is.EqualTo(1));
        Assert.That(allocationSamples, Is.Zero);
        progress.SetFlag(ICodexService.ITEM_DISCOVERY_FLAG_PREFIX + itemId, false);
        Assert.That(codex.IsItemDiscovered(itemId), Is.False, "키 캐시는 Progress 상태를 복제하지 않아야 합니다.");
        Assert.That(codex.Init(new MuseumRelicTestData(), inventory, progress, null), Is.True);
        Assert.That(codex.IsItemDiscovered(itemId), Is.False, "재초기화는 이전 정의의 키를 비워야 합니다.");
    }

    /// <summary>금지 API 검사 자체가 실패 경로를 탐지하는지 검증합니다.</summary>
    [TestCase("Object.FindFirstObjectByType<PlayerMoveController>();", true)]
    [TestCase("GameObject.Find(\"Player\");", true)]
    [TestCase("root.BindMenuPlayer(interaction, movement);", false)]
    public void RuntimeRuleDetectsForbiddenCalls(string source, bool expected) =>
        Assert.That(MuseumPhase8EditorValidator.HasForbiddenRuntimeCall(source), Is.EqualTo(expected));

    // 실제 프리팹은 비활성 부모 아래 복제하여 Start 자동 실행과 기본 세이브 접근을 막습니다.
    private GameRoot CreateRoot()
    {
        var parent = new GameObject("Phase8IsolatedRoot");
        parent.SetActive(false);
        _created.Add(parent);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MuseumPhase8EditorValidator.ROOT_PATH);
        return Object.Instantiate(prefab, parent.transform).GetComponent<GameRoot>();
    }

    private void ConfigureRuntimeCsv(GameRoot root)
    {
        var data = root.GetComponentInChildren<GameDataController>(true);
        var source = (TextAsset)typeof(GameDataController).GetField("_itemsCsv", PRIVATE).GetValue(data);
        Set(data, "_itemsCsv", Csv(source.text.TrimEnd() + "\n"
            + ITEM + ",item.coin.name,item.coin.description,,0,false,ITEM_RELIC_COLLECTIBLE,99\n"));
        Set(data, "_codexEntriesCsv", Csv("entry_id,category,display_order,source_id,chapter\n"
            + "CODEX_PHASE8_RELIC,RELIC,0," + ITEM + ",1\n"));
    }

    private TextAsset Csv(string text)
    {
        var csv = new TextAsset(text);
        _created.Add(csv);
        return csv;
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, PRIVATE).SetValue(target, value);
}
