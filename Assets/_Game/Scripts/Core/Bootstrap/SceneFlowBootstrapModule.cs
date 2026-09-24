//=============================================================================
// File:        SceneFlowBootstrapModule.cs
//
// Description: StageFlow, SceneStateBridge, Camera 참조와 연결 순서를 소유한다.
//
// History:     09/23/2026 + GameRoot SceneFlow 책임 분리 -- Codex
//
//=============================================================================

using System;
using UnityEngine;

/// <summary>씬 전환, 씬 상태 복구, 카메라 연결 단계를 소유합니다.</summary>
[Serializable]
internal sealed class SceneFlowBootstrapModule
{
    [Tooltip("카메라 이동과 추적을 관리하는 컨트롤러입니다.")]
    [SerializeField] private CameraController _cameraController;

    [Tooltip("씬과 스테이지 전환 흐름을 관리하는 컨트롤러입니다.")]
    [SerializeField] private StageFlowController _stageFlowController;

    [Tooltip("씬 상태 Capture 및 Restore 이벤트를 진행 상태와 연결하는 브릿지입니다.")]
    [SerializeField] private SceneStateBridge _sceneStateBridge;

    /// <summary>카메라 이동과 추적을 관리하는 컨트롤러입니다.</summary>
    internal CameraController CameraController => _cameraController;
    /// <summary>스테이지 전환 시스템의 외부 공개 계약입니다.</summary>
    internal IStageFlowService StageFlow => _stageFlowController;

    /// <summary>StageFlow, 선택적 SceneStateBridge, Camera를 기존 순서로 연결합니다.</summary>
    internal bool TryInitialize(IProgressService progress)
    {
        if (_stageFlowController == null)
        {
            Debug.LogError("[SceneFlowBootstrapModule] StageFlowController가 연결되지 않았습니다.");
            return false;
        }

        _stageFlowController.Init(progress);
        if (_sceneStateBridge != null && progress != null)
        {
            _sceneStateBridge.Init(_stageFlowController, progress);
        }

        if (_cameraController == null)
        {
            Debug.LogError("[SceneFlowBootstrapModule] CameraController가 연결되지 않았습니다.");
            return false;
        }

        _cameraController.Init();
        return true;
    }
}
