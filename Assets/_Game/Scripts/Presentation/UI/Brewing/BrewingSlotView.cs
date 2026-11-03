//=============================================================================
// File:        BrewingSlotView.cs
//
// Description: 제조 슬롯 한 칸의 아이콘 표시와 드롭·클릭 입력을 전달한다.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//=============================================================================
//
// Synopsis:   BrewingSlotKind;
//                - 제조 팝업의 슬롯 종류
//=============================================================================
public enum BrewingSlotKind
{
    BREWING_SLOT_INGREDIENT = 0,
    BREWING_SLOT_TOOL,
    BREWING_SLOT_RESULT
}

//=============================================================================
//
// Synopsis:   BrewingSlotView;
//                - 상태를 갖지 않고 입력만 이벤트로 전달 (등록 규칙은 Controller 책임)
//                - 드롭을 받으려면 이 오브젝트나 자식에 Raycast Target Graphic이 필요
//=============================================================================
[DisallowMultipleComponent]
public sealed class BrewingSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    //========================================
    // 직렬화 필드
    //========================================

    [Tooltip("슬롯 종류입니다.")]
    [SerializeField] private BrewingSlotKind _kind;

    [Tooltip("재료 슬롯 인덱스입니다. 도구·결과 슬롯에서는 사용하지 않습니다.")]
    [SerializeField] private int _index;

    [Tooltip("등록된 아이템 아이콘을 표시할 Image입니다.")]
    [SerializeField] private Image _iconImage;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>슬롯 종류입니다.</summary>
    public BrewingSlotKind Kind => _kind;

    /// <summary>재료 슬롯 인덱스입니다.</summary>
    public int Index => _index;

    /// <summary>선택 패널의 아이템이 이 슬롯에 드롭됐을 때 발행됩니다.</summary>
    public event Action<BrewingSlotView, string> ItemDropped;

    /// <summary>이 슬롯이 좌클릭됐을 때 발행됩니다.</summary>
    public event Action<BrewingSlotView> Clicked;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    /// <summary>슬롯 아이콘을 표시합니다. null이면 빈 슬롯으로 표시합니다.</summary>
    public void SetIcon(Sprite icon)
    {
        if (_iconImage == null)
        {
            return;
        }

        _iconImage.sprite = icon;
        _iconImage.enabled = icon != null;
    }

    /// <summary>드래그 중인 선택 패널 아이템을 받아 Presenter에 전달합니다.</summary>
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dragged = eventData != null ? eventData.pointerDrag : null;
        BrewingPaletteItemView source =
            dragged != null ? dragged.GetComponent<BrewingPaletteItemView>() : null;

        if (source != null)
        {
            ItemDropped?.Invoke(this, source.ItemId);
        }
    }

    /// <summary>좌클릭만 Presenter에 전달합니다.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(this);
        }
    }
}
