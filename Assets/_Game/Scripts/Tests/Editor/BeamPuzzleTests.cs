//=============================================================================
// File: BeamPuzzleTests.cs
// Description: 맵 Beam 부품과 실제 저장 경로의 상호작용·물리·복원 회귀를 검증한다.
// History: 10/06/2026 + 월드 직접 조작 요구로 기존 테스트 교체 -- Codex
//=============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

/// <summary>실제 제공 프리팹을 조합하되 사용자 맵과 사용자 저장은 변경하지 않는 수용 테스트입니다.</summary>
public sealed class BeamPuzzleTests
{
    private readonly List<Object> _objects = new List<Object>();
    private const BindingFlags PRIVATE = BindingFlags.NonPublic | BindingFlags.Instance;

    [OneTimeSetUp]
    public void PrepareAssets() => BeamPrefabBuilder.CreatePrefabs();

    [TearDown]
    public void TearDown()
    {
        for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
        _objects.Clear();
    }

    [TestCase(BeamPillarType.BEAM_PILLAR_START, 1, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_START, 0, false)]
    [TestCase(BeamPillarType.BEAM_PILLAR_TWO_WAY, 1, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_TWO_WAY, 2, false)]
    [TestCase(BeamPillarType.BEAM_PILLAR_FOUR_WAY, 3, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_FOUR_WAY, 2, false)]
    [TestCase(BeamPillarType.BEAM_PILLAR_EIGHT_WAY, 7, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_EIGHT_WAY, 6, false)]
    [TestCase(BeamPillarType.BEAM_PILLAR_END, 0, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_END, 1, false)]
    public void PillarTypeRequiresDefinedOutputCount(BeamPillarType type, int count, bool expected)
    {
        Assert.That(new BeamPillarSettings("P", type, true, 0, 45, new float[count]).TryValidate(out _), Is.EqualTo(expected));
    }

    [TestCase(float.NaN, 45f, 0f)]
    [TestCase(0f, 0f, 0f)]
    [TestCase(0f, float.PositiveInfinity, 0f)]
    [TestCase(0f, 45f, float.NaN)]
    public void InvalidAnglesAreRejected(float initial, float step, float output)
    {
        Assert.That(new BeamPillarSettings("P", BeamPillarType.BEAM_PILLAR_TWO_WAY, true, initial, step, output).TryValidate(out _), Is.False);
    }

    [Test]
    public void FirstPressRotatesAndLightsWithoutLockingPlayer()
    {
        Fixture f = CreateFixture();
        var player = New("Player");
        var interaction = player.AddComponent<PlayerInteractionController>();
        var movement = player.AddComponent<PlayerMoveController>();
        Assert.That(f.Controller.BeamActive, Is.False);
        Assert.That(Lines(f.Controller)[0].enabled, Is.False);
        f.Relay.Interact();
        Assert.That(f.Relay.RotationDegrees, Is.EqualTo(315f).Within(0.01));
        Assert.That(f.Controller.BeamActive, Is.True);
        Assert.That(f.Controller.State, Is.EqualTo(BeamPuzzleState.BEAM_PUZZLE_IN_PROGRESS));
        Assert.That(interaction.CanInteract, Is.True);
        Assert.That(Get<bool>(movement, "_canMove"), Is.True);
        Assert.That(f.Start.IsConnected, Is.True);
        Assert.That(f.Relay.IsConnected, Is.True);
    }

    [UnityTest]
    public IEnumerator ExistingInputActionRotatesOnceWhileHeldAndDetectsChildTrigger()
    {
        // 실제 플레이 루프에서 입력 액션을 확인합니다. Editor 입력 억제를 우회하는 런타임 수정은 하지 않습니다.
        yield return new EnterPlayMode();
        Fixture f = CreateFixture();
        GameObject player = New("InputPlayer");
        player.transform.position = f.Relay.transform.position + Vector3.down * 0.7f;
        var interaction = player.AddComponent<PlayerInteractionController>();
        var inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        _objects.Add(inputAsset);
        InputActionMap map = inputAsset.AddActionMap("BeamTest");
        InputAction action = map.AddAction("BeamTestInteraction", InputActionType.Button, "<Keyboard>/space");
        InputActionReference reference = InputActionReference.Create(action);
        _objects.Add(reference);
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        InputSettings.UpdateMode previousMode = InputSystem.settings.updateMode;
        InputSettings.EditorInputBehaviorInPlayMode previousBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSettings.BackgroundBehavior previousBackground = InputSystem.settings.backgroundBehavior;
        bool detected = false, pressed = false;
        string inputDiagnostic = string.Empty;
        float first = 0, held = 0, next = 0;
        try
        {
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Set(interaction, "_interactionLayers", (LayerMask)(1 << 8));
            Set(interaction, "_interactAction", reference);
            Call(interaction, "Awake");
            Call(interaction, "FindNearestInteractable");
            detected = ReferenceEquals(Get<IInteractable>(interaction, "_currentTarget"), f.Relay);
            action.Enable();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            UpdateTestInput();
            pressed = action.WasPressedThisFrame();
            inputDiagnostic = "playing=" + Application.isPlaying + ", key=" + keyboard.spaceKey.isPressed
                + ", enabled=" + keyboard.enabled + ", phase=" + action.phase + ", pressed=" + action.IsPressed()
                + ", update=" + InputState.currentUpdateType;
            Call(interaction, "Update");
            first = f.Relay.RotationDegrees;
            for (int i = 0; i < 3; i++) { UpdateTestInput(); Call(interaction, "Update"); }
            held = f.Relay.RotationDegrees;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            UpdateTestInput();
            Call(interaction, "Update");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            UpdateTestInput();
            Call(interaction, "Update");
            next = f.Relay.RotationDegrees;
        }
        finally
        {
            inputAsset.Disable();
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.updateMode = previousMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousBehavior;
            InputSystem.settings.backgroundBehavior = previousBackground;
        }
        yield return new ExitPlayMode();
        Assert.That(detected, Is.True, "실제 자식 Trigger 탐지");
        Assert.That(pressed, Is.True, "실제 플레이 모드 입력 액션: " + inputDiagnostic);
        Assert.That(first, Is.EqualTo(315f));
        Assert.That(held, Is.EqualTo(first));
        Assert.That(next, Is.EqualTo(270f));
    }
    [Test]
    public void FixedPillarDoesNotLightAndArrowsStayVisible()
    {
        Fixture f = CreateFixture();
        f.Start.Interact();
        Assert.That(f.Controller.BeamActive, Is.False);
        foreach (SpriteRenderer arrow in Get<SpriteRenderer[]>(f.Start, "_outputArrows")) Assert.That(arrow.enabled, Is.True);
    }

    [Test]
    public void PerPillarStepsAndArbitraryOutputsRotateTogether()
    {
        BeamPillarView pillar = Pillar("P", BeamPillarType.BEAM_PILLAR_FOUR_WAY, Vector2.zero, true, 12f, 17f, 3f, 47f, 191f);
        pillar.TryBind(null);
        MethodInfo rotate = typeof(BeamPillarView).GetMethod("RotateClockwise", PRIVATE);
        Assert.That(rotate.Invoke(pillar, new object[] { null }), Is.True);
        Assert.That(Mathf.DeltaAngle(0, pillar.GetOutputAngleDegrees(0)), Is.EqualTo(-2f).Within(0.01));
        Assert.That(Mathf.DeltaAngle(0, pillar.GetOutputAngleDegrees(1)), Is.EqualTo(42f).Within(0.01));
        Assert.That(Mathf.DeltaAngle(0, pillar.GetOutputAngleDegrees(2)), Is.EqualTo(-174f).Within(0.01));
        SpriteRenderer[] arrows = Get<SpriteRenderer[]>(pillar, "_outputArrows");
        Assert.That(Mathf.DeltaAngle(pillar.GetOutputAngleDegrees(1), arrows[1].transform.eulerAngles.z), Is.Zero.Within(0.01));
    }

    [Test]
    public void BranchingDuplicateConnectionsAndCyclesTerminate()
    {
        BeamPillarView start = Pillar("S", BeamPillarType.BEAM_PILLAR_START, Vector2.zero, false, 0, 45, 0, 90);
        BeamPillarView a = Pillar("A", BeamPillarType.BEAM_PILLAR_TWO_WAY, new Vector2(2, 0), false, 0, 45, 135);
        BeamPillarView b = Pillar("B", BeamPillarType.BEAM_PILLAR_TWO_WAY, new Vector2(0, 2), false, 0, 45, -45);
        BeamPillarView shared = Pillar("C", BeamPillarType.BEAM_PILLAR_TWO_WAY, new Vector2(1, 1), false, 0, 45, -45);
        BeamPillarView end = Pillar("E", BeamPillarType.BEAM_PILLAR_END, new Vector2(7, 7), false, 0, 45);
        BeamPropagation propagation = Propagation(start, a, b, shared, end);
        Assert.That(propagation.TryRecalculate(out bool goal), Is.True);
        Assert.That(goal, Is.False);
        Assert.That(propagation.SegmentCount, Is.EqualTo(5));
        Assert.That(propagation.IsConnected(3), Is.True);
    }

    [Test]
    public void NearestBodyBlocksButTriggersAndPlayerDoNot()
    {
        BeamPillarView start = Pillar("S", BeamPillarType.BEAM_PILLAR_START, Vector2.zero, false, 0, 45, 0);
        BeamPillarView end = Pillar("E", BeamPillarType.BEAM_PILLAR_END, new Vector2(4, 0), false, 0, 45);
        GameObject trigger = New("UnrelatedTrigger");
        trigger.layer = 9;
        trigger.transform.position = Vector2.right;
        trigger.AddComponent<BoxCollider2D>().isTrigger = true;
        GameObject player = New("PlayerBlocker");
        player.layer = 9;
        player.transform.position = Vector2.right * 2;
        player.AddComponent<BoxCollider2D>();
        player.AddComponent<PlayerInteractionController>();
        BeamPropagation propagation = Propagation(start, end);
        Assert.That(propagation.TryRecalculate(out bool goal), Is.True);
        Assert.That(goal, Is.True);
        GameObject obstacle = Prefab("BeamFixedObstacle");
        obstacle.transform.position = Vector2.right * 3;
        Physics2D.SyncTransforms();
        propagation.TryRecalculate(out goal);
        Assert.That(goal, Is.False);
        Assert.That(propagation.GetSegment(0).End.x, Is.EqualTo(2.5f).Within(0.01));
    }

    [Test]
    public void OtherPuzzlePillarBlocksWithoutRelaying()
    {
        BeamPillarView start = Pillar("S", BeamPillarType.BEAM_PILLAR_START, Vector2.zero, false, 0, 45, 0);
        BeamPillarView end = Pillar("E", BeamPillarType.BEAM_PILLAR_END, new Vector2(4, 0), false, 0, 45);
        BeamPillarView foreign = Pillar("Foreign", BeamPillarType.BEAM_PILLAR_TWO_WAY, new Vector2(2, 0), false, 0, 45, 0);
        foreign.TryBind(null);
        BeamPropagation propagation = Propagation(start, end);
        propagation.TryRecalculate(out bool goal);
        Assert.That(goal, Is.False);
        Assert.That(foreign.IsConnected, Is.False);
    }

    [Test]
    public void StoneMovementAndWallBreakRefreshWithoutPressingAgain()
    {
        Fixture f = CreateFixture(initialRelay: 45);
        f.Stone.RestoreWorldPose(new Vector2(3, 0), 0);
        f.Wall.RestoreBeamState(false);
        f.Wall.transform.position = new Vector2(3, 0);
        f.Relay.Interact();
        Assert.That(f.Controller.IsClear, Is.False);
        f.Stone.RestoreWorldPose(new Vector2(3, 2), 0);
        f.Controller.RefreshLivePath();
        Assert.That(f.Controller.IsClear, Is.False);
        f.Wall.RestoreBeamState(true);
        f.Controller.RefreshLivePath();
        Assert.That(f.Controller.IsClear, Is.True);
        Assert.That(f.Rewards, Is.EqualTo(1));
    }

    [Test]
    public void ResetRestoresPillarStoneWallAndSavesExplicitWaitingState()
    {
        Fixture f = CreateFixture();
        f.Relay.Interact();
        f.Stone.RestoreWorldPose(new Vector2(6, 5), 37);
        f.Wall.RestoreBeamState(true);
        string old = f.Controller.CaptureState();
        Assert.That(f.Controller.TryResetPuzzle(), Is.True);
        Assert.That(f.Controller.BeamActive, Is.False);
        Assert.That(f.Relay.RotationDegrees, Is.EqualTo(0));
        Assert.That((Vector2)f.Stone.transform.position, Is.EqualTo(new Vector2(2, 2)));
        Assert.That(f.Stone.transform.eulerAngles.z, Is.Zero.Within(0.01));
        Assert.That(f.Wall.IsBroken, Is.False);
        Assert.That(f.Wall.BodyCollider.enabled, Is.True);
        Assert.That(f.Relay.IsConnected, Is.False);
        string reset = f.Controller.CaptureState();
        Assert.That(reset, Is.Not.Empty.And.Not.EqualTo(old));
        f.Controller.ApplyState(old);
        f.Controller.ApplyState(reset);
        Assert.That(f.Controller.State, Is.EqualTo(BeamPuzzleState.BEAM_PUZZLE_WAITING));
        Assert.That(f.Controller.BeamActive, Is.False);
    }

    [Test]
    public void ResetPadActsOncePerEntryIncludingMultiplePlayerColliders()
    {
        Fixture f = CreateFixture();
        var player = New("Player");
        player.AddComponent<PlayerInteractionController>();
        Collider2D body = player.AddComponent<BoxCollider2D>();
        Collider2D second = player.AddComponent<CircleCollider2D>();
        f.Relay.Interact();
        Call(f.Pad, "OnTriggerEnter2D", body);
        Assert.That(f.Controller.BeamActive, Is.False);
        f.Relay.Interact();
        Call(f.Pad, "OnTriggerEnter2D", second);
        Assert.That(f.Controller.BeamActive, Is.True);
        Call(f.Pad, "OnTriggerExit2D", body);
        Call(f.Pad, "OnTriggerExit2D", second);
        Call(f.Pad, "OnTriggerEnter2D", body);
        Assert.That(f.Controller.BeamActive, Is.False);
    }

    [Test]
    public void RestoreUsesIdsAfterArrayReorderingAndPreservesOriginalBaseline()
    {
        Fixture source = CreateFixture();
        source.Relay.Interact();
        source.Stone.RestoreWorldPose(new Vector2(6, 5), 37);
        source.Wall.RestoreBeamState(true);
        string json = source.Controller.CaptureState();
        TearDown();
        Fixture target = CreateFixture(initialize: false);
        Set(target.Controller, "_pillars", new[] { target.End, target.Relay, target.Start });
        target.Controller.ApplyState(json);
        Assert.That(target.Relay.RotationDegrees, Is.EqualTo(315));
        Assert.That((Vector2)target.Stone.transform.position, Is.EqualTo(new Vector2(6, 5)));
        Assert.That(target.Wall.IsBroken, Is.True);
        Assert.That(target.Controller.BeamActive, Is.True);
        Call(target.Stone, "Awake");
        Call(target.Wall, "Awake");
        target.Controller.InitializePuzzle();
        Assert.That(target.Controller.TryResetPuzzle(), Is.True);
        Assert.That((Vector2)target.Stone.transform.position, Is.EqualTo(new Vector2(2, 2)));
        Assert.That(target.Wall.IsBroken, Is.False);
        Assert.That(target.Rewards, Is.Zero);
    }

    [Test]
    public void CompletionKeepsLinesOpensExitAndLocksAllPartsWithoutDisablingPillarBodies()
    {
        Fixture f = CreateFixture(initialRelay: 45);
        Set(f.Stone, "_lockWhenPuzzleCompleted", false);
        f.Relay.Interact();
        Assert.That(f.Controller.IsClear, Is.True);
        Assert.That(f.Exit.activeSelf, Is.False);
        Assert.That(f.Controller.TryRotatePillar(f.Relay), Is.False);
        Assert.That(f.Controller.TryResetPuzzle(), Is.False);
        Assert.That(f.Relay.BodyCollider.enabled, Is.True);
        Assert.That(f.Relay.PillarCollider.enabled, Is.True);
        Assert.That(Lines(f.Controller)[0].enabled, Is.True);
        Assert.That(f.Stone.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Static));
        Assert.That(Get<bool>(f.Stone, "_isPuzzleCompleted"), Is.True);
        Assert.That(Get<bool>(f.Wall, "_isPuzzleCompleted"), Is.True);
        Assert.That(f.Stone.TryResetToInitialPosition(), Is.False);
        f.Relay.Interact();
        f.Controller.CheckClearCondition();
        Assert.That(f.Rewards, Is.EqualTo(1));
    }

    [Test]
    public void ExistingSkillAcquisitionGatesStoneAndWallAndCompletionCancelsBoth()
    {
        Fixture f = CreateFixture();
        var progress = New("SkillProgress").AddComponent<ProgressController>();
        var skills = New("Skills").AddComponent<SkillController>();
        SkillDataSO push = Get<SkillDataSO>(f.Stone, "_requiredSkill");
        SkillDataSO breaking = Get<SkillDataSO>(f.Wall, "_requiredSkill");
        Set(skills, "_skills", new[] { push, breaking });
        Assert.That(skills.Init(progress), Is.True);
        Set(f.Stone, "_skillService", skills);
        Set(f.Wall, "_skillService", skills);
        GameObject player = New("SkillPlayer");
        player.SetActive(false);
        player.AddComponent<Rigidbody2D>();
        var interaction = player.AddComponent<PlayerInteractionController>();
        var move = player.AddComponent<PlayerMoveController>();
        Set(move, "_player", player);
        f.Stone.SetInteractionContext(interaction, move);
        f.Wall.SetInteractionContext(interaction, move);
        f.Stone.OnHoldStarted();
        f.Wall.Interact();
        Assert.That(Get<bool>(f.Stone, "_isHolding"), Is.False);
        Assert.That(Get<bool>(f.Wall, "_isPreparing"), Is.False);
        Assert.That(skills.TryAcquireSkill(push.SkillId, out _), Is.True);
        Assert.That(skills.TryAcquireSkill(breaking.SkillId, out _), Is.True);
        f.Stone.OnHoldStarted();
        f.Wall.Interact();
        Assert.That(Get<bool>(f.Stone, "_isHolding"), Is.True);
        Assert.That(Get<bool>(f.Wall, "_isPreparing"), Is.True);
        Set(move, "_moveDirection", Vector3.right);
        Call(f.Wall, "Update");
        Assert.That(Get<bool>(f.Wall, "_isPreparing"), Is.False);
        Set(move, "_moveDirection", Vector3.zero);
        f.Wall.Interact();
        Set(f.Wall, "_prepareElapsed", 1f);
        Call(f.Wall, "Update");
        Assert.That(f.Wall.IsBroken, Is.True);
        f.Controller.ApplyState("true");
        Assert.That(Get<bool>(f.Stone, "_isHolding"), Is.False);
        f.Stone.OnHoldStarted();
        f.Wall.Interact();
        Assert.That(Get<bool>(f.Stone, "_isHolding"), Is.False);
        Assert.That(Get<bool>(f.Wall, "_isPreparing"), Is.False);
    }

    [Test]
    public void CompletedRestoreKeepsExactFinalSegmentsWithoutRepeatingEvents()
    {
        Fixture f = CreateFixture(initialRelay: 45);
        f.Relay.Interact();
        string json = f.Controller.CaptureState();
        Vector3 end = Lines(f.Controller)[0].GetPosition(1);
        TearDown();
        Fixture restored = CreateFixture(initialize: false);
        restored.Controller.ApplyState(json);
        restored.Controller.ApplyState(json);
        Assert.That(restored.Controller.IsClear, Is.True);
        Assert.That(restored.Exit.activeSelf, Is.False);
        Assert.That(restored.Rewards, Is.Zero);
        Assert.That(Lines(restored.Controller)[0].GetPosition(1), Is.EqualTo(end));
        Assert.That(Lines(restored.Controller)[0].enabled, Is.True);
    }

    [Test]
    public void OpeningExitDoesNotRecalculateFinalBranchAndRestoreKeepsIt()
    {
        Fixture f = CreateFixture(initialRelay: 45, initialize: false);
        f.Start.Configure(new BeamPillarSettings("START", BeamPillarType.BEAM_PILLAR_START, false, 0, 45, 0, 90));
        SpriteRenderer first = Get<SpriteRenderer[]>(f.Start, "_outputArrows")[0];
        SpriteRenderer second = Object.Instantiate(first, first.transform.parent);
        Set(f.Start, "_outputArrows", new[] { first, second });
        f.Exit.transform.position = new Vector2(0, 2);
        f.Controller.InitializePuzzle();
        f.Relay.Interact();
        Assert.That(f.Controller.IsClear, Is.True);
        Vector3 finalBranch = Lines(f.Controller)[1].GetPosition(1);
        Assert.That(finalBranch.y, Is.EqualTo(1.5f).Within(0.01));
        f.Controller.RefreshLivePath();
        Assert.That(Lines(f.Controller)[1].GetPosition(1), Is.EqualTo(finalBranch));
        string json = f.Controller.CaptureState();
        f.Controller.ApplyState(json);
        Assert.That(Lines(f.Controller)[1].GetPosition(1), Is.EqualTo(finalBranch));
        Assert.That(f.Rewards, Is.EqualTo(1));
    }

    [Test]
    public void AllPartArraysRestoreByIdIncludingMultipleStonesAndWalls()
    {
        Fixture f = CreateFixture(initialize: false);
        var stoneB = Prefab("BeamPushableStone").GetComponent<PushableStone>();
        stoneB.transform.position = new Vector2(7, 3);
        Set(stoneB, "_objectId", "STONE_B");
        var wallB = Prefab("BeamBreakableWall").GetComponent<BreakableWall>();
        wallB.transform.position = new Vector2(8, 2);
        Set(wallB, "_objectId", "WALL_B");
        Set(f.Controller, "_stones", new[] { f.Stone, stoneB });
        Set(f.Controller, "_walls", new[] { f.Wall, wallB });
        f.Controller.InitializePuzzle();
        f.Relay.Interact();
        f.Stone.RestoreWorldPose(new Vector2(6, 5), 37);
        stoneB.RestoreWorldPose(new Vector2(7, 6), 71);
        f.Wall.RestoreBeamState(true);
        string json = f.Controller.CaptureState();
        Set(f.Controller, "_pillars", new[] { f.End, f.Start, f.Relay });
        Set(f.Controller, "_stones", new[] { stoneB, f.Stone });
        Set(f.Controller, "_walls", new[] { wallB, f.Wall });
        f.Controller.TryResetPuzzle();
        f.Controller.ApplyState(json);
        Assert.That((Vector2)f.Stone.transform.position, Is.EqualTo(new Vector2(6, 5)));
        Assert.That((Vector2)stoneB.transform.position, Is.EqualTo(new Vector2(7, 6)));
        Assert.That(f.Stone.transform.eulerAngles.z, Is.EqualTo(37).Within(0.01));
        Assert.That(stoneB.transform.eulerAngles.z, Is.EqualTo(71).Within(0.01));
        Assert.That(f.Wall.IsBroken, Is.True);
        Assert.That(wallB.IsBroken, Is.False);
        Assert.That(f.Relay.RotationDegrees, Is.EqualTo(315));
        Assert.That(f.Relay.IsConnected, Is.True);
        Assert.That(f.End.IsConnected, Is.False);
    }

    [Test]
    public void ActualStageCaptureAndRestoreEventsPreserveProgressAndReset()
    {
        Fixture f = CreateFixture();
        var progress = New("StageProgress").AddComponent<ProgressController>();
        var stage = New("StageFlow").AddComponent<StageFlowController>();
        Set(stage, "_currentStageNumber", 23);
        var bridge = New("Bridge").AddComponent<SceneStateBridge>();
        bridge.Init(stage, progress);
        f.Relay.Interact();
        f.Wall.RestoreBeamState(true);
        f.Stone.RestoreWorldPose(new Vector2(6, 5), 37);
        Call(stage, "ExecuteStageCapture");
        f.Controller.TryResetPuzzle();
        // 벽이 관리자보다 먼저 발견되어도 Beam의 전체 상태가 유일한 복원 소유자입니다.
        f.Wall.transform.SetAsFirstSibling();
        progress.SetObjectState(23, f.Wall.ObjectId, "false");
        Call(stage, "ExecuteDataRestoration", new SceneTransferData(), 23);
        Assert.That(f.Relay.RotationDegrees, Is.EqualTo(315));
        Assert.That(f.Wall.IsBroken, Is.True);
        Assert.That((Vector2)f.Stone.transform.position, Is.EqualTo(new Vector2(6, 5)));
        Assert.That(f.Rewards, Is.Zero);
        f.Controller.TryResetPuzzle();
        Call(stage, "ExecuteStageCapture");
        f.Relay.Interact();
        Call(stage, "ExecuteDataRestoration", new SceneTransferData(), 23);
        Assert.That(f.Controller.BeamActive, Is.False);
        Assert.That(f.Wall.IsBroken, Is.False);
    }

    [Test]
    public void LegacyTrueRestoresCompletionWithoutRewards()
    {
        Fixture f = CreateFixture(initialize: false);
        f.Controller.ApplyState("true");
        Assert.That(f.Controller.IsClear, Is.True);
        Assert.That(f.Exit.activeSelf, Is.False);
        Assert.That(f.Rewards, Is.Zero);
        Assert.That(f.Controller.CaptureState(), Does.Contain("\"Version\":1"));
    }

    [Test]
    public void SeparateParentsAndTwoPuzzlesOperateIndependently()
    {
        Fixture first = CreateFixture();
        Fixture second = CreateFixture(offset: new Vector2(20, 0));
        GameObject parentA = New("ParentA");
        GameObject parentB = New("ParentB");
        parentA.transform.rotation = Quaternion.Euler(0, 0, 73);
        first.Start.transform.SetParent(parentA.transform, true);
        first.Relay.transform.SetParent(parentB.transform, true);
        first.Relay.Interact();
        Assert.That(first.Controller.BeamActive, Is.True);
        Assert.That(second.Controller.BeamActive, Is.False);
        Assert.That(second.Relay.RotationDegrees, Is.Zero);
        Assert.That(first.Controller.TryRotatePillar(second.Relay), Is.False);
        first.Controller.TryResetPuzzle();
        second.Relay.Interact();
        Assert.That(first.Controller.BeamActive, Is.False);
        Assert.That(second.Controller.BeamActive, Is.True);
    }

    [Test]
    public void ValidatorRejectsSharedIdsPartsLayerMaskAndExitParent()
    {
        Fixture f = CreateFixture();
        Fixture other = CreateFixture(offset: new Vector2(20, 0));
        Set(other.Controller, "_objectId", f.Controller.ObjectId);
        Set(other.Controller, "_pillars", new[] { f.Start, other.Relay, other.End });
        Set(f.Controller, "_beamCollisionMask", (LayerMask)0);
        Set(f.Controller, "_exitObstacles", new[] { f.Controller.gameObject });
        var issues = new List<string>();
        f.Controller.CollectValidationIssues(issues);
        Assert.That(string.Join(" / ", issues), Does.Contain("중복 저장 ID").And.Contain("공유 등록").And.Contain("부모").And.Contain("LayerMask"));
    }

    [Test]
    public void AllActualPrefabsHaveSpritesScriptsMaterialsAndDetectableColliders()
    {
        string[] names = { "BeamStartPillar", "BeamTwoWayPillar", "BeamFourWayPillar", "BeamEightWayPillar", "BeamEndPillar" };
        foreach (string name in names)
        {
            var pillar = Prefab(name).GetComponent<BeamPillarView>();
            Assert.That(pillar.TryValidate(out string error), Is.True, name + ": " + error);
            Assert.That(pillar.BodyCollider.isTrigger, Is.False);
            Assert.That(pillar.InteractionTrigger.attachedRigidbody.GetComponent<IInteractable>(), Is.SameAs(pillar));
            foreach (var renderer in pillar.GetComponentsInChildren<SpriteRenderer>())
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        }
        Assert.That(Prefab("BeamLine").GetComponent<LineRenderer>().sharedMaterial, Is.Not.Null);
        Assert.That(PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadAssetAtPath<GameObject>(BeamPrefabBuilder.GetPrefabPath("BeamPushableStone"))), Is.EqualTo(PrefabAssetType.Variant));
        Assert.That(PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadAssetAtPath<GameObject>(BeamPrefabBuilder.GetPrefabPath("BeamBreakableWall"))), Is.EqualTo(PrefabAssetType.Variant));
    }

    [UnityTest]
    public IEnumerator RealSaveLoadCapturesLatestProgressAndResetAndIgnoresOldIndependentWallState()
    {
        Fixture f = CreateFixture();
        var progress = New("Progress").AddComponent<ProgressController>();
        var stage = New("StageFlow").AddComponent<StageFlowController>();
        Set(stage, "_currentStageNumber", 777);
        var bridge = New("SceneBridge").AddComponent<SceneStateBridge>();
        bridge.Init(stage, progress);
        string path = Path.GetFullPath("Logs/BeamValidation/save-" + Guid.NewGuid().ToString("N") + ".es3");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Set(progress, "_saveFilePath", path);
        f.Relay.Interact();
        f.Stone.RestoreWorldPose(new Vector2(6, 5), 37);
        f.Wall.RestoreBeamState(true);
        yield return progress.SaveAsync().ToCoroutine(saved => Assert.That(saved, Is.True));
        Assert.That(progress.TryGetObjectState(777, f.Controller.ObjectId, out string savedJson), Is.True);
        Assert.That(savedJson, Does.Contain("315"));
        f.Controller.TryResetPuzzle();
        yield return progress.LoadAsync().ToCoroutine(loaded => Assert.That(loaded, Is.True));
        Assert.That(f.Relay.RotationDegrees, Is.EqualTo(315));
        Assert.That(f.Wall.IsBroken, Is.True);
        Assert.That(f.Controller.TryResetPuzzle(), Is.True);
        progress.SetObjectState(777, f.Wall.ObjectId, "true");
        yield return progress.SaveAsync().ToCoroutine(saved => Assert.That(saved, Is.True));
        f.Relay.Interact();
        yield return progress.LoadAsync().ToCoroutine(loaded => Assert.That(loaded, Is.True));
        Assert.That(f.Controller.BeamActive, Is.False);
        Assert.That(f.Wall.IsBroken, Is.False);
        Assert.That(f.Rewards, Is.Zero);
        Assert.That(f.Wall.CaptureState(), Is.Empty);
        Assert.That(File.Exists(path), Is.True);
        File.Delete(path);
    }

    // 테스트 동안만 포커스 정책과 Manual 모드를 설정하고 원래 입력 설정을 복원합니다.
    private static void UpdateTestInput()
    {
        InputSystem.Update();
    }
    private Fixture CreateFixture(float initialRelay = 0, Vector2 offset = default, bool initialize = true)
    {
        var f = new Fixture();
        f.Controller = Prefab("BeamPuzzleController").GetComponent<BeamPuzzleController>();
        f.Start = Pillar("START", BeamPillarType.BEAM_PILLAR_START, offset, false, 0, 45, 0);
        f.Relay = Pillar("RELAY", BeamPillarType.BEAM_PILLAR_TWO_WAY, offset + new Vector2(2, 0), true, initialRelay, 45, 0);
        f.End = Pillar("END", BeamPillarType.BEAM_PILLAR_END, offset + new Vector2(4, 0), false, 0, 45);
        f.Pad = Prefab("BeamResetPad").GetComponent<BeamResetPad>();
        f.Pad.transform.position = offset + new Vector2(-3, -3);
        f.Stone = Prefab("BeamPushableStone").GetComponent<PushableStone>();
        f.Stone.transform.position = offset + new Vector2(2, 2);
        f.Wall = Prefab("BeamBreakableWall").GetComponent<BreakableWall>();
        f.Wall.transform.position = offset + new Vector2(3, 2);
        f.Exit = Prefab("BeamExitObstacle");
        f.Exit.transform.position = offset + new Vector2(8, 3);
        Set(f.Stone, "_objectId", "STONE");
        Set(f.Wall, "_objectId", "WALL");
        Set(f.Controller, "_objectId", "BEAM_" + Guid.NewGuid().ToString("N"));
        Set(f.Controller, "_pillars", new[] { f.Start, f.Relay, f.End });
        Set(f.Controller, "_resetPads", new[] { f.Pad });
        Set(f.Controller, "_stones", new[] { f.Stone });
        Set(f.Controller, "_walls", new[] { f.Wall });
        Set(f.Controller, "_exitObstacles", new[] { f.Exit });
        var solved = new UnityEvent();
        solved.AddListener(() => f.Rewards++);
        Set(f.Controller, "_onSolved", solved);
        if (initialize) f.Controller.InitializePuzzle();
        Physics2D.SyncTransforms();
        return f;
    }
    private BeamPillarView Pillar(string id, BeamPillarType type, Vector2 position, bool rotatable, float initial, float step, params float[] angles)
    {
        string name = new[] { "BeamStartPillar", "BeamTwoWayPillar", "BeamFourWayPillar", "BeamEightWayPillar", "BeamEndPillar" }[(int)type];
        BeamPillarView pillar = Prefab(name).GetComponent<BeamPillarView>();
        pillar.transform.position = position;
        pillar.Configure(new BeamPillarSettings(id, type, rotatable, initial, step, angles));
        return pillar;
    }
    private BeamPropagation Propagation(params BeamPillarView[] pillars)
    {
        foreach (var pillar in pillars) pillar.TryBind(null);
        Physics2D.SyncTransforms();
        return new BeamPropagation(pillars, (1 << 8) | (1 << 9), 10);
    }
    private GameObject Prefab(string name)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(BeamPrefabBuilder.GetPrefabPath(name));
        Assert.That(source, Is.Not.Null, name);
        GameObject clone = Object.Instantiate(source);
        _objects.Add(clone);
        return clone;
    }
    private GameObject New(string name) { var go = new GameObject(name); _objects.Add(go); return go; }
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, PRIVATE).SetValue(target, value);
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, PRIVATE).GetValue(target);
    private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, PRIVATE).Invoke(target, args);
    private static LineRenderer[] Lines(BeamPuzzleController controller) => Get<LineRenderer[]>(controller, "_beamViews");
    private sealed class Fixture
    {
        internal BeamPuzzleController Controller;
        internal BeamPillarView Start, Relay, End;
        internal BeamResetPad Pad;
        internal PushableStone Stone;
        internal BreakableWall Wall;
        internal GameObject Exit;
        internal int Rewards;
    }
}
