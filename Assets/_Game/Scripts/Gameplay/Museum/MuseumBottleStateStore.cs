//=============================================================================
// File:        MuseumBottleStateStore.cs
// Description: 술병 진행 상태 JSON 변환과 현재 CSV 기준 복원을 담당한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class MuseumBottleRuntimeState
{
    internal readonly HashSet<string> ContributedIds =
        new HashSet<string>(StringComparer.Ordinal);
    internal bool Completed;
    internal bool RewardGranted;
}

internal static class MuseumBottleStateStore
{
    internal static void Load(IProgressService progress, MuseumBottleDataRepository repository,
        Dictionary<string, MuseumBottleRuntimeState> target)
    {
        BottleSaveCollection saved = null;
        if (progress.TryGetSystemState(MuseumBottleController.STATE_ID, out string json))
        {
            try { saved = JsonUtility.FromJson<BottleSaveCollection>(json); }
            catch (ArgumentException) { saved = null; }
        }
        IReadOnlyList<MuseumBottleDefinition> bottles = repository.Bottles;
        for (int i = 0; i < bottles.Count; i++)
        {
            string bottleId = bottles[i].BottleId;
            MuseumBottleRuntimeState state = new MuseumBottleRuntimeState();
            BottleSaveState source = Find(saved, bottleId);
            if (source?.contributedItemIds != null)
                for (int j = 0; j < source.contributedItemIds.Count; j++)
                    if (repository.TryGetBottleItem(source.contributedItemIds[j], out var item)
                        && item.BottleId == bottleId) state.ContributedIds.Add(item.AlcoholItemId);
            repository.TryGetBottleItems(bottleId, out var required);
            state.Completed = required.Count > 0 && state.ContributedIds.Count == required.Count;
            state.RewardGranted = state.Completed && (source?.rewardGranted ?? false);
            target.Add(bottleId, state);
        }
    }

    internal static void Write(IProgressService progress,
        IReadOnlyDictionary<string, MuseumBottleRuntimeState> states)
    {
        BottleSaveCollection save = new BottleSaveCollection();
        foreach (KeyValuePair<string, MuseumBottleRuntimeState> pair in states)
            save.bottles.Add(new BottleSaveState(pair.Key, pair.Value));
        progress.SetSystemState(MuseumBottleController.STATE_ID, JsonUtility.ToJson(save));
    }

    private static BottleSaveState Find(BottleSaveCollection save, string bottleId)
    {
        if (save?.bottles == null) return null;
        for (int i = 0; i < save.bottles.Count; i++)
            if (save.bottles[i]?.bottleId == bottleId) return save.bottles[i];
        return null;
    }

    [Serializable] private sealed class BottleSaveCollection
    {
        public List<BottleSaveState> bottles = new List<BottleSaveState>();
    }

    [Serializable] private sealed class BottleSaveState
    {
        public string bottleId;
        public List<string> contributedItemIds = new List<string>();
        public bool completed;
        public bool rewardGranted;
        public BottleSaveState() { }
        internal BottleSaveState(string id, MuseumBottleRuntimeState state)
        {
            bottleId = id;
            contributedItemIds.AddRange(state.ContributedIds);
            contributedItemIds.Sort(StringComparer.Ordinal);
            completed = state.Completed;
            rewardGranted = state.RewardGranted;
        }
    }
}
