//=============================================================================
// File:        IPlayerMotionSource.cs
//
// Description: 플레이어 애니메이션에 필요한 이동 상태를 노출한다.
//
// History:     09/28/2026 + Created -- Codex
//=============================================================================

/// <summary>플레이어의 이동 방향과 달리기 상태를 제공하는 계약입니다.</summary>
public interface IPlayerMotionSource : IMoveable
{
    /// <summary>현재 이동에 달리기 속도를 적용하는지 반환합니다.</summary>
    bool IsRunning { get; }
}
