//=============================================================================
// File:        MuseumRelicDisplayStateStore.cs
// Description: 전시대별 유물과 마일스톤 수령 상태의 JSON 저장 변환
// History:     08/30/2026 + Created -- Codex
//=============================================================================
//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>전시 상태 직렬화와 인벤토리 연계 커밋·실패 복원을 담당합니다.</summary>
internal static class MuseumRelicDisplayStateStore
{
    //=============================================================================
    // SaveToDiskAsync
    // Description: ES3 저장 결과를 관찰하며 디스크 실패와 상태 적용 실패를 구분합니다.
    // Parameters: IProgressService progress, UnityEngine.Object logContext, CancellationToken token
    // Return: UniTask
    //=============================================================================
    internal static async UniTask SaveToDiskAsync(IProgressService progress,
        UnityEngine.Object logContext, CancellationToken token)
    {
        try
        {
            if (!await progress.SaveAsync(token))
                Debug.LogWarning(
                    "[MuseumRelicDisplayController] 디스크 저장에 실패했습니다. Dirty 상태로 재저장할 수 있습니다.",
                    logContext);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { Debug.LogException(exception, logContext); }
    }

    //=============================================================================
    // Sanitize
    // Description: 구버전·중복 저장 항목을 초기화 시 한 번 정리합니다.
    // Parameters: Dictionary<string, string> exhibits, HashSet<string> claimed, HashSet<string> displayed, MuseumRelicMilestoneRepository milestones, Func<string, bool> isRelic
    // Return: void
    //=============================================================================
    internal static void Sanitize(Dictionary<string, string> exhibits,
        HashSet<string> claimed, HashSet<string> displayed,
        MuseumRelicMilestoneRepository milestones, Func<string, bool> isRelic)
    {
        displayed.Clear();
        List<string> remove = new List<string>();
        foreach (KeyValuePair<string, string> pair in exhibits)
            if (!isRelic(pair.Value) || !displayed.Add(pair.Value)) remove.Add(pair.Key);
        for (int i = 0; i < remove.Count; i++) exhibits.Remove(remove[i]);
        remove.Clear();
        foreach (string id in claimed) if (!milestones.TryGet(id, out _)) remove.Add(id);
        for (int i = 0; i < remove.Count; i++) claimed.Remove(remove[i]);
    }

    //=============================================================================
    // Load
    // Description: Progress JSON을 전시 및 수령 상태로 복원합니다.
    // Parameters: IProgressService progress, Dictionary<string, string> exhibits, HashSet<string> claimed
    // Return: void
    //=============================================================================
    internal static void Load(IProgressService progress,
        Dictionary<string, string> exhibits, HashSet<string> claimed)
    {
        exhibits.Clear();
        claimed.Clear();
        if (!progress.TryGetSystemState(MuseumRelicDisplayController.STATE_ID, out string json)) return;
        SaveCollection save;
        try { save = JsonUtility.FromJson<SaveCollection>(json); }
        catch (ArgumentException) { return; }
        if (save?.exhibits != null)
            for (int i = 0; i < save.exhibits.Count; i++)
            {
                ExhibitSave value = save.exhibits[i];
                if (value != null && !string.IsNullOrWhiteSpace(value.pedestalId)
                    && !string.IsNullOrWhiteSpace(value.relicItemId)
                    && !exhibits.ContainsKey(value.pedestalId))
                    exhibits.Add(value.pedestalId, value.relicItemId);
            }
        if (save?.claimedMilestoneIds != null)
            for (int i = 0; i < save.claimedMilestoneIds.Count; i++)
                if (!string.IsNullOrWhiteSpace(save.claimedMilestoneIds[i]))
                    claimed.Add(save.claimedMilestoneIds[i]);
    }

    //=============================================================================
    // TryWrite
    // Description: 초기화된 전시 상태를 Progress 메모리에 적용합니다.
    // Parameters: IProgressService progress, IReadOnlyDictionary<string, string> exhibits, IEnumerable<string> claimed
    // Return: bool
    //=============================================================================
    internal static bool TryWrite(IProgressService progress,
        IReadOnlyDictionary<string, string> exhibits, IEnumerable<string> claimed)
    {
        try
        {
            progress.SetSystemState(
                MuseumRelicDisplayController.STATE_ID, Serialize(exhibits, claimed));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    //=============================================================================
    // TryPlace
    // Description: 유물 소비·교체와 전시 상태를 함께 적용하고 실패 시 원래 상태를 유지합니다.
    // Parameters: IInventoryService inventory, Dictionary<string, string> exhibits, HashSet<string> displayed, IEnumerable<string> claimed, string pedestalId, string relicId, string previous, out MuseumRelicDisplayFailure failure
    // Return: bool
    //=============================================================================
    internal static bool TryPlace(IInventoryService inventory,
        Dictionary<string, string> exhibits, HashSet<string> displayed,
        IEnumerable<string> claimed, string pedestalId, string relicId, string previous,
        out MuseumRelicDisplayFailure failure)
    {
        if (!string.IsNullOrEmpty(previous) && !inventory.CanAddItem(previous, 1))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVENTORY_OVERFLOW, out failure);
        if (!string.IsNullOrEmpty(previous)) displayed.Remove(previous);
        exhibits[pedestalId] = relicId;
        displayed.Add(relicId);
        if (TryApply(inventory, exhibits, claimed, relicId, 1, previous,
                string.IsNullOrEmpty(previous) ? 0 : 1, out failure)) return true;
        exhibits.Remove(pedestalId);
        displayed.Remove(relicId);
        if (!string.IsNullOrEmpty(previous))
        {
            exhibits[pedestalId] = previous;
            displayed.Add(previous);
        }
        return false;
    }

    //=============================================================================
    // TryRecover
    // Description: 유물 반환과 전시 제거를 함께 적용합니다.
    // Parameters: IInventoryService inventory, Dictionary<string, string> exhibits, HashSet<string> displayed, IEnumerable<string> claimed, string pedestalId, string relicId, out MuseumRelicDisplayFailure failure
    // Return: bool
    //=============================================================================
    internal static bool TryRecover(IInventoryService inventory,
        Dictionary<string, string> exhibits, HashSet<string> displayed,
        IEnumerable<string> claimed, string pedestalId, string relicId,
        out MuseumRelicDisplayFailure failure)
    {
        if (!inventory.CanAddItem(relicId, 1))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVENTORY_OVERFLOW, out failure);
        exhibits.Remove(pedestalId);
        displayed.Remove(relicId);
        if (TryApply(inventory, exhibits, claimed, null, 0, relicId, 1, out failure)) return true;
        exhibits[pedestalId] = relicId;
        displayed.Add(relicId);
        return false;
    }

    //=============================================================================
    // TryGrant
    // Description: 보상 아이템과 한 번성 수령 상태를 함께 적용합니다.
    // Parameters: IInventoryService inventory, IReadOnlyDictionary<string, string> exhibits, HashSet<string> claimed, MuseumRelicMilestoneDefinition milestone, out MuseumRelicDisplayFailure failure
    // Return: bool
    //=============================================================================
    internal static bool TryGrant(IInventoryService inventory,
        IReadOnlyDictionary<string, string> exhibits, HashSet<string> claimed,
        MuseumRelicMilestoneDefinition milestone, out MuseumRelicDisplayFailure failure)
    {
        if (!inventory.CanAddItem(milestone.RewardItemId, milestone.RewardAmount))
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVENTORY_OVERFLOW,
                out failure);
        claimed.Add(milestone.MilestoneId);
        if (TryApply(inventory, exhibits, claimed, null, 0,
                milestone.RewardItemId, milestone.RewardAmount, out failure)) return true;
        claimed.Remove(milestone.MilestoneId);
        return false;
    }

    //=============================================================================
    // TryApply
    // Description: 인벤토리와 전시 JSON을 단일 트랜잭션으로 확정합니다.
    // Parameters: IInventoryService inventory, IReadOnlyDictionary<string, string> exhibits, IEnumerable<string> claimed, string removeId, int removeAmount, string addId, int addAmount, out MuseumRelicDisplayFailure failure
    // Return: bool
    //=============================================================================
    private static bool TryApply(IInventoryService inventory,
        IReadOnlyDictionary<string, string> exhibits, IEnumerable<string> claimed,
        string removeId, int removeAmount, string addId, int addAmount,
        out MuseumRelicDisplayFailure failure)
    {
        failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED;
        if (!(inventory is IInventoryStateTransactionService transaction)) return false;
        try
        {
            if (!transaction.TryApplyWithSystemState(removeId, removeAmount, addId, addAmount,
                    MuseumRelicDisplayController.STATE_ID, Serialize(exhibits, claimed), out var reason))
                return Fail(reason == InventoryStateChangeFailure.INVENTORY_STATE_CHANGE_APPLY_FAILED
                    ? MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED
                    : MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVENTORY_CHANGE_FAILED, out failure);
            failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NONE;
            return true;
        }
        catch (Exception) { return false; }
    }

    private static string Serialize(IReadOnlyDictionary<string, string> exhibits,
        IEnumerable<string> claimed)
    {
        SaveCollection save = new SaveCollection();
        foreach (KeyValuePair<string, string> pair in exhibits)
            save.exhibits.Add(new ExhibitSave(pair.Key, pair.Value));
        save.exhibits.Sort((left, right) => string.CompareOrdinal(left.pedestalId, right.pedestalId));
        save.claimedMilestoneIds.AddRange(claimed);
        save.claimedMilestoneIds.Sort(StringComparer.Ordinal);
        return JsonUtility.ToJson(save);
    }

    // 커밋 후 알림의 예외가 다음 보상·저장까지 중단하지 않도록 구독자별로 격리합니다.
    //=============================================================================
    // NotifyCommitted
    // Description: 커밋 후 구독자별 예외를 격리하여 후속 저장과 알림을 보장합니다.
    // Parameters: Action<string> notification, string id, UnityEngine.Object context
    // Return: void
    //=============================================================================
    internal static void NotifyCommitted(Action<string> notification, string id, UnityEngine.Object context)
    {
        Delegate[] handlers = notification?.GetInvocationList();
        if (handlers == null) return;
        for (int i = 0; i < handlers.Length; i++)
        {
            try { ((Action<string>)handlers[i])(id); }
            catch (Exception exception) { Debug.LogException(exception, context); }
        }
    }

    private static bool Fail(MuseumRelicDisplayFailure value,
        out MuseumRelicDisplayFailure failure)
    {
        failure = value;
        return false;
    }

    [Serializable] private sealed class SaveCollection
    {
        // 씬 전시대 ID별 전시 유물 목록입니다.
        public List<ExhibitSave> exhibits = new List<ExhibitSave>();
        // 한 번 지급된 보상의 안정적인 ID 목록입니다.
        public List<string> claimedMilestoneIds = new List<string>();
    }

    [Serializable] private sealed class ExhibitSave
    {
        // 씬 변경 후에도 유지해야 하는 전시대 ID입니다.
        public string pedestalId;
        // 전시된 수집형 유물의 Item ID입니다.
        public string relicItemId;
        public ExhibitSave() { }
        internal ExhibitSave(string pedestal, string relic)
        { pedestalId = pedestal; relicItemId = relic; }
    }
}
