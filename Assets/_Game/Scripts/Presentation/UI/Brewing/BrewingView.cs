//=============================================================================
// File:        BrewingView.cs
//
// Description: 제조 팝업의 표시, 드래그 아이콘, 제조 버튼 점멸과 버튼 입력을 관리한다.
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

//========================================
// Project Includes
//========================================
using DG.Tweening;

//=============================================================================
//
// Synopsis:   BrewingView;
//                - 상태를 갖지 않고 입력을 이벤트로 Presenter에 전달
//                - 드래그 아이콘 이동과 점멸 연출은 표시 책임이라 View가 소유
//=============================================================================
[DisallowMultipleComponent]
public sealed class BrewingView : MonoBehaviour
{
    //========================================
    // 직렬화 필드
    //========================================

    [Header("Window")]
    [Tooltip("제조 팝업 최상위 오브젝트입니다.")]
    [SerializeField] private GameObject _root;

    [Tooltip("팝업 닫기 버튼입니다.")]
    [SerializeField] private Button _closeButton;

    [Tooltip("제조하기 버튼입니다. 예외 발생 시 이 버튼의 Target Graphic이 붉게 점멸합니다.")]
    [SerializeField] private Button _brewButton;

    [Header("Slots")]
    [Tooltip("재료 슬롯입니다. 배열 순서가 슬롯 인덱스와 같아야 합니다.")]
    [SerializeField] private BrewingSlotView[] _ingredientSlots;

    [Tooltip("도구 슬롯입니다.")]
    [SerializeField] private BrewingSlotView _toolSlot;

    [Tooltip("결과 슬롯(술 칸)입니다.")]
    [SerializeField] private BrewingSlotView _resultSlot;

    [Header("Palette")]
    [Tooltip("재료 선택 칸이 생성될 부모입니다.")]
    [SerializeField] private Transform _ingredientListRoot;

    [Tooltip("도구 선택 칸이 생성될 부모입니다.")]
    [SerializeField] private Transform _toolListRoot;

    [Tooltip("선택 패널 한 칸의 프리팹입니다.")]
    [SerializeField] private BrewingPaletteItemView _paletteItemPrefab;

    [Header("Drag")]
    [Tooltip("드래그 중 포인터를 따라다니는 아이콘입니다. 팝업 안에서 가장 위에 그려집니다.")]
    [SerializeField] private Image _dragIcon;

    [Header("Feedback")]
    [Tooltip("예외 발생 시 제조 버튼이 점멸하는 색상입니다.")]
    [SerializeField] private Color _errorColor = Color.red;

    [Tooltip("점멸 전체 시간(초)입니다. 기획서 기준 0.5초입니다.")]
    [SerializeField] private float _flashDuration = 0.5f;

    //========================================
    // 내부 상태
    //========================================

    // 진행 중인 점멸 트윈 핸들입니다.
    private Tween _flashTween;

    // 점멸 전 제조 버튼 색상입니다.
    private Color _normalColor = Color.white;

    private bool _isInitialized;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>닫기 버튼이 눌렸을 때 발행됩니다.</summary>
    public event Action CloseRequested;

    /// <summary>제조하기 버튼이 눌렸을 때 발행됩니다.</summary>
    public event Action BrewRequested;

    /// <summary>슬롯에 아이템이 드롭됐을 때 발행됩니다.</summary>
    public event Action<BrewingSlotView, string> SlotDropped;

    /// <summary>슬롯이 클릭됐을 때 발행됩니다.</summary>
    public event Action<BrewingSlotView> SlotClicked;

    /// <summary>팝업을 열기 위한 필수 Inspector 참조가 연결됐는지 반환합니다.</summary>
    public bool IsConfigured => _root != null && _paletteItemPrefab != null
        && _ingredientListRoot != null && _toolListRoot != null
        && _toolSlot != null && _resultSlot != null
        && _ingredientSlots != null && _ingredientSlots.Length > 0;

    //******************************************************************************
    //
    // Unity Lifecycle
    //
    //******************************************************************************

    private void OnDestroy()
    {
        _flashTween?.Kill();
    }

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // BrewingView.Init
    //=============================================================================
    // Description: 버튼과 슬롯 입력을 한 번만 연결하고 팝업을 닫아 둔다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>버튼과 슬롯 입력을 한 번만 연결하고 팝업을 닫아 둡니다.</summary>
    public void Init()
    {
        if (_isInitialized || !IsConfigured)
        {
            return;
        }

        _isInitialized = true;
        _closeButton?.onClick.AddListener(HandleCloseClicked);
        _brewButton?.onClick.AddListener(HandleBrewClicked);

        for (int i = 0; i < _ingredientSlots.Length; i++)
        {
            Subscribe(_ingredientSlots[i]);
        }

        Subscribe(_toolSlot);
        Subscribe(_resultSlot);

        // 드래그 아이콘이 포인터 아래의 슬롯 드롭을 가로채지 않게 한다.
        if (_dragIcon != null)
        {
            _dragIcon.raycastTarget = false;
        }

        if (_brewButton != null && _brewButton.targetGraphic != null)
        {
            _normalColor = _brewButton.targetGraphic.color;
        }

        Hide();
    }

    /// <summary>제조 팝업을 엽니다.</summary>
    public void Show()
    {
        _root.SetActive(true);
    }

    /// <summary>제조 팝업을 닫고 드래그·점멸 연출을 정리합니다.</summary>
    public void Hide()
    {
        EndDrag();
        StopFlash();

        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    /// <summary>선택 패널에 재료 또는 도구 칸을 하나 추가하고 드래그 입력을 연결합니다.</summary>
    /// <param name="isTool">true면 도구 목록, false면 재료 목록에 추가합니다.</param>
    public BrewingPaletteItemView AddPaletteItem(bool isTool)
    {
        Transform parent = isTool ? _toolListRoot : _ingredientListRoot;
        BrewingPaletteItemView item = Instantiate(_paletteItemPrefab, parent);
        item.DragBegan += HandleDragBegan;
        item.DragMoved += HandleDragMoved;
        item.DragEnded += HandleDragEnded;
        return item;
    }

    /// <summary>재료 슬롯 아이콘을 갱신합니다.</summary>
    public void SetIngredientIcon(int slotIndex, Sprite icon)
    {
        if (slotIndex >= 0 && slotIndex < _ingredientSlots.Length)
        {
            _ingredientSlots[slotIndex].SetIcon(icon);
        }
    }

    /// <summary>도구 슬롯 아이콘을 갱신합니다.</summary>
    public void SetToolIcon(Sprite icon) => _toolSlot.SetIcon(icon);

    /// <summary>결과 슬롯 아이콘을 갱신합니다.</summary>
    public void SetResultIcon(Sprite icon) => _resultSlot.SetIcon(icon);

    //=============================================================================
    // BrewingView.PlayErrorFlash
    //=============================================================================
    // Description: 제조 버튼을 붉은색으로 점멸시킨다. (원래 색 → 붉은색 → 원래 색)
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>제조 버튼을 붉은색으로 점멸시킵니다.</summary>
    public void PlayErrorFlash()
    {
        Graphic target = _brewButton != null ? _brewButton.targetGraphic : null;

        if (target == null)
        {
            return;
        }

        // 점멸이 겹치면 색이 틀어지므로 이전 트윈을 정리하고 원래 색에서 다시 시작한다.
        StopFlash();
        _flashTween = target
            .DOColor(_errorColor, _flashDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    private void Subscribe(BrewingSlotView slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.ItemDropped += HandleSlotDropped;
        slot.Clicked += HandleSlotClicked;
    }

    private void StopFlash()
    {
        _flashTween?.Kill();
        _flashTween = null;

        if (_brewButton != null && _brewButton.targetGraphic != null)
        {
            _brewButton.targetGraphic.color = _normalColor;
        }
    }

    private void HandleDragBegan(BrewingPaletteItemView item, PointerEventData eventData)
    {
        if (_dragIcon == null)
        {
            return;
        }

        _dragIcon.sprite = item.Icon;
        _dragIcon.color = item.IconColor;
        _dragIcon.enabled = item.Icon != null;
        _dragIcon.transform.SetAsLastSibling();
        MoveDragIcon(eventData);
    }

    private void HandleDragMoved(BrewingPaletteItemView item, PointerEventData eventData)
    {
        MoveDragIcon(eventData);
    }

    private void HandleDragEnded(BrewingPaletteItemView item, PointerEventData eventData)
    {
        EndDrag();
    }

    private void EndDrag()
    {
        if (_dragIcon != null)
        {
            _dragIcon.enabled = false;
        }
    }

    // Canvas 렌더 모드와 무관하게 포인터 위치를 월드 좌표로 바꿔 아이콘을 옮긴다.
    private void MoveDragIcon(PointerEventData eventData)
    {
        RectTransform parent = _dragIcon != null ? _dragIcon.transform.parent as RectTransform : null;

        if (parent != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out Vector3 worldPosition))
        {
            _dragIcon.transform.position = worldPosition;
        }
    }

    private void HandleCloseClicked() => CloseRequested?.Invoke();

    private void HandleBrewClicked() => BrewRequested?.Invoke();

    private void HandleSlotDropped(BrewingSlotView slot, string itemId) => SlotDropped?.Invoke(slot, itemId);

    private void HandleSlotClicked(BrewingSlotView slot) => SlotClicked?.Invoke(slot);
}
