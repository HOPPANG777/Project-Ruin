using UnityEngine;
using UnityEngine.InputSystem;

//==============================================================================
// class::PlayerMoveController
// 플레이어의 이동을 담당한다.
//==============================================================================
// TODO: IMoveable로 의존성 주입 구현.
//
//==============================================================================
public class PlayerMoveController : MonoBehaviour, IPlayerMotionSource
{
    private const string SPRINT_ACTION_NAME = "Sprint";

    //==============================================================================
    // 변수
    //============================================================================== 
    [Header("Move")]
    [Tooltip("플레이어의 이동 방향")]
    [SerializeField] private Vector3 _moveDirection;
    public Vector3 MoveDirection { get => _moveDirection; }

    [Header("Move")]
    [Tooltip("플레이어의 이동 속도")]
    [SerializeField] private float _moveSpeed;
    public float MoveSpeed { get => _moveSpeed; }

    [Tooltip("달리기 입력 중 기본 이동 속도에 곱할 배율입니다.")]
    [Min(1f)]
    [SerializeField] private float _runSpeedMultiplier = 1.5f;

    /// <summary>현재 달리기 입력을 적용하며 이동 중인지 반환합니다.</summary>
    public bool IsRunning { get; private set; }

    [Header("Move")]
    [Tooltip("플레이어")]
    [SerializeField] private GameObject _player;

    [Header("Move")]
    [Tooltip("인풋")]
    [SerializeField] private InputActionReference _moveAction;

    [Header("Move")]
    [Tooltip("플레이어 이동 입력 허용 여부입니다. 대화 중에는 false로 설정합니다.")]
    [SerializeField] private bool _canMove = true;

    private InputAction _sprintAction;

    /// <summary>현재 플레이어 이동 입력 허용 상태입니다.</summary>
    public bool CanMove => _canMove;

    //==============================================================================
    // 함수
    //==============================================================================
    void Update()
    {
        Move();
    }

    private void OnEnable()
    {
        InputAction moveAction = _moveAction != null ? _moveAction.action : null;
        if (moveAction == null) return;

        _sprintAction = moveAction.actionMap?.FindAction(SPRINT_ACTION_NAME, false);
        moveAction.Enable();
        _sprintAction?.Enable();
    }

    private void OnDisable()
    {
        _sprintAction?.Disable();
        _sprintAction = null;
        if (_moveAction != null && _moveAction.action != null) _moveAction.action.Disable();
        _moveDirection = Vector3.zero;
        IsRunning = false;
    }

    /// <summary>
    /// 플레이어 이동 입력 허용 여부를 설정합니다.
    /// </summary>
    /// <param name="canMove">이동 허용 여부입니다.</param>
    public void SetCanMove(bool canMove)
    {
        _canMove = canMove;

        if (_canMove == false)
        {
            _moveDirection = Vector3.zero;
            IsRunning = false;
        }
    }

    // 이동 로직
    public void Move()
    {
        if (_canMove == false || _moveAction == null || _moveAction.action == null
            || _player == null)
        {
            _moveDirection = Vector3.zero;
            IsRunning = false;
            return;
        }

        // 입력
        Vector2 vec = _moveAction.action.ReadValue<Vector2>();
        _moveDirection = new Vector3(vec.x, vec.y, 0.0f);

        IsRunning = _moveDirection.sqrMagnitude > 0.0001f && 
                    _sprintAction != null && 
                    _sprintAction.IsPressed();

        // 방향 벡터
        float speed = _moveSpeed * (IsRunning ? Mathf.Max(1f, _runSpeedMultiplier) : 1f);
        Vector3 nextVector = _moveDirection.normalized * (speed * Time.deltaTime);

        // 이동
        _player.transform.position = _player.transform.position + nextVector;
    }
}
