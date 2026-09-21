//==============================================================================
// 기능: interface::IInteractable
// 설명: 플레이어가 상호작용할 수 있는 오브젝트 규약
//==============================================================================
// NOTE: PlayerInteractionController가 범위 내 대상을 탐지한 뒤 Interact()를 호출합니다.
//==============================================================================

/// <summary>
/// 플레이어 상호작용 입력을 받을 수 있는 오브젝트의 규약입니다.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 플레이어가 상호작용 입력을 했을 때 호출됩니다.
    /// </summary>
    void Interact();
}

/// <summary>
/// 상호작용을 실행한 플레이어의 입력 컨트롤러가 필요한 대상의 선택적 계약입니다.
/// </summary>
public interface IInteractionContextReceiver
{
    /// <summary>상호작용 직전에 플레이어 입력 컨트롤러를 전달합니다.</summary>
    /// <param name="interactionController">상호작용을 실행한 플레이어 컨트롤러입니다.</param>
    /// <param name="moveController">같은 플레이어의 이동 컨트롤러입니다.</param>
    void SetInteractionContext(
        PlayerInteractionController interactionController,
        PlayerMoveController moveController);
}
