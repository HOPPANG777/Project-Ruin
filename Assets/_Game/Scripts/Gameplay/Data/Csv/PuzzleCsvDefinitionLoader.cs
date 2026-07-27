using System;
using System.Collections.Generic;
using System.Globalization;

//=============================================================================
// File:        PuzzleCsvDefinitionLoader.cs
//
// Description: 네 개의 퍼즐 CSV를 검증하고 퍼즐 ID별 Definition으로 조립한다.
//
// History:     07/26/2026 + Created -- VicZero
//
//=============================================================================
public static class PuzzleCsvDefinitionLoader
{
    private const int MAX_GRID_SIZE = 100;

    private static readonly string[] PUZZLE_HEADERS =
    {
        "puzzle_id", "puzzle_type", "rows", "columns"
    };

    private static readonly string[] LIGHT_TILE_HEADERS =
    {
        "puzzle_id", "tile_id", "row", "column", "is_on", "affected_tile_ids"
    };

    private static readonly string[] PIPE_TILE_HEADERS =
    {
        "puzzle_id", "row", "column", "tile_type", "rotation"
    };

    private static readonly string[] CIRCLE_RING_HEADERS =
    {
        "puzzle_id", "ring_order", "pattern_index", "segments", "initial_offset"
    };

    //=============================================================================
    // Public Member Functions
    //=============================================================================

    //=============================================================================
    // PuzzleCsvDefinitionLoader.TryLoad
    //=============================================================================
    // Description: 네 개의 퍼즐 CSV를 검증하고 ID별 퍼즐 정의 목록으로 조립한다.
    //
    // Parameters:  ( GameDataCsvSource source,
    //                out List<PuzzleDefinition> definitions,
    //                out GameDataCsvLoadError loadError )
    //
    // Return:      bool — 모든 퍼즐 CSV를 정상적으로 조립했으면 true
    //
    //=============================================================================
    /// <summary>네 개의 퍼즐 CSV를 검증된 퍼즐 정의 목록으로 조립합니다.</summary>
    public static bool TryLoad(
        GameDataCsvSource source,
        out List<PuzzleDefinition> definitions,
        out GameDataCsvLoadError loadError)
    {
        definitions = new List<PuzzleDefinition>();
        loadError = null;

        if (source == null)
        {
            loadError = new GameDataCsvLoadError(
                "(missing)", 0, 0, string.Empty, "게임 데이터 CSV 원본 묶음이 없습니다.");
            return false;
        }

        if (!CsvDefinitionReadUtility.TryParseRequiredDocument(
                source.Puzzles, PUZZLE_HEADERS, out CsvDocument puzzleDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(
                source.PuzzleLightTiles, LIGHT_TILE_HEADERS, out CsvDocument lightDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(
                source.PuzzlePipeTiles, PIPE_TILE_HEADERS, out CsvDocument pipeDocument, out loadError)
            || !CsvDefinitionReadUtility.TryParseRequiredDocument(
                source.PuzzleCircleRings, CIRCLE_RING_HEADERS, out CsvDocument circleDocument, out loadError))
        {
            return false;
        }

        if (!TryReadPuzzles(
                puzzleDocument,
                out List<PuzzleAssemblyData> puzzleOrder,
                out Dictionary<string, PuzzleAssemblyData> puzzleById,
                out loadError)
            || !TryReadLightTiles(lightDocument, puzzleById, out loadError)
            || !TryReadPipeTiles(pipeDocument, puzzleById, out loadError)
            || !TryReadCircleRings(circleDocument, puzzleById, out loadError)
            || !TryBuildDefinitions(puzzleOrder, out definitions, out loadError))
        {
            return false;
        }

        return true;
    }

    //=============================================================================
    // Private Member Functions
    //=============================================================================

    private static bool TryReadPuzzles(
        CsvDocument document,
        out List<PuzzleAssemblyData> puzzleOrder,
        out Dictionary<string, PuzzleAssemblyData> puzzleById,
        out GameDataCsvLoadError loadError)
    {
        puzzleOrder = new List<PuzzleAssemblyData>();
        puzzleById = new Dictionary<string, PuzzleAssemblyData>(StringComparer.Ordinal);
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
                    row, document.SourceName, "puzzle_id", out string puzzleId, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(
                    row, document.SourceName, "puzzle_type", out string rawPuzzleType, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "rows", out int rows, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "columns", out int columns, out loadError)
                || !TryParsePuzzleType(document.SourceName, row, rawPuzzleType, out PuzzleType puzzleType, out loadError)
                || !TryValidatePuzzleSize(document.SourceName, row, puzzleType, rows, columns, out loadError))
            {
                return false;
            }

            if (puzzleById.ContainsKey(puzzleId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "puzzle_id", "중복 퍼즐 ID가 있습니다.");
                return false;
            }

            PuzzleAssemblyData assemblyData = new PuzzleAssemblyData(
                document.SourceName, row, puzzleId, puzzleType, rows, columns);
            puzzleOrder.Add(assemblyData);
            puzzleById.Add(puzzleId, assemblyData);
        }

        if (puzzleOrder.Count > 0)
        {
            return true;
        }

        loadError = new GameDataCsvLoadError(
            document.SourceName, 1, 1, "puzzle_id", "퍼즐 정의가 한 개 이상 필요합니다.");
        return false;
    }

    private static bool TryReadLightTiles(
        CsvDocument document,
        IReadOnlyDictionary<string, PuzzleAssemblyData> puzzleById,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!TryGetParent(
                    document.SourceName, row, puzzleById, PuzzleType.PUZZLE_TYPE_LIGHT,
                    out PuzzleAssemblyData puzzle, out loadError)
                || !CsvDefinitionReadUtility.TryGetPositiveInt(
                    row, document.SourceName, "tile_id", out int tileId, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "row", out int tileRow, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "column", out int column, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredBool(
                    row, document.SourceName, "is_on", out bool isOn, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(
                    row, document.SourceName, "affected_tile_ids", out string affectedIdsText, out loadError)
                || !TryValidateCoordinate(document.SourceName, row, puzzle, tileRow, column, out loadError)
                || !TryReadAffectedTileIds(
                    document.SourceName, row, affectedIdsText, out List<int> affectedTileIds, out loadError))
            {
                return false;
            }

            if (!puzzle.LightTileIds.Add(tileId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "tile_id", "동일 퍼즐 안에 중복 라이트 타일 ID가 있습니다.");
                return false;
            }

            if (!puzzle.OccupiedCoordinates.Add(GetCoordinateKey(tileRow, column)))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "row", "동일 퍼즐 안에 중복 타일 좌표가 있습니다.");
                return false;
            }

            puzzle.LightTiles.Add(new PuzzleLightTileDefinition(
                tileId, tileRow, column, isOn, affectedTileIds));
            puzzle.LightSourceName = document.SourceName;
            puzzle.LightTileSourceRowById.Add(tileId, row);
        }

        return true;
    }

    private static bool TryReadPipeTiles(
        CsvDocument document,
        IReadOnlyDictionary<string, PuzzleAssemblyData> puzzleById,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!TryGetParent(
                    document.SourceName, row, puzzleById, PuzzleType.PUZZLE_TYPE_PIPE,
                    out PuzzleAssemblyData puzzle, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "row", out int tileRow, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "column", out int column, out loadError)
                || !CsvDefinitionReadUtility.TryGetRequiredValue(
                    row, document.SourceName, "tile_type", out string rawTileType, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "rotation", out int rotation, out loadError)
                || !TryValidateCoordinate(document.SourceName, row, puzzle, tileRow, column, out loadError)
                || !TryParsePipeTileType(
                    document.SourceName, row, rawTileType, out PuzzlePipeTileType tileType, out loadError))
            {
                return false;
            }

            if (rotation > 3)
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "rotation", "회전 값은 0부터 3까지만 사용할 수 있습니다.");
                return false;
            }

            if (!puzzle.OccupiedCoordinates.Add(GetCoordinateKey(tileRow, column)))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "row", "동일 퍼즐 안에 중복 타일 좌표가 있습니다.");
                return false;
            }

            if (tileType == PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_START)
            {
                puzzle.StartTileCount++;
            }
            else if (tileType == PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_END)
            {
                puzzle.EndTileCount++;
            }

            puzzle.PipeTiles.Add(new PuzzlePipeTileDefinition(tileRow, column, tileType, rotation));
        }

        return true;
    }

    private static bool TryReadCircleRings(
        CsvDocument document,
        IReadOnlyDictionary<string, PuzzleAssemblyData> puzzleById,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        for (int i = 0; i < document.Rows.Count; i++)
        {
            CsvRow row = document.Rows[i];

            if (!TryGetParent(
                    document.SourceName, row, puzzleById, PuzzleType.PUZZLE_TYPE_CIRCLE,
                    out PuzzleAssemblyData puzzle, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "ring_order", out int ringOrder, out loadError)
                || !CsvDefinitionReadUtility.TryGetNonNegativeInt(
                    row, document.SourceName, "pattern_index", out int patternIndex, out loadError)
                || !CsvDefinitionReadUtility.TryGetPositiveInt(
                    row, document.SourceName, "segments", out int segments, out loadError)
                || !TryGetSignedInt(
                    document.SourceName, row, "initial_offset", out int initialOffset, out loadError))
            {
                return false;
            }

            if (segments < 2)
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "segments", "원형 퍼즐 회전 단계는 2 이상이어야 합니다.");
                return false;
            }

            if (initialOffset == 0 || initialOffset <= -segments || initialOffset >= segments)
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName,
                    row,
                    "initial_offset",
                    "시작 오프셋은 0이 아니며 -segments보다 크고 segments보다 작아야 합니다.");
                return false;
            }

            if (!puzzle.RingOrders.Add(ringOrder))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    document.SourceName, row, "ring_order", "동일 퍼즐 안에 중복 링 순서가 있습니다.");
                return false;
            }

            puzzle.CircleRings.Add(new PuzzleCircleRingDefinition(
                ringOrder, patternIndex, segments, initialOffset));
        }

        return true;
    }

    private static bool TryBuildDefinitions(
        IReadOnlyList<PuzzleAssemblyData> puzzleOrder,
        out List<PuzzleDefinition> definitions,
        out GameDataCsvLoadError loadError)
    {
        definitions = new List<PuzzleDefinition>();
        loadError = null;

        for (int i = 0; i < puzzleOrder.Count; i++)
        {
            PuzzleAssemblyData puzzle = puzzleOrder[i];

            if (!TryValidateChildren(puzzle, out loadError))
            {
                return false;
            }

            puzzle.LightTiles.Sort(CompareLightTiles);
            puzzle.PipeTiles.Sort(ComparePipeTiles);
            puzzle.CircleRings.Sort(CompareCircleRings);

            definitions.Add(new PuzzleDefinition(
                puzzle.PuzzleId,
                puzzle.PuzzleType,
                puzzle.Rows,
                puzzle.Columns,
                puzzle.LightTiles,
                puzzle.PipeTiles,
                puzzle.CircleRings));
        }

        return true;
    }

    private static bool TryValidateChildren(
        PuzzleAssemblyData puzzle,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (puzzle.PuzzleType == PuzzleType.PUZZLE_TYPE_LIGHT)
        {
            if (puzzle.LightTiles.Count != puzzle.Rows * puzzle.Columns)
            {
                loadError = CreatePuzzleError(puzzle, "라이트 타일 수가 rows × columns와 다릅니다.");
                return false;
            }

            for (int i = 0; i < puzzle.LightTiles.Count; i++)
            {
                IReadOnlyList<int> affectedIds = puzzle.LightTiles[i].AffectedTileIds;

                for (int j = 0; j < affectedIds.Count; j++)
                {
                    if (puzzle.LightTileIds.Contains(affectedIds[j]))
                    {
                        continue;
                    }

                    PuzzleLightTileDefinition sourceTile = puzzle.LightTiles[i];
                    loadError = CsvDefinitionReadUtility.CreateRowError(
                        puzzle.LightSourceName,
                        puzzle.LightTileSourceRowById[sourceTile.TileId],
                        "affected_tile_ids",
                        $"존재하지 않는 라이트 타일 ID를 영향 대상으로 참조합니다. tile_id={affectedIds[j]}");
                    return false;
                }
            }

            return true;
        }

        if (puzzle.PuzzleType == PuzzleType.PUZZLE_TYPE_PIPE)
        {
            if (puzzle.PipeTiles.Count != puzzle.Rows * puzzle.Columns)
            {
                loadError = CreatePuzzleError(puzzle, "파이프 타일 수가 rows × columns와 다릅니다.");
                return false;
            }

            if (puzzle.StartTileCount != 1 || puzzle.EndTileCount != 1)
            {
                loadError = CreatePuzzleError(puzzle, "파이프 퍼즐에는 START와 END가 각각 정확히 한 개 필요합니다.");
                return false;
            }

            return true;
        }

        if (puzzle.CircleRings.Count == 0)
        {
            loadError = CreatePuzzleError(puzzle, "원형 퍼즐에는 링이 한 개 이상 필요합니다.");
            return false;
        }

        puzzle.CircleRings.Sort(CompareCircleRings);

        for (int i = 0; i < puzzle.CircleRings.Count; i++)
        {
            if (puzzle.CircleRings[i].RingOrder == i)
            {
                continue;
            }

            loadError = CreatePuzzleError(puzzle, "ring_order는 0부터 빠짐없이 연속되어야 합니다.");
            return false;
        }

        return true;
    }

    private static bool TryGetParent(
        string sourceName,
        CsvRow row,
        IReadOnlyDictionary<string, PuzzleAssemblyData> puzzleById,
        PuzzleType expectedType,
        out PuzzleAssemblyData puzzle,
        out GameDataCsvLoadError loadError)
    {
        puzzle = null;

        if (!CsvDefinitionReadUtility.TryGetRequiredSystemId(
            row, sourceName, "puzzle_id", out string puzzleId, out loadError))
        {
            return false;
        }

        if (!puzzleById.TryGetValue(puzzleId, out puzzle))
        {
            loadError = CsvDefinitionReadUtility.CreateRowError(
                sourceName, row, "puzzle_id", "puzzles.csv에 존재하지 않는 퍼즐 ID입니다.");
            return false;
        }

        if (puzzle.PuzzleType == expectedType)
        {
            return true;
        }

        loadError = CsvDefinitionReadUtility.CreateRowError(
            sourceName, row, "puzzle_id", "자식 CSV와 부모 퍼즐 종류가 일치하지 않습니다.");
        return false;
    }

    private static bool TryValidatePuzzleSize(
        string sourceName,
        CsvRow row,
        PuzzleType puzzleType,
        int rows,
        int columns,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (puzzleType == PuzzleType.PUZZLE_TYPE_CIRCLE)
        {
            if (rows == 0 && columns == 0)
            {
                return true;
            }

            loadError = CsvDefinitionReadUtility.CreateRowError(
                sourceName, row, "rows", "원형 퍼즐의 rows와 columns는 모두 0이어야 합니다.");
            return false;
        }

        if (rows > 0 && columns > 0 && rows <= MAX_GRID_SIZE && columns <= MAX_GRID_SIZE)
        {
            return true;
        }

        loadError = CsvDefinitionReadUtility.CreateRowError(
            sourceName,
            row,
            "rows",
            $"격자 퍼즐의 rows와 columns는 1부터 {MAX_GRID_SIZE}까지여야 합니다.");
        return false;
    }

    private static bool TryValidateCoordinate(
        string sourceName,
        CsvRow row,
        PuzzleAssemblyData puzzle,
        int tileRow,
        int column,
        out GameDataCsvLoadError loadError)
    {
        loadError = null;

        if (tileRow < puzzle.Rows && column < puzzle.Columns)
        {
            return true;
        }

        loadError = CsvDefinitionReadUtility.CreateRowError(
            sourceName, row, "row", "타일 좌표가 부모 퍼즐의 rows 또는 columns 범위를 벗어났습니다.");
        return false;
    }

    private static bool TryReadAffectedTileIds(
        string sourceName,
        CsvRow row,
        string rawValue,
        out List<int> tileIds,
        out GameDataCsvLoadError loadError)
    {
        tileIds = new List<int>();
        loadError = null;
        HashSet<int> uniqueIds = new HashSet<int>();
        string[] tokens = rawValue.Split('|');

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();

            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int tileId)
                || tileId <= 0)
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    sourceName, row, "affected_tile_ids", "영향 타일 ID는 |로 구분한 1 이상의 정수여야 합니다.");
                return false;
            }

            if (!uniqueIds.Add(tileId))
            {
                loadError = CsvDefinitionReadUtility.CreateRowError(
                    sourceName, row, "affected_tile_ids", "한 행에 중복 영향 타일 ID가 있습니다.");
                return false;
            }

            tileIds.Add(tileId);
        }

        return true;
    }

    private static bool TryGetSignedInt(
        string sourceName,
        CsvRow row,
        string columnName,
        out int value,
        out GameDataCsvLoadError loadError)
    {
        value = 0;

        if (!CsvDefinitionReadUtility.TryGetRequiredValue(
            row, sourceName, columnName, out string rawValue, out loadError))
        {
            return false;
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        loadError = CsvDefinitionReadUtility.CreateRowError(
            sourceName, row, columnName, "부호를 포함한 정수만 입력할 수 있습니다.");
        return false;
    }

    private static bool TryParsePuzzleType(
        string sourceName,
        CsvRow row,
        string rawValue,
        out PuzzleType puzzleType,
        out GameDataCsvLoadError loadError)
    {
        puzzleType = rawValue switch
        {
            "PIPE" => PuzzleType.PUZZLE_TYPE_PIPE,
            "LIGHT" => PuzzleType.PUZZLE_TYPE_LIGHT,
            "CIRCLE" => PuzzleType.PUZZLE_TYPE_CIRCLE,
            _ => PuzzleType.PUZZLE_TYPE_NONE
        };

        if (puzzleType != PuzzleType.PUZZLE_TYPE_NONE)
        {
            loadError = null;
            return true;
        }

        loadError = CsvDefinitionReadUtility.CreateRowError(
            sourceName, row, "puzzle_type", "퍼즐 종류는 PIPE, LIGHT, CIRCLE만 사용할 수 있습니다.");
        return false;
    }

    private static bool TryParsePipeTileType(
        string sourceName,
        CsvRow row,
        string rawValue,
        out PuzzlePipeTileType tileType,
        out GameDataCsvLoadError loadError)
    {
        tileType = rawValue switch
        {
            "EMPTY" => PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_EMPTY,
            "START" => PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_START,
            "END" => PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_END,
            "STRAIGHT" => PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_STRAIGHT,
            "CORNER" => PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_CORNER,
            "T_JUNCTION" => PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_T_JUNCTION,
            "CROSS" => PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_CROSS,
            _ => PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_NONE
        };

        if (tileType != PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_NONE)
        {
            loadError = null;
            return true;
        }

        loadError = CsvDefinitionReadUtility.CreateRowError(
            sourceName,
            row,
            "tile_type",
            "타일 종류는 EMPTY, START, END, STRAIGHT, CORNER, T_JUNCTION, CROSS만 사용할 수 있습니다.");
        return false;
    }

    private static GameDataCsvLoadError CreatePuzzleError(
        PuzzleAssemblyData puzzle,
        string message)
    {
        return CsvDefinitionReadUtility.CreateRowError(
            puzzle.SourceName, puzzle.SourceRow, "puzzle_id", message);
    }

    private static int GetCoordinateKey(int row, int column)
    {
        return row * MAX_GRID_SIZE + column;
    }

    private static int CompareLightTiles(
        PuzzleLightTileDefinition left,
        PuzzleLightTileDefinition right)
    {
        int rowComparison = left.Row.CompareTo(right.Row);
        return rowComparison != 0 ? rowComparison : left.Column.CompareTo(right.Column);
    }

    private static int ComparePipeTiles(
        PuzzlePipeTileDefinition left,
        PuzzlePipeTileDefinition right)
    {
        int rowComparison = left.Row.CompareTo(right.Row);
        return rowComparison != 0 ? rowComparison : left.Column.CompareTo(right.Column);
    }

    private static int CompareCircleRings(
        PuzzleCircleRingDefinition left,
        PuzzleCircleRingDefinition right)
    {
        return left.RingOrder.CompareTo(right.RingOrder);
    }

    // 퍼즐별 자식 행을 검증하는 동안만 사용하는 조립 데이터입니다.
    private sealed class PuzzleAssemblyData
    {
        internal string SourceName { get; }
        internal CsvRow SourceRow { get; }
        internal string PuzzleId { get; }
        internal PuzzleType PuzzleType { get; }
        internal int Rows { get; }
        internal int Columns { get; }
        internal List<PuzzleLightTileDefinition> LightTiles { get; }
        internal List<PuzzlePipeTileDefinition> PipeTiles { get; }
        internal List<PuzzleCircleRingDefinition> CircleRings { get; }
        internal HashSet<int> LightTileIds { get; }
        internal HashSet<int> OccupiedCoordinates { get; }
        internal HashSet<int> RingOrders { get; }
        internal Dictionary<int, CsvRow> LightTileSourceRowById { get; }
        internal string LightSourceName { get; set; }
        internal int StartTileCount { get; set; }
        internal int EndTileCount { get; set; }

        internal PuzzleAssemblyData(
            string sourceName,
            CsvRow sourceRow,
            string puzzleId,
            PuzzleType puzzleType,
            int rows,
            int columns)
        {
            SourceName = sourceName;
            SourceRow = sourceRow;
            PuzzleId = puzzleId;
            PuzzleType = puzzleType;
            Rows = rows;
            Columns = columns;
            LightTiles = new List<PuzzleLightTileDefinition>();
            PipeTiles = new List<PuzzlePipeTileDefinition>();
            CircleRings = new List<PuzzleCircleRingDefinition>();
            LightTileIds = new HashSet<int>();
            OccupiedCoordinates = new HashSet<int>();
            RingOrders = new HashSet<int>();
            LightTileSourceRowById = new Dictionary<int, CsvRow>();
            LightSourceName = string.Empty;
        }
    }
}
