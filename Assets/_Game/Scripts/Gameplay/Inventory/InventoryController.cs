using System;
using System.Collections.Generic;
using UnityEngine;

//==============================================================================
// 기능: class::InventoryController
// 설명: 아이템 보관, 중첩, 소모, 카테고리 조회를 담당하는 인벤토리 컨트롤러
//==============================================================================
[DisallowMultipleComponent]
public class InventoryController : MonoBehaviour, IInventoryService, IInventoryStateTransactionService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 슬롯에 보관할 수 있는 최소 아이템 수량입니다.
    private const int INVENTORY_MIN_ITEM_AMOUNT = 1;

    // 아직 플레이어가 수동 배치하지 않은 슬롯 순서입니다.
    private const int INVENTORY_UNASSIGNED_MANUAL_ORDER = -1;

    private const string INVENTORY_STATE_ID = "inventory.slots";

    [Serializable]
    private sealed class InventorySaveData
    {
        public List<InventorySlotData> Slots = new List<InventorySlotData>();
    }

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [Header("Inventory Visual")]
    [Tooltip("items.csv의 icon_id를 Unity Sprite로 연결하는 카탈로그입니다.")]
    [SerializeField]
    private ItemVisualCatalogSO _visualCatalog;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // CSV에서 생성된 아이템 Definition을 조회할 게임 데이터 저장소입니다.
    private IGameDataRepository _gameDataRepository;

    // 아이템 이름과 설명 Localization Key를 현재 언어 문자열로 변환할 서비스입니다.
    private ILocalizationService _localizationService;

    private IProgressService _progressService;

    // 인벤토리가 현재 보관 중인 런타임 슬롯 목록입니다.
    private readonly List<InventorySlotData> _slots = new List<InventorySlotData>();

    // 카테고리 조회 결과를 재사용하기 위한 캐시 목록입니다.
    private readonly List<InventorySlotData> _visibleSlots = new List<InventorySlotData>();

    // 최근 획득 순 정렬을 위해 슬롯의 현재 리스트 인덱스를 보관하는 캐시입니다.
    private readonly Dictionary<InventorySlotData, int> _slotIndexBySlot = new Dictionary<InventorySlotData, int>();

    // 같은 아이템을 다시 획득했을 때 최근 획득 순서로 옮기기 위한 임시 목록입니다.
    private readonly List<InventorySlotData> _movingSlots = new List<InventorySlotData>();

    // Reset 시 같은 ItemId의 최종 수량 이벤트를 한 번만 보내기 위한 재사용 집합입니다.
    private readonly HashSet<string> _resetItemIds = new HashSet<string>(StringComparer.Ordinal);

    private readonly InventorySaveData _saveData = new InventorySaveData();
    private readonly List<InventorySlotData> _restoredSlots = new List<InventorySlotData>();
    private readonly Dictionary<string, int> _restoredCounts = new Dictionary<string, int>(StringComparer.Ordinal);

    //==============================================================================
    // 공개 이벤트
    //==============================================================================

    /// <summary>아이템 수량 변경 후 ItemId와 최종 보유 수량을 전달합니다.</summary>
    public event Action<string, int> ItemCountChanged;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다.
    /// </summary>
    public void Init(
        IGameDataRepository gameDataRepository,
        ILocalizationService localizationService,
        IProgressService progressService)
    {
        _gameDataRepository = gameDataRepository;
        _localizationService = localizationService;
        _progressService = progressService;
        RestoreState();
    }

    //==============================================================================
    // 공개 API (IInventoryService)
    //==============================================================================

    /// <summary>
    /// CSV 아이템 ID를 기준으로 인벤토리에 아이템을 추가합니다.
    /// </summary>
    /// <param name="itemId">추가할 아이템 ID입니다.</param>
    /// <param name="amount">추가할 아이템 개수입니다.</param>
    /// <returns>아이템 데이터와 개수가 유효하고 추가에 성공하면 true입니다.</returns>
    public bool TryAddItem(string itemId, int amount)
    {
        if (!CanAddItem(itemId, amount)
            || !TryGetItem(itemId, out ItemDefinition itemData))
        {
            return false;
        }

        if (!AddItemAmount(itemData, amount))
        {
            return false;
        }

        NotifyItemCountChanged(itemData.ItemId);
        PersistState();
        return true;
    }

    /// <summary>
    /// 아이템을 현재 수량과 int 범위를 넘지 않고 추가할 수 있는지 확인합니다.
    /// </summary>
    public bool CanAddItem(string itemId, int amount)
    {
        if (amount < INVENTORY_MIN_ITEM_AMOUNT
            || !TryGetItem(itemId, out ItemDefinition itemData)
            || !TryGetItemCount(itemData.ItemId, out int currentCount))
        {
            return false;
        }

        return currentCount <= int.MaxValue - amount;
    }

    /// <summary>
    /// 퀘스트 완료 또는 재료 사용 조건이 만족되었을 때 인벤토리 아이템을 자동 소모합니다.
    /// </summary>
    /// <param name="itemId">소모할 아이템 고유 ID입니다.</param>
    /// <param name="amount">소모할 아이템 개수입니다.</param>
    /// <returns>아이템 ID, 개수, 보유 수량이 유효하고 소모에 성공하면 true입니다.</returns>
    public bool TryConsumeItem(string itemId, int amount)
    {
        if (!TryNormalizeItemId(itemId, out string normalizedItemId))
        {
            return false;
        }

        if (amount < INVENTORY_MIN_ITEM_AMOUNT)
        {
            return false;
        }

        if (!TryGetItemCount(normalizedItemId, out int currentCount)
            || currentCount < amount)
        {
            return false;
        }

        if (!RemoveItemAmount(normalizedItemId, amount))
        {
            return false;
        }

        NotifyItemCountChanged(normalizedItemId);
        PersistState();
        return true;
    }

    /// <summary>
    /// 한 아이템을 제거하고 다른 아이템을 추가하는 교환을 원자적으로 처리합니다.
    /// </summary>
    public bool TryExchangeItem(
        string removeItemId,
        int removeAmount,
        string addItemId,
        int addAmount)
    {
        if (!TryNormalizeItemId(removeItemId, out string normalizedRemoveId)
            || !TryNormalizeItemId(addItemId, out string normalizedAddId)
            || normalizedRemoveId == normalizedAddId
            || removeAmount < INVENTORY_MIN_ITEM_AMOUNT
            || addAmount < INVENTORY_MIN_ITEM_AMOUNT)
        {
            return false;
        }

        if (!TryGetItemCount(normalizedRemoveId, out int removeCount)
            || removeCount < removeAmount
            || !CanAddItem(normalizedAddId, addAmount)
            || !TryGetItem(normalizedAddId, out ItemDefinition addItemData))
        {
            return false;
        }

        // 모든 실패 조건을 먼저 확인했으므로 두 변경은 중간 실패 없이 연속 적용됩니다.
        if (!RemoveItemAmount(normalizedRemoveId, removeAmount)
            || !AddItemAmount(addItemData, addAmount))
        {
            return false;
        }

        NotifyItemCountChanged(normalizedRemoveId);
        NotifyItemCountChanged(normalizedAddId);
        PersistState();
        return true;
    }

    //=============================================================================
    // Description: 슬롯을 임시 복제해 변경하고 연동 상태와 함께 확정합니다.
    // Parameters: 제거/추가 ID·수량, 연동 시스템 ID·JSON, 실패 결과.
    // Return: 두 상태가 함께 적용됐으면 true.
    //=============================================================================
    /// <summary>인벤토리 수량·슬롯과 연동 상태를 원자적으로 적용합니다.</summary>
    public bool TryApplyWithSystemState(string removeId, int removeAmount, string addId, int addAmount,
        string stateId, string stateJson, out InventoryStateChangeFailure failure)
    {
        failure = InventoryStateChangeFailure.INVENTORY_STATE_CHANGE_INVALID;
        ItemDefinition addItem = null;
        if (!(_progressService is IProgressStateTransactionService transaction)
            || removeAmount < 0 || addAmount < 0 || (removeAmount == 0 && addAmount == 0)
            || (removeAmount > 0 && (!TryNormalizeItemId(removeId, out removeId)
                || GetItemCount(removeId) < removeAmount))
            || (addAmount > 0 && (!TryGetItem(addId, out addItem) || !CanAddItem(addId, addAmount)))
            || (removeAmount > 0 && addAmount > 0 && removeId == addItem.ItemId)) return false;
        // 기존 슬롯 객체까지 보존하여 실패 시 new 표시·수동 순서·UI 참조를 유지합니다.
        List<InventorySlotData> original = new List<InventorySlotData>(_slots);
        bool committed = false;
        failure = InventoryStateChangeFailure.INVENTORY_STATE_CHANGE_APPLY_FAILED;
        try
        {
            _slots.Clear();
            for (int i = 0; i < original.Count; i++)
            {
                InventorySlotData source = original[i];
                InventorySlotData copy = new InventorySlotData();
                if (source != null) copy.TrySetSlot(source.ItemId, source.Quantity,
                    source.IsNewItem, source.ManualSlotOrder);
                _slots.Add(source == null ? null : copy);
            }
            if (removeAmount > 0 && !RemoveItemAmount(removeId, removeAmount)) return false;
            if (addAmount > 0 && !AddItemAmount(addItem, addAmount)) return false;
            InventorySaveData save = new InventorySaveData { Slots = _slots };
            committed = transaction.TrySetSystemStates(
                INVENTORY_STATE_ID, JsonUtility.ToJson(save), stateId, stateJson);
            if (!committed) return false;
        }
        catch (Exception) { return false; }
        finally
        {
            if (!committed)
            {
                _slots.Clear();
                _slots.AddRange(original);
            }
        }
        failure = InventoryStateChangeFailure.INVENTORY_STATE_CHANGE_NONE;
        if (removeAmount > 0) NotifyCommittedItemChange(removeId);
        if (addAmount > 0) NotifyCommittedItemChange(addItem.ItemId);
        return true;
    }

    // 커밋 후 구독자 예외는 성공한 상태를 실패로 바꾸거나 후속 저장을 건너뛰지 않습니다.
    private void NotifyCommittedItemChange(string itemId)
    {
        Delegate[] handlers = ItemCountChanged?.GetInvocationList();
        if (handlers == null) return;
        int count = GetItemCount(itemId);
        for (int i = 0; i < handlers.Length; i++)
        {
            try { ((Action<string, int>)handlers[i])(itemId, count); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }

    private bool RemoveItemAmount(string itemId, int amount)
    {
        int remainingAmount = amount;

        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            InventorySlotData slotData = _slots[i];

            if (IsMatchingSlot(slotData, itemId) == false)
            {
                continue;
            }

            if (remainingAmount >= slotData.Quantity)
            {
                remainingAmount -= slotData.Quantity;
                _slots.RemoveAt(i);
            }
            else
            {
                slotData.TrySetQuantity(slotData.Quantity - remainingAmount);
                remainingAmount = 0;
            }

            if (remainingAmount == 0)
            {
                return true;
            }
        }

        return remainingAmount == 0;
    }

    /// <summary>
    /// 아이템 ID로 원본 아이템 데이터를 조회합니다.
    /// </summary>
    /// <param name="itemId">조회할 아이템 고유 ID입니다.</param>
    /// <param name="itemData">조회된 아이템 원본 데이터입니다.</param>
    /// <returns>아이템 데이터를 찾으면 true입니다.</returns>
    public bool TryGetItem(string itemId, out ItemDefinition itemData)
    {
        itemData = null;

        if (_gameDataRepository == null
            || !TryNormalizeItemId(itemId, out string normalizedItemId))
        {
            return false;
        }

        return _gameDataRepository.TryGetItem(normalizedItemId, out itemData);
    }

    /// <summary>
    /// CSV Definition에 현재 언어 문장과 아이콘을 결합한 UI 표시 데이터를 조회합니다.
    /// </summary>
    public bool TryGetItemDisplay(string itemId, out InventoryItemDisplayData displayData)
    {
        displayData = null;

        if (!TryGetItem(itemId, out ItemDefinition itemData))
        {
            return false;
        }

        string displayName = _localizationService != null
            ? _localizationService.GetText(itemData.DisplayNameKey)
            : itemData.DisplayNameKey;
        string description = _localizationService != null
            ? _localizationService.GetText(itemData.DescriptionTextKey)
            : itemData.DescriptionTextKey;
        Sprite icon = null;

        if (_visualCatalog != null && !string.IsNullOrWhiteSpace(itemData.IconId))
        {
            _visualCatalog.TryGetIcon(itemData.IconId, out icon);
        }

        displayData = new InventoryItemDisplayData(itemData, displayName, description, icon);
        return true;
    }

    /// <summary>
    /// 아이템 ID에 해당하는 현재 보유 수량을 조회합니다.
    /// </summary>
    /// <param name="itemId">수량을 조회할 아이템 고유 ID입니다.</param>
    /// <returns>보유 중인 전체 수량입니다. 아이템 ID가 유효하지 않거나 보유하지 않으면 0입니다.</returns>
    public int GetItemCount(string itemId)
    {
        return TryGetItemCount(itemId, out int itemCount) ? itemCount : 0;
    }

    /// <summary>
    /// 카테고리 규칙과 현재 정렬 상태에 맞는 인벤토리 슬롯 목록을 조회합니다.
    /// </summary>
    /// <param name="category">조회할 인벤토리 카테고리입니다.</param>
    /// <returns>카테고리에 포함되는 슬롯 목록입니다.</returns>
    public IReadOnlyList<InventorySlotData> GetSlots(InventoryCategory category)
    {
        _visibleSlots.Clear();

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlotData slotData = _slots[i];

            if (slotData == null || slotData.IsEmpty)
            {
                continue;
            }

            if (IsSlotInCategory(slotData, category))
            {
                _visibleSlots.Add(slotData);
            }
        }

        RebuildSlotIndexCache();
        _visibleSlots.Sort(CompareInventorySlots);

        return _visibleSlots;
    }

    /// <summary>
    /// 현재 보유 슬롯을 최근 획득한 아이템부터 조회합니다.
    /// </summary>
    /// <returns>내부 버퍼를 재사용하는 읽기 전용 슬롯 목록입니다.</returns>
    public IReadOnlyList<InventorySlotData> GetSlotsByAcquisitionOrder()
    {
        _visibleSlots.Clear();

        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            InventorySlotData slotData = _slots[i];

            if (slotData != null && slotData.IsEmpty == false)
            {
                _visibleSlots.Add(slotData);
            }
        }

        return _visibleSlots;
    }

    /// <summary>
    /// 드래그 이동으로 지정된 슬롯의 수동 배치 순서를 변경합니다.
    /// </summary>
    /// <param name="category">현재 표시 중인 인벤토리 카테고리입니다.</param>
    /// <param name="sourceIndex">이동할 슬롯의 현재 표시 인덱스입니다.</param>
    /// <param name="targetIndex">이동할 대상 표시 인덱스입니다.</param>
    /// <returns>이동 인덱스가 유효하고 수동 배치 순서 반영에 성공하면 true입니다.</returns>
    public bool TryMoveSlot(InventoryCategory category, int sourceIndex, int targetIndex)
    {
        IReadOnlyList<InventorySlotData> slots = GetSlots(category);

        if (sourceIndex < 0 || sourceIndex >= slots.Count)
        {
            return false;
        }

        if (targetIndex < 0)
        {
            return false;
        }

        int safeTargetIndex = targetIndex >= slots.Count ? slots.Count - 1 : targetIndex;

        if (sourceIndex == safeTargetIndex)
        {
            return false;
        }

        _movingSlots.Clear();

        for (int i = 0; i < slots.Count; i++)
        {
            _movingSlots.Add(slots[i]);
        }

        InventorySlotData movedSlot = _movingSlots[sourceIndex];

        if (movedSlot == null)
        {
            _movingSlots.Clear();
            return false;
        }

        _movingSlots.RemoveAt(sourceIndex);
        _movingSlots.Insert(safeTargetIndex, movedSlot);

        for (int i = 0; i < _movingSlots.Count; i++)
        {
            InventorySlotData slotData = _movingSlots[i];

            if (slotData != null)
            {
                slotData.SetManualSlotOrder(i);
            }
        }

        _movingSlots.Clear();
        PersistState();
        return true;
    }

    /// <summary>
    /// 플레이어가 new 표시가 있는 아이템 칸을 확인했을 때 new 표시를 해제합니다.
    /// </summary>
    /// <param name="itemId">확인 처리할 아이템 고유 ID입니다.</param>
    /// <returns>아이템 ID가 유효하고 확인 처리할 슬롯을 찾으면 true입니다.</returns>
    public bool TryMarkItemChecked(string itemId)
    {
        if (!TryNormalizeItemId(itemId, out string normalizedItemId))
        {
            return false;
        }

        bool didFindSlot = false;

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlotData slotData = _slots[i];

            if (IsMatchingSlot(slotData, normalizedItemId) == false)
            {
                continue;
            }

            didFindSlot = true;
            slotData.MarkAsChecked();
        }

        if (didFindSlot)
        {
            PersistState();
        }

        return didFindSlot;
    }

    /// <summary>
    /// 인벤토리 런타임 상태를 초기화합니다. (New Game)
    /// </summary>
    public void Reset()
    {
        _resetItemIds.Clear();

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlotData slotData = _slots[i];

            if (slotData != null && slotData.TryGetItemId(out string itemId))
            {
                _resetItemIds.Add(itemId);
            }
        }

        _slots.Clear();
        _visibleSlots.Clear();
        _slotIndexBySlot.Clear();
        _movingSlots.Clear();

        foreach (string itemId in _resetItemIds)
        {
            ItemCountChanged?.Invoke(itemId, 0);
        }

        _resetItemIds.Clear();
        PersistState();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 기존 슬롯에 같은 아이템을 최대 중첩 수까지 채웁니다.
    /// </summary>
    /// <param name="itemId">추가할 아이템 고유 ID입니다.</param>
    /// <param name="maxStackCount">한 슬롯에 들어갈 수 있는 최대 수량입니다.</param>
    /// <param name="remainingAmount">아직 추가하지 못한 남은 수량입니다.</param>
    private void FillExistingStacks(string itemId, int maxStackCount, ref int remainingAmount)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (remainingAmount == 0)
            {
                return;
            }

            InventorySlotData slotData = _slots[i];

            if (IsMatchingSlot(slotData, itemId) == false)
            {
                continue;
            }

            if (slotData.Quantity >= maxStackCount)
            {
                continue;
            }

            int availableAmount = maxStackCount - slotData.Quantity;
            int addAmount = remainingAmount < availableAmount ? remainingAmount : availableAmount;

            if (slotData.TrySetQuantity(slotData.Quantity + addAmount))
            {
                remainingAmount -= addAmount;
            }
        }
    }

    private bool AddItemAmount(ItemDefinition itemData, int amount)
    {
        int maxStackCount = GetSafeMaxStackCount(itemData);
        bool isFirstAcquisition = GetItemCount(itemData.ItemId) == 0;
        int remainingAmount = amount;

        FillExistingStacks(itemData.ItemId, maxStackCount, ref remainingAmount);
        CreateOverflowSlots(itemData.ItemId, maxStackCount, isFirstAcquisition, ref remainingAmount);

        if (HasAnyManualSlotOrder() == false)
        {
            MoveItemSlotsToRecentEnd(itemData.ItemId);
        }

        return remainingAmount == 0;
    }

    /// <summary>
    /// 최대 중첩 수를 초과한 수량을 새 슬롯으로 나누어 보관합니다.
    /// </summary>
    /// <param name="itemId">추가할 아이템 고유 ID입니다.</param>
    /// <param name="maxStackCount">한 슬롯에 들어갈 수 있는 최대 수량입니다.</param>
    /// <param name="isFirstAcquisition">처음 획득한 아이템인지 여부입니다.</param>
    /// <param name="remainingAmount">아직 추가하지 못한 남은 수량입니다.</param>
    private void CreateOverflowSlots(string itemId, int maxStackCount, bool isFirstAcquisition, ref int remainingAmount)
    {
        while (remainingAmount > 0)
        {
            int addAmount = remainingAmount < maxStackCount ? remainingAmount : maxStackCount;
            InventorySlotData slotData = new InventorySlotData();

            if (!slotData.TrySetSlot(itemId, addAmount, isFirstAcquisition, INVENTORY_UNASSIGNED_MANUAL_ORDER))
            {
                return;
            }

            _slots.Add(slotData);
            remainingAmount -= addAmount;
        }
    }

    /// <summary>
    /// 같은 아이템 슬롯들을 최근 획득 기준 정렬에서 앞으로 보이도록 리스트 끝으로 옮깁니다.
    /// </summary>
    /// <param name="itemId">최근 획득 처리할 아이템 고유 ID입니다.</param>
    private void MoveItemSlotsToRecentEnd(string itemId)
    {
        _movingSlots.Clear();

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlotData slotData = _slots[i];

            if (IsMatchingSlot(slotData, itemId))
            {
                _movingSlots.Add(slotData);
            }
        }

        if (_movingSlots.Count == 0)
        {
            return;
        }

        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            if (IsMatchingSlot(_slots[i], itemId))
            {
                _slots.RemoveAt(i);
            }
        }

        for (int i = 0; i < _movingSlots.Count; i++)
        {
            _slots.Add(_movingSlots[i]);
        }

        _movingSlots.Clear();
    }

    /// <summary>
    /// 아이템 ID를 저장/조회에 사용할 수 있는 형태로 정리합니다.
    /// </summary>
    /// <param name="itemId">정리할 아이템 ID입니다.</param>
    /// <param name="normalizedItemId">정리된 아이템 ID입니다.</param>
    /// <returns>아이템 ID가 비어 있지 않으면 true입니다.</returns>
    private bool TryNormalizeItemId(string itemId, out string normalizedItemId)
    {
        normalizedItemId = string.Empty;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        normalizedItemId = itemId.Trim();
        return true;
    }

    private bool TryGetItemCount(string itemId, out int itemCount)
    {
        itemCount = 0;

        if (!TryNormalizeItemId(itemId, out string normalizedItemId))
        {
            return false;
        }

        long totalCount = 0L;

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlotData slotData = _slots[i];

            if (!IsMatchingSlot(slotData, normalizedItemId))
            {
                continue;
            }

            totalCount += slotData.Quantity;

            if (totalCount > int.MaxValue)
            {
                return false;
            }
        }

        itemCount = (int)totalCount;
        return true;
    }

    private void NotifyItemCountChanged(string itemId)
    {
        if (TryGetItemCount(itemId, out int itemCount))
        {
            ItemCountChanged?.Invoke(itemId, itemCount);
        }
    }

    /// <summary>
    /// 슬롯이 지정한 아이템 ID를 보관 중인지 확인합니다.
    /// </summary>
    /// <param name="slotData">확인할 슬롯 데이터입니다.</param>
    /// <param name="itemId">비교할 아이템 고유 ID입니다.</param>
    /// <returns>슬롯의 아이템 ID가 같으면 true입니다.</returns>
    private bool IsMatchingSlot(InventorySlotData slotData, string itemId)
    {
        if (slotData == null || !slotData.TryGetItemId(out string slotItemId))
        {
            return false;
        }

        return slotItemId == itemId;
    }

    /// <summary>
    /// 슬롯이 지정한 인벤토리 카테고리에 포함되는지 확인합니다.
    /// </summary>
    /// <param name="slotData">확인할 슬롯 데이터입니다.</param>
    /// <param name="category">조회할 인벤토리 카테고리입니다.</param>
    /// <returns>카테고리에 포함되면 true입니다.</returns>
    private bool IsSlotInCategory(InventorySlotData slotData, InventoryCategory category)
    {
        if (slotData == null || !TryGetItem(slotData.ItemId, out ItemDefinition itemData))
        {
            return false;
        }

        return IsItemTypeInCategory(itemData.ItemType, category);
    }

    /// <summary>
    /// 아이템 타입이 지정한 인벤토리 카테고리 필터에 포함되는지 확인합니다.
    /// 문서 기준 전체: 1~6, 퀘스트: 1, 술: 2~3, 유물: 4~5, 기타: 6입니다.
    /// </summary>
    /// <param name="itemType">확인할 아이템 타입입니다.</param>
    /// <param name="category">조회할 인벤토리 카테고리입니다.</param>
    /// <returns>아이템 타입이 카테고리에 포함되면 true입니다.</returns>
    private bool IsItemTypeInCategory(ItemType itemType, InventoryCategory category)
    {
        switch (category)
        {
            case InventoryCategory.INVENTORY_CATEGORY_ALL:
                return IsKnownItemType(itemType);

            case InventoryCategory.INVENTORY_CATEGORY_QUEST:
                return itemType == ItemType.ITEM_QUEST;

            case InventoryCategory.INVENTORY_CATEGORY_ALCOHOL:
                return itemType == ItemType.ITEM_ALCOHOL_COLLECTIBLE || itemType == ItemType.ITEM_ALCOHOL_TOOL;

            case InventoryCategory.INVENTORY_CATEGORY_RELIC:
                return itemType == ItemType.ITEM_RELIC_COLLECTIBLE || itemType == ItemType.ITEM_RELIC_TOOL;

            case InventoryCategory.INVENTORY_CATEGORY_MISC:
                return itemType == ItemType.ITEM_MISC;
        }

        return false;
    }

    /// <summary>
    /// 인벤토리 문서에 정의된 6종 아이템 타입인지 확인합니다.
    /// </summary>
    /// <param name="itemType">확인할 아이템 타입입니다.</param>
    /// <returns>문서에 정의된 아이템 타입이면 true입니다.</returns>
    private bool IsKnownItemType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.ITEM_QUEST:
            case ItemType.ITEM_ALCOHOL_COLLECTIBLE:
            case ItemType.ITEM_ALCOHOL_TOOL:
            case ItemType.ITEM_RELIC_COLLECTIBLE:
            case ItemType.ITEM_RELIC_TOOL:
            case ItemType.ITEM_MISC:
                return true;
        }

        return false;
    }

    /// <summary>
    /// 슬롯 정렬 비교에 사용할 현재 슬롯 인덱스 캐시를 다시 구성합니다.
    /// </summary>
    private void RebuildSlotIndexCache()
    {
        _slotIndexBySlot.Clear();

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlotData slotData = _slots[i];

            if (slotData == null || _slotIndexBySlot.ContainsKey(slotData))
            {
                continue;
            }

            _slotIndexBySlot.Add(slotData, i);
        }
    }

    /// <summary>
    /// 인벤토리 문서의 우선순위에 따라 슬롯 표시 순서를 비교합니다.
    /// </summary>
    /// <param name="leftSlot">왼쪽 비교 슬롯입니다.</param>
    /// <param name="rightSlot">오른쪽 비교 슬롯입니다.</param>
    /// <returns>왼쪽 슬롯이 먼저 오면 음수, 오른쪽 슬롯이 먼저 오면 양수입니다.</returns>
    private int CompareInventorySlots(InventorySlotData leftSlot, InventorySlotData rightSlot)
    {
        if (leftSlot == rightSlot)
        {
            return 0;
        }

        int manualCompare = CompareManualSlotOrder(leftSlot, rightSlot);

        if (manualCompare != 0)
        {
            return manualCompare;
        }

        int priorityCompare = GetSlotSortPriority(leftSlot).CompareTo(GetSlotSortPriority(rightSlot));

        if (priorityCompare != 0)
        {
            return priorityCompare;
        }

        int recentCompare = GetSlotIndex(rightSlot).CompareTo(GetSlotIndex(leftSlot));

        if (recentCompare != 0)
        {
            return recentCompare;
        }

        return string.CompareOrdinal(leftSlot != null ? leftSlot.ItemId : string.Empty, rightSlot != null ? rightSlot.ItemId : string.Empty);
    }

    /// <summary>
    /// 플레이어 수동 배치 순서를 우선하여 슬롯을 비교합니다.
    /// </summary>
    /// <param name="leftSlot">왼쪽 비교 슬롯입니다.</param>
    /// <param name="rightSlot">오른쪽 비교 슬롯입니다.</param>
    /// <returns>수동 배치 순서 비교 결과입니다.</returns>
    private int CompareManualSlotOrder(InventorySlotData leftSlot, InventorySlotData rightSlot)
    {
        bool hasLeftManualOrder = leftSlot != null && leftSlot.HasManualSlotOrder;
        bool hasRightManualOrder = rightSlot != null && rightSlot.HasManualSlotOrder;

        if (hasLeftManualOrder && hasRightManualOrder)
        {
            return leftSlot.ManualSlotOrder.CompareTo(rightSlot.ManualSlotOrder);
        }

        if (hasLeftManualOrder)
        {
            return -1;
        }

        if (hasRightManualOrder)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// 슬롯에 들어 있는 아이템 타입 기준 정렬 우선순위를 조회합니다.
    /// </summary>
    /// <param name="slotData">우선순위를 조회할 슬롯 데이터입니다.</param>
    /// <returns>아이템 타입 정렬 우선순위입니다.</returns>
    private int GetSlotSortPriority(InventorySlotData slotData)
    {
        if (slotData == null || !TryGetItem(slotData.ItemId, out ItemDefinition itemData))
        {
            return (int)ItemTypeSortPriority.ITEM_TYPE_SORT_PRIORITY_DEFAULT;
        }

        switch (itemData.ItemType)
        {
            case ItemType.ITEM_ALCOHOL_TOOL:
                return (int)ItemTypeSortPriority.ITEM_TYPE_SORT_PRIORITY_ALCOHOL_TOOL;

            case ItemType.ITEM_RELIC_TOOL:
                return (int)ItemTypeSortPriority.ITEM_TYPE_SORT_PRIORITY_RELIC_TOOL;
        }

        return (int)ItemTypeSortPriority.ITEM_TYPE_SORT_PRIORITY_DEFAULT;
    }

    /// <summary>
    /// 슬롯의 현재 리스트 인덱스를 조회합니다.
    /// </summary>
    /// <param name="slotData">인덱스를 조회할 슬롯 데이터입니다.</param>
    /// <returns>슬롯 인덱스입니다. 찾지 못하면 0입니다.</returns>
    private int GetSlotIndex(InventorySlotData slotData)
    {
        if (slotData == null)
        {
            return 0;
        }

        if (_slotIndexBySlot.TryGetValue(slotData, out int slotIndex))
        {
            return slotIndex;
        }

        return 0;
    }

    /// <summary>
    /// 인벤토리에 수동 배치 순서를 가진 슬롯이 있는지 확인합니다.
    /// </summary>
    /// <returns>수동 배치 순서가 하나라도 있으면 true입니다.</returns>
    private bool HasAnyManualSlotOrder()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlotData slotData = _slots[i];

            if (slotData != null && slotData.HasManualSlotOrder)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 아이템 데이터에 지정된 최대 중첩 수를 안전하게 조회합니다.
    /// </summary>
    /// <param name="itemData">조회할 아이템 원본 데이터입니다.</param>
    /// <returns>1 이상으로 보정된 최대 중첩 수입니다.</returns>
    private int GetSafeMaxStackCount(ItemDefinition itemData)
    {
        if (itemData == null || itemData.MaxStackCount < INVENTORY_MIN_ITEM_AMOUNT)
        {
            return INVENTORY_MIN_ITEM_AMOUNT;
        }

        return itemData.MaxStackCount;
    }

    private void PersistState()
    {
        if (_progressService == null)
        {
            return;
        }

        _saveData.Slots.Clear();

        for (int i = 0; i < _slots.Count; i++)
        {
            _saveData.Slots.Add(_slots[i]);
        }

        _progressService.SetSystemState(INVENTORY_STATE_ID, JsonUtility.ToJson(_saveData));
    }

    private void RestoreState()
    {
        _slots.Clear();
        _visibleSlots.Clear();
        _slotIndexBySlot.Clear();
        _movingSlots.Clear();

        if (_progressService == null
            || !_progressService.TryGetSystemState(INVENTORY_STATE_ID, out string stateJson))
        {
            return;
        }

        InventorySaveData loadedData;

        try
        {
            loadedData = JsonUtility.FromJson<InventorySaveData>(stateJson);
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"[InventoryController] 저장 상태를 복원하지 못했습니다. {exception.Message}");
            return;
        }

        if (loadedData == null || loadedData.Slots == null)
        {
            return;
        }

        _restoredSlots.Clear();
        _restoredCounts.Clear();

        for (int i = 0; i < loadedData.Slots.Count; i++)
        {
            TryAddRestoredSlot(loadedData.Slots[i]);
        }

        for (int i = 0; i < _restoredSlots.Count; i++)
        {
            _slots.Add(_restoredSlots[i]);
        }
    }

    private bool TryAddRestoredSlot(InventorySlotData slot)
    {
        if (slot == null || slot.IsEmpty
            || !TryGetItem(slot.ItemId, out ItemDefinition item)
            || slot.Quantity > GetSafeMaxStackCount(item))
        {
            return false;
        }

        _restoredCounts.TryGetValue(slot.ItemId, out int currentCount);

        if (currentCount > int.MaxValue - slot.Quantity)
        {
            return false;
        }

        _restoredCounts[slot.ItemId] = currentCount + slot.Quantity;
        _restoredSlots.Add(slot);
        return true;
    }
}
