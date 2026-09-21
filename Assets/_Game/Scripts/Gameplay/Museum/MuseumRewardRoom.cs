//=============================================================================
// File:        MuseumRewardRoom.cs
// Description: 최종 방의 문·보검 표시, 방 내부 상호작용과 챕터 3 벽화를 연결한다.
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using UnityEngine;

/// <summary>보검 Pickup 오브젝트에 배치하는 씬 연결 컴포넌트입니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MuseumRewardRoom : MonoBehaviour, IInteractable, IInteractionContextReceiver
{
    ///////////////////////////////////////////////////////////////////////
    // Serialized Fields
    ///////////////////////////////////////////////////////////////////////
    [Header("CSV Identity")]
    [Tooltip("museum_relic_milestones.csv의 ENABLE_ROOM_PICKUP 행 ID입니다.")]
    [SerializeField] private string _rewardMilestoneId;
    [Header("Room References")]
    [Tooltip("실제 방 내부를 감싸는 별도 BoxCollider2D Trigger입니다.")]
    [SerializeField] private BoxCollider2D _roomBounds;
    [Tooltip("잠긴 문 통과를 막는 별도의 비 Trigger Collider2D입니다.")]
    [SerializeField] private Collider2D _doorBlocker;
    [Tooltip("문이 열리면 숨길 닫힌 문 Renderer입니다.")]
    [SerializeField] private Renderer _closedDoorRenderer;
    [Tooltip("수령 완료 시 숨길 보검 Renderer입니다.")]
    [SerializeField] private Renderer _rewardRenderer;
    [Tooltip("방 내부에 배치한 chapter=3 정의의 MuseumMural입니다.")]
    [SerializeField] private MuseumMural _chapter3Mural;
    private BoxCollider2D _pickupCollider;
    private IMuseumRewardRoomService _room;
    private IMuseumService _museum;
    private PlayerInteractionController _player;
    // 서비스가 늦게 준비돼도 저장된 벽화 표시를 한 번만 적용합니다.
    private bool _hasRestoredMural;

    /// <summary>보상 Item이 아닌 CSV 마일스톤의 연결 ID입니다.</summary>
    public string RewardMilestoneId => _rewardMilestoneId;
    /// <summary>기존 개봉·저장 로직을 유지하는 챕터 3 벽화입니다.</summary>
    public MuseumMural Chapter3Mural => _chapter3Mural;

    private void Awake() => _pickupCollider = GetComponent<BoxCollider2D>();
    private void OnEnable()
    {
        _hasRestoredMural = false;
        Subscribe();
        Refresh();
    }
    private void OnDisable()
    {
        if (_room != null) _room.StateChanged -= Refresh;
        _player = null;
    }
    private void Start()
    {
        if (_room != null) return;
        GameRoot root = GameRoot.Instance;
        Bind(root != null ? root.MuseumRewardRoom : null, root != null ? root.Museum : null);
    }

    //=============================================================================
    // Bind
    // Description: GameRoot 또는 테스트가 주입한 서비스와 저장 표시를 연결합니다.
    // Parameters: IMuseumRewardRoomService room, IMuseumService museum
    // Return: void
    //=============================================================================
    /// <summary>씬 서비스 참조를 교체하며 기존 이벤트 구독을 해제합니다.</summary>
    public void Bind(IMuseumRewardRoomService room, IMuseumService museum)
    {
        if (_room != null) _room.StateChanged -= Refresh;
        _room = room;
        _museum = museum;
        if (_pickupCollider == null) _pickupCollider = GetComponent<BoxCollider2D>();
        _chapter3Mural?.Bind(museum);
        _hasRestoredMural = HasChapter3Definition();
        Subscribe();
        Refresh();
    }

    //=============================================================================
    // SetInteractionContext / Interact / TryInteract
    // Description: 실제 입력을 보낸 플레이어의 방 내부 위치를 확정 직전에 검사합니다.
    // Parameters: PlayerInteractionController interactionController, PlayerMoveController moveController / out failure
    // Return: void / bool
    //=============================================================================
    /// <inheritdoc/>
    public void SetInteractionContext(PlayerInteractionController interactionController,
        PlayerMoveController moveController) => _player = interactionController;

    /// <inheritdoc/>
    public void Interact()
    {
        if (!TryInteract(out var failure))
            Debug.LogWarning($"[MuseumRewardRoom] 보상 수령 실패: {failure}", this);
    }

    /// <summary>방 밖 호출, 미설정 참조와 중복 수령을 차단하고 실패 이유를 반환합니다.</summary>
    public bool TryInteract(out MuseumRelicDisplayFailure failure)
    {
        PlayerInteractionController player = _player;
        _player = null;
        failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVALID_ROOM_SETUP;
        if (!isActiveAndEnabled || !TryValidateLayout(out _) || !HasChapter3Definition()) return false;
        failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INTERACTION_BLOCKED;
        if (player != null && !player.CanInteract) return false;
        failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_PLAYER_OUTSIDE_ROOM;
        if (player == null || !player.isActiveAndEnabled
            || !ContainsPoint(player.transform.position)) return false;
        failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NOT_INITIALIZED;
        return _room != null && _room.TryClaimReward(_rewardMilestoneId, out failure);
    }

    //=============================================================================
    // TryValidateLayout
    // Description: 물리 Scene을 실행하지 않고 씬 참조와 방 내부 배치를 검사합니다.
    // Parameters: out string message
    // Return: bool
    //=============================================================================
    /// <summary>누락·잘못된 Collider 설정·방 밖 벽화 및 보검 배치를 검사합니다.</summary>
    public bool TryValidateLayout(out string message)
    {
        message = "방 참조 또는 마일스톤 ID가 누락됐습니다.";
        if (string.IsNullOrWhiteSpace(_rewardMilestoneId) || _roomBounds == null
            || _doorBlocker == null || _closedDoorRenderer == null
            || _rewardRenderer == null || _chapter3Mural == null) return false;
        message = "보검·문·벽화와 Renderer 오브젝트는 활성 상태여야 합니다.";
        if (!enabled || !gameObject.activeInHierarchy || !_doorBlocker.gameObject.activeInHierarchy
            || !_roomBounds.gameObject.activeInHierarchy || !_chapter3Mural.enabled
            || !_chapter3Mural.gameObject.activeInHierarchy
            || !_closedDoorRenderer.gameObject.activeInHierarchy
            || !_rewardRenderer.gameObject.activeInHierarchy) return false;
        // Editor 검사에서는 Awake가 실행되지 않으므로 최초 검사 때만 캐시합니다.
        if (_pickupCollider == null) _pickupCollider = GetComponent<BoxCollider2D>();
        message = "방/보검은 별도 Trigger이고 문은 별도 비 Trigger여야 합니다.";
        if (_pickupCollider == null || !_pickupCollider.isTrigger || !_roomBounds.isTrigger
            || _doorBlocker.isTrigger || _roomBounds == _pickupCollider
            || _doorBlocker == _pickupCollider || _doorBlocker == _roomBounds
            || _closedDoorRenderer == _rewardRenderer
            || _chapter3Mural.gameObject == gameObject
            || _chapter3Mural.gameObject == _doorBlocker.gameObject) return false;
        message = "보검과 챕터 3 벽화는 같은 씬의 방 내부에 있어야 합니다.";
        if (_roomBounds.gameObject.scene != gameObject.scene
            || _chapter3Mural.gameObject.scene != gameObject.scene
            || _doorBlocker.gameObject.scene != gameObject.scene
            || _closedDoorRenderer.gameObject.scene != gameObject.scene
            || _rewardRenderer.gameObject.scene != gameObject.scene
            || !ContainsPoint(transform.position)
            || !ContainsPoint(_chapter3Mural.transform.position)) return false;
        message = string.Empty;
        return true;
    }

    private void Subscribe()
    {
        if (_room == null || !isActiveAndEnabled) return;
        _room.StateChanged -= Refresh;
        _room.StateChanged += Refresh;
    }

    // 전시·보상 알림은 진행 중인 벽화 빛 효과를 중단하지 않습니다.
    private void Refresh()
    {
        bool hasMuralDefinition = HasChapter3Definition();
        bool open = _room != null && _room.IsOpen
            && TryValidateLayout(out _) && hasMuralDefinition;
        bool reward = open && _room.TryGetRewardStatus(_rewardMilestoneId, out var status)
            && !status.IsClaimed;
        if (_doorBlocker != null) _doorBlocker.enabled = !open;
        if (_closedDoorRenderer != null) _closedDoorRenderer.enabled = !open;
        if (_rewardRenderer != null) _rewardRenderer.enabled = reward;
        if (_pickupCollider != null) _pickupCollider.enabled = reward;
        // 로드 전에 Bind되어도 서비스 준비 후 한 번만 최종 표시를 반영합니다.
        if (!_hasRestoredMural && hasMuralDefinition)
        {
            _chapter3Mural.RefreshVisualState();
            _hasRestoredMural = true;
        }
    }

    private bool HasChapter3Definition() => _museum != null && _chapter3Mural != null
        && _museum.TryGetMural(_chapter3Mural.MuralId, out var definition) && definition.Chapter == 3;

    // 로컬 박스 기준으로 검사하여 회전·배율과 Preview Scene에서도 같은 결과를 냅니다.
    private bool ContainsPoint(Vector2 point)
    {
        Vector2 local = (Vector2)_roomBounds.transform.InverseTransformPoint(point) - _roomBounds.offset;
        Vector2 half = _roomBounds.size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y;
    }
}
