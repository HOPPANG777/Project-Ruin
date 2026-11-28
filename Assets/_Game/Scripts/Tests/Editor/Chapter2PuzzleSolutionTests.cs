//=============================================================================
// File:        Chapter2PuzzleSolutionTests.cs
// Description: 챕터2 신규 CSV 퍼즐(파이프 4·등불 3)을 실제 UI 매니저의 드래그·회전·클릭으로 해결합니다.
// History:     11/28/2026 + Created
//=============================================================================
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>해법은 별도 솔버(슬라이드 BFS + 회전 전수)로 구했고, 여기서는 실제 매니저가 받아들이는지만 본다.</summary>
public sealed class Chapter2PuzzleSolutionTests
{
    private GameObject _dataObject;
    private GameObject _ui;
    private GameDataController _data;

    [SetUp]
    public void SetUp()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab");
        _dataObject = Object.Instantiate(root.GetComponentInChildren<GameDataController>(true).gameObject);
        _data = _dataObject.GetComponent<GameDataController>();
        Assert.That(_data.TryInit(out GameDataCsvLoadError error), Is.True, error?.Message);
        _ui = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Puzzle/PuzzleUiRoot.prefab"));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_ui);
        Object.DestroyImmediate(_dataObject);
    }

    // moves: (출발 칸, 빈 도착 칸) 쌍의 나열. rotations: (최종 칸, 회전값) 쌍의 나열. 칸 번호 = row * 3 + column.
    [TestCase("PIPE_STAGE_08", new[] { 0, 3, 1, 0, 2, 1, 5, 2, 7, 8, 8, 5 }, new[] { 0, 3, 1, 1, 2, 0, 3, 1, 5, 0 })]
    [TestCase("PIPE_STAGE_09", new[] { 4, 5, 5, 2, 3, 4, 0, 3, 4, 5, 3, 4, 5, 8, 4, 5 }, new[] { 2, 0, 5, 0, 8, 1 })]
    [TestCase("PIPE_STAGE_10", new[] { 4, 3, 1, 4, 4, 7, 5, 4 }, new[] { 3, 2, 4, 0, 7, 1 })]
    [TestCase("PIPE_STAGE_11", new[] { 0, 1, 3, 0, 6, 3, 8, 7, 7, 6, 5, 8, 8, 7 }, new[] { 0, 3, 1, 1, 3, 1, 6, 2, 7, 1 })]
    public void NewPipePuzzlesStartUnsolvedAndAreSolvedByRealDragAndRotation(string id, int[] moves, int[] rotations)
    {
        PipeManager manager = _ui.GetComponentInChildren<PipeManager>(true);
        manager.Init(_data.Repository);
        manager.TrySetPuzzleId(id);
        manager.SetChapterIndex(2);
        manager.InitializePuzzle();
        int events = 0;
        manager.OnPuzzleClearedEvent += () => events++;
        Assert.That(manager.IsClear, Is.False, "처음부터 풀린 상태면 안 됩니다.");
        for (int i = 0; i < moves.Length; i += 2)
        {
            int from = moves[i], to = moves[i + 1];
            PipeComponent tile = manager.GetSpecificTile(from / 3, from % 3);
            PipeComponent empty = manager.GetSpecificTile(to / 3, to % 3);
            Assert.That(empty.Data.type, Is.EqualTo(PipeType.Empty), $"{from}->{to}");
            Vector2 drag = empty.transform.localPosition - tile.transform.localPosition;
            manager.OnTileBeginDrag(tile, Vector2.zero);
            manager.OnTileDrag(drag);
            manager.OnTileDrag(drag);
            manager.OnTileEndDrag();
            Assert.That(manager.GetSpecificTile(to / 3, to % 3), Is.SameAs(tile));
        }
        for (int i = 0; i < rotations.Length; i += 2)
        {
            PipeComponent tile = manager.GetSpecificTile(rotations[i] / 3, rotations[i] % 3);
            for (int turn = 0; turn < 4 && tile.Data.rotationIndex != rotations[i + 1]; turn++) tile.Rotate();
            Assert.That(tile.Data.rotationIndex, Is.EqualTo(rotations[i + 1]));
        }
        Assert.That(manager.IsClear, Is.True, id);
        Assert.That(events, Is.EqualTo(1));
    }

    /// <summary>원형판 3개: 처음엔 안 풀려 있고, 각 링의 정답 문양을 12시로 돌리면 클리어된다(실제 스냅 사용).</summary>
    [TestCase("CIRCLE_STAGE_03", 4)]
    [TestCase("CIRCLE_STAGE_04", 3)]
    [TestCase("CIRCLE_STAGE_05", 4)]
    public void NewCirclePuzzlesStartUnsolvedAndAreSolvedByAligningTheAnswerSymbols(string id, int ringCount)
    {
        CircleConstellationSettingsSO settings = AssetDatabase.LoadAssetAtPath<CircleConstellationSettingsSO>(
            $"Assets/_Game/Data/Chapter2/{id}_ConstellationSettings.asset");
        UiCirclePuzzleManager manager = _ui.GetComponentInChildren<UiCirclePuzzleManager>(true);
        manager.Init(_data.Repository);
        manager.TrySetPuzzleId(id);
        manager.SetChapterIndex(2);
        manager.SetConstellationSettings(settings);
        manager.InitializePuzzle();
        System.Collections.Generic.List<UiCircleComponent> rings = (System.Collections.Generic.List<UiCircleComponent>)
            typeof(UiCirclePuzzleManager).GetField("_spawnedRings", System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic).GetValue(manager);
        Assert.That(rings.Count, Is.EqualTo(ringCount));
        manager.CheckClearCondition();
        Assert.That(manager.IsClear, Is.False, "처음부터 풀린 상태면 안 됩니다.");
        for (int i = 0; i < rings.Count; i++)
        {
            string answer = settings.GetForRingOrder(i).AnswerId;
            CirclePattern.Symbol target = default;
            for (int s = 0; s < rings[i].Pattern.Symbols.Count; s++)
            {
                if (rings[i].Pattern.Symbols[s].constellationId == answer) target = rings[i].Pattern.Symbols[s];
            }
            Vector3 direction = rings[i].Pattern.GetDirection(target);
            float delta = 90f - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rings[i].SetVisualRotation(rings[i].transform.localEulerAngles.z + delta);
            rings[i].SnapToSegment();
            Assert.That(rings[i].Pattern.HasAlignedConstellation(answer, Vector3.up), Is.True, $"{id} ring {i} {answer}");
        }
        manager.CheckClearCondition();
        Assert.That(manager.IsClear, Is.True, id);
    }

    [TestCase("LIGHT_STAGE_02", new[] { 3, 7 })]
    [TestCase("LIGHT_STAGE_03", new[] { 2, 4, 7, 8, 9 })]
    [TestCase("LIGHT_STAGE_04", new[] { 1, 2, 5, 7, 8, 9 })]
    public void NewLightPuzzlesStartUnsolvedAndAreSolvedByRealClicks(string id, int[] clicks)
    {
        UiLightPuzzleManager manager = _ui.GetComponentInChildren<UiLightPuzzleManager>(true);
        manager.Init(_data.Repository);
        manager.TrySetPuzzleId(id);
        manager.SetChapterIndex(2);
        manager.InitializePuzzle();
        int events = 0;
        manager.OnPuzzleClearedEvent += () => events++;
        Assert.That(manager.IsClear, Is.False);
        for (int i = 0; i < clicks.Length; i++) manager.OnTileClicked(clicks[i]);
        Assert.That(manager.IsClear, Is.True, id);
        Assert.That(events, Is.EqualTo(1));
    }
}
