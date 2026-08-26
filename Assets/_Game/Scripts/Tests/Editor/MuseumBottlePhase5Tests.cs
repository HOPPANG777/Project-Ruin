//=============================================================================
// File:        MuseumBottlePhase5Tests.cs
// Description: Phase 5 CSV, 투입·저장·완성 보상과 재입력 정책을 검증한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public sealed class MuseumBottlePhase5Tests
{
    private const string BOTTLE = "BOTTLE_CH1";
    private const string ALCOHOL_A = "ALCOHOL_A";
    private const string ALCOHOL_B = "ALCOHOL_B";
    private const string REWARD = "REWARD_CH1";
    private MuseumBottleDataRepository _repository;
    private MuseumBottleTestInventory _inventory;
    private MuseumBottleTestProgress _progress;
    private GameObject _root;
    private MuseumBottleController _controller;

    [SetUp]
    public void SetUp()
    {
        _repository = CreateRepository();
        _inventory = new MuseumBottleTestInventory();
        _progress = new MuseumBottleTestProgress();
        _root = new GameObject(nameof(MuseumBottlePhase5Tests));
        _controller = _root.AddComponent<MuseumBottleController>();
        Assert.That(_controller.Init(_repository, _inventory, _progress), Is.True);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    [Test]
    public void LoaderParsesBottleAndItemRows()
    {
        GameDataCsvFile bottles = new GameDataCsvFile("museum_bottles.csv",
            "bottle_id,chapter,display_name_key,completion_reward_item_id,completion_reward_amount\n"
            + "BOTTLE_CH1,1,bottle.ch1,REWARD_CH1,2\n");
        GameDataCsvFile items = new GameDataCsvFile("museum_bottle_items.csv",
            "bottle_id,item_order,alcohol_item_id,contribution_value\n"
            + "BOTTLE_CH1,0,ALCOHOL_A,3\n");
        Assert.That(MuseumBottleCsvDefinitionLoader.TryLoad(
            bottles, items, out var bottleRows, out var itemRows, out _), Is.True);
        Assert.That(bottleRows[0].RewardAmount, Is.EqualTo(2));
        Assert.That(itemRows[0].ContributionValue, Is.EqualTo(3));
    }

    [Test]
    public void RepositoryAcceptsHeaderOnlyPlanningData()
    {
        Assert.That(MuseumBottleDataRepository.TryCreate(
            new List<MuseumBottleDefinition>(), new List<MuseumBottleItemDefinition>(),
            new MuseumBottleTestReferences(), out MuseumBottleDataRepository repository, out _), Is.True);
        Assert.That(repository.BottleCount, Is.Zero);
    }

    [Test]
    public void RepositoryRejectsPartialBottleContent()
    {
        MuseumBottleTestReferences references = CreateReferences();
        Assert.That(MuseumBottleDataRepository.TryCreate(
            new[] { new MuseumBottleDefinition(BOTTLE, 1, "bottle.ch1", string.Empty, null) },
            new[] { new MuseumBottleItemDefinition(BOTTLE, 0, ALCOHOL_A, 1) },
            references, out _, out _), Is.False);
    }

    [Test]
    public void MissingOrWrongBottleAlcoholIsRejectedWithoutConsumption()
    {
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_A, out var missingFailure), Is.False);
        Assert.That(missingFailure, Is.EqualTo(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ITEM_NOT_OWNED));
        _inventory.SetCount("ALCOHOL_CH2", 1);
        Assert.That(_controller.TryContribute(BOTTLE, "ALCOHOL_CH2", out var wrongFailure), Is.False);
        Assert.That(wrongFailure, Is.EqualTo(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ITEM_NOT_ALLOWED));
        Assert.That(_inventory.ConsumeCalls, Is.Zero);
    }

    [Test]
    public void ContributionConsumesOnceAndCannotDuplicate()
    {
        _inventory.SetCount(ALCOHOL_A, 2);
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_A, out _), Is.True);
        Assert.That(_inventory.GetItemCount(ALCOHOL_A), Is.EqualTo(1));
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_A, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ALREADY_CONTRIBUTED));
        Assert.That(_controller.TryGetProgress(BOTTLE, out MuseumBottleProgress progress), Is.True);
        Assert.That(progress.FillRatio, Is.EqualTo(0.25f));
    }

    [Test]
    public void CompletionExchangesRewardOnceAndBlocksFurtherInput()
    {
        ContributeFirstItem();
        _inventory.SetCount(ALCOHOL_B, 1);
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_B, out _), Is.True);
        Assert.That(_inventory.GetItemCount(REWARD), Is.EqualTo(2));
        Assert.That(_inventory.ExchangeCalls, Is.EqualTo(1));
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_B, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_ALREADY_COMPLETED));
    }

    [Test]
    public void RewardFailureDoesNotConsumeAndCanRetry()
    {
        ContributeFirstItem();
        _inventory.SetCount(ALCOHOL_B, 1);
        _inventory.BlockReward = true;
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_B, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_REWARD_UNAVAILABLE));
        Assert.That(_inventory.GetItemCount(ALCOHOL_B), Is.EqualTo(1));
        _inventory.BlockReward = false;
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_B, out _), Is.True);
    }

    [Test]
    public void SavedProgressRestoresWithoutDuplicateReward()
    {
        ContributeFirstItem();
        Object.DestroyImmediate(_root);
        _root = new GameObject("ReloadedMuseumBottleController");
        _controller = _root.AddComponent<MuseumBottleController>();
        Assert.That(_controller.Init(_repository, _inventory, _progress), Is.True);
        Assert.That(_controller.IsContributed(BOTTLE, ALCOHOL_A), Is.True);
        Assert.That(_controller.TryGetProgress(BOTTLE, out MuseumBottleProgress progress), Is.True);
        Assert.That(progress.ContributedCount, Is.EqualTo(1));
    }

    [Test]
    public void ReentrantFastInputIsRejectedAsBusy()
    {
        _inventory.SetCount(ALCOHOL_A, 1);
        MuseumBottleContributionFailure nestedFailure = default;
        _inventory.OnBeforeConsume = () =>
            _controller.TryContribute(BOTTLE, ALCOHOL_A, out nestedFailure);
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_A, out _), Is.True);
        Assert.That(nestedFailure, Is.EqualTo(MuseumBottleContributionFailure.MUSEUM_BOTTLE_FAILURE_BUSY));
        Assert.That(_inventory.ConsumeCalls, Is.EqualTo(1));
    }

    private void ContributeFirstItem()
    {
        _inventory.SetCount(ALCOHOL_A, 1);
        Assert.That(_controller.TryContribute(BOTTLE, ALCOHOL_A, out _), Is.True);
    }

    private static MuseumBottleDataRepository CreateRepository()
    {
        MuseumBottleTestReferences references = CreateReferences();
        MuseumBottleDefinition[] bottles =
        {
            new MuseumBottleDefinition(BOTTLE, 1, "bottle.ch1", REWARD, 2),
            new MuseumBottleDefinition("BOTTLE_CH2", 2, "bottle.ch2", string.Empty, null),
            new MuseumBottleDefinition("BOTTLE_CH3", 3, "bottle.ch3", string.Empty, null)
        };
        MuseumBottleItemDefinition[] items =
        {
            new MuseumBottleItemDefinition(BOTTLE, 0, ALCOHOL_A, 1),
            new MuseumBottleItemDefinition(BOTTLE, 1, ALCOHOL_B, 3),
            new MuseumBottleItemDefinition("BOTTLE_CH2", 0, "ALCOHOL_CH2", 1),
            new MuseumBottleItemDefinition("BOTTLE_CH3", 0, "ALCOHOL_CH3", 1)
        };
        Assert.That(MuseumBottleDataRepository.TryCreate(
            bottles, items, references, out MuseumBottleDataRepository repository,
            out GameDataRepositoryBuildError error), Is.True, error?.ToString());
        return repository;
    }

    private static MuseumBottleTestReferences CreateReferences()
    {
        MuseumBottleTestReferences references = new MuseumBottleTestReferences();
        references.Add(ALCOHOL_A, 1); references.Add(ALCOHOL_B, 1);
        references.Add("ALCOHOL_CH2", 2); references.Add("ALCOHOL_CH3", 3);
        references.AddReward(REWARD);
        references.AddKey("bottle.ch1"); references.AddKey("bottle.ch2");
        references.AddKey("bottle.ch3");
        return references;
    }
}
