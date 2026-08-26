//=============================================================================
// File:        MuseumBottlePresenter.cs
// Description: 술병 선택 UI와 투입 도메인 처리를 중개한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MuseumBottlePresenter : MonoBehaviour
{
    [Tooltip("술 목록과 진행도를 표시하는 View입니다.")]
    [SerializeField] private MuseumBottleView _view;

    private readonly List<MuseumBottleItemDefinition> _items =
        new List<MuseumBottleItemDefinition>();
    private IMuseumBottleService _bottles;
    private IInventoryService _inventory;
    private ILocalizationService _localization;
    private string _bottleId;
    private bool _isBusy;

    private void OnDestroy()
    {
        if (_bottles != null) _bottles.BottleChanged -= HandleBottleChanged;
    }

    /// <summary>술병, 인벤토리와 현지화 서비스를 연결합니다.</summary>
    public void Init(IMuseumBottleService bottles, IInventoryService inventory,
        ILocalizationService localization)
    {
        if (_bottles != null) _bottles.BottleChanged -= HandleBottleChanged;
        _bottles = bottles;
        _inventory = inventory;
        _localization = localization;
        _bottleId = string.Empty;
        _isBusy = false;
        if (_bottles != null) _bottles.BottleChanged += HandleBottleChanged;
        if (_view == null)
        {
            Debug.LogWarning("[MuseumBottlePresenter] View가 연결되지 않았습니다.", this);
            return;
        }
        _view.Init(SelectItem, Close);
    }

    /// <summary>지정한 술병의 현재 투입 후보 UI를 엽니다.</summary>
    public void Open(string bottleId)
    {
        if (_bottles == null || _inventory == null || _localization == null
            || _view == null || _view.IsOpen || _isBusy
            || !_bottles.TryGetBottle(bottleId, out _))
        {
            Debug.LogWarning($"[MuseumBottlePresenter] 술병 UI를 열 수 없습니다. id={bottleId}", this);
            return;
        }
        _bottleId = bottleId;
        Refresh();
        _view.TryShow();
    }

    private void SelectItem(int index)
    {
        if (_isBusy || index < 0 || index >= _items.Count) return;
        _isBusy = true;
        _view.SetSelectionEnabled(false);
        string itemId = _items[index].AlcoholItemId;
        if (!_bottles.TryContribute(_bottleId, itemId,
                out MuseumBottleContributionFailure failure))
            Debug.LogWarning($"[MuseumBottlePresenter] 술 투입 실패: item={itemId}, failure={failure}", this);
        Refresh();
        _isBusy = false;
        _view.SetSelectionEnabled(true);
    }

    private void Refresh()
    {
        if (!_bottles.TryGetBottle(_bottleId, out MuseumBottleDefinition bottle)
            || !_bottles.TryGetProgress(_bottleId, out MuseumBottleProgress progress)) return;
        _bottles.FillContributableItems(_bottleId, _items);
        _view.Refresh(_localization.GetText(bottle.DisplayNameKey), progress, _items, _inventory);
    }

    private void HandleBottleChanged(string bottleId)
    {
        if (_view != null && _view.IsOpen && bottleId == _bottleId) Refresh();
    }

    private void Close()
    {
        if (_view == null || !_view.IsOpen || _isBusy) return;
        _view.Hide();
        _bottleId = string.Empty;
        _items.Clear();
    }
}
