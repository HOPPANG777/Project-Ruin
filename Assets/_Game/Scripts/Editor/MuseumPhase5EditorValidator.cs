//=============================================================================
// File:        MuseumPhase5EditorValidator.cs
// Description: 빌드 전 술병 CSV, GameRoot, UI와 Scene 연결을 자동 검증한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class MuseumPhase5EditorValidator
{
    private const string GAME_ROOT_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";
    private const string UI_PREFAB_PATH = "Assets/_Game/Prefabs/UI/MuseumBottleUi.prefab";

    [MenuItem("Tools/Project Ruin/Museum/Validate Phase 5 Data")]
    internal static void ValidateFromMenu()
    {
        if (!TryValidate(out string message))
        {
            Debug.LogError($"[MuseumPhase5EditorValidator] {message}");
            return;
        }
        Debug.Log($"[MuseumPhase5EditorValidator] {message}");
    }

    internal static bool TryValidate(out string message)
    {
        message = string.Empty;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GAME_ROOT_PATH);
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI_PREFAB_PATH);
        if (prefab == null || uiPrefab == null)
            return Fail("GameRoot 또는 술병 UI 프리팹이 없습니다.", out message);
        GameRoot root = prefab.GetComponent<GameRoot>();
        GameDataController gameData = prefab.GetComponentInChildren<GameDataController>(true);
        MuseumBottleController bottles = prefab.GetComponentInChildren<MuseumBottleController>(true);
        InventoryController inventory = prefab.GetComponentInChildren<InventoryController>(true);
        ProgressController progress = prefab.GetComponentInChildren<ProgressController>(true);
        MuseumBottlePresenter presenter =
            prefab.GetComponentInChildren<MuseumBottlePresenter>(true);
        if (root == null || gameData == null || bottles == null || inventory == null
            || progress == null || presenter == null
            || !object.ReferenceEquals(root.MuseumBottles, bottles)
            || root.MuseumBottlePresenter != presenter)
            return Fail("GameRoot의 Phase 5 Controller 또는 Presenter 연결이 누락됐습니다.", out message);
        if (!TryGetAsset(bottles, "_museumBottlesCsv", out TextAsset bottlesCsv)
            || !TryGetAsset(bottles, "_museumBottleItemsCsv", out TextAsset itemsCsv))
            return Fail("MuseumBottleController의 두 CSV 참조가 누락됐습니다.", out message);
        if (!gameData.TryInit(out GameDataCsvLoadError gameDataError))
            return Fail($"기본 게임 CSV 오류: {gameDataError}", out message);
        if (!bottles.TryInit(gameData.Repository, inventory, progress,
                out GameDataCsvLoadError bottleError))
            return Fail($"술병 CSV 오류: {bottleError}", out message);
        if (!MuseumBottleCsvDefinitionLoader.TryLoad(
                new GameDataCsvFile(bottlesCsv.name, bottlesCsv.text),
                new GameDataCsvFile(itemsCsv.name, itemsCsv.text),
                out List<MuseumBottleDefinition> definitions, out _, out bottleError))
            return Fail($"술병 CSV 재검증 오류: {bottleError}", out message);
        if (!MuseumPhase5SceneValidator.TryValidate(definitions, out string sceneError))
            return Fail(sceneError, out message);
        message = $"검증 완료: bottles={bottles.BottleCount}";
        return true;
    }

    private static bool TryGetAsset<T>(Object owner, string field, out T asset) where T : Object
    {
        SerializedProperty property = new SerializedObject(owner).FindProperty(field);
        asset = property != null ? property.objectReferenceValue as T : null;
        return asset != null;
    }
    private static bool Fail(string value, out string message)
    {
        message = value;
        return false;
    }
}

internal sealed class MuseumPhase5BuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 25;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (!MuseumPhase5EditorValidator.TryValidate(out string message))
            throw new BuildFailedException($"[MuseumPhase5BuildPreprocessor] {message}");
    }
}
