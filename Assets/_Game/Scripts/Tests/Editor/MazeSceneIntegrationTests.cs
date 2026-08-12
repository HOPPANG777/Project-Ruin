//=============================================================================
// File:        MazeSceneIntegrationTests.cs
//
// Description: 미로 프리팹과 VZScene의 필수 연결을 검증합니다.
//
// History:     08/09/2026 + Created -- Codex
//
//=============================================================================

using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[TestFixture]
public sealed class MazeSceneIntegrationTests
{
    private const string PREFAB_FOLDER = "Assets/_Game/Prefabs/Maze/";

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void MazePrefabsContainRequiredReferences()
    {
        GameObject plate = LoadPrefab("MazePuzzlePlate.prefab");
        Assert.That(plate.GetComponent<MazePuzzlePlate>(), Is.Not.Null);

        GameObject fire = LoadPrefab("MazeFireEmitter.prefab");
        MazeFireEmitter emitter = fire.GetComponent<MazeFireEmitter>();
        Assert.That(emitter, Is.Not.Null);
        Assert.That(GetProperty(emitter, "_fireTiles").arraySize, Is.EqualTo(5));

        GameObject spike = LoadPrefab("MazeSpikeTrap.prefab");
        MazeSpikeTrap trap = spike.GetComponent<MazeSpikeTrap>();
        Assert.That(trap, Is.Not.Null);
        Assert.That(GetProperty(trap, "_damageTile").objectReferenceValue, Is.Not.Null);

        GameObject hearts = LoadPrefab("PlayerHeartHUD.prefab");
        PlayerHeartView heartView = hearts.GetComponent<PlayerHeartView>();
        Assert.That(heartView, Is.Not.Null);
        Assert.That(GetProperty(heartView, "_fullHeartObjects").arraySize, Is.EqualTo(3));
        Assert.That(GetProperty(heartView, "_emptyHeartObjects").arraySize, Is.EqualTo(3));

        GameObject stone = LoadPrefab("MazePushableStone.prefab");
        PushableStone pushableStone = stone.GetComponent<PushableStone>();
        Assert.That(pushableStone, Is.Not.Null);
        Assert.That(GetProperty(pushableStone, "_requiredSkill").objectReferenceValue,
            Is.Not.Null);
    }

    [Test]
    public void VzSceneContainsConnectedMazeDemo()
    {
        var scene = EditorSceneManager.OpenScene(
            "Assets/_Game/Scenes/VZScene.unity", OpenSceneMode.Single);
        GameObject demo = null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "MazePlatePuzzle_Demo") demo = roots[i];
            AssertNoMissingScripts(roots[i]);
        }
        Assert.That(demo, Is.Not.Null);

        MazePlatePuzzleController puzzle = demo.GetComponent<MazePlatePuzzleController>();
        Assert.That(puzzle, Is.Not.Null);
        Assert.That(GetProperty(puzzle, "_plates").arraySize, Is.EqualTo(2));
        Assert.That(GetProperty(puzzle, "_stones").arraySize, Is.EqualTo(2));
        Assert.That(demo.GetComponent<MazePuzzleRecovery>(), Is.Not.Null);
        Assert.That(demo.GetComponentInChildren<MazeFireEmitter>(true), Is.Not.Null);
        Assert.That(demo.GetComponentInChildren<MazeSpikeTrap>(true), Is.Not.Null);

        UnitPlayer player = Object.FindFirstObjectByType<UnitPlayer>();
        Assert.That(player, Is.Not.Null);
        Assert.That(player.MaxHealth, Is.EqualTo(3.0f));
        Assert.That(player.CurrentHealth, Is.EqualTo(3.0f));
        PlayerHeartView[] heartViews = Object.FindObjectsByType<PlayerHeartView>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(heartViews, Has.Length.EqualTo(1));
        Assert.That(GetProperty(heartViews[0], "_player").objectReferenceValue,
            Is.EqualTo(player));
        Assert.That(LayerMask.NameToLayer("MazeBlocker"), Is.EqualTo(9));
        Assert.That(LayerMask.NameToLayer("MazeHazard"), Is.EqualTo(10));
    }

    [Test]
    public void GameRootRegistersMazePushSkill()
    {
        GameObject gameRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/GameRoot.prefab");
        SkillController skillController =
            gameRootPrefab.GetComponentInChildren<SkillController>(true);
        SkillDataSO mazeSkill = AssetDatabase.LoadAssetAtPath<SkillDataSO>(
            "Assets/_Game/Data/Maze/MazePushSkill.asset");
        Assert.That(skillController, Is.Not.Null);
        Assert.That(mazeSkill, Is.Not.Null);

        SerializedProperty skills = GetProperty(skillController, "_skills");
        bool found = false;
        for (int i = 0; i < skills.arraySize; i++)
        {
            found |= skills.GetArrayElementAtIndex(i).objectReferenceValue == mazeSkill;
        }
        Assert.That(found, Is.True);
    }

    private static GameObject LoadPrefab(string fileName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_FOLDER + fileName);
        Assert.That(prefab, Is.Not.Null, fileName);
        return prefab;
    }

    private static SerializedProperty GetProperty(Object target, string propertyName)
    {
        return new SerializedObject(target).FindProperty(propertyName);
    }

    private static void AssertNoMissingScripts(GameObject root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                transforms[i].gameObject), Is.Zero, transforms[i].name);
        }
    }
}
