//=============================================================================
// File:        RuinPuzzleDataTests.cs
// Description: 기존 CSV·UI 부품의 실제 드래그·스냅·회전으로 유적 정답을 검증합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>정답 판정 함수를 강제로 호출하지 않고 기존 조작 경로를 재생합니다.</summary>
public sealed class RuinPuzzleDataTests
{
    private GameObject _dataObject;
    private GameObject _ui;
    private GameDataController _data;
    private CircleConstellationSettingsSO _settings;

    /// <summary>실제 GameRoot의 CSV와 기존 공용 UI 복사본을 준비합니다.</summary>
    [SetUp]
    public void SetUp()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab");
        _dataObject = Object.Instantiate(root.GetComponentInChildren<GameDataController>(true).gameObject);
        _data = _dataObject.GetComponent<GameDataController>();
        Assert.That(_data.TryInit(out GameDataCsvLoadError error), Is.True, error?.Message);
        _ui = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Puzzle/PuzzleUiRoot.prefab"));
        _settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CircleConstellationSettingsSO>("Assets/_Game/Data/Ruin/a_ConstellationSettings.asset"));
    }

    /// <summary>원본 에셋을 수정하지 않고 복사본을 정리합니다.</summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_ui);
        Object.DestroyImmediate(_dataObject);
        Object.DestroyImmediate(_settings);
    }

    /// <summary>바깥→안쪽 기본 ID와 12시 방향, 스냅 및 최초 이벤트를 확인합니다.</summary>
    [Test]
    public void DefaultConstellationsUseExplicitIdsAndReverseCsvOrder()
    {
        string[] answers = { "SCORPIO", "VIRGO", "TAURUS", "CANCER" };
        _data.Repository.TryGetPuzzle("CIRCLE_STAGE_01", out PuzzleDefinition definition);
        Assert.That(_settings.TryValidate(definition.CircleRings, out string error), Is.True, error);
        for (int i = 0; i < answers.Length; i++)
        {
            Assert.That(_settings.OuterToInner[i].AnswerId, Is.EqualTo(answers[i]));
            Assert.That(_settings.GetForRingOrder(3-i), Is.SameAs(_settings.OuterToInner[i]));
            Assert.That(_settings.OuterToInner[i].AlignmentDegrees, Is.EqualTo(90));
        }
        UiCirclePuzzleManager manager = Circle();
        List<UiCircleComponent> rings = Rings(manager);
        int events = 0;
        manager.OnPuzzleClearedEvent += () => events++;
        Assert.That(manager.IsClear, Is.False);
        for (int i = 0; i < rings.Count; i++)
        {
            CirclePattern.Symbol symbol = rings[i].Pattern.Symbols[0];
            Vector3 direction = rings[i].Pattern.GetDirection(symbol);
            float delta = 90 - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rings[i].SetVisualRotation(rings[i].transform.localEulerAngles.z + delta);
            rings[i].SnapToSegment();
            Assert.That(rings[i].Pattern.HasAlignedConstellation(_settings.GetForRingOrder(i).AnswerId, Vector3.up), Is.True);
        }
        manager.CheckClearCondition();
        manager.CheckClearCondition();
        Assert.That(manager.IsClear, Is.True);
        Assert.That(events, Is.EqualTo(1));
    }

    /// <summary>회전 오류·누락 참조·필수 ID·도달 불가능 방향은 클리어되지 않습니다.</summary>
    [TestCase("_answerId", "MISSING")]
    [TestCase("_rotationStepDegrees", "0")]
    [TestCase("_rotationStepDegrees", "47")]
    [TestCase("_initialRotationDegrees", "12")]
    [TestCase("_alignmentDegrees", "91")]
    [TestCase("_patternPrefab", "NULL")]
    public void InvalidDesignerSettingsCannotClear(string property, string value)
    {
        var serialized = new SerializedObject(_settings);
        SerializedProperty field = serialized.FindProperty("_outerToInner").GetArrayElementAtIndex(0).FindPropertyRelative(property);
        if (property == "_answerId") field.stringValue = value;
        else if (property == "_patternPrefab") field.objectReferenceValue = null;
        else field.floatValue = float.Parse(value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        LogAssert.Expect(LogType.Error, new Regex("\\[UiCirclePuzzleManager\\]"));
        UiCirclePuzzleManager manager = Circle();
        manager.CheckClearCondition();
        Assert.That(manager.IsClear, Is.False);
        Assert.That(Rings(manager), Is.Empty);
    }

    /// <summary>b·d를 원본 크기와 고정 시작·끝을 유지하면서 해결합니다.</summary>
    [TestCase("PIPE_STAGE_06", 3)]
    [TestCase("PIPE_STAGE_07", 4)]
    public void OriginalPipeDataSolvesWithActualDragAndRotation(string id, int width)
    {
        int[] moves = width == 3 ? new[] { 6,7,7,8,4,7,1,4,7,6,4,7,2,1,1,4 }
            : new[] { 8,12,4,8,0,4,1,0,10,14,9,10,8,9,4,8,10,11,9,10,8,9,9,13,
                0,4,4,8,8,9,12,8,8,4,4,0,0,1,9,8,8,4,4,0,10,9,13,12,12,8,8,4,9,8,11,10,14,13,13,9 };
        int[] rotations = width == 3 ? new[] { 4,0,7,2,8,0 } : new[] { 6,0,10,1,9,1,8,2,4,0,0,3,1,0 };
        PipeManager manager = _ui.GetComponentInChildren<PipeManager>(true);
        manager.Init(_data.Repository);
        manager.TrySetPuzzleId(id);
        manager.SetChapterIndex(1);
        manager.InitializePuzzle();
        Assert.That(manager.GetSpecificTile(width-1,width-1), Is.Not.Null);
        for (int i = 0; i < moves.Length; i += 2)
        {
            int from = moves[i], to = moves[i+1];
            PipeComponent tile = manager.GetSpecificTile(from/width,from%width);
            PipeComponent empty = manager.GetSpecificTile(to/width,to%width);
            Assert.That(empty.Data.type, Is.EqualTo(PipeType.Empty));
            Vector2 drag = empty.transform.localPosition - tile.transform.localPosition;
            manager.OnTileBeginDrag(tile, Vector2.zero);
            manager.OnTileDrag(drag);
            manager.OnTileDrag(drag);
            manager.OnTileEndDrag();
            Assert.That(manager.GetSpecificTile(to/width,to%width), Is.SameAs(tile));
        }
        for (int i = 0; i < rotations.Length; i += 2)
        {
            int index = rotations[i];
            PipeComponent tile = manager.GetSpecificTile(index/width,index%width);
            for (int turn = 0; turn < 4 && tile.Data.rotationIndex != rotations[i+1]; turn++) tile.Rotate();
        }
        Assert.That(manager.IsClear, Is.True);
    }

    /// <summary>문양 수·ID·그림·위치·각도·정답·방향·초기값·간격 편집 후 기존 드래그로 해결합니다.</summary>
    [Test]
    public void DesignerCanEditSymbolListAndSolveWithExistingDrag()
    {
        GameObject copy = Object.Instantiate(_settings.OuterToInner[0].PatternPrefab,_ui.transform);
        CirclePattern pattern = copy.GetComponent<CirclePattern>();
        for (int i = 1; i < pattern.Symbols.Count; i++) Object.DestroyImmediate(pattern.Symbols[i].icon.gameObject);
        RectTransform icon = pattern.Symbols[0].icon;
        icon.anchorMin = icon.anchorMax = new Vector2(.2f,.5f);
        icon.localRotation = Quaternion.Euler(0,0,23);
        Sprite replacement = _settings.OuterToInner[1].PatternPrefab.GetComponent<CirclePattern>().Symbols[1].icon.GetComponent<Image>().sprite;
        icon.GetComponent<Image>().sprite = replacement;
        SerializedObject symbols = new SerializedObject(pattern);
        symbols.FindProperty("_symbols").arraySize = 1;
        symbols.FindProperty("_symbols").GetArrayElementAtIndex(0).FindPropertyRelative("constellationId").stringValue = "CUSTOM_ID";
        symbols.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject settings = new SerializedObject(_settings);
        SerializedProperty ring = settings.FindProperty("_outerToInner").GetArrayElementAtIndex(0);
        ring.FindPropertyRelative("_patternPrefab").objectReferenceValue = copy;
        ring.FindPropertyRelative("_answerId").stringValue = "CUSTOM_ID";
        ring.FindPropertyRelative("_alignmentDegrees").floatValue = 0;
        ring.FindPropertyRelative("_rotationStepDegrees").floatValue = 90;
        ring.FindPropertyRelative("_initialRotationDegrees").floatValue = 90;
        settings.ApplyModifiedPropertiesWithoutUndo();
        UiCirclePuzzleManager manager = Circle();
        List<UiCircleComponent> rings = Rings(manager);
        for (int i = 0; i < rings.Count-1; i++)
        {
            Vector3 direction = rings[i].Pattern.GetDirection(rings[i].Pattern.Symbols[0]);
            float delta = 90 - Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
            rings[i].SetVisualRotation(rings[i].transform.localEulerAngles.z+delta);
            rings[i].SnapToSegment();
        }
        UiCircleComponent outer = rings[rings.Count-1];
        Assert.That(outer.Pattern.Symbols.Count, Is.EqualTo(1));
        Assert.That(outer.Pattern.Symbols[0].icon.GetComponent<Image>().sprite, Is.SameAs(replacement));
        Assert.That(outer.AnglePerSegment, Is.EqualTo(90));
        Vector2 center = RectTransformUtility.WorldToScreenPoint(null,outer.transform.position);
        var pointer = new PointerEventData(null) { position = center + Vector2.right*100 };
        outer.OnBeginDrag(pointer);
        pointer.position = center + Vector2.up*100;
        outer.OnDrag(pointer);
        outer.OnEndDrag(pointer);
        Assert.That(manager.IsClear, Is.True);
    }

    private UiCirclePuzzleManager Circle()
    {
        UiCirclePuzzleManager manager = _ui.GetComponentInChildren<UiCirclePuzzleManager>(true);
        manager.Init(_data.Repository);
        manager.TrySetPuzzleId("CIRCLE_STAGE_01");
        manager.SetConstellationSettings(_settings);
        manager.InitializePuzzle();
        return manager;
    }
    private static List<UiCircleComponent> Rings(UiCirclePuzzleManager manager) =>
        (List<UiCircleComponent>)typeof(UiCirclePuzzleManager).GetField("_spawnedRings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
}
