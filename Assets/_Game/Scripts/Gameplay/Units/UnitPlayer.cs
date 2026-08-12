//=============================================================================
// File:        UnitPlayer.cs
//
// Description: 플레이어의 카메라 대상과 피격 후 무적 시간을 관리합니다.
//
// History:     08/08/2026 + 시간값 기반 피격 무적 처리 추가 -- Codex
//
//=============================================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitPlayer : UnitBase, ICameraTarget
{
    [Header("Camera Target")]
    [Tooltip("카메라가 추적할 대상이며, 비어 있으면 플레이어 자신을 사용합니다.")]
    [SerializeField] private GameObject _cameraTarget;

    [Header("Damage")]
    [Tooltip("피해를 받은 뒤 다음 피해를 무시하는 시간(초)입니다.")]
    [Min(0.0f)]
    [SerializeField] private float _invulnerabilityDuration = 0.75f;

    private float _invulnerableUntil;

    /// <summary>카메라가 추적할 플레이어 대상입니다.</summary>
    public GameObject CameraTarget => _cameraTarget != null ? _cameraTarget : gameObject;
    /// <summary>현재 피격 무적 시간이 남아 있는지 반환합니다.</summary>
    public bool IsInvulnerable => Time.time < _invulnerableUntil;

    /// <summary>무적 시간이 끝난 경우에만 피해와 새 무적 시간을 적용합니다.</summary>
    public override void TakeDamage(float damage)
    {
        if (!IsAlive || IsInvulnerable || float.IsNaN(damage)
            || float.IsInfinity(damage) || damage <= 0.0f)
        {
            return;
        }

        _invulnerableUntil = Time.time + _invulnerabilityDuration;
        base.TakeDamage(damage);
    }

    /// <summary>부활할 때 이전 피격의 무적 시간도 함께 초기화합니다.</summary>
    public override bool TryRestoreFullHealth()
    {
        _invulnerableUntil = 0.0f;
        return base.TryRestoreFullHealth();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (float.IsNaN(_invulnerabilityDuration)
            || float.IsInfinity(_invulnerabilityDuration)
            || _invulnerabilityDuration < 0.0f)
        {
            _invulnerabilityDuration = 0.0f;
        }
    }
#endif
}
