//=============================================================================
// File:        MuseumPhase6PrefabIntegrator.cs
// Description: Phase 6 유물 전시 컨트롤러와 전용 UI를 GameRoot에 연결한다.
// History:     08/30/2026 + Created -- Codex
//              09/23/2026 + Museum Nested Prefab과 Module 경로 반영 -- Codex
//=============================================================================
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class MuseumPhase6PrefabIntegrator
{
    private const string GAME_ROOT_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";
    private const string UI_PATH = "Assets/_Game/Prefabs/UI/MuseumRelicUi.prefab";
    private const string CSV_PATH = "Assets/_Game/Data/Csv/museum_relic_milestones.csv";
    private const int BUTTON_COUNT = 20;

    static MuseumPhase6PrefabIntegrator() => EditorApplication.delayCall += Integrate;

    [MenuItem("Tools/Project Ruin/Museum/Integrate Phase 6 Into GameRoot")]
    internal static void Integrate()
    {
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI_PATH);
        if (uiPrefab == null) uiPrefab = BuildUiPrefab();
        TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CSV_PATH);
        if (uiPrefab == null || csv == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(GAME_ROOT_PATH) == null)
        {
            Debug.LogError("[MuseumPhase6PrefabIntegrator] 필수 프리팹 또는 CSV가 없습니다.");
            return;
        }
        // 도메인 리로드마다 이미 연결된 사용자 프리팹을 불필요하게 재저장하지 않습니다.
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(GAME_ROOT_PATH);
        GameRoot existingRoot = existing.GetComponent<GameRoot>();
        MuseumRelicDisplayController existingController =
            existing.GetComponentInChildren<MuseumRelicDisplayController>(true);
        MuseumRelicPresenter existingPresenter = existing.GetComponentInChildren<MuseumRelicPresenter>(true);
        if (existingRoot != null && existingController != null && existingPresenter != null
            && ReferenceEquals(existingRoot.MuseumRelicDisplays, existingController)
            && existingRoot.MuseumRelicPresenter == existingPresenter
            && new SerializedObject(existingController).FindProperty("_museumRelicMilestonesCsv")
                .objectReferenceValue == csv) return;
        GameObject contents = PrefabUtility.LoadPrefabContents(GAME_ROOT_PATH);
        try
        {
            GameRoot root = contents.GetComponent<GameRoot>();
            MuseumRelicDisplayController controller =
                MuseumBootstrapPrefabUtility.GetOrCreateComponent<MuseumRelicDisplayController>(contents);
            if (controller == null) return;
            MuseumRelicPresenter presenter = contents.GetComponentInChildren<MuseumRelicPresenter>(true);
            if (presenter == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    uiPrefab, contents.transform);
                instance.name = "MuseumRelicUi";
                presenter = instance.GetComponent<MuseumRelicPresenter>();
            }
            CodexUiElementFactory.SetReference(controller, "_museumRelicMilestonesCsv", csv);
            CodexUiElementFactory.SetReference(
                root, "_museumModule._museumRelicDisplayController", controller);
            CodexUiElementFactory.SetReference(
                root, "_uiModule._museumRelicPresenter", presenter);
            PrefabUtility.SaveAsPrefabAsset(contents, GAME_ROOT_PATH);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    [MenuItem("Tools/Project Ruin/Museum/Rebuild Phase 6 Relic UI")]
    internal static void RebuildUi() => BuildUiPrefab();

    private static GameObject BuildUiPrefab()
    {
        GameObject root = CodexUiElementFactory.Create("MuseumRelicUi", null,
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.localScale = Vector3.one;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 26;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        GameObject panel = CodexUiElementFactory.CreateImage(
            "Panel", root.transform, new Color32(239, 224, 190, 250));
        CodexUiElementFactory.SetRect(panel.GetComponent<RectTransform>(),
            Vector2.zero, new Vector2(1100, 850), new Vector2(0.5f, 0.5f));
        CodexUiElementFactory.CreateText("Title", panel.transform, "유물 전시",
            36, TextAlignmentOptions.Center, new Vector2(550, 798), new Vector2(500, 48));
        GameObject progress = CodexUiElementFactory.CreateText("Progress", panel.transform,
            "전시 0 / 0", 23, TextAlignmentOptions.Center,
            new Vector2(430, 744), new Vector2(240, 38));
        GameObject current = CodexUiElementFactory.CreateText("Current", panel.transform,
            "현재 전시: 없음", 22, TextAlignmentOptions.Left,
            new Vector2(70, 690), new Vector2(690, 42));
        Button recover = CodexUiElementFactory.CreateButton(
            "Recover", panel.transform, "회수", new Vector2(780, 686));
        Button close = CodexUiElementFactory.CreateButton(
            "Close", panel.transform, "X", new Vector2(950, 786));
        GameObject list = CodexUiElementFactory.Create("RelicList", panel.transform,
            typeof(GridLayoutGroup));
        CodexUiElementFactory.SetRect(list.GetComponent<RectTransform>(),
            new Vector2(55, 80), new Vector2(990, 580), Vector2.zero);
        GridLayoutGroup grid = list.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(235, 102);
        grid.spacing = new Vector2(16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        MuseumRelicItemButton[] buttons = new MuseumRelicItemButton[BUTTON_COUNT];
        for (int i = 0; i < buttons.Length; i++) buttons[i] = CreateItemButton(list.transform, i);
        GameObject empty = CodexUiElementFactory.Create("Empty", panel.transform);
        CodexUiElementFactory.SetRect(empty.GetComponent<RectTransform>(),
            new Vector2(300, 360), new Vector2(500, 60), Vector2.zero);
        GameObject emptyText = CodexUiElementFactory.CreateText("Label", empty.transform,
            "배치 가능한 보유 유물이 없습니다.", 25, TextAlignmentOptions.Center);
        CodexUiElementFactory.Stretch(emptyText.GetComponent<RectTransform>(), 0);
        MuseumRelicSelectionView view = root.AddComponent<MuseumRelicSelectionView>();
        MuseumRelicPresenter presenter = root.AddComponent<MuseumRelicPresenter>();
        BindView(view, panel, empty, progress.GetComponent<TMP_Text>(),
            current.GetComponent<TMP_Text>(), buttons, recover, close);
        CodexUiElementFactory.SetReference(presenter, "_view", view);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, UI_PATH);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    private static MuseumRelicItemButton CreateItemButton(Transform parent, int index)
    {
        GameObject root = CodexUiElementFactory.CreateImage(
            $"Relic_{index + 1:00}", parent, new Color32(101, 67, 42, 255));
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        GameObject icon = CodexUiElementFactory.CreateImage("Icon", root.transform, Color.white);
        CodexUiElementFactory.SetRect(icon.GetComponent<RectTransform>(),
            new Vector2(8, 17), new Vector2(68, 68), Vector2.zero);
        GameObject name = CodexUiElementFactory.CreateText("Name", root.transform,
            "유물", 19, TextAlignmentOptions.Left, new Vector2(86, 48), new Vector2(138, 40));
        GameObject count = CodexUiElementFactory.CreateText("Count", root.transform,
            "x0", 17, TextAlignmentOptions.Left, new Vector2(86, 18), new Vector2(90, 28));
        MuseumRelicItemButton item = root.AddComponent<MuseumRelicItemButton>();
        CodexUiElementFactory.SetReference(item, "_button", button);
        CodexUiElementFactory.SetReference(item, "_icon", icon.GetComponent<Image>());
        CodexUiElementFactory.SetReference(item, "_nameText", name.GetComponent<TMP_Text>());
        CodexUiElementFactory.SetReference(item, "_countText", count.GetComponent<TMP_Text>());
        return item;
    }

    private static void BindView(MuseumRelicSelectionView view, GameObject panel,
        GameObject empty, TMP_Text progress, TMP_Text current,
        MuseumRelicItemButton[] buttons, Button recover, Button close)
    {
        CodexUiElementFactory.SetReference(view, "_panelRoot", panel);
        CodexUiElementFactory.SetReference(view, "_emptyRoot", empty);
        CodexUiElementFactory.SetReference(view, "_progressText", progress);
        CodexUiElementFactory.SetReference(view, "_currentRelicText", current);
        CodexUiElementFactory.SetReference(view, "_recoverButton", recover);
        CodexUiElementFactory.SetReference(view, "_closeButton", close);
        SerializedObject serialized = new SerializedObject(view);
        SerializedProperty array = serialized.FindProperty("_itemButtons");
        array.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
