//=============================================================================
// File:        MuseumRelicPhase6IntegrityTests.cs
// Description: Phase 6 실패 복구·재진입·Dirty 경계를 검증하는 동일 픽스처의 무결성 테스트
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class MuseumRelicPhase6Tests
{
    [Test]
    public void StateApplyFailureRollsBackPlaceReplaceAndRecover()
    {
        Own(1);
        _progress.ThrowOnStateWrite = true;
        Assert.That(_controller.TryPlace("P01", Relic(1), out var emptyFailure), Is.False);
        Assert.That(emptyFailure,
            Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED));
        Assert.That(_inventory.GetItemCount(Relic(1)), Is.EqualTo(1));
        Assert.That(_controller.DisplayedRelicCount, Is.Zero);

        _progress.ThrowOnStateWrite = false;
        Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        Own(2);
        _progress.ThrowOnStateWrite = true;
        Assert.That(_controller.TryPlace("P01", Relic(2), out var replaceFailure), Is.False);
        Assert.That(replaceFailure,
            Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED));
        Assert.That(_controller.TryGetDisplayedRelic("P01", out string displayed), Is.True);
        Assert.That(displayed, Is.EqualTo(Relic(1)));
        Assert.That(_inventory.GetItemCount(Relic(2)), Is.EqualTo(1));

        Assert.That(_controller.TryRecover("P01", out var recoverFailure), Is.False);
        Assert.That(recoverFailure,
            Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED));
        Assert.That(_inventory.GetItemCount(Relic(1)), Is.Zero);
        Assert.That(_controller.TryGetDisplayedRelic("P01", out displayed), Is.True);
    }

    [Test]
    public void RecoverRejectsActualIntOverflow()
    {
        Own(1);
        Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        _inventory.SetCount(Relic(1), int.MaxValue);
        Assert.That(_controller.TryRecover("P01", out var failure), Is.False);
        Assert.That(failure,
            Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVENTORY_OVERFLOW));
        Assert.That(_controller.TryGetDisplayedRelic("P01", out _), Is.True);
    }

    [Test]
    public void ReentrantPlaceIsRejectedAsBusy()
    {
        Own(1);
        MuseumRelicDisplayFailure nestedFailure =
            MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NONE;
        _inventory.OnBeforeConsume = () =>
            _controller.TryPlace("P02", Relic(1), out nestedFailure);
        Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        Assert.That(nestedFailure,
            Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_BUSY));
        Assert.That(_controller.DisplayedRelicCount, Is.EqualTo(1));
    }

    [Test]
    public void FailedRewardStateWriteRemovesRewardAndRemainsRetryable()
    {
        PlaceRange(1, 9);
        _inventory.OnBeforeAdd = () => _progress.ThrowOnStateWrite = true;
        LogAssert.Expect(LogType.Error, "[MuseumRelicDisplayController] 보상 상태 적용 실패: MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED");
        PlaceRange(10, 10);
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.Zero);
        Assert.That(_controller.TryGetMilestone(
            "MILESTONE_10", out _, out var pending), Is.True);
        Assert.That(pending.IsClaimed, Is.False);

        _inventory.OnBeforeAdd = null;
        _progress.ThrowOnStateWrite = false;
        _controller.RetryPendingImmediateRewards();
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.EqualTo(1));
    }

    [Test]
    public void SaveFailureKeepsDirtyAndEmptyRetryDoesNotSaveAgain()
    {
        _progress.SaveSucceeds = false;
        LogAssert.Expect(LogType.Warning, "[MuseumRelicDisplayController] 디스크 저장에 실패했습니다. Dirty 상태로 재저장할 수 있습니다.");
        Own(1);
        Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        Assert.That(_progress.IsDirty, Is.True);
        int saveCalls = _progress.SaveCalls;
        _controller.RetryPendingImmediateRewards();
        Assert.That(_progress.SaveCalls, Is.EqualTo(saveCalls));
        Assert.That(_progress.IsDirty, Is.True);
    }
}
