//=============================================================================
// File:        TechnologyCsvDefinitionLoader.cs
//
// Description: 기술과 문명 재건 단계 CSV를 불변 Definition으로 변환한다.
//
// History:     08/19/2026 + Created -- Codex
//=============================================================================

using System.Collections.Generic;

public static class TechnologyCsvDefinitionLoader
{
    private static readonly string[] TECHNOLOGY_HEADERS =
    {
        "technology_id", "display_name_key", "description_text_key",
        "operation_text_key", "icon_id", "derived_skill_id", "chapter", "restore_value"
    };

    private static readonly string[] STAGE_HEADERS =
    {
        "stage", "required_restore"
    };

    /// <summary>기술과 재건 단계 CSV를 읽어 Definition 목록을 생성합니다.</summary>
    public static bool TryLoad(
        GameDataCsvFile technologiesCsv,
        GameDataCsvFile restorationStagesCsv,
        out List<TechnologyDefinition> technologies,
        out List<RestorationStageDefinition> stages,
        out GameDataCsvLoadError loadError)
    {
        technologies = null;
        stages = null;
        loadError = null;

        if (!CsvDefinitionReadUtility.TryParseOptionalDocument(
                technologiesCsv, TECHNOLOGY_HEADERS, out CsvDocument technologyDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseOptionalDocument(
                restorationStagesCsv, STAGE_HEADERS, out CsvDocument stageDocument, out loadError))
        {
            return false;
        }

        technologies = new List<TechnologyDefinition>();
        stages = new List<RestorationStageDefinition>();
        return TryReadTechnologies(technologyDocument, technologies, out loadError)
            && TryReadStages(stageDocument, stages, out loadError);
    }

    private static bool TryReadTechnologies(
        CsvDocument document,
        List<TechnologyDefinition> technologies,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (document == null) return true;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "technology_id", out string technologyId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row, document.SourceName, "display_name_key", out string displayNameKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row, document.SourceName, "description_text_key", out string descriptionKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row, document.SourceName, "operation_text_key", out string operationKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(
                    row, document.SourceName, "icon_id", out string iconId, out loadError)
                || !CsvDefinitionReadUtility.TryGetOptionalSystemId(
                    row, document.SourceName, "derived_skill_id", out string skillId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "chapter", out int chapter, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "restore_value", out int restoreValue, out loadError))
            {
                return false;
            }

            technologies.Add(new TechnologyDefinition(
                technologyId, displayNameKey, descriptionKey, operationKey,
                iconId, skillId, chapter, restoreValue));
        }
        return true;
    }

    private static bool TryReadStages(
        CsvDocument document,
        List<RestorationStageDefinition> stages,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (document == null) return true;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetPositiveInt(
                    row, document.SourceName, "stage", out int stage, out loadError)
                || !CsvDefinitionReadUtility.TryGetPositiveInt(
                    row, document.SourceName, "required_restore", out int requiredRestore, out loadError))
            {
                return false;
            }
            stages.Add(new RestorationStageDefinition(stage, requiredRestore));
        }
        return true;
    }
}

