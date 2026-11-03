//=============================================================================
// File:        BrewingPaletteItemView.cs
//
// Description: 선택 패널의 재료·도구 한 칸. 아이콘·보유 수량 또는 실루엣을 표시하고 드래그 입력을 전달한다.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//=============================================================================
//
// Synopsis:   BrewingPaletteItemView;
//                - 보유 수량 1 이상: 정상 아이콘 + 수량
//                - 보유 수량 0: 실루엣 (미보유와 미발견을 구별하지 않음)
//                - 실루엣도 드래그는 시작되며, 등록 거부는 Controller가 판단
//=============================================================================
[DisallowMultipleComponent]
public sealed class BrewingPaletteItemView : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    //========================================
    // 직렬화 필드
    //========================================

    [Tooltip("아이템 아이콘을 표시할 Image입니다. Raycast Target이 켜져 있어야 드래그할 수 있습니다.")]
    [SerializeField] private Image _iconImage;

    [Tooltip("보유 수량을 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text _countText;

    [Tooltip("보유 수량이 0일 때 아이콘에 곱해 실루엣으로 보이게 하는 색상입니다.")]
    [SerializeField] private Color _silhouetteColor = Color.black;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>이 칸이 나타내는 아이템 ID입니다.</summary>
    public string ItemId { get; private set; }

    /// <summary>현재 표시 중인 아이콘입니다.</summary>
    public Sprite Icon => _iconImage != null ? _iconImage.sprite : null;

    /// <summary>현재 아이콘 색상입니다. 실루엣 여부가 반영됩니다.</summary>
    public Color IconColor => _iconImage != null ? _iconImage.color : Color.white;

    /// <summary>드래그가 시작됐을 때 발행됩니다.</summary>
    public event Action<BrewingPaletteItemView, PointerEventData> DragBegan;

    /// <summary>드래그 중 포인터가 움직일 때 발행됩니다.</summary>
    public event Action<BrewingPaletteItemView, PointerEventData> DragMoved;

    /// <summary>드래그가 끝났을 때 발행됩니다.</summary>
    public event Action<BrewingPaletteItemView, PointerEventData> DragEnded;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    /// <summary>아이템 ID와 아이콘을 연결하고 보유 수량을 표시합니다.</summary>
    public void Bind(string itemId, Sprite icon, int ownedCount)
    {
        ItemId = itemId;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        SetCount(ownedCount);
    }

    /// <summary>보유 수량에 맞춰 정상 아이콘과 실루엣을 전환합니다.</summary>
    public void SetCount(int ownedCount)
    {
        bool isOwned = ownedCount > 0;

        if (_iconImage != null)
        {
            _iconImage.color = isOwned ? Color.white : _silhouetteColor;
        }

        if (_countText != null)
        {
            _countText.text = isOwned ? ownedCount.ToString() : string.Empty;
        }
    }

    /// <summary>드래그 시작을 View에 전달합니다.</summary>
    public void OnBeginDrag(PointerEventData eventData) => DragBegan?.Invoke(this, eventData);

    /// <summary>드래그 이동을 View에 전달합니다.</summary>
    public void OnDrag(PointerEventData eventData) => DragMoved?.Invoke(this, eventData);

    /// <summary>드래그 종료를 View에 전달합니다.</summary>
    public void OnEndDrag(PointerEventData eventData) => DragEnded?.Invoke(this, eventData);
}
