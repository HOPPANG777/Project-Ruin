//=============================================================================
// File:        BrewingController.cs
//
// Description: 술 제조 슬롯 등록 규칙, 레시피 판정, 재료 차감과 결과 회수를 처리한다.
//
// History:     10/22/2026 + Created -- Claude
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using UnityEngine;

//=============================================================================
//
// Synopsis:   BrewingController;
//                - 등록 시에는 인벤토리 수량을 바꾸지 않고, 제조 성공 시에만 재료 차감
//                - 결과 슬롯에 술이 있으면 등록·교체·추가 제조를 막고 제거만 허용
//                - 도구는 소모하지 않음
//=============================================================================
[DisallowMultipleComponent]
public sealed class BrewingController : MonoBehaviour, IBrewingService
{
    //========================================
    // 내부 상태
    //========================================

    // 재료 슬롯입니다. 비어 있으면 null입니다.
    private readonly string[] _ingredientIds = new string[BrewingRecipe.MAX_INGREDIENT_COUNT];

    // 레시피 판정과 차감에 쓰는 등록 재료 버퍼입니다. 제조마다 리스트를 새로 만들지 않기 위해 재사용합니다.
    private readonly List<string> _brewIngredients =
        new List<string>(BrewingRecipe.MAX_INGREDIENT_COUNT);

    // 도구 슬롯과 결과 슬롯입니다. 비어 있으면 null입니다.
    private string _toolId;
    private string _resultItemId;

    private IInventoryService _inventory;
    private IBrewingRecipeBook _recipeBook;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>슬롯 또는 결과 상태가 바뀐 뒤 발행됩니다.</summary>
    public event Action StateChanged;

    /// <summary>재료 슬롯 개수입니다.</summary>
    public int IngredientSlotCount => _ingredientIds.Length;

    /// <summary>도구 슬롯에 등록된 아이템 ID입니다.</summary>
    public string ToolItemId => _toolId;

    /// <summary>결과 슬롯에 남아 있는 술 아이템 ID입니다.</summary>
    public string ResultItemId => _resultItemId;

    /// <summary>선택 패널에 표시할 재료 아이템 ID 목록입니다.</summary>
    public IReadOnlyList<string> IngredientItemIds => _recipeBook?.IngredientItemIds;

    /// <summary>선택 패널에 표시할 도구 아이템 ID 목록입니다.</summary>
    public IReadOnlyList<string> ToolItemIds => _recipeBook?.ToolItemIds;

    // 의존성이 주입되기 전에는 모든 공개 동작을 거부합니다.
    private bool IsReady => _inventory != null && _recipeBook != null;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // BrewingController.Init
    //=============================================================================
    // Description: 인벤토리와 레시피 저장소를 주입받고 슬롯을 비운다.
    //
    // Parameters:  ( IInventoryService inventory, IBrewingRecipeBook recipeBook )
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>인벤토리와 레시피 저장소를 주입받고 슬롯을 비웁니다.</summary>
    public void Init(IInventoryService inventory, IBrewingRecipeBook recipeBook)
    {
        _inventory = inventory;
        _recipeBook = recipeBook;
        _resultItemId = null;
        ResetSlots();
    }

    /// <summary>재료 슬롯에 등록된 아이템 ID를 조회합니다. 비어 있거나 범위 밖이면 null입니다.</summary>
    public string GetIngredientItemId(int slotIndex)
    {
        return IsValidSlot(slotIndex) ? _ingredientIds[slotIndex] : null;
    }

    //=============================================================================
    // BrewingController.TrySetIngredient
    //=============================================================================
    // Description: 재료를 슬롯에 등록하거나 교체한다. 인벤토리 수량은 바꾸지 않는다.
    //
    // Parameters:  ( int slotIndex, string itemId )
    //
    // Return:      bool — 등록에 성공하면 true
    //
    //=============================================================================
    /// <summary>재료를 슬롯에 등록하거나 기존 재료와 교체합니다.</summary>
    public bool TrySetIngredient(int slotIndex, string itemId)
    {
        // 결과 슬롯에 술이 있거나, 재료 슬롯·재료 아이템이 아니면 거부한다.
        if (!IsReady || _resultItemId != null || !IsValidSlot(slotIndex)
            || !_recipeBook.IsIngredient(itemId))
        {
            return false;
        }

        // 교체 대상 슬롯은 등록 수에서 빼고 계산해야 같은 재료로 교체할 때 오탐하지 않는다.
        if (CountRegistered(itemId, slotIndex) + 1 > _inventory.GetItemCount(itemId))
        {
            return false;
        }

        _ingredientIds[slotIndex] = itemId;
        StateChanged?.Invoke();
        return true;
    }

    //=============================================================================
    // BrewingController.TrySetTool
    //=============================================================================
    // Description: 도구를 슬롯에 등록하거나 교체한다. 인벤토리 수량은 바꾸지 않는다.
    //
    // Parameters:  ( string itemId )
    //
    // Return:      bool — 등록에 성공하면 true
    //
    //=============================================================================
    /// <summary>도구를 슬롯에 등록하거나 기존 도구와 교체합니다.</summary>
    public bool TrySetTool(string itemId)
    {
        if (!IsReady || _resultItemId != null || !_recipeBook.IsTool(itemId)
            || _inventory.GetItemCount(itemId) < 1)
        {
            return false;
        }

        _toolId = itemId;
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>재료 슬롯 등록을 해제합니다.</summary>
    public void ClearIngredient(int slotIndex)
    {
        if (!IsValidSlot(slotIndex) || _ingredientIds[slotIndex] == null)
        {
            return;
        }

        _ingredientIds[slotIndex] = null;
        StateChanged?.Invoke();
    }

    /// <summary>도구 슬롯 등록을 해제합니다.</summary>
    public void ClearTool()
    {
        if (_toolId == null)
        {
            return;
        }

        _toolId = null;
        StateChanged?.Invoke();
    }

    //=============================================================================
    // BrewingController.TryBrew
    //=============================================================================
    // Description: 조건을 재검증하고 레시피가 일치하면 재료를 차감한 뒤 결과 술을 생성한다.
    //              실패하면 슬롯·인벤토리·결과 슬롯을 모두 그대로 둔다.
    //
    // Parameters:  None.
    //
    // Return:      bool — 제조에 성공하면 true
    //
    //=============================================================================
    /// <summary>조건을 검사하고 재료를 차감한 뒤 결과 슬롯에 술을 생성합니다.</summary>
    public bool TryBrew()
    {
        if (!IsReady || _resultItemId != null || _toolId == null)
        {
            return false;
        }

        CollectRegisteredIngredients();

        // 등록 이후 외부에서 수량이 바뀔 수 있으므로 차감 직전에 다시 검증한다.
        if (_brewIngredients.Count < BrewingRecipe.MIN_INGREDIENT_COUNT || !HasEnoughItems()
            || !_recipeBook.TryFindRecipe(_brewIngredients, _toolId, out BrewingRecipe recipe)
            || !TryConsumeIngredients())
        {
            return false;
        }

        // 도구는 유지하고 재료 슬롯만 비운다.
        Array.Clear(_ingredientIds, 0, _ingredientIds.Length);
        _resultItemId = recipe.ResultItemId;
        StateChanged?.Invoke();
        return true;
    }

    //=============================================================================
    // BrewingController.TryCollectResult
    //=============================================================================
    // Description: 결과 슬롯의 술을 인벤토리에 추가하고 결과 슬롯을 비운다.
    //
    // Parameters:  None.
    //
    // Return:      bool — 회수에 성공하면 true
    //
    //=============================================================================
    /// <summary>결과 슬롯의 술을 인벤토리로 회수합니다.</summary>
    public bool TryCollectResult()
    {
        if (!IsReady || _resultItemId == null
            || !_inventory.TryAddItem(_resultItemId, BrewingRecipe.RESULT_COUNT))
        {
            return false;
        }

        _resultItemId = null;
        StateChanged?.Invoke();
        return true;
    }

    //=============================================================================
    // BrewingController.CloseSession
    //=============================================================================
    // Description: 슬롯 등록만 해제하고(수량 가산 없음) 남은 술은 인벤토리로 회수한다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>모든 슬롯 등록을 해제하고 결과 슬롯에 남은 술을 회수합니다.</summary>
    public void CloseSession()
    {
        ResetSlots();

        // 회수가 상태 변경을 알리므로, 회수할 술이 없을 때만 직접 알린다.
        if (!TryCollectResult())
        {
            StateChanged?.Invoke();
        }
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _ingredientIds.Length;
    }

    private void ResetSlots()
    {
        Array.Clear(_ingredientIds, 0, _ingredientIds.Length);
        _toolId = null;
    }

    // 같은 재료가 exceptSlot 이외의 슬롯에 몇 개 등록됐는지 센다. (exceptSlot = -1 이면 전체)
    private int CountRegistered(string itemId, int exceptSlot)
    {
        int count = 0;

        for (int i = 0; i < _ingredientIds.Length; i++)
        {
            if (i != exceptSlot && _ingredientIds[i] == itemId)
            {
                count++;
            }
        }

        return count;
    }

    private void CollectRegisteredIngredients()
    {
        _brewIngredients.Clear();

        for (int i = 0; i < _ingredientIds.Length; i++)
        {
            if (_ingredientIds[i] != null)
            {
                _brewIngredients.Add(_ingredientIds[i]);
            }
        }
    }

    // 도구 보유 여부와 재료별 등록 수 ≤ 보유 수량을 확인한다.
    private bool HasEnoughItems()
    {
        if (_inventory.GetItemCount(_toolId) < 1)
        {
            return false;
        }

        for (int i = 0; i < _brewIngredients.Count; i++)
        {
            string itemId = _brewIngredients[i];

            if (CountRegistered(itemId, -1) > _inventory.GetItemCount(itemId))
            {
                return false;
            }
        }

        return true;
    }

    // 재료를 1개씩 차감하다 실패하면 이미 차감한 재료를 되돌려 부분 완료 상태를 막는다.
    private bool TryConsumeIngredients()
    {
        for (int i = 0; i < _brewIngredients.Count; i++)
        {
            if (_inventory.TryConsumeItem(_brewIngredients[i], 1))
            {
                continue;
            }

            RollbackConsumption(i);
            return false;
        }

        return true;
    }

    private void RollbackConsumption(int consumedCount)
    {
        for (int i = 0; i < consumedCount; i++)
        {
            if (!_inventory.TryAddItem(_brewIngredients[i], 1))
            {
                Debug.LogError($"[BrewingController] 재료 복구에 실패했습니다. itemId={_brewIngredients[i]}");
            }
        }
    }
}
