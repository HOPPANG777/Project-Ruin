using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CirclePuzzleTests
{
    private GameObject _root;
    private UiCirclePuzzleManager _manager;
    private List<UiCircleComponent> _rings;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Circle test");
        _manager = _root.AddComponent<UiCirclePuzzleManager>();
        _rings = (List<UiCircleComponent>)typeof(UiCirclePuzzleManager)
            .GetField("_spawnedRings", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_manager);

        for (int i = 0; i < 4; i++)
        {
            var pattern = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/_Game/Scripts/Puzzle/Circle/Temp/CirclePattern_{i}.prefab");
            Assert.That(pattern, Is.Not.Null);
            var go = new GameObject($"Ring {i}", typeof(RectTransform), typeof(UiCircleComponent));
            go.transform.SetParent(_root.transform, false);
            ((RectTransform)go.transform).sizeDelta = Vector2.one * 600;
            var ring = go.GetComponent<UiCircleComponent>();
            ring.InitRingState(8, 0, pattern, _manager);
            Assert.That(ring.Pattern, Is.Not.Null);
            _rings.Add(ring);
        }
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    [TestCase(0, 0, 45, 45, true)]
    [TestCase(0, 0, 45, -90, true)]
    [TestCase(90, 90, 135, 135, false)]
    [TestCase(180, 180, 225, 225, false)]
    [TestCase(0, 0, 0, 0, false)]
    [TestCase(0, 0, 45, 0, false)]
    [TestCase(0, 90, 45, 45, false)]
    [TestCase(0, 0, 45, 225, false)]
    public void OnlyColorAlignmentUnderPointerClears(float a, float b, float c, float d, bool expected)
    {
        float[] angles = { a, b, c, d };
        for (int i = 0; i < 4; i++) Rotate(i, angles[i]);
        int events = 0;
        _manager.OnPuzzleClearedEvent += () => events++;
        _manager.CheckClearCondition();
        _manager.CheckClearCondition();
        Assert.That(_manager.IsClear, Is.EqualTo(expected));
        Assert.That(events, Is.EqualTo(expected ? 1 : 0));
    }

    [Test]
    public void ExplicitPointerDeterminesTargetDirection()
    {
        var pointer = new GameObject("Pointer", typeof(RectTransform));
        pointer.transform.SetParent(_root.transform, false);
        pointer.transform.localPosition = Vector3.left * 320;
        typeof(UiCirclePuzzleManager).GetField("_targetPointer", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(_manager, pointer.GetComponent<RectTransform>());
        Rotate(0, 90);
        Rotate(1, 90);
        Rotate(2, 135);
        Rotate(3, 135);
        _manager.CheckClearCondition();
        Assert.That(_manager.IsClear, Is.True);
    }

    [Test]
    public void MatchingColorIsNotHardcodedToYellow()
    {
        foreach (UiCircleComponent ring in _rings)
        {
            var serialized = new SerializedObject(ring.Pattern);
            var symbols = serialized.FindProperty("_symbols");
            for (int i = 0; i < symbols.arraySize; i++)
            {
                var color = symbols.GetArrayElementAtIndex(i).FindPropertyRelative("color");
                if (color.intValue == (int)CirclePattern.SymbolColor.Yellow)
                    color.intValue = (int)CirclePattern.SymbolColor.Red;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        Rotate(2, 45);
        Rotate(3, 45);
        _manager.CheckClearCondition();
        Assert.That(_manager.IsClear, Is.True);
    }

    [Test]
    public void ReenablePreservesSnappedRotation()
    {
        Rotate(0, 135);
        _rings[0].gameObject.SetActive(false);
        _rings[0].gameObject.SetActive(true);
        Assert.That(Mathf.DeltaAngle(_rings[0].transform.localEulerAngles.z, 135), Is.EqualTo(0).Within(0.01));
    }

    [Test]
    public void ClearEventCanCompleteAWorldPuzzleOnce()
    {
        var go = new GameObject("World puzzle", typeof(BoxCollider2D), typeof(UiPuzzleInteractable));
        go.transform.SetParent(_root.transform);
        var requester = go.GetComponent<UiPuzzleInteractable>();
        requester.SendMessage("Awake");
        var notify = typeof(UiPuzzleInteractable).GetMethod("NotifyPuzzleCleared",
            BindingFlags.Instance | BindingFlags.NonPublic);
        _manager.OnPuzzleClearedEvent += () => notify.Invoke(requester, null);
        Rotate(2, 45);
        Rotate(3, -90);
        _manager.CheckClearCondition();
        Assert.That(requester.IsSolved, Is.True);
        Assert.That(go.GetComponent<BoxCollider2D>().enabled, Is.False);
    }

    private void Rotate(int ring, float angle)
    {
        _rings[ring].SetVisualRotation(angle);
        _rings[ring].SnapToSegment();
    }
}
