//=============================================================================
// File:        WorldPuzzleSceneState.cs
// Description: 기존 Beam·압력 발판의 contributor와 최초 이벤트를 저장 서비스에 연결합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System;
using UnityEngine;

/// <summary>새 퍼즐 상태 형식 없이 기존 부품의 저장 형식을 그대로 사용합니다.</summary>
[DisallowMultipleComponent]
public sealed class WorldPuzzleSceneState : MonoBehaviour
{
    [Tooltip("같은 오브젝트의 기존 BeamPuzzleController 또는 MazePressurePlate입니다.")]
    [SerializeField] private MonoBehaviour _source;
    [Tooltip("UI ChapterIndex와 다른 실제 저장 StageNumber입니다. Chapter1은 3입니다.")]
    [SerializeField] private int _stageNumber = 3;

    // 최초 이벤트만 구독하며 메모리 복원과 파일 저장 요청을 분리합니다.
    private Action<int, ISceneStateContributor> _requestSave;
    private IProgressService _progress;
    private bool _isReady;
    private bool _hasRestored;
    private BeamPuzzleController _beam;
    private MazePressurePlate _plate;

    /// <summary>기존 부품이 소유하는 저장 ID입니다. 어댑터는 contributor를 중복 등록하지 않습니다.</summary>
    public string ConfiguredId => (_source as ISceneStateContributor)?.ObjectId;
    /// <summary>이 배치의 실제 저장 스테이지 번호입니다.</summary>
    public int StageNumber => _stageNumber;

    // SceneFlow의 현재 스테이지가 미지정인 직접 씬 실행에서도 배치의 저장 번호를 사용합니다.
    internal bool IsReady => _isReady;
    internal ISceneStateContributor Contributor => _source as ISceneStateContributor;

    //=============================================================================
    // Description: 기존 상태를 복원하고 최초 이벤트를 중복 없이 연결합니다.
    // Parameters: progress, requestSave, valid, forceRestore. Return: void.
    //=============================================================================
    /// <summary>기존 Bootstrap 연결 객체에서만 의존성을 주입합니다.</summary>
    public void Init(IProgressService progress, Action<int, ISceneStateContributor> requestSave,
        bool valid, bool forceRestore)
    {
        bool restore = !_hasRestored || _progress != progress || forceRestore;
        Unsubscribe();
        _beam = _source as BeamPuzzleController;
        _plate = _source as MazePressurePlate;
        _progress = progress;
        _requestSave = requestSave;
        _isReady = valid && progress != null && (_beam != null || _plate != null)
            && _source.gameObject == gameObject;
        if (!_isReady) return;
        if (_source is ISceneStatePreparation preparation) preparation.PrepareSceneState();
        if (restore && progress.TryGetObjectState(_stageNumber, ConfiguredId, out string json))
            ((ISceneStateContributor)_source).ApplyState(json);
        else if (restore && _plate != null) _plate.ApplyState("false");
        else if (restore && _beam != null) _beam.ApplyState("false");
        _hasRestored = true;
        if (_beam != null) _beam.OnPuzzleClearedEvent += HandleFirstActivation;
        if (_plate != null) _plate.Activated += HandleFirstActivation;
    }

    private void HandleFirstActivation()
    {
        if (_isReady) _requestSave?.Invoke(_stageNumber, (ISceneStateContributor)_source);
    }

    private void Unsubscribe()
    {
        if (_beam != null) _beam.OnPuzzleClearedEvent -= HandleFirstActivation;
        if (_plate != null) _plate.Activated -= HandleFirstActivation;
    }

    /// <summary>저장 연결 객체 파괴·재초기화 때 이전 구독과 콜백을 해제합니다.</summary>
    public void Disconnect()
    {
        Unsubscribe();
        _isReady = false;
        _requestSave = null;
    }

    private void OnDestroy()
    {
        Disconnect();
        _progress = null;
    }
}
