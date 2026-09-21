//=============================================================================
// File:        MuseumPhase6EditorValidator.cs
// Description: 빌드 전 유물 전시 CSV, GameRoot, UI, 폰트와 Scene을 검증한다.
// History:     08/30/2026 + Created -- Codex
//=============================================================================
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class MuseumPhase6EditorValidator
{
    private const string GAME_ROOT_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";
    private const string UI_PATH = "Assets/_Game/Prefabs/UI/MuseumRelicUi.prefab";
    private const string BAZZI_PATH = "Assets/TextMesh Pro/Fonts/Bazzi SDF.asset";

    [MenuItem("Tools/Project Ruin/Museum/Validate Phase 6 Data")]
    internal static void ValidateFromMenu()
    {
        if (!TryValidate(out string message)) Debug.LogError($"[MuseumPhase6] {message}");
        else Debug.Log($"[MuseumPhase6] {message}");
    }

    internal static bool TryValidate(out string message)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GAME_ROOT_PATH);
        GameObject ui = AssetDatabase.LoadAssetAtPath<GameObject>(UI_PATH);
        TMP_FontAsset bazzi = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BAZZI_PATH);
        if (prefab == null || ui == null || bazzi == null)
            return Fail("GameRoot, 유물 UI 또는 Bazzi Font가 없습니다.", out message);
        GameRoot root = prefab.GetComponent<GameRoot>();
        GameDataController data = prefab.GetComponentInChildren<GameDataController>(true);
        InventoryController inventory = prefab.GetComponentInChildren<InventoryController>(true);
        ProgressController progress = prefab.GetComponentInChildren<ProgressController>(true);
        MuseumRelicDisplayController displays =
            prefab.GetComponentInChildren<MuseumRelicDisplayController>(true);
        MuseumRelicPresenter presenter = prefab.GetComponentInChildren<MuseumRelicPresenter>(true);
        if (root == null || data == null || inventory == null || progress == null
            || displays == null || presenter == null
            || !ReferenceEquals(root.MuseumRelicDisplays, displays)
            || root.MuseumRelicPresenter != presenter)
            return Fail("GameRoot의 Phase 6 Controller 또는 Presenter 연결이 누락됐습니다.", out message);
        SerializedProperty csvProperty = new SerializedObject(displays)
            .FindProperty("_museumRelicMilestonesCsv");
        if (csvProperty?.objectReferenceValue == null)
            return Fail("마일스톤 CSV 참조가 누락됐습니다.", out message);
        if (!data.TryInit(out GameDataCsvLoadError dataError))
            return Fail($"기본 게임 CSV 오류: {dataError}", out message);
        TextAsset csv = csvProperty.objectReferenceValue as TextAsset;
        if (!MuseumRelicMilestoneCsvLoader.TryLoadRepository(
                new GameDataCsvFile(csv.name, csv.text), data.Repository,
                out var repository, out GameDataCsvLoadError displayError))
            return Fail($"유물 전시 CSV 오류: {displayError}", out message);
        if (!TryValidateUi(ui, out message)
            || !TryValidateUi(presenter.gameObject, out message)) return false;
        TMP_Text[] texts = ui.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            if (texts[i].font != bazzi)
                return Fail($"유물 UI Text '{texts[i].name}'가 Bazzi가 아닙니다.", out message);
        if (!MuseumPhase6SceneValidator.TryValidate(out int pedestalCount, out message)) return false;
        int relicCount = 0;
        var entries = data.Repository.CodexData.CodexEntries;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Category == CodexCategory.CODEX_CATEGORY_RELIC) relicCount++;
        bool contentReady = relicCount == 20 && repository.Milestones.Count == 2 && pedestalCount == 20;
        message = (contentReady ? "기반·콘텐츠 연결 검증 통과" : "기반 검증 통과 / 콘텐츠 미완성")
            + $": relics={relicCount}, milestones={repository.Milestones.Count}, pedestals={pedestalCount}";
        return true;
    }

    private static bool TryValidateUi(GameObject ui, out string message)
    {
        message = string.Empty;
        MuseumRelicSelectionView view = ui.GetComponent<MuseumRelicSelectionView>();
        MuseumRelicPresenter presenter = ui.GetComponent<MuseumRelicPresenter>();
        if (view == null || presenter == null
            || new SerializedObject(presenter).FindProperty("_view").objectReferenceValue != view)
            return Fail("유물 UI의 View/Presenter 연결이 누락됐습니다.", out message);
        SerializedObject serialized = new SerializedObject(view);
        string[] required = { "_panelRoot", "_emptyRoot", "_progressText",
            "_currentRelicText", "_recoverButton", "_closeButton" };
        for (int i = 0; i < required.Length; i++)
            if (serialized.FindProperty(required[i]).objectReferenceValue == null)
                return Fail($"유물 UI 참조 누락: {required[i]}", out message);
        SerializedProperty buttons = serialized.FindProperty("_itemButtons");
        if (buttons.arraySize != 20) return Fail("유물 UI 선택 슬롯은 20개여야 합니다.", out message);
        System.Collections.Generic.HashSet<Object> seen = new System.Collections.Generic.HashSet<Object>();
        for (int i = 0; i < buttons.arraySize; i++)
        {
            Object button = buttons.GetArrayElementAtIndex(i).objectReferenceValue;
            if (button == null || !seen.Add(button))
                return Fail("유물 UI 선택 슬롯에 누락 또는 중복이 있습니다.", out message);
        }
        return true;
    }

    private static bool Fail(string value, out string message)
    { message = value; return false; }
}

internal sealed class MuseumPhase6BuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 26;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (!MuseumPhase6EditorValidator.TryValidate(out string message))
            throw new BuildFailedException($"[MuseumPhase6BuildPreprocessor] {message}");
    }
}
