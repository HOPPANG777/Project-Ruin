//=============================================================================
// File:        StageFlowSceneUnloadTests.cs
// Description: 스테이지 씬 언로드, 플레이어 이관과 목적지 스폰 배치를 검증합니다.
// History:     10/11/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>실제 씬 API를 사용하되 GameRoot와 사용자 저장 파일에는 접근하지 않습니다.</summary>
public sealed class StageFlowSceneUnloadTests
{
    private GameObject _services;
    private StageFlowController _flow;
    private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        _services = new GameObject("SceneFlowTestServices");
        Object.DontDestroyOnLoad(_services);
        _flow = _services.AddComponent<StageFlowController>();
        typeof(StageFlowController).GetField("_sceneTransition", PRIVATE)
            .SetValue(_flow, new SceneOperationMonitor());
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_services != null) Object.DestroyImmediate(_services);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator PlayerKeepsHealthAndVisualsWhileMapUnloadsAndMovesToDestinationSpawn()
    {
        Scene source = SceneManager.GetActiveScene();
        GameObject oldMap = new GameObject("OldSceneMap");
        UnitPlayer player = new GameObject("TransferredPlayer").AddComponent<UnitPlayer>();
        player.transform.SetParent(oldMap.transform);
        player.transform.position = new Vector3(4f, 2f, 0f);
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform);
        Rigidbody2D body = player.gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        typeof(UnitBase).GetField("_maxHealth", PRIVATE).SetValue(player, 3f);
        player.TryRestoreFullHealth();
        player.TakeDamage(1f);
        Assert.That(SceneManager.sceneCount, Is.EqualTo(1));

        GameObject transferred = (GameObject)Invoke("PreserveStagePlayer", source);
        Assert.That(transferred, Is.SameAs(player.gameObject));
        Assert.That(player.transform.parent, Is.Null);
        Assert.That(player.transform.position, Is.EqualTo(new Vector3(4f, 2f, 0f)));
        Scene temporary = (Scene)Invoke("CreateTransitionSceneIfNeeded", source);
        Assert.That(temporary.IsValid() && temporary.isLoaded, Is.True);
        yield return ((UniTask)Invoke("UnloadSceneIfNeededAsync", source, CancellationToken.None))
            .Timeout(TimeSpan.FromSeconds(5)).ToCoroutine();

        Assert.That(source.isLoaded, Is.False);
        Assert.That(oldMap == null, Is.True);
        Assert.That(player != null && visual != null, Is.True);
        Assert.That(player.CurrentHealth, Is.EqualTo(2f));
        Scene destination = SceneManager.CreateScene("StageFlowTestDestination");
        Invoke("SetActiveSceneIfLoaded", destination.name, transferred);
        yield return Release(temporary).Timeout(TimeSpan.FromSeconds(5)).ToCoroutine();

        Assert.That(temporary.isLoaded, Is.False);
        Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(destination));
        Assert.That(player.gameObject.scene, Is.EqualTo(destination));
        Assert.That(Object.FindObjectsByType<UnitPlayer>(FindObjectsSortMode.None), Has.Length.EqualTo(1));

        SpawnPoint spawn = new GameObject("DestinationSpawn").AddComponent<SpawnPoint>();
        typeof(SpawnPoint).GetField("_spawnPointId", PRIVATE).SetValue(spawn, "TRANSFER_TEST");
        spawn.transform.position = new Vector3(100f, 20f, 0f);
        body.linearVelocity = new Vector2(7f, 0f);
        body.angularVelocity = 20f;
        SceneStateBridge bridge = _services.AddComponent<SceneStateBridge>();
        typeof(SceneStateBridge).GetMethod("ApplySpawnPoint", PRIVATE)
            .Invoke(bridge, new object[] { "TRANSFER_TEST" });
        Assert.That(player.transform.position, Is.EqualTo(spawn.Position));
        Assert.That(body.position, Is.EqualTo((Vector2)spawn.Position));
        Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(body.angularVelocity, Is.Zero);
        Assert.That(player.CurrentHealth, Is.EqualTo(2f));
        Assert.That(visual.transform.parent, Is.EqualTo(player.transform));

        CameraController camera = _services.AddComponent<CameraController>();
        Unity.Cinemachine.CinemachineCamera follow = _services.AddComponent<Unity.Cinemachine.CinemachineCamera>();
        typeof(CameraController).GetField("_followCamera", PRIVATE).SetValue(camera, follow);
        follow.PreviousStateIsValid = true;
        camera.Follow(player);
        Assert.That(follow.Follow, Is.EqualTo(player.CameraTarget.transform));
        Assert.That(follow.PreviousStateIsValid, Is.False);

        // 이관 후에는 목적지 씬 소유이므로 일반 씬 교체 때 캐릭터가 남지 않습니다.
        Scene menu = SceneManager.CreateScene("StageFlowTestMenu");
        SceneManager.SetActiveScene(menu);
        yield return SceneManager.UnloadSceneAsync(destination);
        Assert.That(player == null, Is.True);
    }

    [UnityTest]
    public IEnumerator ExistingPersistentSceneRemainsWhileGameplaySceneIsRemoved()
    {
        Scene source = SceneManager.GetActiveScene();
        Scene persistent = SceneManager.CreateScene("SceneFlowTestPersistent");
        typeof(StageFlowController).GetField("_persistentSceneName", PRIVATE)
            .SetValue(_flow, persistent.name);
        Scene temporary = (Scene)Invoke("CreateTransitionSceneIfNeeded", source);
        Assert.That(temporary.IsValid(), Is.False);

        yield return ((UniTask)Invoke("UnloadSceneIfNeededAsync", source, CancellationToken.None))
            .Timeout(TimeSpan.FromSeconds(5)).ToCoroutine();

        Assert.That(source.isLoaded, Is.False);
        Assert.That(persistent.isLoaded, Is.True);
        Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
    }

    private object Invoke(string method, params object[] arguments) =>
        typeof(StageFlowController).GetMethod(method, PRIVATE).Invoke(_flow, arguments);

    private static UniTask Release(Scene scene) => (UniTask)typeof(StageFlowController)
        .GetMethod("ReleaseTransitionSceneAsync", BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { scene });

    private sealed class SceneOperationMonitor : ISceneTransitionService
    {
        public void Init() { }
        public UniTask FadeInAsync(CancellationToken token) => UniTask.CompletedTask;
        public UniTask FadeOutAsync(CancellationToken token) => UniTask.CompletedTask;
        public UniTask MonitorTransitionAsync(AsyncOperation operation, CancellationToken token)
        {
            Assert.That(operation, Is.Not.Null);
            return operation.ToUniTask(cancellationToken: token);
        }
    }
}

/// <summary>기획자가 구성한 실제 Chapter 씬 등록 정보를 읽기 전용으로 확인합니다.</summary>
public sealed class ChapterSceneRegistrationTests
{
    [Test]
    public void BothChaptersHaveStageNumbersAndEnabledBuildScenes()
    {
        SceneCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SceneCatalogSO>(
            "Assets/_Game/Data/SceneCatalog/SceneCatalogSO.asset");
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.TryGetByStageNumber(0, out SceneCatalogSO.SceneEntry chapterZero), Is.True);
        Assert.That(chapterZero.SceneName, Is.EqualTo("Chapter0"));
        Assert.That(catalog.TryGetByStageNumber(3, out SceneCatalogSO.SceneEntry chapterOne), Is.True);
        Assert.That(chapterOne.SceneName, Is.EqualTo("Chapter1"));
        foreach (string chapter in new[] { "Chapter0", "Chapter1" })
        {
            Assert.That(Array.Exists(EditorBuildSettings.scenes,
                scene => scene.enabled && scene.path == "Assets/_Game/Scenes/Chapter/" + chapter + ".unity"), Is.True);
        }
    }
}
