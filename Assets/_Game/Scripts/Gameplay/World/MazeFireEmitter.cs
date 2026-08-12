//=============================================================================
// File:        MazeFireEmitter.cs
//
// Description: 진행 중인 미로에서 타일 단위 불과 돌·벽 차단을 갱신합니다.
//
// History:     08/08/2026 + Created -- Codex
//
//=============================================================================

using UnityEngine;

public enum MazeFireDirection
{
    MAZE_FIRE_RIGHT = 0,
    MAZE_FIRE_UP,
    MAZE_FIRE_LEFT,
    MAZE_FIRE_DOWN
}

[DisallowMultipleComponent]
public sealed class MazeFireEmitter : MonoBehaviour
{
    private const int FIRE_TILE_CAPACITY = 5;
    private const int HIT_BUFFER_CAPACITY = 8;

    [Header("Puzzle")]
    [SerializeField] private MazePlatePuzzleController _puzzle;
    [Header("Fire")]
    [Tooltip("첫 불 타일이 시작되는 기계 앞면 기준점입니다.")]
    [SerializeField] private Transform _origin;
    [SerializeField] private MazeFireDirection _direction;
    [Range(1, FIRE_TILE_CAPACITY)]
    [SerializeField] private int _length = 1;
    [Min(0.01f)]
    [SerializeField] private float _cellSize = 1.0f;
    [Tooltip("불을 막는 벽과 돌의 LayerMask입니다.")]
    [SerializeField] private LayerMask _blockerLayer;
    [Tooltip("거리 1부터 5까지 순서대로 배치되는 재사용 불 타일입니다.")]
    [SerializeField] private MazeHazardTile[] _fireTiles;

    private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[HIT_BUFFER_CAPACITY];
    private ContactFilter2D _blockerFilter;
    private bool _isSetupValid;
    private bool _isFiring;
    private int _activeTileCount = -1;

    /// <summary>불의 고정 월드 방향입니다.</summary>
    public MazeFireDirection Direction => _direction;

    private void Awake()
    {
        _isSetupValid = TryValidateSetup(true);
        if (!_isSetupValid)
        {
            return;
        }

        _blockerFilter.useLayerMask = true;
        _blockerFilter.layerMask = _blockerLayer;
        _blockerFilter.useTriggers = false;
        Vector2 direction = GetDirectionVector();
        for (int i = 0; i < _fireTiles.Length; i++)
        {
            _fireTiles[i].InitializeFire(this);
            _fireTiles[i].transform.position = (Vector2)_origin.position
                + direction * ((i + 0.5f) * _cellSize);
        }
        SetActiveTileCount(0);
    }

    private void OnEnable()
    {
        if (!_isSetupValid)
        {
            return;
        }
        _puzzle.StateChanged += ApplyPuzzleState;
        ApplyPuzzleState(_puzzle.State);
    }

    private void OnDisable()
    {
        if (_puzzle != null)
        {
            _puzzle.StateChanged -= ApplyPuzzleState;
        }
        _isFiring = false;
        if (_isSetupValid)
        {
            SetActiveTileCount(0);
        }
    }

    private void FixedUpdate()
    {
        if (_isFiring)
        {
            RefreshBlockedLength();
        }
    }

    private void ApplyPuzzleState(MazePlatePuzzleState state)
    {
        _isFiring = state == MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS;
        if (_isFiring)
        {
            RefreshBlockedLength();
        }
        else
        {
            SetActiveTileCount(0);
        }
    }

    private void RefreshBlockedLength()
    {
        Vector2 direction = GetDirectionVector();
        float maxDistance = _length * _cellSize;
        int hitCount = Physics2D.Raycast(
            _origin.position, direction, _blockerFilter, _hitBuffer, maxDistance);
        float nearestDistance = maxDistance;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D blocker = _hitBuffer[i].collider;
            if (blocker == null || blocker.transform.IsChildOf(transform)
                || transform.IsChildOf(blocker.transform))
            {
                continue;
            }
            nearestDistance = Mathf.Min(nearestDistance, _hitBuffer[i].distance);
        }
        int activeCount = Mathf.Clamp(
            Mathf.FloorToInt(nearestDistance / _cellSize + 0.001f), 0, _length);
        SetActiveTileCount(activeCount);
    }

    private void SetActiveTileCount(int activeCount)
    {
        if (_activeTileCount == activeCount)
        {
            return;
        }
        _activeTileCount = activeCount;
        for (int i = 0; i < _fireTiles.Length; i++)
        {
            _fireTiles[i].gameObject.SetActive(i < activeCount);
        }
    }

    private Vector2 GetDirectionVector()
    {
        switch (_direction)
        {
            case MazeFireDirection.MAZE_FIRE_UP: return Vector2.up;
            case MazeFireDirection.MAZE_FIRE_LEFT: return Vector2.left;
            case MazeFireDirection.MAZE_FIRE_DOWN: return Vector2.down;
            default: return Vector2.right;
        }
    }

    private bool TryValidateSetup(bool logIssue)
    {
        string issue = GetSetupIssue();
        if (issue.Length == 0)
        {
            return true;
        }
        if (logIssue)
        {
            Debug.LogError($"[MazeFireEmitter] {name}: {issue}", this);
        }
        return false;
    }

    private string GetSetupIssue()
    {
        if (_puzzle == null || _origin == null || _blockerLayer.value == 0)
        {
            return "퍼즐, 불 시작점과 돌·벽 LayerMask가 필요합니다.";
        }
        if (_length < 1 || _length > FIRE_TILE_CAPACITY || float.IsNaN(_cellSize)
            || float.IsInfinity(_cellSize) || _cellSize <= 0.0f)
        {
            return "불 길이는 1~5, 타일 크기는 0보다 큰 유한한 값이어야 합니다.";
        }
        if (_fireTiles == null || _fireTiles.Length != FIRE_TILE_CAPACITY)
        {
            return "거리 1~5에 대응하는 불 타일 5개가 필요합니다.";
        }
        for (int i = 0; i < _fireTiles.Length; i++)
        {
            if (_fireTiles[i] == null)
            {
                return $"불 타일 참조가 비어 있습니다. index={i}";
            }
            if (!_fireTiles[i].TryValidate(true, out string tileIssue))
            {
                return $"불 타일 설정이 올바르지 않습니다. index={i}, reason={tileIssue}";
            }
            if (_fireTiles[i].gameObject == gameObject
                || transform.IsChildOf(_fireTiles[i].transform))
            {
                return $"불 타일은 발사기 자신이나 발사기의 부모일 수 없습니다. index={i}";
            }
            for (int otherIndex = 0; otherIndex < i; otherIndex++)
            {
                if (_fireTiles[otherIndex] == _fireTiles[i])
                {
                    return $"같은 불 타일이 중복 연결되었습니다. index={i}";
                }
            }
        }
        return string.Empty;
    }

#if UNITY_EDITOR
    private void OnValidate() => TryValidateSetup(false);
#endif
}
