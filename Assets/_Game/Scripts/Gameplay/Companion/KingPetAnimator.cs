//=============================================================================
// File:        KingPetAnimator.cs
//
// Description: 왕(펫)의 실제 이동 방향에 따라 4방향 걷기·대기 프레임을 재생한다.
//
// History:     11/07/2026 + Created -- Claude
//=============================================================================

using System;
using UnityEngine;

/// <summary>실제 이동 방향 기준 4방향 걷기 애니메이션과, 정지 시 마지막 방향의 대기 애니메이션을 재생합니다.</summary>
[DisallowMultipleComponent]
public sealed class KingPetAnimator : MonoBehaviour
{
    [Serializable]
    private struct DirectionFrames
    {
        public Sprite[] Idle;
        public Sprite[] Walk;
    }

    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private KingPetFollower _follower;

    [Tooltip("위쪽 이동(후면)")] [SerializeField] private DirectionFrames _up;
    [Tooltip("아래쪽 이동(정면)")] [SerializeField] private DirectionFrames _down;
    [Tooltip("왼쪽 이동")] [SerializeField] private DirectionFrames _left;
    [Tooltip("오른쪽 이동")] [SerializeField] private DirectionFrames _right;

    [Min(1f)] [SerializeField] private float _framesPerSecond = 8f;

    private DirectionFrames _facing;
    private bool _moving;
    private float _elapsed;

    private void Awake() => _facing = _down;

    private void LateUpdate()
    {
        Vector2 dir = _follower.MoveDirection;
        bool moving = dir.sqrMagnitude > 0.0001f;
        DirectionFrames facing = moving ? Pick(dir) : _facing;
        if (moving != _moving || !ReferenceEquals(facing.Walk, _facing.Walk))
            _elapsed = 0f;
        _moving = moving;
        _facing = facing;

        Sprite[] frames = moving ? facing.Walk : facing.Idle;
        if (frames == null || frames.Length == 0) return;
        _elapsed += Time.deltaTime;
        _renderer.sprite = frames[(int)(_elapsed * _framesPerSecond) % frames.Length];
    }

    private DirectionFrames Pick(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) return dir.x > 0f ? _right : _left;
        return dir.y > 0f ? _up : _down;
    }
}
