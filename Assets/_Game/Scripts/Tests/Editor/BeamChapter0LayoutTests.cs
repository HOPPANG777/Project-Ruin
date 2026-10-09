//=============================================================================
// File: BeamChapter0LayoutTests.cs
// Description: 실제 Chapter0 Beam 배치의 연결, 해결과 플레이어 통행을 검증한다.
// History: 10/06/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>맵 원본을 바꾸지 않고 배치된 개별 부품을 검증합니다.</summary>
public sealed class BeamChapter0LayoutTests
{
    private const string MAP_PATH = "Assets/_Game/Prefabs/Map/GridChapter0.prefab";
    private GameObject _map;
    private BeamPuzzleController _controller;
    private BeamPillarView[] _pillars;
    private string _sourceId;

    [SetUp]
    public void SetUp()
    {
        _map = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MAP_PATH));
        // 테스트 중 다른 열린 맵과 물리적으로 겹치지 않는 위치에 복사본만 옮깁니다.
        _map.transform.position = new Vector3(10000f,10000f,0f);
        _controller = _map.GetComponentInChildren<BeamPuzzleController>();
        Assert.That(_controller, Is.Not.Null, "BeamPuzzlePoint에 퍼즐이 배치되어야 합니다.");
        var data = new SerializedObject(_controller);
        _sourceId = _controller.ObjectId;
        // 열린 실제 맵의 저장 ID와 테스트 복사본의 ID가 겹치지 않게 합니다.
        data.FindProperty("_objectId").stringValue = "BeamLayoutTest_" + System.Guid.NewGuid().ToString("N");
        data.ApplyModifiedPropertiesWithoutUndo();
        SerializedProperty pillars = data.FindProperty("_pillars");
        _pillars = new BeamPillarView[pillars.arraySize];
        for (int i = 0; i < _pillars.Length; i++) _pillars[i] = (BeamPillarView)pillars.GetArrayElementAtIndex(i).objectReferenceValue;
        TilemapCollider2D tileCollider = _map.GetComponentInChildren<TilemapCollider2D>();
        if (tileCollider.hasTilemapChanges) tileCollider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
    }

    [TearDown]
    public void TearDown()
    {
        if (_map != null) Object.DestroyImmediate(_map);
    }

    [Test]
    public void PlacedPartsAreBoundToOneManagerAndKeepPrefabLinks()
    {
        Assert.That(_sourceId, Is.EqualTo("CH00_BEAM_ROOM_01"));
        Assert.That(_pillars.Length, Is.EqualTo(7));
        Assert.That(_pillars[0].PillarType, Is.EqualTo(BeamPillarType.BEAM_PILLAR_START));
        Assert.That(_pillars[6].PillarType, Is.EqualTo(BeamPillarType.BEAM_PILLAR_END));
        for (int i = 1; i <= 5; i++) Assert.That(_pillars[i].PillarType, Is.EqualTo(BeamPillarType.BEAM_PILLAR_TWO_WAY));
        var issues = new List<string>();
        _controller.CollectValidationIssues(issues);
        Assert.That(issues, Is.Empty, string.Join(" / ", issues));
        SerializedObject data = new SerializedObject(_controller);
        Assert.That(data.FindProperty("_resetPads").arraySize, Is.EqualTo(1));
        Assert.That(data.FindProperty("_fixedObstacles").arraySize, Is.EqualTo(3));
        Assert.That(data.FindProperty("_exitObstacles").arraySize, Is.EqualTo(1));
        Assert.That(data.FindProperty("_stones").arraySize, Is.Zero);
        Assert.That(data.FindProperty("_walls").arraySize, Is.Zero);
        Transform marker = _controller.transform.parent;
        Assert.That(marker.name, Is.EqualTo("BeamPuzzlePoint"));
        Assert.That(marker.localPosition.x, Is.EqualTo(188.1f).Within(0.01f));
        Assert.That(marker.localPosition.y, Is.EqualTo(7.2f).Within(0.01f));
        Assert.That(marker.childCount, Is.EqualTo(13));
        for (int i = 0; i < marker.childCount; i++)
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(marker.GetChild(i).gameObject), Is.True);
        for (int i = 0; i < data.FindProperty("_fixedObstacles").arraySize; i++)
        {
            var body = (Collider2D)data.FindProperty("_fixedObstacles").GetArrayElementAtIndex(i).objectReferenceValue;
            SpriteRenderer visual = body.GetComponentInChildren<SpriteRenderer>();
            Assert.That(visual.bounds.size.x, Is.EqualTo(body.bounds.size.x).Within(0.05f));
            Assert.That(visual.bounds.size.y, Is.EqualTo(body.bounds.size.y).Within(0.05f));
        }
    }

    [Test]
    public void ReferenceRouteResetsAndCompletesThroughAllFiveRelays()
    {
        _controller.InitializePuzzle();
        Assert.That(_controller.BeamActive, Is.False);
        Assert.That(_controller.TryRotatePillar(_pillars[1]), Is.True);
        Assert.That(_controller.BeamActive, Is.True);
        Assert.That(_controller.IsClear, Is.False);
        Assert.That(_controller.TryResetPuzzle(), Is.True);
        Assert.That(_controller.BeamActive, Is.False);
        Assert.That(_pillars[1].RotationDegrees, Is.EqualTo(45f).Within(0.01f));
        for (int i = 1; i <= 5; i++)
        {
            int turns = Mathf.RoundToInt(_pillars[i].RotationDegrees / 45f);
            for (int turn = 0; turn < turns; turn++)
                Assert.That(_controller.TryRotatePillar(_pillars[i]), Is.True, _pillars[i].PillarId);
            if (i < 5) Assert.That(_controller.IsClear, Is.False, "끝까지 중계되어야 완료됩니다.");
        }
        Assert.That(_controller.IsClear, Is.True);
        for (int i = 0; i < _pillars.Length; i++)
        {
            Assert.That(_pillars[i].IsConnected, Is.True, _pillars[i].PillarId);
            Assert.That(_pillars[i].BodyCollider.enabled, Is.True);
        }
        LineRenderer[] lines = _controller.GetComponentsInChildren<LineRenderer>();
        Assert.That(lines.Length, Is.EqualTo(6));
        for (int i = 0; i < lines.Length; i++) Assert.That(lines[i].enabled, Is.True);
        Assert.That(Gate().activeSelf, Is.False);
        Assert.That(_controller.TryResetPuzzle(), Is.False);
        Assert.That(_controller.TryRotatePillar(_pillars[1]), Is.False);
    }

    [Test]
    public void PlayerCanReachEveryRelayAndPadAndLeaveAfterGateOpens()
    {
        HashSet<Vector2Int> reached = WalkableCells();
        for (int i = 1; i <= 5; i++)
        {
            bool canInteract = false;
            foreach (Vector2Int cell in reached)
            {
                Vector2 position = World(cell);
                if (Vector2.Distance(position, _pillars[i].InteractionTrigger.ClosestPoint(position)) <= 0.95f)
                { canInteract = true; break; }
            }
            Assert.That(canInteract, Is.True, "입구에서 걸어가 조작할 수 있어야 합니다: " + _pillars[i].PillarId);
        }
        Assert.That(reached.Contains(Cell(new Vector2(181f,8f))), Is.True, "초기화 발판까지 통행 가능");
        Assert.That(reached.Contains(Cell(new Vector2(199.75f,8.75f))), Is.False, "미완료 출구는 닫힘");
        Gate().SetActive(false);
        Physics2D.SyncTransforms();
        Assert.That(WalkableCells().Contains(Cell(new Vector2(199.75f,8.75f))), Is.True, "완료 후 오른쪽 통로로 이동 가능");
    }

    private HashSet<Vector2Int> WalkableCells()
    {
        // 기존 플레이어의 2×3 몸체로 0.25 단위 통행을 검사합니다. Trigger는 이동을 막지 않습니다.
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player.prefab");
        Vector2 size = player.GetComponent<BoxCollider2D>().size + new Vector2(0.02f,0.02f);
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        var hits = new List<Collider2D>();
        var cache = new Dictionary<Vector2Int,bool>();
        var reached = new HashSet<Vector2Int>();
        var pending = new Queue<Vector2Int>();
        // 서쪽의 기존 미로 문을 통과해 Beam 방에 들어온 위치에서 시작합니다.
        Vector2Int start = Cell(new Vector2(179.5f,8.5f));
        pending.Enqueue(start);
        reached.Add(start);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        while (pending.Count > 0)
        {
            Vector2Int current = pending.Dequeue();
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];
                if (next.x < 0 || next.x > 96 || next.y < 0 || next.y > 104 || reached.Contains(next)) continue;
                if (!cache.TryGetValue(next, out bool free))
                {
                    int count = Physics2D.OverlapBox(World(next), size, 0f, filter, hits);
                    free = true;
                    for (int hit = 0; hit < count; hit++)
                        if (hits[hit].transform.IsChildOf(_map.transform)) { free = false; break; }
                    cache.Add(next, free);
                }
                if (!free) continue;
                reached.Add(next);
                pending.Enqueue(next);
            }
        }
        return reached;
    }

    private Vector2 World(Vector2Int cell) => _map.transform.TransformPoint(new Vector3(176f + cell.x * 0.25f, -5.5f + cell.y * 0.25f, 0f));
    private static Vector2Int Cell(Vector2 local) => new Vector2Int(Mathf.RoundToInt((local.x - 176f) * 4f), Mathf.RoundToInt((local.y + 5.5f) * 4f));
    private GameObject Gate() => (GameObject)new SerializedObject(_controller).FindProperty("_exitObstacles").GetArrayElementAtIndex(0).objectReferenceValue;
}
