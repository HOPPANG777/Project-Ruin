//=============================================================================
// File:        MuseumBottleItemButton.cs
// Description: 투입 가능한 술 한 종류의 아이콘·이름·수량 버튼을 표시한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MuseumBottleItemButton : MonoBehaviour
{
    [Tooltip("이 술을 선택하는 버튼입니다.")]
    [SerializeField] private Button _button;
    [Tooltip("술 아이콘을 표시합니다.")]
    [SerializeField] private Image _icon;
    [Tooltip("현지화된 술 이름을 표시합니다.")]
    [SerializeField] private TMP_Text _nameText;
    [Tooltip("현재 보유 수량을 표시합니다.")]
    [SerializeField] private TMP_Text _countText;

    private Action<int> _selected;
    private int _index;

    private void OnDestroy() => _button?.onClick.RemoveListener(HandleClick);

    /// <summary>고정 슬롯의 선택 콜백을 연결하고 처음에는 숨깁니다.</summary>
    public void Init(int index, Action<int> selected)
    {
        _button?.onClick.RemoveListener(HandleClick);
        _index = index;
        _selected = selected;
        _button?.onClick.AddListener(HandleClick);
        Hide();
    }

    /// <summary>술 표시 데이터와 보유 수량을 버튼에 반영합니다.</summary>
    public void Show(InventoryItemDisplayData display, int count)
    {
        gameObject.SetActive(true);
        if (_icon != null)
        {
            _icon.sprite = display?.Icon;
            _icon.enabled = display?.Icon != null;
        }
        if (_nameText != null) _nameText.text = display?.DisplayName ?? string.Empty;
        _countText?.SetText("x{0}", count);
    }

    public void Hide() => gameObject.SetActive(false);
    public void SetInteractable(bool value)
    {
        if (_button != null) _button.interactable = value;
    }
    private void HandleClick() => _selected?.Invoke(_index);
}
