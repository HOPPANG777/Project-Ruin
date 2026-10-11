using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MazePlatePuzzlePrefabTests
{
    private const string TEST_PREFAB_PATH = "Assets/_Game/Prefabs/Maze/TestMazePlatePuzzle.prefab";
    private const string CONTROLLER_PREFAB_PATH = "Assets/_Game/Prefabs/Maze/MazePlatePuzzleController.prefab";
    private readonly List<GameObject> _created = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject gameObject in _created)
            if (gameObject != null) Object.DestroyImmediate(gameObject);
        _created.Clear();
    }

    [Test]
    public void PlayerAndStonePrefabsHaveDistinctInputModes()
    {
        foreach (string name in new[] { "MazePlayerPlate", "MazeStonePlate" })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Maze/" + name + ".prefab");
            Assert.That(prefab, Is.Not.Null, name);
            MazePuzzlePlate plate = prefab.GetComponent<MazePuzzlePlate>();
            Assert.That(plate.Mode, Is.EqualTo(name == "MazePlayerPlate"
                ? MazePuzzlePlateMode.MAZE_PLATE_PLAYER_ONCE : MazePuzzlePlateMode.MAZE_PLATE_STONE_ONLY));
            Assert.That(Property(plate, "_releasedObject").objectReferenceValue, Is.Not.Null);
            Assert.That(Property(plate, "_pressedObject").objectReferenceValue, Is.Not.Null);
        }
    }

    [Test]
    public void StandaloneControllerUsesOnlyManuallyRegisteredPlate()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CONTROLLER_PREFAB_PATH);
        Assert.That(prefab, Is.Not.Null);
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        _created.Add(root);
        GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Maze/MazePlayerPlate.prefab");
        GameObject registered = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
        _created.Add(registered);
        GameObject unregistered = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab, root.transform);
        MazePlatePuzzleController controller = root.GetComponent<MazePlatePuzzleController>();
        SerializedObject configuration = new SerializedObject(controller);
        SerializedProperty plates = configuration.FindProperty("_plates");
        plates.arraySize = 1;
        plates.GetArrayElementAtIndex(0).objectReferenceValue = registered.GetComponent<MazePuzzlePlate>();
        configuration.ApplyModifiedPropertiesWithoutUndo();
        int solvedCount = 0;
        UnityEngine.Events.UnityEvent onSolved = new UnityEngine.Events.UnityEvent();
        onSolved.AddListener(() => solvedCount++);
        typeof(MazePlatePuzzleController).GetField("_onSolved", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, onSolved);
        Invoke(controller, "Awake");
        Assert.That(Property(controller, "_plates").arraySize, Is.EqualTo(1));

        GameObject player = new GameObject("Player", typeof(UnitPlayer), typeof(BoxCollider2D));
        _created.Add(player);
        MazePuzzlePlate unusedPlate = unregistered.GetComponent<MazePuzzlePlate>();
        Invoke(unusedPlate, "Awake");
        Invoke(unusedPlate, "OnTriggerEnter2D", player.GetComponent<BoxCollider2D>());
        Assert.That(controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_INITIAL));
        MazePuzzlePlate plate = registered.GetComponent<MazePuzzlePlate>();
        Invoke(plate, "Awake");
        Invoke(plate, "OnTriggerEnter2D", player.GetComponent<BoxCollider2D>());
        Invoke(plate, "OnTriggerExit2D", player.GetComponent<BoxCollider2D>());

        Assert.That(controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED));
        Assert.That(plate.IsPressed, Is.True);
        Assert.That(solvedCount, Is.EqualTo(1));
    }

    [Test]
    public void ChapterZeroPlayerPlatesOpenTheirRegisteredResultWithoutBeginTrigger()
    {
        GameObject map = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Map/GridChapter0.prefab");
        Assert.That(map, Is.Not.Null);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(map);
        _created.Add(instance);
        MazePlatePuzzleController[] controllers = instance.GetComponentsInChildren<MazePlatePuzzleController>(true);
        Assert.That(controllers, Has.Length.EqualTo(2));
        GameObject player = new GameObject("Player", typeof(UnitPlayer), typeof(BoxCollider2D));
        _created.Add(player);
        foreach (MazePlatePuzzleController controller in controllers)
        {
            Invoke(controller, "Awake");
            Assert.That(Property(controller, "_beginOnPlatePress").boolValue, Is.True);
            MazePuzzlePlate plate = (MazePuzzlePlate)Property(controller, "_plates").GetArrayElementAtIndex(0).objectReferenceValue;
            Assert.That(plate.Mode, Is.EqualTo(MazePuzzlePlateMode.MAZE_PLATE_PLAYER_ONCE));
            Invoke(plate, "OnTriggerEnter2D", player.GetComponent<BoxCollider2D>());
            Invoke(plate, "OnTriggerExit2D", player.GetComponent<BoxCollider2D>());
            Assert.That(controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED));
            Assert.That(plate.IsPressed, Is.True);
            Assert.That(((GameObject)Property(controller, "_unsolvedResultObject").objectReferenceValue).activeSelf, Is.False);
            Assert.That(((GameObject)Property(controller, "_solvedResultObject").objectReferenceValue).activeSelf, Is.True);
        }
    }

    [Test]
    public void PrefabHasCompleteLocalBindingsAndNoScenePlayerReference()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TEST_PREFAB_PATH);
        Assert.That(prefab, Is.Not.Null);
        MazePlatePuzzleController controller = prefab.GetComponent<MazePlatePuzzleController>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(Property(controller, "_plates").arraySize, Is.EqualTo(2));
        Assert.That(Property(controller, "_stones").arraySize, Is.EqualTo(2));
        Assert.That(Property(controller, "_unsolvedResultObject").objectReferenceValue, Is.Not.Null);
        Assert.That(Property(controller, "_solvedResultObject").objectReferenceValue, Is.Not.Null);
        foreach (MazePlatePuzzleTrigger trigger in prefab.GetComponentsInChildren<MazePlatePuzzleTrigger>(true))
            Assert.That(Property(trigger, "_puzzle").objectReferenceValue, Is.EqualTo(controller));
        Assert.That(prefab.GetComponentsInChildren<MazePlatePuzzleTrigger>(true).Length, Is.EqualTo(2));
        foreach (PushableStone stone in prefab.GetComponentsInChildren<PushableStone>(true))
        {
            Assert.That(Property(stone, "_playerMoveController").objectReferenceValue, Is.Null);
            Assert.That(Property(stone, "_requiredSkill").objectReferenceValue, Is.Not.Null);
            Assert.That(Property(stone, "_puzzleId").stringValue, Is.EqualTo(controller.ObjectId));
        }
        foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero);
    }

    [Test]
    public void PlacedPrefabBeginsThroughTriggerAndOpensGateWhenBothPlatesArePressed()
    {
        GameObject instance = CreateInstance();
        MazePlatePuzzleController controller = instance.GetComponent<MazePlatePuzzleController>();
        PushableStone[] stones = instance.GetComponentsInChildren<PushableStone>();
        foreach (PushableStone stone in stones) Invoke(stone, "Awake");
        Invoke(controller, "Awake");
        MazePlatePuzzleTrigger begin = instance.transform.Find("BeginTrigger").GetComponent<MazePlatePuzzleTrigger>();
        Invoke(begin, "Awake");
        GameObject player = new GameObject("Player", typeof(UnitPlayer), typeof(BoxCollider2D));
        _created.Add(player);
        Invoke(begin, "OnTriggerEnter2D", player.GetComponent<BoxCollider2D>());
        Assert.That(controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS));

        MazePuzzlePlate[] plates = instance.GetComponentsInChildren<MazePuzzlePlate>();
        Invoke(plates[0], "OnTriggerEnter2D", stones[0].GetComponent<Collider2D>());
        Assert.That(controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS));
        Invoke(plates[1], "OnTriggerEnter2D", stones[1].GetComponent<Collider2D>());
        Assert.That(controller.State, Is.EqualTo(MazePlatePuzzleState.MAZE_PLATE_PUZZLE_COMPLETED));
        Assert.That(instance.transform.Find("GateClosed").gameObject.activeSelf, Is.False);
        Assert.That(instance.transform.Find("GateOpen").gameObject.activeSelf, Is.True);
        Assert.That(instance.transform.Find("GateOpen").GetComponent<Collider2D>(), Is.Null);
    }

    [Test]
    public void StoneUsesInteractionPlayerWithoutChangingPrefabReference()
    {
        PushableStone stone = CreateInstance().GetComponentInChildren<PushableStone>();
        Invoke(stone, "Awake");
        GameObject player = new GameObject("Player");
        player.SetActive(false);
        _created.Add(player);
        PlayerMoveController movement = player.AddComponent<PlayerMoveController>();
        PlayerInteractionController interaction = player.AddComponent<PlayerInteractionController>();
        stone.SetInteractionContext(interaction, movement);
        Assert.That(Field(stone, "_playerMove"), Is.SameAs(movement));
        Assert.That(Field(stone, "_playerTransform"), Is.SameAs(player.transform));
        Assert.That(Property(stone, "_playerMoveController").objectReferenceValue, Is.Null);
        stone.SetInteractionContext(null, null);
        Assert.That(Field(stone, "_playerMove"), Is.Null);
        Assert.That(Field(stone, "_playerTransform"), Is.Null);
    }

    private GameObject CreateInstance()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TEST_PREFAB_PATH);
        Assert.That(prefab, Is.Not.Null);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        _created.Add(instance);
        return instance;
    }

    private static SerializedProperty Property(Object target, string name) => new SerializedObject(target).FindProperty(name);
    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
}
