//=============================================================================
// File:        PlayerInteractionController.cs
//
// Description: 주변 상호작용 대상을 탐지하고 누르기·홀드 입력을 전달한다.
//
// History:     07/31/2026 + 홀드 입력 계약과 저할당 탐지 흐름 추가 -- Codex
//              08/15/2026 + 복수 상호작용 Layer와 자식 Trigger 탐지 추가 -- Codex
//
//=============================================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
//=============================================================================
// Synopsis:   PlayerInteractionController;
//                - 가장 가까운 상호작용 대상 탐지와 하이라이트 관리
//                - 대상별 행동을 모른 채 누르기·홀드 시작·해제만 전달
//=============================================================================
[DisallowMultipleComponent]
public sealed class PlayerInteractionController : MonoBehaviour
{
    // 기존 탐지 용량을 유지해 씬의 동작 범위를 바꾸지 않습니다.
    private const int DETECTION_BUFFER_SIZE = 10;
    [Header("Interaction")]
    [Tooltip("상호작용 탐지 기준 위치이며, 비어 있으면 플레이어 위치를 사용합니다.")]
    [SerializeField] private Transform _origin;
    [Tooltip("상호작용 대상으로 탐지할 하나 이상의 레이어입니다.")]
    [FormerlySerializedAs("_layer")]
    [SerializeField] private LayerMask _interactionLayers;
    [Tooltip("상호작용 대상을 탐지할 반경입니다.")]
    [SerializeField] private float _interactRange = 1.0f;
    [Tooltip("누르기와 홀드에 함께 사용하는 상호작용 입력 액션입니다.")]
    [SerializeField] private InputActionReference _interactAction;
    [Tooltip("대화나 UI 중 상호작용 탐지와 입력을 허용할지 결정합니다.")]
    [SerializeField] private bool _canInteract = true;
    [Tooltip("상호작용 대상에 입력 잠금 컨텍스트로 전달할 같은 플레이어의 이동 컨트롤러입니다.")]
    [SerializeField] private PlayerMoveController _playerMoveController;
    /// <summary>현재 월드 상호작용 입력 허용 상태입니다.</summary>
    public bool CanInteract => _canInteract;
    private readonly Collider2D[] _detectResults = new Collider2D[DETECTION_BUFFER_SIZE];
    private ContactFilter2D _contactFilter;
    private Collider2D _currentCollider;
    private IInteractable _currentTarget;
    private IHighlightable _currentHighlightTarget;
    private IHoldInteractable _currentHoldTarget;
    private IHoldInteractable _activeHoldTarget;
    private void Awake()
    {
        if (_playerMoveController == null)
        {
            TryGetComponent(out _playerMoveController);
        }
        _contactFilter.useLayerMask = true;
        _contactFilter.layerMask = _interactionLayers;
        _contactFilter.useTriggers = true;
    }
    private void OnEnable()
    {
        if (_interactAction != null && _interactAction.action != null)
        {
            _interactAction.action.Enable();
        }
    }
    private void OnDisable()
    {
        if (_interactAction != null && _interactAction.action != null)
        {
            _interactAction.action.Disable();
        }
        ClearTarget();
    }
    private void Update()
    {
        if (!_canInteract)
        {
            ClearTarget();
            return;
        }
        FindNearestInteractable();
        HandleInteractionInput();
    }

    //=============================================================================
    // PlayerInteractionController.SetCanInteract
    //=============================================================================
    /// <summary>상호작용 탐지와 입력을 허용하거나 즉시 중지합니다.</summary>
    public void SetCanInteract(bool canInteract)
    {
        _canInteract = canInteract;
        if (!canInteract)
        {
            ClearTarget();
        }
    }

    // 고정 버퍼에서 가장 가까운 계약만 선택해 매 프레임 컬렉션 할당을 피합니다.
    private void FindNearestInteractable()
    {
        Vector2 originPosition = _origin != null ? _origin.position : transform.position;
        int hitCount = Physics2D.OverlapCircle(
            originPosition, _interactRange, _contactFilter, _detectResults);
        Collider2D nearestCollider = null;
        IInteractable nearestTarget = null;
        IHighlightable nearestHighlight = null;
        float nearestSqrDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = _detectResults[i];
            if (hitCollider == null)
            {
                continue;
            }
            IInteractable interactable;
            if (hitCollider == _currentCollider)
            {
                interactable = _currentTarget;
            }
            else if (!TryGetInteractable(hitCollider, out interactable))
            {
                continue;
            }
            float sqrDistance = ((Vector2)hitCollider.transform.position - originPosition).sqrMagnitude;
            if (interactable == null || sqrDistance >= nearestSqrDistance)
            {
                continue;
            }
            nearestCollider = hitCollider;
            nearestTarget = interactable;
            nearestSqrDistance = sqrDistance;
            if (hitCollider == _currentCollider)
            {
                nearestHighlight = _currentHighlightTarget;
            }
            else
            {
                TryGetHighlightable(hitCollider, out nearestHighlight);
            }
        }
        if (nearestTarget == null)
        {
            ClearTarget();
            return;
        }
        if (nearestTarget == _currentTarget)
        {
            _currentCollider = nearestCollider;
            return;
        }
        ChangeTarget(nearestCollider, nearestTarget, nearestHighlight);
    }

    // 자식 Trigger는 자신 또는 연결된 Rigidbody 오브젝트의 계약을 사용합니다.
    private static bool TryGetInteractable(Collider2D targetCollider,
        out IInteractable interactable)
    {
        if (targetCollider.TryGetComponent(out interactable))
        {
            return true;
        }
        Rigidbody2D attachedBody = targetCollider.attachedRigidbody;
        return attachedBody != null && attachedBody.TryGetComponent(out interactable);
    }

    private static bool TryGetHighlightable(Collider2D targetCollider,
        out IHighlightable highlightable)
    {
        if (targetCollider.TryGetComponent(out highlightable))
        {
            return true;
        }
        Rigidbody2D attachedBody = targetCollider.attachedRigidbody;
        return attachedBody != null && attachedBody.TryGetComponent(out highlightable);
    }
    // 대상 변경은 진행 중인 홀드를 먼저 끝내 중복 상태를 남기지 않습니다.
    private void ChangeTarget(Collider2D newCollider, IInteractable newTarget,
        IHighlightable newHighlightTarget)
    {
        EndHold();
        _currentHighlightTarget?.SetHighlight(false);
        _currentCollider = newCollider;
        _currentTarget = newTarget;
        _currentHighlightTarget = newHighlightTarget;
        _currentHoldTarget = newTarget as IHoldInteractable;
        _currentHighlightTarget?.SetHighlight(true);
    }
    private void ClearTarget()
    {
        EndHold();
        _currentHighlightTarget?.SetHighlight(false);
        _currentCollider = null;
        _currentTarget = null;
        _currentHighlightTarget = null;
        _currentHoldTarget = null;
    }

    private void HandleInteractionInput()
    {
        if (_interactAction == null || _interactAction.action == null)
        {
            EndHold();
            return;
        }
        InputAction action = _interactAction.action;
        if (action.WasPressedThisFrame())
        {
            if (_currentTarget is IInteractionContextReceiver contextReceiver)
            {
                contextReceiver.SetInteractionContext(this, _playerMoveController);
            }
            _currentTarget?.Interact();
        }
        if (action.WasReleasedThisFrame())
        {
            EndHold();
            return;
        }
        if (!action.IsPressed() || _activeHoldTarget != null || _currentHoldTarget == null)
        {
            return;
        }
        // 입력을 누른 채 새 대상을 선택한 경우에도 해당 플레이어를 전달합니다.
        if (_currentHoldTarget is IInteractionContextReceiver holdContextReceiver)
        {
            holdContextReceiver.SetInteractionContext(this, _playerMoveController);
        }
        _activeHoldTarget = _currentHoldTarget;
        _activeHoldTarget.OnHoldStarted();
    }

    private void EndHold()
    {
        if (_activeHoldTarget == null)
        {
            return;
        }
        IHoldInteractable holdTarget = _activeHoldTarget;
        _activeHoldTarget = null;
        if (holdTarget is Object unityObject && unityObject == null)
        {
            return;
        }
        holdTarget.OnHoldEnded();
    }
}
