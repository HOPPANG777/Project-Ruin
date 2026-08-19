//=============================================================================
// File:        TechnologyDefinition.cs
//
// Description: technologies.csv와 restoration_stages.csv의 불변 런타임 정의
//
// History:     08/19/2026 + CSV 런타임 원본 전환 -- Codex
//=============================================================================

//=============================================================================
//
// Synopsis:   TechnologyDefinition, RestorationStageDefinition;
//                - Unity 에셋 참조와 분리된 문명 기술·재건 단계 데이터
//=============================================================================
public sealed class TechnologyDefinition
{
    public string TechnologyId { get; }
    public string DisplayNameKey { get; }
    public string DescriptionTextKey { get; }
    public string OperationTextKey { get; }
    public string IconId { get; }
    public string DerivedSkillId { get; }
    public int Chapter { get; }
    public int RestoreValue { get; }

    /// <summary>검증이 끝난 문명 기술 정의를 생성합니다.</summary>
    public TechnologyDefinition(
        string technologyId,
        string displayNameKey,
        string descriptionTextKey,
        string operationTextKey,
        string iconId,
        string derivedSkillId,
        int chapter,
        int restoreValue)
    {
        TechnologyId = technologyId ?? string.Empty;
        DisplayNameKey = displayNameKey ?? string.Empty;
        DescriptionTextKey = descriptionTextKey ?? string.Empty;
        OperationTextKey = operationTextKey ?? string.Empty;
        IconId = iconId ?? string.Empty;
        DerivedSkillId = derivedSkillId ?? string.Empty;
        Chapter = chapter;
        RestoreValue = restoreValue;
    }
}

public sealed class RestorationStageDefinition
{
    public int Stage { get; }
    public int RequiredRestore { get; }

    /// <summary>검증이 끝난 문명 재건 단계 정의를 생성합니다.</summary>
    public RestorationStageDefinition(int stage, int requiredRestore)
    {
        Stage = stage;
        RequiredRestore = requiredRestore;
    }
}

