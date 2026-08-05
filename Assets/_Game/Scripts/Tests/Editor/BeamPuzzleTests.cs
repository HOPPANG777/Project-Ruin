//=============================================================================
// File:        BeamPuzzleTests.cs
//
// Description: 빛줄기 기둥 데이터와 반복형 광전파의 수용 기준을 검증한다.
//
// History:     08/04/2026 + Created -- Codex
//
//=============================================================================

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;

//=============================================================================
//
// Synopsis:   BeamPuzzleTests;
//                - 종류별 출력 검증과 장애물, 순환, 다중 진입 경로 회귀
//=============================================================================
[TestFixture]
public sealed class BeamPuzzleTests
{
    private const int PILLAR_LAYER = 8;
    private const int OBSTACLE_LAYER = 9;
    private const float MAX_DISTANCE = 10f;
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
        {
            Object.DestroyImmediate(_createdObjects[i]);
        }
        _createdObjects.Clear();
    }

    [TestCase(BeamPillarType.BEAM_PILLAR_TWO_WAY, 1, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_TWO_WAY, 2, false)]
    [TestCase(BeamPillarType.BEAM_PILLAR_FOUR_WAY, 3, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_FOUR_WAY, 2, false)]
    [TestCase(BeamPillarType.BEAM_PILLAR_EIGHT_WAY, 7, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_EIGHT_WAY, 6, false)]
    [TestCase(BeamPillarType.BEAM_PILLAR_END, 0, true)]
    [TestCase(BeamPillarType.BEAM_PILLAR_END, 1, false)]
    public void PillarTypeRequiresDefinedOutputCount(
        BeamPillarType pillarType, int outputCount, bool expected)
    {
        BeamPillarView pillar = CreatePillar("P", pillarType, Vector2.zero,
            new float[outputCount]);
        Assert.That(ValidatePillar(pillar), Is.EqualTo(expected));
    }

    [Test]
    public void InvalidOutputAngleIsRejected()
    {
        BeamPillarView pillar = CreatePillar("P", BeamPillarType.BEAM_PILLAR_TWO_WAY,
            Vector2.zero, new[] { float.NaN });
        Assert.That(ValidatePillar(pillar), Is.False);
    }

    [Test]
    public void DirectRayConnectsGoal()
    {
        BeamPillarView start = CreatePillar("START", BeamPillarType.BEAM_PILLAR_START,
            Vector2.zero, new[] { 0f });
        BeamPillarView end = CreatePillar("END", BeamPillarType.BEAM_PILLAR_END,
            new Vector2(3f, 0f), new float[0]);
        BeamPropagation propagation = CreatePropagation(start, end);

        Assert.That(propagation.TryRecalculate(out bool goalReached), Is.True);
        Assert.That(goalReached, Is.True);
    }

    [Test]
    public void ObstacleBlocksGoalBehindIt()
    {
        BeamPillarView start = CreatePillar("START", BeamPillarType.BEAM_PILLAR_START,
            Vector2.zero, new[] { 0f });
        BeamPillarView end = CreatePillar("END", BeamPillarType.BEAM_PILLAR_END,
            new Vector2(3f, 0f), new float[0]);
        GameObject obstacle = new GameObject("Obstacle", typeof(BoxCollider2D));
        _createdObjects.Add(obstacle);
        obstacle.layer = OBSTACLE_LAYER;
        obstacle.transform.position = new Vector2(1.5f, 0f);
        BeamPropagation propagation = CreatePropagation(start, end);

        Assert.That(propagation.TryRecalculate(out bool goalReached), Is.True);
        Assert.That(goalReached, Is.False);
    }

    [Test]
    public void CyclicPathTerminatesAfterEachPillarPropagatesOnce()
    {
        BeamPillarView start = CreatePillar("START", BeamPillarType.BEAM_PILLAR_START,
            Vector2.zero, new[] { 0f });
        BeamPillarView a = CreatePillar("A", BeamPillarType.BEAM_PILLAR_TWO_WAY,
            new Vector2(2f, 0f), new[] { 90f });
        BeamPillarView b = CreatePillar("B", BeamPillarType.BEAM_PILLAR_TWO_WAY,
            new Vector2(2f, 2f), new[] { -90f });
        BeamPillarView end = CreatePillar("END", BeamPillarType.BEAM_PILLAR_END,
            new Vector2(-4f, 0f), new float[0]);
        BeamPropagation propagation = CreatePropagation(start, a, b, end);

        Assert.That(propagation.TryRecalculate(out _), Is.True);
        Assert.That(propagation.SegmentCount, Is.EqualTo(3));
    }

    [Test]
    public void MultipleInputsPropagateSharedPillarOnlyOnce()
    {
        BeamPillarView start = CreatePillar("START", BeamPillarType.BEAM_PILLAR_START,
            Vector2.zero, new[] { 0f, 90f });
        BeamPillarView a = CreatePillar("A", BeamPillarType.BEAM_PILLAR_TWO_WAY,
            new Vector2(2f, 0f), new[] { 135f });
        BeamPillarView b = CreatePillar("B", BeamPillarType.BEAM_PILLAR_TWO_WAY,
            new Vector2(0f, 2f), new[] { -45f });
        BeamPillarView shared = CreatePillar("SHARED", BeamPillarType.BEAM_PILLAR_TWO_WAY,
            new Vector2(1f, 1f), new[] { 0f });
        BeamPillarView end = CreatePillar("END", BeamPillarType.BEAM_PILLAR_END,
            new Vector2(-4f, -4f), new float[0]);
        BeamPropagation propagation = CreatePropagation(start, a, b, shared, end);

        Assert.That(propagation.TryRecalculate(out _), Is.True);
        Assert.That(propagation.SegmentCount, Is.EqualTo(5));
    }

    [Test]
    public void ClockwiseRotationImmediatelyChangesPath()
    {
        BeamPillarView start = CreatePillar("START", BeamPillarType.BEAM_PILLAR_START,
            Vector2.zero, new[] { 0f }, true);
        BeamPillarView end = CreatePillar("END", BeamPillarType.BEAM_PILLAR_END,
            new Vector2(3f, 0f), new float[0]);
        BeamPropagation propagation = CreatePropagation(start, end);
        Assert.That(propagation.TryRecalculate(out bool beforeRotation), Is.True);

        Assert.That(start.TryRotateClockwise(), Is.True);
        Physics2D.SyncTransforms();
        Assert.That(propagation.TryRecalculate(out bool afterRotation), Is.True);
        Assert.That(afterRotation, Is.False);
    }

    [Test]
    public void CloseResetsRotationAndRestoresPlayerInput()
    {
        ControllerFixture fixture = CreateControllerFixture(false, true);
        fixture.Controller.Interact();
        Assert.That(GetField<bool>(fixture.Move, "_canMove"), Is.False);
        Assert.That(fixture.Controller.TryRotatePillar(0), Is.True);

        fixture.Popup.ClosePopup();

        Assert.That(GetField<bool>(fixture.Move, "_canMove"), Is.True);
        Assert.That(GetField<bool>(fixture.Interaction, "_canInteract"), Is.True);
        Assert.That(Mathf.DeltaAngle(0f, fixture.Start.transform.localEulerAngles.z), Is.Zero);
    }

    [Test]
    public void CompletionClosesAndRewardsExactlyOnce()
    {
        ControllerFixture fixture = CreateControllerFixture(true, false);

        fixture.Controller.Interact();
        fixture.Controller.Interact();

        Assert.That(fixture.Controller.State,
            Is.EqualTo(BeamPuzzleState.BEAM_PUZZLE_COMPLETED));
        Assert.That(fixture.RewardCount, Is.EqualTo(1));
        Assert.That(GetField<bool>(fixture.Move, "_canMove"), Is.True);
    }

    [Test]
    public void RestoredCompletionDoesNotReward()
    {
        ControllerFixture fixture = CreateControllerFixture(false, false);

        fixture.Controller.ApplyState("true");

        Assert.That(fixture.Controller.State,
            Is.EqualTo(BeamPuzzleState.BEAM_PUZZLE_COMPLETED));
        Assert.That(fixture.RewardCount, Is.Zero);
    }

    [Test]
    public void InitializationFailureLeavesPlayerInputEnabled()
    {
        ControllerFixture fixture = CreateControllerFixture(false, false);
        SetField(fixture.Controller, "_maxBeamDistance", 0f);

        LogAssert.Expect(LogType.Error,
            "[BeamPuzzleController] 퍼즐 데이터를 사용할 수 없습니다. 필수 ID, 참조 또는 최대 빛 거리가 올바르지 않습니다.");
        fixture.Controller.Interact();

        Assert.That(GetField<bool>(fixture.Move, "_canMove"), Is.True);
        Assert.That(GetField<bool>(fixture.Interaction, "_canInteract"), Is.True);
    }

    private BeamPropagation CreatePropagation(params BeamPillarView[] pillars)
    {
        for (int i = 0; i < pillars.Length; i++)
        {
            pillars[i].Initialize(null, i);
        }
        Physics2D.SyncTransforms();
        return new BeamPropagation(pillars, 1 << PILLAR_LAYER,
            1 << OBSTACLE_LAYER, MAX_DISTANCE);
    }

    private BeamPillarView CreatePillar(string id, BeamPillarType type,
        Vector2 position, float[] angles, bool isRotatable = false)
    {
        GameObject pillarObject = new GameObject(id, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(CircleCollider2D),
            typeof(BeamPillarView));
        _createdObjects.Add(pillarObject);
        pillarObject.layer = PILLAR_LAYER;
        pillarObject.transform.position = position;
        CircleCollider2D collider = pillarObject.GetComponent<CircleCollider2D>();
        collider.radius = 0.25f;
        BeamPillarView pillar = pillarObject.GetComponent<BeamPillarView>();
        SetField(pillar, "_pillarId", id);
        SetField(pillar, "_pillarType", type);
        SetField(pillar, "_isRotatable", isRotatable);
        SetField(pillar, "_outputAnglesDegrees", angles);
        SetField(pillar, "_pillarImage", pillarObject.GetComponent<Image>());
        return pillar;
    }

    private ControllerFixture CreateControllerFixture(bool directGoal, bool rotatable)
    {
        ControllerFixture fixture = new ControllerFixture();
        GameObject popupHost = CreateObject("PopupHost", false);
        GameObject controllerObject = CreateObject("BeamController", false);
        GameObject popupContainer = CreateObject("PopupContainer", false);
        GameObject closeButtonObject = CreateObject("CloseButton", true);
        GameObject beamParentObject = CreateObject("BeamParent", true);
        GameObject beamPrefabObject = CreateObject("BeamPrefab", true);
        GameObject unsolved = CreateObject("Unsolved", true);
        GameObject solved = CreateObject("Solved", true);
        fixture.Move = CreateObject("Move", true).AddComponent<PlayerMoveController>();
        fixture.Interaction = CreateObject("Interaction", true)
            .AddComponent<PlayerInteractionController>();
        fixture.Start = CreatePillar("START", BeamPillarType.BEAM_PILLAR_START,
            Vector2.zero, new[] { 0f }, rotatable);
        BeamPillarView end = CreatePillar("END", BeamPillarType.BEAM_PILLAR_END,
            directGoal ? new Vector2(3f, 0f) : new Vector2(0f, 3f), new float[0]);
        fixture.Start.transform.SetParent(popupContainer.transform, true);
        end.transform.SetParent(popupContainer.transform, true);
        fixture.Popup = popupHost.AddComponent<UiPuzzlePopup>();
        fixture.Controller = controllerObject.AddComponent<BeamPuzzleController>();
        UnityEvent solvedEvent = new UnityEvent();
        solvedEvent.AddListener(() => fixture.RewardCount++);
        SetField(fixture.Popup, "_popupContainer", popupContainer);
        SetField(fixture.Popup, "_closeButton", closeButtonObject.AddComponent<Button>());
        SetField(fixture.Popup, "_uiPuzzleManager", fixture.Controller);
        SetField(fixture.Popup, "_disablePuzzleManagerWhenClosed", false);
        SetField(fixture.Popup, "_closeAutomaticallyOnClear", false);
        SetField(fixture.Controller, "_objectId", "BEAM_TEST");
        SetField(fixture.Controller, "_puzzlePopup", fixture.Popup);
        SetField(fixture.Controller, "_playerMoveController", fixture.Move);
        SetField(fixture.Controller, "_playerInteractionController", fixture.Interaction);
        SetField(fixture.Controller, "_pillars", new[] { fixture.Start, end });
        SetField(fixture.Controller, "_pillarLayerMask", (LayerMask)(1 << PILLAR_LAYER));
        SetField(fixture.Controller, "_obstacleLayerMask", (LayerMask)(1 << OBSTACLE_LAYER));
        SetField(fixture.Controller, "_beamPrefab", beamPrefabObject.AddComponent<LineRenderer>());
        SetField(fixture.Controller, "_beamParent", beamParentObject.transform);
        SetField(fixture.Controller, "_unsolvedStructure", unsolved);
        SetField(fixture.Controller, "_solvedStructure", solved);
        SetField(fixture.Controller, "_onSolved", solvedEvent);
        InvokeAwake(fixture.Controller);
        InvokeAwake(fixture.Popup);
        return fixture;
    }

    private GameObject CreateObject(string objectName, bool isActive)
    {
        GameObject createdObject = new GameObject(objectName);
        _createdObjects.Add(createdObject);
        createdObject.SetActive(isActive);
        return createdObject;
    }

    private static bool ValidatePillar(BeamPillarView pillar)
    {
        MethodInfo method = typeof(BeamPillarView).GetMethod(
            "TryValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] arguments = { null };
        return (bool)method.Invoke(pillar, arguments);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FindField(target, fieldName).SetValue(target, value);
    }

    private static T GetField<T>(object target, string fieldName)
    {
        return (T)FindField(target, fieldName).GetValue(target);
    }

    private static void InvokeAwake(object target)
    {
        target.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }

    private static FieldInfo FindField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field;
    }

    private sealed class ControllerFixture
    {
        internal BeamPuzzleController Controller;
        internal UiPuzzlePopup Popup;
        internal PlayerMoveController Move;
        internal PlayerInteractionController Interaction;
        internal BeamPillarView Start;
        internal int RewardCount;
    }
}
