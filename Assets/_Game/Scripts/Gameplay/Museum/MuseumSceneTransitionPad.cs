//=============================================================================
// File:        MuseumSceneTransitionPad.cs
// Description: CSV 제단 목적지를 기존 Stage Flow 전환 발판과 연결한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class MuseumSceneTransitionPad : TransitionPadBase
{
    [Header("Museum Destination")]
    [Tooltip("altar_destinations.csv의 altar_id입니다.")]
    [SerializeField] private string _altarId;
    [Tooltip("원형 퍼즐 등 이동 활성화 조건 Flag입니다. 비어 있으면 조건 없이 사용합니다.")]
    [SerializeField] private string _requiredProgressFlagId;

    protected override UniTask ExecuteTransitionAsync(UnitPlayer player, IStageFlowService stageFlow)
    {
        GameRoot gameRoot = GameRoot.Instance;
        if (gameRoot == null || gameRoot.Museum == null)
        {
            Debug.LogWarning("[MuseumSceneTransitionPad] 박물관 서비스가 초기화되지 않았습니다.");
            return UniTask.CompletedTask;
        }
        if (!string.IsNullOrWhiteSpace(_requiredProgressFlagId)
            && (gameRoot.Progress == null || !gameRoot.Progress.GetFlag(_requiredProgressFlagId)))
            return UniTask.CompletedTask;
        if (!gameRoot.Museum.TryCreateAltarTransferData(
                _altarId, out SceneTransferData transferData))
        {
            Debug.LogWarning($"[MuseumSceneTransitionPad] 제단 목적지를 찾을 수 없습니다: {_altarId}");
            return UniTask.CompletedTask;
        }

        return stageFlow.ChangeStageAsync(transferData.enteringStageNumber, transferData);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_altarId))
            Debug.LogError($"[MuseumSceneTransitionPad] {name}: Altar ID가 비어 있습니다.", this);
    }
#endif
}
