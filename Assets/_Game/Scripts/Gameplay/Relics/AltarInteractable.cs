//=============================================================================
// File:        AltarInteractable.cs
//
// Description: 제단 고정 정보를 보관하고 상호작용을 제단 UI로 전달한다.
//
// History:     07/22/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using UnityEngine;

//=============================================================================
//
// Synopsis:   AltarInteractable;
//                - 기존 플레이어 상호작용 시스템의 제단 진입점
//                - 기술·텔레포트 판단은 AltarPresenter에 위임
//
//=============================================================================
[DisallowMultipleComponent]
public sealed class AltarInteractable : MonoBehaviour, IInteractable
{
    //========================================
    // 직렬화 필드
    //========================================

    [Header("Identity")]
    [Tooltip("저장과 조회에 사용하는 중복되지 않는 제단 ID입니다.")]
    [SerializeField]
    private string _altarId;

    [Tooltip("제단이 위치한 스테이지 번호입니다.")]
    [Min(0)]
    [SerializeField]
    private int _stageNumber;

    [Tooltip("스테이지 진입 후 플레이어를 배치할 SpawnPoint ID입니다.")]
    [SerializeField]
    private string _spawnPointId;

    [Tooltip("박물관의 메인 제단이면 활성화합니다.")]
    [SerializeField]
    private bool _isMainAltar;

    [Header("Presentation")]
    [Tooltip("상호작용한 제단 정보를 전달할 제단 UI Presenter입니다.")]
    [SerializeField]
    private AltarPresenter _altarPresenter;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>저장과 조회에 사용하는 제단 ID입니다.</summary>
    public string AltarId => _altarId;

    /// <summary>제단이 위치한 스테이지 번호입니다.</summary>
    public int StageNumber => _stageNumber;

    /// <summary>플레이어를 배치할 SpawnPoint ID입니다.</summary>
    public string SpawnPointId => _spawnPointId;

    /// <summary>박물관의 메인 제단인지 여부입니다.</summary>
    public bool IsMainAltar => _isMainAltar;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // AltarInteractable.Interact
    //=============================================================================
    // Description: 현재 제단 정보를 연결된 Presenter에 전달한다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>현재 제단 정보를 연결된 Presenter에 전달합니다.</summary>
    public void Interact()
    {
        if (_altarPresenter == null)
        {
            Debug.LogWarning("[AltarInteractable] AltarPresenter가 연결되지 않았습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_altarId)
            || string.IsNullOrWhiteSpace(_spawnPointId))
        {
            Debug.LogWarning("[AltarInteractable] 제단 ID 또는 SpawnPoint ID가 비어 있습니다.");
            return;
        }

        _altarPresenter.Open(this);
    }

#if UNITY_EDITOR
    private void OnValidate() => ValidateSetup();

    [ContextMenu("제단 설정 검증")]
    private void ValidateSetup()
    {
        if (_altarPresenter == null)
        {
            LogValidationError("AltarPresenter가 연결되지 않았습니다.");
        }
        if (string.IsNullOrWhiteSpace(_altarId))
        {
            LogValidationError("제단 ID가 비어 있습니다.");
        }
        if (string.IsNullOrWhiteSpace(_spawnPointId))
        {
            LogValidationError("SpawnPoint ID가 비어 있습니다.");
        }
        if (_stageNumber < 0)
        {
            LogValidationError("스테이지 번호는 0 이상이어야 합니다.");
        }
    }

    private void LogValidationError(string message)
    {
        Debug.LogError($"[AltarInteractable] {name}: {message}", this);
    }
#endif
}
