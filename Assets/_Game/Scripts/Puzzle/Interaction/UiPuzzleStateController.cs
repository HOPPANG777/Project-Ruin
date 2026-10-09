//=============================================================================
// File:        UiPuzzleStateController.cs
// Description: Bootstrap에서 받은 저장 서비스와 씬의 UI 퍼즐 배치를 연결합니다.
// History:     10/07/2026 + Created -- Codex
//              10/07/2026 + 유적 Beam·발판의 기존 contributor 연결 -- Codex
//=============================================================================
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>씬 배치 초기화와 즉시 저장 요청을 중개하며 디스크 I/O는 Progress에 맡깁니다.</summary>
[DisallowMultipleComponent]
public sealed class UiPuzzleStateController : MonoBehaviour
{
    // 저장 객체는 기존 Bootstrap/Module에서만 주입합니다.
    private IProgressService _progress;
    private ProgressController _loadEvents;
    private bool _saveRequested;
    private bool _isSaving;
    // 씬 초기화에서만 수집하고 연결 객체 파괴 시 기존 배치 구독까지 해제합니다.
    private UiPuzzleSceneState[] _boundUi;
    private WorldPuzzleSceneState[] _boundWorld;

    /// <summary>최근 파일 저장 요청 결과입니다. 아직 요청하지 않았으면 null입니다.</summary>
    public bool? LastSaveSucceeded { get; private set; }
    /// <summary>이 연결 객체가 현재 파일 저장 응답을 기다리는지 여부입니다.</summary>
    public bool IsSaving => _isSaving;

    //=============================================================================
    // Description: 파일 로드 완료 후 현재 배치와 이후 로드할 씬을 연결합니다.
    // Parameters: progress — 기존 저장 서비스. Return: void.
    //=============================================================================
    /// <summary>기존 SceneFlowBootstrapModule이 저장 복구 후 한 번 주입합니다.</summary>
    public void Init(IProgressService progress)
    {
        Unsubscribe();
        _progress = progress;
        _loadEvents = progress as ProgressController;
        if (_progress == null) return;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        if (_loadEvents != null)
        {
            _loadEvents.StateLoaded += RestoreLoadedScenes;
            _loadEvents.BeforeSaveRequested += CaptureWorldStates;
        }
        BindLoadedScenes();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => BindLoadedScenes();
    private void RestoreLoadedScenes() => BindLoadedScenes(true);

    // 발견은 이 초기화 경로 한 곳에 한정합니다. 비활성 조작대도 복구 대상입니다.
    private void BindLoadedScenes(bool forceRestore = false)
    {
        UiPuzzleSceneState[] states = FindObjectsByType<UiPuzzleSceneState>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        WorldPuzzleSceneState[] world = FindObjectsByType<WorldPuzzleSceneState>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        _boundUi = states;
        _boundWorld = world;
        for (int i = 0; i < states.Length; i++)
        {
            UiPuzzleSceneState state = states[i];
            bool valid = state.StageNumber >= 0 && !string.IsNullOrWhiteSpace(state.ConfiguredId);
            for (int j = 0; j < states.Length; j++)
            {
                if (i != j && state.StageNumber == states[j].StageNumber
                    && state.ConfiguredId == states[j].ConfiguredId) valid = false;
            }
            for (int j = 0; j < world.Length; j++)
                if (state.StageNumber == world[j].StageNumber && state.ConfiguredId == world[j].ConfiguredId) valid = false;
            if (!valid)
            {
                Debug.LogError($"[UiPuzzleStateController] 비어 있거나 중복된 배치 저장 ID: {state.ConfiguredId}", state);
            }
            state.Init(_progress, RequestSave, valid);
        }
        for (int i = 0; i < world.Length; i++)
        {
            WorldPuzzleSceneState state = world[i];
            bool valid = state.StageNumber >= 0 && !string.IsNullOrWhiteSpace(state.ConfiguredId);
            for (int j = 0; j < world.Length; j++)
                if (i != j && state.StageNumber == world[j].StageNumber && state.ConfiguredId == world[j].ConfiguredId) valid = false;
            for (int j = 0; j < states.Length; j++)
                if (state.StageNumber == states[j].StageNumber && state.ConfiguredId == states[j].ConfiguredId) valid = false;
            if (!valid) Debug.LogError("[UiPuzzleStateController] 비어 있거나 중복된 배치 저장 ID: " + state.ConfiguredId, state);
            state.Init(_progress, RequestSave, valid, forceRestore);
        }
    }

    private void RequestSave(UiPuzzleSceneState state)
    {
        RequestSave(state.StageNumber, state);
    }

    // 기존 저장 요청 시에만 수집하며 회전 입력마다 파일 저장을 요청하지 않습니다.
    private void CaptureWorldStates()
    {
        if (_progress == null || _boundWorld == null) return;
        for (int i = 0; i < _boundWorld.Length; i++)
        {
            WorldPuzzleSceneState state = _boundWorld[i];
            if (state == null || !state.IsReady) continue;
            string json = state.Contributor.CaptureState();
            if (!string.IsNullOrWhiteSpace(json)) _progress.SetObjectState(state.StageNumber, state.ConfiguredId, json);
        }
    }

    private void RequestSave(int stageNumber, ISceneStateContributor state)
    {
        // SceneStateBridge의 현재 스테이지 초기화 순서에 기대지 않고 먼저 메모리를 갱신합니다.
        _progress.SetObjectState(stageNumber, state.ObjectId, state.CaptureState());
        _saveRequested = true;
        if (!_isSaving) SaveRequestedAsync().Forget();
    }

    private async UniTask SaveRequestedAsync()
    {
        _isSaving = true;
        try
        {
            while (_saveRequested)
            {
                _saveRequested = false;
                LastSaveSucceeded = await _progress.SaveAsync(this.GetCancellationTokenOnDestroy());
                if (LastSaveSucceeded != true)
                {
                    // 실패를 성공으로 바꾸지 않습니다. 메모리와 Dirty는 다음 기존 저장 요청을 위해 유지됩니다.
                    Debug.LogError("[UiPuzzleStateController] 퍼즐 완료 파일 저장에 실패했습니다. 진행 상태는 메모리에 유지됩니다.", this);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            LastSaveSucceeded = false;
        }
        catch (Exception exception)
        {
            LastSaveSucceeded = false;
            Debug.LogError($"[UiPuzzleStateController] 퍼즐 완료 저장 요청 실패: {exception.Message}", this);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void Unsubscribe()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (_loadEvents != null)
        {
            _loadEvents.StateLoaded -= RestoreLoadedScenes;
            _loadEvents.BeforeSaveRequested -= CaptureWorldStates;
        }
        if (_boundUi != null) for (int i = 0; i < _boundUi.Length; i++)
            if (_boundUi[i] != null) _boundUi[i].Disconnect();
        if (_boundWorld != null) for (int i = 0; i < _boundWorld.Length; i++)
            if (_boundWorld[i] != null) _boundWorld[i].Disconnect();
        _boundUi = null;
        _boundWorld = null;
    }

    private void OnDestroy() => Unsubscribe();
}
