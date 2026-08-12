//=============================================================================
// File:        ShopItemView.cs
//
// Description: 상점 구매·판매 목록 한 행의 표시와 포인터 입력을 전달한다.
//
// History:     08/12/2026 + Created -- Codex
//
//=============================================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ShopUiMode
{
    SHOP_UI_BUY = 0,
    SHOP_UI_SELL
}

//=============================================================================
//
// Synopsis:   ShopItemView;
//                - Bind 시 받은 표시 데이터와 수량을 캐시
//                - 클릭과 Hover 입력만 Presenter에 전달
//=============================================================================
[DisallowMultipleComponent]
public sealed class ShopItemView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private GameObject _soldOutRoot;

    private InventoryItemDisplayData _displayData;
    private ShopUiMode _mode;
    private int _ownedQuantity;
    private bool _isSoldOut;
    private Action<ShopItemView> _clicked;
    private Action<ShopItemView> _pointerEntered;
    private Action<ShopItemView> _pointerExited;

    /// <summary>현재 행에 캐시된 아이템 표시 데이터입니다.</summary>
    public InventoryItemDisplayData DisplayData => _displayData;

    /// <summary>현재 행이 속한 구매·판매 목록 종류입니다.</summary>
    public ShopUiMode Mode => _mode;

    /// <summary>Bind 시점의 플레이어 보유 수량입니다.</summary>
    public int OwnedQuantity => _ownedQuantity;

    /// <summary>행의 표시 값과 입력 콜백을 갱신합니다.</summary>
    public void Bind(
        InventoryItemDisplayData displayData,
        ShopUiMode mode,
        int listQuantity,
        int ownedQuantity,
        bool isSoldOut,
        Action<ShopItemView> clicked,
        Action<ShopItemView> pointerEntered,
        Action<ShopItemView> pointerExited)
    {
        _displayData = displayData;
        _mode = mode;
        _ownedQuantity = Mathf.Max(0, ownedQuantity);
        _isSoldOut = isSoldOut;
        _clicked = clicked;
        _pointerEntered = pointerEntered;
        _pointerExited = pointerExited;

        gameObject.SetActive(true);
        SetImage(_iconImage, displayData != null ? displayData.Icon : null);
        SetText(_nameText, displayData != null ? displayData.DisplayName : string.Empty);
        SetText(_priceText, displayData != null ? displayData.Price.ToString() : string.Empty);
        SetText(_quantityText, Mathf.Max(0, listQuantity).ToString());

        if (_soldOutRoot != null)
        {
            _soldOutRoot.SetActive(isSoldOut);
        }
    }

    /// <summary>표시 데이터와 콜백을 비우고 풀 안에서 행을 숨깁니다.</summary>
    public void Clear()
    {
        _displayData = null;
        _ownedQuantity = 0;
        _isSoldOut = false;
        _clicked = null;
        _pointerEntered = null;
        _pointerExited = null;
        gameObject.SetActive(false);
    }

    /// <summary>유효한 좌클릭을 Presenter에 전달합니다.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left
            || _displayData == null || _mode == ShopUiMode.SHOP_UI_BUY && _isSoldOut)
        {
            return;
        }

        _clicked?.Invoke(this);
    }

    /// <summary>Hover 시작을 Presenter에 전달합니다.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_displayData != null)
        {
            _pointerEntered?.Invoke(this);
        }
    }

    /// <summary>Hover 종료를 Presenter에 전달합니다.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerExited?.Invoke(this);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }

    private static void SetImage(Image target, Sprite sprite)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
    }
}
