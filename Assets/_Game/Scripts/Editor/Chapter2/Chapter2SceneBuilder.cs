//=============================================================================
// File:        Chapter2SceneBuilder.cs
// Description: Chapter1 씬을 본떠 Chapter2 씬(카메라 + Grid_2 + 도착 지점)을 만들고 SceneCatalog·빌드 설정에 등록합니다.
// History:     11/28/2026 + Created
//=============================================================================
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>스테이지 씬은 GameRoot/플레이어 없이 맵·카메라·도착 지점만 가진다(Chapter1과 동일 구성).</summary>
public static class Chapter2SceneBuilder
{
    private const string SourcePath = "Assets/_Game/Scenes/Chapter/Chapter1.unity";
    private const string ScenePath = "Assets/_Game/Scenes/Chapter/Chapter2.unity";
    private const string CatalogPath = "Assets/_Game/Data/SceneCatalog/SceneCatalogSO.asset";
    private const string GridPath = "Assets/_Game/Prefabs/Map/Grid_2.prefab";
    private const string SpawnPrefab = "Assets/_Game/Prefabs/World/TransitionSpawnPoint.prefab";
    public const string EntranceId = "CH2_VILLAGE_ENTRANCE";

    [MenuItem("Tools/Project Ruin/Chapter 2/Chapter2 씬 만들기")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            if (!AssetDatabase.CopyAsset(SourcePath, ScenePath)) throw new InvalidOperationException("Chapter1 씬 복제 실패");
        }
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name != "Main Camera") UnityEngine.Object.DestroyImmediate(roots[i]);
        }

        GameObject grid = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GridPath), scene);
        grid.transform.position = Vector3.zero;
        Transform village = Ch2Util.FindDeep(grid.transform, "Tilemap2");

        // 마을 서쪽 입구 통로(Tilemap2 셀 x=-300, 바닥 y=-730~-726)의 중앙. 챕터1에서 넘어오는 플레이어가 도착한다.
        GameObject spawn = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SpawnPrefab), scene);
        spawn.name = "마을 입구 (챕터1에서 도착)";
        spawn.transform.position = village.TransformPoint(new Vector3(-299.5f, -727.5f, 0f));
        Ch2Util.SetString(spawn.GetComponent<SpawnPoint>(), "_spawnPointId", EntranceId);

        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        SceneCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SceneCatalogSO>(CatalogPath);
        EditorSceneManager.SaveScene(scene);
        int stageNumber = TransitionPadSceneSetup.RegisterDestination(sceneAsset, catalog);
        if (stageNumber != Ch2Util.StageNumber)
        {
            Debug.LogWarning($"[Chapter2SceneBuilder] SceneCatalog 스테이지 번호가 {stageNumber}입니다. Ch2Util.StageNumber({Ch2Util.StageNumber})와 저장 ID 연결을 확인하세요.");
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[Chapter2SceneBuilder] Chapter2 씬 생성·등록 완료. stage={stageNumber}, spawn={spawn.transform.position}");
    }

    /// <summary>배치 모드 진입점: Unity.exe -batchmode -executeMethod Chapter2SceneBuilder.BuildBatch</summary>
    public static void BuildBatch()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }
}
