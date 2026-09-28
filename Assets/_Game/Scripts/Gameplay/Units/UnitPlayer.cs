//=============================================================================
// File:        UnitPlayer.cs
//
// Description: 플레이어의 카메라 대상·피격 무적과 현재 메뉴 입력 참조 등록을 관리합니다.
//
// History:     08/08/2026 + 시간값 기반 피격 무적 처리 추가 -- Codex
//              08/31/2026 + 활성 플레이어의 메뉴 입력 참조 등록 및 안전한 해제 -- Codex
//              09/28/2026 + 공격 컨트롤러의 이동 참조 주입 누락 수정 -- Codex
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
    // 종료 시 전역 접근 없이 해제할 수 있도록 등록 대상과 자기 입력 참조를 보관합니다.
    private GameRoot _menuRoot;
    private PlayerInteractionController _menuInteraction;
    private PlayerMoveController _menuMovement;

    // 최초 씬에서는 모든 Awake 이후 Start에서 다시 연결하고, 이후 씬은 OnEnable로 연결합니다.
    private void OnEnable() => BindMenuInput();
    private void Start()
    {
        BindMenuInput();
        BindAnimation();
        BindAttack();
    }
    private void OnDisable()
    {
        if (_menuRoot != null) _menuRoot.UnbindMenuPlayer(_menuInteraction, _menuMovement);
        _menuRoot = null;
    }

    private void BindMenuInput()
    {
        if (!GameRoot.HasInstance) return;
        _menuInteraction = GetComponent<PlayerInteractionController>();
        _menuMovement = GetComponent<PlayerMoveController>();
        if (_menuInteraction == null && _menuMovement == null) return;
        _menuRoot = GameRoot.Instance;
        _menuRoot.BindMenuPlayer(_menuInteraction, _menuMovement);
    }

    // 플레이어가 소유한 컴포넌트가 인터페이스로 이동 상태를 전달합니다.
    private void BindAnimation()
    {
        PlayerAnimationController animation = GetComponent<PlayerAnimationController>();
        if (animation == null) return;

        PlayerMoveController movement = GetComponent<PlayerMoveController>();
        animation.TryInit(movement);
    }

    // 프리팹 인스턴스가 자신의 이동 참조를 사용하도록 연결합니다.
    private void BindAttack()
    {
        if (!TryGetComponent(out PlayerAttackController attack)) return;
        if (!TryGetComponent(out PlayerMoveController movement)) return;

        attack.Construct(movement);
    }

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
