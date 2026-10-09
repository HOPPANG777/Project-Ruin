using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MazePlatePuzzlePrefabTests
{
    private readonly List<GameObject> _created = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject gameObject in _created)
            if (gameObject != null) Object.DestroyImmediate(gameObject);
        _created.Clear();
    }

    [Test]
    public void PrefabHasCompleteLocalBindingsAndNoScenePlayerReference()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MazePlatePuzzlePrefabBuilder.PrefabPath);
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
    public void ChildCollectionIncludesAddedPlateButExcludesNestedPuzzle()
    {
        GameObject outer = CreateInstance();
        GameObject inner = CreateInstance();
        inner.transform.SetParent(outer.transform);
        GameObject plateAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Maze/MazePuzzlePlate.prefab");
        GameObject addedPlate = (GameObject)PrefabUtility.InstantiatePrefab(plateAsset, outer.transform);
        addedPlate.SetActive(false);
        MazePlatePuzzleController controller = outer.GetComponent<MazePlatePuzzleController>();
        MazePlatePuzzleControllerEditor.ConnectChildren(controller);
        Assert.That(Property(controller, "_plates").arraySize, Is.EqualTo(3));
        Assert.That(Property(controller, "_stones").arraySize, Is.EqualTo(2));
        foreach (MazePlatePuzzleTrigger trigger in inner.GetComponentsInChildren<MazePlatePuzzleTrigger>(true))
            Assert.That(Property(trigger, "_puzzle").objectReferenceValue, Is.EqualTo(inner.GetComponent<MazePlatePuzzleController>()));
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
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MazePlatePuzzlePrefabBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        _created.Add(instance);
        return instance;
    }

    private static SerializedProperty Property(Object target, string name) => new SerializedObject(target).FindProperty(name);
    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
}
