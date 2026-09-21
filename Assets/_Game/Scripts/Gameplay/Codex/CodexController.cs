//=============================================================================
// File:        CodexController.cs
//
// Description: 도감 영구 발견 상태와 기술 해제 상태를 조회·동기화한다.
//
// History:     08/19/2026 + Created -- Codex
//              08/31/2026 + 발견 조회 키를 초기화 시 캐시하여 반복 문자열 할당 제거 -- Codex
//=============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public interface ICodexService
{
    const string ITEM_DISCOVERY_FLAG_PREFIX = "codex.item.discovered.";

    /// <summary>아이템 도감 항목이 처음 발견될 때 Entry ID를 전달합니다.</summary>
    event Action<string> DiscoveryChanged;

    /// <summary>Entry ID의 도감 정보가 해제됐는지 확인합니다.</summary>
    bool IsEntryUnlocked(string entryId);

    /// <summary>유물 또는 술 Item ID가 영구 발견됐는지 확인합니다.</summary>
    bool IsItemDiscovered(string itemId);
}

//=============================================================================
//
// Synopsis:   CodexController;
//                - 아이템 최초 획득을 Progress Flag로 영구 기록
//                - 기존 저장의 현재 보유 아이템을 초기화 시 동기화
//                - 기술은 기존 획득 상태를 중복 저장하지 않고 조회
//=============================================================================
[DisallowMultipleComponent]
public sealed class CodexController : MonoBehaviour, ICodexService
{
    private ICodexDataRepository _data;
    private IInventoryService _inventory;
    private IProgressService _progress;
    private IRelicRestorationService _restoration;
    private bool _isInitialized;
    // 초기화 때만 생성하는 조회 키입니다. 발견 bool은 Progress에서 직접 읽습니다.
    private readonly Dictionary<string, CodexEntryDefinition> _itemEntries =
        new Dictionary<string, CodexEntryDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _discoveryFlagIds =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>아이템 도감 항목이 처음 발견될 때 Entry ID를 전달합니다.</summary>
    public event Action<string> DiscoveryChanged;

    private void OnDestroy() => Unsubscribe();

    /// <summary>도감 정의와 상태 서비스를 주입하고 기존 보유 아이템을 동기화합니다.</summary>
    public bool Init(
        ICodexDataRepository data,
        IInventoryService inventory,
        IProgressService progress,
        IRelicRestorationService restoration)
    {
        Unsubscribe();
        _data = data;
        _inventory = inventory;
        _progress = progress;
        _restoration = restoration;
        _isInitialized = _data != null && _inventory != null && _progress != null;

        if (!_isInitialized)
        {
            Debug.LogWarning("[CodexController] Data, Inventory 또는 Progress가 연결되지 않았습니다.");
            return false;
        }

        BuildItemLookup();
        _inventory.ItemCountChanged += HandleItemCountChanged;
        ReconcileOwnedItems();
        return true;
    }

    /// <summary>Entry ID의 도감 정보가 해제됐는지 확인합니다.</summary>
    public bool IsEntryUnlocked(string entryId)
    {
        if (!_isInitialized || string.IsNullOrWhiteSpace(entryId)
            || !_data.TryGetCodexEntry(entryId, out CodexEntryDefinition entry))
        {
            return false;
        }

        return entry.Category == CodexCategory.CODEX_CATEGORY_TECHNOLOGY
            ? _restoration != null && _restoration.IsAcquired(entry.SourceId)
            : IsItemDiscovered(entry.SourceId);
    }

    /// <summary>유물 또는 술 Item ID가 영구 발견됐는지 확인합니다.</summary>
    public bool IsItemDiscovered(string itemId)
    {
        if (!_isInitialized || string.IsNullOrWhiteSpace(itemId)
            || !_discoveryFlagIds.TryGetValue(itemId, out string flagId))
        {
            return false;
        }
        return _progress.GetFlag(flagId);
    }

    // 첫 발견 알림을 보내기 전에 모든 항목의 조회 키가 준비돼 있어야 합니다.
    private void BuildItemLookup()
    {
        IReadOnlyList<CodexEntryDefinition> entries = _data.CodexEntries;
        for (int i = 0; i < entries.Count; i++)
        {
            CodexEntryDefinition entry = entries[i];
            if (entry.Category == CodexCategory.CODEX_CATEGORY_TECHNOLOGY) continue;
            _itemEntries[entry.SourceId] = entry;
            _discoveryFlagIds[entry.SourceId] = CreateDiscoveryFlagId(entry.SourceId);
        }
    }

    private void ReconcileOwnedItems()
    {
        IReadOnlyList<CodexEntryDefinition> entries = _data.CodexEntries;
        for (int i = 0; i < entries.Count; i++)
        {
            CodexEntryDefinition entry = entries[i];
            if (entry.Category != CodexCategory.CODEX_CATEGORY_TECHNOLOGY
                && _inventory.GetItemCount(entry.SourceId) > 0)
            {
                TryMarkDiscovered(entry);
            }
        }
    }

    private void HandleItemCountChanged(string itemId, int finalCount)
    {
        if (!_isInitialized || finalCount <= 0 || !TryGetItemEntry(itemId, out CodexEntryDefinition entry))
        {
            return;
        }
        TryMarkDiscovered(entry);
    }

    private bool TryMarkDiscovered(CodexEntryDefinition entry)
    {
        string flagId = _discoveryFlagIds[entry.SourceId];
        if (_progress.GetFlag(flagId))
        {
            return false;
        }
        _progress.SetFlag(flagId, true);
        DiscoveryChanged?.Invoke(entry.EntryId);
        return true;
    }

    private bool TryGetItemEntry(string itemId, out CodexEntryDefinition entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }
        return _itemEntries.TryGetValue(itemId, out entry);
    }

    private static string CreateDiscoveryFlagId(string itemId) =>
        string.Concat(ICodexService.ITEM_DISCOVERY_FLAG_PREFIX, itemId);

    private void Unsubscribe()
    {
        if (_inventory != null)
        {
            _inventory.ItemCountChanged -= HandleItemCountChanged;
        }
        _isInitialized = false;
        _itemEntries.Clear();
        _discoveryFlagIds.Clear();
        _data = null;
        _inventory = null;
        _progress = null;
        _restoration = null;
    }
}
