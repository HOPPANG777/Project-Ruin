//=============================================================================
// File:        IMuseumRelicDisplayService.cs
// Description: 유물 전시대 배치·회수·마일스톤의 외부 공개 계약
// History:     08/30/2026 + Created -- Codex
//=============================================================================
//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;

/// <summary>입력·인벤토리·상태 적용·보상 조건의 실패 원인을 구분합니다.</summary>
public enum MuseumRelicDisplayFailure
{
    MUSEUM_RELIC_FAILURE_NONE = 0,
    MUSEUM_RELIC_FAILURE_NOT_INITIALIZED,
    MUSEUM_RELIC_FAILURE_BUSY,
    MUSEUM_RELIC_FAILURE_INVALID_PEDESTAL,
    MUSEUM_RELIC_FAILURE_INVALID_ITEM,
    MUSEUM_RELIC_FAILURE_ITEM_NOT_RELIC,
    MUSEUM_RELIC_FAILURE_ITEM_NOT_OWNED,
    MUSEUM_RELIC_FAILURE_ALREADY_DISPLAYED,
    MUSEUM_RELIC_FAILURE_PEDESTAL_EMPTY,
    MUSEUM_RELIC_FAILURE_INVENTORY_OVERFLOW,
    MUSEUM_RELIC_FAILURE_INVENTORY_CHANGE_FAILED,
    MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED,
    MUSEUM_RELIC_FAILURE_UNKNOWN_MILESTONE,
    MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ELIGIBLE,
    MUSEUM_RELIC_FAILURE_MILESTONE_ALREADY_CLAIMED,
    MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ROOM_PICKUP,
    MUSEUM_RELIC_FAILURE_ROOM_LOCKED,
    MUSEUM_RELIC_FAILURE_PLAYER_OUTSIDE_ROOM,
    MUSEUM_RELIC_FAILURE_INVALID_ROOM_SETUP,
    MUSEUM_RELIC_FAILURE_INTERACTION_BLOCKED
}

/// <summary>현재 달성 여부와 영구 저장되는 한 번성 수령 상태입니다.</summary>
public readonly struct MuseumRelicMilestoneStatus
{
    //=============================================================================
    // MuseumRelicMilestoneStatus
    // Description: 마일스톤 달성 및 수령 상태를 생성합니다.
    // Parameters: bool eligible, bool claimed
    // Return: N/A.
    //=============================================================================
    /// <summary>마일스톤 달성 및 수령 상태를 생성합니다.</summary>
    public MuseumRelicMilestoneStatus(bool eligible, bool claimed)
    {
        IsEligible = eligible;
        IsClaimed = claimed;
    }

    /// <summary>현재 전시 수가 마일스톤 조건을 충족했는지 여부입니다.</summary>
    public bool IsEligible { get; }
    /// <summary>마일스톤 보상이 이미 지급됐는지 여부입니다.</summary>
    public bool IsClaimed { get; }
}

/// <summary>유물 전시 상태와 박물관 마일스톤을 제공하는 계약입니다.</summary>
public interface IMuseumRelicDisplayService
{
    /// <summary>지정 전시대의 표시 유물이 변경된 후 전시대 ID를 전달합니다.</summary>
    event Action<string> ExhibitChanged;
    /// <summary>마일스톤 보상이 지급된 후 마일스톤 ID를 전달합니다.</summary>
    event Action<string> MilestoneChanged;
    /// <summary>현재 실제 전시된 서로 다른 유물 수입니다.</summary>
    int DisplayedRelicCount { get; }
    /// <summary>도감 데이터에 등록된 수집형 유물 종류 수입니다.</summary>
    int RelicTypeCount { get; }
    /// <summary>씬 전시대의 안정적인 ID를 등록합니다.</summary>
    void RegisterPedestal(string pedestalId);
    /// <summary>전시대에 배치된 유물 Item ID를 조회합니다.</summary>
    bool TryGetDisplayedRelic(string pedestalId, out string relicItemId);
    /// <summary>현재 보유 중이며 다른 전시대에 없는 유물 ID를 버퍼에 채웁니다.</summary>
    int FillPlaceableRelics(List<string> buffer);
    /// <summary>유물을 소비하여 전시대에 배치하거나 기존 유물과 교체합니다.</summary>
    bool TryPlace(string pedestalId, string relicItemId,
        out MuseumRelicDisplayFailure failure);
    /// <summary>전시 유물을 인벤토리로 회수합니다.</summary>
    bool TryRecover(string pedestalId, out MuseumRelicDisplayFailure failure);
    /// <summary>마일스톤 정의와 현재 달성·수령 상태를 조회합니다.</summary>
    bool TryGetMilestone(string milestoneId,
        out MuseumRelicMilestoneDefinition milestone,
        out MuseumRelicMilestoneStatus status);
    /// <summary>방 안에서 수령하는 마일스톤 보상을 한 번만 지급합니다.</summary>
    bool TryClaimRoomReward(string milestoneId,
        out MuseumRelicDisplayFailure failure);
    /// <summary>인벤토리 부족으로 보류된 즉시 지급 보상을 다시 시도합니다.</summary>
    void RetryPendingImmediateRewards();
}
