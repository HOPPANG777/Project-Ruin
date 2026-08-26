//=============================================================================
// File:        MuseumCsvDefinitionLoader.cs
// Description: 벽화와 제단 목적지 CSV를 불변 Definition으로 변환한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;

public static class MuseumCsvDefinitionLoader
{
    private static readonly string[] MURAL_HEADERS =
    {
        "mural_id", "chapter", "technology_id"
    };

    private static readonly string[] ALTAR_HEADERS =
    {
        "altar_id", "display_name_key", "stage_number", "spawn_point_id", "is_main_altar"
    };

    /// <summary>선택 박물관 CSV 두 개를 읽어 Definition 목록을 생성합니다.</summary>
    public static bool TryLoad(GameDataCsvFile muralsCsv, GameDataCsvFile altarsCsv,
        out List<MuseumMuralDefinition> murals,
        out List<AltarDestinationDefinition> altars,
        out GameDataCsvLoadError loadError)
    {
        murals = null;
        altars = null;
        loadError = null;
        if (!CsvDefinitionReadUtility.TryParseOptionalDocument(
                muralsCsv, MURAL_HEADERS, out CsvDocument muralDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseOptionalDocument(
                altarsCsv, ALTAR_HEADERS, out CsvDocument altarDocument, out loadError))
            return false;

        murals = new List<MuseumMuralDefinition>();
        altars = new List<AltarDestinationDefinition>();
        return TryReadMurals(muralDocument, murals, out loadError)
            && TryReadAltars(altarDocument, altars, out loadError);
    }

    private static bool TryReadMurals(CsvDocument document,
        List<MuseumMuralDefinition> murals, out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (document == null) return true;
        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "mural_id", out string muralId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "chapter", out int chapter, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "technology_id", out string technologyId, out loadError))
                return false;
            murals.Add(new MuseumMuralDefinition(muralId, chapter, technologyId));
        }
        return true;
    }

    private static bool TryReadAltars(CsvDocument document,
        List<AltarDestinationDefinition> altars, out GameDataCsvLoadError loadError)
    {
        loadError = null;
        if (document == null) return true;
        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];
            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "altar_id", out string altarId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredLocalizationKey(
                    row, document.SourceName, "display_name_key", out string displayNameKey, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "stage_number", out int stageNumber, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "spawn_point_id", out string spawnPointId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredBool(
                    row, document.SourceName, "is_main_altar", out bool isMainAltar, out loadError))
                return false;
            altars.Add(new AltarDestinationDefinition(
                altarId, displayNameKey, stageNumber, spawnPointId, isMainAltar));
        }
        return true;
    }
}
