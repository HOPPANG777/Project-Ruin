//=============================================================================
// File:        InventoryUiPrefabBuilder.cs
// Description: 기존 인벤토리 Presenter/View를 사용하는 런타임 UI 프리팹을 생성한다.
// History:     08/23/2026 + Created -- Codex
//=============================================================================

using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class InventoryUiPrefabBuilder
{
    internal const string InventoryPath = "Assets/_Game/Prefabs/UI/InventoryBook.prefab";
    private const string SlotPath = "Assets/_Game/Prefabs/UI/InventorySlotView.prefab";
    private const string UiDirectory = "Assets/_Game/Prefabs/UI";

    static InventoryUiPrefabBuilder() => EditorApplication.delayCall += BuildIfMissing;

    [MenuItem("Tools/Project Ruin/Codex/Rebuild Inventory UI Prefabs")]
    internal static void Rebuild()
    {
        Build();
        Debug.Log("[InventoryUiPrefabBuilder] 인벤토리 UI 프리팹을 갱신했습니다.");
    }

    internal static void BuildIfMissing()
    {
        GameObject inventory = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPath);
        if (inventory == null || inventory.GetComponent<InventoryNavigationBridge>() == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath) == null) Build();
    }

    internal static void Build()
    {
        if (!Directory.Exists(UiDirectory)) Directory.CreateDirectory(UiDirectory);
        InventorySlotView slotPrefab = BuildSlotPrefab();
        BuildInventoryPrefab(slotPrefab);
        AssetDatabase.SaveAssets();
    }

    private static InventorySlotView BuildSlotPrefab()
    {
        GameObject root = CodexUiElementFactory.Create("InventorySlotView", null,
            typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(InventorySlotView));
        CodexUiElementFactory.SetRect(root.GetComponent<RectTransform>(), Vector2.zero,
            new Vector2(96, 96), Vector2.zero);
        Image background = root.GetComponent<Image>();
        background.color = new Color32(215, 199, 158, 255);
        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;
        GameObject selected = CodexUiElementFactory.CreateImage("Selected", root.transform,
            new Color32(188, 137, 42, 125));
        CodexUiElementFactory.Stretch(selected.GetComponent<RectTransform>(), 2);
        selected.GetComponent<Image>().raycastTarget = false;
        GameObject icon = CodexUiElementFactory.CreateImage("Icon", root.transform, Color.white);
        CodexUiElementFactory.SetRect(icon.GetComponent<RectTransform>(), new Vector2(8, 20),
            new Vector2(72, 72), Vector2.zero);
        icon.GetComponent<Image>().raycastTarget = false;
        GameObject quantity = CodexUiElementFactory.CreateText("Quantity", root.transform, "0",
            18, TextAlignmentOptions.BottomRight, new Vector2(48, 2), new Vector2(42, 26));
        quantity.GetComponent<TMP_Text>().raycastTarget = false;
        GameObject marker = CodexUiElementFactory.CreateText("New", root.transform, "NEW", 13,
            TextAlignmentOptions.TopLeft, new Vector2(4, 70), new Vector2(48, 22));
        marker.GetComponent<TMP_Text>().color = new Color32(160, 45, 35, 255);
        marker.GetComponent<TMP_Text>().raycastTarget = false;
        InventorySlotView view = root.GetComponent<InventorySlotView>();
        CodexUiElementFactory.SetReference(view, "_button", button);
        CodexUiElementFactory.SetReference(view, "_canvasGroup", root.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(view, "_iconImage", icon.GetComponent<Image>());
        CodexUiElementFactory.SetReference(view, "_quantityText", quantity.GetComponent<TMP_Text>());
        CodexUiElementFactory.SetReference(view, "_newMarkerRoot", marker);
        CodexUiElementFactory.SetReference(view, "_selectedMarkerRoot", selected);
        selected.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, SlotPath);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath).GetComponent<InventorySlotView>();
    }

    private static void BuildInventoryPrefab(InventorySlotView slotPrefab)
    {
        GameObject root = CreateCanvas();
        GameObject panel = CodexUiElementFactory.Create("InventoryRoot", root.transform,
            typeof(Image), typeof(CanvasGroup));
        CodexUiElementFactory.SetRect(panel.GetComponent<RectTransform>(), Vector2.zero,
            new Vector2(1500, 820), new Vector2(0.5f, 0.5f));
        panel.GetComponent<Image>().color = new Color32(72, 45, 28, 245);
        CanvasGroup rootGroup = panel.GetComponent<CanvasGroup>();
        rootGroup.alpha = 0;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;
        GameObject slotPage = CreatePage("SlotPage", panel.transform, new Vector2(28, 28));
        GameObject detailPage = CreatePage("DetailPage", panel.transform, new Vector2(760, 28));
        Button all = CreateCategory("All", slotPage.transform, "전체", 24);
        Button alcohol = CreateCategory("Alcohol", slotPage.transform, "술", 188);
        Button relic = CreateCategory("Relic", slotPage.transform, "유물", 352);
        Button misc = CreateCategory("Misc", slotPage.transform, "기타", 516);
        CodexUiScrollParts scroll = CodexUiElementFactory.CreateScroll("InventoryScroll",
            slotPage.transform, new Vector2(24, 28), new Vector2(664, 640), true);
        GridLayoutGroup grid = scroll.Content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(96, 96);
        grid.spacing = new Vector2(12, 12);
        grid.constraintCount = 6;
        Image detailIcon = CreateDetail(detailPage.transform, out TMP_Text detailName,
            out TMP_Text detailPrice, out TMP_Text detailDescription);
        Button close = CreateButton("CloseButton", panel.transform, "X",
            new Vector2(1428, 742), new Vector2(48, 48));
        Button map = CreateButton("MapButton", panel.transform, "<",
            new Vector2(12, 370), new Vector2(56, 80));
        Button codex = CreateButton("CodexButton", panel.transform, ">",
            new Vector2(1432, 370), new Vector2(56, 80));
        map.interactable = false;
        Button hud = CreateButton("InventoryHudButton", root.transform, "인벤토리",
            new Vector2(-218, -32), new Vector2(170, 52), Vector2.one);
        Bind(root, panel, slotPage, detailPage, scroll, grid, slotPrefab, detailIcon,
            detailName, detailPrice, detailDescription, hud, close, all, alcohol, relic, misc, codex);
        PrefabUtility.SaveAsPrefabAsset(root, InventoryPath);
        Object.DestroyImmediate(root);
    }

    private static GameObject CreateCanvas()
    {
        GameObject root = CodexUiElementFactory.Create("InventoryBook", null, typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InventoryView),
            typeof(InventoryPresenter), typeof(InventoryNavigationBridge));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 79;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static GameObject CreatePage(string name, Transform parent, Vector2 position)
    {
        GameObject page = CodexUiElementFactory.Create(name, parent, typeof(Image), typeof(CanvasGroup));
        CodexUiElementFactory.SetRect(page.GetComponent<RectTransform>(), position,
            new Vector2(712, 764), Vector2.zero);
        page.GetComponent<Image>().color = new Color32(239, 227, 190, 255);
        return page;
    }

    private static Button CreateCategory(string name, Transform parent, string label, float x)
        => CreateButton(name + "Category", parent, label, new Vector2(x, 690), new Vector2(140, 50));

    private static Button CreateButton(string name, Transform parent, string label,
        Vector2 position, Vector2 size, Vector2? anchor = null)
    {
        Button button = CodexUiElementFactory.CreateButton(name, parent, label, Vector2.zero);
        CodexUiElementFactory.SetRect(button.GetComponent<RectTransform>(), position, size,
            anchor ?? Vector2.zero);
        return button;
    }

    private static Image CreateDetail(Transform parent, out TMP_Text name, out TMP_Text price,
        out TMP_Text description)
    {
        CodexUiElementFactory.CreateText("Title", parent, "아이템 상세", 32,
            TextAlignmentOptions.Center, new Vector2(24, 690), new Vector2(664, 48));
        GameObject icon = CodexUiElementFactory.CreateImage("Icon", parent, Color.white);
        CodexUiElementFactory.SetRect(icon.GetComponent<RectTransform>(), new Vector2(48, 500),
            new Vector2(180, 180), Vector2.zero);
        name = CodexUiElementFactory.CreateText("Name", parent, string.Empty, 28,
            TextAlignmentOptions.Left, new Vector2(260, 610), new Vector2(400, 56)).GetComponent<TMP_Text>();
        price = CodexUiElementFactory.CreateText("Price", parent, string.Empty, 22,
            TextAlignmentOptions.Left, new Vector2(260, 540), new Vector2(400, 48)).GetComponent<TMP_Text>();
        description = CodexUiElementFactory.CreateText("Description", parent, string.Empty, 20,
            TextAlignmentOptions.TopLeft, new Vector2(48, 80), new Vector2(612, 380)).GetComponent<TMP_Text>();
        return icon.GetComponent<Image>();
    }

    private static void Bind(GameObject root, GameObject panel, GameObject slotPage,
        GameObject detailPage, CodexUiScrollParts scroll, GridLayoutGroup grid,
        InventorySlotView slotPrefab, Image icon, TMP_Text name, TMP_Text price,
        TMP_Text description, Button hud, Button close, Button all, Button alcohol,
        Button relic, Button misc, Button codex)
    {
        InventoryView view = root.GetComponent<InventoryView>();
        CodexUiElementFactory.SetReference(view, "_rootCanvasGroup", panel.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(view, "_slotPageCanvasGroup", slotPage.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(view, "_detailPageCanvasGroup", detailPage.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(view, "_slotScrollRect", scroll.ScrollRect);
        CodexUiElementFactory.SetReference(view, "_slotContentRoot", scroll.Content);
        CodexUiElementFactory.SetReference(view, "_slotGridLayout", grid);
        CodexUiElementFactory.SetReference(view, "_slotViewPrefab", slotPrefab);
        CodexUiElementFactory.SetReference(view, "_selectedItemIconImage", icon);
        CodexUiElementFactory.SetReference(view, "_selectedItemNameText", name);
        CodexUiElementFactory.SetReference(view, "_selectedItemPriceText", price);
        CodexUiElementFactory.SetReference(view, "_selectedItemDescriptionText", description);
        InventoryPresenter presenter = root.GetComponent<InventoryPresenter>();
        CodexUiElementFactory.SetReference(presenter, "_inventoryView", view);
        CodexUiElementFactory.SetReference(presenter, "_popupPanelRoot", panel.GetComponent<RectTransform>());
        CodexUiElementFactory.SetReference(presenter, "_hudOpenButton", hud);
        CodexUiElementFactory.SetReference(presenter, "_closeButton", close);
        CodexUiElementFactory.SetReference(presenter, "_allCategoryButton", all);
        CodexUiElementFactory.SetReference(presenter, "_alcoholCategoryButton", alcohol);
        CodexUiElementFactory.SetReference(presenter, "_relicCategoryButton", relic);
        CodexUiElementFactory.SetReference(presenter, "_miscCategoryButton", misc);
        InventoryNavigationBridge bridge = root.GetComponent<InventoryNavigationBridge>();
        CodexUiElementFactory.SetReference(bridge, "_presenter", presenter);
        CodexUiElementFactory.SetReference(bridge, "_rootCanvasGroup", panel.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(bridge, "_codexButton", codex);
    }
}
