//=============================================================================
// File:        MuseumRewardRoomPhase7ValidationTests.cs
// Description: 실제 방 배치 검증 경로와 잠금 Collider·벽화 빛 효과를 검증한다.
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed partial class MuseumRewardRoomPhase7SceneTests
{
    [Test]
    public void LockedInputContextCannotBypassPlayerInputLock()
    {
        _room.Open();
        _player.SetCanInteract(false);
        _view.SetInteractionContext(_player, null);
        Assert.That(_view.TryInteract(out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INTERACTION_BLOCKED));
        Assert.That(_room.ClaimCalls, Is.Zero);
    }

    [Test]
    public void ClaimNotificationDoesNotStopMuralRevealParticle()
    {
        var effect = Child("MuralLight").AddComponent<ParticleSystem>();
        var main = effect.main;
        main.loop = true;
        Set(_mural, "_lightEffect", effect);
        _museum.IsRevealed = false;
        _view.Bind(_room, _museum);
        _room.Open();
        Assert.That(_mural.TryReveal(), Is.True);
        Assert.That(effect.isPlaying, Is.True);
        _view.SetInteractionContext(_player, null);
        Assert.That(_view.TryInteract(out _), Is.True);
        Assert.That(effect.isPlaying, Is.True, "보상 알림이 이미 진행 중인 벽화 연출을 중단하면 안 됩니다.");
        _room.Open();
        Assert.That(effect.isPlaying, Is.True);
        Assert.That(_museum.RevealCalls, Is.EqualTo(1));
    }

    [Test]
    public void DoorPhysicsBlocksBeforeOpeningAndClearsAfterOpening()
    {
        int layer = LayerMask.NameToLayer("MazeBlocker");
        Assert.That(layer, Is.GreaterThanOrEqualTo(0));
        _door.gameObject.layer = layer;
        Physics2D.SyncTransforms();
        Assert.That(Physics2D.Raycast(Vector2.left * 2, Vector2.right, 4, 1 << layer).collider, Is.SameAs(_door));
        _room.Open();
        Physics2D.SyncTransforms();
        Assert.That(Physics2D.Raycast(Vector2.left * 2, Vector2.right, 4, 1 << layer).collider, Is.Null);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void NonemptyRoomValidatorRejectsBrokenReferences(int invalidCase)
    {
        CreateRealServices(new MuseumBottleTestProgress());
        int chapter = invalidCase == 2 ? 2 : 3;
        var murals = new Dictionary<string, MuseumMuralDefinition>
        {
            { "TEST_MURAL_3", new MuseumMuralDefinition("TEST_MURAL_3", chapter, "TEST_TECH") }
        };
        if (invalidCase == 1) Set(_view, "_rewardMilestoneId", "TEST_TEN");
        if (invalidCase == 3) _door.gameObject.SetActive(false);
        if (invalidCase == 4) _mural.transform.position = new Vector3(30, 0, 0);
        if (invalidCase == 5) _reward.gameObject.SetActive(false);
        Assert.That(MuseumPhase7EditorTools.TryValidateRoom(_view, _rewards, murals, out string message),
            Is.EqualTo(invalidCase == 0), message);
    }

    [TestCase(8, 0, 256, true)]
    [TestCase(0, 0, 256, false)]
    [TestCase(8, 8, 256, false)]
    [TestCase(8, 0, 0, false)]
    public void LayerValidationUsesConfiguredPlayerMask(int pickup, int bounds, int mask, bool valid)
    {
        Assert.That(MuseumPhase7EditorTools.TryValidateInteractionLayers(pickup, bounds, mask, out _),
            Is.EqualTo(valid));
    }
}
