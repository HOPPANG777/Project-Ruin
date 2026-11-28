//=============================================================================
// File:        Chapter2LayoutTests.cs
// Description: Grid_2의 챕터2 퍼즐 16개가 CSV·저장 ID·문·별자리·광선 경로와 맞게 연결됐는지 검사합니다.
// History:     11/28/2026 + Created
//=============================================================================
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>배치 구성이 코드에 기대는 연결(ID·참조·정답 설정)만 검사한다. 지형은 검사하지 않는다.</summary>
public sealed class Chapter2LayoutTests
{
    private const string GridPath = "Assets/_Game/Prefabs/Map/Grid_2.prefab";
    private GameObject _grid;
    private GameObject _dataObject;
    private GameDataController _data;

    [SetUp]
    public void SetUp()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab");
        _dataObject = Object.Instantiate(root.GetComponentInChildren<GameDataController>(true).gameObject);
        _data = _dataObject.GetComponent<GameDataController>();
        Assert.That(_data.TryInit(out GameDataCsvLoadError error), Is.True, error?.Message);
        _grid = PrefabUtility.LoadPrefabContents(GridPath);
    }

    [TearDown]
    public void TearDown()
    {
        PrefabUtility.UnloadPrefabContents(_grid);
        Object.DestroyImmediate(_dataObject);
    }

    private T[] InChapter2<T>() where T : Component
    {
        List<T> found = new List<T>();
        T[] all = _grid.GetComponentsInChildren<T>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (InGroup(all[i].transform)) found.Add(all[i]);
        }
        return found.ToArray();
    }

    private static bool InGroup(Transform t)
    {
        for (; t != null; t = t.parent) if (t.name.StartsWith("Ch2_Puzzles")) return true;
        return false;
    }

    /// <summary>마을 6개(UI) + 유적 4개(UI)가 있고 모두 CSV 퍼즐과 종류가 같다.</summary>
    [Test]
    public void EveryUiPuzzleUsesAnExistingCsvPuzzleOfTheSameType()
    {
        UiPuzzleInteractable[] puzzles = InChapter2<UiPuzzleInteractable>();
        Assert.That(puzzles.Length, Is.EqualTo(10));
        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < puzzles.Length; i++)
        {
            Assert.That(_data.Repository.TryGetPuzzle(puzzles[i].PuzzleId, out PuzzleDefinition definition), Is.True, puzzles[i].PuzzleId);
            Assert.That(definition.PuzzleType, Is.EqualTo(puzzles[i].PuzzleType), puzzles[i].PuzzleId);
            Assert.That(puzzles[i].ChapterIndex, Is.EqualTo(2));
            Assert.That(ids.Add(puzzles[i].PuzzleId), Is.True, "퍼즐 ID 중복 " + puzzles[i].PuzzleId);
        }
    }

    /// <summary>모든 UI 퍼즐은 고유 저장 ID와 자기 문을 가진다. 문은 다른 퍼즐의 조작대와 무관하다.</summary>
    [Test]
    public void EveryUiPuzzleHasUniqueSaveIdAndOwnDoor()
    {
        UiPuzzleSceneState[] states = InChapter2<UiPuzzleSceneState>();
        Assert.That(states.Length, Is.EqualTo(10));
        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < states.Length; i++)
        {
            SerializedObject so = new SerializedObject(states[i]);
            string stateId = so.FindProperty("_objectId").stringValue;
            Assert.That(ids.Add(stateId), Is.True, "저장 ID 중복 " + stateId);
            Assert.That(so.FindProperty("_stageNumber").intValue, Is.EqualTo(Ch2Util.StageNumber));
            GameObject door = (GameObject)so.FindProperty("_closedDoor").objectReferenceValue;
            Assert.That(door, Is.Not.Null, stateId);
            Assert.That(door.GetComponent<BoxCollider2D>(), Is.Not.Null, "문에 충돌체가 필요합니다.");
            Assert.That(states[i].transform.IsChildOf(door.transform), Is.False, "조작대는 문의 자식이면 안 됩니다.");
        }
    }

    /// <summary>원형판 3개는 CSV 링 수·순서와 별자리 설정이 코드의 검증을 통과한다.</summary>
    [Test]
    public void CircleConstellationSettingsPassRuntimeValidation()
    {
        UiPuzzleInteractable[] puzzles = InChapter2<UiPuzzleInteractable>();
        int checkedCount = 0;
        for (int i = 0; i < puzzles.Length; i++)
        {
            if (puzzles[i].PuzzleType != PuzzleType.PUZZLE_TYPE_CIRCLE) continue;
            SerializedObject so = new SerializedObject(puzzles[i]);
            CircleConstellationSettingsSO settings = (CircleConstellationSettingsSO)so.FindProperty("_constellationSettings").objectReferenceValue;
            Assert.That(settings, Is.Not.Null, puzzles[i].PuzzleId);
            _data.Repository.TryGetPuzzle(puzzles[i].PuzzleId, out PuzzleDefinition definition);
            Assert.That(settings.TryValidate(definition.CircleRings, out string error), Is.True, puzzles[i].PuzzleId + ": " + error);
            checkedCount++;
        }
        Assert.That(checkedCount, Is.EqualTo(3));
    }

    /// <summary>광선 퍼즐 2개: 기둥 ID 고유, 시작·끝 1개씩, 각 구간이 다음 기둥을 향하고 다른 기둥을 가리지 않는다.</summary>
    [Test]
    public void BeamRoutesReachTheEndPillarWithoutOtherPillarsInTheWay()
    {
        BeamPuzzleController[] beams = InChapter2<BeamPuzzleController>();
        Assert.That(beams.Length, Is.EqualTo(2));
        for (int b = 0; b < beams.Length; b++)
        {
            SerializedObject so = new SerializedObject(beams[b]);
            SerializedProperty pillars = so.FindProperty("_pillars");
            float maxDistance = so.FindProperty("_maxBeamDistance").floatValue;
            int count = pillars.arraySize;
            Assert.That(count, Is.GreaterThanOrEqualTo(3));
            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                SerializedObject pillar = new SerializedObject(pillars.GetArrayElementAtIndex(i).objectReferenceValue);
                string id = pillar.FindProperty("_settings._id").stringValue;
                Assert.That(ids.Add(id), Is.True, "기둥 ID 중복 " + id);
                Assert.That(pillar.FindProperty("_settings._type").intValue,
                    Is.EqualTo(i == 0 ? (int)BeamPillarType.BEAM_PILLAR_START : i == count - 1 ? (int)BeamPillarType.BEAM_PILLAR_END : (int)BeamPillarType.BEAM_PILLAR_TWO_WAY));
                if (i == count - 1) continue;
                Vector2 from = ((Component)pillars.GetArrayElementAtIndex(i).objectReferenceValue).transform.position;
                Vector2 to = ((Component)pillars.GetArrayElementAtIndex(i + 1).objectReferenceValue).transform.position;
                float angle = pillar.FindProperty("_settings._outputAnglesDegrees").GetArrayElementAtIndex(0).floatValue;
                Vector2 expected = (to - from).normalized;
                Assert.That(Vector2.Angle(expected, new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad))), Is.LessThan(0.1f));
                Assert.That((to - from).magnitude, Is.LessThan(maxDistance), "광선 최대 길이 초과 " + id);
                for (int other = 0; other < count; other++)
                {
                    if (other == i || other == i + 1) continue;
                    Vector2 point = ((Component)pillars.GetArrayElementAtIndex(other).objectReferenceValue).transform.position;
                    Assert.That(DistanceToSegment(point, from, to), Is.GreaterThan(1.6f), $"{id} 구간이 다른 기둥에 가려집니다.");
                }
            }
            Assert.That(so.FindProperty("_exitObstacles").arraySize, Is.EqualTo(1));
            Assert.That(so.FindProperty("_resetPads").arraySize, Is.EqualTo(1));
        }
    }

    /// <summary>발판 퍼즐 4개: 돌 수만큼 돌 발판이 있고 돌·발판은 컨트롤러에 모두 등록돼 있다.</summary>
    [Test]
    public void MazeControllersRegisterAllPlatesAndStones()
    {
        MazePlatePuzzleController[] mazes = InChapter2<MazePlatePuzzleController>();
        Assert.That(mazes.Length, Is.EqualTo(4));
        for (int i = 0; i < mazes.Length; i++)
        {
            SerializedObject so = new SerializedObject(mazes[i]);
            int plates = so.FindProperty("_plates").arraySize, stones = so.FindProperty("_stones").arraySize;
            Assert.That(plates, Is.EqualTo(2), mazes[i].ObjectId);
            Assert.That(stones, Is.GreaterThanOrEqualTo(1).And.LessThanOrEqualTo(plates), mazes[i].ObjectId);
            Assert.That(so.FindProperty("_unsolvedResultObject").objectReferenceValue, Is.Not.Null);
            Assert.That(so.FindProperty("_solvedResultObject").objectReferenceValue, Is.Not.Null);
            for (int s = 0; s < stones; s++)
            {
                SerializedObject stone = new SerializedObject(so.FindProperty("_stones").GetArrayElementAtIndex(s).objectReferenceValue);
                Assert.That(stone.FindProperty("_puzzleId").stringValue, Is.EqualTo(mazes[i].ObjectId));
            }
        }
    }

    /// <summary>모든 챕터2 저장 ID(UI·광선·발판 미로)는 서로 겹치지 않는다.</summary>
    [Test]
    public void AllSaveIdsAreUnique()
    {
        HashSet<string> ids = new HashSet<string>();
        MonoBehaviour[] contributors = InChapter2<MonoBehaviour>();
        for (int i = 0; i < contributors.Length; i++)
        {
            bool isContributor = contributors[i] is UiPuzzleSceneState || contributors[i] is BeamPuzzleController
                || contributors[i] is MazePlatePuzzleController;
            if (!isContributor) continue;
            string id = new SerializedObject(contributors[i]).FindProperty("_objectId").stringValue;
            Assert.That(string.IsNullOrWhiteSpace(id), Is.False);
            Assert.That(ids.Add(id), Is.True, "저장 ID 중복 " + id);
        }
        Assert.That(ids.Count, Is.EqualTo(16));
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }
}
