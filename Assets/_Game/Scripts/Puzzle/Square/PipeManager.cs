using System.Collections.Generic;
using UnityEngine;

//=============================================================================
// File:        PipeManager.cs
//
// Description: CSV 파이프 퍼즐 정의로 격자를 생성하고 이동, 회전 및 물 연결을 판정한다.
//
// History:     07/26/2026 + CSV Definition 기반 생성으로 전환 -- VicZero
//
//=============================================================================
public sealed class PipeManager : BasePuzzleManager
{
    [Header("UI References")]
    [Tooltip("생성한 파이프 타일이 배치될 UI 부모입니다.")]
    [SerializeField]
    private RectTransform _gridParent;

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

    /// <summary>CSV에서 현재 파이프 퍼즐을 조회하고 UI 격자를 다시 생성합니다.</summary>
    public override void InitializePuzzle()
    {
        ClearGrid();
        IsClear = false;
        ResetDragState();

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
        Vector3 startLocalPosition = new Vector3(
            -(_columns - 1) * ItemSpacing / 2f,
            (_rows - 1) * ItemSpacing / 2f,
            0f);

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
            tileRect.localPosition = startLocalPosition + new Vector3(
                tileDefinition.Column * ItemSpacing,
                -tileDefinition.Row * ItemSpacing,
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
        Vector3 startLocalPosition = new Vector3(
            -(_columns - 1) * ItemSpacing / 2f,
            (_rows - 1) * ItemSpacing / 2f,
            0f);
        return startLocalPosition + new Vector3(
            column * ItemSpacing,
            -row * ItemSpacing,
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
