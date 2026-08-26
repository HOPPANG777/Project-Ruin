//=============================================================================
// File:        MuseumPhase4PrefabIntegrator.cs
// Description: MuseumController와 Phase 4 CSV를 GameRoot 프리팹에 연결한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class MuseumPhase4PrefabIntegrator
{
    private const string GAME_ROOT_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";
    private const string MURALS_CSV_PATH = "Assets/_Game/Data/Csv/museum_murals.csv";
    private const string ALTARS_CSV_PATH = "Assets/_Game/Data/Csv/altar_destinations.csv";

    static MuseumPhase4PrefabIntegrator() => EditorApplication.delayCall += Integrate;

    [MenuItem("Tools/Project Ruin/Museum/Integrate Phase 4 Into GameRoot")]
    internal static void Integrate()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GAME_ROOT_PATH);
        TextAsset murals = AssetDatabase.LoadAssetAtPath<TextAsset>(MURALS_CSV_PATH);
        TextAsset altars = AssetDatabase.LoadAssetAtPath<TextAsset>(ALTARS_CSV_PATH);
        if (prefab == null || murals == null || altars == null)
        {
            Debug.LogError("[MuseumPhase4PrefabIntegrator] GameRoot 또는 Phase 4 CSV가 없습니다.");
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(GAME_ROOT_PATH);
        try
        {
            GameRoot gameRoot = contents.GetComponent<GameRoot>();
            MuseumController museum = contents.GetComponent<MuseumController>();
            if (museum == null) museum = contents.AddComponent<MuseumController>();
            CodexUiElementFactory.SetReference(gameRoot, "_museumController", museum);
            CodexUiElementFactory.SetReference(museum, "_museumMuralsCsv", murals);
            CodexUiElementFactory.SetReference(museum, "_altarDestinationsCsv", altars);
            PrefabUtility.SaveAsPrefabAsset(contents, GAME_ROOT_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
