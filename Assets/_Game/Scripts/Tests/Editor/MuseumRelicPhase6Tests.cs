//=============================================================================
// File:        MuseumRelicPhase6Tests.cs
// Description: Phase 6 배치·교체·저장·마일스톤 경계 회귀 테스트
// History:     08/30/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed partial class MuseumRelicPhase6Tests
{
    private GameObject _root;
    private MuseumRelicDisplayController _controller;
    private MuseumRelicTestData _data;
    private MuseumBottleTestInventory _inventory;
    private MuseumBottleTestProgress _progress;
    private MuseumRelicMilestoneRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _data = new MuseumRelicTestData();
        for (int i = 1; i <= 20; i++) _data.AddRelic(Relic(i), i - 1);
        _data.AddItem("ITEM_REWARD_10");
        _data.AddItem("ITEM_REWARD_20");
        _data.AddItem("ITEM_NOT_RELIC", ItemType.ITEM_MISC);
        MuseumRelicMilestoneDefinition[] values =
        {
            new MuseumRelicMilestoneDefinition("MILESTONE_10", 10,
                MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_GRANT_IMMEDIATELY,
                "ITEM_REWARD_10", 1),
            new MuseumRelicMilestoneDefinition("MILESTONE_20", 20,
                MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP,
                "ITEM_REWARD_20", 1)
        };
        Assert.That(MuseumRelicMilestoneRepository.TryCreate(
            values, _data, out _repository, out _), Is.True);
        _inventory = new MuseumBottleTestInventory();
        _progress = new MuseumBottleTestProgress();
        _inventory.TransactionProgress = _progress;
        CreateController();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    [Test]
    public void NonRelicAndUnownedRelicCannotBePlaced()
    {
        _inventory.SetCount("ITEM_NOT_RELIC", 1);
        Assert.That(_controller.TryPlace("P01", "ITEM_NOT_RELIC", out var nonRelic), Is.False);
        Assert.That(nonRelic, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ITEM_NOT_RELIC));
        Assert.That(_controller.TryPlace("P01", Relic(1), out var unowned), Is.False);
        Assert.That(unowned, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ITEM_NOT_OWNED));
    }

    [Test]
    public void SameRelicCannotBeDisplayedTwice()
    {
        _inventory.SetCount(Relic(1), 2);
        Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        Assert.That(_controller.TryPlace("P02", Relic(1), out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_ALREADY_DISPLAYED));
        Assert.That(_inventory.GetItemCount(Relic(1)), Is.EqualTo(1));
    }

    [Test]
    public void PlaceReplaceAndRecoverNeverLoseItems()
    {
        Own(1, 2);
        Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        Assert.That(_controller.TryPlace("P01", Relic(2), out _), Is.True);
        Assert.That(_inventory.GetItemCount(Relic(1)), Is.EqualTo(1));
        Assert.That(_inventory.GetItemCount(Relic(2)), Is.Zero);
        Assert.That(_controller.TryRecover("P01", out _), Is.True);
        Assert.That(_inventory.GetItemCount(Relic(2)), Is.EqualTo(1));
        Assert.That(_controller.DisplayedRelicCount, Is.Zero);
    }

    [Test]
    public void FailedReplaceAndOverflowRecoveryKeepOriginalState()
    {
        Own(1, 2);
        Assert.That(_controller.TryPlace("P01", Relic(1), out _), Is.True);
        _inventory.BlockReward = true;
        Assert.That(_controller.TryPlace("P01", Relic(2), out _), Is.False);
        Assert.That(_controller.TryGetDisplayedRelic("P01", out string displayed), Is.True);
        Assert.That(displayed, Is.EqualTo(Relic(1)));
        Assert.That(_inventory.GetItemCount(Relic(2)), Is.EqualTo(1));
        Assert.That(_controller.TryRecover("P01", out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVENTORY_OVERFLOW));
    }




    [Test]
    public void SavedExhibitsReloadExactly()
    {
        Own(1, 2);
        _controller.TryPlace("P01", Relic(1), out _);
        _controller.TryPlace("P02", Relic(2), out _);
        Object.DestroyImmediate(_root);
        CreateController();
        Assert.That(_controller.DisplayedRelicCount, Is.EqualTo(2));
        Assert.That(_controller.TryGetDisplayedRelic("P02", out string id), Is.True);
        Assert.That(id, Is.EqualTo(Relic(2)));
    }

    [Test]
    public void ImmediateMilestoneUsesTenBoundaryAndNeverDuplicates()
    {
        PlaceRange(1, 9);
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.Zero);
        PlaceRange(10, 10);
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.EqualTo(1));
        _controller.RetryPendingImmediateRewards();
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.EqualTo(1));
    }

    [Test]
    public void TenAndTwentyClaimsNeverDuplicateAfterReload()
    {
        PlaceRange(1, 10);
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.EqualTo(1));
        ReloadController();
        _controller.RetryPendingImmediateRewards();
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.EqualTo(1));

        PlaceRange(11, 20);
        Assert.That(_controller.TryClaimRoomReward("MILESTONE_20", out _), Is.True);
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_20"), Is.EqualTo(1));
        ReloadController();
        Assert.That(_controller.TryClaimRoomReward(
            "MILESTONE_20", out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(
            MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_ALREADY_CLAIMED));
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_20"), Is.EqualTo(1));
    }

    [Test]
    public void FailedImmediateRewardRemainsRetryable()
    {
        _inventory.BlockReward = true;
        PlaceRange(1, 10);
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.Zero);
        Assert.That(_controller.TryGetMilestone("MILESTONE_10", out _, out var pending), Is.True);
        Assert.That(pending.IsEligible, Is.True);
        Assert.That(pending.IsClaimed, Is.False);
        _inventory.BlockReward = false;
        _controller.RetryPendingImmediateRewards();
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_10"), Is.EqualTo(1));
    }



    [Test]
    public void RoomPickupUsesTwentyBoundaryAndClaimsOnce()
    {
        PlaceRange(1, 19);
        Assert.That(_controller.TryClaimRoomReward("MILESTONE_20", out var early), Is.False);
        Assert.That(early, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ELIGIBLE));
        PlaceRange(20, 20);
        Assert.That(_inventory.GetItemCount("ITEM_REWARD_20"), Is.Zero);
        Assert.That(_controller.TryClaimRoomReward("MILESTONE_20", out _), Is.True);
        Assert.That(_controller.TryClaimRoomReward("MILESTONE_20", out var duplicate), Is.False);
        Assert.That(duplicate, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_ALREADY_CLAIMED));
    }

    [TestCase("OPEN_ROOM")]
    [TestCase("0")]
    public void CsvRejectsUnknownRewardMode(string mode)
    {
        string csv = "milestone_id,required_exhibit_count,reward_mode,reward_item_id,reward_amount\n"
            + $"MILESTONE_BAD,10,{mode},ITEM_REWARD_10,1";
        Assert.That(MuseumRelicMilestoneCsvLoader.TryLoad(
            new GameDataCsvFile("test", csv), out _, out var error), Is.False);
        Assert.That(error.ColumnName, Is.EqualTo("reward_mode"));
    }

    private void CreateController()
    {
        _root = new GameObject(nameof(MuseumRelicPhase6Tests));
        _controller = _root.AddComponent<MuseumRelicDisplayController>();
        Assert.That(_controller.Init(_repository, _data, _inventory, _progress), Is.True);
        for (int i = 1; i <= 20; i++) _controller.RegisterPedestal($"P{i:00}");
    }

    private void ReloadController()
    {
        Object.DestroyImmediate(_root);
        CreateController();
    }

    private void Own(params int[] values)
    { for (int i = 0; i < values.Length; i++) _inventory.SetCount(Relic(values[i]), 1); }
    private void PlaceRange(int first, int last)
    {
        for (int i = first; i <= last; i++)
        { Own(i); Assert.That(_controller.TryPlace($"P{i:00}", Relic(i), out _), Is.True); }
    }
    private static string Relic(int value) => $"ITEM_RELIC_{value:00}";
}
