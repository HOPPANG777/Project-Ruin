//=============================================================================
// File:        RuinAccessTests.cs
// Description: 기존 지형과 실제 플레이어 크기로 문 양측의 통행을 검사합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>PD의 미래 미로 대신 현재 지형에서만 닫힘·열림 통행을 확인합니다.</summary>
public sealed class RuinAccessTests
{
    private GameObject _map;
    private Vector2 _playerSize;
    private readonly List<Collider2D> _hits = new List<Collider2D>();

    /// <summary>실제 플레이어 프리팹의 몸체와 Tilemap 충돌을 준비합니다.</summary>
    [SetUp]
    public void SetUp()
    {
        _map = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Map/Grid_1.prefab"));
        _map.transform.position = new Vector3(10000,10000,0);
        _playerSize = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player.prefab").GetComponent<BoxCollider2D>().size;
        TilemapCollider2D[] tiles = _map.GetComponentsInChildren<TilemapCollider2D>();
        for (int i = 0; i < tiles.Length; i++) tiles[i].ProcessTilemapChanges();
        Physics2D.SyncTransforms();
    }

    /// <summary>검증 복사본만 제거합니다.</summary>
    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_map);

    /// <summary>현재 통로에서 입장문이 막고 자기 퍼즐 복원으로만 통행이 열립니다.</summary>
    [TestCase("b",2364,358,2348,377)]
    [TestCase("d",2412,358,2427,384)]
    [TestCase("a",2385,358.5f,2367,403)]
    public void CurrentFloorOneEntrancePassagesAreGated(string prefix, float x, float y, float targetX, float targetY)
    {
        Tilemap floor = _map.transform.Find("Chapter_1/Chapter1_ruin").GetComponent<Tilemap>();
        UiPuzzleSceneState[] states = floor.GetComponentsInChildren<UiPuzzleSceneState>();
        UiPuzzleSceneState state = null;
        for (int i = 0; i < states.Length; i++) if (states[i].name.StartsWith(prefix+"_")) state=states[i];
        Assert.That(state, Is.Not.Null);
        Vector2 from = new Vector2(x,y), to = new Vector2(targetX,targetY);
        Assert.That(CanReach(floor,from,to), Is.False,"현재 닫힌 입장문");
        state.ApplyState("{\"Solved\":true}");
        Assert.That(CanReach(floor,from,to), Is.True,"현재 지형의 입장→발판 통행; PD 미로 완성 뒤 재검증 필요");
    }

    /// <summary>접근문과 보상문을 각각 열어야 다음 구역으로 통행할 수 있습니다.</summary>
    [Test]
    public void FloorTwoAccessAndRewardDoorsBlockSeparatePassages()
    {
        Tilemap floor = _map.transform.Find("Chapter_1/Chapter1_ruin2f").GetComponent<Tilemap>();
        Transform group = floor.transform.Find("Ruin2F_Puzzles");
        Vector2 from = new Vector2(775,-635), yellow = new Vector2(787,-635), blue = new Vector2(801,-635);
        Assert.That(CanReach(floor,from,yellow), Is.False);
        group.Find("g_노란장치_접근문").gameObject.SetActive(false);
        Assert.That(CanReach(floor,from,yellow), Is.True);
        Assert.That(CanReach(floor,yellow,blue), Is.False);
        MazePressurePlate plate = group.GetComponentInChildren<MazePressurePlate>();
        plate.ApplyState("true");
        Assert.That(CanReach(floor,yellow,blue), Is.True);
    }

    /// <summary>현재 보상·계단 통로도 노란 장치에 연결된 문만으로 열립니다.</summary>
    [TestCase("c",2348,377,2348,386.5f)]
    [TestCase("e",2427,384,2435.5f,384)]
    [TestCase("f",2367,403,2413.5f,423.5f)]
    public void YellowDeviceDoorsGateCurrentRewardAndStairPassages(string prefix, float x, float y, float targetX, float targetY)
    {
        Tilemap floor = _map.transform.Find("Chapter_1/Chapter1_ruin").GetComponent<Tilemap>();
        MazePressurePlate[] plates = floor.GetComponentsInChildren<MazePressurePlate>();
        MazePressurePlate plate = null;
        for (int i = 0; i < plates.Length; i++) if (plates[i].name.StartsWith(prefix+"_")) plate=plates[i];
        Assert.That(plate, Is.Not.Null);
        Vector2 from=new Vector2(x,y), to=new Vector2(targetX,targetY);
        Assert.That(CanReach(floor,from,to), Is.False);
        plate.ApplyState("true");
        Assert.That(CanReach(floor,from,to), Is.True,"현재 보상·계단 통행; PD 미로 완성 뒤 재검증 필요");
    }

    /// <summary>계단 도착에서 모든 중간 기둥의 기존 입력 Trigger 옆까지 통행할 수 있습니다.</summary>
    [Test]
    public void EveryRelayCanBeReachedFromStairArrivalOnCurrentFloorTwo()
    {
        Tilemap floor = _map.transform.Find("Chapter_1/Chapter1_ruin2f").GetComponent<Tilemap>();
        BeamPillarView[] pillars=floor.GetComponentsInChildren<BeamPillarView>();
        for (int i = 0; i < pillars.Length; i++)
        {
            if (!pillars[i].IsRotatable) continue;
            Vector2 local=floor.transform.InverseTransformPoint(pillars[i].transform.position)+Vector3.left*1.5f;
            Assert.That(CanReach(floor,new Vector2(755,-635),local), Is.True,pillars[i].name);
            Vector2 world=floor.transform.TransformPoint(local);
            Assert.That(Vector2.Distance(world,pillars[i].InteractionTrigger.ClosestPoint(world)), Is.LessThanOrEqualTo(1));
        }
    }

    private bool CanReach(Tilemap floor, Vector2 from, Vector2 to)
    {
        Physics2D.SyncTransforms();
        var pending = new Queue<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        Vector2Int start = Vector2Int.RoundToInt(from*2), target = Vector2Int.RoundToInt(to*2);
        pending.Enqueue(start);
        seen.Add(start);
        Vector2Int[] directions = { Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left };
        ContactFilter2D filter = new ContactFilter2D { useTriggers=false };
        while (pending.Count>0)
        {
            Vector2Int current = pending.Dequeue();
            if (current==target) return true;
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next=current+directions[i];
                if (next.x<Mathf.Min(start.x,target.x)-24 || next.x>Mathf.Max(start.x,target.x)+24
                    || next.y<Mathf.Min(start.y,target.y)-24 || next.y>Mathf.Max(start.y,target.y)+24 || !seen.Add(next)) continue;
                Vector2 world=floor.transform.TransformPoint((Vector2)next/2);
                if (floor.GetTile(floor.WorldToCell(world))==null) continue;
                if (Physics2D.OverlapBox(world,_playerSize,0,filter,_hits)>0) continue;
                pending.Enqueue(next);
            }
        }
        return false;
    }
}
