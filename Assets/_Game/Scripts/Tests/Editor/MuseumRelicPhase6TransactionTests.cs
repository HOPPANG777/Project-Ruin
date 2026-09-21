//=============================================================================
// File:        MuseumRelicPhase6TransactionTests.cs
// Description: 실제 인벤토리 슬롯·Progress와 유물 상태의 원자적 적용을 검증한다.
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>실제 서비스 연동과 재진입을 검증하는 Phase 6 픽스처입니다.</summary>
public sealed partial class MuseumRelicPhase6Tests
{
    [TestCase("place")]
    [TestCase("replace")]
    [TestCase("recover")]
    [TestCase("reward")]
    public void RealInventoryStateFailurePreservesSlotsAndBothSaveStates(string operation)
    {
        InventoryController real = _root.AddComponent<InventoryController>();
        real.Init(_data, null, _progress);
        Assert.That(_controller.Init(_repository, _data, real, _progress), Is.True);
        Assert.That(real.TryAddItem(Relic(1), 2), Is.True);
        Assert.That(real.TryAddItem(Relic(2), 1), Is.True);
        real.TryMarkItemChecked(Relic(1));
        if (operation != "place") Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        if (operation == "reward")
            for (int i = 2; i <= 20; i++)
            {
                real.TryAddItem(Relic(i), 1);
                Assert.That(_controller.TryPlace($"P{i:00}", Relic(i), out _), Is.True);
            }
        _progress.TryGetSystemState("inventory.slots", out string beforeInventory);
        _progress.TryGetSystemState(MuseumRelicDisplayController.STATE_ID, out string beforeMuseum);
        int beforeCount = _controller.DisplayedRelicCount;
        int count1 = real.GetItemCount(Relic(1));
        int count2 = real.GetItemCount(Relic(2));
        var originalSlots = new System.Collections.Generic.List<InventorySlotData>(
            real.GetSlotsByAcquisitionOrder());
        int notifications = 0;
        real.ItemCountChanged += (_, __) => notifications++;
        _progress.ThrowOnStateWrite = true;
        MuseumRelicDisplayFailure failure;
        bool result;
        if (operation == "recover") result = _controller.TryRecover("P01", out failure);
        else if (operation == "reward") result = _controller.TryClaimRoomReward("MILESTONE_20", out failure);
        else result = _controller.TryPlace("P01", operation == "place" ? Relic(1) : Relic(2), out failure);
        Assert.That(result, Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED));
        Assert.That(_controller.DisplayedRelicCount, Is.EqualTo(beforeCount));
        Assert.That(real.GetItemCount(Relic(1)), Is.EqualTo(count1));
        Assert.That(real.GetItemCount(Relic(2)), Is.EqualTo(count2));
        Assert.That(real.GetItemCount("ITEM_REWARD_20"), Is.Zero);
        Assert.That(notifications, Is.Zero);
        var restoredSlots = real.GetSlotsByAcquisitionOrder();
        Assert.That(restoredSlots.Count, Is.EqualTo(originalSlots.Count));
        for (int i = 0; i < originalSlots.Count; i++)
            Assert.That(restoredSlots[i], Is.SameAs(originalSlots[i]));
        _progress.TryGetSystemState("inventory.slots", out string afterInventory);
        _progress.TryGetSystemState(MuseumRelicDisplayController.STATE_ID, out string afterMuseum);
        Assert.That(afterInventory, Is.EqualTo(beforeInventory));
        Assert.That(afterMuseum, Is.EqualTo(beforeMuseum));
        _progress.ThrowOnStateWrite = false;
        Assert.That(real.TryAddItem(Relic(1), 1), Is.True);
        Assert.That(real.GetItemCount(Relic(1)), Is.EqualTo(count1 + 1));
    }

    [Test]
    public void ReentrantRoomClaimAndImmediateRetryNeverGrantTwice()
    {
        _inventory.BlockReward = true;
        PlaceRange(1, 20);
        _inventory.BlockReward = false;
        _inventory.OnBeforeAdd = () => _controller.RetryPendingImmediateRewards();
        _controller.RetryPendingImmediateRewards();
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.EqualTo(1));
        MuseumRelicDisplayFailure nested = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NONE;
        _inventory.OnBeforeAdd = () => _controller.TryClaimRoomReward("MILESTONE_20", out nested);
        Assert.That(_controller.TryClaimRoomReward("MILESTONE_20", out _), Is.True);
        Assert.That(nested, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_BUSY));
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_20"), Is.EqualTo(1));
    }

    [Test]
    public void SubscriberExceptionCannotUndoCommittedStateOrSkipSave()
    {
        InventoryController real = _root.AddComponent<InventoryController>();
        real.Init(_data, null, _progress);
        _controller.Init(_repository, _data, real, _progress);
        real.TryAddItem(Relic(1), 1);
        real.ItemCountChanged += (_, __) => throw new InvalidOperationException("inventory subscriber");
        bool sawCommittedState = false;
        real.ItemCountChanged += (_, __) =>
            sawCommittedState = _controller.TryGetDisplayedRelic("P01", out _);
        _controller.ExhibitChanged += _ => throw new InvalidOperationException("museum subscriber");
        LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("inventory subscriber"));
        LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("museum subscriber"));
        int before = _progress.SaveCalls;
        Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        Assert.That(sawCommittedState, Is.True);
        Assert.That(real.GetItemCount(Relic(1)), Is.Zero);
        Assert.That(_progress.SaveCalls, Is.EqualTo(before + 1));
    }

    [Test]
    public void RealProgressPairRejectsInvalidSecondStateWithoutPartialChange()
    {
        ProgressController progress = _root.AddComponent<ProgressController>();
        Assert.That(progress.TrySetSystemStates("first", "old", "second", "old"), Is.True);
        Assert.That(progress.TrySetSystemStates("first", "new", "", "new"), Is.False);
        Assert.That(progress.TryGetSystemState("first", out string first), Is.True);
        Assert.That(first, Is.EqualTo("old"));
        Assert.That(progress.TryGetSystemState("second", out string second), Is.True);
        Assert.That(second, Is.EqualTo("old"));
        Assert.That(progress.IsDirty, Is.True);
    }
}
