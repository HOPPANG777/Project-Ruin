//=============================================================================
// File:        MuseumPhase4EditorValidator.cs
// Description: 빌드 전 박물관 CSV와 GameRoot 연결을 자동 검증한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class MuseumPhase4EditorValidator
{
    private const string GAME_ROOT_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";

    [MenuItem("Tools/Project Ruin/Museum/Validate Phase 4 Data")]
    internal static void ValidateFromMenu()
    {
        if (!TryValidate(out string message))
        {
            Debug.LogError($"[MuseumPhase4EditorValidator] {message}");
            return;
        }
        Debug.Log($"[MuseumPhase4EditorValidator] {message}");
    }

    internal static bool TryValidate(out string message)
    {
        message = string.Empty;
        GameObject gameRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GAME_ROOT_PATH);
        if (gameRootPrefab == null)
            return Fail("GameRoot 프리팹을 찾을 수 없습니다.", out message);

        GameDataController gameData =
            gameRootPrefab.GetComponentInChildren<GameDataController>(true);
        MuseumController museum =
            gameRootPrefab.GetComponentInChildren<MuseumController>(true);
        ProgressController progress =
            gameRootPrefab.GetComponentInChildren<ProgressController>(true);
        RelicRestorationController restoration =
            gameRootPrefab.GetComponentInChildren<RelicRestorationController>(true);
        StageFlowController stageFlow =
            gameRootPrefab.GetComponentInChildren<StageFlowController>(true);
        if (gameData == null || museum == null || progress == null
            || restoration == null || stageFlow == null)
            return Fail("GameRoot의 Phase 4 필수 Controller 연결이 누락됐습니다.", out message);

        if (!TryGetAsset(museum, "_museumMuralsCsv", out TextAsset muralsCsv)
            || !TryGetAsset(museum, "_altarDestinationsCsv", out TextAsset altarsCsv))
            return Fail("MuseumController의 두 CSV 참조가 누락됐습니다.", out message);
        if (!TryGetAsset(stageFlow, "_sceneCatalog", out SceneCatalogSO sceneCatalog))
            return Fail("StageFlowController의 SceneCatalog 참조가 누락됐습니다.", out message);

        if (!gameData.TryInit(out GameDataCsvLoadError gameDataError))
            return Fail($"기본 게임 CSV 오류: {gameDataError}", out message);
        if (!museum.TryInit(gameData.Repository, progress, restoration,
                out GameDataCsvLoadError museumError))
            return Fail($"박물관 CSV 오류: {museumError}", out message);

        if (!MuseumCsvDefinitionLoader.TryLoad(
                new GameDataCsvFile(muralsCsv.name, muralsCsv.text),
                new GameDataCsvFile(altarsCsv.name, altarsCsv.text),
                out List<MuseumMuralDefinition> murals,
                out List<AltarDestinationDefinition> altars, out museumError))
            return Fail($"박물관 CSV 재검증 오류: {museumError}", out message);
        if (!MuseumPhase4SceneValidator.TryValidate(
                murals, altars, sceneCatalog, out string sceneError))
            return Fail(sceneError, out message);

        message = $"검증 완료: murals={museum.MuralCount}, altars={museum.AltarDestinationCount}";
        return true;
    }

    private static bool TryGetAsset<T>(Object owner, string fieldName, out T asset)
        where T : Object
    {
        SerializedProperty property = new SerializedObject(owner).FindProperty(fieldName);
        asset = property != null ? property.objectReferenceValue as T : null;
        return asset != null;
    }

    private static bool Fail(string error, out string message)
    {
        message = error;
        return false;
    }
}

internal sealed class MuseumPhase4BuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 20;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!MuseumPhase4EditorValidator.TryValidate(out string message))
            throw new BuildFailedException($"[MuseumPhase4BuildPreprocessor] {message}");
    }
}
