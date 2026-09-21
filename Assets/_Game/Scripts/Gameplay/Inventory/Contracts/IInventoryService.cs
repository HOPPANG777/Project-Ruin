using System;
using System.Collections.Generic;

/// <summary>아이템 변경과 연동 도메인 상태 적용의 실패 원인입니다.</summary>
public enum InventoryStateChangeFailure
{
    INVENTORY_STATE_CHANGE_NONE = 0,
    INVENTORY_STATE_CHANGE_INVALID,
    INVENTORY_STATE_CHANGE_APPLY_FAILED
}

/// <summary>아이템과 연동 상태를 함께 확정한 뒤에만 이벤트를 발행하는 선택적 계약입니다.</summary>
public interface IInventoryStateTransactionService
{
    /// <summary>수량 0인 쪽은 생략합니다. 실패 시 수량·슬롯·저장 상태를 보존합니다.</summary>
    bool TryApplyWithSystemState(string removeId, int removeAmount, string addId, int addAmount,
        string stateId, string stateJson, out InventoryStateChangeFailure failure);
}

//==============================================================================
// 기능: interface::IInventoryService
// 설명: 인벤토리 아이템 보관, 조회, 소모, 카테고리 슬롯 조회에 대한 외부 공개 계약
//==============================================================================
// NOTE:
//  - 외부는 GameRoot.Instance.Inventory를 통해서만 접근합니다.
//  - 런타임 슬롯 저장 데이터는 ItemDefinition 참조가 아닌 ItemId 중심으로 유지합니다.
//  - 플레이어 수동 드랍/삭제는 제공하지 않고, 퀘스트 완료 또는 재료 사용 시 자동 소모만 허용합니다.
//==============================================================================

/// <summary>
/// 인벤토리 시스템의 외부 공개 계약입니다.
/// </summary>
public interface IInventoryService
{
    /// <summary>아이템 수량 변경 후 ItemId와 최종 보유 수량을 전달합니다.</summary>
    event Action<string, int> ItemCountChanged;

    /// <summary>
    /// 아이템 원본 데이터를 기준으로 인벤토리에 아이템을 추가합니다.
    /// </summary>
    /// <param name="itemId">추가할 아이템 고유 ID입니다.</param>
    /// <param name="amount">추가할 아이템 개수입니다.</param>
    /// <returns>아이템 데이터와 개수가 유효하고 추가에 성공하면 true입니다.</returns>
    bool TryAddItem(string itemId, int amount);

    /// <summary>
    /// 아이템을 현재 수량과 int 범위를 넘지 않고 추가할 수 있는지 확인합니다.
    /// </summary>
    /// <param name="itemId">추가 가능 여부를 확인할 아이템 고유 ID입니다.</param>
    /// <param name="amount">추가할 아이템 개수입니다.</param>
    /// <returns>아이템이 존재하고 추가 후 총수량이 int 범위 안이면 true입니다.</returns>
    bool CanAddItem(string itemId, int amount);

    /// <summary>
    /// 퀘스트 완료 또는 재료 사용 조건이 만족되었을 때 인벤토리 아이템을 자동 소모합니다.
    /// </summary>
    /// <param name="itemId">소모할 아이템 고유 ID입니다.</param>
    /// <param name="amount">소모할 아이템 개수입니다.</param>
    /// <returns>아이템 ID, 개수, 보유 수량이 유효하고 소모에 성공하면 true입니다.</returns>
    bool TryConsumeItem(string itemId, int amount);

    /// <summary>
    /// 한 아이템을 제거하고 다른 아이템을 추가하는 교환을 원자적으로 처리합니다.
    /// </summary>
    /// <param name="removeItemId">제거할 아이템 고유 ID입니다.</param>
    /// <param name="removeAmount">제거할 아이템 개수입니다.</param>
    /// <param name="addItemId">추가할 아이템 고유 ID입니다.</param>
    /// <param name="addAmount">추가할 아이템 개수입니다.</param>
    /// <returns>두 변경을 모두 적용했으면 true입니다.</returns>
    bool TryExchangeItem(
        string removeItemId,
        int removeAmount,
        string addItemId,
        int addAmount);

    /// <summary>
    /// 아이템 ID로 원본 아이템 데이터를 조회합니다.
    /// </summary>
    /// <param name="itemId">조회할 아이템 고유 ID입니다.</param>
    /// <param name="itemData">조회된 아이템 원본 데이터입니다.</param>
    /// <returns>아이템 데이터를 찾으면 true입니다.</returns>
    bool TryGetItem(string itemId, out ItemDefinition itemData);

    /// <summary>아이템 ID로 현재 언어 문장과 아이콘이 결합된 UI 표시 데이터를 조회합니다.</summary>
    bool TryGetItemDisplay(string itemId, out InventoryItemDisplayData displayData);

    /// <summary>
    /// 아이템 ID에 해당하는 현재 보유 수량을 조회합니다.
    /// </summary>
    /// <param name="itemId">수량을 조회할 아이템 고유 ID입니다.</param>
    /// <returns>보유 중인 전체 수량입니다. 아이템 ID가 유효하지 않거나 보유하지 않으면 0입니다.</returns>
    int GetItemCount(string itemId);

    /// <summary>
    /// 카테고리 규칙과 현재 정렬 상태에 맞는 인벤토리 슬롯 목록을 조회합니다.
    /// </summary>
    /// <param name="category">조회할 인벤토리 카테고리입니다.</param>
    /// <returns>카테고리에 포함되는 슬롯 목록입니다.</returns>
    IReadOnlyList<InventorySlotData> GetSlots(InventoryCategory category);

    /// <summary>
    /// 현재 보유 슬롯을 최근 획득한 아이템부터 조회합니다.
    /// </summary>
    /// <returns>재사용되는 읽기 전용 슬롯 목록입니다.</returns>
    IReadOnlyList<InventorySlotData> GetSlotsByAcquisitionOrder();

    /// <summary>
    /// 드래그 이동으로 지정된 슬롯의 수동 배치 순서를 변경합니다.
    /// </summary>
    /// <param name="category">현재 표시 중인 인벤토리 카테고리입니다.</param>
    /// <param name="sourceIndex">이동할 슬롯의 현재 표시 인덱스입니다.</param>
    /// <param name="targetIndex">이동할 대상 표시 인덱스입니다.</param>
    /// <returns>이동 인덱스가 유효하고 수동 배치 순서 반영에 성공하면 true입니다.</returns>
    bool TryMoveSlot(InventoryCategory category, int sourceIndex, int targetIndex);

    /// <summary>
    /// 플레이어가 new 표시가 있는 아이템 칸을 확인했을 때 new 표시를 해제합니다.
    /// </summary>
    /// <param name="itemId">확인 처리할 아이템 고유 ID입니다.</param>
    /// <returns>아이템 ID가 유효하고 확인 처리할 슬롯을 찾으면 true입니다.</returns>
    bool TryMarkItemChecked(string itemId);

    /// <summary>
    /// 인벤토리 런타임 상태를 초기화합니다. (New Game)
    /// </summary>
    void Reset();
}
