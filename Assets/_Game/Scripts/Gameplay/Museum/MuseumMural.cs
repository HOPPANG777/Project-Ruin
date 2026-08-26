//=============================================================================
// File:        MuseumMural.cs
// Description: 플레이어 Trigger 진입 시 벽화를 한 번 개봉하고 최종 표시를 복원한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MuseumMural : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("museum_murals.csv의 mural_id와 일치해야 합니다.")]
    [SerializeField] private string _muralId;

    [Header("Visual State")]
    [Tooltip("개봉 전 검은 가림 표시입니다.")]
    [SerializeField] private GameObject _coveredRoot;
    [Tooltip("개봉 후 원본 벽화 표시입니다.")]
    [SerializeField] private GameObject _revealedRoot;
    [Tooltip("최초 개봉 때만 재생할 빛 Particle입니다. 누락돼도 개봉 상태는 유지됩니다.")]
    [SerializeField] private ParticleSystem _lightEffect;

    private IMuseumService _museum;

    private void Reset()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        if (trigger != null) trigger.isTrigger = true;
    }

    private void OnEnable()
    {
        if (_museum != null) RefreshVisualState();
    }

    private void OnDisable() => StopEffect();

    private void Start()
    {
        if (_museum == null)
        {
            GameRoot gameRoot = GameRoot.Instance;
            Bind(gameRoot != null ? gameRoot.Museum : null);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<UnitPlayer>() != null)
            TryReveal();
    }

    /// <summary>테스트 또는 씬 초기화에서 박물관 서비스를 연결하고 저장 상태를 반영합니다.</summary>
    public void Bind(IMuseumService museum)
    {
        _museum = museum;
        RefreshVisualState();
    }

    /// <summary>반환 조건을 만족하면 개봉 상태를 한 번 기록하고 빛 효과를 재생합니다.</summary>
    public bool TryReveal()
    {
        if (_museum == null)
            return false;
        if (!_museum.TryRevealMural(_muralId, out MuseumRevealFailure failure))
        {
            if (failure != MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_ALREADY_REVEALED
                && failure != MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_TECHNOLOGY_NOT_RETURNED)
                Debug.LogWarning($"[MuseumMural] 벽화 개봉 실패: id={_muralId}, failure={failure}", this);
            return false;
        }

        ApplyRevealedState(false);
        if (_lightEffect != null) _lightEffect.Play(true);
        return true;
    }

    /// <summary>로드된 개봉 상태를 이펙트 재생 없이 즉시 표시합니다.</summary>
    public void RefreshVisualState()
    {
        bool isRevealed = _museum != null && _museum.IsMuralRevealed(_muralId);
        if (isRevealed)
            ApplyRevealedState(true);
        else
            ApplyCoveredState();
    }

    private void ApplyCoveredState()
    {
        _coveredRoot?.SetActive(true);
        _revealedRoot?.SetActive(false);
        StopEffect();
    }

    private void ApplyRevealedState(bool clearEffect)
    {
        _coveredRoot?.SetActive(false);
        _revealedRoot?.SetActive(true);
        if (clearEffect) StopEffect();
    }

    private void StopEffect()
    {
        if (_lightEffect != null)
            _lightEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        if (trigger != null && !trigger.isTrigger)
            Debug.LogError($"[MuseumMural] {name}: Collider2D는 Trigger여야 합니다.", this);
        if (string.IsNullOrWhiteSpace(_muralId))
            Debug.LogError($"[MuseumMural] {name}: Mural ID가 비어 있습니다.", this);
        if (_coveredRoot == null || _revealedRoot == null)
            Debug.LogWarning($"[MuseumMural] {name}: 가림 또는 원본 표시 참조가 누락됐습니다.", this);
    }
#endif
}
