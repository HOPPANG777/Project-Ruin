//=============================================================================
// File:        MazeHazardTests.cs
//
// Description: 미로 불·가시의 피해, 차단, 교차 표시와 상태 연동을 검증합니다.
//
// History:     08/08/2026 + Created -- Codex
//
//=============================================================================

using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

[TestFixture]
public sealed class MazeHazardTests
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
    public void HazardRetriesDamageAfterPlayerInvulnerabilityEnds()
    {
        UnitPlayer player = CreatePlayer(3.0f, 10.0f);
        BoxCollider2D playerCollider = player.gameObject.AddComponent<BoxCollider2D>();
        MazeHazardTile hazard = CreateHazardTile("Hazard", false);
        Invoke(hazard, "OnTriggerEnter2D", playerCollider);

        Invoke(hazard, "OnTriggerStay2D", playerCollider);
        Invoke(hazard, "OnTriggerStay2D", playerCollider);
        Assert.That(player.CurrentHealth, Is.EqualTo(2.0f));

        SetField(player, "_invulnerableUntil", 0.0f);
        SetField(hazard, "_lastDamageFixedTime", float.NegativeInfinity);
        Invoke(hazard, "OnTriggerStay2D", playerCollider);
        Assert.That(player.CurrentHealth, Is.EqualTo(1.0f));
    }

    [Test]
    public void FireUsesTileLengthAndStopsBeforeBlocker()
    {
        MazePlatePuzzleController puzzle = CreatePuzzle();
        Transform origin = CreateObject("Origin").transform;
        MazeHazardTile[] tiles = CreateFireTiles("Fire");
        MazeFireEmitter emitter = CreateFireEmitter(puzzle, origin, tiles);
        GameObject blocker = CreateObject("StoneBlocker");
        blocker.layer = 8;
        blocker.transform.position = new Vector3(2.5f, 0.0f, 0.0f);
        blocker.AddComponent<BoxCollider2D>().size = Vector2.one;
        Physics2D.SyncTransforms();

        puzzle.TryBeginPuzzle();

        Assert.That(tiles[0].gameObject.activeSelf, Is.True);
        Assert.That(tiles[1].gameObject.activeSelf, Is.True);
        Assert.That(tiles[2].gameObject.activeSelf, Is.False);
        Assert.That(tiles[4].gameObject.activeSelf, Is.False);

        puzzle.ApplyState("true");
        Assert.That(tiles[0].gameObject.activeSelf, Is.False);
    }

    [Test]
    public void DifferentFireDirectionsEnableOnlyOneOverlapMarker()
    {
        MazeFireEmitter firstEmitter = CreateObject("FirstEmitter")
            .AddComponent<MazeFireEmitter>();
        MazeFireEmitter secondEmitter = CreateObject("SecondEmitter")
            .AddComponent<MazeFireEmitter>();
        SetField(firstEmitter, "_direction", MazeFireDirection.MAZE_FIRE_RIGHT);
        SetField(secondEmitter, "_direction", MazeFireDirection.MAZE_FIRE_UP);
        MazeHazardTile first = CreateHazardTile("FirstTile", true);
        MazeHazardTile second = CreateHazardTile("SecondTile", true);
        Invoke(first, "InitializeFire", firstEmitter);
        Invoke(second, "InitializeFire", secondEmitter);

        Invoke(first, "OnTriggerEnter2D", second.GetComponent<Collider2D>());
        Invoke(second, "OnTriggerEnter2D", first.GetComponent<Collider2D>());

        GameObject firstMarker = GetField<GameObject>(first, "_overlapMarker");
        GameObject secondMarker = GetField<GameObject>(second, "_overlapMarker");
        Assert.That(firstMarker.activeSelf ^ secondMarker.activeSelf, Is.True);
    }

    [Test]
    public void SpikeCyclesOnlyWhilePuzzleIsInProgress()
    {
        MazePlatePuzzleController puzzle = CreatePuzzle();
        GameObject retracted = CreateObject("Retracted");
        GameObject extended = CreateObject("Extended");
        MazeHazardTile damageTile = CreateHazardTile("SpikeDamage", false);
        damageTile.transform.SetParent(extended.transform);
        MazeSpikeTrap spike = CreateObject("Spike").AddComponent<MazeSpikeTrap>();
        SetField(spike, "_puzzle", puzzle);
        SetField(spike, "_startExtended", true);
        SetField(spike, "_extendedDuration", 1.0f);
        SetField(spike, "_retractedDuration", 1.0f);
        SetField(spike, "_retractedObject", retracted);
        SetField(spike, "_extendedObject", extended);
        SetField(spike, "_damageTile", damageTile);
        Invoke(spike, "Awake");
        Invoke(spike, "OnEnable");

        puzzle.TryBeginPuzzle();
        Assert.That(extended.activeSelf, Is.True);
        SetField(spike, "_elapsedTime", 1.0f);
        Invoke(spike, "Update");
        Assert.That(extended.activeSelf, Is.False);

        puzzle.ApplyState("true");
        Assert.That(retracted.activeSelf, Is.True);
        Assert.That(extended.activeSelf, Is.False);
    }

    [Test]
    public void FireRejectsLengthOutsideOneToFive()
    {
        MazeFireEmitter emitter = CreateObject("InvalidFire").AddComponent<MazeFireEmitter>();
        SetField(emitter, "_puzzle", CreatePuzzle());
        SetField(emitter, "_origin", CreateObject("Origin").transform);
        SetField(emitter, "_length", 6);
        SetField(emitter, "_cellSize", 1.0f);
        SetField(emitter, "_blockerLayer", (LayerMask)(1 << 8));
        SetField(emitter, "_fireTiles", CreateFireTiles("InvalidTile"));
        LogAssert.Expect(LogType.Error,
            new Regex("\\[MazeFireEmitter\\].*불 길이는 1~5"));

        Invoke(emitter, "Awake");

        Assert.That(GetField<bool>(emitter, "_isSetupValid"), Is.False);
    }

    [Test]
    public void HazardRejectsMissingMovementBlocker()
    {
        GameObject root = CreateObject("InvalidHazard");
        BoxCollider2D damageTrigger = root.AddComponent<BoxCollider2D>();
        damageTrigger.isTrigger = true;
        MazeHazardTile hazard = root.AddComponent<MazeHazardTile>();
        SetField(hazard, "_damage", 1.0f);
        SetField(hazard, "_damageTrigger", damageTrigger);
        LogAssert.Expect(LogType.Error,
            new Regex("\\[MazeHazardTile\\].*이동 차단 Collider"));

        Invoke(hazard, "Awake");
        UnitPlayer player = CreatePlayer(3.0f, 0.0f);
        BoxCollider2D playerCollider = player.gameObject.AddComponent<BoxCollider2D>();
        Invoke(hazard, "OnTriggerEnter2D", playerCollider);
        Invoke(hazard, "OnTriggerStay2D", playerCollider);

        Assert.That(player.CurrentHealth, Is.EqualTo(3.0f));
    }

    private MazeFireEmitter CreateFireEmitter(MazePlatePuzzleController puzzle,
        Transform origin, MazeHazardTile[] tiles)
    {
        MazeFireEmitter emitter = CreateObject("Emitter").AddComponent<MazeFireEmitter>();
        SetField(emitter, "_puzzle", puzzle);
        SetField(emitter, "_origin", origin);
        SetField(emitter, "_direction", MazeFireDirection.MAZE_FIRE_RIGHT);
        SetField(emitter, "_length", 5);
        SetField(emitter, "_cellSize", 1.0f);
        SetField(emitter, "_blockerLayer", (LayerMask)(1 << 8));
        SetField(emitter, "_fireTiles", tiles);
        Invoke(emitter, "Awake");
        Invoke(emitter, "OnEnable");
        return emitter;
    }

    private MazeHazardTile[] CreateFireTiles(string prefix)
    {
        MazeHazardTile[] tiles = new MazeHazardTile[5];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = CreateHazardTile($"{prefix}{i}", true);
        }
        return tiles;
    }

    private MazeHazardTile CreateHazardTile(string objectName, bool withMarker)
    {
        GameObject root = CreateObject(objectName);
        BoxCollider2D damageTrigger = root.AddComponent<BoxCollider2D>();
        damageTrigger.isTrigger = true;
        BoxCollider2D movementBlocker = root.AddComponent<BoxCollider2D>();
        movementBlocker.isTrigger = false;
        MazeHazardTile tile = root.AddComponent<MazeHazardTile>();
        SetField(tile, "_damage", 1.0f);
        SetField(tile, "_damageTrigger", damageTrigger);
        SetField(tile, "_movementBlocker", movementBlocker);
        if (withMarker)
        {
            GameObject marker = CreateObject($"{objectName}Marker");
            marker.transform.SetParent(root.transform);
            SetField(tile, "_overlapMarker", marker);
        }
        Invoke(tile, "Awake");
        return tile;
    }

    private UnitPlayer CreatePlayer(float health, float invulnerability)
    {
        UnitPlayer player = CreateObject("Player").AddComponent<UnitPlayer>();
        SetField(player, "_maxHealth", health);
        SetField(player, "_currentHealth", health);
        SetField(player, "_isAlive", true);
        SetField(player, "_invulnerabilityDuration", invulnerability);
        Invoke(player, "Awake");
        return player;
    }

    private MazePlatePuzzleController CreatePuzzle()
    {
        MazePlatePuzzleController puzzle = CreateObject("Puzzle")
            .AddComponent<MazePlatePuzzleController>();
        MazePuzzlePlate[] plates = new MazePuzzlePlate[2];
        for (int i = 0; i < plates.Length; i++)
        {
            GameObject root = CreateObject($"Plate{i}");
            root.AddComponent<BoxCollider2D>().isTrigger = true;
            plates[i] = root.AddComponent<MazePuzzlePlate>();
            SetField(plates[i], "_releasedObject", CreateObject($"Released{i}"));
            SetField(plates[i], "_pressedObject", CreateObject($"Pressed{i}"));
        }
        SetField(puzzle, "_objectId", "HAZARD_TEST");
        SetField(puzzle, "_plates", plates);
        SetField(puzzle, "_stones", new PushableStone[0]);
        SetField(puzzle, "_unsolvedResultObject", CreateObject("Unsolved"));
        SetField(puzzle, "_solvedResultObject", CreateObject("Solved"));
        SetField(puzzle, "_onSolved", new UnityEvent());
        Invoke(puzzle, "Awake");
        return puzzle;
    }

    private GameObject CreateObject(string objectName)
    {
        GameObject created = new GameObject(objectName);
        _createdObjects.Add(created);
        return created;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FindField(target.GetType(), fieldName).SetValue(target, value);
    }

    private static T GetField<T>(object target, string fieldName)
    {
        return (T)FindField(target.GetType(), fieldName).GetValue(target);
    }

    private static FieldInfo FindField(System.Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) return field;
            type = type.BaseType;
        }
        return null;
    }

    private static void Invoke(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        method.Invoke(target, arguments);
    }
}
