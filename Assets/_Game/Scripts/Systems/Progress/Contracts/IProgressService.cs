//==============================================================================
// 기능: interface::IProgressService
// 설명: 게임 진행 상태(플래그, 씬 오브젝트 상태)에 대한 외부 공개 계약
//==============================================================================
// NOTE: 외부는 GameRoot.Instance.Progress를 통해서만 접근합니다.
//==============================================================================

using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 게임 진행 상태를 보관·조회하는 서비스의 공개 계약입니다.
/// </summary>
public interface IProgressService
{
    /// <summary>메모리 상태가 마지막 디스크 저장 이후 변경되었는지 여부입니다.</summary>
    bool IsDirty { get; }

    /// <summary>ES3 파일에서 진행 상태를 비동기로 불러옵니다.</summary>
    /// <param name="cancellationToken">오브젝트 생명주기와 연결된 취소 토큰입니다.</param>
    /// <returns>불러오기에 성공하면 true입니다.</returns>
    UniTask<bool> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>변경된 진행 상태를 ES3 파일에 비동기로 저장합니다.</summary>
    /// <param name="cancellationToken">오브젝트 생명주기와 연결된 취소 토큰입니다.</param>
    /// <returns>저장 또는 Dirty가 없는 저장 건너뛰기에 성공하면 true입니다.</returns>
    UniTask<bool> SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 전역 플래그 값을 저장합니다. (스토리, 아이템 획득 등)
    /// </summary>
    /// <param name="flagId">플래그 고유 ID입니다.</param>
    /// <param name="value">저장할 bool 값입니다.</param>
    void SetFlag(string flagId, bool value);

    /// <summary>
    /// 전역 플래그 값을 조회합니다.
    /// </summary>
    /// <param name="flagId">플래그 고유 ID입니다.</param>
    /// <returns>저장된 값이 있으면 해당 값, 없으면 false입니다.</returns>
    bool GetFlag(string flagId);

    /// <summary>
    /// 특정 스테이지의 오브젝트 상태(JSON blob)를 저장합니다.
    /// </summary>
    /// <param name="stageNumber">스테이지 번호입니다.</param>
    /// <param name="objectId">씬 내 오브젝트 고유 ID입니다.</param>
    /// <param name="stateJson">직렬화된 상태 문자열입니다.</param>
    void SetObjectState(int stageNumber, string objectId, string stateJson);

    /// <summary>
    /// 특정 스테이지의 오브젝트 상태(JSON blob)를 조회합니다.
    /// </summary>
    /// <param name="stageNumber">스테이지 번호입니다.</param>
    /// <param name="objectId">씬 내 오브젝트 고유 ID입니다.</param>
    /// <param name="stateJson">조회된 상태 문자열입니다.</param>
    /// <returns>저장된 상태가 있으면 true, 없으면 false입니다.</returns>
    bool TryGetObjectState(int stageNumber, string objectId, out string stateJson);

    /// <summary>
    /// 진행 상태를 초기화합니다. (New Game)
    /// </summary>
    void Reset();
}
