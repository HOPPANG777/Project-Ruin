//=============================================================================
// File:        RuinLayoutTests.cs
// Description: 유적의 실제 부품 연결, 광선 경로와 플레이어 충돌 범위를 검증합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>원본 지형을 바꾸지 않고 실제 프리팹 복사본에서 검사합니다.</summary>
public sealed class RuinLayoutTests
{
    private GameObject _map;
    private Transform _first;
    private Transform _second;
    private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>기존 맵과 겹치지 않는 실제 물리 배치를 준비합니다.</summary>
    [SetUp]
    public void SetUp()
    {
        _map = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Map/Grid_1.prefab"));
        _map.transform.position = new Vector3(10000,10000,0);
        _first = _map.transform.Find("Chapter_1/Chapter1_ruin/Ruin1F_Puzzles");
        _second = _map.transform.Find("Chapter_1/Chapter1_ruin2f/Ruin2F_Puzzles");
        TilemapCollider2D[] tiles = _map.GetComponentsInChildren<TilemapCollider2D>();
        for (int i = 0; i < tiles.Length; i++) tiles[i].ProcessTilemapChanges();
        Physics2D.SyncTransforms();
    }

    /// <summary>검증 복사본만 제거합니다.</summary>
    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_map);

    /// <summary>모든 배치 ID·문 연결과 기존 프리팹 출처를 확인합니다.</summary>
    [Test]
    public void IndependentDoorsAndPersistenceIdsAreWired()
    {
        var ids = new HashSet<string>();
        MonoBehaviour[] behaviours = _map.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (!(behaviours[i] is ISceneStateContributor state)) continue;
            string id = state is UiPuzzleSceneState ? new SerializedObject(behaviours[i]).FindProperty("_objectId").stringValue : state.ObjectId;
            Assert.That(ids.Add(id), Is.True, id);
        }
        UiPuzzleSceneState[] ui = _first.GetComponentsInChildren<UiPuzzleSceneState>();
        Assert.That(ui.Length, Is.EqualTo(3));
        ProgressController progress = _map.AddComponent<ProgressController>();
        for (int i = 0; i < ui.Length; i++) ui[i].Init(progress,null,true);
        for (int i = 0; i < ui.Length; i++)
        {
            Assert.That(ui[i].StageNumber, Is.EqualTo(3));
            GameObject gate = Door(ui[i]);
            Assert.That(ui[i].transform.IsChildOf(gate.transform), Is.False);
            Assert.That(gate.GetComponent<Collider2D>().isTrigger, Is.False);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(ui[i].GetComponent<UiPuzzleInteractable>()), Is.Not.Null);
            ui[i].ApplyState("{\"Solved\":true}");
            Assert.That(gate.activeSelf, Is.False);
            for (int j = i+1; j < ui.Length; j++) Assert.That(Door(ui[j]).activeSelf, Is.True);
        }
        MazePressurePlate[] plates = _map.GetComponentsInChildren<MazePressurePlate>();
        Assert.That(plates.Length, Is.EqualTo(4));
        for (int i = 0; i < plates.Length; i++)
        {
            plates[i].PrepareSceneState();
            int events = 0;
            plates[i].Activated += () => events++;
            plates[i].ApplyState("true");
            plates[i].ApplyState("true");
            Assert.That(plates[i].IsActivated, Is.True);
            Assert.That(events, Is.Zero);
            SerializedObject data = new SerializedObject(plates[i]);
            GameObject gate = (GameObject)data.FindProperty("_closedDoorObject").objectReferenceValue;
            Assert.That(gate.activeSelf, Is.False);
            Assert.That(((Collider2D)data.FindProperty("_doorBlocker").objectReferenceValue).enabled, Is.False);
            Assert.That(plates[i].transform.IsChildOf(gate.transform), Is.False);
        }
    }

    /// <summary>끝 기둥 도달로 접근문만 열고 발판을 따로 밟아야 보상문이 열립니다.</summary>
    [Test]
    public void BeamRouteIsSolvableWithObstacleAndOnlyOpensAccessDoor()
    {
        BeamPuzzleController beam = _second.GetComponentInChildren<BeamPuzzleController>();
        var issues = new List<string>();
        beam.CollectValidationIssues(issues);
        Assert.That(issues, Is.Empty, string.Join(" / ",issues));
        SerializedObject data = new SerializedObject(beam);
        SerializedProperty array = data.FindProperty("_pillars");
        Assert.That(array.arraySize, Is.EqualTo(7));
        beam.InitializePuzzle();
        Assert.That(beam.BeamActive, Is.False);
        var first = (BeamPillarView)array.GetArrayElementAtIndex(1).objectReferenceValue;
        Assert.That(beam.TryRotatePillar(first), Is.True);
        Assert.That(first.RotationDegrees, Is.EqualTo(0).Within(0.01));
        Assert.That(beam.BeamActive, Is.True);
        Assert.That(beam.TryResetPuzzle(), Is.True);
        for (int i = 1; i < 6; i++)
            Assert.That(beam.TryRotatePillar((BeamPillarView)array.GetArrayElementAtIndex(i).objectReferenceValue), Is.True);
        Assert.That(beam.IsClear, Is.True);
        for (int i = 0; i < 7; i++)
        {
            var pillar = (BeamPillarView)array.GetArrayElementAtIndex(i).objectReferenceValue;
            Assert.That(pillar.IsConnected, Is.True);
            Assert.That(beam.TryRotatePillar(pillar), Is.False);
        }
        Assert.That(beam.TryResetPuzzle(), Is.False);
        Assert.That(_second.Find("g_노란장치_접근문").gameObject.activeSelf, Is.False);
        Assert.That(_second.Find("2F_보상방문").gameObject.activeSelf, Is.True);
        MazePressurePlate plate = _second.GetComponentInChildren<MazePressurePlate>();
        plate.PrepareSceneState();
        GameObject player = new GameObject("PlatePlayer",typeof(UnitPlayer),typeof(BoxCollider2D));
        typeof(MazePressurePlate).GetMethod("OnTriggerEnter2D",PRIVATE).Invoke(plate,new object[] { player.GetComponent<Collider2D>() });
        Object.DestroyImmediate(player);
        Assert.That(_second.Find("2F_보상방문").gameObject.activeSelf, Is.False);
        Assert.That(_first.GetComponentInChildren<UiPuzzleInteractable>().IsSolved, Is.False);
        LineRenderer[] lines = beam.GetComponentsInChildren<LineRenderer>();
        Assert.That(lines.Length, Is.EqualTo(6));
        for (int i = 0; i < lines.Length; i++) Assert.That(lines[i].enabled, Is.True);
        Collider2D obstacle = (Collider2D)data.FindProperty("_fixedObstacles").GetArrayElementAtIndex(0).objectReferenceValue;
        Vector2 origin = obstacle.bounds.center + Vector3.left*5;
        RaycastHit2D hit = Physics2D.Raycast(origin,Vector2.right,10,1<<9);
        Assert.That(hit.collider, Is.SameAs(obstacle));
    }

    /// <summary>실제 플레이어 몸체로 양층 도착이 벽·기둥·발판·트리거와 겹치지 않는지 검사합니다.</summary>
    [Test]
    public void StairArrivalsAreOutsideEveryColliderAndResetPad()
    {
        PositionTransitionPad[] pads = _map.GetComponentsInChildren<PositionTransitionPad>();
        Assert.That(pads.Length, Is.EqualTo(4));
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player.prefab");
        Vector2 size = player.GetComponent<BoxCollider2D>().size;
        for (int i = 0; i < pads.Length; i++)
        {
            Transform arrival = (Transform)new SerializedObject(pads[i]).FindProperty("_destination").objectReferenceValue;
            Collider2D[] hits = Physics2D.OverlapBoxAll(arrival.position,size,0);
            for (int j = 0; j < hits.Length; j++) Assert.Fail("도착 중첩: " + arrival.name + " / " + hits[j].name);
        }
    }

    /// <summary>표시 전용 보상에 충돌·획득 컴포넌트를 추가하지 않았는지 확인합니다.</summary>
    [Test]
    public void RewardMarkersOnlyHaveTransformsAndExistingSpriteRenderers()
    {
        SpriteRenderer[] sprites = _map.GetComponentsInChildren<SpriteRenderer>(true);
        int markers = 0;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (!sprites[i].name.EndsWith("위치표시")) continue;
            markers++;
            Assert.That(sprites[i].sprite, Is.Not.Null);
            Assert.That(sprites[i].GetComponents<Component>().Length, Is.EqualTo(2));
        }
        Assert.That(markers, Is.EqualTo(5));
    }
    private static GameObject Door(UiPuzzleSceneState state) =>
        (GameObject)new SerializedObject(state).FindProperty("_closedDoor").objectReferenceValue;
}
