using System.Collections.Generic;
using UnityEngine;

//=============================================================================
// File:        PipeManager.cs
//
// Description: CSV 파이프 퍼즐 정의로 격자를 생성하고 이동, 회전 및 물 연결을 판정한다.
//
// History:     07/26/2026 + CSV Definition 기반 생성으로 전환 -- VicZero
//              08/21/2026 + 보드/챕터 오브젝트와 타일 프리팹 비주얼 적용 -- VicZero
//
//=============================================================================
public sealed class PipeManager : BasePuzzleManager
{
    [Header("UI References")]
    [Tooltip("생성한 파이프 타일이 배치될 UI 부모입니다.")]
    [SerializeField]
    private RectTransform _gridParent;

    [Header("Board Prefabs")]
    [Tooltip("3x3 보드 오브젝트입니다.")]
    [SerializeField]
    private GameObject _board3x3;

    [Tooltip("4x4 보드 오브젝트입니다.")]
    [SerializeField]
    private GameObject _board4x4;

    [Tooltip("5x5 보드 오브젝트입니다.")]
    [SerializeField]
    private GameObject _board5x5;

    [Header("Chapter Prefabs")]
    [Tooltip("챕터 0~3 배경 오브젝트입니다. 인덱스 0이 Chap0입니다.")]
    [SerializeField]
    private GameObject[] _chapterBackgrounds;

    [Tooltip("보드 한 변의 UI 크기입니다.")]
    [SerializeField]
    private float _boardDisplaySize = 640f;

    [Tooltip("보드에서 실제 칸이 차지하는 비율입니다.")]
    [SerializeField]
    private float _boardInnerFill = 0.84f;

    [Header("Drag")]
    [Tooltip("드래그 방향을 확정하기 위해 필요한 최소 화면 이동 거리입니다.")]
    [SerializeField]
    private float _moveThreshold = 20f;

    [Header("Tile Prefabs")]
    [Tooltip("빈 칸 타일 프리팹입니다.")]
    [SerializeField]
    private GameObject _emptyTilePrefab;

    [Tooltip("물 시작 타일 프리팹입니다.")]
    [SerializeField]
    private GameObject _startTilePrefab;

    [Tooltip("물 도착 타일 프리팹입니다.")]
    [SerializeField]
    private GameObject _endTilePrefab;

    [Tooltip("직선 파이프 타일 프리팹입니다.")]
    [SerializeField]
    private GameObject _pipeStraightPrefab;

    [Tooltip("곡선 파이프 타일 프리팹입니다.")]
    [SerializeField]
    private GameObject _pipeCornerPrefab;

    [Tooltip("T자 파이프 타일 프리팹입니다.")]
    [SerializeField]
    private GameObject _pipeTPrefab;

    [Tooltip("십자 파이프 타일 프리팹입니다.")]
    [SerializeField]
    private GameObject _pipeCrossPrefab;

    private int _rows;
    private int _columns;
    private PipeComponent[,] _grid;
    private PipeComponent _selectedTile;
    private PipeComponent _targetEmptyTile;
    private Vector3 _originalLocalPosition;
    private Vector3 _emptyTileLocalPosition;
    private bool _isDragging;
    private bool _isDirectionSet;
    private Vector2 _dragStartScreenPosition;
    private float _cellSpacing;
    private int _chapterIndex;

    /// <summary>챕터 배경 오브젝트 인덱스를 바꾸고, 이미 열려 있으면 배경만 다시 적용합니다.</summary>
    public override void SetChapterIndex(int chapterIndex)
    {
        _chapterIndex = Mathf.Clamp(chapterIndex, 0, 3);
        ApplyChapterBackground();
    }

    /// <summary>CSV에서 현재 파이프 퍼즐을 조회하고 UI 격자를 다시 생성합니다.</summary>
    public override void InitializePuzzle()
    {
        ClearGrid();
        IsClear = false;
        ResetDragState();
        _cellSpacing = ItemSpacing;

        if (!AreRequiredReferencesAssigned())
        {
            Debug.LogError("[PipeManager] Grid Parent 또는 파이프 타일 프리팹 연결이 누락되었습니다.");
            return;
        }

        if (!TryGetPuzzleDefinition(
            PuzzleType.PUZZLE_TYPE_PIPE,
            out PuzzleDefinition puzzleDefinition))
        {
            return;
        }

        _rows = puzzleDefinition.Rows;
        _columns = puzzleDefinition.Columns;
        _grid = new PipeComponent[_rows, _columns];
        ApplyBoardAndSpacing();
        ApplyChapterBackground();
        Vector3 startLocalPosition = CreateGridStartLocalPosition();

        for (int i = 0; i < puzzleDefinition.PipeTiles.Count; i++)
        {
            PuzzlePipeTileDefinition tileDefinition = puzzleDefinition.PipeTiles[i];
            PipeData tileData = new PipeData
            {
                type = ConvertTileType(tileDefinition.TileType),
                rotationIndex = tileDefinition.Rotation
            };
            GameObject prefab = GetTilePrefab(tileData.type);
            GameObject tileObject = Instantiate(prefab, _gridParent);
            tileObject.name = $"UI_PipeTile_{tileDefinition.Row}_{tileDefinition.Column}_{tileData.type}";

            RectTransform tileRect = tileObject.GetComponent<RectTransform>();
            tileRect.sizeDelta = new Vector2(_cellSpacing, _cellSpacing);
            tileRect.localPosition = startLocalPosition + new Vector3(
                tileDefinition.Column * _cellSpacing,
                -tileDefinition.Row * _cellSpacing,
                0f);

            PipeComponent tileComponent = tileObject.GetComponent<PipeComponent>();

            if (tileComponent == null)
            {
                Debug.LogError("[PipeManager] 파이프 타일 프리팹에 PipeComponent가 없습니다.");
                Destroy(tileObject);
                ClearGrid();
                return;
            }

            tileComponent.Setup(
                tileData,
                tileDefinition.Row,
                tileDefinition.Column,
                this);
            _grid[tileDefinition.Row, tileDefinition.Column] = tileComponent;
        }

        UpdateWaterFlow();
        Debug.Log(
            $"[PipeManager] CSV 퍼즐 배치 완료. id={PuzzleId}, rows={_rows}, columns={_columns}");
    }

    /// <summary>파이프 타일의 드래그 시작 상태를 기록합니다.</summary>
    public void OnTileBeginDrag(PipeComponent tile, Vector2 screenPosition)
    {
        if (tile == null
            || tile.Data.type == PipeType.Empty
            || tile.Data.type == PipeType.Start
            || tile.Data.type == PipeType.End)
        {
            return;
        }

        _selectedTile = tile;
        _originalLocalPosition = tile.GetComponent<RectTransform>().localPosition;
        _dragStartScreenPosition = screenPosition;
        _isDragging = true;
        _isDirectionSet = false;
        _targetEmptyTile = null;
    }

    /// <summary>드래그 방향의 인접 빈 칸까지 선택 타일을 이동해 보여줍니다.</summary>
    public void OnTileDrag(Vector2 currentScreenPosition)
    {
        if (!_isDragging || _selectedTile == null)
        {
            return;
        }

        if (!_isDirectionSet)
        {
            TrySetDragDirection(currentScreenPosition);
            return;
        }

        if (_targetEmptyTile == null)
        {
            return;
        }

        Vector3 dragAxis = _emptyTileLocalPosition - _originalLocalPosition;
        float maximumDistance = dragAxis.magnitude;
        Vector3 moveDirection = dragAxis.normalized;
        Vector3 mouseMoveVector = currentScreenPosition - _dragStartScreenPosition;
        float dragProgress = Vector3.Dot(mouseMoveVector, moveDirection);
        dragProgress = Mathf.Clamp(dragProgress, 0f, maximumDistance);
        _selectedTile.GetComponent<RectTransform>().localPosition =
            _originalLocalPosition + moveDirection * dragProgress;
    }

    /// <summary>드래그 거리에 따라 선택 타일과 빈 칸을 교환하거나 원위치로 되돌립니다.</summary>
    public void OnTileEndDrag()
    {
        if (!_isDragging || _selectedTile == null)
        {
            return;
        }

        _isDragging = false;
        RectTransform selectedRect = _selectedTile.GetComponent<RectTransform>();

        if (_isDirectionSet && _targetEmptyTile != null)
        {
            float currentDistance = Vector3.Distance(selectedRect.localPosition, _originalLocalPosition);
            float totalDistance = Vector3.Distance(_originalLocalPosition, _emptyTileLocalPosition);

            if (currentDistance > totalDistance * 0.4f)
            {
                ExecuteGridSwap(_selectedTile, _targetEmptyTile);
            }
            else
            {
                selectedRect.localPosition = _originalLocalPosition;
            }
        }
        else
        {
            selectedRect.localPosition = _originalLocalPosition;
        }

        ResetDragState();
    }

    /// <summary>타일 회전 뒤 물 연결 상태를 다시 계산합니다.</summary>
    public void OnTileStateChanged()
    {
        UpdateWaterFlow();
    }

    /// <summary>드래그한 파이프와 인접 빈 칸의 논리 좌표 및 UI 위치를 교환합니다.</summary>
    public void ExecuteGridSwap(PipeComponent draggedTile, PipeComponent emptyTile)
    {
        int draggedRow = draggedTile.Row;
        int draggedColumn = draggedTile.Col;
        int emptyRow = emptyTile.Row;
        int emptyColumn = emptyTile.Col;

        _grid[draggedRow, draggedColumn] = emptyTile;
        _grid[emptyRow, emptyColumn] = draggedTile;
        draggedTile.Row = emptyRow;
        draggedTile.Col = emptyColumn;
        emptyTile.Row = draggedRow;
        emptyTile.Col = draggedColumn;
        draggedTile.GetComponent<RectTransform>().localPosition = GetGridLocalPosition(emptyRow, emptyColumn);
        emptyTile.GetComponent<RectTransform>().localPosition = GetGridLocalPosition(draggedRow, draggedColumn);
        UpdateWaterFlow();
    }

    /// <summary>END 타일까지 물이 연결된 상태를 클리어로 확정합니다.</summary>
    public override void CheckClearCondition()
    {
        IsClear = true;
        OnPuzzleClear();
    }

    /// <summary>행과 열 좌표에 있는 파이프 타일을 안전하게 조회합니다.</summary>
    public PipeComponent GetSpecificTile(int row, int column)
    {
        if (row < 0 || row >= _rows || column < 0 || column >= _columns)
        {
            return null;
        }

        return _grid[row, column];
    }

    /// <summary>행과 열 좌표를 중앙 정렬된 UI 로컬 좌표로 변환합니다.</summary>
    public Vector3 GetGridLocalPosition(int row, int column)
    {
        Vector3 startLocalPosition = CreateGridStartLocalPosition();
        return startLocalPosition + new Vector3(
            column * _cellSpacing,
            -row * _cellSpacing,
            0f);
    }

    /// <summary>파이프 퍼즐 클리어 이벤트를 외부에 전달합니다.</summary>
    protected override void OnPuzzleClear()
    {
        Debug.Log($"[PipeManager] 파이프 퍼즐을 클리어했습니다. id={PuzzleId}");
        TriggerClearEvent();
    }

    private void TrySetDragDirection(Vector2 currentScreenPosition)
    {
        Vector2 mouseDelta = currentScreenPosition - _dragStartScreenPosition;

        if (mouseDelta.magnitude <= _moveThreshold)
        {
            return;
        }

        int targetRow = _selectedTile.Row;
        int targetColumn = _selectedTile.Col;

        if (Mathf.Abs(mouseDelta.x) > Mathf.Abs(mouseDelta.y))
        {
            targetColumn += mouseDelta.x > 0f ? 1 : -1;
        }
        else
        {
            targetRow += mouseDelta.y > 0f ? -1 : 1;
        }

        PipeComponent neighbor = GetSpecificTile(targetRow, targetColumn);

        if (neighbor == null || neighbor.Data.type != PipeType.Empty)
        {
            _isDragging = false;
            _selectedTile = null;
            return;
        }

        _targetEmptyTile = neighbor;
        _emptyTileLocalPosition = neighbor.GetComponent<RectTransform>().localPosition;
        _isDirectionSet = true;
    }

    private void UpdateWaterFlow()
    {
        if (_grid == null)
        {
            return;
        }

        PipeComponent startTile = null;

        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                PipeComponent tile = _grid[row, column];

                if (tile == null)
                {
                    continue;
                }

                tile.IsWatered = false;

                if (tile.Data.type == PipeType.Start)
                {
                    startTile = tile;
                }
            }
        }

        if (startTile == null)
        {
            return;
        }

        Queue<PipeComponent> queue = new Queue<PipeComponent>();
        HashSet<PipeComponent> visited = new HashSet<PipeComponent>();
        int[] rowOffsets = { -1, 1, 0, 0 };
        int[] columnOffsets = { 0, 0, -1, 1 };
        bool isEndConnected = false;
        queue.Enqueue(startTile);
        visited.Add(startTile);

        while (queue.Count > 0)
        {
            PipeComponent current = queue.Dequeue();
            current.IsWatered = true;

            if (current.Data.type == PipeType.End)
            {
                isEndConnected = true;
            }

            for (int i = 0; i < rowOffsets.Length; i++)
            {
                PipeComponent next = GetSpecificTile(
                    current.Row + rowOffsets[i],
                    current.Col + columnOffsets[i]);

                if (next == null || visited.Contains(next) || !IsConnected(current, next))
                {
                    continue;
                }

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                _grid[row, column]?.UpdateVisualColor();
            }
        }

        if (isEndConnected && !IsClear)
        {
            CheckClearCondition();
        }
    }

    private bool IsConnected(PipeComponent from, PipeComponent to)
    {
        int rowDifference = to.Row - from.Row;
        int columnDifference = to.Col - from.Col;
        PipeDir fromDirection = from.Data.GetCurrentDirections();
        PipeDir toDirection = to.Data.GetCurrentDirections();

        if (rowDifference == 0 && columnDifference == 1)
        {
            return (fromDirection & PipeDir.Right) != 0 && (toDirection & PipeDir.Left) != 0;
        }

        if (rowDifference == 0 && columnDifference == -1)
        {
            return (fromDirection & PipeDir.Left) != 0 && (toDirection & PipeDir.Right) != 0;
        }

        if (columnDifference == 0 && rowDifference == 1)
        {
            return (fromDirection & PipeDir.Bottom) != 0 && (toDirection & PipeDir.Top) != 0;
        }

        if (columnDifference == 0 && rowDifference == -1)
        {
            return (fromDirection & PipeDir.Top) != 0 && (toDirection & PipeDir.Bottom) != 0;
        }

        return false;
    }

    private void ResetDragState()
    {
        _selectedTile = null;
        _targetEmptyTile = null;
        _isDragging = false;
        _isDirectionSet = false;
    }

    private void ClearGrid()
    {
        if (_grid == null)
        {
            return;
        }

        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                if (_grid[row, column] != null)
                {
                    Destroy(_grid[row, column].gameObject);
                }
            }
        }

        _grid = null;
    }

    private bool AreRequiredReferencesAssigned()
    {
        return _gridParent != null
            && _emptyTilePrefab != null
            && _startTilePrefab != null
            && _endTilePrefab != null
            && _pipeStraightPrefab != null
            && _pipeCornerPrefab != null
            && _pipeTPrefab != null
            && _pipeCrossPrefab != null;
    }

    private Vector3 CreateGridStartLocalPosition()
    {
        return new Vector3(
            -(_columns - 1) * _cellSpacing / 2f,
            (_rows - 1) * _cellSpacing / 2f,
            0f);
    }

    private void ApplyBoardAndSpacing()
    {
        float boardSize = _boardDisplaySize > 0f ? _boardDisplaySize : ItemSpacing * _columns;
        float innerFill = Mathf.Clamp01(_boardInnerFill);
        if (innerFill <= 0f)
        {
            innerFill = 1f;
        }

        SetBoardActive(_board3x3, _rows == 3 && _columns == 3, boardSize);
        SetBoardActive(_board4x4, _rows == 4 && _columns == 4, boardSize);
        SetBoardActive(_board5x5, _rows == 5 && _columns == 5, boardSize);

        int gridSize = _rows >= _columns ? _rows : _columns;
        _cellSpacing = gridSize > 0 ? boardSize * innerFill / gridSize : ItemSpacing;
    }

    private static void SetBoardActive(GameObject boardObject, bool isActive, float boardSize)
    {
        if (boardObject == null)
        {
            return;
        }

        boardObject.SetActive(isActive);
        RectTransform boardRect = boardObject.GetComponent<RectTransform>();
        if (boardRect != null)
        {
            boardRect.sizeDelta = new Vector2(boardSize, boardSize);
        }
    }

    private void ApplyChapterBackground()
    {
        if (_chapterBackgrounds == null)
        {
            return;
        }

        for (int i = 0; i < _chapterBackgrounds.Length; i++)
        {
            if (_chapterBackgrounds[i] != null)
            {
                _chapterBackgrounds[i].SetActive(i == _chapterIndex);
            }
        }
    }

    private GameObject GetTilePrefab(PipeType type)
    {
        return type switch
        {
            PipeType.Empty => _emptyTilePrefab,
            PipeType.Start => _startTilePrefab,
            PipeType.End => _endTilePrefab,
            PipeType.Pipe_1_Straight => _pipeStraightPrefab,
            PipeType.Pipe_1_Corner => _pipeCornerPrefab,
            PipeType.Pipe_2_T => _pipeTPrefab,
            PipeType.Pipe_3_Cross => _pipeCrossPrefab,
            _ => null
        };
    }

    private static PipeType ConvertTileType(PuzzlePipeTileType tileType)
    {
        return tileType switch
        {
            PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_EMPTY => PipeType.Empty,
            PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_START => PipeType.Start,
            PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_END => PipeType.End,
            PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_STRAIGHT => PipeType.Pipe_1_Straight,
            PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_CORNER => PipeType.Pipe_1_Corner,
            PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_T_JUNCTION => PipeType.Pipe_2_T,
            PuzzlePipeTileType.PUZZLE_PIPE_TILE_TYPE_CROSS => PipeType.Pipe_3_Cross,
            _ => PipeType.Empty
        };
    }
}
