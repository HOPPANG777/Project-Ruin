//=============================================================================
// File:        MazePlatePuzzleTests.cs
//
// Description: 다중 순간압력 발판의 점유, 해결과 저장 규칙을 검증합니다.
//
// History:     08/06/2026 + Created -- Codex
//
//=============================================================================

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

[TestFixture]
public sealed class MazePlatePuzzleTests
{
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
        {
            if (_createdObjects[i] != null)
            {
                Object.DestroyImmediate(_createdObjects[i]);
            }
        }
        _createdObjects.Clear();
    }

    [Test]
    public void PlayerPressesPlateOnlyWhileOverlapping()
    {
        PuzzleFixture fixture = CreateFixture(1);
        Collider2D player = CreatePlayerCollider();

        Enter(fixture.Plates[0], player);
        Assert.That(fixture.Plates[0].IsPressed, Is.True);

        Exit(fixture.Plates[0], player);
        Assert.That(fixture.Plates[0].IsPressed, Is.False);
    }

    [Test]
    public void PushableStoneCanPressPlate()
    {
        PuzzleFixture fixture = CreateFixture(1);
        Collider2D stone = CreateStoneCollider();

        Enter(fixture.Plates[0], stone);

        Assert.That(fixture.Plates[0].IsPressed, Is.True);
    }

    [Test]
    public void ChildTriggerFindsInteractableOnAttachedRigidbody()
    {
        PushableStone stone = CreateStone(Vector2.zero);
        Collider2D trigger = CreateChildCollider(stone.gameObject, "InteractionTrigger");
        trigger.isTrigger = true;
        MethodInfo method = typeof(PlayerInteractionController).GetMethod("TryGetInteractable", BindingFlags.Static | BindingFlags.NonPublic);
        object[] arguments = { trigger, null };
        Assert.That(method.Invoke(null, arguments), Is.True);
        Assert.That(arguments[1], Is.SameAs(stone));
    }

    [Test]
    public void MultipleCollidersFromOnePlayerKeepPlatePressedUntilLastExit()
    {
        PuzzleFixture fixture = CreateFixture(1);
        GameObject player = CreateObject("Player", typeof(UnitPlayer));
        Collider2D first = CreateChildCollider(player, "FirstCollider");
        Collider2D second = CreateChildCollider(player, "SecondCollider");

        Enter(fixture.Plates[0], first);
        Enter(fixture.Plates[0], second);
        Exit(fixture.Plates[0], first);

        Assert.That(fixture.Plates[0].IsPressed, Is.True);
        Exit(fixture.Plates[0], second);
        Assert.That(fixture.Plates[0].IsPressed, Is.False);
    }

    [Test]
    public void AllPlatesPressedCompletesAndRewardsOnce()
    {
        int solvedCount = 0;
        UnityEvent onSolved = new UnityEvent();
        onSolved.AddListener(() => solvedCount++);
        PuzzleFixture fixture = CreateFixture(2, onSolved);

        Assert.That(fixture.Controller.TryBeginPuzzle(), Is.True);
        Enter(fixture.Plates[0], CreatePlayerCollider());
        Collider2D stone = CreateStoneCollider();
        Enter(fixture.Plates[1], stone);
        Enter(fixture.Plates[1], stone);

        Assert.That(fixture.Controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED));
        Assert.That(fixture.Unsolved.activeSelf, Is.False);
        Assert.That(fixture.Solved.activeSelf, Is.True);
        Assert.That(solvedCount, Is.EqualTo(1));
    }

    [Test]
    public void ReleasedPlatePreventsCompletion()
    {
        PuzzleFixture fixture = CreateFixture(2);
        Collider2D player = CreatePlayerCollider();

        fixture.Controller.TryBeginPuzzle();
        Enter(fixture.Plates[0], player);
        Exit(fixture.Plates[0], player);
        Enter(fixture.Plates[1], CreateStoneCollider());

        Assert.That(fixture.Controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS));
    }

    [Test]
    public void OccupiedPlatesCompleteWhenPuzzleBegins()
    {
        PuzzleFixture fixture = CreateFixture(1);
        Enter(fixture.Plates[0], CreatePlayerCollider());

        fixture.Controller.TryBeginPuzzle();

        Assert.That(fixture.Controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED));
    }

    [Test]
    public void BeginTriggerIgnoresStoneAndStartsForPlayer()
    {
        PuzzleFixture fixture = CreateFixture(2);
        MazePlatePuzzleTrigger trigger = CreateTrigger(fixture, MazePlatePuzzleTriggerAction.MAZE_PLATE_TRIGGER_BEGIN);

        EnterTrigger(trigger, CreateStoneCollider());
        Assert.That(fixture.Controller.State,
            Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_INITIAL));

        EnterTrigger(trigger, CreatePlayerCollider());
        Assert.That(fixture.Controller.State,
            Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS));
    }

    [Test]
    public void ResetTriggerRestoresPlateStoneAndInitialState()
    {
        Vector2 initialPosition = new Vector2(2f, 3f);
        PushableStone stone = CreateStone(initialPosition);
        PuzzleFixture fixture = CreateFixture(2, null, new[] { stone });
        MazePlatePuzzleTrigger trigger = CreateTrigger(fixture, MazePlatePuzzleTriggerAction.MAZE_PLATE_TRIGGER_RESET);
        Collider2D player = CreatePlayerCollider();
        fixture.Controller.TryBeginPuzzle();
        Enter(fixture.Plates[0], player);
        stone.GetComponent<Rigidbody2D>().position = new Vector2(8f, 9f);

        EnterTrigger(trigger, player);

        Assert.That(fixture.Controller.State,
            Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_INITIAL));
        Assert.That(fixture.Plates[0].IsPressed, Is.False);
        Assert.That(stone.GetComponent<Rigidbody2D>().position, Is.EqualTo(initialPosition));
    }

    [Test]
    public void RestoreSolvedStateDoesNotRewardAgain()
    {
        int solvedCount = 0;
        UnityEvent onSolved = new UnityEvent();
        onSolved.AddListener(() => solvedCount++);
        PuzzleFixture fixture = CreateFixture(1, onSolved);

        fixture.Controller.ApplyState("true");

        Assert.That(fixture.Controller.CaptureState(), Is.EqualTo("true"));
        Assert.That(fixture.Controller.State,
            Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED));
        Assert.That(solvedCount, Is.Zero);
    }

    [Test]
    public void InvalidStoredStateIsIgnored()
    {
        PuzzleFixture fixture = CreateFixture(1);
        LogAssert.Expect(LogType.Warning,
            "[MazePlatePuzzleController] 저장 상태를 읽을 수 없습니다. object=Puzzle");

        fixture.Controller.ApplyState("invalid");

        Assert.That(fixture.Controller.State,
            Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_INITIAL));
    }

    private PuzzleFixture CreateFixture(int plateCount, UnityEvent onSolved = null,
        PushableStone[] stones = null)
    {
        PuzzleFixture fixture = new PuzzleFixture
        {
            Controller = CreateObject("Puzzle", typeof(MazePlatePuzzleController))
                .GetComponent<MazePlatePuzzleController>(),
            Plates = new MazePuzzlePlate[plateCount],
            Unsolved = CreateObject("Unsolved"),
            Solved = CreateObject("Solved")
        };
        for (int i = 0; i < plateCount; i++)
        {
            fixture.Plates[i] = CreatePlate(i);
        }
        SetField(fixture.Controller, "_objectId", "MAZE_PLATE_TEST");
        SetField(fixture.Controller, "_plates", fixture.Plates);
        SetField(fixture.Controller, "_stones", stones ?? new PushableStone[0]);
        SetField(fixture.Controller, "_unsolvedResultObject", fixture.Unsolved);
        SetField(fixture.Controller, "_solvedResultObject", fixture.Solved);
        SetField(fixture.Controller, "_onSolved", onSolved ?? new UnityEvent());
        Invoke(fixture.Controller, "Awake");
        return fixture;
    }

    private MazePuzzlePlate CreatePlate(int index)
    {
        GameObject root = CreateObject($"Plate{index}", typeof(BoxCollider2D),
            typeof(MazePuzzlePlate));
        root.GetComponent<BoxCollider2D>().isTrigger = true;
        MazePuzzlePlate plate = root.GetComponent<MazePuzzlePlate>();
        SetField(plate, "_releasedObject", CreateObject($"Released{index}"));
        SetField(plate, "_pressedObject", CreateObject($"Pressed{index}"));
        return plate;
    }

    private Collider2D CreatePlayerCollider()
    {
        return CreateObject("Player", typeof(UnitPlayer), typeof(BoxCollider2D))
            .GetComponent<BoxCollider2D>();
    }

    private Collider2D CreateStoneCollider()
    {
        return CreateStone(Vector2.zero).GetComponent<Collider2D>();
    }

    private PushableStone CreateStone(Vector2 initialPosition)
    {
        LogAssert.Expect(LogType.Error, "[PushableStone] Stone: objectId가 비어 있습니다.");
        LogAssert.Expect(LogType.Error, "[PushableStone] Stone: 필요 스킬이 연결되지 않았습니다.");
        GameObject root = CreateObject("Stone", typeof(Rigidbody2D),
            typeof(BoxCollider2D), typeof(PushableStone));
        root.GetComponent<Rigidbody2D>().position = initialPosition;
        PushableStone stone = root.GetComponent<PushableStone>();
        Invoke(stone, "Awake");
        return stone;
    }

    private MazePlatePuzzleTrigger CreateTrigger(PuzzleFixture fixture,
        MazePlatePuzzleTriggerAction action)
    {
        GameObject root = CreateObject("Trigger", typeof(BoxCollider2D),
            typeof(MazePlatePuzzleTrigger));
        root.GetComponent<BoxCollider2D>().isTrigger = true;
        MazePlatePuzzleTrigger trigger = root.GetComponent<MazePlatePuzzleTrigger>();
        SetField(trigger, "_puzzle", fixture.Controller);
        SetField(trigger, "_action", action);
        SetField(trigger, "_onTriggered", new UnityEvent());
        Invoke(trigger, "Awake");
        return trigger;
    }

    private Collider2D CreateChildCollider(GameObject parent, string objectName)
    {
        GameObject child = CreateObject(objectName, typeof(BoxCollider2D));
        child.transform.SetParent(parent.transform);
        return child.GetComponent<BoxCollider2D>();
    }

    private GameObject CreateObject(string objectName, params System.Type[] components)
    {
        GameObject created = new GameObject(objectName, components);
        _createdObjects.Add(created);
        return created;
    }

    private static void Enter(MazePuzzlePlate plate, Collider2D other)
    {
        Invoke(plate, "OnTriggerEnter2D", other);
    }

    private static void Exit(MazePuzzlePlate plate, Collider2D other)
    {
        Invoke(plate, "OnTriggerExit2D", other);
    }

    private static void EnterTrigger(MazePlatePuzzleTrigger trigger, Collider2D other)
    {
        Invoke(trigger, "OnTriggerEnter2D", other);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }

    private static void Invoke(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(target, arguments);
    }

    private sealed class PuzzleFixture
    {
        public MazePlatePuzzleController Controller;
        public MazePuzzlePlate[] Plates;
        public GameObject Unsolved;
        public GameObject Solved;
    }
}
