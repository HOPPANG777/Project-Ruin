//=============================================================================
// File:        IRelicRestorationService.cs
//
// Description: 기술 획득·반납과 유적 재건 상태를 사용하는 외부 계약을 정의한다.
//
// History:     07/22/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System.Collections.Generic;

//=============================================================================
//
// Synopsis:   RelicRestorationFailure;
//                - 재건 상태 변경이 실패한 이유
//
//=============================================================================
public enum RelicRestorationFailure
{
    RESTORATION_FAILURE_NONE = 0,
    RESTORATION_FAILURE_NOT_INITIALIZED,
    RESTORATION_FAILURE_INVALID_SKILL_ID,
    RESTORATION_FAILURE_SKILL_NOT_FOUND,
    RESTORATION_FAILURE_SKILL_NOT_ACQUIRED,
    RESTORATION_FAILURE_SKILL_ALREADY_ACQUIRED,
    RESTORATION_FAILURE_SKILL_ALREADY_RETURNED
}

//=============================================================================
//
// Synopsis:   RelicRestorationResult;
//                - 기술 반납 전후의 재건 상태 변화
//
//=============================================================================
public readonly struct RelicRestorationResult
{
    /// <summary>기술 반납 결과를 생성합니다.</summary>
    public RelicRestorationResult(
        SkillDataSO skill,
        int previousRestore,
        int currentRestore,
        int previousStage,
        int currentStage,
        bool shouldPlayEnding)
    {
        Skill = skill;
        PreviousRestore = previousRestore;
        CurrentRestore = currentRestore;
        PreviousStage = previousStage;
        CurrentStage = currentStage;
        ShouldPlayEnding = shouldPlayEnding;
    }

    /// <summary>반납된 기술 정의입니다.</summary>
    public SkillDataSO Skill { get; }

    /// <summary>기술 반납 전 재건도입니다.</summary>
    public int PreviousRestore { get; }

    /// <summary>기술 반납 후 재건도입니다.</summary>
    public int CurrentRestore { get; }

    /// <summary>기술 반납 전 복원 단계입니다.</summary>
    public int PreviousStage { get; }

    /// <summary>기술 반납 후 복원 단계입니다.</summary>
    public int CurrentStage { get; }

    /// <summary>이번 상태 변경으로 엔딩 조건을 처음 달성했는지 여부입니다.</summary>
    public bool ShouldPlayEnding { get; }

    /// <summary>기술 반납으로 복원 단계가 변경되었는지 여부입니다.</summary>
    public bool StageChanged => PreviousStage != CurrentStage;
}

//=============================================================================
//
// Synopsis:   IRelicRestorationService;
//                - 기술 획득과 반납 상태 조회
//                - 현재 재건도와 복원 단계 제공
//
//=============================================================================
public interface IRelicRestorationService
{
    /// <summary>기술 획득 상태에 사용하는 Progress Flag 접두사입니다.</summary>
    const string SKILL_ACQUIRED_FLAG_PREFIX = "skill.acquired.";

    /// <summary>기술 반납 상태에 사용하는 Progress Flag 접두사입니다.</summary>
    const string SKILL_RETURNED_FLAG_PREFIX = "skill.returned.";

    /// <summary>현재 유적 재건도입니다.</summary>
    int CurrentRestore { get; }

    /// <summary>최대 유적 재건도입니다.</summary>
    int MaxRestore { get; }

    /// <summary>현재 유적 복원 단계입니다.</summary>
    int RestoreStage { get; }

    /// <summary>Catalog에 등록된 기술 ID인지 확인합니다.</summary>
    /// <param name="skillId">확인할 기술 ID입니다.</param>
    /// <returns>등록된 기술이면 true입니다.</returns>
    bool IsKnownSkill(string skillId);

    /// <summary>아직 반납하지 않은 획득 기술이 있는지 확인합니다.</summary>
    /// <returns>반납 가능한 기술이 있으면 true입니다.</returns>
    bool HasReturnableSkill();

    /// <summary>반납 가능한 기술을 전달받은 재사용 목록에 채웁니다.</summary>
    /// <param name="buffer">결과를 저장할 재사용 목록입니다.</param>
    /// <returns>목록에 추가한 기술 개수입니다.</returns>
    int FillReturnableSkills(List<SkillDataSO> buffer);

    /// <summary>기술 획득 여부를 확인합니다.</summary>
    /// <param name="skillId">조회할 기술 ID입니다.</param>
    /// <returns>획득한 기술이면 true입니다.</returns>
    bool IsAcquired(string skillId);

    /// <summary>기술 반납 여부를 확인합니다.</summary>
    /// <param name="skillId">조회할 기술 ID입니다.</param>
    /// <returns>반납한 기술이면 true입니다.</returns>
    bool IsReturned(string skillId);

    /// <summary>기술을 획득 상태로 변경합니다.</summary>
    /// <param name="skillId">획득할 기술 ID입니다.</param>
    /// <param name="failure">실패한 경우의 이유입니다.</param>
    /// <returns>처음 획득 상태로 변경했으면 true입니다.</returns>
    bool TryAcquireSkill(string skillId, out RelicRestorationFailure failure);

    /// <summary>획득한 기술을 반납하고 재건 상태를 갱신합니다.</summary>
    /// <param name="skillId">반납할 기술 ID입니다.</param>
    /// <param name="result">반납 전후의 재건 상태 변화입니다.</param>
    /// <param name="failure">실패한 경우의 이유입니다.</param>
    /// <returns>기술 반납에 성공하면 true입니다.</returns>
    bool TryReturnSkill(
        string skillId,
        out RelicRestorationResult result,
        out RelicRestorationFailure failure);
}
