//=============================================================================
// File:        MuseumRewardRoomPhase7RewardTests.cs
// Description: 최종 방의 보상 원자성·중복·실패·재로드 회귀 테스트
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using NUnit.Framework;
using UnityEngine;

public sealed partial class MuseumRewardRoomPhase7Tests
{
    [Test]
    public void SavedGameRootPrefabHasUniqueRoomControllerAndDependencies()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab");
        var root = prefab.GetComponent<GameRoot>();
        Assert.That(prefab.GetComponents<MuseumRewardRoomController>().Length, Is.EqualTo(1));
        Assert.That(root.MuseumRewardRoom, Is.SameAs(prefab.GetComponent<MuseumRewardRoomController>()));
        Assert.That(root.Codex, Is.Not.Null);
        Assert.That(root.MuseumRelicDisplays, Is.Not.Null);
        Assert.That(root.Progress, Is.Not.Null);
        Assert.That(root.Museum, Is.Not.Null);
    }

    [Test]
    public void SwordRequiresAllCurrentlyExhibitedEvenAfterDoorOpened()
    {
        ExhibitAll();
        Assert.That(_displays.TryRecover("P20", out _), Is.True);
        Assert.That(_room.IsOpen, Is.True);
        Assert.That(_room.TryClaimReward("TEST_ALL", out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ELIGIBLE));
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.Zero);
    }

    [Test]
    public void ClaimedSwordRestoresAfterReloadWithoutBeingGrantedAgain()
    {
        ExhibitAll();
        Assert.That(_room.TryClaimReward("TEST_ALL", out _), Is.True);
        Assert.That(_inventory.TryConsumeItem("TEST_SWORD", 1), Is.True);
        Object.DestroyImmediate(_root);
        CreateControllers();
        Assert.That(_room.IsOpen, Is.True);
        Assert.That(_room.TryGetRewardStatus("TEST_ALL", out var status), Is.True);
        Assert.That(status.IsClaimed, Is.True);
        Assert.That(_room.TryClaimReward("TEST_ALL", out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_ALREADY_CLAIMED));
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InventoryFailureDoesNotClaimAndRetryGrantsExactlyOnce(bool overflow)
    {
        ExhibitAll();
        _inventory.SetCount("TEST_SWORD", overflow ? int.MaxValue : 0);
        _inventory.BlockReward = !overflow;
        Assert.That(_room.TryClaimReward("TEST_ALL", out _), Is.False);
        Assert.That(_room.TryGetRewardStatus("TEST_ALL", out var status), Is.True);
        Assert.That(status.IsClaimed, Is.False);
        _inventory.BlockReward = false;
        _inventory.SetCount("TEST_SWORD", 0);
        Assert.That(_room.TryClaimReward("TEST_ALL", out _), Is.True);
        Assert.That(_room.TryClaimReward("TEST_ALL", out _), Is.False);
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.EqualTo(1));
    }

    [Test]
    public void StateApplyFailureKeepsSwordAndClaimUnchanged()
    {
        ExhibitAll();
        _progress.ThrowOnStateWrite = true;
        Assert.That(_room.TryClaimReward("TEST_ALL", out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_STATE_APPLY_FAILED));
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.Zero);
        _room.TryGetRewardStatus("TEST_ALL", out var status);
        Assert.That(status.IsClaimed, Is.False);
        _progress.ThrowOnStateWrite = false;
        Assert.That(_room.TryClaimReward("TEST_ALL", out _), Is.True);
    }

    [Test]
    public void ReentrantPickupIsBlockedByExistingTransaction()
    {
        ExhibitAll();
        bool nested = true;
        MuseumRelicDisplayFailure nestedFailure = default;
        _inventory.OnBeforeAdd = () => nested = _room.TryClaimReward("TEST_ALL", out nestedFailure);
        Assert.That(_room.TryClaimReward("TEST_ALL", out _), Is.True);
        Assert.That(nested, Is.False);
        Assert.That(nestedFailure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_BUSY));
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.EqualTo(1));
    }

    [Test]
    public void MissingOrImmediateMilestoneCannotBeClaimedAsSword()
    {
        ExhibitAll();
        Assert.That(_room.TryClaimReward("MISSING", out var missing), Is.False);
        Assert.That(missing, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_UNKNOWN_MILESTONE));
        Assert.That(_room.TryClaimReward("TEST_TEN", out var immediate), Is.False);
        Assert.That(immediate, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ROOM_PICKUP));
        Assert.That(_room.TryGetRewardStatus("TEST_TEN", out _), Is.False);
    }
}
