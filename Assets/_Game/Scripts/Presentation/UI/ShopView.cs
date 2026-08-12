//=============================================================================
// File:        ShopView.cs
//
// Description: 상점 목록 풀과 설명·수량 패널의 표시 및 버튼 입력을 관리한다.
//
// History:     08/12/2026 + Created -- Codex
//
//=============================================================================

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

internal sealed class ShopInputLock
{
    private readonly PlayerMoveController _moveController;
    private readonly PlayerInteractionController _interactionController;
    private bool _hadMoveControl;
    private bool _hadInteractionControl;
    private bool _isAcquired;

    /// <summary>이동과 상호작용 Controller가 모두 연결되었는지 확인합니다.</summary>
    public bool IsConfigured => _moveController != null && _interactionController != null;

    /// <summary>입력 상태를 보존할 Controller를 연결합니다.</summary>
    public ShopInputLock(
        PlayerMoveController moveController,
        PlayerInteractionController interactionController)
    {
        _moveController = moveController;
        _interactionController = interactionController;
    }

    /// <summary>현재 입력 상태를 저장하고 상점이 사용하는 입력 잠금을 획득합니다.</summary>
    public void Acquire()
    {
        if (_isAcquired || !IsConfigured)
        {
            return;
        }

        _hadMoveControl = _moveController.CanMove;
        _hadInteractionControl = _interactionController.CanInteract;
        _moveController.SetCanMove(false);
        _interactionController.SetCanInteract(false);
        _isAcquired = true;
    }

    /// <summary>이 객체가 저장한 입력 상태만 복원합니다.</summary>
    public void Release()
    {
        if (!_isAcquired)
        {
            return;
        }

        _moveController.SetCanMove(_hadMoveControl);
        _interactionController.SetCanInteract(_hadInteractionControl);
        _isAcquired = false;
    }
}

[Serializable]
public sealed class ShopTransactionPanel
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _totalPriceText;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Button _increaseButton;
    [SerializeField] private Button _decreaseButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _confirmButton;

    public void AddListeners(
        UnityAction increase,
        UnityAction decrease,
        UnityAction cancel,
        UnityAction confirm)
    {
        _increaseButton?.onClick.AddListener(increase);
        _decreaseButton?.onClick.AddListener(decrease);
        _cancelButton?.onClick.AddListener(cancel);
        _confirmButton?.onClick.AddListener(confirm);
    }

    public void RemoveListeners(
        UnityAction increase,
        UnityAction decrease,
        UnityAction cancel,
        UnityAction confirm)
    {
        _increaseButton?.onClick.RemoveListener(increase);
        _decreaseButton?.onClick.RemoveListener(decrease);
        _cancelButton?.onClick.RemoveListener(cancel);
        _confirmButton?.onClick.RemoveListener(confirm);
    }

    public void Show(InventoryItemDisplayData data, int quantity, int totalPrice)
    {
        if (_root != null)
        {
            _root.SetActive(true);
        }

        SetImage(_iconImage, data != null ? data.Icon : null);
        SetText(_nameText, data != null ? data.DisplayName : string.Empty);
        UpdateValues(quantity, totalPrice);
    }

    public void UpdateValues(int quantity, int totalPrice)
    {
        SetText(_quantityText, quantity.ToString());
        SetText(_totalPriceText, totalPrice.ToString());
    }

    public void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
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

//=============================================================================
//
// Synopsis:   ShopView;
//                - 구매·판매 행을 필요한 수만 생성하고 이후 재사용
//                - UI 입력을 상태 변경 없이 Presenter에 전달
//=============================================================================
[DisallowMultipleComponent]
public sealed class ShopView : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;
    [SerializeField] private ScrollRect _buyScrollRect;
    [SerializeField] private ScrollRect _sellScrollRect;
    [SerializeField] private ShopItemView _itemViewPrefab;

    [Header("Tooltip")]
    [SerializeField] private GameObject _tooltipRoot;
    [SerializeField] private Image _tooltipIconImage;
    [SerializeField] private TMP_Text _tooltipNameText;
    [SerializeField] private TMP_Text _tooltipPriceText;
    [SerializeField] private TMP_Text _tooltipDescriptionText;
    [SerializeField] private TMP_Text _tooltipOwnedText;

    [Header("Transaction")]
    [SerializeField] private ShopTransactionPanel _purchasePanel = new ShopTransactionPanel();
    [SerializeField] private ShopTransactionPanel _salePanel = new ShopTransactionPanel();

    private readonly List<ShopItemView> _buyViews = new List<ShopItemView>();
    private readonly List<ShopItemView> _sellViews = new List<ShopItemView>();
    private bool _isInitialized;

    public event Action CloseRequested;
    public event Action<ShopUiMode> IncreaseRequested;
    public event Action<ShopUiMode> DecreaseRequested;
    public event Action<ShopUiMode> CancelRequested;
    public event Action<ShopUiMode> ConfirmRequested;

    /// <summary>상점 목록을 열기 위한 필수 Inspector 참조가 연결되었는지 확인합니다.</summary>
    public bool IsConfigured => _root != null
        && _itemViewPrefab != null
        && _buyScrollRect != null && _buyScrollRect.content != null
        && _sellScrollRect != null && _sellScrollRect.content != null;

    private void OnDestroy()
    {
        RemoveListeners();
    }

    /// <summary>버튼 Listener를 한 번만 연결하고 모든 하위 패널을 닫습니다.</summary>
    public void Init()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _closeButton?.onClick.AddListener(HandleClose);
        _purchasePanel.AddListeners(HandleBuyIncrease, HandleBuyDecrease, HandleBuyCancel, HandleBuyConfirm);
        _salePanel.AddListeners(HandleSellIncrease, HandleSellDecrease, HandleSellCancel, HandleSellConfirm);
        Hide();
    }

    /// <summary>상점 메인 창을 열고 Scroll 위치를 처음으로 돌립니다.</summary>
    public void Show()
    {
        SetActive(_root, true);

        if (_buyScrollRect != null)
        {
            _buyScrollRect.verticalNormalizedPosition = 1.0f;
        }

        if (_sellScrollRect != null)
        {
            _sellScrollRect.verticalNormalizedPosition = 1.0f;
        }
    }

    /// <summary>상점과 모든 하위 창을 닫고 풀의 활성 행을 숨깁니다.</summary>
    public void Hide()
    {
        HideTooltip();
        HideTransactions();
        CompleteRows(ShopUiMode.SHOP_UI_BUY, 0);
        CompleteRows(ShopUiMode.SHOP_UI_SELL, 0);
        SetActive(_root, false);
    }

    /// <summary>지정 목록의 행을 확보해 표시 데이터를 Bind합니다.</summary>
    public void BindRow(
        ShopUiMode mode,
        int index,
        InventoryItemDisplayData data,
        int listQuantity,
        int ownedQuantity,
        bool isSoldOut,
        Action<ShopItemView> clicked,
        Action<ShopItemView> entered,
        Action<ShopItemView> exited)
    {
        ShopItemView itemView = GetOrCreateRow(mode, index);

        if (itemView != null)
        {
            itemView.Bind(data, mode, listQuantity, ownedQuantity, isSoldOut, clicked, entered, exited);
        }
    }

    /// <summary>사용 중인 행 뒤의 남은 풀 행을 숨깁니다.</summary>
    public void CompleteRows(ShopUiMode mode, int usedCount)
    {
        List<ShopItemView> views = GetViews(mode);

        for (int i = Mathf.Max(0, usedCount); i < views.Count; i++)
        {
            views[i]?.Clear();
        }
    }

    /// <summary>캐시된 아이템 설명과 현재 보유 수량을 표시합니다.</summary>
    public void ShowTooltip(InventoryItemDisplayData data, int ownedQuantity)
    {
        SetActive(_tooltipRoot, data != null);
        SetImage(_tooltipIconImage, data != null ? data.Icon : null);
        SetText(_tooltipNameText, data != null ? data.DisplayName : string.Empty);
        SetText(_tooltipPriceText, data != null ? data.Price.ToString() : string.Empty);
        SetText(_tooltipDescriptionText, data != null ? data.Description : string.Empty);
        SetText(_tooltipOwnedText, Mathf.Max(0, ownedQuantity).ToString());
    }

    /// <summary>아이템 설명 창을 닫습니다.</summary>
    public void HideTooltip()
    {
        SetActive(_tooltipRoot, false);
    }

    /// <summary>선택한 종류의 구매·판매 수량 창을 표시합니다.</summary>
    public void ShowTransaction(ShopUiMode mode, InventoryItemDisplayData data, int quantity, int totalPrice)
    {
        HideTransactions();
        GetPanel(mode).Show(data, quantity, totalPrice);
    }

    /// <summary>열린 수량 창의 수량과 총가격을 갱신합니다.</summary>
    public void UpdateTransaction(ShopUiMode mode, int quantity, int totalPrice)
    {
        GetPanel(mode).UpdateValues(quantity, totalPrice);
    }

    /// <summary>구매와 판매 수량 창을 모두 닫습니다.</summary>
    public void HideTransactions()
    {
        _purchasePanel.Hide();
        _salePanel.Hide();
    }

    private ShopItemView GetOrCreateRow(ShopUiMode mode, int index)
    {
        List<ShopItemView> views = GetViews(mode);
        ScrollRect scrollRect = mode == ShopUiMode.SHOP_UI_BUY ? _buyScrollRect : _sellScrollRect;

        if (index < 0 || _itemViewPrefab == null || scrollRect == null || scrollRect.content == null)
        {
            return null;
        }

        while (views.Count <= index)
        {
            views.Add(Instantiate(_itemViewPrefab, scrollRect.content));
        }

        return views[index];
    }

    private List<ShopItemView> GetViews(ShopUiMode mode)
    {
        return mode == ShopUiMode.SHOP_UI_BUY ? _buyViews : _sellViews;
    }

    private ShopTransactionPanel GetPanel(ShopUiMode mode)
    {
        return mode == ShopUiMode.SHOP_UI_BUY ? _purchasePanel : _salePanel;
    }

    private void RemoveListeners()
    {
        if (!_isInitialized)
        {
            return;
        }

        _closeButton?.onClick.RemoveListener(HandleClose);
        _purchasePanel.RemoveListeners(HandleBuyIncrease, HandleBuyDecrease, HandleBuyCancel, HandleBuyConfirm);
        _salePanel.RemoveListeners(HandleSellIncrease, HandleSellDecrease, HandleSellCancel, HandleSellConfirm);
        _isInitialized = false;
    }

    private void HandleClose() => CloseRequested?.Invoke();
    private void HandleBuyIncrease() => IncreaseRequested?.Invoke(ShopUiMode.SHOP_UI_BUY);
    private void HandleBuyDecrease() => DecreaseRequested?.Invoke(ShopUiMode.SHOP_UI_BUY);
    private void HandleBuyCancel() => CancelRequested?.Invoke(ShopUiMode.SHOP_UI_BUY);
    private void HandleBuyConfirm() => ConfirmRequested?.Invoke(ShopUiMode.SHOP_UI_BUY);
    private void HandleSellIncrease() => IncreaseRequested?.Invoke(ShopUiMode.SHOP_UI_SELL);
    private void HandleSellDecrease() => DecreaseRequested?.Invoke(ShopUiMode.SHOP_UI_SELL);
    private void HandleSellCancel() => CancelRequested?.Invoke(ShopUiMode.SHOP_UI_SELL);
    private void HandleSellConfirm() => ConfirmRequested?.Invoke(ShopUiMode.SHOP_UI_SELL);

    private static void SetActive(GameObject target, bool isActive)
    {
        target?.SetActive(isActive);
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
