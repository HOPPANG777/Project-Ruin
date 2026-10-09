//=============================================================================
// File:        PushableStone.cs
// Description: 돌 밀기 스킬과 홀드 입력을 확인해 돌의 물리 이동을 처리한다.
// History:     07/31/2026 + Created -- Codex
//=============================================================================
using UnityEngine;
// 스킬·플레이어 위치·전방 공간을 확인하고 초기 위치 복구를 지원합니다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class PushableStone : MonoBehaviour, IInteractable, IHoldInteractable,
    IInteractionContextReceiver
{
    // 한 돌이 동시에 접촉할 수 있는 벽·돌 후보만 담는 소규모 고정 버퍼입니다.
    private const int OBSTACLE_BUFFER_SIZE = 8;
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    [Header("Identity")]
    [Tooltip("씬과 향후 저장에서 돌을 구분할 고유 ID입니다.")]
    [SerializeField] private string _objectId;
    [Tooltip("돌이 속한 퍼즐을 구분하는 고유 ID입니다.")]
    [SerializeField] private string _puzzleId;
    [Tooltip("돌을 밀기 위해 플레이어가 획득해야 하는 환경 스킬입니다.")]
    [SerializeField] private SkillDataSO _requiredSkill;
    [Header("Movement")]
    [Tooltip("직접 연결하지 않으면 상호작용한 플레이어의 이동 컴포넌트를 사용합니다.")]
    [SerializeField] private PlayerMoveController _playerMoveController;
    [Tooltip("홀드 중 돌이 초당 이동하는 거리입니다.")]
    [Min(0f), SerializeField] private float _pushSpeed = 2.0f;
    [Tooltip("벽과 다른 돌처럼 전방 이동을 막는 레이어입니다.")]
    [SerializeField] private LayerMask _obstacleLayer;
    [Tooltip("한 물리 프레임 이동 거리에 더할 전방 여유 검사 거리입니다.")]
    [Min(0f), SerializeField] private float _obstacleCheckDistance = 0.05f;
    [Header("Puzzle")]
    [Tooltip("퍼즐 초기화 시 돌을 시작 위치로 되돌릴 수 있는지 결정합니다.")]
    [SerializeField] private bool _canReset = true;
    [Tooltip("퍼즐 완료 후 이 돌을 더 이상 밀 수 없게 할지 결정합니다.")]
    [SerializeField] private bool _lockWhenPuzzleCompleted = true;
    private readonly RaycastHit2D[] _obstacleHits = new RaycastHit2D[OBSTACLE_BUFFER_SIZE];
    private Rigidbody2D _rigidbody;
    private Collider2D _stoneCollider;
    private ContactFilter2D _obstacleFilter;
    private IMoveable _playerMove;
    private Transform _playerTransform;
    private ISkillService _skillService;
    private Vector2 _initialPosition;
    private float _initialRotation;
    private bool _isHolding;
    private bool _isPuzzleCompleted;
    // Beam 소유권과 초기 스냅샷은 Awake/저장 복원 순서와 무관하게 한 번만 설정합니다.
    private bool _hasInitialState;
    private BeamPuzzleController _beamOwner;
    private RigidbodyType2D _initialBodyType;
    /// <summary>퍼즐 상태를 배열 순서와 무관하게 대응시키는 돌 ID입니다.</summary>
    public string ObjectId => _objectId;
    /// <summary>Beam에 명시적으로 등록한 이동 차단 충돌체입니다.</summary>
    public Collider2D BodyCollider => _stoneCollider != null ? _stoneCollider : GetComponent<Collider2D>();
    /// <summary>돌을 명시적으로 등록한 Beam 관리자입니다.</summary>
    public BeamPuzzleController BeamOwner => _beamOwner;
    /// <summary>외부 퍼즐 초기화에서 이 돌을 최초 위치로 복원할 수 있는지 나타냅니다.</summary>
    public bool CanResetToInitialPosition => _canReset;
    private void Awake()
    {
        EnsureInitialState();
        TryCacheSkillService();
    }
    /// <summary>저장 상태를 적용하기 전에 씬 원래 위치와 회전을 한 번 보관합니다.</summary>
    public void EnsureInitialState()
    {
        if (_hasInitialState) return;
        _hasInitialState = true;
        _rigidbody = GetComponent<Rigidbody2D>();
        _stoneCollider = GetComponent<Collider2D>();
        _playerMove = _playerMoveController;
        if (_playerMoveController != null)
        {
            _playerTransform = _playerMoveController.transform;
        }
        if (_rigidbody != null)
        {
            // 일반 돌의 기존 물리 위치 기준 초기화 동작을 보존합니다.
            _initialPosition = _rigidbody.position;
            _initialRotation = _rigidbody.rotation;
            _initialBodyType = _rigidbody.bodyType;
        }
        _obstacleFilter.useLayerMask = true;
        _obstacleFilter.layerMask = _obstacleLayer;
        _obstacleFilter.useTriggers = false;
    }
    /// <summary>다른 Beam에 공유하지 않고 저장과 완료 잠금의 소유권을 연결합니다.</summary>
    public bool TryBindBeamPuzzle(BeamPuzzleController owner)
    {
        if (_beamOwner != null && _beamOwner != owner) return false;
        EnsureInitialState();
        if (_beamOwner == null && owner != null)
        {
            // Beam 원래 배치는 Physics2D 동기화보다 먼저 복원 요청이 와도 보존합니다.
            _initialPosition = transform.position;
            _initialRotation = transform.eulerAngles.z;
        }
        _beamOwner = owner;
        return true;
    }
    /// <summary>저장된 월드 위치와 Z 회전을 이동 잔여 속도 없이 적용합니다.</summary>
    public void RestoreWorldPose(Vector2 position, float rotation)
    {
        EnsureInitialState();
        OnHoldEnded();
        _rigidbody.position = position;
        _rigidbody.rotation = rotation;
        _rigidbody.angularVelocity = 0f;
        transform.SetPositionAndRotation(new Vector3(position.x, position.y, transform.position.z),
            Quaternion.Euler(0f, 0f, rotation));
    }
    /// <summary>Beam 초기화는 CanReset 옵션과 무관하게 원래 씬 배치로 복원합니다.</summary>
    public void ResetBeamState()
    {
        EnsureInitialState();
        RestoreWorldPose(_initialPosition, _initialRotation);
    }
    private void OnDisable() => OnHoldEnded();
    private void FixedUpdate()
    {
        if (!_isHolding || _rigidbody == null || _stoneCollider == null
            || _playerMove == null || _playerTransform == null)
        {
            return;
        }
        Vector2 moveDirection = (Vector2)_playerMove.MoveDirection;
        if (moveDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
        {
            StopStone();
            return;
        }
        moveDirection.Normalize();
        if (!IsPlayerBehindStone(moveDirection) || IsBlocked(moveDirection))
        {
            StopStone();
            return;
        }
        float moveDistance = _pushSpeed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(_rigidbody.position + moveDirection * moveDistance);
    }
    /// <summary>짧게 누르는 상호작용에는 돌 밀기 행동을 실행하지 않습니다.</summary>
    public void Interact() { }
    /// <inheritdoc/>
    public void SetInteractionContext(
        PlayerInteractionController interactionController,
        PlayerMoveController moveController)
    {
        OnHoldEnded();
        _playerMove = moveController;
        _playerTransform = interactionController != null
            ? interactionController.transform
            : moveController != null ? moveController.transform : null;
    }
    /// <summary>필요 스킬을 획득한 경우에만 홀드 밀기 상태를 시작합니다.</summary>
    public void OnHoldStarted()
    {
        if (_isHolding || _requiredSkill == null || !TryCacheSkillService()
            || _requiredSkill.SkillActionState != SkillActionState.SKILL_ACTION_HOLD_MOVE)
        {
            return;
        }
        if ((_beamOwner != null || _lockWhenPuzzleCompleted) && _isPuzzleCompleted)
        {
            return;
        }
        if (!_skillService.IsAcquired(_requiredSkill.SkillId))
        {
            return;
        }
        _isHolding = true;
    }
    /// <summary>홀드 밀기 상태를 끝내고 남은 물리 속도를 제거합니다.</summary>
    public void OnHoldEnded()
    {
        _isHolding = false;
        StopStone();
    }
    /// <summary>허용된 돌을 퍼즐 시작 위치와 회전으로 되돌립니다.</summary>
    public bool TryResetToInitialPosition()
    {
        if (!_canReset || _rigidbody == null
            || ((_beamOwner != null || _lockWhenPuzzleCompleted) && _isPuzzleCompleted))
        {
            return false;
        }
        OnHoldEnded();
        _rigidbody.position = _initialPosition;
        _rigidbody.rotation = _initialRotation;
        return true;
    }
    /// <summary>외부 퍼즐 흐름에서 완료 여부를 전달합니다.</summary>
    public void SetPuzzleCompleted(bool isCompleted)
    {
        _isPuzzleCompleted = isCompleted;
        // Beam 완료 후 이동 입력 외의 물리 접촉으로도 돌이 밀리지 않게 고정합니다.
        if (_beamOwner != null && _rigidbody != null)
            _rigidbody.bodyType = isCompleted ? RigidbodyType2D.Static : _initialBodyType;
        if (isCompleted && (_beamOwner != null || _lockWhenPuzzleCompleted))
        {
            OnHoldEnded();
        }
    }
    private bool IsPlayerBehindStone(Vector2 moveDirection)
    {
        Vector2 playerToStone = _rigidbody.position - (Vector2)_playerTransform.position;
        if (playerToStone.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
        {
            return false;
        }
        return Vector2.Dot(moveDirection, playerToStone.normalized) > 0f;
    }
    private bool IsBlocked(Vector2 moveDirection)
    {
        float checkDistance = _pushSpeed * Time.fixedDeltaTime + _obstacleCheckDistance;
        return _stoneCollider.Cast(
            moveDirection, _obstacleFilter, _obstacleHits, checkDistance) > 0;
    }
    private void StopStone()
    {
        if (_rigidbody == null) return;
        _rigidbody.linearVelocity = Vector2.zero;
    }
    private bool TryCacheSkillService()
    {
        if (_skillService != null) return true;
        if (!GameRoot.TryGetInstance(out GameRoot gameRoot) || gameRoot.Skill == null) return false;
        _skillService = gameRoot.Skill;
        return true;
    }
#if UNITY_EDITOR
    private void OnValidate() => ValidateSetup();

    [ContextMenu("돌 설정 검증")]
    private void ValidateSetup()
    {
        if (string.IsNullOrWhiteSpace(_objectId)) LogValidationIssue("objectId가 비어 있습니다.", true);
        if (string.IsNullOrWhiteSpace(_puzzleId)) LogValidationIssue("퍼즐 ID가 비어 있습니다.", false);
        if (_requiredSkill == null) LogValidationIssue("필요 스킬이 연결되지 않았습니다.", true);
        else if (string.IsNullOrWhiteSpace(_requiredSkill.SkillId)
            || _requiredSkill.SkillActionState != SkillActionState.SKILL_ACTION_HOLD_MOVE)
        {
            LogValidationIssue("필요 스킬은 ID가 있는 홀드 이동 스킬이어야 합니다.", true);
        }
        if (!TryGetComponent<Rigidbody2D>(out _)) LogValidationIssue("Rigidbody2D가 없습니다.", true);
        if (!TryGetComponent<Collider2D>(out _)) LogValidationIssue("Collider2D가 없습니다.", true);
        if (_obstacleLayer.value == 0) LogValidationIssue("막힘 검사 LayerMask가 비어 있습니다.", false);
    }
    private void LogValidationIssue(string message, bool isError)
    {
        string formattedMessage = $"[PushableStone] {name}: {message}";
        if (isError) Debug.LogError(formattedMessage, this);
        else Debug.LogWarning(formattedMessage, this);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _obstacleCheckDistance);
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_initialPosition, _obstacleCheckDistance);
        }
    }
#endif
}
