//=============================================================================
// File:        RelicRestorationCatalogSO.cs
//
// Description: 문명 재건 기술, 복원 단계와 제단 목적지를 정의한다.
//
// History:     07/22/2026 + Created -- Codex
//              07/31/2026 + Technology 데이터 적용 -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Timeline;
//=============================================================================
// Synopsis:   RelicRestorationCatalogSO;
//                - 재건 시스템의 읽기 전용 설정과 데이터 검증
//=============================================================================
[CreateAssetMenu(menuName = "_Game/Relics/Restoration Catalog", fileName = "RelicRestorationCatalog")]
public sealed class RelicRestorationCatalogSO : ScriptableObject
{
    private const int RESTORE_MAX = 100;
    /// <summary>재건도 임계값과 해당 단계의 Timeline을 묶은 정의입니다.</summary>
    [Serializable]
    public sealed class RestorationStageEntry
    {
        [Tooltip("1부터 시작하는 복원 단계입니다."), Min(1), SerializeField] private int _stage;
        [Tooltip("해당 단계에 진입하는 최소 재건도입니다."), Range(1, RESTORE_MAX), SerializeField] private int _requiredRestore;
        [Tooltip("복원 단계 진입 시 재생할 Timeline입니다.")]
        [SerializeField] private TimelineAsset _timeline;
        /// <summary>1부터 시작하는 복원 단계입니다.</summary>
        public int Stage => _stage;
        /// <summary>복원 단계 진입에 필요한 최소 재건도입니다.</summary>
        public int RequiredRestore => _requiredRestore;
        /// <summary>복원 단계 진입 시 재생할 Timeline입니다.</summary>
        public TimelineAsset Timeline => _timeline;
    }
    /// <summary>텔레포트 화면에 표시할 제단 목적지 정의입니다.</summary>
    [Serializable]
    public sealed class AltarDestinationEntry
    {
        [Tooltip("저장과 조회에 사용하는 중복되지 않는 제단 ID입니다.")]
        [SerializeField] private string _altarId;
        [Tooltip("제단 이름을 표시할 Localization Key입니다.")]
        [SerializeField] private string _displayNameKey;
        [Tooltip("제단이 위치한 스테이지 번호입니다."), Min(0)]
        [SerializeField] private int _stageNumber;
        [Tooltip("이동 후 플레이어를 배치할 SpawnPoint ID입니다.")]
        [SerializeField] private string _spawnPointId;
        [Tooltip("박물관의 메인 제단이면 활성화합니다.")]
        [SerializeField] private bool _isMainAltar;
        /// <summary>제단 ID입니다.</summary>
        public string AltarId => _altarId;
        /// <summary>제단 이름의 Localization Key입니다.</summary>
        public string DisplayNameKey => _displayNameKey;
        /// <summary>제단이 위치한 스테이지 번호입니다.</summary>
        public int StageNumber => _stageNumber;
        /// <summary>이동 후 사용할 SpawnPoint ID입니다.</summary>
        public string SpawnPointId => _spawnPointId;
        /// <summary>박물관의 메인 제단인지 여부입니다.</summary>
        public bool IsMainAltar => _isMainAltar;
    }
    //========================================
    // 직렬화 필드
    //========================================
    [Header("Technologies")]
    [Tooltip("획득하고 반환할 수 있는 문명 기술 목록입니다.")]
    [FormerlySerializedAs("_skills")]
    [SerializeField] private List<TechnologyDataSO> _technologies = new List<TechnologyDataSO>();
    [Header("Restoration Stages")]
    [Tooltip("재건도 임계값 순서로 등록할 복원 단계 목록입니다.")]
    [SerializeField] private List<RestorationStageEntry> _stages = new List<RestorationStageEntry>();
    [Header("Altar Destinations")]
    [Tooltip("텔레포트 화면에 사용할 제단 목적지 목록입니다.")]
    [SerializeField] private List<AltarDestinationEntry> _altarDestinations = new List<AltarDestinationEntry>();
    /// <summary>최대 문명 재건도입니다.</summary>
    public int MaxRestore => RESTORE_MAX;
    /// <summary>획득하고 반환할 수 있는 문명 기술 목록입니다.</summary>
    public IReadOnlyList<TechnologyDataSO> Technologies => _technologies;
    /// <summary>재건도 임계값 순서의 복원 단계 목록입니다.</summary>
    public IReadOnlyList<RestorationStageEntry> Stages => _stages;
    /// <summary>텔레포트 화면에 사용할 제단 목적지 목록입니다.</summary>
    public IReadOnlyList<AltarDestinationEntry> AltarDestinations => _altarDestinations;
    //=============================================================================
    // RelicRestorationCatalogSO.TryValidate
    //=============================================================================
    // Description: 기술, 복원 단계와 제단 목적지의 필수 값을 검증한다.
    // Parameters:  ( out string errorMessage )
    // Return:      bool — 모든 데이터가 유효하면 true
    //=============================================================================
    /// <summary>등록된 재건 데이터를 검증합니다.</summary>
    public bool TryValidate(out string errorMessage)
    {
        return TryValidateTechnologies(out errorMessage)
            && TryValidateStages(out errorMessage)
            && TryValidateAltars(out errorMessage);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!TryValidate(out string errorMessage))
        {
            Debug.LogWarning($"[RelicRestorationCatalogSO] {errorMessage}", this);
        }
    }
#endif
    private bool TryValidateTechnologies(out string errorMessage)
    {
        errorMessage = string.Empty;
        int totalRestore = 0;
        if (_technologies == null || _technologies.Count == 0)
        {
            errorMessage = "기술이 등록되지 않았습니다.";
            return false;
        }
        for (int i = 0; i < _technologies.Count; i++)
        {
            TechnologyDataSO technology = _technologies[i];
            if (technology == null || string.IsNullOrWhiteSpace(technology.TechnologyId)
                || string.IsNullOrWhiteSpace(technology.DisplayNameKey)
                || string.IsNullOrWhiteSpace(technology.DescriptionKey))
            {
                errorMessage = $"기술 {i}번 항목의 참조, ID 또는 표시 키가 비어 있습니다.";
                return false;
            }
            for (int compareIndex = i + 1; compareIndex < _technologies.Count; compareIndex++)
            {
                TechnologyDataSO other = _technologies[compareIndex];
                if (other != null && technology.TechnologyId == other.TechnologyId)
                {
                    errorMessage = $"중복 기술 ID가 있습니다. id={technology.TechnologyId}";
                    return false;
                }
            }
            totalRestore += technology.RestoreValue;
        }
        if (totalRestore != RESTORE_MAX)
        {
            errorMessage = $"기술 재건도 합계는 {RESTORE_MAX}이어야 합니다. current={totalRestore}";
            return false;
        }
        return true;
    }
    private bool TryValidateStages(out string errorMessage)
    {
        errorMessage = string.Empty;
        int previousRestore = 0;
        if (_stages == null || _stages.Count == 0)
        {
            errorMessage = "복원 단계가 등록되지 않았습니다.";
            return false;
        }
        for (int i = 0; i < _stages.Count; i++)
        {
            RestorationStageEntry stage = _stages[i];
            if (stage == null || stage.Stage != i + 1 || stage.Timeline == null
                || stage.RequiredRestore <= previousRestore || stage.RequiredRestore > RESTORE_MAX)
            {
                errorMessage = $"복원 단계 {i}번 항목의 순서, 재건도 또는 Timeline이 잘못되었습니다.";
                return false;
            }
            previousRestore = stage.RequiredRestore;
        }
        if (previousRestore != RESTORE_MAX)
        {
            errorMessage = $"마지막 복원 단계는 재건도 {RESTORE_MAX}이어야 합니다.";
            return false;
        }
        return true;
    }
    private bool TryValidateAltars(out string errorMessage)
    {
        errorMessage = string.Empty;
        if (_altarDestinations == null || _altarDestinations.Count == 0)
        {
            errorMessage = "제단 목적지가 등록되지 않았습니다.";
            return false;
        }
        for (int i = 0; i < _altarDestinations.Count; i++)
        {
            AltarDestinationEntry altar = _altarDestinations[i];
            if (altar == null || string.IsNullOrWhiteSpace(altar.AltarId)
                || string.IsNullOrWhiteSpace(altar.DisplayNameKey)
                || string.IsNullOrWhiteSpace(altar.SpawnPointId) || altar.StageNumber < 0)
            {
                errorMessage = $"제단 {i}번 항목의 필수 값이 비어 있거나 잘못되었습니다.";
                return false;
            }
            for (int compareIndex = i + 1; compareIndex < _altarDestinations.Count; compareIndex++)
            {
                AltarDestinationEntry other = _altarDestinations[compareIndex];
                if (other != null && altar.AltarId == other.AltarId)
                {
                    errorMessage = $"중복 제단 ID가 있습니다. id={altar.AltarId}";
                    return false;
                }
            }
        }
        return true;
    }
}
