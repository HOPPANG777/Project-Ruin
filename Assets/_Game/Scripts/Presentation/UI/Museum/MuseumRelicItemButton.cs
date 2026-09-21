//=============================================================================
// File:        MuseumRelicItemButton.cs
// Description: 배치 가능한 유물 한 종류의 아이콘·이름·수량 버튼을 표시한다.
// History:     08/30/2026 + Created -- Codex
//=============================================================================
//========================================
// System Includes
//========================================
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>한 유물의 표시와 고정 인덱스 선택 이벤트를 담당합니다.</summary>
[DisallowMultipleComponent]
public sealed class MuseumRelicItemButton : MonoBehaviour
{
    [Tooltip("이 유물을 전시 대상으로 선택하는 버튼입니다.")]
    [SerializeField] private Button _button;
    [Tooltip("유물 아이콘을 표시할 이미지입니다.")]
    [SerializeField] private Image _icon;
    [Tooltip("현지화된 유물 이름을 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text _nameText;
    [Tooltip("현재 보유 수량을 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text _countText;
    // 선택한 고정 슬롯 인덱스를 전달할 콜백입니다.
    private Action<int> _selected;
    // 재사용 버튼의 고정 슬롯 인덱스입니다.
    private int _index;

    private void OnDestroy() => _button?.onClick.RemoveListener(HandleClick);

    //=============================================================================
    // Init
    // Description: 고정 슬롯 인덱스와 선택 콜백을 중복 없이 연결합니다.
    // Parameters: int index, Action<int> selected
    // Return: void
    //=============================================================================
    /// <summary>고정 슬롯 인덱스와 선택 콜백을 중복 없이 연결합니다.</summary>
    public void Init(int index, Action<int> selected)
    {
        _button?.onClick.RemoveListener(HandleClick);
        _index = index;
        _selected = selected;
        _button?.onClick.AddListener(HandleClick);
        Hide();
    }

    //=============================================================================
    // Show
    // Description: 유물 표시 데이터와 현재 수량을 버튼에 적용합니다.
    // Parameters: InventoryItemDisplayData display, int count
    // Return: void
    //=============================================================================
    /// <summary>유물 표시 데이터와 현재 수량을 버튼에 적용합니다.</summary>
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

    //=============================================================================
    // Hide
    // Description: 사용하지 않는 고정 슬롯을 숨깁니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
    /// <summary>사용하지 않는 고정 슬롯을 숨깁니다.</summary>
    public void Hide() => gameObject.SetActive(false);
    //=============================================================================
    // SetInteractable
    // Description: 중복 입력 방지를 위해 버튼 입력 가능 여부를 설정합니다.
    // Parameters: bool value
    // Return: void
    //=============================================================================
    /// <summary>중복 입력 방지를 위해 버튼 입력 가능 여부를 설정합니다.</summary>
    public void SetInteractable(bool value)
    {
        if (_button != null) _button.interactable = value;
    }
    // 선택한 고정 슬롯 인덱스를 전달할 콜백입니다.
    private void HandleClick() => _selected?.Invoke(_index);
}
