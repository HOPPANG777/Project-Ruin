//=============================================================================
// File:        RuinRuntimeTests.cs
// Description: 실제 물리 발판과 기존 StageFlow·페이드 Presenter로 계단 왕복을 검증합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System.Collections;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>페이드의 암전 시점을 관찰하며 실제 기존 전환은 그대로 실행합니다.</summary>
public sealed class RuinRuntimeTests
{
    private GameObject _map;
    private GameRoot _root;
    private UnitPlayer _player;
    private StageFlowController _flow;
    private FadeObserver _observer;
    private SimulationMode2D _simulation;
    private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>실제 GameRoot의 Presenter와 기존 물리 레이어를 사용합니다.</summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        _simulation = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameRoot.prefab")).GetComponent<GameRoot>();
        _root.enabled = false;
        _map = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Map/Grid_1.prefab"));
        _player = new GameObject("RuntimePlayer",typeof(UnitPlayer),typeof(BoxCollider2D),typeof(Rigidbody2D)).GetComponent<UnitPlayer>();
        _player.GetComponent<BoxCollider2D>().size = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player.prefab").GetComponent<BoxCollider2D>().size;
        _player.gameObject.AddComponent<PlayerInteractionController>().enabled = false;
        _player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        _player.GetComponent<Rigidbody2D>().sleepMode = RigidbodySleepMode2D.NeverSleep;
        _player.transform.position = new Vector3(10000,10000,0);
        _flow = (StageFlowController)_root.StageFlow;
        SceneTransitionPresenter presenter = _root.GetComponentInChildren<SceneTransitionPresenter>(true);
        CanvasGroup group = (CanvasGroup)new SerializedObject(presenter).FindProperty("_fadeCanvasGroup").objectReferenceValue;
        Assert.That(group, Is.Not.Null);
        _observer = new FadeObserver(presenter,group,_player);
        typeof(StageFlowController).GetField("_sceneTransition",PRIVATE).SetValue(_flow,_observer);
        _flow.Init(null);
    }

    /// <summary>원본 세이브에 접근하지 않고 검증 오브젝트만 정리합니다.</summary>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(_player.gameObject);
        Object.DestroyImmediate(_map);
        Object.DestroyImmediate(_root.gameObject);
        Physics2D.simulationMode = _simulation;
        yield return new ExitPlayMode();
    }

    /// <summary>기존 계단이 완전 암전 중 이동하고 도착 후 다시 이동하지 않습니다.</summary>
    [UnityTest]
    public IEnumerator StairsRoundTripThroughActualFadeAndNoArrivalBounce()
    {
        PositionTransitionPad[] pads = _map.GetComponentsInChildren<PositionTransitionPad>();
        int trips = 0;
        for (int i = 0; i < pads.Length; i++)
        {
            if (!pads[i].name.StartsWith("1F_") && !pads[i].name.StartsWith("2F_")) continue;
            Transform arrival = (Transform)new SerializedObject(pads[i]).FindProperty("_destination").objectReferenceValue;
            Move(pads[i].transform.position + Vector3.down*8);
            _observer.Before = pads[i].transform.position;
            _observer.After = arrival.position;
            Move(pads[i].transform.position);
            Assert.That(_flow.IsTransitioning, Is.True);
            float deadline = Time.realtimeSinceStartup + 5;
            while (_flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(_flow.IsTransitioning, Is.False);
            Assert.That(Vector2.Distance(_player.transform.position,arrival.position), Is.LessThan(0.01));
            for (int step = 0; step < 5; step++) Physics2D.Simulate(.02f);
            Assert.That(_observer.Trips, Is.EqualTo(++trips));
        }
        Assert.That(trips, Is.EqualTo(2));
    }

    /// <summary>노란 장치는 실제 플레이어 트리거 진입당 최초 한 번만 문을 엽니다.</summary>
    [UnityTest]
    public IEnumerator YellowPlatesActivateOnceWithRealPhysicsTriggers()
    {
        MazePressurePlate[] plates = _map.GetComponentsInChildren<MazePressurePlate>();
        int events = 0;
        for (int i = 0; i < plates.Length; i++)
        {
            plates[i].Activated += () => events++;
            Move(plates[i].transform.position + Vector3.down*8);
            Move(plates[i].transform.position);
            Assert.That(plates[i].IsActivated, Is.True);
            Move(plates[i].transform.position + Vector3.down*8);
            Move(plates[i].transform.position);
            Assert.That(events, Is.EqualTo(i+1));
        }
        yield return null;
    }

    /// <summary>초기화 발판을 실제로 밟아 소등하며 완료 후에는 같은 발판이 잠깁니다.</summary>
    [UnityTest]
    public IEnumerator RealResetPadResetsIncompleteBeamAndLocksAfterClear()
    {
        BeamPuzzleController beam = _map.GetComponentInChildren<BeamPuzzleController>();
        SerializedProperty pillars = new SerializedObject(beam).FindProperty("_pillars");
        beam.InitializePuzzle();
        beam.TryRotatePillar((BeamPillarView)pillars.GetArrayElementAtIndex(1).objectReferenceValue);
        BeamResetPad reset = _map.GetComponentInChildren<BeamResetPad>();
        Move(reset.transform.position);
        Assert.That(beam.BeamActive, Is.False);
        Move(reset.transform.position + Vector3.down*8);
        for (int i = 1; i < 6; i++) beam.TryRotatePillar((BeamPillarView)pillars.GetArrayElementAtIndex(i).objectReferenceValue);
        Assert.That(beam.IsClear, Is.True);
        Move(reset.transform.position);
        Assert.That(beam.IsClear, Is.True);
        Assert.That(beam.BeamActive, Is.True);
        yield return null;
    }

    private void Move(Vector3 position)
    {
        _player.transform.position = position;
        Physics2D.SyncTransforms();
        Physics2D.Simulate(.02f);
    }

    /// <summary>실제 Presenter의 비동기 완료 경계에서 위치와 암전 alpha를 관찰합니다.</summary>
    private sealed class FadeObserver : ISceneTransitionService
    {
        private readonly SceneTransitionPresenter _presenter;
        private readonly CanvasGroup _group;
        private readonly UnitPlayer _player;
        public Vector3 Before; // 암전 전 위치입니다.
        public Vector3 After; // 암전 중 적용할 목적지입니다.
        public int Trips; // 완료한 실제 페이드 왕복 수입니다.
        public FadeObserver(SceneTransitionPresenter presenter, CanvasGroup group, UnitPlayer player)
        {
            _presenter = presenter;
            _group = group;
            _player = player;
        }
        public void Init() => _presenter.Init();
        public async UniTask FadeInAsync(CancellationToken token)
        {
            await _presenter.FadeInAsync(token);
            Assert.That(_group.alpha, Is.EqualTo(1).Within(0.001));
            Assert.That(Vector2.Distance(_player.transform.position,Before), Is.LessThan(0.01));
        }
        public async UniTask FadeOutAsync(CancellationToken token)
        {
            Assert.That(_group.alpha, Is.EqualTo(1).Within(0.001));
            Assert.That(Vector2.Distance(_player.transform.position,After), Is.LessThan(0.01));
            await _presenter.FadeOutAsync(token);
            Assert.That(_group.alpha, Is.EqualTo(0).Within(0.001));
            Trips++;
        }
        public UniTask MonitorTransitionAsync(AsyncOperation operation, CancellationToken token) => _presenter.MonitorTransitionAsync(operation,token);
    }
}
