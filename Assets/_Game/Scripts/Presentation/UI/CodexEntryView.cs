//=============================================================================
// File:        CodexEntryView.cs
// Description: 유물·술·기술·재료 항목의 표시와 포인터 입력을 공통 처리한다.
// History:     08/22/2026 + Created -- Codex
//=============================================================================

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CodexEntryLayout
{
    CODEX_ENTRY_RELIC = 0,
    CODEX_ENTRY_ALCOHOL,
    CODEX_ENTRY_TECHNOLOGY,
    CODEX_ENTRY_INGREDIENT
}

public sealed class CodexEntryDisplayData
{
    public string EntryId { get; }
    public Sprite Icon { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string SecondaryText { get; }
    public IReadOnlyList<CodexEntryDisplayData> Ingredients { get; }
    public CodexCategory Category { get; }
    public bool IsAlwaysUnlocked { get; }
    public bool IsUnlocked { get; set; }

    public CodexEntryDisplayData(string entryId, Sprite icon, string displayName,
        string description, string secondaryText,
        IReadOnlyList<CodexEntryDisplayData> ingredients = null, bool isAlwaysUnlocked = false,
        CodexCategory category = CodexCategory.CODEX_CATEGORY_RELIC)
    {
        EntryId = entryId ?? string.Empty;
        Icon = icon;
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        SecondaryText = secondaryText ?? string.Empty;
        Ingredients = ingredients ?? Array.Empty<CodexEntryDisplayData>();
        Category = category;
        IsAlwaysUnlocked = isAlwaysUnlocked;
        IsUnlocked = isAlwaysUnlocked;
    }
}

[DisallowMultipleComponent]
public sealed class CodexEntryView : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("항목 크기를 목록 종류에 맞게 조절할 LayoutElement입니다.")]
    [SerializeField] private LayoutElement _layoutElement;
    [Tooltip("항목 아이콘을 표시할 Image입니다.")]
    [SerializeField] private Image _iconImage;
    [Tooltip("항목 이름을 표시할 TMP 텍스트입니다.")]
    [SerializeField] private TMP_Text _nameText;
    [Tooltip("선택된 항목에만 표시할 강조 오브젝트입니다.")]
    [SerializeField] private GameObject _selectedRoot;
    [Tooltip("술 제조 재료 목록 전체 오브젝트입니다.")]
    [SerializeField] private GameObject _ingredientsRoot;
    [Tooltip("술 제조 재료 View가 배치될 부모입니다.")]
    [SerializeField] private RectTransform _ingredientContentRoot;
    [Tooltip("재료에도 재사용할 동일한 도감 항목 View 프리팹입니다.")]
    [SerializeField] private CodexEntryView _ingredientViewPrefab;
    [Tooltip("해제 상태에서 사용할 공유 Material입니다. 비어 있으면 UI 기본 Material을 사용합니다.")]
    [SerializeField] private Material _unlockedMaterial;

    private readonly List<CodexEntryView> _ingredientViews = new List<CodexEntryView>();
    private CodexEntryDisplayData _data;
    private CodexEntryLayout _layout;
    private Material _lockedTechnologyMaterial;
    private Action<CodexEntryView> _clicked;
    private Action<CodexEntryView, PointerEventData> _entered;
    private Action<CodexEntryView> _exited;

    public CodexEntryDisplayData Data => _data;

    /// <summary>표시 데이터와 항목 종류 및 입력 콜백을 연결합니다.</summary>
    public void Bind(CodexEntryDisplayData data, CodexEntryLayout layout,
        Material lockedTechnologyMaterial, Action<CodexEntryView> clicked,
        Action<CodexEntryView, PointerEventData> entered, Action<CodexEntryView> exited)
    {
        _data = data;
        _layout = layout;
        _lockedTechnologyMaterial = lockedTechnologyMaterial;
        _clicked = clicked;
        _entered = entered;
        _exited = exited;
        gameObject.SetActive(data != null);
        if (data == null) return;
        ApplyLayout(layout);
        ApplyIcon(data, layout);
        SetText(_nameText, data.IsUnlocked ? data.DisplayName : "???");
        SetSelected(false);
        BindIngredients(data, layout);
    }

    /// <summary>선택 강조 표시를 변경합니다.</summary>
    public void SetSelected(bool isSelected) => _selectedRoot?.SetActive(isSelected);

    /// <summary>표시 데이터와 콜백을 비우고 풀에서 숨깁니다.</summary>
    public void Clear()
    {
        _data = null;
        _clicked = null;
        _entered = null;
        _exited = null;
        SetSelected(false);
        CompleteIngredients(0);
        gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_data != null && _layout != CodexEntryLayout.CODEX_ENTRY_INGREDIENT
            && eventData != null && eventData.button == PointerEventData.InputButton.Left)
            _clicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data != null) _entered?.Invoke(this, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_data != null) _exited?.Invoke(this);
    }

    private void ApplyLayout(CodexEntryLayout layout)
    {
        if (_layoutElement == null) return;
        bool isAlcohol = layout == CodexEntryLayout.CODEX_ENTRY_ALCOHOL;
        bool isIngredient = layout == CodexEntryLayout.CODEX_ENTRY_INGREDIENT;
        _layoutElement.preferredWidth = isAlcohol ? -1.0f : isIngredient ? 64.0f : 112.0f;
        _layoutElement.preferredHeight = isAlcohol ? 128.0f : isIngredient ? 64.0f : 112.0f;
        _layoutElement.flexibleWidth = isAlcohol ? 1.0f : 0.0f;
    }

    private void ApplyIcon(CodexEntryDisplayData data, CodexEntryLayout layout)
    {
        if (_iconImage == null) return;
        bool isLocked = !data.IsUnlocked;
        _iconImage.sprite = data.Icon;
        _iconImage.enabled = data.Icon != null;
        _iconImage.color = isLocked && layout != CodexEntryLayout.CODEX_ENTRY_TECHNOLOGY
            ? Color.black : Color.white;
        _iconImage.material = isLocked && layout == CodexEntryLayout.CODEX_ENTRY_TECHNOLOGY
            ? _lockedTechnologyMaterial : _unlockedMaterial;
    }

    private void BindIngredients(CodexEntryDisplayData data, CodexEntryLayout layout)
    {
        bool showIngredients = layout == CodexEntryLayout.CODEX_ENTRY_ALCOHOL
            && data.Ingredients.Count > 0;
        _ingredientsRoot?.SetActive(showIngredients);
        if (!showIngredients) { CompleteIngredients(0); return; }
        EnsureIngredients(data.Ingredients.Count);
        int count = Mathf.Min(data.Ingredients.Count, _ingredientViews.Count);
        for (int i = 0; i < count; i++)
            _ingredientViews[i].Bind(data.Ingredients[i], CodexEntryLayout.CODEX_ENTRY_INGREDIENT,
                _lockedTechnologyMaterial, null, _entered, _exited);
        CompleteIngredients(count);
    }

    private void EnsureIngredients(int requiredCount)
    {
        if (_ingredientViewPrefab == null || _ingredientContentRoot == null) return;
        while (_ingredientViews.Count < requiredCount)
            _ingredientViews.Add(Instantiate(_ingredientViewPrefab, _ingredientContentRoot));
    }

    private void CompleteIngredients(int usedCount)
    {
        for (int i = Mathf.Max(0, usedCount); i < _ingredientViews.Count; i++)
            _ingredientViews[i]?.Clear();
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value ?? string.Empty;
    }
}
