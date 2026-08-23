//=============================================================================
// File:        CodexUiElementFactory.cs
// Description: 도감 프리팹 Builder가 사용하는 공통 UI 요소와 참조를 생성한다.
// History:     08/22/2026 + Created -- Codex
//=============================================================================

using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal sealed class CodexUiScrollParts
{
    internal ScrollRect ScrollRect;
    internal RectTransform Content;
}

internal sealed class CodexTooltipParts
{
    internal RectTransform Root;
    internal Image Icon;
    internal TMP_Text Name;
    internal TMP_Text Description;
    internal TMP_Text Secondary;
}

internal static class CodexUiElementFactory
{
    private const string BazziFontPath = "Assets/TextMesh Pro/Fonts/Bazzi SDF.asset";
    private static TMP_FontAsset bazziFont;

    internal static CodexUiScrollParts CreateScroll(string name, Transform parent,
        Vector2 position, Vector2 size, bool grid)
    {
        GameObject root = Create(name, parent, typeof(Image), typeof(ScrollRect));
        SetRect(root.GetComponent<RectTransform>(), position, size, Vector2.zero);
        root.GetComponent<Image>().color = new Color(1, 1, 1, 0.04f);
        GameObject viewport = Create("Viewport", root.transform, typeof(Image), typeof(RectMask2D));
        Stretch(viewport.GetComponent<RectTransform>(), 0);
        viewport.GetComponent<Image>().color = Color.clear;
        GameObject content = Create("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 1);
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        AddLayout(content, name, grid);
        ScrollRect scroll = root.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return new CodexUiScrollParts { ScrollRect = scroll, Content = contentRect };
    }

    internal static CodexTooltipParts CreateTooltip(Transform parent)
    {
        GameObject root = CreateImage("Tooltip", parent, new Color32(42, 31, 23, 248));
        SetRect(root.GetComponent<RectTransform>(), Vector2.zero, new Vector2(420, 250), Vector2.zero);
        GameObject icon = CreateImage("Icon", root.transform, Color.white);
        SetRect(icon.GetComponent<RectTransform>(), new Vector2(18, 154),
            new Vector2(78, 78), Vector2.zero);
        GameObject name = CreateText("Name", root.transform, "???", 24.0f,
            TextAlignmentOptions.Left, new Vector2(112, 190), new Vector2(286, 42));
        GameObject description = CreateText("Description", root.transform, "?", 18.0f,
            TextAlignmentOptions.TopLeft, new Vector2(18, 54), new Vector2(380, 126));
        GameObject secondary = CreateText("Secondary", root.transform, string.Empty, 16.0f,
            TextAlignmentOptions.TopLeft, new Vector2(18, 14), new Vector2(380, 36));
        return new CodexTooltipParts
        {
            Root = root.GetComponent<RectTransform>(),
            Icon = icon.GetComponent<Image>(),
            Name = name.GetComponent<TMP_Text>(),
            Description = description.GetComponent<TMP_Text>(),
            Secondary = secondary.GetComponent<TMP_Text>()
        };
    }

    internal static void BindView(CodexView view, GameObject book, Button relicTab,
        Button alcoholTab, GameObject relicSelected, GameObject alcoholSelected,
        CodexUiScrollParts relic, CodexUiScrollParts alcohol, CodexUiScrollParts technology,
        CodexEntryView entryPrefab, Material grayscale, CodexTooltipParts tooltip)
    {
        SetReference(view, "_rootCanvasGroup", book.GetComponent<CanvasGroup>());
        SetReference(view, "_relicTabButton", relicTab);
        SetReference(view, "_alcoholTabButton", alcoholTab);
        SetReference(view, "_relicTabSelectedRoot", relicSelected);
        SetReference(view, "_alcoholTabSelectedRoot", alcoholSelected);
        SetReference(view, "_relicScrollRect", relic.ScrollRect);
        SetReference(view, "_alcoholScrollRect", alcohol.ScrollRect);
        SetReference(view, "_technologyScrollRect", technology.ScrollRect);
        SetReference(view, "_entryViewPrefab", entryPrefab);
        SetReference(view, "_lockedTechnologyMaterial", grayscale);
        SetReference(view, "_tooltipRoot", tooltip.Root);
        SetReference(view, "_tooltipBounds", book.GetComponent<RectTransform>());
        SetReference(view, "_tooltipIconImage", tooltip.Icon);
        SetReference(view, "_tooltipNameText", tooltip.Name);
        SetReference(view, "_tooltipDescriptionText", tooltip.Description);
        SetReference(view, "_tooltipSecondaryText", tooltip.Secondary);
    }

    internal static Button CreateButton(string name, Transform parent,
        string label, Vector2 position)
    {
        GameObject root = CreateImage(name, parent, new Color32(101, 67, 42, 255));
        SetRect(root.GetComponent<RectTransform>(), position, new Vector2(150, 50), Vector2.zero);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        CreateText("Label", root.transform, label, 24.0f, TextAlignmentOptions.Center);
        Stretch(root.transform.GetChild(0).GetComponent<RectTransform>(), 0);
        return button;
    }

    internal static GameObject CreateText(string name, Transform parent, string value,
        float size, TextAlignmentOptions alignment, Vector2? position = null,
        Vector2? dimensions = null)
    {
        GameObject root = Create(name, parent, typeof(TextMeshProUGUI));
        TMP_Text text = root.GetComponent<TMP_Text>();
        text.text = value;
        text.font = GetBazziFont();
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color32(62, 45, 31, 255);
        text.textWrappingMode = TextWrappingModes.Normal;
        if (position.HasValue)
            SetRect(root.GetComponent<RectTransform>(), position.Value,
                dimensions ?? new Vector2(200, 40), Vector2.zero);
        return root;
    }

    internal static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject root = Create(name, parent, typeof(Image));
        root.GetComponent<Image>().color = color;
        return root;
    }

    internal static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        if (parent != null) root.transform.SetParent(parent, false);
        for (int i = 0; i < components.Length; i++) root.AddComponent(components[i]);
        return root;
    }

    internal static void SetRect(RectTransform rect, Vector2 position,
        Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    internal static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    internal static void SetReference(UnityEngine.Object target, string property,
        UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_FontAsset GetBazziFont()
    {
        if (bazziFont == null)
            bazziFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BazziFontPath);
        return bazziFont;
    }

    private static void AddLayout(GameObject content, string name, bool grid)
    {
        if (grid)
        {
            GridLayoutGroup layout = content.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(112, 112);
            layout.spacing = new Vector2(18, 18);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = name.StartsWith("Technology", StringComparison.Ordinal) ? 5 : 4;
            return;
        }
        VerticalLayoutGroup vertical = content.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 12;
        vertical.childControlWidth = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
    }
}
