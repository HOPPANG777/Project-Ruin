//=============================================================================
// File:        IBrewingService.cs
//
// Description: 술 제조 슬롯 상태와 제조 실행에 대한 외부 공개 계약.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;

//=============================================================================
//
// Synopsis:   IBrewingService;
//                - 재료 3칸 / 도구 1칸 / 결과 1칸의 등록·제거·제조·회수
//                - 외부는 GameRoot.Instance.Brewing 으로만 접근
//=============================================================================
public interface IBrewingService
{
    /// <summary>슬롯 또는 결과 상태가 바뀐 뒤 발행됩니다.</summary>
    event Action StateChanged;

    /// <summary>재료 슬롯 개수입니다.</summary>
    int IngredientSlotCount { get; }

    /// <summary>도구 슬롯에 등록된 아이템 ID입니다. 비어 있으면 null입니다.</summary>
    string ToolItemId { get; }

    /// <summary>결과 슬롯에 남아 있는 술 아이템 ID입니다. 비어 있으면 null입니다.</summary>
    string ResultItemId { get; }

    /// <summary>선택 패널에 표시할 재료 아이템 ID 목록입니다.</summary>
    IReadOnlyList<string> IngredientItemIds { get; }

    /// <summary>선택 패널에 표시할 도구 아이템 ID 목록입니다.</summary>
    IReadOnlyList<string> ToolItemIds { get; }

    /// <summary>재료 슬롯에 등록된 아이템 ID를 조회합니다. 비어 있거나 범위 밖이면 null입니다.</summary>
    /// <param name="slotIndex">조회할 재료 슬롯 인덱스입니다.</param>
    string GetIngredientItemId(int slotIndex);

    /// <summary>재료를 슬롯에 등록하거나 기존 재료와 교체합니다. 인벤토리 수량은 바뀌지 않습니다.</summary>
    /// <param name="slotIndex">등록할 재료 슬롯 인덱스입니다.</param>
    /// <param name="itemId">등록할 재료 아이템 ID입니다.</param>
    /// <returns>규칙을 모두 만족해 등록하면 true입니다.</returns>
    bool TrySetIngredient(int slotIndex, string itemId);

    /// <summary>도구를 슬롯에 등록하거나 기존 도구와 교체합니다. 인벤토리 수량은 바뀌지 않습니다.</summary>
    /// <param name="itemId">등록할 도구 아이템 ID입니다.</param>
    /// <returns>규칙을 모두 만족해 등록하면 true입니다.</returns>
    bool TrySetTool(string itemId);

    /// <summary>재료 슬롯 등록을 해제합니다. 결과 슬롯 상태와 무관하게 허용됩니다.</summary>
    /// <param name="slotIndex">해제할 재료 슬롯 인덱스입니다.</param>
    void ClearIngredient(int slotIndex);

    /// <summary>도구 슬롯 등록을 해제합니다. 결과 슬롯 상태와 무관하게 허용됩니다.</summary>
    void ClearTool();

    /// <summary>조건을 검사하고 재료를 차감한 뒤 결과 슬롯에 술을 생성합니다.</summary>
    /// <returns>제조에 성공하면 true입니다.</returns>
    bool TryBrew();

    /// <summary>결과 슬롯의 술을 인벤토리로 회수합니다.</summary>
    /// <returns>회수할 술이 있고 인벤토리에 추가했으면 true입니다.</returns>
    bool TryCollectResult();

    /// <summary>모든 슬롯 등록을 해제하고 결과 슬롯에 남은 술을 인벤토리로 회수합니다.</summary>
    void CloseSession();
}
