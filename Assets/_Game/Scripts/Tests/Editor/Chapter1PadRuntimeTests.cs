//=============================================================================
// File:        Chapter1PadRuntimeTests.cs
// Description: 플레이 모드의 실제 트리거로 왕복과 미정 패드의 무효과를 검증합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>실제 이동 패드는 유지하고 전환 서비스 호출만 기록합니다.</summary>
public sealed class Chapter1PadRuntimeTests
{
    private GameObject _map;
    private GameRoot _root;
    private UnitPlayer _player;
    private PadFlowProbe _flow;
    private SimulationMode2D _previousSimulation;

    /// <summary>실제 트리거를 받을 플레이어와 기존 GameRoot 계약을 준비합니다.</summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        _previousSimulation = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        _root = new GameObject("PadTestRoot").AddComponent<GameRoot>();
        _root.enabled = false;
        _flow = _root.gameObject.AddComponent<PadFlowProbe>();
        object module = typeof(GameRoot).GetField("_sceneFlowModule", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_root);
        module.GetType().GetField("_stageFlowController", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(module, _flow);
        _map = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Map/Grid_1.prefab"));
        _player = new GameObject("Player").AddComponent<UnitPlayer>();
        _player.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(2, 3);
        Rigidbody2D body = _player.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        _player.transform.position = new Vector3(5000, 5000, 0);
    }

    /// <summary>검증 오브젝트만 정리합니다.</summary>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(_player.gameObject);
        Object.DestroyImmediate(_map);
        Object.DestroyImmediate(_root.gameObject);
        Physics2D.simulationMode = _previousSimulation;
        yield return new ExitPlayMode();
    }

    /// <summary>유적 왕복 후 자동 재이동 없이 같은 완료 상태를 유지합니다.</summary>
    [UnityTest]
    public IEnumerator RealPositionPadTriggersRoundTripWithoutAutomaticBounce()
    {
        UiPuzzleSceneState[] states = _map.GetComponentsInChildren<UiPuzzleSceneState>(true);
        for (int i = 0; i < states.Length; i++) states[i].ApplyState("{\"Solved\":true}");
        PositionTransitionPad[] pads = _map.GetComponentsInChildren<PositionTransitionPad>();
        for (int i = 0; i < pads.Length; i++)
        {
            Transform arrival = (Transform)new SerializedObject(pads[i]).FindProperty("_destination").objectReferenceValue;
            _player.transform.position = pads[i].transform.position + Vector3.up * 5;
            Physics2D.SyncTransforms();
            Physics2D.Simulate(.02f);
            _player.transform.position = pads[i].transform.position;
            Physics2D.SyncTransforms();
            Physics2D.Simulate(.02f);
            yield return null;
            Assert.That(_flow.FadeCalls, Is.EqualTo(i + 1));
            Assert.That(Vector2.Distance(_player.transform.position, arrival.position), Is.LessThan(0.05f));
            for (int step = 0; step < 4; step++) Physics2D.Simulate(.02f);
            Assert.That(_flow.FadeCalls, Is.EqualTo(i + 1), "도착 즉시 자동 왕복하면 안 됩니다.");
        }
        for (int i = 0; i < states.Length; i++) Assert.That(states[i].GetComponent<UiPuzzleInteractable>().IsSolved, Is.True);
        Assert.That(_flow.SceneCalls, Is.Zero);
    }

    /// <summary>미정 Scene 패드 위를 밟아도 이동·페이드·경고가 없어야 합니다.</summary>
    [UnityTest]
    public IEnumerator DisabledScenePadTriggersProduceNoEffectsOrWarnings()
    {
        SceneTransitionPad[] pads = _map.GetComponentsInChildren<SceneTransitionPad>();
        for (int i = 0; i < pads.Length; i++)
        {
            _player.transform.position = pads[i].transform.position;
            Physics2D.SyncTransforms();
            Physics2D.Simulate(.02f);
            yield return null;
            Assert.That(_flow.SceneCalls, Is.Zero);
            Assert.That(_flow.FadeCalls, Is.Zero);
            Assert.That(Vector2.Distance(_player.transform.position, pads[i].transform.position), Is.LessThan(0.05f));
        }
        LogAssert.NoUnexpectedReceived();
    }

    /// <summary>네 조작대의 기존 Interact 진입점이 공용 UI와 챕터 표시를 연결합니다.</summary>
    [UnityTest]
    public IEnumerator WorldInteractionOpensEachConfiguredSharedPopup()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab");
        GameObject dataObject = Object.Instantiate(source.GetComponentInChildren<GameDataController>(true).gameObject);
        dataObject.transform.SetParent(_root.transform);
        GameDataController data = dataObject.GetComponent<GameDataController>();
        Assert.That(data.TryInit(out GameDataCsvLoadError error), Is.True, error?.Message);
        object core = typeof(GameRoot).GetField("_coreModule", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_root);
        core.GetType().GetField("_gameDataController", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(core, data);
        GameObject ui = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Puzzle/PuzzleUiRoot.prefab"));
        ui.transform.SetParent(_root.transform);
        ProgressController progress = _root.gameObject.AddComponent<ProgressController>();
        UiPuzzleSceneState[] states = _map.GetComponentsInChildren<UiPuzzleSceneState>(true);
        UiPuzzlePopup[] popups = ui.GetComponentsInChildren<UiPuzzlePopup>(true);
        for (int i = 0; i < states.Length; i++)
        {
            states[i].Init(progress, null, true);
            UiPuzzleInteractable puzzle = states[i].GetComponent<UiPuzzleInteractable>();
            puzzle.Interact();
            Assert.That(ui.GetComponent<PuzzleUiRoot>().IsPuzzleOpen, Is.True);
            int openCount = 0;
            for (int j = 0; j < popups.Length; j++)
            {
                if (!popups[j].IsOpen) continue;
                openCount++;
                SerializedObject serialized = new SerializedObject(popups[j].PuzzleManager);
                Assert.That(serialized.FindProperty("_puzzleId").stringValue, Is.EqualTo(puzzle.PuzzleId));
                Assert.That(popups[j].PuzzleManager.GetType().GetField("_chapterIndex",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popups[j].PuzzleManager), Is.EqualTo(1));
                popups[j].ClosePopup();
            }
            Assert.That(openCount, Is.EqualTo(1));
            yield return null;
        }
    }

    /// <summary>테스트에서만 페이드의 암전 콜백을 즉시 실행하는 전환 호출 기록입니다.</summary>
    public sealed class PadFlowProbe : StageFlowController, IStageFlowService
    {
        /// <summary>요청된 위치 전환 횟수입니다.</summary>
        public int FadeCalls;
        /// <summary>요청된 씬 전환 횟수입니다.</summary>
        public int SceneCalls;
        UniTask IStageFlowService.FadeThroughAsync(Action coveredAction)
        {
            FadeCalls++;
            coveredAction();
            return UniTask.CompletedTask;
        }
        UniTask IStageFlowService.ChangeStageAsync(int stage, SceneTransferData data)
        {
            SceneCalls++;
            return UniTask.CompletedTask;
        }
        UniTask IStageFlowService.ChangeSceneAsync(string id, SceneTransferData data)
        {
            SceneCalls++;
            return UniTask.CompletedTask;
        }
    }
}
