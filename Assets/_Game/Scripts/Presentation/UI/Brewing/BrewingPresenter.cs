//=============================================================================
// File:        BrewingPresenter.cs
//
// Description: 제조 팝업 열기·닫기, 선택 패널 구성, 슬롯 입력과 Controller 호출을 중개한다.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//========================================
// System Includes
//========================================
using System.Collections.Generic;
using UnityEngine;

//=============================================================================
//
// Synopsis:   BrewingPresenter;
//                - 드롭·클릭·버튼 입력을 IBrewingService 호출로 변환
//                - 규칙 위반(서비스가 false 반환) 시 제조 버튼 점멸을 요청
//                - 팝업이 열려 있는 동안만 플레이어 입력을 잠금
//=============================================================================
[DisallowMultipleComponent]
public sealed class BrewingPresenter : MonoBehaviour
{
    //========================================
    // 직렬화 필드
    //========================================

    [Tooltip("제조 팝업 View입니다.")]
    [SerializeField] private BrewingView _view;

    [Tooltip("팝업이 열려 있는 동안 이동을 잠글 플레이어 이동 컨트롤러입니다.")]
    [SerializeField] private PlayerMoveController _playerMoveController;

    [Tooltip("팝업이 열려 있는 동안 상호작용을 잠글 플레이어 상호작용 컨트롤러입니다.")]
    [SerializeField] private PlayerInteractionController _playerInteractionController;

    //========================================
    // 내부 상태
    //========================================

    private IBrewingService _brewing;
    private IInventoryService _inventory;

    // 상점 팝업과 같은 이동·상호작용 잠금 동작을 재사용한다. (ShopView.cs)
    private ShopInputLock _inputLock;

    // 선택 패널 칸 목록입니다. 레시피에 쓰이는 아이템 수만큼 한 번만 생성합니다.
    private readonly List<BrewingPaletteItemView> _paletteItems = new List<BrewingPaletteItemView>();

    private bool _isOpen;

    //******************************************************************************
    //
    // Unity Lifecycle
    //
    //******************************************************************************

    private void OnDisable()
    {
        Close();
    }

    private void OnDestroy()
    {
        Close();
        Unsubscribe();
    }

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // BrewingPresenter.Init
    //=============================================================================
    // Description: 서비스와 View 입력을 연결하고 선택 패널 칸을 생성한다.
    //
    // Parameters:  ( IBrewingService brewing, IInventoryService inventory )
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>서비스와 View 입력을 연결하고 선택 패널 칸을 생성합니다.</summary>
    public void Init(IBrewingService brewing, IInventoryService inventory)
    {
        Close();
        Unsubscribe();
        _brewing = brewing;
        _inventory = inventory;
        _inputLock = new ShopInputLock(_playerMoveController, _playerInteractionController);

        if (_view == null || _brewing == null || _inventory == null)
        {
            Debug.LogWarning("[BrewingPresenter] View 또는 서비스 연결이 누락됐습니다.");
            return;
        }

        _view.Init();
        _view.CloseRequested += Close;
        _view.BrewRequested += HandleBrewRequested;
        _view.SlotDropped += HandleSlotDropped;
        _view.SlotClicked += HandleSlotClicked;
        _brewing.StateChanged += HandleStateChanged;

        if (_paletteItems.Count == 0)
        {
            BuildPalette(_brewing.IngredientItemIds, false);
            BuildPalette(_brewing.ToolItemIds, true);
        }
    }

    //=============================================================================
    // BrewingPresenter.Open
    //=============================================================================
    // Description: 입력을 잠그고 최신 수량으로 선택 패널과 슬롯을 갱신한 뒤 팝업을 연다.
    //
    // Parameters:  None.
    //
    // Return:      bool — 팝업을 열었으면 true
    //
    //=============================================================================
    /// <summary>제조 팝업을 엽니다.</summary>
    public bool Open()
    {
        if (_isOpen || !isActiveAndEnabled || _brewing == null || _view == null
            || !_view.IsConfigured || _inputLock == null || !_inputLock.IsConfigured)
        {
            return false;
        }

        _isOpen = true;
        _inputLock.Acquire();
        _inventory.ItemCountChanged += HandleItemCountChanged;
        RefreshPalette();
        RefreshSlots();
        _view.Show();
        return true;
    }

    //=============================================================================
    // BrewingPresenter.Close
    //=============================================================================
    // Description: 슬롯 등록을 해제하고 남은 술을 회수한 뒤 팝업을 닫는다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>제조 팝업을 닫습니다. 결과 슬롯에 남은 술은 인벤토리로 회수됩니다.</summary>
    public void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        // 먼저 닫힘 상태로 바꿔 CloseSession이 발행하는 상태 변경에 화면을 갱신하지 않게 한다.
        _isOpen = false;
        _inventory.ItemCountChanged -= HandleItemCountChanged;
        _brewing.CloseSession();
        _view.Hide();
        _inputLock.Release();
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    private void BuildPalette(IReadOnlyList<string> itemIds, bool isTool)
    {
        for (int i = 0; itemIds != null && i < itemIds.Count; i++)
        {
            if (!_inventory.TryGetItemDisplay(itemIds[i], out InventoryItemDisplayData data))
            {
                Debug.LogWarning($"[BrewingPresenter] 아이템 정의를 찾을 수 없습니다. itemId={itemIds[i]}");
                continue;
            }

            BrewingPaletteItemView item = _view.AddPaletteItem(isTool);
            item.Bind(data.ItemId, data.Icon, 0);
            _paletteItems.Add(item);
        }
    }

    private void RefreshPalette()
    {
        for (int i = 0; i < _paletteItems.Count; i++)
        {
            _paletteItems[i].SetCount(_inventory.GetItemCount(_paletteItems[i].ItemId));
        }
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < _brewing.IngredientSlotCount; i++)
        {
            _view.SetIngredientIcon(i, GetIcon(_brewing.GetIngredientItemId(i)));
        }

        _view.SetToolIcon(GetIcon(_brewing.ToolItemId));
        _view.SetResultIcon(GetIcon(_brewing.ResultItemId));
    }

    private Sprite GetIcon(string itemId)
    {
        return itemId != null && _inventory.TryGetItemDisplay(itemId, out InventoryItemDisplayData data)
            ? data.Icon
            : null;
    }

    private void HandleStateChanged()
    {
        if (_isOpen)
        {
            RefreshSlots();
        }
    }

    // 제조 성공으로 수량이 0이 되면 해당 칸이 즉시 실루엣으로 바뀐다.
    private void HandleItemCountChanged(string itemId, int finalCount)
    {
        for (int i = 0; i < _paletteItems.Count; i++)
        {
            if (_paletteItems[i].ItemId == itemId)
            {
                _paletteItems[i].SetCount(finalCount);
            }
        }
    }

    private void HandleBrewRequested()
    {
        if (!_brewing.TryBrew())
        {
            _view.PlayErrorFlash();
        }
    }

    private void HandleSlotDropped(BrewingSlotView slot, string itemId)
    {
        bool isRegistered;

        switch (slot.Kind)
        {
            case BrewingSlotKind.BREWING_SLOT_INGREDIENT:
                isRegistered = _brewing.TrySetIngredient(slot.Index, itemId);
                break;
            case BrewingSlotKind.BREWING_SLOT_TOOL:
                isRegistered = _brewing.TrySetTool(itemId);
                break;
            default:
                // 결과 슬롯에는 아이템을 등록할 수 없다. (슬롯 유형 불일치)
                isRegistered = false;
                break;
        }

        if (!isRegistered)
        {
            _view.PlayErrorFlash();
        }
    }

    // 제거 입력 방식은 기획서 미정 항목이라 임시로 "슬롯 클릭"에 연결했다. 바꿀 때는 이 메서드만 수정한다.
    private void HandleSlotClicked(BrewingSlotView slot)
    {
        switch (slot.Kind)
        {
            case BrewingSlotKind.BREWING_SLOT_INGREDIENT:
                _brewing.ClearIngredient(slot.Index);
                break;
            case BrewingSlotKind.BREWING_SLOT_TOOL:
                _brewing.ClearTool();
                break;
            default:
                _brewing.TryCollectResult();
                break;
        }
    }

    private void Unsubscribe()
    {
        if (_view != null)
        {
            _view.CloseRequested -= Close;
            _view.BrewRequested -= HandleBrewRequested;
            _view.SlotDropped -= HandleSlotDropped;
            _view.SlotClicked -= HandleSlotClicked;
        }

        if (_brewing != null)
        {
            _brewing.StateChanged -= HandleStateChanged;
        }
    }
}
