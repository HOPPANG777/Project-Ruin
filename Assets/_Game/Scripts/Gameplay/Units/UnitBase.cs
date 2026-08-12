//=============================================================================
// File:        UnitBase.cs
//
// Description: 유닛의 생존 여부와 체력 변경 및 사망 이벤트를 관리합니다.
//
// History:     08/08/2026 + 체력 변경·사망·완전 회복 계약 보완 -- Codex
//
//=============================================================================

using System;
using UnityEngine;

//=============================================================================
//
// Synopsis:   UnitBase;
//                - 체력을 0부터 최대 체력 사이로 유지
//                - 체력 변경과 최초 사망을 이벤트로 전달
//
//=============================================================================
public class UnitBase : MonoBehaviour, IKillable, IDamageable, IHealth
{
    [Header("Unit")]
    [Tooltip("유닛이 현재 살아 있는지 나타냅니다.")]
    [SerializeField] private bool _isAlive;
    [Tooltip("유닛의 최대 체력입니다.")]
    [SerializeField] private float _maxHealth;
    [Tooltip("유닛의 현재 체력입니다.")]
    [SerializeField] private float _currentHealth;

    /// <summary>유닛이 현재 살아 있는지 반환합니다.</summary>
    public bool IsAlive => _isAlive;
    /// <summary>유닛의 최대 체력을 반환합니다.</summary>
    public float MaxHealth => _maxHealth;
    /// <summary>유닛의 현재 체력을 반환합니다.</summary>
    public float CurrentHealth => _currentHealth;

    /// <summary>현재 체력과 최대 체력이 바뀌었을 때 전달합니다.</summary>
    public event Action<float, float> HealthChanged;
    /// <summary>살아 있던 유닛이 처음 사망했을 때 전달합니다.</summary>
    public event Action Died;

    protected virtual void Awake()
    {
        if (!IsFinite(_maxHealth) || _maxHealth < 0.0f)
        {
            _maxHealth = 0.0f;
        }
        if (!IsFinite(_currentHealth))
        {
            _currentHealth = 0.0f;
        }
        _currentHealth = Mathf.Clamp(_currentHealth, 0.0f, _maxHealth);
        _isAlive = _isAlive && _currentHealth > 0.0f;
    }

    /// <summary>유효한 양수 피해를 현재 체력에 한 번 반영합니다.</summary>
    public virtual void TakeDamage(float damage)
    {
        if (!_isAlive || !IsFinite(damage) || damage <= 0.0f)
        {
            return;
        }

        _currentHealth = Mathf.Max(0.0f, _currentHealth - damage);
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
        if (_currentHealth <= 0.0f)
        {
            Die();
        }
    }

    /// <summary>살아 있는 유닛을 한 번 사망 처리합니다.</summary>
    public virtual void Die()
    {
        if (!_isAlive)
        {
            return;
        }

        bool healthChanged = _currentHealth > 0.0f;
        _currentHealth = 0.0f;
        _isAlive = false;
        if (healthChanged)
        {
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
        Died?.Invoke();
    }

    /// <summary>유효한 최대 체력으로 유닛을 완전히 회복하고 부활시킵니다.</summary>
    public virtual bool TryRestoreFullHealth()
    {
        if (!IsFinite(_maxHealth) || _maxHealth <= 0.0f)
        {
            Debug.LogError($"[UnitBase] 최대 체력이 0보다 커야 합니다. object={name}", this);
            return false;
        }

        _currentHealth = _maxHealth;
        _isAlive = true;
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
