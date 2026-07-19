//=============================================================================
// File:        SceneTransitionPad.cs
//
// Description: 플레이어를 카탈로그에 등록된 다른 스테이지 씬으로 이동시키는 발판이다.
//
// History:     07/19/2026 + Created -- VicZero
//
//=============================================================================

//========================================
// Project Includes
//========================================
using Cysharp.Threading.Tasks;
using UnityEngine;

//=============================================================================
//
// Synopsis:   SceneTransitionPad;
//                - 목적지 스테이지 씬으로 Additive 전환 요청
//                - SpawnPoint ID를 기존 복구 파이프라인에 전달
//
//=============================================================================
public sealed class SceneTransitionPad : TransitionPadBase
{
    //========================================
    // 직렬화 필드
    //========================================

    [Header("Destination")]
    [Tooltip("SceneCatalogSO에 등록된 목적지 스테이지 번호입니다.")]
    [SerializeField]
    private int _destinationStageNumber = -1;

    [Header("Destination")]
    [Tooltip("목적지 씬에서 플레이어를 배치할 SpawnPoint ID입니다.")]
    [SerializeField]
    private string _targetSpawnPointId;

    /// <summary>기존 씬 전환 파이프라인을 통해 목적지 스테이지 씬으로 이동합니다.</summary>
    /// <param name="player">발판을 사용한 플레이어입니다.</param>
    /// <param name="stageFlow">씬 전환 서비스입니다.</param>
    /// <returns>씬 전환 완료를 await 할 수 있는 UniTask입니다.</returns>
    protected override UniTask ExecuteTransitionAsync(
        UnitPlayer player,
        IStageFlowService stageFlow)
    {
        if (_destinationStageNumber < 0)
        {
            Debug.LogWarning("[SceneTransitionPad] 목적지 스테이지 번호가 설정되지 않았습니다.");
            return UniTask.CompletedTask;
        }

        SceneTransferData transferData = new SceneTransferData(
            _targetSpawnPointId,
            _destinationStageNumber);

        return stageFlow.ChangeStageAsync(_destinationStageNumber, transferData);
    }
}
