//=============================================================================
// File:        CurrencyHudPresenter.cs
//
// Description: ITEM_COIN의 아이콘과 현재 인벤토리 수량을 HUD에 표시한다.
//
// History:     08/11/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//=============================================================================
//
// Synopsis:   CurrencyHudPresenter;
//                - ITEM_COIN 초기 수량 표시
//                - ITEM_COIN 수량 변경 이벤트만 HUD에 반영
//=============================================================================
[DisallowMultipleComponent]
public sealed class CurrencyHudPresenter : MonoBehaviour
{
    //========================================
    // 상수
    //========================================

    private const string CURRENCY_ITEM_ID = "ITEM_COIN";

    //========================================
    // 직렬화 필드
    //========================================

    [Tooltip("ITEM_COIN 아이콘을 표시할 이미지입니다.")]
    [SerializeField]
    private Image _coinIconImage;

    [Tooltip("현재 ITEM_COIN 보유 수량을 표시할 텍스트입니다.")]
    [SerializeField]
    private TMP_Text _coinCountText;

    //========================================
    // 내부 상태
    //========================================

    private IInventoryService _inventoryService;

    //******************************************************************************
    //
    // Unity Lifecycle
    //
    //******************************************************************************

    private void OnDestroy()
    {
        Unsubscribe();
    }

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // CurrencyHudPresenter.Init
    //=============================================================================
    // Description: 인벤토리 서비스를 연결하고 코인 아이콘과 초기 수량을 표시한다.
    //
    // Parameters:  ( IInventoryService inventoryService )
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>인벤토리 서비스를 연결하고 코인 아이콘과 초기 수량을 표시합니다.</summary>
    public void Init(IInventoryService inventoryService)
    {
        Unsubscribe();
        _inventoryService = inventoryService;

        if (_inventoryService == null)
        {
            SetCoinCount(0);
            SetCoinIcon(null);
            return;
        }

        _inventoryService.ItemCountChanged += HandleItemCountChanged;
        SetCoinCount(_inventoryService.GetItemCount(CURRENCY_ITEM_ID));

        if (_inventoryService.TryGetItemDisplay(CURRENCY_ITEM_ID, out InventoryItemDisplayData displayData))
        {
            SetCoinIcon(displayData.Icon);
        }
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    private void HandleItemCountChanged(string itemId, int finalCount)
    {
        if (!string.Equals(itemId, CURRENCY_ITEM_ID, StringComparison.Ordinal))
        {
            return;
        }

        SetCoinCount(finalCount);
    }

    private void SetCoinCount(int count)
    {
        if (_coinCountText != null)
        {
            _coinCountText.text = Mathf.Max(0, count).ToString();
        }
    }

    private void SetCoinIcon(Sprite icon)
    {
        if (_coinIconImage == null)
        {
            return;
        }

        _coinIconImage.sprite = icon;
        _coinIconImage.enabled = icon != null;
    }

    private void Unsubscribe()
    {
        if (_inventoryService != null)
        {
            _inventoryService.ItemCountChanged -= HandleItemCountChanged;
            _inventoryService = null;
        }
    }
}
