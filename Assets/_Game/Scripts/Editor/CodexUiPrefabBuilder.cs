//=============================================================================
// File:        CodexUiPrefabBuilder.cs
// Description: Phase 2~3 도감 UI 프리팹과 공유 흑백 Material 생성을 조율한다.
// History:     08/22/2026 + Created -- Codex
//              08/22/2026 + UI 요소 생성 책임 분리 -- Codex
//              08/22/2026 + Phase 3 입력 및 전환 UI 연결 -- Codex
//              08/23/2026 + GUID 보존 재생성과 기존 Phase 2 자산 마이그레이션 -- Codex
//=============================================================================

using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CodexUiPrefabBuilder
{
    private const string UI_DIRECTORY = "Assets/_Game/Prefabs/UI";
    private const string ENTRY_PATH = UI_DIRECTORY + "/CodexEntryView.prefab";
    private const string BOOK_PATH = UI_DIRECTORY + "/CodexBook.prefab";
    private const string MATERIAL_PATH = UI_DIRECTORY + "/CodexUiGrayscale.mat";
    private const string SHADER_PATH = "Assets/_Game/Shaders/CodexUiGrayscale.shader";

    static CodexUiPrefabBuilder() => EditorApplication.delayCall += BuildIfMissing;

    [MenuItem("Tools/Project Ruin/Codex/Rebuild Codex UI Prefabs")]
    public static void Rebuild() => Build();

    private static void BuildIfMissing()
    {
        GameObject book = AssetDatabase.LoadAssetAtPath<GameObject>(BOOK_PATH);
        if (book == null || book.GetComponent<CodexNavigationPresenter>() == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(ENTRY_PATH) == null) Build();
    }

    private static void Build()
    {
        if (!Directory.Exists(UI_DIRECTORY)) Directory.CreateDirectory(UI_DIRECTORY);
        InventoryUiPrefabBuilder.Build();
        Material grayscale = GetOrCreateGrayscaleMaterial();
        CodexEntryView entryPrefab = BuildEntryPrefab();
        BuildBookPrefab(entryPrefab, grayscale);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GameUiPrefabIntegrator.Integrate();
        Debug.Log("[CodexUiPrefabBuilder] Phase 2~3 도감 UI 프리팹을 갱신했습니다.");
    }

    private static CodexEntryView BuildEntryPrefab()
    {
        GameObject root = CodexUiElementFactory.Create("CodexEntryView", null,
            typeof(Image), typeof(LayoutElement), typeof(CodexEntryView));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(112.0f, 112.0f);
        root.GetComponent<Image>().color = new Color32(222, 207, 170, 255);
        GameObject selected = CodexUiElementFactory.CreateImage("Selected", root.transform,
            new Color32(188, 137, 42, 160));
        CodexUiElementFactory.Stretch(selected.GetComponent<RectTransform>(), -5.0f);
        GameObject icon = CodexUiElementFactory.CreateImage("Icon", root.transform, Color.white);
        CodexUiElementFactory.SetRect(icon.GetComponent<RectTransform>(), new Vector2(8, 26),
            new Vector2(78, 78), Vector2.zero);
        GameObject name = CodexUiElementFactory.CreateText("Name", root.transform, "???", 18.0f,
            TextAlignmentOptions.Center);
        CodexUiElementFactory.SetRect(name.GetComponent<RectTransform>(), new Vector2(4, 2),
            new Vector2(104, 24), Vector2.zero);
        GameObject ingredients = CreateIngredientsRoot(root.transform);
        CodexEntryView view = root.GetComponent<CodexEntryView>();
        CodexUiElementFactory.SetReference(view, "_layoutElement", root.GetComponent<LayoutElement>());
        CodexUiElementFactory.SetReference(view, "_iconImage", icon.GetComponent<Image>());
        CodexUiElementFactory.SetReference(view, "_nameText", name.GetComponent<TMP_Text>());
        CodexUiElementFactory.SetReference(view, "_selectedRoot", selected);
        CodexUiElementFactory.SetReference(view, "_ingredientsRoot", ingredients);
        CodexUiElementFactory.SetReference(view, "_ingredientContentRoot",
            ingredients.GetComponent<RectTransform>());
        PrefabUtility.SaveAsPrefabAsset(root, ENTRY_PATH);
        Object.DestroyImmediate(root);
        LinkIngredientPrefab();
        return AssetDatabase.LoadAssetAtPath<GameObject>(ENTRY_PATH).GetComponent<CodexEntryView>();
    }

    private static GameObject CreateIngredientsRoot(Transform parent)
    {
        GameObject ingredients = CodexUiElementFactory.Create("Ingredients", parent);
        CodexUiElementFactory.SetRect(ingredients.GetComponent<RectTransform>(), new Vector2(120, 24),
            new Vector2(360, 80), Vector2.zero);
        HorizontalLayoutGroup layout = ingredients.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8.0f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return ingredients;
    }

    private static void LinkIngredientPrefab()
    {
        CodexEntryView assetView = AssetDatabase.LoadAssetAtPath<GameObject>(ENTRY_PATH)
            .GetComponent<CodexEntryView>();
        GameObject contents = PrefabUtility.LoadPrefabContents(ENTRY_PATH);
        CodexUiElementFactory.SetReference(contents.GetComponent<CodexEntryView>(),
            "_ingredientViewPrefab", assetView);
        PrefabUtility.SaveAsPrefabAsset(contents, ENTRY_PATH);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void BuildBookPrefab(CodexEntryView entryPrefab, Material grayscale)
    {
        GameObject root = CreateCanvas();
        GameObject book = CodexUiElementFactory.Create("BookRoot", root.transform,
            typeof(Image), typeof(CanvasGroup));
        CodexUiElementFactory.SetRect(book.GetComponent<RectTransform>(), Vector2.zero,
            new Vector2(1500, 820), new Vector2(0.5f, 0.5f));
        book.GetComponent<Image>().color = new Color32(72, 45, 28, 245);
        GameObject left = CreatePage("LeftPage", book.transform, new Vector2(28, 28));
        GameObject right = CreatePage("RightPage", book.transform, new Vector2(760, 28));
        Button relicTab = CodexUiElementFactory.CreateButton("RelicTab", left.transform,
            "유물", new Vector2(24, 690));
        Button alcoholTab = CodexUiElementFactory.CreateButton("AlcoholTab", left.transform,
            "술", new Vector2(190, 690));
        GameObject relicSelected = CreateSelection(relicTab.transform);
        GameObject alcoholSelected = CreateSelection(alcoholTab.transform);
        CodexUiScrollParts relic = CodexUiElementFactory.CreateScroll("RelicScroll", left.transform,
            new Vector2(24, 28), new Vector2(664, 640), true);
        CodexUiScrollParts alcohol = CodexUiElementFactory.CreateScroll("AlcoholScroll", left.transform,
            new Vector2(24, 28), new Vector2(664, 640), false);
        CodexUiElementFactory.CreateText("TechnologyTitle", right.transform, "기술", 32.0f,
            TextAlignmentOptions.Center, new Vector2(0, 700), new Vector2(712, 48));
        CodexUiScrollParts technology = CodexUiElementFactory.CreateScroll("TechnologyScroll",
            right.transform, new Vector2(24, 28), new Vector2(664, 650), true);
        BindBook(root, book, relicTab, alcoholTab, relicSelected, alcoholSelected,
            relic, alcohol, technology, entryPrefab, grayscale);
        PrefabUtility.SaveAsPrefabAsset(root, BOOK_PATH);
        Object.DestroyImmediate(root);
    }

    private static GameObject CreateCanvas()
    {
        GameObject root = CodexUiElementFactory.Create("CodexBook", null, typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CodexView),
            typeof(CodexPresenter), typeof(CodexNavigationPresenter));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static GameObject CreatePage(string name, Transform parent, Vector2 position)
    {
        GameObject page = CodexUiElementFactory.CreateImage(name, parent,
            new Color32(239, 227, 190, 255));
        CodexUiElementFactory.SetRect(page.GetComponent<RectTransform>(), position,
            new Vector2(712, 764), Vector2.zero);
        return page;
    }

    private static GameObject CreateSelection(Transform parent)
    {
        GameObject selected = CodexUiElementFactory.CreateImage("Selected", parent,
            new Color32(188, 137, 42, 90));
        CodexUiElementFactory.Stretch(selected.GetComponent<RectTransform>(), 0);
        return selected;
    }

    private static void BindBook(GameObject root, GameObject book, Button relicTab,
        Button alcoholTab, GameObject relicSelected, GameObject alcoholSelected,
        CodexUiScrollParts relic, CodexUiScrollParts alcohol, CodexUiScrollParts technology,
        CodexEntryView entryPrefab, Material grayscale)
    {
        CodexTooltipParts tooltip = CodexUiElementFactory.CreateTooltip(book.transform);
        CodexView view = root.GetComponent<CodexView>();
        CodexUiElementFactory.BindView(view, book, relicTab, alcoholTab, relicSelected,
            alcoholSelected, relic, alcohol, technology, entryPrefab, grayscale, tooltip);
        CodexPresenter presenter = root.GetComponent<CodexPresenter>();
        CodexUiElementFactory.SetReference(presenter, "_view", view);
        CodexUiElementFactory.SetReference(presenter, "_visualCatalog",
            AssetDatabase.LoadAssetAtPath<ItemVisualCatalogSO>(
                "Assets/_Game/Data/InventoryData/ItemVisualCatalog.asset"));
        CodexPhase3PrefabParts.Bind(root, book, presenter);
    }

    private static Material GetOrCreateGrayscaleMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (material != null) return material;
        AssetDatabase.ImportAsset(SHADER_PATH, ImportAssetOptions.ForceSynchronousImport);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
        if (shader == null) return null;
        material = new Material(shader) { name = "CodexUiGrayscale" };
        AssetDatabase.CreateAsset(material, MATERIAL_PATH);
        return material;
    }
}
