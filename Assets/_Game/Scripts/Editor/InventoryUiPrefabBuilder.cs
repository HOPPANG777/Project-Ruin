//=============================================================================
// File:        InventoryUiPrefabBuilder.cs
// Description: 기존 인벤토리 Presenter/View를 사용하는 런타임 UI 프리팹을 생성한다.
// History:     08/23/2026 + Created -- Codex
//=============================================================================

using System.Collections.Generic;
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
    private const string SpriteDirectory = "Assets/_Game/Sprites/UISprite/InventroyUISprite/";
    private const string SlotSpritePath = SpriteDirectory + "Inventory_FrontSlotUI 1.png";

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
            new Vector2(100, 90), Vector2.zero);
        Image background = root.GetComponent<Image>();
        Sprite[] slotSprites = LoadInventorySlotSprites();
        background.sprite = slotSprites.Length > 0 ? slotSprites[0] : null;
        background.color = Color.white;
        background.preserveAspect = true;
        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;
        GameObject selected = CodexUiElementFactory.CreateImage("Selected", root.transform,
            new Color32(188, 137, 42, 125));
        CodexUiElementFactory.Stretch(selected.GetComponent<RectTransform>(), 2);
        selected.GetComponent<Image>().raycastTarget = false;
        GameObject icon = CodexUiElementFactory.CreateImage("Icon", root.transform, Color.white);
        CodexUiElementFactory.SetRect(icon.GetComponent<RectTransform>(), new Vector2(15, 10),
            new Vector2(70, 70), Vector2.zero);
        icon.GetComponent<Image>().raycastTarget = false;
        GameObject quantity = CodexUiElementFactory.CreateText("Quantity", root.transform, "0",
            18, TextAlignmentOptions.BottomRight, new Vector2(48, 2), new Vector2(42, 26));
        quantity.GetComponent<TMP_Text>().raycastTarget = false;
        GameObject marker = CodexUiElementFactory.CreateText("New", root.transform, "NEW", 13,
            TextAlignmentOptions.TopLeft, new Vector2(4, 66), new Vector2(48, 22));
        marker.GetComponent<TMP_Text>().color = new Color32(160, 45, 35, 255);
        marker.GetComponent<TMP_Text>().raycastTarget = false;
        InventorySlotView view = root.GetComponent<InventorySlotView>();
        CodexUiElementFactory.SetReference(view, "_button", button);
        CodexUiElementFactory.SetReference(view, "_canvasGroup", root.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(view, "_backgroundImage", background);
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
            new Vector2(1920, 1080), new Vector2(0.5f, 0.5f));
        panel.GetComponent<Image>().sprite = LoadInventorySprite("InventoryUI");
        panel.GetComponent<Image>().preserveAspect = true;
        CanvasGroup rootGroup = panel.GetComponent<CanvasGroup>();
        rootGroup.alpha = 1;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;
        GameObject cover = CodexUiElementFactory.Create("InventoryCover", panel.transform);
        CodexUiElementFactory.SetRect(cover.GetComponent<RectTransform>(), Vector2.zero,
            new Vector2(100, 100), new Vector2(0.5f, 0.5f));
        CreateSprite("InventoryRibborn", cover.transform, "Inventory_RibbonUI",
            new Vector2(-718, -80), new Vector2(297, 158), new Vector2(0.5f, 0.5f));
        CreateSprite("InventoryFrontCover", cover.transform, "Inventory_FrontCoverUI",
            new Vector2(-375, -75), new Vector2(660, 740), new Vector2(0.5f, 0.5f));
        CreateSprite("InventoryBackCover", cover.transform, "Inventory_BackCoverUI",
            new Vector2(320, -75), new Vector2(660, 740), new Vector2(0.5f, 0.5f));

        GameObject slotPage = CreatePage("SlotPage", panel.transform,
            "Inventory_FrontPageUI", new Vector2(310, 110), new Vector2(640, 740));
        GameObject detailPage = CreatePage("DetailPage", panel.transform,
            "Inventory_BackPageUI", new Vector2(938, 95), new Vector2(662, 740));
        Button all = CreateCategory("All", slotPage.transform, "Inventory_StrapUI", -118, 538);
        Button quest = CreateCategory("Quest", slotPage.transform, "Inventory_Strap2UI", -80, 453);
        Button alcohol = CreateCategory("Alcohol", slotPage.transform, "Inventory_Strap3UI", -80, 368);
        Button relic = CreateCategory("Relic", slotPage.transform, "Inventory_Strap4UI", -80, 283);
        Button misc = CreateCategory("Misc", slotPage.transform, "Inventory_Strap5UI", -80, 198);
        ConfigureCategorySorting(root.GetComponent<Canvas>(), slotPage,
            all, quest, alcohol, relic, misc);
        CodexUiScrollParts scroll = CodexUiElementFactory.CreateScroll("InventoryScroll",
            slotPage.transform, new Vector2(43, 36), new Vector2(530, 660), true);
        scroll.ScrollRect.GetComponent<Image>().color = Color.clear;
        scroll.ScrollRect.scrollSensitivity = 35.0f;
        // 프리팹에서 조정한 스크롤바와 스프라이트를 재생성 시 그대로 유지합니다.
        GameObject existingInventory = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPath);
        Scrollbar authoredScrollbar = existingInventory?.transform.Find(
            "InventoryRoot/SlotPage/InventoryScroll/InventoryVerticalScrollbar")?.GetComponent<Scrollbar>();
        if (authoredScrollbar != null)
        {
            Scrollbar scrollbar = Object.Instantiate(authoredScrollbar, scroll.ScrollRect.transform, false);
            scrollbar.gameObject.name = authoredScrollbar.gameObject.name;
            scroll.ScrollRect.verticalScrollbar = scrollbar;
        }
        GridLayoutGroup grid = scroll.Content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100, 91);
        grid.spacing = new Vector2(6, 3);
        grid.constraintCount = 5;
        GameObject spine = CreateSprite("InventorySpring", panel.transform, "Inventory_BookSpineUI",
            new Vector2(-15, -75), new Vector2(70, 565), new Vector2(0.5f, 0.5f));
        Canvas spineCanvas = spine.AddComponent<Canvas>();
        spineCanvas.overrideSorting = true;
        spineCanvas.sortingOrder = root.GetComponent<Canvas>().sortingOrder + 9;
        Image detailIcon = CreateDetail(detailPage.transform, out TMP_Text detailName,
            out TMP_Text detailPrice, out TMP_Text detailDescription);
        Button close = CreateButton("CloseButton", panel.transform, string.Empty,
            new Vector2(1840, 1000), new Vector2(48, 48));
        close.GetComponent<Image>().color = Color.clear;
        Button map = CreateButton("MapButton", panel.transform, string.Empty,
            new Vector2(120, 470), new Vector2(100, 100));
        map.GetComponent<Image>().sprite = LoadInventorySprite("Arrow_Left_Icon");
        map.GetComponent<Image>().color = Color.white;
        Button codex = CreateButton("CodexButton", panel.transform, string.Empty,
            new Vector2(1642, 470), new Vector2(100, 100));
        codex.GetComponent<Image>().sprite = LoadInventorySprite("Arrow_Right_Icon");
        codex.GetComponent<Image>().color = Color.white;
        map.interactable = false;
        ColorBlock mapColors = map.colors;
        mapColors.disabledColor = Color.white;
        map.colors = mapColors;
        Button hud = CreateButton("InventoryHudButton", root.transform, string.Empty,
            new Vector2(170, -200), new Vector2(120, 120), new Vector2(0, 1));
        hud.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
        hud.transform.SetAsFirstSibling();
        hud.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Game/Sprites/UISprite/HUD/HUD_Inventory.png");
        hud.GetComponent<Image>().color = Color.white;
        Bind(root, panel, slotPage, detailPage, scroll, grid, slotPrefab, detailIcon,
            detailName, detailPrice, detailDescription, hud, close, all, quest,
            alcohol, relic, misc, codex);
        PrefabUtility.SaveAsPrefabAsset(root, InventoryPath);
        Object.DestroyImmediate(root);
    }

    private static GameObject CreateCanvas()
    {
        GameObject root = CodexUiElementFactory.Create("InventoryBook", null, typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InventoryView),
            typeof(InventoryPresenter), typeof(InventoryNavigationBridge));
        root.transform.localScale = Vector3.one;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 81;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static Sprite LoadInventorySprite(string name)
        => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDirectory + name + ".png");

    private static Sprite[] LoadInventorySlotSprites()
    {
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(SlotSpritePath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort((left, right) => GetSlotSpriteIndex(left).CompareTo(GetSlotSpriteIndex(right)));
        return sprites.ToArray();
    }

    private static int GetSlotSpriteIndex(Sprite sprite)
    {
        int suffixStart = sprite.name.LastIndexOf('_') + 1;
        return int.TryParse(sprite.name.Substring(suffixStart), out int index) ? index : int.MaxValue;
    }

    private static GameObject CreateSprite(string name, Transform parent, string spriteName,
        Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject imageObject = CodexUiElementFactory.CreateImage(name, parent, Color.white);
        CodexUiElementFactory.SetRect(imageObject.GetComponent<RectTransform>(), position, size, anchor);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = LoadInventorySprite(spriteName);
        image.raycastTarget = false;
        return imageObject;
    }

    private static GameObject CreatePage(string name, Transform parent, string spriteName,
        Vector2 position, Vector2 size)
    {
        GameObject page = CodexUiElementFactory.Create(name, parent, typeof(Image), typeof(CanvasGroup));
        CodexUiElementFactory.SetRect(page.GetComponent<RectTransform>(), position, size, Vector2.zero);
        page.GetComponent<Image>().sprite = LoadInventorySprite(spriteName);
        return page;
    }

    private static Button CreateCategory(string name, Transform parent, string spriteName,
        float x, float y)
    {
        Button category = CreateButton(name + "Category", parent, string.Empty,
            new Vector2(x, y), new Vector2(130, 80));
        category.GetComponent<Image>().sprite = LoadInventorySprite(spriteName);
        category.GetComponent<Image>().color = Color.white;
        category.transition = Selectable.Transition.None;
        return category;
    }

    private static void ConfigureCategorySorting(Canvas rootCanvas, GameObject slotPage,
        params Button[] categories)
    {
        // 분류 버튼은 커버보다 앞, 페이지보다 뒤에 그립니다.
        Canvas pageCanvas = slotPage.AddComponent<Canvas>();
        pageCanvas.overrideSorting = true;
        pageCanvas.sortingOrder = rootCanvas.sortingOrder + 2;
        pageCanvas.additionalShaderChannels = rootCanvas.additionalShaderChannels;
        slotPage.AddComponent<GraphicRaycaster>();
        slotPage.GetComponent<Image>().raycastTarget = false;

        foreach (Button category in categories)
        {
            Canvas categoryCanvas = category.gameObject.AddComponent<Canvas>();
            categoryCanvas.overrideSorting = true;
            categoryCanvas.sortingOrder = rootCanvas.sortingOrder + 1;
            categoryCanvas.additionalShaderChannels = rootCanvas.additionalShaderChannels;
            category.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

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
        CodexUiElementFactory.CreateText("Title", parent, string.Empty, 32,
            TextAlignmentOptions.Center, new Vector2(20, 665), new Vector2(610, 48));
        GameObject icon = CodexUiElementFactory.CreateImage("Icon", parent, Color.white);
        CodexUiElementFactory.SetRect(icon.GetComponent<RectTransform>(), new Vector2(40, 480),
            new Vector2(160, 160), Vector2.zero);
        icon.GetComponent<Image>().enabled = false;
        name = CodexUiElementFactory.CreateText("Name", parent, string.Empty, 28,
            TextAlignmentOptions.Left, new Vector2(220, 590), new Vector2(390, 54)).GetComponent<TMP_Text>();
        price = CodexUiElementFactory.CreateText("Price", parent, string.Empty, 22,
            TextAlignmentOptions.Left, new Vector2(220, 520), new Vector2(390, 48)).GetComponent<TMP_Text>();
        description = CodexUiElementFactory.CreateText("Description", parent, string.Empty, 20,
            TextAlignmentOptions.TopLeft, new Vector2(40, 75), new Vector2(570, 350)).GetComponent<TMP_Text>();
        return icon.GetComponent<Image>();
    }

    private static void Bind(GameObject root, GameObject panel, GameObject slotPage,
        GameObject detailPage, CodexUiScrollParts scroll, GridLayoutGroup grid,
        InventorySlotView slotPrefab, Image icon, TMP_Text name, TMP_Text price,
        TMP_Text description, Button hud, Button close, Button all, Button quest,
        Button alcohol, Button relic, Button misc, Button codex)
    {
        InventoryView view = root.GetComponent<InventoryView>();
        CodexUiElementFactory.SetReference(view, "_rootCanvasGroup", panel.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(view, "_slotPageCanvasGroup", slotPage.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(view, "_detailPageCanvasGroup", detailPage.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(view, "_slotScrollRect", scroll.ScrollRect);
        CodexUiElementFactory.SetReference(view, "_slotContentRoot", scroll.Content);
        CodexUiElementFactory.SetReference(view, "_slotGridLayout", grid);
        CodexUiElementFactory.SetReference(view, "_slotViewPrefab", slotPrefab);
        Sprite[] slotSprites = LoadInventorySlotSprites();
        SerializedObject serializedView = new SerializedObject(view);
        SerializedProperty backgrounds = serializedView.FindProperty("_slotBackgroundSprites");
        backgrounds.arraySize = slotSprites.Length;
        for (int i = 0; i < slotSprites.Length; i++)
        {
            backgrounds.GetArrayElementAtIndex(i).objectReferenceValue = slotSprites[i];
        }
        serializedView.ApplyModifiedPropertiesWithoutUndo();
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
        CodexUiElementFactory.SetReference(presenter, "_questCategoryButton", quest);
        CodexUiElementFactory.SetReference(presenter, "_alcoholCategoryButton", alcohol);
        CodexUiElementFactory.SetReference(presenter, "_relicCategoryButton", relic);
        CodexUiElementFactory.SetReference(presenter, "_miscCategoryButton", misc);
        InventoryNavigationBridge bridge = root.GetComponent<InventoryNavigationBridge>();
        CodexUiElementFactory.SetReference(bridge, "_presenter", presenter);
        CodexUiElementFactory.SetReference(bridge, "_rootCanvasGroup", panel.GetComponent<CanvasGroup>());
        CodexUiElementFactory.SetReference(bridge, "_codexButton", codex);
    }
}
