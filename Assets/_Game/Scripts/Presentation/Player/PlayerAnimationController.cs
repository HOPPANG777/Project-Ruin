//=============================================================================
// File:        PlayerAnimationController.cs
//
// Description: 이동 상태에 맞춰 플레이어 스프라이트 프레임을 재생한다.
//
// History:     09/28/2026 + Created -- Codex
//              10/01/2026 + 이동 속도에 따른 재생 배율 추가 -- Codex
//=============================================================================

using UnityEngine;

/// <summary>주입된 이동 상태에 따라 8방향 대기·걷기·달리기를 재생합니다.</summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("플레이어 프레임을 표시할 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Tooltip("24개 방향별 애니메이션의 프레임 데이터입니다.")]
    [SerializeField] private PlayerAnimationLibrarySO _library;

    [Header("Playback")]
    [Tooltip("걷기 애니메이션을 원래 FPS로 재생할 이동 속도로, 낮추면 발 움직임이 빨라집니다.")]
    [Min(0.01f)]
    [SerializeField] private float _walkReferenceSpeed = 7f;

    [Tooltip("달리기 애니메이션을 원래 FPS로 재생할 이동 속도로, 낮추면 발 움직임이 빨라집니다.")]
    [Min(0.01f)]
    [SerializeField] private float _runReferenceSpeed = 10.5f;

    private IPlayerMotionSource _movement;
    private PlayerAnimationMotion _motion = PlayerAnimationMotion.PLAYER_ANIMATION_IDLE;
    private PlayerFacing _facing = PlayerFacing.PLAYER_FACING_FRONT;
    private int _frame;
    private float _elapsed;

    //=============================================================================
    // PlayerAnimationController.TryInit
    //=============================================================================
    // Description: 이동 상태를 주입하고 첫 대기 프레임을 표시한다.
    // Parameters:  ( IPlayerMotionSource movement )
    // Return:      bool — 재생 준비가 완료되면 true
    //=============================================================================
    /// <summary>이동 상태를 주입하고 애니메이션 재생을 준비합니다.</summary>
    public bool TryInit(IPlayerMotionSource movement)
    {
        if (movement == null || _spriteRenderer == null || _library == null
            || !_library.IsComplete)
        {
            Debug.LogError("[PlayerAnimationController] 이동 상태, 렌더러 또는 160개 프레임이 누락되었습니다.", this);
            return false;
        }

        _movement = movement;
        _motion = PlayerAnimationMotion.PLAYER_ANIMATION_IDLE;
        _facing = PlayerFacing.PLAYER_FACING_FRONT;
        _frame = 0;
        _elapsed = 0f;
        ShowFrame();
        return true;
    }

    private void LateUpdate()
    {
        if (_movement == null) return;

        Vector3 direction = _movement.MoveDirection;
        float speed = Mathf.Abs(_movement.CurrentMoveSpeed);
        bool moving = direction.sqrMagnitude > 0.0001f && speed > 0f;
        PlayerAnimationMotion motion = PlayerAnimationMotion.PLAYER_ANIMATION_IDLE;
        if (moving)
        {
            motion = _movement.IsRunning
                ? PlayerAnimationMotion.PLAYER_ANIMATION_RUN
                : PlayerAnimationMotion.PLAYER_ANIMATION_WALK;
        }
        PlayerFacing facing = moving ? GetFacing(direction) : _facing;

        if (motion != _motion || facing != _facing)
        {
            _motion = motion;
            _facing = facing;
            _frame = 0;
            _elapsed = 0f;
            ShowFrame();
            return;
        }

        float duration = _library.FrameDuration(_motion);
        // 대기는 원래 속도를 유지하고, 이동 동작만 이동 속도에 비례해 재생합니다.
        float referenceSpeed = _motion == PlayerAnimationMotion.PLAYER_ANIMATION_RUN
            ? _runReferenceSpeed : _walkReferenceSpeed;
        float playbackSpeed = moving ? speed / Mathf.Max(0.01f, referenceSpeed) : 1f;
        _elapsed += Time.deltaTime * playbackSpeed;
        if (_elapsed < duration) return;

        int advance = Mathf.FloorToInt(_elapsed / duration);
        _elapsed -= advance * duration;
        _frame = (_frame + advance) % _library.FrameCount(_motion);
        ShowFrame();
    }

    private void ShowFrame() =>
        _spriteRenderer.sprite = _library.GetFrame(_motion, _facing, _frame);

    private static PlayerFacing GetFacing(Vector3 direction)
    {
        // 화면 아래(-Y)가 캐릭터의 정면입니다.
        float angle = Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg;
        int sector = Mathf.FloorToInt(Mathf.Repeat(angle + 22.5f, 360f) / 45f);
        return (PlayerFacing)sector;
    }
}
