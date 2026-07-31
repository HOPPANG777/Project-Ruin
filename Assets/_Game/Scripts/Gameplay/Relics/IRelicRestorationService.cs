//=============================================================================
// File:        IRelicRestorationService.cs
//
// Description: 기술 획득·반환과 문명 재건 상태의 외부 계약을 정의한다.
//
// History:     07/22/2026 + Created -- Codex
//              07/31/2026 + Technology 용어로 분리 -- Codex
//
//=============================================================================

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
    RESTORATION_FAILURE_INVALID_TECHNOLOGY_ID,
    RESTORATION_FAILURE_TECHNOLOGY_NOT_FOUND,
    RESTORATION_FAILURE_TECHNOLOGY_NOT_ACQUIRED,
    RESTORATION_FAILURE_TECHNOLOGY_ALREADY_ACQUIRED,
    RESTORATION_FAILURE_TECHNOLOGY_ALREADY_RETURNED
}

//=============================================================================
//
// Synopsis:   RelicRestorationResult;
//                - 기술 반환 전후의 재건 상태 변화
//
//=============================================================================
public readonly struct RelicRestorationResult
{
    /// <summary>기술 반환 결과를 생성합니다.</summary>
    public RelicRestorationResult(
        TechnologyDataSO technology,
        int previousRestore,
        int currentRestore,
        int previousStage,
        int currentStage,
        bool shouldPlayEnding)
    {
        Technology = technology;
        PreviousRestore = previousRestore;
        CurrentRestore = currentRestore;
        PreviousStage = previousStage;
        CurrentStage = currentStage;
        ShouldPlayEnding = shouldPlayEnding;
    }

    /// <summary>반환된 기술 정의입니다.</summary>
    public TechnologyDataSO Technology { get; }

    /// <summary>기술 반환 전 재건도입니다.</summary>
    public int PreviousRestore { get; }

    /// <summary>기술 반환 후 재건도입니다.</summary>
    public int CurrentRestore { get; }

    /// <summary>기술 반환 전 복원 단계입니다.</summary>
    public int PreviousStage { get; }

    /// <summary>기술 반환 후 복원 단계입니다.</summary>
    public int CurrentStage { get; }

    /// <summary>이번 반환으로 엔딩 조건을 처음 달성했는지 여부입니다.</summary>
    public bool ShouldPlayEnding { get; }

    /// <summary>기술 반환으로 복원 단계가 변경되었는지 여부입니다.</summary>
    public bool StageChanged => PreviousStage != CurrentStage;
}

//=============================================================================
//
// Synopsis:   IRelicRestorationService;
//                - 기술 획득과 반환 상태 조회
//                - 현재 재건도와 복원 단계 제공
//
//=============================================================================
public interface IRelicRestorationService
{
    // 기존 세이브와 호환하기 위해 문자열 값은 변경하지 않습니다.
    const string TECHNOLOGY_ACQUIRED_FLAG_PREFIX = "skill.acquired.";
    const string TECHNOLOGY_RETURNED_FLAG_PREFIX = "skill.returned.";

    /// <summary>현재 문명 재건도입니다.</summary>
    int CurrentRestore { get; }

    /// <summary>최대 문명 재건도입니다.</summary>
    int MaxRestore { get; }

    /// <summary>현재 문명 복원 단계입니다.</summary>
    int RestoreStage { get; }

    /// <summary>Catalog에 등록된 기술인지 확인합니다.</summary>
    bool IsKnownTechnology(string technologyId);

    /// <summary>반환 가능한 기술을 재사용 목록에 채웁니다.</summary>
    int FillReturnableTechnologies(List<TechnologyDataSO> buffer);

    /// <summary>기술 획득 여부를 확인합니다.</summary>
    bool IsAcquired(string technologyId);

    /// <summary>기술 반환 여부를 확인합니다.</summary>
    bool IsReturned(string technologyId);

    /// <summary>기술을 처음 획득한 상태로 변경합니다.</summary>
    bool TryAcquireTechnology(
        string technologyId,
        out RelicRestorationFailure failure);

    /// <summary>획득한 기술을 한 번만 반환하고 재건 상태를 갱신합니다.</summary>
    bool TryReturnTechnology(
        string technologyId,
        out RelicRestorationResult result,
        out RelicRestorationFailure failure);
}
