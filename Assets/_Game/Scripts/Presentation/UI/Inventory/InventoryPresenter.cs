using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//==============================================================================
// 기능: class::InventoryPresenter
// 설명: 인벤토리 UI 열기/닫기, 카테고리 전환, 선택 입력을 중개하는 Presenter
//==============================================================================
[DisallowMultipleComponent]
public class InventoryPresenter : MonoBehaviour
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 외부 클릭 닫기를 무시할 프레임이 없음을 나타냅니다.
    private const int INVENTORY_NO_IGNORE_FRAME = -1;

    // All 탭의 기본 위치를 선택 상태로 사용합니다.
    private const float INVENTORY_CATEGORY_SELECTED_X = -90.0f;
    private const float INVENTORY_CATEGORY_INSET_X = -60.0f;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("View")]
    [Tooltip("인벤토리 슬롯과 상세 정보를 표시하는 View입니다.")]
    [SerializeField]
    private InventoryView _inventoryView;

    [Header("View")]
    [Tooltip("외부 클릭 닫기 판정에 사용할 인벤토리 팝업 패널 RectTransform입니다.")]
    [SerializeField]
    private RectTransform _popupPanelRoot;

    [Header("View")]
    [Tooltip("Screen Space Camera UI에서 좌표 변환에 사용할 이벤트 카메라입니다. Overlay면 비워둡니다.")]
    [SerializeField]
    private Camera _uiEventCamera;

    [Header("Open Close")]
    [Tooltip("HUD에서 인벤토리 팝업을 여는 버튼입니다.")]
    [SerializeField]
    private Button _hudOpenButton;

    [Header("Open Close")]
    [Tooltip("인벤토리 팝업을 닫는 X 버튼입니다.")]
    [SerializeField]
    private Button _closeButton;

    [Header("Open Close")]
    [Tooltip("외부 클릭 닫기를 사용할지 여부입니다.")]
    [SerializeField]
    private bool _closeWhenClickOutside = true;

    [Header("Open Close")]
    [Tooltip("Tab 키로 인벤토리 팝업 열기/닫기를 사용할지 여부입니다.")]
    [SerializeField]
    private bool _toggleWithTabKey = true;

    [Header("Open Close")]
    [Tooltip("Esc 키로 인벤토리 팝업 닫기를 사용할지 여부입니다.")]
    [SerializeField]
    private bool _closeWithEscapeKey = true;

    [Header("Category")]
    [Tooltip("처음 열 때 표시할 인벤토리 카테고리입니다.")]
    [SerializeField]
    private InventoryCategory _defaultCategory = InventoryCategory.INVENTORY_CATEGORY_ALL;

    [Header("Category")]
    [Tooltip("전체 카테고리 버튼입니다.")]
    [SerializeField]
    private Button _allCategoryButton;

    [Header("Category")]
    [Tooltip("퀘스트 아이템 카테고리 버튼입니다.")]
    [SerializeField]
    private Button _questCategoryButton;

    [Header("Category")]
    [Tooltip("술 카테고리 버튼입니다.")]
    [SerializeField]
    private Button _alcoholCategoryButton;

    [Header("Category")]
    [Tooltip("유물 카테고리 버튼입니다.")]
    [SerializeField]
    private Button _relicCategoryButton;

    [Header("Category")]
    [Tooltip("기타 카테고리 버튼입니다.")]
    [SerializeField]
    private Button _miscCategoryButton;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 카테고리에서 표시 중인 슬롯 데이터 스냅샷입니다.
    private readonly List<InventorySlotData> _currentSlots = new List<InventorySlotData>();

    // 현재 카테고리에서 표시 중인 아이템 원본 데이터 스냅샷입니다.
    private readonly List<InventoryItemDisplayData> _currentItems = new List<InventoryItemDisplayData>();

    // 인벤토리 도메인 데이터 변경과 조회를 위임할 서비스입니다.
    private IInventoryService _inventoryService;

    // 현재 표시 중인 인벤토리 카테고리입니다.
    private InventoryCategory _currentCategory;

    // 현재 선택된 슬롯 데이터입니다.
    private InventorySlotData _selectedSlotData;

    // 현재 인벤토리 팝업이 열려 있는지 여부입니다.
    private bool _isOpen;

    // 전용 선택 UI가 일반 인벤토리 열기 입력을 차단하고 있는지 여부입니다.
    private bool _isExternalInputBlocked;

    // 팝업을 연 프레임의 외부 클릭 닫기를 방지하기 위한 프레임 값입니다.
    private int _ignoreOutsideClickFrame = INVENTORY_NO_IGNORE_FRAME;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// UI 키 입력과 외부 클릭 닫기를 갱신합니다.
    /// </summary>
    private void Update()
    {
        HandleKeyboardInput();
        HandleOutsideClickInput();
    }

    /// <summary>
    /// 연결한 버튼 이벤트를 정리합니다.
    /// </summary>
    private void OnDestroy()
    {
        UnregisterButtonListeners();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>현재 인벤토리 팝업이 열려 있는지 여부입니다.</summary>
    public bool IsOpen => _isOpen;

    /// <summary>전용 선택 UI가 일반 인벤토리 열기 입력을 차단하는지 여부입니다.</summary>
    public bool IsExternalInputBlocked => _isExternalInputBlocked;

    /// <summary>
    /// GameRoot에서 호출해 인벤토리 서비스와 UI 입력 콜백을 연결합니다.
    /// </summary>
    /// <param name="inventoryService">인벤토리 도메인 서비스입니다.</param>
    public void Init(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        _currentCategory = _defaultCategory;
        _selectedSlotData = null;
        _isOpen = false;
        _isExternalInputBlocked = false;

        RegisterButtonListeners();
        UpdateCategoryButtonStates();

        if (_inventoryView != null)
        {
            _inventoryView.SetCallbacks(HandleSlotSelected, HandleSlotMoved);
            _inventoryView.ClearSelectedItem();
            _inventoryView.Hide();
        }
    }

    /// <summary>
    /// 인벤토리 팝업을 엽니다.
    /// </summary>
    public void Open()
    {
        if (_isExternalInputBlocked)
        {
            return;
        }

        if (_inventoryView == null || _inventoryService == null)
        {
            Debug.LogWarning("[InventoryPresenter] InventoryView 또는 InventoryService가 연결되지 않았습니다.");
            return;
        }

        _isOpen = true;
        _ignoreOutsideClickFrame = Time.frameCount;
        _currentCategory = _defaultCategory;
        _selectedSlotData = null;
        UpdateCategoryButtonStates();

        _inventoryView.Show();
        _inventoryView.ClearSelectedItem();
        RefreshSlots();
        _inventoryView.ResetSlotScroll();
    }

    /// <summary>
    /// 인벤토리 팝업을 닫습니다.
    /// </summary>
    public void Close()
    {
        _isOpen = false;
        _ignoreOutsideClickFrame = INVENTORY_NO_IGNORE_FRAME;
        _selectedSlotData = null;

        if (_inventoryView != null)
        {
            _inventoryView.ClearSelectedItem();
            _inventoryView.Hide();
        }
    }

    /// <summary>
    /// 인벤토리 팝업 열림 상태를 전환합니다.
    /// </summary>
    public void Toggle()
    {
        if (_isExternalInputBlocked)
        {
            return;
        }

        if (_isOpen)
        {
            Close();
            return;
        }

        Open();
    }

    /// <summary>전용 선택 UI가 활성화된 동안 일반 인벤토리 열기 입력을 차단합니다.</summary>
    /// <param name="blocked">일반 인벤토리 열기 입력을 차단하면 true입니다.</param>
    public void SetExternalInputBlocked(bool blocked)
    {
        _isExternalInputBlocked = blocked;
        if (blocked)
        {
            Close();
        }
    }

    /// <summary>
    /// 인벤토리 카테고리를 변경하고 슬롯 페이지를 갱신합니다.
    /// </summary>
    /// <param name="category">표시할 인벤토리 카테고리입니다.</param>
    public void SetCategory(InventoryCategory category)
    {
        _currentCategory = category;
        _selectedSlotData = null;
        UpdateCategoryButtonStates();

        if (_inventoryView != null)
        {
            _inventoryView.ClearSelectedItem();
            _inventoryView.ShowSlotPage();
        }

        RefreshSlots();
        _inventoryView?.ResetSlotScroll();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 선택한 탭만 페이지 바깥으로 내밀고 나머지는 안쪽에 둡니다.
    /// </summary>
    private void UpdateCategoryButtonStates()
    {
        SetCategoryButtonState(_allCategoryButton,
            _currentCategory == InventoryCategory.INVENTORY_CATEGORY_ALL);
        SetCategoryButtonState(_questCategoryButton,
            _currentCategory == InventoryCategory.INVENTORY_CATEGORY_QUEST);
        SetCategoryButtonState(_alcoholCategoryButton,
            _currentCategory == InventoryCategory.INVENTORY_CATEGORY_ALCOHOL);
        SetCategoryButtonState(_relicCategoryButton,
            _currentCategory == InventoryCategory.INVENTORY_CATEGORY_RELIC);
        SetCategoryButtonState(_miscCategoryButton,
            _currentCategory == InventoryCategory.INVENTORY_CATEGORY_MISC);
    }

    private static void SetCategoryButtonState(Button button, bool isSelected)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector2 position = rect.anchoredPosition;
            position.x = isSelected ? INVENTORY_CATEGORY_SELECTED_X : INVENTORY_CATEGORY_INSET_X;
            rect.anchoredPosition = position;
        }

        button.transition = Selectable.Transition.None;
        button.interactable = true;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = Color.white;
        }
    }

    /// <summary>
    /// 버튼 클릭 입력을 Presenter 메서드에 연결합니다.
    /// </summary>
    private void RegisterButtonListeners()
    {
        UnregisterButtonListeners();

        if (_hudOpenButton != null)
        {
            _hudOpenButton.onClick.AddListener(Open);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        if (_allCategoryButton != null)
        {
            _allCategoryButton.onClick.AddListener(ShowAllCategory);
        }

        if (_questCategoryButton != null)
        {
            _questCategoryButton.onClick.AddListener(ShowQuestCategory);
        }

        if (_alcoholCategoryButton != null)
        {
            _alcoholCategoryButton.onClick.AddListener(ShowAlcoholCategory);
        }

        if (_relicCategoryButton != null)
        {
            _relicCategoryButton.onClick.AddListener(ShowRelicCategory);
        }

        if (_miscCategoryButton != null)
        {
            _miscCategoryButton.onClick.AddListener(ShowMiscCategory);
        }
    }

    /// <summary>
    /// 버튼 클릭 입력 연결을 해제합니다.
    /// </summary>
    private void UnregisterButtonListeners()
    {
        if (_hudOpenButton != null)
        {
            _hudOpenButton.onClick.RemoveListener(Open);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }

        if (_allCategoryButton != null)
        {
            _allCategoryButton.onClick.RemoveListener(ShowAllCategory);
        }

        if (_questCategoryButton != null)
        {
            _questCategoryButton.onClick.RemoveListener(ShowQuestCategory);
        }

        if (_alcoholCategoryButton != null)
        {
            _alcoholCategoryButton.onClick.RemoveListener(ShowAlcoholCategory);
        }

        if (_relicCategoryButton != null)
        {
            _relicCategoryButton.onClick.RemoveListener(ShowRelicCategory);
        }

        if (_miscCategoryButton != null)
        {
            _miscCategoryButton.onClick.RemoveListener(ShowMiscCategory);
        }
    }

    /// <summary>
    /// 키보드로 인벤토리 열기/닫기를 처리합니다.
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (_isExternalInputBlocked)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (_toggleWithTabKey && keyboard.tabKey.wasPressedThisFrame)
        {
            Toggle();
            return;
        }

        if (_isOpen && _closeWithEscapeKey && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    /// <summary>
    /// 팝업 바깥 마우스 클릭으로 인벤토리를 닫습니다.
    /// </summary>
    private void HandleOutsideClickInput()
    {
        if (_isOpen == false || _closeWhenClickOutside == false || _popupPanelRoot == null)
        {
            return;
        }

        if (_ignoreOutsideClickFrame == Time.frameCount)
        {
            return;
        }

        Mouse mouse = Mouse.current;

        if (mouse == null || mouse.leftButton.wasPressedThisFrame == false)
        {
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();

        if (RectTransformUtility.RectangleContainsScreenPoint(_popupPanelRoot, screenPosition, _uiEventCamera))
        {
            return;
        }

        Close();
    }

    /// <summary>
    /// 현재 카테고리 슬롯과 표시 데이터를 다시 조회해 View에 전달합니다.
    /// </summary>
    private void RefreshSlots()
    {
        _currentSlots.Clear();
        _currentItems.Clear();

        if (_inventoryService == null)
        {
            ApplySlotsToView();
            return;
        }

        IReadOnlyList<InventorySlotData> slots = _inventoryService.GetSlots(_currentCategory);

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData slotData = slots[i];
            _currentSlots.Add(slotData);
            _currentItems.Add(GetItemData(slotData));
        }

        ApplySlotsToView();
    }

    /// <summary>
    /// 현재 슬롯 스냅샷을 View에 반영합니다.
    /// </summary>
    private void ApplySlotsToView()
    {
        if (_inventoryView == null)
        {
            return;
        }

        _inventoryView.SetSlots(_currentSlots, _currentItems, _selectedSlotData);
    }

    /// <summary>
    /// 슬롯 데이터로 아이템 원본 데이터를 조회합니다.
    /// </summary>
    /// <param name="slotData">조회할 슬롯 데이터입니다.</param>
    /// <returns>조회된 아이템 원본 데이터입니다. 찾지 못하면 null입니다.</returns>
    private InventoryItemDisplayData GetItemData(InventorySlotData slotData)
    {
        if (_inventoryService == null || slotData == null || slotData.TryGetItemId(out string itemId) == false)
        {
            return null;
        }

        return _inventoryService.TryGetItemDisplay(itemId, out InventoryItemDisplayData itemData) ? itemData : null;
    }

    /// <summary>
    /// 슬롯 선택 입력을 처리하고 new 표시 해제를 인벤토리 서비스에 위임합니다.
    /// </summary>
    /// <param name="slotIndex">선택한 슬롯 인덱스입니다.</param>
    private void HandleSlotSelected(int slotIndex)
    {
        if (TryGetSlotData(slotIndex, out InventorySlotData slotData) == false)
        {
            _selectedSlotData = null;

            if (_inventoryView != null)
            {
                _inventoryView.ClearSelectedItem();
            }

            RefreshSlots();
            return;
        }

        _selectedSlotData = slotData;
        InventoryItemDisplayData itemData = GetItemData(slotData);

        if (_inventoryService != null && slotData.TryGetItemId(out string itemId))
        {
            _inventoryService.TryMarkItemChecked(itemId);
        }

        if (_inventoryView != null)
        {
            _inventoryView.SetSelectedItem(itemData);
            _inventoryView.ShowSelectedItemPage();
        }

        RefreshSlots();
    }

    /// <summary>
    /// 슬롯 드래그 이동 입력을 인벤토리 서비스에 위임합니다.
    /// </summary>
    /// <param name="sourceSlotIndex">이동할 슬롯 인덱스입니다.</param>
    /// <param name="targetSlotIndex">이동 대상 슬롯 인덱스입니다.</param>
    private void HandleSlotMoved(int sourceSlotIndex, int targetSlotIndex)
    {
        if (_inventoryService == null)
        {
            return;
        }

        TryGetSlotData(sourceSlotIndex, out InventorySlotData movedSlotData);

        if (_inventoryService.TryMoveSlot(_currentCategory, sourceSlotIndex, targetSlotIndex) == false)
        {
            return;
        }

        _selectedSlotData = movedSlotData;
        RefreshSlots();
    }

    /// <summary>
    /// 인덱스로 현재 슬롯 데이터를 조회합니다.
    /// </summary>
    /// <param name="slotIndex">조회할 슬롯 인덱스입니다.</param>
    /// <param name="slotData">조회된 슬롯 데이터입니다.</param>
    /// <returns>실제 아이템 슬롯 데이터가 있으면 true입니다.</returns>
    private bool TryGetSlotData(int slotIndex, out InventorySlotData slotData)
    {
        slotData = null;

        if (slotIndex < 0 || slotIndex >= _currentSlots.Count)
        {
            return false;
        }

        slotData = _currentSlots[slotIndex];
        return slotData != null && slotData.IsEmpty == false;
    }

    /// <summary>
    /// 전체 카테고리를 표시합니다.
    /// </summary>
    private void ShowAllCategory()
    {
        SetCategory(InventoryCategory.INVENTORY_CATEGORY_ALL);
    }

    /// <summary>퀘스트 아이템 카테고리를 표시합니다.</summary>
    private void ShowQuestCategory()
    {
        SetCategory(InventoryCategory.INVENTORY_CATEGORY_QUEST);
    }

    /// <summary>
    /// 술 카테고리를 표시합니다.
    /// </summary>
    private void ShowAlcoholCategory()
    {
        SetCategory(InventoryCategory.INVENTORY_CATEGORY_ALCOHOL);
    }

    /// <summary>
    /// 유물 카테고리를 표시합니다.
    /// </summary>
    private void ShowRelicCategory()
    {
        SetCategory(InventoryCategory.INVENTORY_CATEGORY_RELIC);
    }

    /// <summary>
    /// 기타 카테고리를 표시합니다.
    /// </summary>
    private void ShowMiscCategory()
    {
        SetCategory(InventoryCategory.INVENTORY_CATEGORY_MISC);
    }
}
