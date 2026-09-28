using System.Collections.Generic;

//=============================================================================
// File:        PuzzleDefinition.cs
//
// Description: CSV에서 읽은 퍼즐 종류별 구성 데이터를 읽기 전용 Definition으로 표현한다.
//
// History:     07/26/2026 + Created -- VicZero
//
//=============================================================================

//=============================================================================
// PuzzleType
//=============================================================================
// Description: CSV 퍼즐 정의가 사용하는 퍼즐 종류를 구분한다.
//
//=============================================================================
public enum PuzzleType
{
    PUZZLE_TYPE_NONE = 0,
    PUZZLE_TYPE_PIPE,
    PUZZLE_TYPE_LIGHT,
    PUZZLE_TYPE_CIRCLE
}

//=============================================================================
// PuzzlePipeTileType
//=============================================================================
// Description: 파이프 퍼즐의 한 칸에 배치할 논리 타일 종류를 구분한다.
//
//=============================================================================
public enum PuzzlePipeTileType
{
    PUZZLE_PIPE_TILE_TYPE_NONE = 0,
    PUZZLE_PIPE_TILE_TYPE_EMPTY,
    PUZZLE_PIPE_TILE_TYPE_START,
    PUZZLE_PIPE_TILE_TYPE_END,
    PUZZLE_PIPE_TILE_TYPE_STRAIGHT,
    PUZZLE_PIPE_TILE_TYPE_CORNER,
    PUZZLE_PIPE_TILE_TYPE_T_JUNCTION,
    PUZZLE_PIPE_TILE_TYPE_CROSS
}

//=============================================================================
// PuzzleLightTileDefinition
//=============================================================================
// Description: 라이트 퍼즐 한 타일의 시작 상태와 클릭 영향을 정의한다.
//
//=============================================================================
[System.Serializable]
public sealed class PuzzleLightTileDefinition
{
    private readonly int _tileId;
    private readonly int _row;
    private readonly int _column;
    private readonly bool _isOn;
    private readonly List<int> _affectedTileIds;

    /// <summary>동일 라이트 퍼즐 안에서 사용하는 타일 고유 ID입니다.</summary>
    public int TileId => _tileId;

    /// <summary>0부터 시작하는 타일 행 위치입니다.</summary>
    public int Row => _row;

    /// <summary>0부터 시작하는 타일 열 위치입니다.</summary>
    public int Column => _column;

    /// <summary>퍼즐을 열었을 때 불이 켜져 있는지 여부입니다.</summary>
    public bool IsOn => _isOn;

    /// <summary>이 타일을 클릭했을 때 함께 반전할 타일 ID 목록입니다.</summary>
    public IReadOnlyList<int> AffectedTileIds => _affectedTileIds;

    /// <summary>검증된 라이트 타일 정의를 생성합니다.</summary>
    public PuzzleLightTileDefinition(
        int tileId,
        int row,
        int column,
        bool isOn,
        IReadOnlyList<int> affectedTileIds)
    {
        _tileId = tileId;
        _row = row;
        _column = column;
        _isOn = isOn;
        _affectedTileIds = affectedTileIds != null
            ? new List<int>(affectedTileIds)
            : new List<int>();
    }
}

//=============================================================================
// PuzzlePipeTileDefinition
//=============================================================================
// Description: 파이프 퍼즐 한 칸의 위치, 모양 및 시작 회전을 정의한다.
//
//=============================================================================
[System.Serializable]
public sealed class PuzzlePipeTileDefinition
{
    private readonly int _row;
    private readonly int _column;
    private readonly PuzzlePipeTileType _tileType;
    private readonly int _rotation;

    /// <summary>0부터 시작하는 타일 행 위치입니다.</summary>
    public int Row => _row;

    /// <summary>0부터 시작하는 타일 열 위치입니다.</summary>
    public int Column => _column;

    /// <summary>이 칸에 배치할 파이프 타일 종류입니다.</summary>
    public PuzzlePipeTileType TileType => _tileType;

    /// <summary>시계 방향 90도 회전 횟수이며 0부터 3까지입니다.</summary>
    public int Rotation => _rotation;

    /// <summary>검증된 파이프 타일 정의를 생성합니다.</summary>
    public PuzzlePipeTileDefinition(
        int row,
        int column,
        PuzzlePipeTileType tileType,
        int rotation)
    {
        _row = row;
        _column = column;
        _tileType = tileType;
        _rotation = rotation;
    }
}

//=============================================================================
// PuzzleCircleRingDefinition
//=============================================================================
// Description: 원형 퍼즐 한 링의 순서, 문양 및 회전 단위를 정의한다.
//
//=============================================================================
[System.Serializable]
public sealed class PuzzleCircleRingDefinition
{
    private readonly int _ringOrder;
    private readonly int _patternIndex;
    private readonly int _segments;
    private readonly int _initialOffset;

    /// <summary>안쪽부터 시작하는 0 기반 링 순서입니다.</summary>
    public int RingOrder => _ringOrder;

    /// <summary>원형 퍼즐 매니저의 문양 프리팹 목록에서 사용할 0 기반 인덱스입니다.</summary>
    public int PatternIndex => _patternIndex;

    /// <summary>한 바퀴를 나눌 회전 단계 개수입니다.</summary>
    public int Segments => _segments;

    /// <summary>문양 프리팹의 기본 배치에서 회전시킬 시작 단계입니다.</summary>
    public int InitialOffset => _initialOffset;

    /// <summary>검증된 원형 퍼즐 링 정의를 생성합니다.</summary>
    public PuzzleCircleRingDefinition(
        int ringOrder,
        int patternIndex,
        int segments,
        int initialOffset)
    {
        _ringOrder = ringOrder;
        _patternIndex = patternIndex;
        _segments = segments;
        _initialOffset = initialOffset;
    }
}

//=============================================================================
// PuzzleDefinition
//=============================================================================
// Description: 퍼즐 ID 한 개에 속한 공통 크기와 종류별 자식 데이터를 묶는다.
//
//=============================================================================
[System.Serializable]
public sealed class PuzzleDefinition
{
    private readonly string _puzzleId;
    private readonly PuzzleType _puzzleType;
    private readonly int _rows;
    private readonly int _columns;
    private readonly List<PuzzleLightTileDefinition> _lightTiles;
    private readonly List<PuzzlePipeTileDefinition> _pipeTiles;
    private readonly List<PuzzleCircleRingDefinition> _circleRings;

    /// <summary>씬과 저장 데이터에서 사용할 퍼즐 고유 ID입니다.</summary>
    public string PuzzleId => _puzzleId;

    /// <summary>이 정의가 구성하는 퍼즐 종류입니다.</summary>
    public PuzzleType PuzzleType => _puzzleType;

    /// <summary>격자 퍼즐의 행 개수이며 원형 퍼즐에서는 0입니다.</summary>
    public int Rows => _rows;

    /// <summary>격자 퍼즐의 열 개수이며 원형 퍼즐에서는 0입니다.</summary>
    public int Columns => _columns;

    /// <summary>라이트 퍼즐 타일 목록이며 다른 퍼즐 종류에서는 비어 있습니다.</summary>
    public IReadOnlyList<PuzzleLightTileDefinition> LightTiles => _lightTiles;

    /// <summary>파이프 퍼즐 타일 목록이며 다른 퍼즐 종류에서는 비어 있습니다.</summary>
    public IReadOnlyList<PuzzlePipeTileDefinition> PipeTiles => _pipeTiles;

    /// <summary>원형 퍼즐 링 목록이며 다른 퍼즐 종류에서는 비어 있습니다.</summary>
    public IReadOnlyList<PuzzleCircleRingDefinition> CircleRings => _circleRings;

    /// <summary>검증과 정렬이 끝난 퍼즐 정의를 생성합니다.</summary>
    public PuzzleDefinition(
        string puzzleId,
        PuzzleType puzzleType,
        int rows,
        int columns,
        IReadOnlyList<PuzzleLightTileDefinition> lightTiles,
        IReadOnlyList<PuzzlePipeTileDefinition> pipeTiles,
        IReadOnlyList<PuzzleCircleRingDefinition> circleRings)
    {
        _puzzleId = puzzleId ?? string.Empty;
        _puzzleType = puzzleType;
        _rows = rows;
        _columns = columns;
        _lightTiles = lightTiles != null
            ? new List<PuzzleLightTileDefinition>(lightTiles)
            : new List<PuzzleLightTileDefinition>();
        _pipeTiles = pipeTiles != null
            ? new List<PuzzlePipeTileDefinition>(pipeTiles)
            : new List<PuzzlePipeTileDefinition>();
        _circleRings = circleRings != null
            ? new List<PuzzleCircleRingDefinition>(circleRings)
            : new List<PuzzleCircleRingDefinition>();
    }
}
