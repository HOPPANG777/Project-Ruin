//=============================================================================
// File:        RuinPersistenceTests.cs
// Description: 실제 ES3 파일로 유적 완료·문·미완료 Beam 진행 복원을 검증합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>사용자 세이브와 분리한 Logs의 검증 파일만 사용합니다.</summary>
public sealed class RuinPersistenceTests
{
    private GameObject _map;
    private GameObject _services;
    private ProgressController _progress;
    private UiPuzzleStateController _connection;
    private string _file;
    private UnitPlayer _player;
    private const BindingFlags PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>플레이 모드에서 실제 저장 서비스와 SceneStateBridge를 준비합니다.</summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        _services = new GameObject("RuinValidationServices");
        _progress = _services.AddComponent<ProgressController>();
        _file = Path.GetFullPath("Logs/RuinValidation/evidence-" + Guid.NewGuid().ToString("N") + ".es3");
        Directory.CreateDirectory(Path.GetDirectoryName(_file));
        Set(_progress,"_saveFilePath",_file);
        StageFlowController flow = _services.AddComponent<StageFlowController>();
        Set(flow,"_currentStageNumber",3);
        _services.AddComponent<SceneStateBridge>().Init(flow,_progress);
        _connection = _services.AddComponent<UiPuzzleStateController>();
        _player = new GameObject("ValidationPlayer",typeof(UnitPlayer),typeof(BoxCollider2D)).GetComponent<UnitPlayer>();
        _player.transform.position = new Vector3(10000,10000,0);
        CreateMap();
    }

    /// <summary>검증 오브젝트와 이벤트 연결을 제거합니다.</summary>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(_map);
        Object.DestroyImmediate(_player.gameObject);
        Object.DestroyImmediate(_services);
        yield return new ExitPlayMode();
    }

    /// <summary>회전마다 저장하지 않고 기존 저장 요청으로 미완료 상태를 복원합니다.</summary>
    [UnityTest]
    public IEnumerator IncompleteBeamUsesExistingCaptureAndReloadFormat()
    {
        BeamPuzzleController beam = _map.GetComponentInChildren<BeamPuzzleController>();
        BeamPillarView pillar = Pillar(beam,1);
        Assert.That(beam.TryRotatePillar(pillar), Is.True);
        Assert.That(File.Exists(_file), Is.False);
        Assert.That(_connection.LastSaveSucceeded, Is.Null);
        // 직접 Chapter1을 실행하는 실제 Bootstrap의 StageFlow 초기 번호를 재현합니다.
        Set(_services.GetComponent<StageFlowController>(),"_currentStageNumber",-1);
        yield return _progress.SaveAsync().ToCoroutine(result => Assert.That(result,Is.True));
        string saved = beam.CaptureState();
        Assert.That(File.Exists(_file), Is.True);
        beam.TryResetPuzzle();
        yield return _progress.LoadAsync().ToCoroutine(result => Assert.That(result,Is.True));
        Assert.That(beam.BeamActive, Is.True);
        Assert.That(beam.IsClear, Is.False);
        Assert.That(pillar.RotationDegrees, Is.EqualTo(0).Within(0.01));
        Assert.That(beam.CaptureState(), Is.EqualTo(saved));
        _connection.Init(_progress);
        _connection.Init(_progress);
        Assert.That(pillar.RotationDegrees, Is.EqualTo(0).Within(0.01));
    }

    /// <summary>연속 완료·저장 중 추가 완료를 실제 파일과 재방문으로 확인합니다.</summary>
    [UnityTest]
    public IEnumerator AllEightRuinStatesImmediatelyPersistAndRestoreWithoutEvents()
    {
        UiPuzzleSceneState[] ui = _map.transform.Find("Chapter_1/Chapter1_ruin").GetComponentsInChildren<UiPuzzleSceneState>();
        for (int i = 0; i < ui.Length; i++)
            typeof(UiPuzzleInteractable).GetMethod("NotifyPuzzleCleared",PRIVATE).Invoke(ui[i].GetComponent<UiPuzzleInteractable>(),null);
        MazePressurePlate[] plates = _map.GetComponentsInChildren<MazePressurePlate>();
        for (int i = 0; i < plates.Length; i++) Activate(plates[i]);
        BeamPuzzleController beam = _map.GetComponentInChildren<BeamPuzzleController>();
        for (int i = 1; i < 6; i++) Assert.That(beam.TryRotatePillar(Pillar(beam,i)), Is.True);
        float deadline = Time.realtimeSinceStartup + 15;
        while (_connection.IsSaving && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(_connection.IsSaving, Is.False);
        Assert.That(_connection.LastSaveSucceeded, Is.True);
        Assert.That(_progress.IsDirty, Is.False);
        Assert.That(new FileInfo(_file).Length, Is.GreaterThan(0));
        Assert.That(beam.IsClear, Is.True);
        Object.DestroyImmediate(_map);
        CreateMap();
        ui = _map.transform.Find("Chapter_1/Chapter1_ruin").GetComponentsInChildren<UiPuzzleSceneState>();
        for (int i = 0; i < ui.Length; i++) Assert.That(ui[i].GetComponent<UiPuzzleInteractable>().IsSolved, Is.True);
        plates = _map.GetComponentsInChildren<MazePressurePlate>();
        int events = 0;
        for (int i = 0; i < plates.Length; i++) plates[i].Activated += () => events++;
        beam = _map.GetComponentInChildren<BeamPuzzleController>();
        beam.OnPuzzleClearedEvent += () => events++;
        yield return _progress.LoadAsync().ToCoroutine(result => Assert.That(result,Is.True));
        _connection.Init(_progress);
        _connection.Init(_progress);
        for (int i = 0; i < plates.Length; i++)
        {
            Assert.That(plates[i].IsActivated, Is.True);
            Activate(plates[i]);
            SerializedObject data = new SerializedObject(plates[i]);
            Assert.That(((GameObject)data.FindProperty("_closedDoorObject").objectReferenceValue).activeSelf, Is.False);
        }
        Assert.That(beam.IsClear, Is.True);
        Assert.That(beam.TryResetPuzzle(), Is.False);
        Assert.That(events, Is.Zero);
        Assert.That(_progress.IsDirty, Is.False);
    }

    /// <summary>연결 객체 파괴 후 소스에 저장 구독을 남기지 않고 취소를 성공으로 보고하지 않습니다.</summary>
    [UnityTest]
    public IEnumerator DestroyedConnectionDetachesAndCanceledFileSaveReportsFailure()
    {
        MazePressurePlate plate = _map.GetComponentInChildren<MazePressurePlate>();
        Object.DestroyImmediate(_connection);
        Activate(plate);
        Assert.That(File.Exists(_file), Is.False);
        _progress.SetFlag("CANCELED_TEST",true);
        var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        yield return _progress.SaveAsync(cancellation.Token).ToCoroutine(result => Assert.That(result,Is.False));
        Assert.That(_progress.IsDirty, Is.True);
        cancellation.Dispose();
    }

    private void CreateMap()
    {
        _map = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Map/Grid_1.prefab"));
        _connection.Init(_progress);
    }
    private void Activate(MazePressurePlate plate) => typeof(MazePressurePlate)
        .GetMethod("OnTriggerEnter2D",PRIVATE).Invoke(plate,new object[] { _player.GetComponent<Collider2D>() });
    private static BeamPillarView Pillar(BeamPuzzleController beam, int index) =>
        (BeamPillarView)new SerializedObject(beam).FindProperty("_pillars").GetArrayElementAtIndex(index).objectReferenceValue;
    private static void Set(object target, string field, object value) => target.GetType().GetField(field,PRIVATE).SetValue(target,value);
}
