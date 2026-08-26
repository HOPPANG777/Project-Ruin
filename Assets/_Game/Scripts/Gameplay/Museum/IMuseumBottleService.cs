//=============================================================================
// File:        IMuseumBottleService.cs
// Description: 술병 조회와 한 번만 투입하는 런타임 공개 계약이다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;

public enum MuseumBottleContributionFailure
{
    MUSEUM_BOTTLE_FAILURE_NONE = 0,
    MUSEUM_BOTTLE_FAILURE_NOT_INITIALIZED,
    MUSEUM_BOTTLE_FAILURE_BUSY,
    MUSEUM_BOTTLE_FAILURE_INVALID_ID,
    MUSEUM_BOTTLE_FAILURE_UNKNOWN_BOTTLE,
    MUSEUM_BOTTLE_FAILURE_ITEM_NOT_ALLOWED,
    MUSEUM_BOTTLE_FAILURE_ALREADY_CONTRIBUTED,
    MUSEUM_BOTTLE_FAILURE_ALREADY_COMPLETED,
    MUSEUM_BOTTLE_FAILURE_ITEM_NOT_OWNED,
    MUSEUM_BOTTLE_FAILURE_REWARD_UNAVAILABLE,
    MUSEUM_BOTTLE_FAILURE_INVENTORY_CHANGE_FAILED
}

/// <summary>술병의 현재 채움과 영구 완료 상태입니다.</summary>
public readonly struct MuseumBottleProgress
{
    public MuseumBottleProgress(int contributedCount, int requiredCount,
        int contributedValue, int requiredValue, bool isCompleted, bool isRewardGranted)
    {
        ContributedCount = contributedCount;
        RequiredCount = requiredCount;
        ContributedValue = contributedValue;
        RequiredValue = requiredValue;
        IsCompleted = isCompleted;
        IsRewardGranted = isRewardGranted;
    }

    public int ContributedCount { get; }
    public int RequiredCount { get; }
    public int ContributedValue { get; }
    public int RequiredValue { get; }
    public bool IsCompleted { get; }
    public bool IsRewardGranted { get; }
    public float FillRatio => RequiredValue > 0
        ? Math.Min(1f, (float)ContributedValue / RequiredValue)
        : 0f;
}

/// <summary>챕터별 술병 조회, 선택 후보와 투입 처리를 제공하는 계약입니다.</summary>
public interface IMuseumBottleService
{
    event Action<string> BottleChanged;
    int BottleCount { get; }
    bool TryGetBottle(string bottleId, out MuseumBottleDefinition bottle);
    bool TryGetProgress(string bottleId, out MuseumBottleProgress progress);
    bool IsContributed(string bottleId, string alcoholItemId);
    int FillContributableItems(string bottleId, List<MuseumBottleItemDefinition> buffer);
    bool TryContribute(string bottleId, string alcoholItemId,
        out MuseumBottleContributionFailure failure);
}
