//=============================================================================
// File:        PositionTransitionPad.cs
//
// Description: 같은 씬 안에서 플레이어를 지정한 위치로 이동시키는 발판이다.
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
// Synopsis:   PositionTransitionPad;
//                - 페이드 암전 중 플레이어 위치 이동
//                - 씬 로드 없이 같은 씬의 구역 사이를 연결
//
//=============================================================================
public sealed class PositionTransitionPad : TransitionPadBase
{
    //========================================
    // 직렬화 필드
    //========================================

    [Header("Destination")]
    [Tooltip("화면이 암전된 동안 플레이어를 이동시킬 목적지입니다.")]
    [SerializeField]
    private Transform _destination;

    /// <summary>화면을 가린 동안 플레이어를 같은 씬의 목적지로 이동시킵니다.</summary>
    /// <param name="player">이동할 플레이어입니다.</param>
    /// <param name="stageFlow">페이드 전환 서비스입니다.</param>
    /// <returns>페이드와 위치 이동 완료를 await 할 수 있는 UniTask입니다.</returns>
    protected override UniTask ExecuteTransitionAsync(
        UnitPlayer player,
        IStageFlowService stageFlow)
    {
        if (_destination == null)
        {
            Debug.LogWarning("[PositionTransitionPad] 목적지가 연결되지 않았습니다.");
            return UniTask.CompletedTask;
        }

        return stageFlow.FadeThroughAsync(
            () =>
            {
                if (player != null && _destination != null)
                {
                    player.transform.position = _destination.position;
                }
            });
    }
}
