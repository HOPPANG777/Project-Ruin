using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬을 닫거나 저장하지 않고 목적지의 도착 지점을 조회합니다.
public static class TransitionPadSceneSetup
{
    public sealed class SpawnOption
    {
        public string Id;
        public string Label;
    }

    private static readonly Dictionary<string, (Hash128 hash, SpawnOption[] spawns)> Cache =
        new Dictionary<string, (Hash128, SpawnOption[])>();

    public static SceneCatalogSO FindCatalog()
    {
        foreach (StageFlowController flow in UnityEngine.Object.FindObjectsByType<StageFlowController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (EditorSceneManager.IsPreviewSceneObject(flow)) continue;
            SceneCatalogSO catalog = ReadCatalog(flow);
            if (catalog != null) return catalog;
        }
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab");
        return root != null ? ReadCatalog(root.GetComponentInChildren<StageFlowController>(true)) : null;
    }

    private static SceneCatalogSO ReadCatalog(StageFlowController flow) => flow != null
        ? new SerializedObject(flow).FindProperty("_sceneCatalog").objectReferenceValue as SceneCatalogSO
        : null;

    public static SceneAsset FindScene(SceneCatalogSO catalog, int stageNumber)
    {
        if (catalog == null || !catalog.TryGetByStageNumber(stageNumber, out var entry)) return null;
        string[] matches = ScenePaths(entry.SceneName);
        return matches.Length == 1 ? AssetDatabase.LoadAssetAtPath<SceneAsset>(matches[0]) : null;
    }

    private static string[] ScenePaths(string name) => AssetDatabase.FindAssets("t:Scene " + name,
            new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath)
        .Where(path => Path.GetFileNameWithoutExtension(path) == name).ToArray();

    public static int RegisterDestination(SceneAsset scene, SceneCatalogSO catalog)
    {
        if (scene == null || catalog == null)
            throw new InvalidOperationException("목적지 맵과 GameRoot의 SceneCatalog가 필요합니다.");
        string path = AssetDatabase.GetAssetPath(scene);
        if (ScenePaths(scene.name).Length != 1)
            throw new InvalidOperationException("같은 이름의 씬이 여러 개입니다. 씬 이름을 다르게 지정해 주세요.");

        SerializedObject data = new SerializedObject(catalog);
        SerializedProperty entries = data.FindProperty("_entries");
        int index = -1;
        int nextNumber = 0;
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty item = entries.GetArrayElementAtIndex(i);
            nextNumber = Math.Max(nextNumber, item.FindPropertyRelative("_stageNumber").intValue + 1);
            if (item.FindPropertyRelative("_sceneName").stringValue != scene.name) continue;
            if (index >= 0) throw new InvalidOperationException("SceneCatalog에 같은 맵이 중복 등록되어 있습니다.");
            index = i;
        }

        int stageNumber = index >= 0
            ? entries.GetArrayElementAtIndex(index).FindPropertyRelative("_stageNumber").intValue
            : nextNumber;
        if (stageNumber < 0) stageNumber = nextNumber;
        for (int i = 0; i < entries.arraySize; i++)
        {
            if (i == index) continue;
            SerializedProperty item = entries.GetArrayElementAtIndex(i);
            if (item.FindPropertyRelative("_sceneCategory").intValue == (int)SceneCategory.STAGE
                && item.FindPropertyRelative("_stageNumber").intValue == stageNumber)
                throw new InvalidOperationException("SceneCatalog의 스테이지 번호가 중복됩니다. 담당 개발자에게 확인해 주세요.");
        }

        if (index < 0)
        {
            index = entries.arraySize++;
            SerializedProperty added = entries.GetArrayElementAtIndex(index);
            added.FindPropertyRelative("_sceneId").stringValue = scene.name;
            added.FindPropertyRelative("_sceneName").stringValue = scene.name;
        }
        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("_sceneCategory").intValue = (int)SceneCategory.STAGE;
        entry.FindPropertyRelative("_stageNumber").intValue = stageNumber;
        data.ApplyModifiedProperties();

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        EditorBuildSettingsScene buildScene = scenes.Find(item => item.path == path);
        if (buildScene != null) buildScene.enabled = true;
        else scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssetIfDirty(catalog);
        return stageNumber;
    }

    public static SpawnOption[] ReadSpawnPoints(string path)
    {
        Scene loaded = SceneManager.GetSceneByPath(path);
        if (loaded.IsValid() && loaded.isLoaded) return Collect(loaded);
        Hash128 hash = AssetDatabase.GetAssetDependencyHash(path);
        if (Cache.TryGetValue(path, out var cached) && cached.hash == hash) return cached.spawns;
        Scene preview = EditorSceneManager.OpenPreviewScene(path);
        try
        {
            SpawnOption[] options = Collect(preview);
            Cache[path] = (hash, options);
            return options;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static SpawnOption[] Collect(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<SpawnPoint>(true))
        .Where(point => point.isActiveAndEnabled && !string.IsNullOrWhiteSpace(point.SpawnPointId))
        .Select(point => new SpawnOption { Id = point.SpawnPointId, Label = point.name })
        .OrderBy(point => point.Label, StringComparer.Ordinal).ToArray();
}
