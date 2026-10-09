//=============================================================================
// File:        BreakableWall.cs
//
// Description: 벽 부수기 스킬과 준비 상태를 확인해 길을 여는 상호작용을 처리한다.
//
// History:     08/01/2026 + Created -- Codex
//
//=============================================================================
using UnityEngine;
//=============================================================================
// Synopsis:   BreakableWall;
//                - 누르기 입력 후 이동하지 않아야 파괴되는 환경 상호작용 대상
//                - 씬 전환 시 파괴 여부를 저장하고 복원
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BreakableWall : MonoBehaviour, IInteractable, ISceneStateContributor,
    IInteractionContextReceiver, ISceneStateOwnedContributor
{
    private const string BROKEN_STATE_JSON = "true";
    [Header("Identity")]
    [Tooltip("씬 상태 저장에서 벽을 구분할 고유 ID입니다.")]
    [SerializeField] private string _objectId;
    [Tooltip("씬을 다시 방문할 때 파괴 상태를 복원할지 결정합니다.")]
    [SerializeField] private bool _saveState = true;
    [Header("Skill")]
    [Tooltip("벽을 부수기 위해 플레이어가 획득해야 하는 누르기 환경 스킬입니다.")]
    [SerializeField] private SkillDataSO _requiredSkill;
    [Header("Preparation")]
    [Tooltip("준비 중 이동 여부를 확인할 플레이어 이동 컴포넌트입니다.")]
    [SerializeField] private PlayerMoveController _playerMoveController;
    [Tooltip("상호작용을 시작한 뒤 벽이 부서질 때까지 걸리는 시간입니다.")]
    [Min(0f), SerializeField] private float _prepareDuration = 0.6f;
    [Tooltip("준비를 취소하는 이동 입력의 제곱 크기 기준값입니다.")]
    [Min(0f), SerializeField] private float _moveCancelSqrThreshold = 0.01f;
    [Header("Result")]
    [Tooltip("파괴 완료 시 숨길 벽의 스프라이트 렌더러입니다.")]
    [SerializeField] private SpriteRenderer _wallRenderer;
    [Tooltip("별도 주변 상호작용 Trigger입니다. 비어 있으면 기존 단일 충돌체 동작을 유지합니다.")]
    [SerializeField] private Collider2D _interactionTrigger;
    private Collider2D _wallCollider;
    private IMoveable _playerMove;
    private ISkillService _skillService;
    private float _prepareElapsed;
    private bool _isPreparing;
    private bool _isBroken;
    // Beam에 등록된 벽은 Beam 상태만 저장/복원하여 독립 상태와 충돌하지 않습니다.
    private BeamPuzzleController _beamOwner;
    private bool _hasInitialState;
    private bool _initialBroken;
    private bool _isPuzzleCompleted;
    /// <summary>현재 파괴 상태입니다.</summary>
    public bool IsBroken => _isBroken;
    /// <summary>광선을 차단하는 기존 벽 몸체입니다.</summary>
    public Collider2D BodyCollider => _wallCollider != null ? _wallCollider : GetComponent<Collider2D>();
    /// <summary>벽 상태를 소유한 Beam 관리자입니다.</summary>
    public BeamPuzzleController BeamOwner => _beamOwner;
    /// <summary>Beam에 등록된 벽은 독립 씬 상태 저장에서 제외됩니다.</summary>
    public bool IsSceneStateOwned => _beamOwner != null;
    /// <summary>씬 상태 저장에서 이 벽을 구분하는 고유 ID입니다.</summary>
    public string ObjectId => _objectId;
    private void Awake()
    {
        EnsureInitialState();
        TryCacheSkillService();
    }
    /// <summary>씬 배치 초기 상태를 복원 전에 한 번만 보관합니다.</summary>
    public void EnsureInitialState()
    {
        if (_hasInitialState) return;
        _hasInitialState = true;
        _wallCollider = GetComponent<Collider2D>();
        _playerMove = _playerMoveController;

        if (_wallRenderer == null)
        {
            TryGetComponent(out _wallRenderer);
        }
        _initialBroken = _isBroken;
    }
    /// <summary>하나의 Beam 관리자만 벽 상태를 소유하도록 연결합니다.</summary>
    public bool TryBindBeamPuzzle(BeamPuzzleController owner)
    {
        if (_beamOwner != null && _beamOwner != owner) return false;
        EnsureInitialState();
        _beamOwner = owner;
        return true;
    }
    /// <inheritdoc/>
    public void SetInteractionContext(PlayerInteractionController interactionController, PlayerMoveController moveController)
    {
        CancelPreparation();
        _playerMove = moveController;
    }
    /// <summary>Beam 저장 상태를 독립 저장 설정과 무관하게 복원합니다.</summary>
    public void RestoreBeamState(bool broken) { EnsureInitialState(); ApplyWallState(broken); }
    /// <summary>씬 배치 때의 원래 파괴 상태로 돌아갑니다.</summary>
    public void ResetBeamState() { EnsureInitialState(); ApplyWallState(_initialBroken); }
    /// <summary>완료 시 준비 중인 파괴도 취소하고 추가 조작을 막습니다.</summary>
    public void SetPuzzleCompleted(bool completed)
    {
        _isPuzzleCompleted = completed;
        if (completed) CancelPreparation();
    }
    private void OnDisable() => CancelPreparation();
    private void Update()
    {
        if (_isBroken || !_isPreparing)
        {
            return;
        }
        if (_playerMove.MoveDirection.sqrMagnitude > _moveCancelSqrThreshold)
        {
            CancelPreparation();
            return;
        }
        _prepareElapsed += Time.deltaTime;
        if (_prepareElapsed >= _prepareDuration)
        {
            TryBreakOnce();
        }
    }
    /// <summary>필요 스킬을 획득한 경우 벽 부수기 준비를 시작합니다.</summary>
    public void Interact()
    {
        if (_isPuzzleCompleted || _isBroken || _isPreparing || !CanStartPreparation())
        {
            return;
        }
        _prepareElapsed = 0f;
        _isPreparing = true;
    }
    /// <summary>현재 파괴 여부를 씬 상태용 JSON 불리언으로 반환합니다.</summary>
    public string CaptureState()
    {
        if (_beamOwner != null || !_saveState || !_isBroken)
        {
            return string.Empty;
        }
        return BROKEN_STATE_JSON;
    }
    /// <summary>저장된 파괴 여부를 연출 없이 즉시 적용합니다.</summary>
    public void ApplyState(string stateJson)
    {
        if (_beamOwner != null || !_saveState || string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }
        if (!bool.TryParse(stateJson, out bool isBroken))
        {
            Debug.LogWarning($"[BreakableWall] 저장 상태를 읽을 수 없습니다. object={name}", this);
            return;
        }
        EnsureInitialState();
        ApplyWallState(isBroken);
    }
    private bool CanStartPreparation()
    {
        if (_requiredSkill == null || _playerMove == null
            || string.IsNullOrWhiteSpace(_requiredSkill.SkillId))
        {
            return false;
        }
        if (_requiredSkill.SkillActionState != SkillActionState.SKILL_ACTION_PRESS
            || !TryCacheSkillService())
        {
            return false;
        }
        return _skillService.IsAcquired(_requiredSkill.SkillId);
    }
    private bool TryBreakOnce()
    {
        if (_isPuzzleCompleted || _isBroken || !_isPreparing)
        {
            return false;
        }
        ApplyWallState(true);
        return true;
    }
    private void CancelPreparation()
    {
        _isPreparing = false;
        _prepareElapsed = 0f;
    }
    private void ApplyWallState(bool isBroken)
    {
        _isBroken = isBroken;
        CancelPreparation();
        if (_wallCollider != null)
        {
            _wallCollider.enabled = !isBroken;
        }
        if (_wallRenderer != null)
        {
            _wallRenderer.enabled = !isBroken;
        }
        if (_interactionTrigger != null) _interactionTrigger.enabled = !isBroken;
    }
    private bool TryCacheSkillService()
    {
        if (_skillService != null)
        {
            return true;
        }
        if (!GameRoot.TryGetInstance(out GameRoot gameRoot) || gameRoot.Skill == null)
        {
            return false;
        }
        _skillService = gameRoot.Skill;
        return true;
    }
#if UNITY_EDITOR
    private void OnValidate() => ValidateSetup();

    [ContextMenu("벽 설정 검증")]
    private void ValidateSetup()
    {
        // 부품 생성 도구의 Preview 씬은 설정을 연결하는 중이므로 완료된 에셋에서 검증합니다.
        if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
        if (string.IsNullOrWhiteSpace(_objectId)) LogValidationIssue("objectId가 비어 있습니다.", true);
        if (_requiredSkill == null) LogValidationIssue("필요 스킬이 연결되지 않았습니다.", true);
        else if (string.IsNullOrWhiteSpace(_requiredSkill.SkillId)
            || _requiredSkill.SkillActionState != SkillActionState.SKILL_ACTION_PRESS)
        {
            LogValidationIssue("필요 스킬은 ID가 있는 누르기 스킬이어야 합니다.", true);
        }
        // 별도 Trigger는 기존 플레이어 입력 경로에서 이동 컨텍스트를 받습니다.
        if (_playerMoveController == null && _interactionTrigger == null)
            LogValidationIssue("PlayerMoveController 또는 상호작용 Trigger가 필요합니다.", true);
        if (!TryGetComponent<Collider2D>(out _)) LogValidationIssue("Collider2D가 없습니다.", true);
        if (_prepareDuration < 0f) LogValidationIssue("준비 시간은 0 이상이어야 합니다.", true);
        if (_moveCancelSqrThreshold < 0f) LogValidationIssue("이동 취소 기준값은 0 이상이어야 합니다.", true);
        if (_wallRenderer == null && !TryGetComponent<SpriteRenderer>(out _))
        {
            LogValidationIssue("파괴 시 숨길 SpriteRenderer가 없습니다.", false);
        }
    }
    private void LogValidationIssue(string message, bool isError)
    {
        string formattedMessage = $"[BreakableWall] {name}: {message}";
        if (isError) Debug.LogError(formattedMessage, this);
        else Debug.LogWarning(formattedMessage, this);
    }
#endif
}
