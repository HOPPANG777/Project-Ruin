//=============================================================================
// File:        CodexBootstrapPhase8Tests.cs
// Description: 실제 Start·씬 플레이어 교체·손상 ES3·취소 및 초기 UI 차단 검증
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class CodexBootstrapPhase8Tests
{
    private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<GameObject> _created = new List<GameObject>();

    /// <summary>실제 Start와 OnEnable 순서를 검증할 빈 런타임 씬을 준비합니다.</summary>
    [UnitySetUp]
    public IEnumerator EnterRuntime()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    /// <summary>검증 인스턴스만 정리하고 편집 모드로 복귀합니다.</summary>
    [UnityTearDown]
    public IEnumerator ExitRuntime()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
            if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
        yield return new ExitPlayMode();
    }

    /// <summary>자동 Start가 성공한 뒤에만 메뉴가 열리고, 다른 씬의 새 플레이어로 안전하게 연결됩니다.</summary>
    [UnityTest]
    public IEnumerator StartAndSceneReplacementBindWithoutSearching()
    {
        var first = CreatePlayer("First");
        var root = CreateRoot(out string path);
        var navigation = root.GetComponentInChildren<CodexNavigationPresenter>(true);
        var codex = root.GetComponentInChildren<CodexPresenter>(true);
        root.gameObject.SetActive(true);
        navigation.OpenCodex();
        Assert.That(codex.IsOpen, Is.False, "초기화 전 입력은 무시해야 합니다.");
        float deadline = Time.realtimeSinceStartup + 10;
        while (!root.IsInitialized && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(root.IsInitialized, Is.True);
        Assert.That(navigation.IsInitialized, Is.True);
        navigation.OpenCodex();
        Assert.That(codex.IsOpen, Is.True);
        Assert.That(first.GetComponent<PlayerMoveController>().CanMove, Is.False);

        var nextScene = SceneManager.CreateScene("Phase8NextScene");
        var second = CreatePlayer("Second");
        SceneManager.MoveGameObjectToScene(second, nextScene);
        Assert.That(codex.IsOpen, Is.False);
        Assert.That(first.GetComponent<PlayerMoveController>().CanMove, Is.True);
        yield return null;
        navigation.OpenCodex();
        Assert.That(second.GetComponent<PlayerMoveController>().CanMove, Is.False);
        first.SetActive(false);
        Assert.That(second.GetComponent<PlayerMoveController>().CanMove, Is.False,
            "이전 플레이어의 늦은 해제가 새 연결을 지우면 안 됩니다.");
        yield return SceneManager.UnloadSceneAsync(nextScene);
        Assert.That(codex.IsOpen, Is.False);
        Assert.That(Get(navigation, "_playerInteractionController"), Is.Null);
        var third = CreatePlayer("Third");
        yield return null;
        navigation.OpenCodex();
        Assert.That(third.GetComponent<PlayerMoveController>().CanMove, Is.False);
        navigation.enabled = false;
        Assert.That(third.GetComponent<PlayerMoveController>().CanMove, Is.True);
        yield return null;
        navigation.OpenCodex();
        Assert.That(codex.IsOpen, Is.False, "비활성 Presenter는 버튼 호출로 다시 열리면 안 됩니다.");
        navigation.enabled = true;
        yield return null;
        navigation.OpenCodex();
        Assert.That(codex.IsOpen, Is.True);
        typeof(CodexNavigationPresenter).GetField("_inventoryBridge", PRIVATE).SetValue(navigation, null);
        Assert.That(navigation.TryInitialize(root.GameData.CodexData, root.Codex,
            root.Inventory, root.Localization), Is.False);
        Assert.That(codex.IsOpen, Is.False);
        Assert.That(third.GetComponent<PlayerMoveController>().CanMove, Is.True);
        Assert.That(root.Progress.IsDirty || File.Exists(path), Is.True);
    }

    /// <summary>손상된 독립 ES3 파일은 원문을 보존하고 UI·박물관 초기화를 중단합니다.</summary>
    [UnityTest]
    public IEnumerator MalformedSaveStopsActualStartWithoutOverwritingFile()
    {
        var root = CreateRoot(out string path);
        ES3.SaveRaw("{invalid phase8 save", path);
        byte[] before = File.ReadAllBytes(path);
        LogAssert.Expect(LogType.Error, new Regex(@"\[ProgressController\] 불러오기에 실패했습니다\."));
        LogAssert.Expect(LogType.Error, "[GameRoot] 진행 상태 불러오기에 실패하여 초기화를 중단합니다.");
        root.gameObject.SetActive(true);
        var progress = root.GetComponentInChildren<ProgressController>(true);
        // 실패 로그가 처리될 때까지 최소 한 프레임 및 진행 중 I/O를 기다립니다.
        yield return null;
        float deadline = Time.realtimeSinceStartup + 10;
        while ((bool)Get(progress, "_isIoRunning") && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That((bool)Get(progress, "_isIoRunning"), Is.False);
        Assert.That(root.IsInitialized, Is.False);
        Assert.That(progress.IsDirty, Is.False);
        Assert.That(progress.TryGetSystemState(MuseumRelicDisplayController.STATE_ID, out _), Is.False);
        var navigation = root.GetComponentInChildren<CodexNavigationPresenter>(true);
        navigation.OpenCodex();
        Assert.That(navigation.IsInitialized, Is.False);
        Assert.That(root.GetComponentInChildren<CodexPresenter>(true).IsOpen, Is.False);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
    }

    /// <summary>취소된 초기화는 상태·파일을 생성하지 않고 조용히 중단됩니다.</summary>
    [UnityTest]
    public IEnumerator CancelledInitializationDoesNotWriteProgress()
    {
        var root = CreateRoot(out string path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var initialize = (UniTask)typeof(GameRoot).GetMethod("InitializeAsync", PRIVATE)
            .Invoke(root, new object[] { cancellation.Token });
        yield return initialize.ToCoroutine();
        Assert.That(root.IsInitialized, Is.False);
        Assert.That(root.Progress.IsDirty, Is.False);
        Assert.That(File.Exists(path), Is.False);
        Assert.That(root.GetComponentInChildren<CodexNavigationPresenter>(true).IsInitialized, Is.False);
    }

    private GameRoot CreateRoot(out string path)
    {
        // Awake 전에 테스트 경로를 지정합니다. 기본 플레이어 저장을 읽지 않습니다.
        var staging = new GameObject("InactiveStaging");
        staging.SetActive(false);
        _created.Add(staging);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MuseumPhase8EditorValidator.ROOT_PATH);
        var instance = Object.Instantiate(prefab, staging.transform);
        instance.SetActive(false);
        instance.transform.SetParent(null);
        _created.Add(instance);
        string directory = Path.GetFullPath("Logs/Phase8Validation");
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "bootstrap-" + Guid.NewGuid().ToString("N") + ".es3");
        var progress = instance.GetComponentInChildren<ProgressController>(true);
        typeof(ProgressController).GetField("_saveFilePath", PRIVATE).SetValue(progress, path);
        return instance.GetComponent<GameRoot>();
    }

    private GameObject CreatePlayer(string name)
    {
        var player = new GameObject(name);
        _created.Add(player);
        player.AddComponent<PlayerMoveController>();
        player.AddComponent<PlayerInteractionController>();
        player.AddComponent<UnitPlayer>();
        return player;
    }

    private static object Get(object target, string field) =>
        target.GetType().GetField(field, PRIVATE).GetValue(target);
}
