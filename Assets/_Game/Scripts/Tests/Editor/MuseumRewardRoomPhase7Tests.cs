//=============================================================================
// File:        MuseumRewardRoomPhase7Tests.cs
// Description: 실제 도감·전시 Controller를 사용한 최종 방 개방·복원 테스트
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class MuseumRewardRoomPhase7Tests
{
    private GameObject _root;
    private MuseumRewardRoomController _room;
    private CodexController _codex;
    private MuseumRelicDisplayController _displays;
    private MuseumRelicTestData _data;
    private MuseumBottleTestInventory _inventory;
    private MuseumBottleTestProgress _progress;
    private MuseumRelicMilestoneRepository _rewards;

    [SetUp]
    public void SetUp()
    {
        _data = new MuseumRelicTestData();
        for (int i = 1; i <= 20; i++) _data.AddRelic(Relic(i), i);
        _data.AddItem("TEST_BONUS");
        _data.AddItem("TEST_SWORD");
        var definitions = new[]
        {
            new MuseumRelicMilestoneDefinition("TEST_TEN", 10,
                MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_GRANT_IMMEDIATELY, "TEST_BONUS", 1),
            new MuseumRelicMilestoneDefinition("TEST_ALL", 20,
                MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP, "TEST_SWORD", 1)
        };
        Assert.That(MuseumRelicMilestoneRepository.TryCreate(definitions, _data, out _rewards, out _), Is.True);
        _progress = new MuseumBottleTestProgress();
        _inventory = new MuseumBottleTestInventory { TransactionProgress = _progress };
        CreateControllers();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    [Test]
    public void NineteenDiscoveriesStayLockedAndTwentiethOpensImmediately()
    {
        Discover(1, 19);
        Assert.That(_room.IsOpen, Is.False);
        Assert.That(_room.TryClaimReward("TEST_ALL", out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ROOM_LOCKED));
        Discover(20, 20);
        Assert.That(_room.IsOpen, Is.True);
        Assert.That(_progress.GetFlag(MuseumRewardRoomController.OPEN_FLAG_ID), Is.True);
        Assert.That(_displays.DisplayedRelicCount, Is.Zero);
    }

    [Test]
    public void HeaderOnlyCodexNeverOpensRoom()
    {
        Assert.That(_room.Init(new MuseumRelicTestData(), _codex, _progress, _displays), Is.True);
        Assert.That(_room.TryOpen(out _), Is.False);
        Assert.That(_room.IsOpen, Is.False);
    }

    [Test]
    public void DiscoveryRemainsValidWithNoOwnedItemsAndNoExhibits()
    {
        Discover(1, 20);
        for (int i = 1; i <= 20; i++) _inventory.TryConsumeItem(Relic(i), 1);
        Object.DestroyImmediate(_root);
        CreateControllers();
        Assert.That(_room.IsOpen, Is.True);
        Assert.That(_inventory.GetItemCount(Relic(1)), Is.Zero);
        Assert.That(_room.TryClaimReward("TEST_ALL", out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ELIGIBLE));
    }

    [Test]
    public void LegacyOwnedItemsAreReconciledBeforeDoorEvaluation()
    {
        for (int i = 1; i <= 20; i++) _inventory.SetCount(Relic(i), 1);
        Object.DestroyImmediate(_root);
        CreateControllers();
        Assert.That(_room.IsOpen, Is.True);
    }

    [Test]
    public void PersistedOpenFlagSurvivesMissingContentAndDoesNotResave()
    {
        _progress.SetFlag(MuseumRewardRoomController.OPEN_FLAG_ID, true);
        int saves = _progress.SaveCalls;
        _room.Init(new MuseumRelicTestData(), _codex, _progress, _displays);
        Assert.That(_room.IsOpen, Is.True);
        Assert.That(_progress.SaveCalls, Is.EqualTo(saves));
    }

    [Test]
    public void FailedFlagApplyDoesNotOpenAndCanRetry()
    {
        Discover(1, 19);
        _progress.SetFlag(ICodexService.ITEM_DISCOVERY_FLAG_PREFIX + Relic(20), true);
        _progress.ThrowOnFlagWrite = true;
        LogAssert.Expect(LogType.Error, "[MuseumRewardRoomController] 문 상태 적용 실패: flag write failed");
        Assert.That(_room.TryOpen(out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED));
        Assert.That(_room.IsOpen, Is.False);
        _progress.ThrowOnFlagWrite = false;
        Assert.That(_room.TryOpen(out _), Is.True);
    }

    [Test]
    public void DiskFailureKeepsOpenAndDirtyUntilRetry()
    {
        Discover(1, 19);
        _progress.SaveSucceeds = false;
        LogAssert.Expect(LogType.Warning, "[MuseumRewardRoomController] 문 개방 저장 실패. Dirty 상태로 재저장합니다.");
        Discover(20, 20);
        Assert.That(_room.IsOpen, Is.True);
        Assert.That(_progress.IsDirty, Is.True);
        _progress.SaveSucceeds = true;
        Assert.That(_progress.SaveAsync().GetAwaiter().GetResult(), Is.True);
        Assert.That(_progress.IsDirty, Is.False);
    }

    [Test]
    public void ReinitializationDoesNotMultiplyEventSubscriptionsOrSaves()
    {
        _room.Init(_data, _codex, _progress, _displays);
        _room.Init(_data, _codex, _progress, _displays);
        int changes = 0;
        _room.StateChanged += () => changes++;
        int saves = _progress.SaveCalls;
        Discover(1, 20);
        Discover(20, 20);
        Assert.That(changes, Is.EqualTo(1));
        Assert.That(_progress.SaveCalls, Is.EqualTo(saves + 1));
    }

    [Test]
    public void ThrowingViewDoesNotBlockOtherViewsOrPersistence()
    {
        int changes = 0;
        _room.StateChanged += () => throw new System.InvalidOperationException("view failed");
        _room.StateChanged += () => changes++;
        int saves = _progress.SaveCalls;
        LogAssert.Expect(LogType.Error, "[MuseumRewardRoomController] 표시 알림 실패: view failed");
        Discover(1, 20);
        Assert.That(changes, Is.EqualTo(1));
        Assert.That(_room.IsOpen, Is.True);
        Assert.That(_progress.SaveCalls, Is.EqualTo(saves + 1));
    }

    private void CreateControllers()
    {
        _root = new GameObject(nameof(MuseumRewardRoomPhase7Tests));
        _codex = _root.AddComponent<CodexController>();
        _displays = _root.AddComponent<MuseumRelicDisplayController>();
        _room = _root.AddComponent<MuseumRewardRoomController>();
        Assert.That(_displays.Init(_rewards, _data, _inventory, _progress), Is.True);
        for (int i = 1; i <= 20; i++) _displays.RegisterPedestal("P" + i);
        Assert.That(_codex.Init(_data, _inventory, _progress, null), Is.True);
        Assert.That(_room.Init(_data, _codex, _progress, _displays), Is.True);
    }

    private void Discover(int first, int last)
    {
        for (int i = first; i <= last; i++) Assert.That(_inventory.TryAddItem(Relic(i), 1), Is.True);
    }

    private void ExhibitAll()
    {
        Discover(1, 20);
        for (int i = 1; i <= 20; i++) Assert.That(_displays.TryPlace("P" + i, Relic(i), out _), Is.True);
    }

    private static string Relic(int index) => "TEST_RELIC_" + index;
}
