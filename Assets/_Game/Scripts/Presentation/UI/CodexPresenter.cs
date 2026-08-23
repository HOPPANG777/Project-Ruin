//=============================================================================
// File:        CodexPresenter.cs
// Description: 도감 데이터와 영구 해제 상태를 펼친 책 UI에 중개한다.
// History:     08/22/2026 + Created -- Codex
//=============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class CodexPresenter : MonoBehaviour
{
    [Tooltip("펼친 책 2P 도감 View입니다.")]
    [SerializeField] private CodexView _view;
    [Tooltip("Technology icon_id를 Sprite로 변환할 공유 Catalog입니다.")]
    [SerializeField] private ItemVisualCatalogSO _visualCatalog;

    private readonly List<CodexEntryDisplayData> _relics = new List<CodexEntryDisplayData>();
    private readonly List<CodexEntryDisplayData> _alcohols = new List<CodexEntryDisplayData>();
    private readonly List<CodexEntryDisplayData> _technologies = new List<CodexEntryDisplayData>();
    private ICodexDataRepository _data;
    private ICodexService _codex;
    private IInventoryService _inventory;
    private ILocalizationService _localization;
    private CodexCategory _category = CodexCategory.CODEX_CATEGORY_RELIC;
    private CodexEntryView _selectedView;
    private CodexEntryView _hoveredView;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void OnDestroy() => Unsubscribe();

    /// <summary>도감 서비스와 표시 서비스를 주입하고 재사용 표시 데이터를 구성합니다.</summary>
    public bool Init(ICodexDataRepository data, ICodexService codex,
        IInventoryService inventory, ILocalizationService localization)
    {
        Unsubscribe();
        _data = data;
        _codex = codex;
        _inventory = inventory;
        _localization = localization;
        if (_view == null || _data == null || _codex == null
            || _inventory == null || _localization == null)
        {
            Debug.LogWarning("[CodexPresenter] View 또는 도감 표시 의존성이 연결되지 않았습니다.");
            return false;
        }

        _view.Init();
        _view.CategoryRequested += HandleCategoryRequested;
        _codex.DiscoveryChanged += HandleDiscoveryChanged;
        BuildDisplayCache();
        Close();
        return true;
    }

    /// <summary>펼친 책 도감을 열고 최신 잠금 상태를 표시합니다.</summary>
    public void Open()
    {
        if (_view == null || _codex == null) return;
        _isOpen = true;
        _view.Show();
        RefreshAll();
    }

    /// <summary>도감을 닫고 Hover와 선택 상태를 초기화합니다.</summary>
    public void Close()
    {
        _isOpen = false;
        ResetPointerState();
        _view?.Hide();
    }

    private void BuildDisplayCache()
    {
        _relics.Clear();
        _alcohols.Clear();
        _technologies.Clear();
        IReadOnlyList<CodexEntryDefinition> entries = _data.CodexEntries;
        for (int i = 0; i < entries.Count; i++)
        {
            CodexEntryDefinition entry = entries[i];
            if (!TryCreateDisplay(entry, out CodexEntryDisplayData display)) continue;
            GetList(entry.Category)?.Add(display);
        }
    }

    private bool TryCreateDisplay(CodexEntryDefinition entry, out CodexEntryDisplayData display)
    {
        display = null;
        if (entry.Category == CodexCategory.CODEX_CATEGORY_TECHNOLOGY)
        {
            if (!_data.TryGetTechnology(entry.SourceId, out TechnologyDefinition technology)) return false;
            Sprite icon = null;
            _visualCatalog?.TryGetIcon(technology.IconId, out icon);
            display = new CodexEntryDisplayData(entry.EntryId, icon,
                _localization.GetText(technology.DisplayNameKey),
                _localization.GetText(technology.DescriptionTextKey),
                _localization.GetText(technology.OperationTextKey));
            return true;
        }

        if (!_inventory.TryGetItemDisplay(entry.SourceId, out InventoryItemDisplayData item)) return false;
        IReadOnlyList<CodexEntryDisplayData> ingredients = entry.Category
            == CodexCategory.CODEX_CATEGORY_ALCOHOL ? CreateIngredients(entry.EntryId) : null;
        display = new CodexEntryDisplayData(entry.EntryId, item.Icon,
            item.DisplayName, item.Description, string.Empty, ingredients);
        return true;
    }

    private IReadOnlyList<CodexEntryDisplayData> CreateIngredients(string entryId)
    {
        if (!_data.TryGetCodexIngredients(entryId,
            out IReadOnlyList<CodexIngredientDefinition> definitions))
            return System.Array.Empty<CodexEntryDisplayData>();

        List<CodexEntryDisplayData> ingredients = new List<CodexEntryDisplayData>(definitions.Count);
        for (int i = 0; i < definitions.Count; i++)
        {
            CodexIngredientDefinition definition = definitions[i];
            if (!_inventory.TryGetItemDisplay(definition.ItemId,
                out InventoryItemDisplayData item)) continue;
            ingredients.Add(new CodexEntryDisplayData(string.Empty, item.Icon,
                item.DisplayName, item.Description,
                _localization.GetText(definition.LocationTextKey), null, true));
        }
        return ingredients;
    }

    private void RefreshAll()
    {
        RefreshUnlocks(_relics);
        RefreshUnlocks(_alcohols);
        RefreshUnlocks(_technologies);
        _view.BindLeftPage(_category, GetList(_category), HandleClicked,
            HandleEntered, HandleExited);
        _view.BindTechnologyPage(_technologies, HandleClicked, HandleEntered, HandleExited);
    }

    private void RefreshUnlocks(List<CodexEntryDisplayData> entries)
    {
        for (int i = 0; i < entries.Count; i++)
            entries[i].IsUnlocked = entries[i].IsAlwaysUnlocked
                || _codex.IsEntryUnlocked(entries[i].EntryId);
    }

    private List<CodexEntryDisplayData> GetList(CodexCategory category)
    {
        if (category == CodexCategory.CODEX_CATEGORY_RELIC) return _relics;
        if (category == CodexCategory.CODEX_CATEGORY_ALCOHOL) return _alcohols;
        return category == CodexCategory.CODEX_CATEGORY_TECHNOLOGY ? _technologies : null;
    }

    private void HandleCategoryRequested(CodexCategory category)
    {
        if (!_isOpen || category == _category || GetList(category) == null) return;
        ResetPointerState();
        _category = category;
        _view.BindLeftPage(_category, GetList(_category), HandleClicked,
            HandleEntered, HandleExited);
    }

    private void HandleDiscoveryChanged(string entryId)
    {
        if (_isOpen) RefreshAll();
    }

    private void HandleClicked(CodexEntryView view)
    {
        if (_selectedView == view) return;
        _selectedView?.SetSelected(false);
        _selectedView = view;
        _selectedView?.SetSelected(true);
    }

    private void HandleEntered(CodexEntryView view, PointerEventData eventData)
    {
        if (view == null || view.Data == null) return;
        _hoveredView = view;
        _view.ShowTooltip(view.Data, eventData);
    }

    private void HandleExited(CodexEntryView view)
    {
        if (_hoveredView != view) return;
        _hoveredView = null;
        _view.HideTooltip();
    }

    private void ResetPointerState()
    {
        _hoveredView = null;
        _selectedView?.SetSelected(false);
        _selectedView = null;
        _view?.HideTooltip();
    }

    private void Unsubscribe()
    {
        if (_view != null) _view.CategoryRequested -= HandleCategoryRequested;
        if (_codex != null) _codex.DiscoveryChanged -= HandleDiscoveryChanged;
        _isOpen = false;
    }
}
