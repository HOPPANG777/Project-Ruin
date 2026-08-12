//=============================================================================
// File:        PlayerMazeRecoveryTests.cs
//
// Description: 플레이어 체력·무적·하트와 미로 사망 복구를 검증합니다.
//
// History:     08/08/2026 + Created -- Codex
//
//=============================================================================

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Events;

[TestFixture]
public sealed class PlayerMazeRecoveryTests
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
    public void LethalDamageClampsHealthAndRaisesDeathOnce()
    {
        UnitPlayer player = CreatePlayer(3.0f);
        int deathCount = 0;
        player.Died += () => deathCount++;

        player.TakeDamage(5.0f);
        player.TakeDamage(1.0f);

        Assert.That(player.CurrentHealth, Is.Zero);
        Assert.That(player.IsAlive, Is.False);
        Assert.That(deathCount, Is.EqualTo(1));
    }

    [Test]
    public void InvulnerabilityIgnoresDamageUntilItsEndTime()
    {
        UnitPlayer player = CreatePlayer(3.0f, 10.0f);

        player.TakeDamage(1.0f);
        player.TakeDamage(1.0f);
        Assert.That(player.CurrentHealth, Is.EqualTo(2.0f));

        SetField(player, "_invulnerableUntil", 0.0f);
        player.TakeDamage(1.0f);
        Assert.That(player.CurrentHealth, Is.EqualTo(1.0f));
    }

    [Test]
    public void RestoreFullHealthRevivesAndClearsInvulnerability()
    {
        UnitPlayer player = CreatePlayer(3.0f);
        player.Die();

        bool restored = player.TryRestoreFullHealth();

        Assert.That(restored, Is.True);
        Assert.That(player.IsAlive, Is.True);
        Assert.That(player.CurrentHealth, Is.EqualTo(3.0f));
        Assert.That(player.IsInvulnerable, Is.False);
    }

    [Test]
    public void HeartViewTracksCurrentHealthWithoutCreatingObjects()
    {
        UnitPlayer player = CreatePlayer(3.0f);
        GameObject[] fullHearts =
        {
            CreateObject("Full0"), CreateObject("Full1"), CreateObject("Full2")
        };
        GameObject[] emptyHearts =
        {
            CreateObject("Empty0"), CreateObject("Empty1"), CreateObject("Empty2")
        };
        GameObject heartRoot = CreateObject("HeartRoot");
        GameObject viewObject = CreateObject("HeartView");
        viewObject.SetActive(false);
        PlayerHeartView view = viewObject.AddComponent<PlayerHeartView>();
        SetField(view, "_player", player);
        SetField(view, "_heartRoot", heartRoot);
        SetField(view, "_fullHeartObjects", fullHearts);
        SetField(view, "_emptyHeartObjects", emptyHearts);
        viewObject.SetActive(true);
        Invoke(view, "OnEnable");

        player.TakeDamage(1.0f);

        Assert.That(heartRoot.activeSelf, Is.True);
        Assert.That(fullHearts[0].activeSelf, Is.True);
        Assert.That(fullHearts[1].activeSelf, Is.True);
        Assert.That(fullHearts[2].activeSelf, Is.False);
        Assert.That(emptyHearts[2].activeSelf, Is.True);

        player.TryRestoreFullHealth();
        Assert.That(heartRoot.activeSelf, Is.False);
    }

    [Test]
    public void InvalidHeartViewUpdateDoesNotThrow()
    {
        GameObject viewObject = CreateObject("InvalidHeartView");
        viewObject.SetActive(false);
        PlayerHeartView view = viewObject.AddComponent<PlayerHeartView>();
        LogAssert.Expect(LogType.Error,
            "[PlayerHeartView] 플레이어와 동일 개수의 하트 View가 필요합니다.");

        Invoke(view, "OnEnable");
        Invoke(view, "Update");
    }

    [Test]
    public void DeathDuringMazeResetsPuzzlePositionAndHealth()
    {
        MazeFixture fixture = CreateMazeFixture(2);
        UnitPlayer player = CreatePlayer(3.0f);
        BoxCollider2D playerCollider = player.gameObject.AddComponent<BoxCollider2D>();
        fixture.Controller.TryBeginPuzzle();
        Invoke(fixture.Plates[0], "OnTriggerEnter2D", playerCollider);
        player.transform.position = new Vector3(9.0f, 9.0f, 0.0f);
        Transform checkpoint = CreateObject("Checkpoint").transform;
        checkpoint.position = new Vector3(2.0f, 3.0f, 0.0f);
        CreateRecovery(fixture.Controller, player, checkpoint);

        player.TakeDamage(3.0f);

        Assert.That(fixture.Controller.State,
            Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_INITIAL));
        Assert.That(fixture.Plates[0].IsPressed, Is.False);
        Assert.That(player.transform.position, Is.EqualTo(checkpoint.position));
        Assert.That(player.CurrentHealth, Is.EqualTo(3.0f));
        Assert.That(player.IsAlive, Is.True);
    }

    private UnitPlayer CreatePlayer(float health, float invulnerability = 0.0f)
    {
        UnitPlayer player = CreateObject("Player").AddComponent<UnitPlayer>();
        SetField(player, "_maxHealth", health);
        SetField(player, "_currentHealth", health);
        SetField(player, "_isAlive", true);
        SetField(player, "_invulnerabilityDuration", invulnerability);
        Invoke(player, "Awake");
        return player;
    }

    private MazeFixture CreateMazeFixture(int plateCount)
    {
        MazeFixture fixture = new MazeFixture
        {
            Controller = CreateObject("Puzzle").AddComponent<MazePlatePuzzleController>(),
            Plates = new MazePuzzlePlate[plateCount]
        };
        for (int i = 0; i < plateCount; i++)
        {
            GameObject root = CreateObject($"Plate{i}");
            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            MazePuzzlePlate plate = root.AddComponent<MazePuzzlePlate>();
            SetField(plate, "_releasedObject", CreateObject($"Released{i}"));
            SetField(plate, "_pressedObject", CreateObject($"Pressed{i}"));
            fixture.Plates[i] = plate;
        }
        SetField(fixture.Controller, "_objectId", "RECOVERY_TEST");
        SetField(fixture.Controller, "_plates", fixture.Plates);
        SetField(fixture.Controller, "_stones", new PushableStone[0]);
        SetField(fixture.Controller, "_unsolvedResultObject", CreateObject("Unsolved"));
        SetField(fixture.Controller, "_solvedResultObject", CreateObject("Solved"));
        SetField(fixture.Controller, "_onSolved", new UnityEvent());
        Invoke(fixture.Controller, "Awake");
        return fixture;
    }

    private void CreateRecovery(MazePlatePuzzleController puzzle,
        UnitPlayer player, Transform checkpoint)
    {
        GameObject root = CreateObject("Recovery");
        root.SetActive(false);
        MazePuzzleRecovery recovery = root.AddComponent<MazePuzzleRecovery>();
        SetField(recovery, "_puzzle", puzzle);
        SetField(recovery, "_checkpoint", checkpoint);
        SetField(recovery, "_player", player);
        SetField(recovery, "_moveController",
            CreateObject("Move").AddComponent<PlayerMoveController>());
        SetField(recovery, "_interactionController",
            CreateObject("Interaction").AddComponent<PlayerInteractionController>());
        root.SetActive(true);
        Invoke(recovery, "OnEnable");
    }

    private GameObject CreateObject(string objectName)
    {
        GameObject created = new GameObject(objectName);
        _createdObjects.Add(created);
        return created;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = FindField(target.GetType(), fieldName);
        field.SetValue(target, value);
    }

    private static FieldInfo FindField(System.Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                return field;
            }
            type = type.BaseType;
        }
        return null;
    }

    private static void Invoke(object target, string methodName, params object[] arguments)
    {
        System.Type type = target.GetType();
        MethodInfo method = null;
        while (type != null && method == null)
        {
            method = type.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            type = type.BaseType;
        }
        method.Invoke(target, arguments);
    }

    private sealed class MazeFixture
    {
        public MazePlatePuzzleController Controller;
        public MazePuzzlePlate[] Plates;
    }
}
