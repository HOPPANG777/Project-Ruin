//=============================================================================
// File:        TransitionPadBase.cs
//
// Description: 이동 발판의 플레이어 감지와 전환 요청 진입점을 공통 처리한다.
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
// Synopsis:   TransitionPadBase;
//                - UnitPlayer만 이동 발판을 사용할 수 있도록 제한
//                - 실제 목적지 전환은 파생 발판에 위임
//
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public abstract class TransitionPadBase : MonoBehaviour
{
    //******************************************************************************
    //
    // Unity Lifecycle
    //
    //******************************************************************************

    /// <summary>컴포넌트를 추가할 때 발판 Collider2D를 트리거로 설정합니다.</summary>
    private void Reset()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();

        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    /// <summary>플레이어 진입 시 파생 발판의 전환을 요청합니다.</summary>
    /// <param name="other">발판 트리거에 진입한 Collider2D입니다.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitPlayer player = other.GetComponentInParent<UnitPlayer>();

        if (player == null)
        {
            return;
        }

        GameRoot gameRoot = GameRoot.Instance;
        IStageFlowService stageFlow = gameRoot != null ? gameRoot.StageFlow : null;

        // 전환 상태는 서비스가 단일 진입점에서 선점하므로 발판마다 별도 플래그를 두지 않습니다.
        if (stageFlow == null || stageFlow.IsTransitioning)
        {
            return;
        }

        ExecuteTransitionAsync(player, stageFlow).Forget();
    }

    // 파생 발판은 목적지 방식만 결정하고 페이드와 중복 전환 처리는 서비스에 위임합니다.
    protected abstract UniTask ExecuteTransitionAsync(
        UnitPlayer player,
        IStageFlowService stageFlow);
}
