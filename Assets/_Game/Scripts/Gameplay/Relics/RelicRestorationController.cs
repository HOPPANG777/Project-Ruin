//=============================================================================
// File:        RelicRestorationController.cs
//
// Description: 기술 획득·반납을 검증하고 현재 유적 재건 상태를 계산한다.
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

//=============================================================================
//
// Synopsis:   RelicRestorationController;
//                - 기술 획득·반납 상태의 단일 진입점
//                - 반납된 기술을 기준으로 재건도와 복원 단계 계산
//
//=============================================================================
[DisallowMultipleComponent]
public sealed class RelicRestorationController : MonoBehaviour, IRelicRestorationService
{
    //========================================
    // 직렬화 필드
    //========================================

    [Header("Data")]
    [Tooltip("기술, 복원 단계, 제단 목적지를 정의한 재건 Catalog입니다.")]
    [SerializeField]
    private RelicRestorationCatalogSO _catalog;

    //========================================
    // 내부 상태
    //========================================

    // 기술 ID를 매번 목록 순회하지 않도록 초기화 시 한 번 구성하는 캐시입니다.
    private readonly Dictionary<string, SkillDataSO> _skillById =
        new Dictionary<string, SkillDataSO>(StringComparer.Ordinal);

    // 획득·반납 플래그를 보관하는 진행 상태 서비스입니다.
    private IProgressService _progressService;

    // 반납된 기술의 재건도 합계입니다.
    private int _currentRestore;

    // 현재 재건도에 해당하는 복원 단계입니다.
    private int _restoreStage;

    // Catalog와 Progress 주입이 완료되었는지 여부입니다.
    private bool _isInitialized;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>현재 유적 재건도입니다.</summary>
    public int CurrentRestore => _currentRestore;

    /// <summary>최대 유적 재건도입니다.</summary>
    public int MaxRestore => _catalog != null ? _catalog.MaxRestore : 0;

    /// <summary>현재 유적 복원 단계입니다.</summary>
    public int RestoreStage => _restoreStage;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    /// <summary>Catalog에 등록된 기술 ID인지 확인합니다.</summary>
    /// <param name="skillId">확인할 기술 ID입니다.</param>
    /// <returns>등록된 기술이면 true입니다.</returns>
    public bool IsKnownSkill(string skillId)
    {
        return TryGetSkill(skillId, out _, out _);
    }

    //=============================================================================
    // RelicRestorationController.Init
    //=============================================================================
    // Description: 진행 상태 서비스를 주입하고 기술 ID 캐시와 현재 재건 상태를 구성한다.
    //
    // Parameters:  ( IProgressService progressService )
    //
    // Return:      bool — 초기화에 성공하면 true
    //
    //=============================================================================
    /// <summary>진행 상태 서비스를 주입하고 재건 상태를 초기화합니다.</summary>
    /// <param name="progressService">획득·반납 플래그를 보관할 서비스입니다.</param>
    /// <returns>초기화에 성공하면 true입니다.</returns>
    public bool Init(IProgressService progressService)
    {
        _isInitialized = false;
        _progressService = progressService;
        _skillById.Clear();
        _currentRestore = 0;
        _restoreStage = 0;

        if (_progressService == null || _catalog == null)
        {
            Debug.LogWarning("[RelicRestorationController] Progress 또는 Catalog가 연결되지 않았습니다.");
            return false;
        }

        if (!_catalog.TryValidate(out string errorMessage))
        {
            Debug.LogWarning($"[RelicRestorationController] Catalog 검증 실패: {errorMessage}");
            return false;
        }

        IReadOnlyList<SkillDataSO> skills = _catalog.Skills;

        for (int i = 0; i < skills.Count; i++)
        {
            SkillDataSO skill = skills[i];
            _skillById.Add(skill.SkillId, skill);
        }

        RefreshRestoreState();
        _isInitialized = true;
        return true;
    }

    /// <summary>아직 반납하지 않은 획득 기술이 있는지 확인합니다.</summary>
    /// <returns>반납 가능한 기술이 있으면 true입니다.</returns>
    public bool HasReturnableSkill()
    {
        if (!_isInitialized)
        {
            return false;
        }

        IReadOnlyList<SkillDataSO> skills = _catalog.Skills;

        for (int i = 0; i < skills.Count; i++)
        {
            string skillId = skills[i].SkillId;

            if (GetAcquiredFlag(skillId) && !GetReturnedFlag(skillId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>반납 가능한 기술을 전달받은 재사용 목록에 채웁니다.</summary>
    /// <param name="buffer">결과를 저장할 재사용 목록입니다.</param>
    /// <returns>목록에 추가한 기술 개수입니다.</returns>
    public int FillReturnableSkills(List<SkillDataSO> buffer)
    {
        if (buffer == null)
        {
            return 0;
        }

        buffer.Clear();

        if (!_isInitialized)
        {
            return 0;
        }

        IReadOnlyList<SkillDataSO> skills = _catalog.Skills;

        for (int i = 0; i < skills.Count; i++)
        {
            SkillDataSO skill = skills[i];

            if (GetAcquiredFlag(skill.SkillId) && !GetReturnedFlag(skill.SkillId))
            {
                buffer.Add(skill);
            }
        }

        return buffer.Count;
    }

    /// <summary>기술 획득 여부를 확인합니다.</summary>
    /// <param name="skillId">조회할 기술 ID입니다.</param>
    /// <returns>획득한 기술이면 true입니다.</returns>
    public bool IsAcquired(string skillId)
    {
        return TryGetSkill(skillId, out _, out _) && GetAcquiredFlag(skillId);
    }

    /// <summary>기술 반납 여부를 확인합니다.</summary>
    /// <param name="skillId">조회할 기술 ID입니다.</param>
    /// <returns>반납한 기술이면 true입니다.</returns>
    public bool IsReturned(string skillId)
    {
        return TryGetSkill(skillId, out _, out _) && GetReturnedFlag(skillId);
    }

    //=============================================================================
    // RelicRestorationController.TryAcquireSkill
    //=============================================================================
    // Description: 등록된 기술을 처음 획득한 상태로 변경한다.
    //
    // Parameters:  ( string skillId, out RelicRestorationFailure failure )
    //
    // Return:      bool — 처음 획득 상태로 변경하면 true
    //
    //=============================================================================
    /// <summary>등록된 기술을 획득 상태로 변경합니다.</summary>
    /// <param name="skillId">획득할 기술 ID입니다.</param>
    /// <param name="failure">실패한 경우의 이유입니다.</param>
    /// <returns>처음 획득 상태로 변경하면 true입니다.</returns>
    public bool TryAcquireSkill(string skillId, out RelicRestorationFailure failure)
    {
        if (!TryGetSkill(skillId, out _, out failure))
        {
            return false;
        }

        if (GetAcquiredFlag(skillId))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_SKILL_ALREADY_ACQUIRED;
            return false;
        }

        _progressService.SetFlag(GetAcquiredFlagId(skillId), true);
        failure = RelicRestorationFailure.RESTORATION_FAILURE_NONE;
        return true;
    }

    //=============================================================================
    // RelicRestorationController.TryReturnSkill
    //=============================================================================
    // Description: 획득한 기술을 한 번만 반납하고 재건 상태 변화를 반환한다.
    //
    // Parameters:  ( string skillId, out RelicRestorationResult result,
    //                out RelicRestorationFailure failure )
    //
    // Return:      bool — 반납에 성공하면 true
    //
    //=============================================================================
    /// <summary>획득한 기술을 반납하고 재건 상태를 갱신합니다.</summary>
    /// <param name="skillId">반납할 기술 ID입니다.</param>
    /// <param name="result">반납 전후의 재건 상태 변화입니다.</param>
    /// <param name="failure">실패한 경우의 이유입니다.</param>
    /// <returns>기술 반납에 성공하면 true입니다.</returns>
    public bool TryReturnSkill(
        string skillId,
        out RelicRestorationResult result,
        out RelicRestorationFailure failure)
    {
        result = default;

        if (!TryGetSkill(skillId, out SkillDataSO skill, out failure))
        {
            return false;
        }

        if (!GetAcquiredFlag(skillId))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_SKILL_NOT_ACQUIRED;
            return false;
        }

        if (GetReturnedFlag(skillId))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_SKILL_ALREADY_RETURNED;
            return false;
        }

        int previousRestore = _currentRestore;
        int previousStage = _restoreStage;

        _progressService.SetFlag(GetReturnedFlagId(skillId), true);
        RefreshRestoreState();

        bool shouldPlayEnding = previousRestore < MaxRestore && _currentRestore == MaxRestore;
        result = new RelicRestorationResult(
            skill,
            previousRestore,
            _currentRestore,
            previousStage,
            _restoreStage,
            shouldPlayEnding);
        failure = RelicRestorationFailure.RESTORATION_FAILURE_NONE;
        return true;
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    /// <summary>기술 ID의 기본 유효성과 등록 여부를 검사합니다.</summary>
    private bool TryGetSkill(
        string skillId,
        out SkillDataSO skill,
        out RelicRestorationFailure failure)
    {
        skill = null;

        if (!_isInitialized)
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_NOT_INITIALIZED;
            return false;
        }

        if (string.IsNullOrWhiteSpace(skillId))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_INVALID_SKILL_ID;
            return false;
        }

        if (!_skillById.TryGetValue(skillId, out skill))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_SKILL_NOT_FOUND;
            return false;
        }

        failure = RelicRestorationFailure.RESTORATION_FAILURE_NONE;
        return true;
    }

    /// <summary>반납된 기술을 기준으로 재건도와 복원 단계를 다시 계산합니다.</summary>
    private void RefreshRestoreState()
    {
        int restoreValue = 0;
        IReadOnlyList<SkillDataSO> skills = _catalog.Skills;

        for (int i = 0; i < skills.Count; i++)
        {
            SkillDataSO skill = skills[i];

            if (GetReturnedFlag(skill.SkillId))
            {
                restoreValue += skill.RestoreValue;
            }
        }

        _currentRestore = Mathf.Clamp(restoreValue, 0, MaxRestore);
        _restoreStage = CalculateRestoreStage(_currentRestore);
    }

    /// <summary>현재 재건도에 해당하는 가장 높은 복원 단계를 계산합니다.</summary>
    private int CalculateRestoreStage(int restoreValue)
    {
        int stage = 0;
        IReadOnlyList<RelicRestorationCatalogSO.RestorationStageEntry> stages = _catalog.Stages;

        for (int i = 0; i < stages.Count; i++)
        {
            RelicRestorationCatalogSO.RestorationStageEntry entry = stages[i];

            if (restoreValue < entry.RequiredRestore)
            {
                break;
            }

            stage = entry.Stage;
        }

        return stage;
    }

    private bool GetAcquiredFlag(string skillId)
    {
        return _progressService.GetFlag(GetAcquiredFlagId(skillId));
    }

    private bool GetReturnedFlag(string skillId)
    {
        return _progressService.GetFlag(GetReturnedFlagId(skillId));
    }

    private static string GetAcquiredFlagId(string skillId)
    {
        return string.Concat(IRelicRestorationService.SKILL_ACQUIRED_FLAG_PREFIX, skillId);
    }

    private static string GetReturnedFlagId(string skillId)
    {
        return string.Concat(IRelicRestorationService.SKILL_RETURNED_FLAG_PREFIX, skillId);
    }
}
