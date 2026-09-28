//=============================================================================
// File:        MuseumPhase5PrefabIntegrator.cs
// Description: Phase 5 술병 CSV·컨트롤러·선택 UI를 GameRoot에 연결한다.
// History:     08/24/2026 + Created -- Codex
//              09/23/2026 + Museum Nested Prefab과 Module 경로 반영 -- Codex
//=============================================================================
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class MuseumPhase5PrefabIntegrator
{
    private const string GAME_ROOT_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";
    private const string UI_PREFAB_PATH = "Assets/_Game/Prefabs/UI/MuseumBottleUi.prefab";
    private const string BOTTLES_CSV_PATH = "Assets/_Game/Data/Csv/museum_bottles.csv";
    private const string ITEMS_CSV_PATH = "Assets/_Game/Data/Csv/museum_bottle_items.csv";
    private const int ITEM_BUTTON_COUNT = 12;

    static MuseumPhase5PrefabIntegrator() => EditorApplication.delayCall += Integrate;

    [MenuItem("Tools/Project Ruin/Museum/Integrate Phase 5 Into GameRoot")]
    internal static void Integrate()
    {
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI_PREFAB_PATH);
        if (uiPrefab == null) uiPrefab = BuildUiPrefab();
        TextAsset bottles = AssetDatabase.LoadAssetAtPath<TextAsset>(BOTTLES_CSV_PATH);
        TextAsset items = AssetDatabase.LoadAssetAtPath<TextAsset>(ITEMS_CSV_PATH);
        if (uiPrefab == null || bottles == null || items == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(GAME_ROOT_PATH) == null)
        {
            Debug.LogError("[MuseumPhase5PrefabIntegrator] 필수 프리팹 또는 CSV가 없습니다.");
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(GAME_ROOT_PATH);
        try
        {
            GameRoot root = contents.GetComponent<GameRoot>();
            MuseumBottleController controller =
                MuseumBootstrapPrefabUtility.GetOrCreateComponent<MuseumBottleController>(contents);
            if (controller == null) return;
            MuseumBottlePresenter presenter = contents.GetComponentInChildren<MuseumBottlePresenter>(true);
            if (presenter == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, contents.transform);
                instance.name = "MuseumBottleUi";
                presenter = instance.GetComponent<MuseumBottlePresenter>();
            }
            CodexUiElementFactory.SetReference(controller, "_museumBottlesCsv", bottles);
            CodexUiElementFactory.SetReference(controller, "_museumBottleItemsCsv", items);
            CodexUiElementFactory.SetReference(
                root, "_museumModule._museumBottleController", controller);
            CodexUiElementFactory.SetReference(
                root, "_uiModule._museumBottlePresenter", presenter);
            PrefabUtility.SaveAsPrefabAsset(contents, GAME_ROOT_PATH);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    [MenuItem("Tools/Project Ruin/Museum/Rebuild Phase 5 Bottle UI")]
    internal static void RebuildUi() => BuildUiPrefab();

    private static GameObject BuildUiPrefab()
    {
        GameObject root = CodexUiElementFactory.Create("MuseumBottleUi", null,
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.localScale = Vector3.one;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = CodexUiElementFactory.CreateImage(
            "Panel", root.transform, new Color32(239, 224, 190, 250));
        CodexUiElementFactory.SetRect(panel.GetComponent<RectTransform>(),
            Vector2.zero, new Vector2(900, 760), new Vector2(0.5f, 0.5f));
        GameObject title = CodexUiElementFactory.CreateText("Title", panel.transform,
            "술병", 34, TextAlignmentOptions.Center, new Vector2(450, 710), new Vector2(620, 48));
        GameObject progress = CodexUiElementFactory.CreateText("Progress", panel.transform,
            "0 / 0", 24, TextAlignmentOptions.Center, new Vector2(450, 655), new Vector2(240, 40));
        Button close = CodexUiElementFactory.CreateButton(
            "Close", panel.transform, "X", new Vector2(820, 690));

        GameObject fillBack = CodexUiElementFactory.CreateImage(
            "FillBack", panel.transform, new Color32(74, 55, 42, 255));
        CodexUiElementFactory.SetRect(fillBack.GetComponent<RectTransform>(),
            new Vector2(160, 615), new Vector2(580, 24), Vector2.zero);
        GameObject fill = CodexUiElementFactory.CreateImage(
            "Fill", fillBack.transform, new Color32(155, 61, 69, 255));
        CodexUiElementFactory.Stretch(fill.GetComponent<RectTransform>(), 2);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;

        GameObject list = CodexUiElementFactory.Create("ItemList", panel.transform,
            typeof(GridLayoutGroup));
        CodexUiElementFactory.SetRect(list.GetComponent<RectTransform>(),
            new Vector2(110, 105), new Vector2(680, 480), Vector2.zero);
        GridLayoutGroup grid = list.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(330, 68);
        grid.spacing = new Vector2(18, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        MuseumBottleItemButton[] itemButtons = new MuseumBottleItemButton[ITEM_BUTTON_COUNT];
        for (int i = 0; i < itemButtons.Length; i++)
            itemButtons[i] = CreateItemButton(list.transform, i);

        GameObject empty = CreateStatus(panel.transform, "Empty", "투입 가능한 술이 없습니다.");
        GameObject completed = CreateStatus(panel.transform, "Completed", "술병이 완성되었습니다.");
        MuseumBottleView view = root.AddComponent<MuseumBottleView>();
        MuseumBottlePresenter presenter = root.AddComponent<MuseumBottlePresenter>();
        BindView(view, panel, empty, completed, title.GetComponent<TMP_Text>(),
            progress.GetComponent<TMP_Text>(), fillImage, itemButtons, close);
        CodexUiElementFactory.SetReference(presenter, "_view", view);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, UI_PREFAB_PATH);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    private static MuseumBottleItemButton CreateItemButton(Transform parent, int index)
    {
        GameObject root = CodexUiElementFactory.CreateImage(
            $"Alcohol_{index + 1:00}", parent, new Color32(101, 67, 42, 255));
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        GameObject icon = CodexUiElementFactory.CreateImage("Icon", root.transform, Color.white);
        CodexUiElementFactory.SetRect(icon.GetComponent<RectTransform>(),
            new Vector2(8, 8), new Vector2(52, 52), Vector2.zero);
        GameObject name = CodexUiElementFactory.CreateText("Name", root.transform,
            "술", 20, TextAlignmentOptions.Left, new Vector2(72, 18), new Vector2(190, 38));
        GameObject count = CodexUiElementFactory.CreateText("Count", root.transform,
            "x0", 18, TextAlignmentOptions.Right, new Vector2(266, 18), new Vector2(54, 38));
        MuseumBottleItemButton item = root.AddComponent<MuseumBottleItemButton>();
        CodexUiElementFactory.SetReference(item, "_button", button);
        CodexUiElementFactory.SetReference(item, "_icon", icon.GetComponent<Image>());
        CodexUiElementFactory.SetReference(item, "_nameText", name.GetComponent<TMP_Text>());
        CodexUiElementFactory.SetReference(item, "_countText", count.GetComponent<TMP_Text>());
        return item;
    }

    private static GameObject CreateStatus(Transform parent, string name, string label)
    {
        GameObject root = CodexUiElementFactory.Create(name, parent);
        CodexUiElementFactory.SetRect(root.GetComponent<RectTransform>(),
            new Vector2(200, 330), new Vector2(500, 60), Vector2.zero);
        GameObject text = CodexUiElementFactory.CreateText(
            "Label", root.transform, label, 25, TextAlignmentOptions.Center);
        CodexUiElementFactory.Stretch(text.GetComponent<RectTransform>(), 0);
        return root;
    }

    private static void BindView(MuseumBottleView view, GameObject panel,
        GameObject empty, GameObject completed, TMP_Text title, TMP_Text progress,
        Image fill, MuseumBottleItemButton[] buttons, Button close)
    {
        CodexUiElementFactory.SetReference(view, "_panelRoot", panel);
        CodexUiElementFactory.SetReference(view, "_emptyRoot", empty);
        CodexUiElementFactory.SetReference(view, "_completedRoot", completed);
        CodexUiElementFactory.SetReference(view, "_titleText", title);
        CodexUiElementFactory.SetReference(view, "_progressText", progress);
        CodexUiElementFactory.SetReference(view, "_fillImage", fill);
        CodexUiElementFactory.SetReference(view, "_closeButton", close);
        SerializedObject serialized = new SerializedObject(view);
        SerializedProperty array = serialized.FindProperty("_itemButtons");
        array.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
