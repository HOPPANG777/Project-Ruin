//=============================================================================
// File:        Chapter1PuzzleDataTests.cs
// Description: 기존 공용 UI와 실제 CSV로 허용된 드래그·회전 및 라이트 클릭을 재생합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>새 퍼즐 규칙 없이 기존 매니저에서 검증된 해법을 실행합니다.</summary>
public sealed class Chapter1PuzzleDataTests
{
    private GameObject _dataObject;
    private GameObject _ui;
    private GameDataController _data;

    /// <summary>실제 GameRoot의 CSV 참조와 공용 PuzzleUiRoot를 복사합니다.</summary>
    [SetUp]
    public void SetUp()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab");
        _dataObject = Object.Instantiate(root.GetComponentInChildren<GameDataController>(true).gameObject);
        _data = _dataObject.GetComponent<GameDataController>();
        Assert.That(_data.TryInit(out GameDataCsvLoadError error), Is.True, error?.Message);
        _ui = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Puzzle/PuzzleUiRoot.prefab"));
    }

    /// <summary>검증에 만든 UI와 CSV 저장소만 제거합니다.</summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_ui);
        Object.DestroyImmediate(_dataObject);
    }

    /// <summary>세 파이프 데이터의 고정 시작·끝과 실제 조작으로 클리어합니다.</summary>
    [TestCase("PIPE_STAGE_03", new int[] { 1, 4, 4, 5, 7, 4, 4, 1, 8, 7, 7, 4 })]
    [TestCase("PIPE_STAGE_04", new int[] { 5, 8, 4, 5, 3, 4, 4, 1, 7, 4, 8, 7 })]
    [TestCase("PIPE_STAGE_05", new int[] { 3, 6, 4, 3, 5, 8, 7, 4, 4, 5, 6, 7, 7, 4 })]
    public void ActualDragAndRotationRulesSolveApprovedPipeData(string id, int[] moves)
    {
        PipeManager manager = _ui.GetComponentInChildren<PipeManager>(true);
        manager.Init(_data.Repository);
        manager.TrySetPuzzleId(id);
        manager.SetChapterIndex(1);
        manager.InitializePuzzle();
        int events = 0;
        manager.OnPuzzleClearedEvent += () => events++;
        Assert.That(manager.IsClear, Is.False);
        for (int i = 0; i < moves.Length; i += 2)
        {
            int from = moves[i], to = moves[i + 1];
            PipeComponent tile = manager.GetSpecificTile(from / 3, from % 3);
            PipeComponent empty = manager.GetSpecificTile(to / 3, to % 3);
            Assert.That(empty.Data.type, Is.EqualTo(PipeType.Empty));
            Assert.That(Mathf.Abs(from / 3 - to / 3) + Mathf.Abs(from % 3 - to % 3), Is.EqualTo(1));
            Vector2 drag = empty.transform.localPosition - tile.transform.localPosition;
            manager.OnTileBeginDrag(tile, Vector2.zero);
            manager.OnTileDrag(drag);
            manager.OnTileDrag(drag);
            manager.OnTileEndDrag();
            Assert.That(manager.GetSpecificTile(to / 3, to % 3), Is.SameAs(tile));
        }
        RotateTo(manager.GetSpecificTile(0, 1), 0);
        RotateTo(manager.GetSpecificTile(1, 1), id == "PIPE_STAGE_04" ? 0 : 2);
        if (id == "PIPE_STAGE_04")
        {
            Assert.That(manager.GetSpecificTile(2, 0).Data.rotationIndex, Is.EqualTo(1));
            RotateTo(manager.GetSpecificTile(2, 1), 1);
        }
        else if (id == "PIPE_STAGE_05") RotateTo(manager.GetSpecificTile(1, 2), 2);
        Assert.That(manager.IsClear, Is.True);
        Assert.That(events, Is.EqualTo(1));
    }

    /// <summary>첨부 이미지와 다른 원본 LIGHT_STAGE_01을 변경 없이 해결합니다.</summary>
    [Test]
    public void UnchangedLightDataClearsWithItsExistingClickRules()
    {
        UiLightPuzzleManager manager = _ui.GetComponentInChildren<UiLightPuzzleManager>(true);
        manager.Init(_data.Repository);
        manager.TrySetPuzzleId("LIGHT_STAGE_01");
        manager.SetChapterIndex(1);
        manager.InitializePuzzle();
        int[] clicks = { 2, 3, 4, 8 };
        int events = 0;
        manager.OnPuzzleClearedEvent += () => events++;
        for (int i = 0; i < clicks.Length; i++) manager.OnTileClicked(clicks[i]);
        Assert.That(manager.IsClear, Is.True);
        Assert.That(events, Is.EqualTo(1));
    }

    private static void RotateTo(PipeComponent tile, int rotation)
    {
        for (int i = 0; i < 4 && tile.Data.rotationIndex != rotation; i++) tile.Rotate();
        Assert.That(tile.Data.rotationIndex, Is.EqualTo(rotation));
    }
}
