//=============================================================================
// File:        MazePressurePlateTests.cs
//
// Description: 미로 압력 발판의 플레이어 감지와 단일 문 개방을 검증합니다.
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

//=============================================================================
//
// Synopsis:   MazePressurePlateTests;
//                - 플레이어 전용 감지, 상태 전환, 중복 이벤트 방지 검증
//
//=============================================================================
[TestFixture]
public sealed class MazePressurePlateTests
{
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

    [Test]
    public void PlayerEntryOpensDoorAndPressesPlate()
    {
        MazePressurePlate plate = CreateValidPlate(out PlateFixture fixture);

        EnterPlate(plate, CreateCollider("Player", true));

        Assert.That(plate.IsActivated, Is.True);
        Assert.That(fixture.Raised.activeSelf, Is.False);
        Assert.That(fixture.Pressed.activeSelf, Is.True);
        Assert.That(fixture.ClosedDoor.activeSelf, Is.False);
        Assert.That(fixture.OpenDoor.activeSelf, Is.True);
        Assert.That(fixture.DoorBlocker.enabled, Is.False);
    }

    [Test]
    public void NonPlayerEntryDoesNotOpenDoor()
    {
        MazePressurePlate plate = CreateValidPlate(out PlateFixture fixture);

        EnterPlate(plate, CreateCollider("Crate", false));

        Assert.That(plate.IsActivated, Is.False);
        Assert.That(fixture.DoorBlocker.enabled, Is.True);
    }

    [Test]
    public void RepeatedPlayerEntryInvokesResultOnce()
    {
        int activationCount = 0;
        UnityEvent onActivated = new UnityEvent();
        onActivated.AddListener(() => activationCount++);
        MazePressurePlate plate = CreateValidPlate(out _, onActivated);
        Collider2D playerCollider = CreateCollider("Player", true);

        EnterPlate(plate, playerCollider);
        EnterPlate(plate, playerCollider);

        Assert.That(activationCount, Is.EqualTo(1));
    }

    [Test]
    public void InvalidSetupRejectsActivation()
    {
        GameObject root = CreateObject("InvalidPlate", typeof(BoxCollider2D),
            typeof(MazePressurePlate));
        BoxCollider2D trigger = root.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        MazePressurePlate plate = root.GetComponent<MazePressurePlate>();
        SetField(plate, "_objectId", "INVALID_PLATE");
        LogAssert.Expect(LogType.Error,
            "[MazePressurePlate] InvalidPlate: 필수 발판 또는 문 참조가 연결되지 않았습니다.");

        Invoke(plate, "Awake");
        EnterPlate(plate, CreateCollider("Player", true));

        Assert.That(plate.IsActivated, Is.False);
    }

    [Test]
    public void ClosedPlateCapturesNoState()
    {
        MazePressurePlate plate = CreateValidPlate(out _);

        Assert.That(plate.CaptureState(), Is.Empty);
    }

    [Test]
    public void ActivatedPlateCapturesOpenedState()
    {
        MazePressurePlate plate = CreateValidPlate(out _);
        EnterPlate(plate, CreateCollider("Player", true));

        Assert.That(plate.CaptureState(), Is.EqualTo("true"));
    }

    [Test]
    public void RestoreOpenedStateDoesNotInvokeResult()
    {
        int activationCount = 0;
        UnityEvent onActivated = new UnityEvent();
        onActivated.AddListener(() => activationCount++);
        MazePressurePlate plate = CreateValidPlate(out PlateFixture fixture, onActivated);

        plate.ApplyState("true");

        Assert.That(plate.IsActivated, Is.True);
        Assert.That(fixture.OpenDoor.activeSelf, Is.True);
        Assert.That(fixture.DoorBlocker.enabled, Is.False);
        Assert.That(activationCount, Is.Zero);
    }

    [Test]
    public void InvalidStoredStateIsIgnored()
    {
        MazePressurePlate plate = CreateValidPlate(out PlateFixture fixture);
        LogAssert.Expect(LogType.Warning,
            "[MazePressurePlate] 저장 상태를 읽을 수 없습니다. object=Plate");

        plate.ApplyState("invalid");

        Assert.That(plate.IsActivated, Is.False);
        Assert.That(fixture.DoorBlocker.enabled, Is.True);
    }

    private MazePressurePlate CreateValidPlate(out PlateFixture fixture,
        UnityEvent onActivated = null)
    {
        GameObject root = CreateObject("Plate", typeof(BoxCollider2D),
            typeof(MazePressurePlate));
        root.GetComponent<BoxCollider2D>().isTrigger = true;
        MazePressurePlate plate = root.GetComponent<MazePressurePlate>();
        SetField(plate, "_objectId", "PLATE_TEST");
        fixture = new PlateFixture
        {
            Raised = CreateObject("Raised"),
            Pressed = CreateObject("Pressed"),
            ClosedDoor = CreateObject("ClosedDoor", typeof(BoxCollider2D)),
            OpenDoor = CreateObject("OpenDoor")
        };
        fixture.DoorBlocker = fixture.ClosedDoor.GetComponent<BoxCollider2D>();
        SetField(plate, "_raisedPlateObject", fixture.Raised);
        SetField(plate, "_pressedPlateObject", fixture.Pressed);
        SetField(plate, "_closedDoorObject", fixture.ClosedDoor);
        SetField(plate, "_openDoorObject", fixture.OpenDoor);
        SetField(plate, "_doorBlocker", fixture.DoorBlocker);
        SetField(plate, "_onActivated", onActivated ?? new UnityEvent());
        Invoke(plate, "Awake");
        return plate;
    }

    private Collider2D CreateCollider(string objectName, bool hasPlayer)
    {
        GameObject target = CreateObject(objectName, typeof(BoxCollider2D));
        if (hasPlayer)
        {
            target.AddComponent<UnitPlayer>();
        }
        return target.GetComponent<BoxCollider2D>();
    }

    private GameObject CreateObject(string objectName, params System.Type[] components)
    {
        GameObject created = new GameObject(objectName, components);
        _createdObjects.Add(created);
        return created;
    }

    private static void EnterPlate(MazePressurePlate plate, Collider2D other)
    {
        Invoke(plate, "OnTriggerEnter2D", other);
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

    private sealed class PlateFixture
    {
        public GameObject Raised;
        public GameObject Pressed;
        public GameObject ClosedDoor;
        public GameObject OpenDoor;
        public Collider2D DoorBlocker;
    }
}
