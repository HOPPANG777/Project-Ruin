//=============================================================================
// File:        MuseumRewardRoomController.cs
// Description: 영구 발견 기반 문 개방과 기존 전시 보상의 방 수령을 연결한다.
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>최종 방의 영구 개방과 보상 수령 계약입니다.</summary>
public interface IMuseumRewardRoomService
{
    /// <summary>초기화, 문 개방, 전시 및 수령 변경 후 표시를 갱신합니다.</summary>
    event Action StateChanged;
    /// <summary>한 번 열린 뒤 영구 유지되는 문 개방 상태입니다.</summary>
    bool IsOpen { get; }
    /// <summary>현재 발견 상태로 문 개방을 재시도합니다.</summary>
    bool TryOpen(out MuseumRelicDisplayFailure failure);
    /// <summary>방 수령 마일스톤의 현재 조건과 영구 수령 상태를 조회합니다.</summary>
    bool TryGetRewardStatus(string milestoneId, out MuseumRelicMilestoneStatus status);
    /// <summary>문 개방 및 전시 조건을 재검증하고 기존 원자적 보상을 요청합니다.</summary>
    bool TryClaimReward(string milestoneId, out MuseumRelicDisplayFailure failure);
}

/// <summary>CSV 도감 발견을 문 개방 상태에 연결하며 보상 원본은 복제하지 않습니다.</summary>
[DisallowMultipleComponent]
public sealed class MuseumRewardRoomController : MonoBehaviour, IMuseumRewardRoomService
{
    /// <summary>보상 수령 System State와 분리된 영구 문 개방 Flag입니다.</summary>
    public const string OPEN_FLAG_ID = "museum.reward_room.open.v1";
    // 초기화 때만 구성하는 수집형 유물 ID 목록입니다.
    private readonly List<string> _relicIds = new List<string>();
    private ICodexService _codex;
    private IProgressService _progress;
    private IMuseumRelicDisplayService _displays;
    private bool _isOpening;
    /// <inheritdoc/>
    public event Action StateChanged;
    /// <inheritdoc/>
    public bool IsOpen { get; private set; }

    //=============================================================================
    // Init
    // Description: 도감 초기 동기화 이후 GameRoot에서 호출합니다.
    // Parameters: ICodexDataRepository data, ICodexService codex, IProgressService progress, IMuseumRelicDisplayService displays
    // Return: bool — 의존성 연결 성공 여부
    //=============================================================================
    /// <summary>저장된 개방 상태를 복원하고 최초 발견 및 전시 이벤트를 연결합니다.</summary>
    public bool Init(ICodexDataRepository data, ICodexService codex,
        IProgressService progress, IMuseumRelicDisplayService displays)
    {
        Unsubscribe();
        IsOpen = false;
        _relicIds.Clear();
        if (data == null || codex == null || progress == null || displays == null)
        {
            NotifyChanged();
            return false;
        }
        _codex = codex;
        _progress = progress;
        _displays = displays;
        for (int i = 0; i < data.CodexEntries.Count; i++)
            if (data.CodexEntries[i].Category == CodexCategory.CODEX_CATEGORY_RELIC)
                _relicIds.Add(data.CodexEntries[i].SourceId);
        IsOpen = _progress.GetFlag(OPEN_FLAG_ID);
        _codex.DiscoveryChanged += HandleDiscovery;
        _displays.ExhibitChanged += HandleDisplay;
        _displays.MilestoneChanged += HandleDisplay;
        TryOpen(out _);
        NotifyChanged();
        return true;
    }

    //=============================================================================
    // TryOpen
    // Description: 비어 있지 않은 전체 유물 발견 조건으로 영구 개방을 적용합니다.
    // Parameters: out MuseumRelicDisplayFailure failure
    // Return: bool — 문이 열려 있는지 여부
    //=============================================================================
    /// <inheritdoc/>
    public bool TryOpen(out MuseumRelicDisplayFailure failure)
    {
        failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NONE;
        if (_progress == null)
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NOT_INITIALIZED, out failure);
        if (IsOpen) return true;
        if (_isOpening)
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_BUSY, out failure);
        // 헤더 전용 CSV가 전체 발견으로 취급되지 않도록 차단합니다.
        if (_relicIds.Count == 0)
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ROOM_LOCKED, out failure);
        for (int i = 0; i < _relicIds.Count; i++)
            if (!_codex.IsItemDiscovered(_relicIds[i]))
                return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ROOM_LOCKED, out failure);
        _isOpening = true;
        try
        {
            _progress.SetFlag(OPEN_FLAG_ID, true);
            IsOpen = _progress.GetFlag(OPEN_FLAG_ID);
            if (!IsOpen)
                return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED, out failure);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MuseumRewardRoomController] 문 상태 적용 실패: {exception.Message}", this);
            return Fail(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED, out failure);
        }
        finally { _isOpening = false; }
        NotifyChanged();
        SaveAsync().Forget();
        return true;
    }

    //=============================================================================
    // TryGetRewardStatus / TryClaimReward
    // Description: 전시 도메인의 보상 정의와 트랜잭션을 그대로 재사용합니다.
    // Parameters: string milestoneId, out status / failure
    // Return: bool — 조회 또는 수령 성공 여부
    //=============================================================================
    /// <inheritdoc/>
    public bool TryGetRewardStatus(string milestoneId, out MuseumRelicMilestoneStatus status)
    {
        status = default;
        return _displays != null && _displays.TryGetMilestone(milestoneId, out var value, out status)
            && value.RewardMode == MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP;
    }

    /// <inheritdoc/>
    public bool TryClaimReward(string milestoneId, out MuseumRelicDisplayFailure failure)
    {
        if (!TryOpen(out failure)) return false;
        return _displays.TryClaimRoomReward(milestoneId, out failure);
    }

    // 발견 시에만 전체 조건을 평가합니다. 전시 변경으로 문을 다시 잠그지 않습니다.
    private void HandleDiscovery(string entryId) => TryOpen(out _);
    private void HandleDisplay(string id) => NotifyChanged();
    private void OnDestroy() => Unsubscribe();

    private void Unsubscribe()
    {
        if (_codex != null) _codex.DiscoveryChanged -= HandleDiscovery;
        if (_displays != null)
        {
            _displays.ExhibitChanged -= HandleDisplay;
            _displays.MilestoneChanged -= HandleDisplay;
        }
        _codex = null;
        _displays = null;
        _progress = null;
    }

    // 한 표시 구독자의 실패가 저장이나 다른 표시 갱신을 막지 않게 격리합니다.
    private void NotifyChanged()
    {
        if (StateChanged == null) return;
        foreach (Action subscriber in StateChanged.GetInvocationList())
        {
            try { subscriber(); }
            catch (Exception exception)
            { Debug.LogError($"[MuseumRewardRoomController] 표시 알림 실패: {exception.Message}", this); }
        }
    }

    // 디스크 실패는 개방을 되돌리지 않고 기존 Progress Dirty 재저장 대상으로 남깁니다.
    private async UniTask SaveAsync()
    {
        try
        {
            if (!await _progress.SaveAsync(this.GetCancellationTokenOnDestroy()))
                Debug.LogWarning("[MuseumRewardRoomController] 문 개방 저장 실패. Dirty 상태로 재저장합니다.", this);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        { Debug.LogWarning($"[MuseumRewardRoomController] 문 개방 저장 실패: {exception.Message}", this); }
    }

    private static bool Fail(MuseumRelicDisplayFailure value, out MuseumRelicDisplayFailure failure)
    {
        failure = value;
        return false;
    }
}
