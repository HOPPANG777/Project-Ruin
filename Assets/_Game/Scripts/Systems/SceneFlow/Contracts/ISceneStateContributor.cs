//==============================================================================
// 기능: interface::ISceneStateContributor
// 설명: 씬 오브젝트(퍼즐, 문 등)가 자신의 상태를 Capture/Apply하는 규약
//==============================================================================
// NOTE: 씬 언로드 전 Capture, 로드 후 Apply 시점에 ProgressController와 연동됩니다.
//       구현체는 MonoBehaviour로 씬에 배치하고 본 인터페이스를 구현합니다.
//==============================================================================

/// <summary>
/// 씬 오브젝트가 진행 상태 저장/복원에 참여하기 위한 규약입니다.
/// </summary>
public interface ISceneStateContributor
{
    /// <summary>씬 내에서 고유한 오브젝트 ID입니다.</summary>
    string ObjectId { get; }

    /// <summary>현재 상태를 JSON 문자열로 직렬화해 반환합니다.</summary>
    string CaptureState();

    /// <summary>저장된 JSON 상태를 역직렬화해 오브젝트에 적용합니다.</summary>
    /// <param name="stateJson">복원할 상태 JSON입니다.</param>
    void ApplyState(string stateJson);
}

/// <summary>복원 순서에 앞서 초기 상태와 부품 저장 소유권을 준비하는 선택적 계약입니다.</summary>
public interface ISceneStatePreparation
{
    /// <summary>Capture/Apply 이전에 부품의 원래 상태와 저장 소유권을 확보합니다.</summary>
    void PrepareSceneState();
}

/// <summary>상위 contributor가 저장하는 부품의 중복 Capture/Apply를 막는 선택적 계약입니다.</summary>
public interface ISceneStateOwnedContributor
{
    /// <summary>다른 contributor가 이 부품 상태를 저장하는지 여부입니다.</summary>
    bool IsSceneStateOwned { get; }
}
