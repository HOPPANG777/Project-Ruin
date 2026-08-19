//=============================================================================
// File:        ICodexDataRepository.cs
//
// Description: 기술·재건·도감 CSV 정의의 읽기 전용 조회 계약
//
// History:     08/19/2026 + Created -- Codex
//=============================================================================

using System.Collections.Generic;

public interface ICodexDataRepository
{
    int MaxRestore { get; }
    int TechnologyCount { get; }
    int CodexEntryCount { get; }
    IReadOnlyList<TechnologyDefinition> Technologies { get; }
    IReadOnlyList<RestorationStageDefinition> RestorationStages { get; }
    IReadOnlyList<CodexEntryDefinition> CodexEntries { get; }

    /// <summary>기술 ID로 문명 기술 정의를 조회합니다.</summary>
    bool TryGetTechnology(string technologyId, out TechnologyDefinition technology);

    /// <summary>Entry ID로 도감 항목 정의를 조회합니다.</summary>
    bool TryGetCodexEntry(string entryId, out CodexEntryDefinition entry);

    /// <summary>카테고리와 원본 ID로 도감 항목 정의를 조회합니다.</summary>
    bool TryGetCodexEntryBySource(
        CodexCategory category,
        string sourceId,
        out CodexEntryDefinition entry);

    /// <summary>술 도감 Entry의 표시 순서가 정렬된 재료 목록을 조회합니다.</summary>
    bool TryGetCodexIngredients(
        string entryId,
        out IReadOnlyList<CodexIngredientDefinition> ingredients);
}

