//=============================================================================
// File:        MazeHazardTile.cs
//
// Description: 불·가시 타일의 플레이어 피해와 불 교차 표시를 처리합니다.
//
// History:     08/08/2026 + Created -- Codex
//
//=============================================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class MazeHazardTile : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("플레이어가 이 타일과 겹쳐 있는 동안 시도할 피해량입니다.")]
    [Min(0.0f)]
    [SerializeField] private float _damage = 1.0f;
    [Tooltip("타일 칸의 피해 범위를 판정하는 Trigger Collider입니다.")]
    [SerializeField] private Collider2D _damageTrigger;
    [Tooltip("활성 위험 타일을 플레이어가 통과하지 못하게 하는 Collider입니다.")]
    [SerializeField] private Collider2D _movementBlocker;
    [Header("Fire Overlap")]
    [Tooltip("다른 방향의 불과 교차할 때 표시할 거리별 이미지입니다.")]
    [SerializeField] private GameObject _overlapMarker;

    private MazeFireEmitter _fireOwner;
    private UnitPlayer _overlappingPlayer;
    private bool _isSetupValid;
    private int _playerColliderCount;
    private int _fireOverlapCount;
    private float _lastDamageFixedTime = float.NegativeInfinity;

    private void Reset()
    {
        if (TryGetComponent(out Collider2D tileTrigger))
        {
            tileTrigger.isTrigger = true;
            _damageTrigger = tileTrigger;
        }
    }

    private void Awake()
    {
        _isSetupValid = TryValidate(false, out string issue);
        if (!_isSetupValid)
        {
            Debug.LogError($"[MazeHazardTile] {name}: {issue}", this);
        }
        if (_overlapMarker != null)
        {
            _overlapMarker.SetActive(false);
        }
    }

    private void OnDisable()
    {
        _overlappingPlayer = null;
        _playerColliderCount = 0;
        _fireOverlapCount = 0;
        _lastDamageFixedTime = float.NegativeInfinity;
        if (_overlapMarker != null)
        {
            _overlapMarker.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isSetupValid)
        {
            return;
        }
        UnitPlayer player = other.GetComponentInParent<UnitPlayer>();
        if (player != null)
        {
            if (_overlappingPlayer == null || _overlappingPlayer == player)
            {
                _overlappingPlayer = player;
                _playerColliderCount++;
            }
            return;
        }
        AddFireOverlap(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_isSetupValid || _overlappingPlayer == null
            || (other.transform != _overlappingPlayer.transform
                && !other.transform.IsChildOf(_overlappingPlayer.transform))
            || _lastDamageFixedTime == Time.fixedTime)
        {
            return;
        }
        _lastDamageFixedTime = Time.fixedTime;
        _overlappingPlayer.TakeDamage(_damage);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_isSetupValid)
        {
            return;
        }
        if (_overlappingPlayer != null
            && (other.transform == _overlappingPlayer.transform
                || other.transform.IsChildOf(_overlappingPlayer.transform)))
        {
            _playerColliderCount--;
            if (_playerColliderCount <= 0)
            {
                _overlappingPlayer = null;
                _playerColliderCount = 0;
            }
            return;
        }
        RemoveFireOverlap(other);
    }

    /// <summary>불 발사기와 거리별 교차 이미지를 초기화 시 한 번 연결합니다.</summary>
    internal void InitializeFire(MazeFireEmitter owner)
    {
        _fireOwner = owner;
        _fireOverlapCount = 0;
        _overlapMarker.SetActive(false);
    }

    /// <summary>피해 Trigger와 선택적인 불 교차 표시 설정을 검증합니다.</summary>
    internal bool TryValidate(bool requireOverlapMarker, out string issue)
    {
        issue = string.Empty;
        if (_damageTrigger == null || !_damageTrigger.isTrigger
            || _damageTrigger.gameObject != gameObject)
        {
            issue = "같은 오브젝트의 피해 Collider가 Trigger로 설정되어야 합니다.";
        }
        else if (_movementBlocker == null || _movementBlocker.isTrigger
            || _movementBlocker == _damageTrigger)
        {
            issue = "피해 Trigger와 다른 이동 차단 Collider가 필요합니다.";
        }
        else if (float.IsNaN(_damage) || float.IsInfinity(_damage) || _damage <= 0.0f)
        {
            issue = "피해량은 0보다 큰 유한한 값이어야 합니다.";
        }
        else if (requireOverlapMarker && _overlapMarker == null)
        {
            issue = "불 타일에는 교차 표시 이미지가 필요합니다.";
        }
        return issue.Length == 0;
    }

    private void AddFireOverlap(Collider2D other)
    {
        if (!TryGetOwnedFireTile(other, out MazeHazardTile otherTile)
            || _fireOwner.GetInstanceID() > otherTile._fireOwner.GetInstanceID())
        {
            return;
        }
        _fireOverlapCount++;
        _overlapMarker.SetActive(true);
    }

    private void RemoveFireOverlap(Collider2D other)
    {
        if (!TryGetOwnedFireTile(other, out MazeHazardTile otherTile)
            || _fireOwner.GetInstanceID() > otherTile._fireOwner.GetInstanceID())
        {
            return;
        }
        _fireOverlapCount = Mathf.Max(0, _fireOverlapCount - 1);
        _overlapMarker.SetActive(_fireOverlapCount > 0);
    }

    private bool TryGetOwnedFireTile(Collider2D other, out MazeHazardTile otherTile)
    {
        other.TryGetComponent(out otherTile);
        return _fireOwner != null && otherTile != null && otherTile != this
            && otherTile._fireOwner != null && otherTile._fireOwner != _fireOwner
            && otherTile._fireOwner.Direction != _fireOwner.Direction;
    }

#if UNITY_EDITOR
    private void OnValidate() => TryValidate(false, out _);
#endif
}
