//=============================================================================
// File:        RelicRestorationCatalogSO.cs
//
// Description: 유적 재건에 사용하는 기술, 복원 단계, 제단 목적지를 한곳에 정의한다.
//
// History:     07/22/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

//=============================================================================
//
// Synopsis:   RelicRestorationCatalogSO;
//                - 재건 시스템의 읽기 전용 설정 모음
//                - 에디터와 초기화 시 데이터 오류 검증
//
//=============================================================================
[CreateAssetMenu(
    menuName = "_Game/Relics/Restoration Catalog",
    fileName = "RelicRestorationCatalog")]
public sealed class RelicRestorationCatalogSO : ScriptableObject
{
    //========================================
    // 상수
    //========================================

    // 기획서에서 정의한 최대 재건도입니다.
    private const int RESTORE_MAX = 100;

    // 기획서에서 정의한 복원 단계 수입니다.
    private const int RESTORE_STAGE_COUNT = 3;

    //========================================
    // 내부 데이터
    //========================================

    /// <summary>재건도 임계값과 해당 단계의 Timeline을 묶은 정의입니다.</summary>
    [Serializable]
    public sealed class RestorationStageEntry
    {
        [Tooltip("1부터 시작하는 복원 단계입니다.")]
        [Min(1)]
        [SerializeField]
        private int _stage;

        [Tooltip("해당 복원 단계에 진입하는 최소 재건도입니다.")]
        [Range(1, RESTORE_MAX)]
        [SerializeField]
        private int _requiredRestore;

        [Tooltip("복원 단계 진입 시 재생할 Timeline입니다.")]
        [SerializeField]
        private TimelineAsset _timeline;

        /// <summary>1부터 시작하는 복원 단계입니다.</summary>
        public int Stage => _stage;

        /// <summary>복원 단계에 진입하는 최소 재건도입니다.</summary>
        public int RequiredRestore => _requiredRestore;

        /// <summary>복원 단계 진입 시 재생할 Timeline입니다.</summary>
        public TimelineAsset Timeline => _timeline;
    }

    /// <summary>텔레포트 화면에 표시할 제단 목적지 정의입니다.</summary>
    [Serializable]
    public sealed class AltarDestinationEntry
    {
        [Tooltip("저장과 조회에 사용하는 중복되지 않는 제단 ID입니다.")]
        [SerializeField]
        private string _altarId;

        [Tooltip("제단 이름을 현재 언어로 표시할 때 사용하는 Localization Key입니다.")]
        [SerializeField]
        private string _displayNameKey;

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

        /// <summary>저장과 조회에 사용하는 제단 ID입니다.</summary>
        public string AltarId => _altarId;

        /// <summary>제단 이름의 Localization Key입니다.</summary>
        public string DisplayNameKey => _displayNameKey;

        /// <summary>제단이 위치한 스테이지 번호입니다.</summary>
        public int StageNumber => _stageNumber;

        /// <summary>플레이어를 배치할 SpawnPoint ID입니다.</summary>
        public string SpawnPointId => _spawnPointId;

        /// <summary>박물관의 메인 제단인지 여부입니다.</summary>
        public bool IsMainAltar => _isMainAltar;
    }

    //========================================
    // 직렬화 필드
    //========================================

    [Header("Skills")]
    [Tooltip("되돌려놓을 수 있는 기술 정의 목록입니다.")]
    [SerializeField]
    private List<SkillDataSO> _skills = new List<SkillDataSO>();

    [Header("Restoration Stages")]
    [Tooltip("재건도 임계값 순서로 등록할 복원 단계 목록입니다.")]
    [SerializeField]
    private List<RestorationStageEntry> _stages = new List<RestorationStageEntry>();

    [Header("Altar Destinations")]
    [Tooltip("텔레포트 화면에 사용할 제단 목적지 목록입니다.")]
    [SerializeField]
    private List<AltarDestinationEntry> _altarDestinations = new List<AltarDestinationEntry>();

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>기획서에서 정의한 최대 재건도입니다.</summary>
    public int MaxRestore => RESTORE_MAX;

    /// <summary>되돌려놓을 수 있는 기술 정의 목록입니다.</summary>
    public IReadOnlyList<SkillDataSO> Skills => _skills;

    /// <summary>재건도 임계값 순서의 복원 단계 목록입니다.</summary>
    public IReadOnlyList<RestorationStageEntry> Stages => _stages;

    /// <summary>텔레포트 화면에 사용할 제단 목적지 목록입니다.</summary>
    public IReadOnlyList<AltarDestinationEntry> AltarDestinations => _altarDestinations;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // RelicRestorationCatalogSO.TryValidate
    //=============================================================================
    // Description: 등록된 기술, 복원 단계, 제단 목적지의 필수 값을 검증한다.
    //
    // Parameters:  ( out string errorMessage )
    //
    // Return:      bool — 모든 데이터가 유효하면 true
    //
    //=============================================================================
    /// <summary>등록된 재건 데이터를 검증합니다.</summary>
    /// <param name="errorMessage">검증 실패 이유입니다.</param>
    /// <returns>모든 데이터가 유효하면 true입니다.</returns>
    public bool TryValidate(out string errorMessage)
    {
        if (!TryValidateSkills(out errorMessage))
        {
            return false;
        }

        if (!TryValidateStages(out errorMessage))
        {
            return false;
        }

        return TryValidateAltars(out errorMessage);
    }

    //******************************************************************************
    //
    // Unity Lifecycle
    //
    //******************************************************************************

#if UNITY_EDITOR
    /// <summary>인스펙터 값이 바뀔 때 잘못된 설정을 즉시 알립니다.</summary>
    private void OnValidate()
    {
        if (!TryValidate(out string errorMessage))
        {
            Debug.LogWarning($"[RelicRestorationCatalogSO] {errorMessage}", this);
        }
    }
#endif

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    /// <summary>기술 ID와 재건도 합계를 검증합니다.</summary>
    private bool TryValidateSkills(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (_skills == null || _skills.Count == 0)
        {
            errorMessage = "기술이 등록되지 않았습니다.";
            return false;
        }

        int totalRestore = 0;

        for (int i = 0; i < _skills.Count; i++)
        {
            SkillDataSO skill = _skills[i];

            if (skill == null
                || string.IsNullOrWhiteSpace(skill.SkillId)
                || string.IsNullOrWhiteSpace(skill.DisplayNameKey)
                || string.IsNullOrWhiteSpace(skill.DescriptionKey))
            {
                errorMessage = $"기술 {i}번 항목의 참조, ID 또는 표시 키가 비어 있습니다.";
                return false;
            }

            if (skill.Chapter < 0 || skill.RestoreValue < 0)
            {
                errorMessage = $"기술 값은 음수가 될 수 없습니다. skillId={skill.SkillId}";
                return false;
            }

            for (int compareIndex = i + 1; compareIndex < _skills.Count; compareIndex++)
            {
                SkillDataSO compareSkill = _skills[compareIndex];

                if (compareSkill != null
                    && string.Equals(skill.SkillId, compareSkill.SkillId, StringComparison.Ordinal))
                {
                    errorMessage = $"중복 기술 ID가 있습니다. skillId={skill.SkillId}";
                    return false;
                }
            }

            totalRestore += skill.RestoreValue;
        }

        if (totalRestore != RESTORE_MAX)
        {
            errorMessage = $"기술 재건도 합계는 {RESTORE_MAX}이어야 합니다. current={totalRestore}";
            return false;
        }

        return true;
    }

    /// <summary>복원 단계 번호와 임계값을 검증합니다.</summary>
    private bool TryValidateStages(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (_stages == null || _stages.Count != RESTORE_STAGE_COUNT)
        {
            errorMessage = $"복원 단계는 {RESTORE_STAGE_COUNT}개여야 합니다.";
            return false;
        }

        int previousRestore = 0;

        for (int i = 0; i < _stages.Count; i++)
        {
            RestorationStageEntry stage = _stages[i];
            int expectedStage = i + 1;

            if (stage == null || stage.Stage != expectedStage)
            {
                errorMessage = $"복원 단계는 1부터 순서대로 등록해야 합니다. index={i}";
                return false;
            }

            if (stage.Timeline == null)
            {
                errorMessage = $"복원 단계 Timeline이 연결되지 않았습니다. stage={stage.Stage}";
                return false;
            }

            if (stage.RequiredRestore <= previousRestore || stage.RequiredRestore > RESTORE_MAX)
            {
                errorMessage = $"복원 단계의 재건도는 오름차순이어야 합니다. stage={stage.Stage}";
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

    /// <summary>제단 목적지의 필수 ID와 중복을 검증합니다.</summary>
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

            if (altar == null
                || string.IsNullOrWhiteSpace(altar.AltarId)
                || string.IsNullOrWhiteSpace(altar.DisplayNameKey)
                || string.IsNullOrWhiteSpace(altar.SpawnPointId)
                || altar.StageNumber < 0)
            {
                errorMessage = $"제단 {i}번 항목의 필수 값이 비어 있거나 잘못되었습니다.";
                return false;
            }

            for (int compareIndex = i + 1; compareIndex < _altarDestinations.Count; compareIndex++)
            {
                AltarDestinationEntry compareAltar = _altarDestinations[compareIndex];

                if (compareAltar != null
                    && string.Equals(altar.AltarId, compareAltar.AltarId, StringComparison.Ordinal))
                {
                    errorMessage = $"중복 제단 ID가 있습니다. altarId={altar.AltarId}";
                    return false;
                }
            }
        }

        return true;
    }
}
