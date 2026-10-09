//=============================================================================
// File:        Chapter1VillageLayoutTests.cs
// Description: Grid_1의 실제 배치, 통행, 목적지 및 기존 공용 참조를 검증합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>맵 복사본에서만 실제 충돌과 프리팹 연결을 확인합니다.</summary>
public sealed class Chapter1VillageLayoutTests
{
    private GameObject _map;
    private ProgressController _progress;
    private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>기존 맵과 겹치지 않는 검증 복사본을 준비합니다.</summary>
    [SetUp]
    public void SetUp()
    {
        _map = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Map/Grid_1.prefab"));
        _map.transform.position = new Vector3(10000, 10000, 0);
        _progress = new GameObject("LayoutProgress").AddComponent<ProgressController>();
        TilemapCollider2D[] tiles = _map.GetComponentsInChildren<TilemapCollider2D>(true);
        for (int i = 0; i < tiles.Length; i++) tiles[i].ProcessTilemapChanges();
        Physics2D.SyncTransforms();
    }

    /// <summary>검증 복사본만 정리합니다.</summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_map);
        Object.DestroyImmediate(_progress.gameObject);
    }

    /// <summary>CSV ID·종류·챕터와 독립 문 개방, 반복 복원을 검증합니다.</summary>
    [Test]
    public void FourIndependentPuzzlesKeepPointsAndOnlyOpenTheirOwnDoor()
    {
        string[] ids = { "PIPE_STAGE_03", "PIPE_STAGE_04", "LIGHT_STAGE_01", "PIPE_STAGE_05" };
        Vector2[] positions = { new Vector2(460.6f, 50.6f), new Vector2(612.2f, 44.8f),
            new Vector2(659.8f, -40), new Vector2(604.8f, -80.6f) };
        UiPuzzleSceneState[] states = _map.transform.Find("Chapter_1/Chapter1VillagePuzzles").GetComponentsInChildren<UiPuzzleSceneState>(true);
        Assert.That(states.Length, Is.EqualTo(4));
        SceneCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SceneCatalogSO>("Assets/_Game/Data/SceneCatalog/SceneCatalogSO.asset");
        Assert.That(catalog.TryGetById("Chapter1", out SceneCatalogSO.SceneEntry entry), Is.True);
        for (int i = 0; i < states.Length; i++) Assert.That(states[i].StageNumber, Is.EqualTo(entry.StageNumber));
        for (int i = 0; i < states.Length; i++) states[i].Init(_progress, null, true);
        for (int i = 0; i < ids.Length; i++)
        {
            UiPuzzleInteractable puzzle = Puzzle(ids[i]);
            Assert.That(puzzle.ChapterIndex, Is.EqualTo(1));
            Assert.That(puzzle.PuzzleType, Is.EqualTo(i == 2 ? PuzzleType.PUZZLE_TYPE_LIGHT : PuzzleType.PUZZLE_TYPE_PIPE));
            Assert.That(puzzle.gameObject.layer, Is.EqualTo(8));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(puzzle), Is.Not.Null);
            UiPuzzleSceneState state = puzzle.GetComponent<UiPuzzleSceneState>();
            GameObject door = Door(state);
            Assert.That(door.name, Is.EqualTo("Point" + (i + 1)));
            Assert.That((Vector2)door.transform.localPosition, Is.EqualTo(positions[i]));
            Assert.That(door.GetComponent<BoxCollider2D>().isTrigger, Is.False);
            Assert.That(door.GetComponent<BoxCollider2D>().size, Is.EqualTo(door.GetComponent<SpriteRenderer>().size));
            Assert.That(puzzle.transform.IsChildOf(door.transform), Is.False);
            int clearCalls = 0;
            puzzle.PuzzleCleared += () => clearCalls++;
            Clear(puzzle);
            Clear(puzzle);
            Assert.That(clearCalls, Is.EqualTo(1));
            Assert.That(door.activeSelf, Is.False);
            Assert.That(puzzle.gameObject.activeSelf, Is.True);
            Assert.That(puzzle.GetComponent<Collider2D>().enabled, Is.False);
            for (int j = i + 1; j < ids.Length; j++) Assert.That(Door(Puzzle(ids[j]).GetComponent<UiPuzzleSceneState>()).activeSelf, Is.True);
            state.ApplyState(state.CaptureState());
            state.ApplyState(state.CaptureState());
            Assert.That(clearCalls, Is.EqualTo(1));
            for (int j = 0; j < states.Length; j++)
                if (state != states[j]) Assert.That(state.ObjectId, Is.Not.EqualTo(states[j].ObjectId));
        }
        Transform point5 = _map.transform.Find("Chapter_1/Point5");
        Assert.That(point5.localPosition, Is.EqualTo(new Vector3(504.9f, -91.9f, 0)));
        Assert.That(point5.GetComponent<Collider2D>(), Is.Null);
    }

    /// <summary>닫힌 문과 열린 굽은 통로를 실제 플레이어 크기의 경로 탐색으로 확인합니다.</summary>
    [TestCase("PIPE_STAGE_03")]
    [TestCase("PIPE_STAGE_04")]
    [TestCase("LIGHT_STAGE_01")]
    [TestCase("PIPE_STAGE_05")]
    public void EachClosedDoorBlocksItsPassageAndOpenPassageHasNoSolidCollision(string id)
    {
        UiPuzzleSceneState state = Puzzle(id).GetComponent<UiPuzzleSceneState>();
        state.Init(_progress, null, true);
        GameObject door = Door(state);
        string number = door.name.Substring(5);
        Transform pad = _map.transform.Find("Chapter_1/Chapter1VillagePuzzles/Point" + number
            + (number == "1" ? "_ToRuin" : "_ScenePad_Unassigned"));
        Vector2 from = state.transform.position;
        Vector2 delta = (Vector2)pad.position - from;
        Assert.That(CanReach(from, delta), Is.False, "닫힌 통로: " + door.name);
        Clear(state.GetComponent<UiPuzzleInteractable>());
        Physics2D.SyncTransforms();
        Assert.That(CanReach(from, delta), Is.True, $"열린 통로: {door.name}, tableFree={IsFree(from)}, padFree={IsFree(pad.position)}");
    }

    /// <summary>도착 지점은 패드 감지와 벽에서 실제 플레이어 크기만큼 떨어져야 합니다.</summary>
    [Test]
    public void PositionPadRoundTripArrivalsAreOutsideAllTriggersAndWalls()
    {
        PositionTransitionPad[] pads = _map.transform.Find("Chapter_1/Chapter1VillagePuzzles").GetComponentsInChildren<PositionTransitionPad>(true);
        Assert.That(pads.Length, Is.EqualTo(2));
        for (int i = 0; i < pads.Length; i++)
        {
            Transform arrival = (Transform)new SerializedObject(pads[i]).FindProperty("_destination").objectReferenceValue;
            Assert.That(arrival, Is.Not.Null);
            Assert.That(arrival.IsChildOf(pads[1 - i].transform), Is.True);
            Collider2D[] hits = Physics2D.OverlapBoxAll(arrival.position, new Vector2(2, 3), 0);
            for (int j = 0; j < hits.Length; j++) Assert.Fail("도착 범위 중첩: " + arrival.name + " / " + hits[j].name);
        }
        SceneTransitionPad[] scenes = _map.GetComponentsInChildren<SceneTransitionPad>(true);
        Assert.That(scenes.Length, Is.EqualTo(3));
        for (int i = 0; i < scenes.Length; i++)
        {
            Assert.That(scenes[i].gameObject.activeInHierarchy, Is.True);
            Assert.That(scenes[i].GetComponent<BoxCollider2D>().enabled, Is.False);
            Assert.That(scenes[i].GetComponentInChildren<SpriteRenderer>().enabled, Is.True);
            SerializedObject data = new SerializedObject(scenes[i]);
            Assert.That(data.FindProperty("_destinationStageNumber").intValue, Is.EqualTo(-1));
            Assert.That(data.FindProperty("_targetSpawnPointId").stringValue, Is.Empty);
        }
    }

    /// <summary>공용 GameRoot 프리팹의 기존 초기화 경로에 연결되었는지 확인합니다.</summary>
    [Test]
    public void GameRootModuleInjectsControllerAndRetainsSharedPuzzleUi()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab");
        UiPuzzleStateController controller = root.GetComponent<UiPuzzleStateController>();
        Assert.That(controller, Is.Not.Null);
        SerializedObject data = new SerializedObject(root.GetComponent<GameRoot>());
        Assert.That(data.FindProperty("_sceneFlowModule._uiPuzzleStateController").objectReferenceValue, Is.SameAs(controller));
        Assert.That(root.GetComponentInChildren<PuzzleUiRoot>(true), Is.Not.Null);
    }

    private UiPuzzleInteractable Puzzle(string id)
    {
        UiPuzzleInteractable[] puzzles = _map.GetComponentsInChildren<UiPuzzleInteractable>(true);
        for (int i = 0; i < puzzles.Length; i++) if (puzzles[i].PuzzleId == id) return puzzles[i];
        Assert.Fail(id);
        return null;
    }
    private static GameObject Door(UiPuzzleSceneState state) =>
        (GameObject)new SerializedObject(state).FindProperty("_closedDoor").objectReferenceValue;
    private static void Clear(UiPuzzleInteractable puzzle) => typeof(UiPuzzleInteractable)
        .GetMethod("NotifyPuzzleCleared", PRIVATE).Invoke(puzzle, null);
    private static bool CanReach(Vector2 from, Vector2 delta)
    {
        Vector2Int target = new Vector2Int(Mathf.RoundToInt(delta.x * 4), Mathf.RoundToInt(delta.y * 4));
        Queue<Vector2Int> pending = new Queue<Vector2Int>();
        HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
        if (!IsFree(from)) return false;
        pending.Enqueue(Vector2Int.zero);
        seen.Add(Vector2Int.zero);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        while (pending.Count > 0)
        {
            Vector2Int current = pending.Dequeue();
            if (current == target) return true;
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];
                if (next.x < Mathf.Min(0, target.x) - 24 || next.x > Mathf.Max(0, target.x) + 24
                    || next.y < Mathf.Min(0, target.y) - 24 || next.y > Mathf.Max(0, target.y) + 24
                    || !seen.Add(next) || !IsFree(from + (Vector2)next / 4)) continue;
                pending.Enqueue(next);
            }
        }
        return false;
    }
    private static bool IsFree(Vector2 position)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(position, new Vector2(2.02f, 3.02f), 0);
        for (int i = 0; i < hits.Length; i++)
            if (!hits[i].isTrigger) return false;
        return true;
    }
}
