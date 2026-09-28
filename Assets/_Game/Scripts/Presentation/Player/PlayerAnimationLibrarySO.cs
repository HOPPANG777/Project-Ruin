//=============================================================================
// File:        PlayerAnimationLibrarySO.cs
//
// Description: 8방향 대기·걷기·달리기의 프레임을 순서대로 보관한다.
//
// History:     09/28/2026 + Created -- Codex
//=============================================================================

using UnityEngine;

/// <summary>플레이어의 세 동작과 여덟 방향에 대응하는 스프라이트를 보관합니다.</summary>
public sealed class PlayerAnimationLibrarySO : ScriptableObject
{
    public const int DIRECTION_COUNT = 8;
    public const int IDLE_FRAME_COUNT = 4;
    public const int MOVE_FRAME_COUNT = 8;

    [Header("Frames")]
    [Tooltip("방향별로 네 프레임씩 이어지는 대기 스프라이트입니다.")]
    [SerializeField] private Sprite[] _idleFrames;

    [Tooltip("방향별로 여덟 프레임씩 이어지는 걷기 스프라이트입니다.")]
    [SerializeField] private Sprite[] _walkFrames;

    [Tooltip("방향별로 여덟 프레임씩 이어지는 달리기 스프라이트입니다.")]
    [SerializeField] private Sprite[] _runFrames;

    [Header("Playback")]
    [Tooltip("대기 애니메이션의 초당 프레임 수입니다.")]
    [Min(1f)] [SerializeField] private float _idleFramesPerSecond = 6f;

    [Tooltip("걷기 애니메이션의 초당 프레임 수입니다.")]
    [Min(1f)] [SerializeField] private float _walkFramesPerSecond = 10f;

    [Tooltip("달리기 애니메이션의 초당 프레임 수입니다.")]
    [Min(1f)] [SerializeField] private float _runFramesPerSecond = 12f;

    /// <summary>24개 방향별 애니메이션의 모든 프레임이 연결됐는지 확인합니다.</summary>
    public bool IsComplete => HasFrames(_idleFrames, DIRECTION_COUNT * IDLE_FRAME_COUNT)
        && HasFrames(_walkFrames, DIRECTION_COUNT * MOVE_FRAME_COUNT)
        && HasFrames(_runFrames, DIRECTION_COUNT * MOVE_FRAME_COUNT);

    /// <summary>동작과 방향에 맞는 애니메이션의 프레임 수를 반환합니다.</summary>
    public int FrameCount(PlayerAnimationMotion motion) =>
        motion == PlayerAnimationMotion.PLAYER_ANIMATION_IDLE
            ? IDLE_FRAME_COUNT : MOVE_FRAME_COUNT;

    /// <summary>동작에 맞는 한 프레임의 재생 시간을 반환합니다.</summary>
    public float FrameDuration(PlayerAnimationMotion motion)
    {
        float framesPerSecond = motion switch
        {
            PlayerAnimationMotion.PLAYER_ANIMATION_IDLE => _idleFramesPerSecond,
            PlayerAnimationMotion.PLAYER_ANIMATION_WALK => _walkFramesPerSecond,
            _ => _runFramesPerSecond
        };
        return 1f / Mathf.Max(1f, framesPerSecond);
    }

    /// <summary>동작·방향·프레임에 해당하는 스프라이트를 반환합니다.</summary>
    public Sprite GetFrame(PlayerAnimationMotion motion, PlayerFacing facing, int frame)
    {
        int frameCount = FrameCount(motion);
        Sprite[] frames = motion switch
        {
            PlayerAnimationMotion.PLAYER_ANIMATION_IDLE => _idleFrames,
            PlayerAnimationMotion.PLAYER_ANIMATION_WALK => _walkFrames,
            _ => _runFrames
        };
        return frames[(int)facing * frameCount + frame];
    }

#if UNITY_EDITOR
    /// <summary>에디터 빌더가 정렬된 전체 프레임을 애셋에 기록합니다.</summary>
    public void SetFrames(Sprite[] idleFrames, Sprite[] walkFrames, Sprite[] runFrames)
    {
        _idleFrames = idleFrames;
        _walkFrames = walkFrames;
        _runFrames = runFrames;
    }
#endif

    private static bool HasFrames(Sprite[] frames, int expectedCount)
    {
        if (frames == null || frames.Length != expectedCount) return false;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null) return false;
        }

        return true;
    }
}

/// <summary>플레이어 애니메이션의 세 가지 동작입니다.</summary>
public enum PlayerAnimationMotion
{
    PLAYER_ANIMATION_IDLE = 0,
    PLAYER_ANIMATION_WALK,
    PLAYER_ANIMATION_RUN
}

/// <summary>오른쪽에서 화면 아래쪽으로 나열한 여덟 방향입니다.</summary>
public enum PlayerFacing
{
    PLAYER_FACING_RIGHT = 0,
    PLAYER_FACING_FRONT_RIGHT,
    PLAYER_FACING_FRONT,
    PLAYER_FACING_FRONT_LEFT,
    PLAYER_FACING_LEFT,
    PLAYER_FACING_BACK_LEFT,
    PLAYER_FACING_BACK,
    PLAYER_FACING_BACK_RIGHT
}
