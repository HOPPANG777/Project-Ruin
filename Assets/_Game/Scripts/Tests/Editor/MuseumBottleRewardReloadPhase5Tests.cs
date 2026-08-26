//=============================================================================
// File:        MuseumBottleRewardReloadPhase5Tests.cs
// Description: 완료·보상 상태 재로드 후 중복 지급 방지를 검증한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public sealed class MuseumBottleRewardReloadPhase5Tests
{
    private GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null) Object.DestroyImmediate(_root);
    }

    [Test]
    public void CompletedRewardStateReloadCannotGrantRewardAgain()
    {
        MuseumBottleDataRepository repository = CreateRepository();
        MuseumBottleTestInventory inventory = new MuseumBottleTestInventory();
        MuseumBottleTestProgress progress = new MuseumBottleTestProgress();
        inventory.SetCount("ALCOHOL_A", 1);
        MuseumBottleController first = CreateController(repository, inventory, progress);
        Assert.That(first.TryContribute("BOTTLE_CH1", "ALCOHOL_A", out _), Is.True);
        Assert.That(inventory.GetItemCount("REWARD_CH1"), Is.EqualTo(2));

        Object.DestroyImmediate(_root);
        _root = null;
        MuseumBottleController loaded = CreateController(repository, inventory, progress);
        inventory.SetCount("ALCOHOL_A", 1);
        Assert.That(loaded.TryContribute(
            "BOTTLE_CH1", "ALCOHOL_A", out MuseumBottleContributionFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(
            MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ALREADY_COMPLETED));
        Assert.That(inventory.GetItemCount("REWARD_CH1"), Is.EqualTo(2));
        Assert.That(inventory.ExchangeCalls, Is.EqualTo(1));
    }

    private MuseumBottleController CreateController(MuseumBottleDataRepository repository,
        MuseumBottleTestInventory inventory, MuseumBottleTestProgress progress)
    {
        _root = new GameObject(nameof(MuseumBottleRewardReloadPhase5Tests));
        MuseumBottleController controller = _root.AddComponent<MuseumBottleController>();
        Assert.That(controller.Init(repository, inventory, progress), Is.True);
        return controller;
    }

    private static MuseumBottleDataRepository CreateRepository()
    {
        MuseumBottleTestReferences references = new MuseumBottleTestReferences();
        references.Add("ALCOHOL_A", 1);
        references.Add("ALCOHOL_B", 2);
        references.Add("ALCOHOL_C", 3);
        references.AddReward("REWARD_CH1");
        references.AddKey("bottle.ch1");
        references.AddKey("bottle.ch2");
        references.AddKey("bottle.ch3");
        MuseumBottleDefinition[] bottles =
        {
            new MuseumBottleDefinition("BOTTLE_CH1", 1, "bottle.ch1", "REWARD_CH1", 2),
            new MuseumBottleDefinition("BOTTLE_CH2", 2, "bottle.ch2", string.Empty, null),
            new MuseumBottleDefinition("BOTTLE_CH3", 3, "bottle.ch3", string.Empty, null)
        };
        MuseumBottleItemDefinition[] items =
        {
            new MuseumBottleItemDefinition("BOTTLE_CH1", 0, "ALCOHOL_A", 1),
            new MuseumBottleItemDefinition("BOTTLE_CH2", 0, "ALCOHOL_B", 1),
            new MuseumBottleItemDefinition("BOTTLE_CH3", 0, "ALCOHOL_C", 1)
        };
        Assert.That(MuseumBottleDataRepository.TryCreate(
            bottles, items, references, out MuseumBottleDataRepository repository,
            out GameDataRepositoryBuildError error), Is.True, error?.ToString());
        return repository;
    }
}
