//=============================================================================
// File:        KingPetFollower.cs
//
// Description: 플레이어가 지나간 경로를 따라 약 2블록 뒤에서 동행하는 왕(펫)의 이동.
//
// History:     11/07/2026 + Created -- Claude
//=============================================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>플레이어의 이동 경로를 기록해 순서대로 따라가며, 막히면 경로 뒤쪽으로 순간이동합니다.</summary>
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public sealed class KingPetFollower : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("플레이어 이동 경로 기준으로 유지할 거리(블록 수)입니다.")]
    [Min(0f)] [SerializeField] private float _followBlocks = 2f;

    [Tooltip("1블록의 월드 길이입니다. 타일 크기에 맞춥니다.")]
    [Min(0.01f)] [SerializeField] private float _blockSize = 1f;

    [Tooltip("플레이어 경로를 새로 기록하는 최소 간격입니다.")]
    [Min(0.01f)] [SerializeField] private float _sampleSpacing = 0.1f;

    [Tooltip("경로 지점에 도착했다고 보는 반경입니다.")]
    [Min(0.001f)] [SerializeField] private float _arriveRadius = 0.05f;

    [Header("Stuck")]
    [Tooltip("이동하려 하지만 진행하지 못한 채 이 시간이 지나면 순간이동합니다.")]
    [Min(0.01f)] [SerializeField] private float _stuckSeconds = 0.3f;

    private readonly List<Vector2> _path = new List<Vector2>();
    private Rigidbody2D _rigidbody;
    private Transform _player;
    private IPlayerMotionSource _motion;
    private Vector2 _lastSample;
    private Vector2 _previousPosition;
    private float _lastSpeed;
    private float _stuckTime;

    /// <summary>실제 이동 방향입니다. 정지 중에는 zero입니다.</summary>
    public Vector2 MoveDirection { get; private set; }

    private float FollowDistance => _followBlocks * _blockSize;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;
    }

    /// <summary>추적을 시작합니다. 플레이어와는 충돌하지 않고 벽·지형과는 충돌합니다.</summary>
    public void Activate(PlayerMoveController player)
    {
        _player = player.transform;
        _motion = player;
        _lastSpeed = player.MoveSpeed;
        _path.Clear();
        _lastSample = _player.position;
        _previousPosition = _rigidbody.position;
        _stuckTime = 0f;

        foreach (Collider2D mine in GetComponentsInChildren<Collider2D>())
            foreach (Collider2D theirs in _player.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(mine, theirs, true);
    }

    /// <summary>추적을 끝내고 경로를 비웁니다.</summary>
    public void Deactivate()
    {
        _player = null;
        _path.Clear();
        MoveDirection = Vector2.zero;
        if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        float dt = Time.fixedDeltaTime;
        Vector2 playerPos = _player.position;
        Vector2 pos = _rigidbody.position;

        if (_motion.CurrentMoveSpeed > 0f) _lastSpeed = _motion.CurrentMoveSpeed;
        if ((playerPos - _lastSample).sqrMagnitude >= _sampleSpacing * _sampleSpacing)
        {
            _path.Add(playerPos);
            _lastSample = playerPos;
        }

        while (_path.Count > 0 && (_path[0] - pos).sqrMagnitude <= _arriveRadius * _arriveRadius)
            _path.RemoveAt(0);

        bool wantsMove = RemainingDistance(pos, playerPos) > FollowDistance;
        if (!wantsMove)
        {
            Stop();
            return;
        }

        float moved = (pos - _previousPosition).magnitude;
        _previousPosition = pos;
        _stuckTime = moved < _lastSpeed * dt * 0.2f ? _stuckTime + dt : 0f;
        if (_stuckTime >= _stuckSeconds)
        {
            TeleportBehindPlayer(playerPos);
            return;
        }

        Vector2 target = _path.Count > 0 ? _path[0] : playerPos;
        Vector2 toTarget = target - pos;
        float speed = Mathf.Min(_lastSpeed, toTarget.magnitude / dt);
        MoveDirection = toTarget.normalized;
        _rigidbody.linearVelocity = MoveDirection * speed;
    }

    private void Stop()
    {
        _stuckTime = 0f;
        _previousPosition = _rigidbody.position;
        MoveDirection = Vector2.zero;
        _rigidbody.linearVelocity = Vector2.zero;
    }

    // 현재 위치에서 남은 경로를 거쳐 플레이어까지의 경로 길이입니다.
    private float RemainingDistance(Vector2 pos, Vector2 playerPos)
    {
        float total = 0f;
        Vector2 prev = pos;
        for (int i = 0; i < _path.Count; i++)
        {
            total += Vector2.Distance(prev, _path[i]);
            prev = _path[i];
        }
        return total + Vector2.Distance(prev, playerPos);
    }

    // 플레이어 경로를 거꾸로 FollowDistance만큼 거슬러 올라간 지점으로 이동하고 추적을 재개합니다.
    private void TeleportBehindPlayer(Vector2 playerPos)
    {
        Vector2 prev = playerPos;
        float remaining = FollowDistance;
        Vector2 point = playerPos;
        bool found = false;

        for (int i = _path.Count - 1; i >= 0; i--)
        {
            float segment = Vector2.Distance(prev, _path[i]);
            if (segment >= remaining)
            {
                point = prev + (_path[i] - prev).normalized * remaining;
                _path.RemoveRange(0, i + 1);
                found = true;
                break;
            }
            remaining -= segment;
            prev = _path[i];
        }
        if (!found) point = prev;

        _rigidbody.position = point;
        transform.position = point;
        _rigidbody.linearVelocity = Vector2.zero;
        MoveDirection = Vector2.zero;
        _previousPosition = point;
        _stuckTime = 0f;
    }
}
