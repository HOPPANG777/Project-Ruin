//=============================================================================
// File:        CodexView.cs
// Description: 펼친 책 2P 도감과 목록 풀 및 단일 Tooltip을 표시한다.
// History:     08/22/2026 + Created -- Codex
//=============================================================================

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CodexView : MonoBehaviour
{
    [Tooltip("펼친 책 UI 전체의 표시와 입력을 제어할 CanvasGroup입니다.")]
    [SerializeField] private CanvasGroup _rootCanvasGroup;
    [Tooltip("유물 1P 목록으로 전환할 버튼입니다.")]
    [SerializeField] private Button _relicTabButton;
    [Tooltip("술 1P 목록으로 전환할 버튼입니다.")]
    [SerializeField] private Button _alcoholTabButton;
    [Tooltip("유물 탭 선택 강조 오브젝트입니다.")]
    [SerializeField] private GameObject _relicTabSelectedRoot;
    [Tooltip("술 탭 선택 강조 오브젝트입니다.")]
    [SerializeField] private GameObject _alcoholTabSelectedRoot;
    [Tooltip("유물 정사각형 슬롯 ScrollRect입니다.")]
    [SerializeField] private ScrollRect _relicScrollRect;
    [Tooltip("술 가로형 행 ScrollRect입니다.")]
    [SerializeField] private ScrollRect _alcoholScrollRect;
    [Tooltip("오른쪽 기술 이미지 슬롯 ScrollRect입니다.")]
    [SerializeField] private ScrollRect _technologyScrollRect;
    [Tooltip("모든 도감 항목에 재사용할 View 프리팹입니다.")]
    [SerializeField] private CodexEntryView _entryViewPrefab;
    [Tooltip("잠긴 기술 이미지에 사용할 공유 흑백 Material입니다.")]
    [SerializeField] private Material _lockedTechnologyMaterial;

    [Header("Tooltip")]
    [Tooltip("한 번에 하나만 표시할 Tooltip RectTransform입니다.")]
    [SerializeField] private RectTransform _tooltipRoot;
    [Tooltip("Tooltip 위치를 제한할 Canvas 영역입니다.")]
    [SerializeField] private RectTransform _tooltipBounds;
    [Tooltip("Tooltip 이미지입니다.")]
    [SerializeField] private Image _tooltipIconImage;
    [Tooltip("Tooltip 이름 텍스트입니다.")]
    [SerializeField] private TMP_Text _tooltipNameText;
    [Tooltip("Tooltip 설명 텍스트입니다.")]
    [SerializeField] private TMP_Text _tooltipDescriptionText;
    [Tooltip("조작법 또는 재료 위치 텍스트입니다.")]
    [SerializeField] private TMP_Text _tooltipSecondaryText;
    [Tooltip("카테고리별 정보 카드 Sprite를 표시할 Tooltip 배경입니다.")]
    [SerializeField] private Image _tooltipBackgroundImage;
    [Tooltip("술 항목에 사용할 정보 카드 Sprite입니다.")]
    [SerializeField] private Sprite _drinkTooltipSprite;
    [Tooltip("기술 항목에 사용할 정보 카드 Sprite입니다.")]
    [SerializeField] private Sprite _skillTooltipSprite;

    private readonly List<CodexEntryView> _relicViews = new List<CodexEntryView>();
    private readonly List<CodexEntryView> _alcoholViews = new List<CodexEntryView>();
    private readonly List<CodexEntryView> _technologyViews = new List<CodexEntryView>();
    private bool _isInitialized;

    public event Action<CodexCategory> CategoryRequested;
    public bool IsConfigured => _rootCanvasGroup != null && _entryViewPrefab != null
        && HasContent(_relicScrollRect) && HasContent(_alcoholScrollRect)
        && HasContent(_technologyScrollRect);

    private void OnDestroy() => RemoveListeners();

    /// <summary>탭 Listener를 한 번만 연결하고 도감 UI를 닫습니다.</summary>
    public void Init()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        _relicTabButton?.onClick.AddListener(HandleRelicTab);
        _alcoholTabButton?.onClick.AddListener(HandleAlcoholTab);
        Hide();
    }

    /// <summary>펼친 책 UI를 표시합니다.</summary>
    public void Show()
    {
        SetCanvasVisible(true);
        ResetScroll(_relicScrollRect);
        ResetScroll(_alcoholScrollRect);
        ResetScroll(_technologyScrollRect);
    }

    /// <summary>Tooltip과 풀 항목을 초기화하고 도감 UI를 숨깁니다.</summary>
    public void Hide()
    {
        HideTooltip();
        Complete(_relicViews, 0);
        Complete(_alcoholViews, 0);
        Complete(_technologyViews, 0);
        SetCanvasVisible(false);
    }

    /// <summary>왼쪽 1P의 선택 카테고리 목록만 갱신합니다.</summary>
    public void BindLeftPage(CodexCategory category,
        IReadOnlyList<CodexEntryDisplayData> entries, Action<CodexEntryView> clicked,
        Action<CodexEntryView, PointerEventData> entered, Action<CodexEntryView> exited)
    {
        bool showRelics = category == CodexCategory.CODEX_CATEGORY_RELIC;
        _relicTabSelectedRoot?.SetActive(showRelics);
        _alcoholTabSelectedRoot?.SetActive(!showRelics);
        SetScrollVisible(_relicScrollRect, showRelics);
        SetScrollVisible(_alcoholScrollRect, !showRelics);
        if (showRelics)
        {
            BindPool(entries, _relicViews, _relicScrollRect,
                CodexEntryLayout.CODEX_ENTRY_RELIC, clicked, entered, exited);
            Complete(_alcoholViews, 0);
        }
        else
        {
            BindPool(entries, _alcoholViews, _alcoholScrollRect,
                CodexEntryLayout.CODEX_ENTRY_ALCOHOL, clicked, entered, exited);
            Complete(_relicViews, 0);
        }
    }

    /// <summary>오른쪽 2P 기술 목록을 카테고리와 독립적으로 갱신합니다.</summary>
    public void BindTechnologyPage(IReadOnlyList<CodexEntryDisplayData> entries,
        Action<CodexEntryView> clicked, Action<CodexEntryView, PointerEventData> entered,
        Action<CodexEntryView> exited) => BindPool(entries, _technologyViews,
            _technologyScrollRect, CodexEntryLayout.CODEX_ENTRY_TECHNOLOGY,
            clicked, entered, exited);

    /// <summary>단일 Tooltip에 항목 내용을 표시하고 화면 안으로 위치를 보정합니다.</summary>
    public void ShowTooltip(CodexEntryDisplayData data, PointerEventData eventData)
    {
        if (_tooltipRoot == null || data == null) return;
        bool unlocked = data.IsUnlocked;
        _tooltipRoot.gameObject.SetActive(true);
        ApplyTooltipBackground(data.Category);
        SetImage(_tooltipIconImage, data.Icon, unlocked);
        SetText(_tooltipNameText, unlocked ? data.DisplayName : "???");
        SetText(_tooltipDescriptionText, unlocked ? data.Description : "?");
        SetText(_tooltipSecondaryText, unlocked ? data.SecondaryText : string.Empty);
        PositionTooltip(eventData);
    }

    /// <summary>공유 Tooltip을 숨깁니다.</summary>
    public void HideTooltip() => _tooltipRoot?.gameObject.SetActive(false);

    private void BindPool(IReadOnlyList<CodexEntryDisplayData> entries,
        List<CodexEntryView> views, ScrollRect scrollRect, CodexEntryLayout layout,
        Action<CodexEntryView> clicked, Action<CodexEntryView, PointerEventData> entered,
        Action<CodexEntryView> exited)
    {
        int count = entries != null ? entries.Count : 0;
        Ensure(views, scrollRect, count);
        int bindCount = Mathf.Min(count, views.Count);
        for (int i = 0; i < bindCount; i++)
            views[i].Bind(entries[i], layout, _lockedTechnologyMaterial, clicked, entered, exited);
        Complete(views, bindCount);
    }

    private void Ensure(List<CodexEntryView> views, ScrollRect scrollRect, int requiredCount)
    {
        if (_entryViewPrefab == null || !HasContent(scrollRect)) return;
        while (views.Count < requiredCount)
            views.Add(Instantiate(_entryViewPrefab, scrollRect.content));
    }

    private static void Complete(List<CodexEntryView> views, int usedCount)
    {
        for (int i = Mathf.Max(0, usedCount); i < views.Count; i++) views[i]?.Clear();
    }

    private void PositionTooltip(PointerEventData eventData)
    {
        if (_tooltipBounds == null || eventData == null) return;
        Canvas.ForceUpdateCanvases();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_tooltipBounds,
            eventData.position, eventData.enterEventCamera, out Vector2 localPoint)) return;
        Rect bounds = _tooltipBounds.rect;
        Rect tooltip = _tooltipRoot.rect;
        float x = Mathf.Clamp(localPoint.x + 20.0f, bounds.xMin,
            bounds.xMax - tooltip.width);
        float y = Mathf.Clamp(localPoint.y - 20.0f, bounds.yMin + tooltip.height,
            bounds.yMax);
        _tooltipRoot.anchoredPosition = new Vector2(x, y);
    }

    private void SetCanvasVisible(bool visible)
    {
        if (_rootCanvasGroup == null) return;
        _rootCanvasGroup.gameObject.SetActive(true);
        _rootCanvasGroup.alpha = visible ? 1.0f : 0.0f;
        _rootCanvasGroup.interactable = visible;
        _rootCanvasGroup.blocksRaycasts = visible;
    }

    private void RemoveListeners()
    {
        _relicTabButton?.onClick.RemoveListener(HandleRelicTab);
        _alcoholTabButton?.onClick.RemoveListener(HandleAlcoholTab);
    }

    private void HandleRelicTab() => CategoryRequested?.Invoke(CodexCategory.CODEX_CATEGORY_RELIC);
    private void HandleAlcoholTab() => CategoryRequested?.Invoke(CodexCategory.CODEX_CATEGORY_ALCOHOL);
    private static bool HasContent(ScrollRect target) => target != null && target.content != null;
    private static void ResetScroll(ScrollRect target) { if (target != null) target.verticalNormalizedPosition = 1.0f; }
    private static void SetScrollVisible(ScrollRect target, bool visible) { if (target != null) target.gameObject.SetActive(visible); }
    private static void SetText(TMP_Text target, string value) { if (target != null) target.text = value ?? string.Empty; }
    private static void SetImage(Image target, Sprite sprite, bool unlocked)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.enabled = sprite != null;
        target.color = unlocked ? Color.white : Color.black;
    }

    private void ApplyTooltipBackground(CodexCategory category)
    {
        if (_tooltipBackgroundImage == null) return;

        Sprite card = category == CodexCategory.CODEX_CATEGORY_ALCOHOL
            ? _drinkTooltipSprite
            : category == CodexCategory.CODEX_CATEGORY_TECHNOLOGY
                ? _skillTooltipSprite
                : null;

        _tooltipBackgroundImage.sprite = card;
        _tooltipBackgroundImage.color = card != null
            ? Color.white
            : new Color32(42, 31, 23, 248);
        _tooltipBackgroundImage.preserveAspect = card != null;
    }
}
