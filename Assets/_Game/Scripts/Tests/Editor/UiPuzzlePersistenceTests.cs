//=============================================================================
// File:        UiPuzzlePersistenceTests.cs
// Description: 기존 Progress/ES3 파일 저장, 복원 순서와 실패를 실제로 검증합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>기본 사용자 세이브 대신 Logs의 독립된 검증 파일만 사용합니다.</summary>
public sealed class UiPuzzlePersistenceTests
{
    private readonly List<GameObject> _created = new List<GameObject>();
    private UiPuzzleStateController _controller;
    private ProgressController _progress;
    private string _path;
    private int _clearEvents;
    private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>실제 Awake와 비동기 파일 I/O가 실행되는 플레이 모드를 준비합니다.</summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        _clearEvents = 0;
        _path = Path.GetFullPath("Logs/Chapter1Validation/save-" + Guid.NewGuid().ToString("N") + ".es3");
        Directory.CreateDirectory(Path.GetDirectoryName(_path));
        _progress = Create("Progress").AddComponent<ProgressController>();
        Set(_progress, "_saveFilePath", _path);
        _controller = Create("PuzzleStateConnection").AddComponent<UiPuzzleStateController>();
    }

    /// <summary>검증 오브젝트만 정리하고 편집 모드로 복귀합니다.</summary>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
            if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
        yield return new ExitPlayMode();
    }

    /// <summary>클리어 직후 파일, 연속 요청, 재방문과 재로드를 확인합니다.</summary>
    [UnityTest]
    public IEnumerator ImmediateFileSaveAndReloadRestoreIndependentInstancesWithoutClearEvents()
    {
        UiPuzzleSceneState first = State("INSTANCE_A");
        UiPuzzleSceneState second = State("INSTANCE_B");
        Assert.That(first.IsReady, Is.False);
        first.GetComponent<UiPuzzleInteractable>().Interact();
        LogAssert.NoUnexpectedReceived();
        // 두 배치 모두 같은 데이터 ID를 사용해도 저장 키가 분리되어야 합니다.
        Assert.That(first.GetComponent<UiPuzzleInteractable>().PuzzleId, Is.EqualTo(second.GetComponent<UiPuzzleInteractable>().PuzzleId));
        _controller.Init(_progress);
        Assert.That(first.IsReady, Is.True);
        SceneStateBridge bridge = Create("Bridge").AddComponent<SceneStateBridge>();
        StageFlowController flow = Create("Flow").AddComponent<StageFlowController>();
        Set(flow, "_currentStageNumber", 1);
        bridge.Init(flow, _progress);
        Clear(first);
        Clear(first);
        Assert.That(second.GetComponent<UiPuzzleInteractable>().IsSolved, Is.False);
        Clear(second);
        yield return WaitForSave();
        Assert.That(_controller.LastSaveSucceeded, Is.True);
        Assert.That(_progress.IsDirty, Is.False);
        Assert.That(File.Exists(_path), Is.True);
        Assert.That(new FileInfo(_path).Length, Is.GreaterThan(0));
        Assert.That(_clearEvents, Is.EqualTo(2));
        Assert.That(_progress.TryGetObjectState(1, "INSTANCE_A", out string json), Is.True);
        first.ApplyState(json);
        first.ApplyState(json);
        Assert.That(_clearEvents, Is.EqualTo(2));
        Object.DestroyImmediate(first.gameObject);
        first = State("INSTANCE_A", false);
        _controller.Init(_progress);
        Assert.That(first.GetComponent<UiPuzzleInteractable>().IsSolved, Is.True);
        Assert.That(Door(first).activeSelf, Is.False);
        first.gameObject.SetActive(true);
        Assert.That(first.GetComponent<Collider2D>().enabled, Is.False, "Awake가 복원 결과를 덮어쓰면 안 됩니다.");
        // 파일의 완료 상태를 새 Progress 인스턴스에서도 직접 읽습니다.
        ProgressController fresh = Create("FreshProgress").AddComponent<ProgressController>();
        Set(fresh, "_saveFilePath", _path);
        yield return fresh.LoadAsync().ToCoroutine(result => Assert.That(result, Is.True));
        Assert.That(fresh.TryGetObjectState(1, "INSTANCE_A", out string loaded), Is.True);
        Assert.That(loaded, Is.EqualTo(json));
        first.ApplyState("{\"Solved\":false}");
        _controller.Init(_progress);
        yield return _progress.LoadAsync().ToCoroutine(result => Assert.That(result, Is.True));
        Assert.That(first.GetComponent<UiPuzzleInteractable>().IsSolved, Is.True);
        Assert.That(Door(first).activeSelf, Is.False);
        Assert.That(_clearEvents, Is.EqualTo(2));
        Assert.That(_progress.IsDirty, Is.False, "복원은 파일 저장을 요청하면 안 됩니다.");
    }

    /// <summary>실제 쓰기 실패는 완료 메모리를 보존하며 다음 요청에서 다시 저장됩니다.</summary>
    [UnityTest]
    public IEnumerator FileFailureRetainsDirtyStateAndNextClearPersistsBoth()
    {
        string blocker = Path.GetFullPath("Logs/Chapter1Validation/blocked-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(blocker, "test fixture");
        Set(_progress, "_saveFilePath", Path.Combine(blocker, "save.es3"));
        UiPuzzleSceneState first = State("FAILURE_A");
        UiPuzzleSceneState second = State("FAILURE_B");
        _controller.Init(_progress);
        LogAssert.Expect(LogType.Error, new Regex("\\[ProgressController\\] 저장에 실패했습니다\\."));
        LogAssert.Expect(LogType.Error, "[UiPuzzleStateController] 퍼즐 완료 파일 저장에 실패했습니다. 진행 상태는 메모리에 유지됩니다.");
        Clear(first);
        yield return WaitForSave();
        Assert.That(_controller.LastSaveSucceeded, Is.False);
        Assert.That(_progress.IsDirty, Is.True);
        Assert.That(first.GetComponent<UiPuzzleInteractable>().IsSolved, Is.True);
        Assert.That(Door(first).activeSelf, Is.False);
        Assert.That(File.Exists(_path), Is.False);
        Set(_progress, "_saveFilePath", _path);
        Clear(second);
        yield return WaitForSave();
        Assert.That(_controller.LastSaveSucceeded, Is.True);
        _progress.Reset();
        yield return _progress.LoadAsync().ToCoroutine(result => Assert.That(result, Is.True));
        Assert.That(first.GetComponent<UiPuzzleInteractable>().IsSolved, Is.True);
        Assert.That(second.GetComponent<UiPuzzleInteractable>().IsSolved, Is.True);
    }

    /// <summary>중복 고정 ID는 둘 다 저장 계약에서 제외하여 상태 충돌을 방지합니다.</summary>
    [Test]
    public void DuplicateIdsAreRejectedAndInactiveInitialRestoreCanReturnToUnsolved()
    {
        UiPuzzleSceneState first = State("DUPLICATE", false);
        UiPuzzleSceneState second = State("DUPLICATE");
        for (int i = 0; i < 2; i++)
            LogAssert.Expect(LogType.Error, "[UiPuzzleStateController] 비어 있거나 중복된 배치 저장 ID: DUPLICATE");
        _controller.Init(_progress);
        Assert.That(first.ObjectId, Is.Empty);
        Assert.That(second.ObjectId, Is.Empty);
        first.ApplyState("{\"Solved\":true}");
        first.ApplyState("{\"Solved\":false}");
        first.gameObject.SetActive(true);
        Assert.That(first.GetComponent<Collider2D>().enabled, Is.True);
        Assert.That(Door(first).activeSelf, Is.True);
        Assert.That(_clearEvents, Is.Zero);
        Assert.That(_controller.LastSaveSucceeded, Is.Null);
    }

    private GameObject Create(string name)
    {
        GameObject go = new GameObject(name);
        _created.Add(go);
        return go;
    }
    private UiPuzzleSceneState State(string id, bool active = true)
    {
        GameObject go = Create(id);
        go.SetActive(false);
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        UiPuzzleInteractable puzzle = go.AddComponent<UiPuzzleInteractable>();
        UnityEvent clear = new UnityEvent();
        clear.AddListener(() => _clearEvents++);
        Set(puzzle, "_onPuzzleCleared", clear);
        UiPuzzleSceneState state = go.AddComponent<UiPuzzleSceneState>();
        Set(state, "_objectId", id);
        Set(state, "_stageNumber", 1);
        Set(state, "_closedDoor", Create(id + "Door"));
        go.SetActive(active);
        return state;
    }
    private IEnumerator WaitForSave()
    {
        float deadline = Time.realtimeSinceStartup + 15;
        while (_controller.IsSaving && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(_controller.IsSaving, Is.False, "파일 저장 응답 시간 초과");
    }
    private static void Set(Object target, string name, object value) =>
        target.GetType().GetField(name, PRIVATE).SetValue(target, value);
    private static GameObject Door(UiPuzzleSceneState state) =>
        (GameObject)new SerializedObject(state).FindProperty("_closedDoor").objectReferenceValue;
    private static void Clear(UiPuzzleSceneState state) => typeof(UiPuzzleInteractable)
        .GetMethod("NotifyPuzzleCleared", PRIVATE).Invoke(state.GetComponent<UiPuzzleInteractable>(), null);
}
