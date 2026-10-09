using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class TransitionPadPrefabTests
{
    private readonly List<Object> _created = new List<Object>();
    private Scene _temporaryScene;
    private string _scenePath;
    private EditorBuildSettingsScene[] _buildScenes;

    [TearDown]
    public void TearDown()
    {
        if (_temporaryScene.IsValid())
        {
            if (EditorSceneManager.IsPreviewScene(_temporaryScene)) EditorSceneManager.ClosePreviewScene(_temporaryScene);
            else EditorSceneManager.CloseScene(_temporaryScene, true);
        }
        foreach (Object created in _created)
            if (created != null) Object.DestroyImmediate(created);
        _created.Clear();
        if (_buildScenes != null) EditorBuildSettings.scenes = _buildScenes;
        if (_scenePath != null) AssetDatabase.DeleteAsset(_scenePath);
        _temporaryScene = default;
        _scenePath = null;
        _buildScenes = null;
    }

    [TestCase(TransitionPadMapTools.PositionPadPath, typeof(PositionTransitionPad))]
    [TestCase(TransitionPadMapTools.ScenePadPath, typeof(SceneTransitionPad))]
    public void PrefabsAreVisibleTriggerPadsWithoutMissingScripts(string path, Type componentType)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent(componentType), Is.Not.Null);
        BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
        Assert.That(collider.enabled && collider.isTrigger, Is.True);
        Assert.That(collider.size, Is.EqualTo(Vector2.one));
        Assert.That(prefab.layer, Is.Zero);
        Assert.That(prefab.GetComponentInChildren<SpriteRenderer>().sprite, Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<Collider2D>().Length, Is.EqualTo(1));
        foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero);
    }

    [Test]
    public void DuplicatedPositionPadKeepsItsOwnDestinationAndMovesOnlyDuringFade()
    {
        PositionTransitionPad original = PositionInstance();
        GameObject cloneObject = Object.Instantiate(original.gameObject);
        _created.Add(cloneObject);
        PositionTransitionPad clone = cloneObject.GetComponent<PositionTransitionPad>();
        Transform originalDestination = Destination(original);
        Transform cloneDestination = Destination(clone);
        Assert.That(cloneDestination.IsChildOf(clone.transform), Is.True);
        Assert.That(cloneDestination, Is.Not.SameAs(originalDestination));
        cloneDestination.position = new Vector3(12f, 7f, 0f);
        UnitPlayer player = Player();
        FakeFlow flow = new FakeFlow();
        Execute(clone, player, flow);
        Assert.That(player.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(flow.FadeCalls, Is.EqualTo(1));
        Assert.That(flow.StageCalls, Is.Zero);
        flow.CoveredAction();
        Assert.That(player.transform.position, Is.EqualTo(cloneDestination.position));
        Assert.That(originalDestination.localPosition, Is.EqualTo(new Vector3(3f, 0f, 0f)));
    }

    [Test]
    public void MissingOrCrossScenePositionDestinationDoesNotRequestFade()
    {
        PositionTransitionPad pad = PositionInstance();
        SetReference(pad, "_destination", null);
        FakeFlow flow = new FakeFlow();
        LogAssert.Expect(LogType.Warning, "[PositionTransitionPad] 목적지가 연결되지 않았습니다.");
        Execute(pad, Player(), flow);
        Assert.That(flow.FadeCalls, Is.Zero);

        _temporaryScene = EditorSceneManager.NewPreviewScene();
        GameObject destination = new GameObject("OtherSceneDestination");
        SceneManager.MoveGameObjectToScene(destination, _temporaryScene);
        SetReference(pad, "_destination", destination.transform);
        LogAssert.Expect(LogType.Warning, "[PositionTransitionPad] 도착 위치는 발판과 같은 씬에 있어야 합니다.");
        Execute(pad, Player(), flow);
        Assert.That(flow.FadeCalls, Is.Zero);
    }

    [Test]
    public void ScenePadStartsUnconfiguredAndPassesSelectedStageAndSpawnToExistingPipeline()
    {
        SceneTransitionPad pad = Instance(TransitionPadMapTools.ScenePadPath).GetComponent<SceneTransitionPad>();
        FakeFlow flow = new FakeFlow();
        LogAssert.Expect(LogType.Warning, "[SceneTransitionPad] 목적지 스테이지 번호가 설정되지 않았습니다.");
        Execute(pad, Player(), flow);
        Assert.That(flow.StageCalls, Is.Zero);
        SerializedObject data = new SerializedObject(pad);
        data.FindProperty("_destinationStageNumber").intValue = 7;
        data.FindProperty("_targetSpawnPointId").stringValue = "SPAWN_EAST";
        data.ApplyModifiedPropertiesWithoutUndo();
        Execute(pad, Player(), flow);
        Assert.That(flow.StageCalls, Is.EqualTo(1));
        Assert.That(flow.Stage, Is.EqualTo(7));
        Assert.That(flow.Transfer.enteringStageNumber, Is.EqualTo(7));
        Assert.That(flow.Transfer.targetSpawnPointId, Is.EqualTo("SPAWN_EAST"));
    }

    [Test]
    public void AwakeRepairsAccidentallyDisabledTriggerMode()
    {
        PositionTransitionPad pad = PositionInstance();
        BoxCollider2D collider = pad.GetComponent<BoxCollider2D>();
        collider.isTrigger = false;
        typeof(TransitionPadBase).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(pad, null);
        Assert.That(collider.isTrigger, Is.True);
    }

    [Test]
    public void SceneRegistrationIsIdempotentAndSpawnInspectionPreservesOpenScenes()
    {
        _buildScenes = EditorBuildSettings.scenes;
        _scenePath = "Assets/__TransitionPadTest_" + Guid.NewGuid().ToString("N") + ".unity";
        // EditMode Runner의 임시 빈 씬을 사용합니다. 사용자가 열었던 맵은 Runner가 복구합니다.
        _temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject spawn = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(TransitionPadMapTools.SpawnPointPath), _temporaryScene);
        spawn.name = "East Entrance";
        SerializedObject point = new SerializedObject(spawn.GetComponent<SpawnPoint>());
        point.FindProperty("_spawnPointId").stringValue = "SPAWN_EAST";
        point.ApplyModifiedPropertiesWithoutUndo();
        GameObject inactive = Object.Instantiate(spawn);
        inactive.SetActive(false);
        Assert.That(EditorSceneManager.SaveScene(_temporaryScene, _scenePath), Is.True);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _temporaryScene = default;

        SceneCatalogSO catalog = ScriptableObject.CreateInstance<SceneCatalogSO>();
        _created.Add(catalog);
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(_scenePath);
        SerializedObject legacy = new SerializedObject(catalog);
        SerializedProperty entries = legacy.FindProperty("_entries");
        entries.arraySize = 1;
        SerializedProperty oldEntry = entries.GetArrayElementAtIndex(0);
        oldEntry.FindPropertyRelative("_sceneId").stringValue = "LEGACY_MAP";
        oldEntry.FindPropertyRelative("_sceneName").stringValue = scene.name;
        oldEntry.FindPropertyRelative("_sceneCategory").intValue = (int)SceneCategory.NONE;
        oldEntry.FindPropertyRelative("_stageNumber").intValue = 3;
        legacy.ApplyModifiedPropertiesWithoutUndo();
        int stage = TransitionPadSceneSetup.RegisterDestination(scene, catalog);
        int again = TransitionPadSceneSetup.RegisterDestination(scene, catalog);
        Assert.That(again, Is.EqualTo(stage));
        Assert.That(stage, Is.EqualTo(3));
        Assert.That(new SerializedObject(catalog).FindProperty("_entries").arraySize, Is.EqualTo(1));
        Assert.That(catalog.TryGetByStageNumber(stage, out var entry), Is.True);
        Assert.That(entry.SceneName, Is.EqualTo(scene.name));
        Assert.That(entry.SceneId, Is.EqualTo("LEGACY_MAP"));
        Assert.That(EditorBuildSettings.scenes.Count(item => item.path == _scenePath && item.enabled), Is.EqualTo(1));
        int sceneCount = SceneManager.sceneCount;
        Scene activeScene = SceneManager.GetActiveScene();
        var options = TransitionPadSceneSetup.ReadSpawnPoints(_scenePath);
        Assert.That(options.Length, Is.EqualTo(1));
        Assert.That(options[0].Id, Is.EqualTo("SPAWN_EAST"));
        Assert.That(options[0].Label, Does.Contain("East Entrance"));
        Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeScene));
    }

    [Test]
    public void AreaWarningRespectsColliderOffsetScaleAndRotation()
    {
        PositionTransitionPad pad = PositionInstance();
        pad.transform.SetPositionAndRotation(new Vector3(4f, 5f, 0f), Quaternion.Euler(0f, 0f, 45f));
        pad.transform.localScale = new Vector3(2f, 3f, 1f);
        BoxCollider2D collider = pad.GetComponent<BoxCollider2D>();
        collider.offset = new Vector2(1f, 0f);
        Assert.That(TransitionPadEditor.Contains(collider, pad.transform.TransformPoint(collider.offset)), Is.True);
        Assert.That(TransitionPadEditor.Contains(collider, pad.transform.TransformPoint(new Vector3(3f, 0f))), Is.False);
    }

    private GameObject Instance(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null, path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        _created.Add(instance);
        return instance;
    }

    private PositionTransitionPad PositionInstance() => Instance(TransitionPadMapTools.PositionPadPath)
        .GetComponent<PositionTransitionPad>();
    private static Transform Destination(PositionTransitionPad pad) =>
        new SerializedObject(pad).FindProperty("_destination").objectReferenceValue as Transform;
    private UnitPlayer Player()
    {
        GameObject player = new GameObject("Player");
        player.SetActive(false);
        _created.Add(player);
        return player.AddComponent<UnitPlayer>();
    }
    private static void SetReference(Object target, string name, Object value)
    {
        SerializedObject data = new SerializedObject(target);
        data.FindProperty(name).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Execute(TransitionPadBase pad, UnitPlayer player, IStageFlowService flow) =>
        pad.GetType().GetMethod("ExecuteTransitionAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(pad, new object[] { player, flow });

    private sealed class FakeFlow : IStageFlowService
    {
        public bool IsTransitioning => false;
        public Action CoveredAction;
        public int FadeCalls;
        public int StageCalls;
        public int Stage;
        public SceneTransferData Transfer;
        public UniTask FadeThroughAsync(Action coveredAction)
        {
            FadeCalls++;
            CoveredAction = coveredAction;
            return UniTask.CompletedTask;
        }
        public UniTask ChangeStageAsync(int stageNumber, SceneTransferData data)
        {
            StageCalls++;
            Stage = stageNumber;
            Transfer = data;
            return UniTask.CompletedTask;
        }
        public UniTask ChangeSceneAsync(string sceneId, SceneTransferData data) => UniTask.CompletedTask;
    }
}
