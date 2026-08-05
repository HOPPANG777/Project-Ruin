//=============================================================================
// File:        BeamPuzzleController.cs
//
// Description: 빛줄기 퍼즐의 데이터 검증, 상태 전이와 외부 상호작용을 담당한다.
//
// History:     08/04/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using UnityEngine;
using UnityEngine.Events;

//=============================================================================
//
// Synopsis:   BeamPuzzleState;
//                - 역행하지 않는 빛줄기 퍼즐 생명주기
//=============================================================================
public enum BeamPuzzleState
{
    BEAM_PUZZLE_WAITING = 0,
    BEAM_PUZZLE_IN_PROGRESS,
    BEAM_PUZZLE_COMPLETED
}

//=============================================================================
//
// Synopsis:   BeamPuzzleController;
//                - 월드 구조물 상호작용과 팝업 퍼즐 상태의 단일 소유자
//                - 기둥 데이터 검증 실패 시 퍼즐을 열지 않는 진입점
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class BeamPuzzleController : BasePuzzleManager, IInteractable, ISceneStateContributor
{
    private const string SOLVED_STATE_JSON = "true";

    [Header("Identity")]
    [Tooltip("씬 상태 저장에서 이 퍼즐 구조물을 구분할 고유 ID입니다.")]
    [SerializeField] private string _objectId;

    [Header("Lifecycle")]
    [Tooltip("빛줄기 퍼즐을 표시하고 닫는 공용 퍼즐 팝업입니다.")]
    [SerializeField] private UiPuzzlePopup _puzzlePopup;
    [Tooltip("퍼즐이 열린 동안 월드 상호작용을 막을 플레이어 컨트롤러입니다.")]
    [SerializeField] private PlayerInteractionController _playerInteractionController;
    [Tooltip("퍼즐이 열린 동안 이동을 막을 플레이어 컨트롤러입니다.")]
    [SerializeField] private PlayerMoveController _playerMoveController;

    [Header("Puzzle Data")]
    [Tooltip("같은 월드 또는 World Space Canvas 좌표계에 배치된 모든 기둥입니다.")]
    [SerializeField] private BeamPillarView[] _pillars;
    [Tooltip("빛이 충돌할 기둥 Collider2D 레이어입니다.")]
    [SerializeField] private LayerMask _pillarLayerMask;
    [Tooltip("빛을 차단할 장애물 Collider2D 레이어입니다.")]
    [SerializeField] private LayerMask _obstacleLayerMask;
    [Tooltip("충돌 대상이 없을 때 빛이 도달하는 최대 월드 거리입니다.")]
    [Min(0f), SerializeField] private float _maxBeamDistance = 20f;

    [Header("Beam View")]
    [Tooltip("초기화 때 필요한 개수만 준비할 LineRenderer 원본입니다.")]
    [SerializeField] private LineRenderer _beamPrefab;
    [Tooltip("준비된 빛 View를 보관할 부모 Transform입니다.")]
    [SerializeField] private Transform _beamParent;

    [Header("Structure State")]
    [Tooltip("퍼즐을 풀기 전 표시할 구조물 자식 오브젝트입니다.")]
    [SerializeField] private GameObject _unsolvedStructure;
    [Tooltip("퍼즐을 푼 뒤 표시할 구조물 자식 오브젝트입니다.")]
    [SerializeField] private GameObject _solvedStructure;
    [Tooltip("해결 후 문 개방, 다음 퍼즐 또는 보상을 한 번 실행합니다.")]
    [SerializeField] private UnityEvent _onSolved;

    private BeamPuzzleState _state = BeamPuzzleState.BEAM_PUZZLE_WAITING;
    private BeamPropagation _propagation;
    private LineRenderer[] _beamViews;
    private Collider2D _interactionCollider;
    private bool _isSetupValid;
    private bool _isGoalConnected;
    private bool _ownsPlayerInputLock;

    /// <summary>CSV 게임 데이터 없이 직렬화된 씬 데이터를 사용합니다.</summary>
    public override bool RequiresGameData => false;
    /// <summary>현재 빛줄기 퍼즐 생명주기 상태입니다.</summary>
    public BeamPuzzleState State => _state;
    /// <summary>씬 상태 저장에서 사용하는 고유 구조물 ID입니다.</summary>
    public string ObjectId => _objectId;

    private void Awake()
    {
        TryGetComponent(out _interactionCollider);
        _isSetupValid = TryValidateSetup();
        if (_puzzlePopup != null)
        {
            _puzzlePopup.OnPopupClosedEvent += HandlePopupClosed;
        }
        if (_unsolvedStructure != null && _solvedStructure != null
            && _interactionCollider != null)
        {
            ApplyStructureState(false);
        }
    }

    private void OnDestroy()
    {
        if (_puzzlePopup != null)
        {
            _puzzlePopup.OnPopupClosedEvent -= HandlePopupClosed;
        }
        ReleasePlayerInput();
    }

    private void OnDisable() => ReleasePlayerInput();

    //=============================================================================
    // BeamPuzzleController.Interact
    //=============================================================================
    // Description: 미해결 구조물 상호작용을 팝업 열기로 전달한다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>검증된 미해결 빛줄기 퍼즐 팝업을 엽니다.</summary>
    public void Interact()
    {
        if (!_isSetupValid || _state != BeamPuzzleState.BEAM_PUZZLE_WAITING)
        {
            return;
        }
        _puzzlePopup.OpenPuzzlePopup();
    }

    //=============================================================================
    // BeamPuzzleController.InitializePuzzle
    //=============================================================================
    // Description: 모든 기둥을 최초 상태로 복원하고 진행 상태로 전환한다.
    //
    // Parameters:  None.
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>직렬화 데이터를 다시 검증하고 새 퍼즐 시도를 시작합니다.</summary>
    public override void InitializePuzzle()
    {
        if (_state == BeamPuzzleState.BEAM_PUZZLE_COMPLETED || !TryValidateSetup())
        {
            _puzzlePopup.ClosePopup();
            return;
        }
        _isSetupValid = true;
        IsClear = false;
        _state = BeamPuzzleState.BEAM_PUZZLE_IN_PROGRESS;
        for (int i = 0; i < _pillars.Length; i++)
        {
            _pillars[i].Initialize(this, i);
        }
        if (_propagation == null)
        {
            _propagation = new BeamPropagation(
                _pillars, _pillarLayerMask, _obstacleLayerMask, _maxBeamDistance);
        }
        if (!PrepareBeamViews())
        {
            _isSetupValid = false;
            _puzzlePopup.ClosePopup();
            return;
        }
        AcquirePlayerInput();
        RecalculatePath();
    }

    /// <summary>현재 광경로의 끝 기둥 연결 여부를 검사합니다.</summary>
    public override void CheckClearCondition()
    {
        if (_state != BeamPuzzleState.BEAM_PUZZLE_IN_PROGRESS
            || IsClear || !_isGoalConnected)
        {
            return;
        }
        IsClear = true;
        _state = BeamPuzzleState.BEAM_PUZZLE_COMPLETED;
        OnPuzzleClear();
    }

    /// <summary>진행 중인 회전 가능 기둥을 한 단계 시계방향으로 회전합니다.</summary>
    public bool TryRotatePillar(int pillarIndex)
    {
        if (_state != BeamPuzzleState.BEAM_PUZZLE_IN_PROGRESS
            || pillarIndex < 0 || pillarIndex >= _pillars.Length)
        {
            return false;
        }
        if (!_pillars[pillarIndex].TryRotateClockwise())
        {
            return false;
        }
        RecalculatePath();
        return true;
    }

    /// <summary>최종 해결 상태만 씬 저장 문자열로 반환합니다.</summary>
    public string CaptureState()
    {
        return _state == BeamPuzzleState.BEAM_PUZZLE_COMPLETED
            ? SOLVED_STATE_JSON : string.Empty;
    }

    /// <summary>저장된 해결 상태를 보상과 클리어 이벤트 없이 적용합니다.</summary>
    public void ApplyState(string stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }
        if (!bool.TryParse(stateJson, out bool isSolved))
        {
            Debug.LogWarning(
                $"[BeamPuzzleController] 저장 상태를 읽을 수 없습니다. object={name}", this);
            return;
        }
        if (!isSolved || _state == BeamPuzzleState.BEAM_PUZZLE_COMPLETED)
        {
            return;
        }
        IsClear = true;
        _state = BeamPuzzleState.BEAM_PUZZLE_COMPLETED;
        ApplyStructureState(true);
        ReleasePlayerInput();
        HideBeamViews();
        if (_puzzlePopup != null && _puzzlePopup.IsOpen)
        {
            _puzzlePopup.ClosePopup();
        }
    }

    /// <summary>해결 처리를 외부 팝업과 결과 이벤트에 전달합니다.</summary>
    protected override void OnPuzzleClear()
    {
        ApplyStructureState(true);
        ReleasePlayerInput();
        HideBeamViews();
        _puzzlePopup.ClosePopup();
        _onSolved?.Invoke();
        TriggerClearEvent();
    }

    private bool RecalculatePath()
    {
        Physics2D.SyncTransforms();
        if (_propagation == null || !_propagation.TryRecalculate(out _isGoalConnected))
        {
            _isSetupValid = false;
            LogInvalid("광선이 자기 Collider와 먼저 충돌했거나 경로 버퍼가 부족합니다.");
            _puzzlePopup.ClosePopup();
            return false;
        }
        for (int i = 0; i < _pillars.Length; i++)
        {
            _pillars[i].SetConnected(_propagation.IsConnected(i));
        }
        RefreshBeamViews();
        CheckClearCondition();
        return true;
    }

    private bool PrepareBeamViews()
    {
        int requiredCount = _propagation.SegmentCapacity;
        if (_beamViews != null)
        {
            return _beamViews.Length >= requiredCount;
        }
        _beamViews = new LineRenderer[requiredCount];
        for (int i = 0; i < requiredCount; i++)
        {
            LineRenderer beamView = Instantiate(_beamPrefab, _beamParent);
            beamView.name = $"BeamView_{i}";
            beamView.useWorldSpace = true;
            beamView.positionCount = 2;
            beamView.enabled = false;
            _beamViews[i] = beamView;
        }
        return true;
    }

    private void RefreshBeamViews()
    {
        for (int i = 0; i < _beamViews.Length; i++)
        {
            bool isVisible = i < _propagation.SegmentCount;
            _beamViews[i].enabled = isVisible;
            if (!isVisible)
            {
                continue;
            }
            BeamSegment segment = _propagation.GetSegment(i);
            _beamViews[i].SetPosition(0, segment.Start);
            _beamViews[i].SetPosition(1, segment.End);
        }
    }

    private void HideBeamViews()
    {
        if (_beamViews == null)
        {
            return;
        }
        for (int i = 0; i < _beamViews.Length; i++)
        {
            _beamViews[i].enabled = false;
        }
    }

    private bool TryValidateSetup()
    {
        if (string.IsNullOrWhiteSpace(_objectId) || _puzzlePopup == null
            || _playerInteractionController == null || _playerMoveController == null
            || _pillars == null || _pillars.Length == 0 || _beamPrefab == null
            || _beamParent == null || _unsolvedStructure == null || _solvedStructure == null
            || _interactionCollider == null || _maxBeamDistance <= 0f
            || float.IsNaN(_maxBeamDistance) || float.IsInfinity(_maxBeamDistance))
        {
            return LogInvalid("필수 ID, 참조 또는 최대 빛 거리가 올바르지 않습니다.");
        }
        if (!_puzzlePopup.KeepsPuzzleManagerEnabled || _puzzlePopup.ClosesAutomaticallyOnClear
            || _puzzlePopup.PuzzleManager != this)
        {
            return LogInvalid("팝업의 Beam 전용 생명주기 설정 또는 Puzzle Manager 연결이 올바르지 않습니다.");
        }
        if (_pillarLayerMask.value == 0 || _obstacleLayerMask.value == 0
            || (_pillarLayerMask.value & _obstacleLayerMask.value) != 0)
        {
            return LogInvalid("기둥과 장애물 LayerMask는 비어 있지 않고 서로 분리되어야 합니다.");
        }
        int startCount = 0;
        int endCount = 0;
        for (int i = 0; i < _pillars.Length; i++)
        {
            BeamPillarView pillar = _pillars[i];
            if (pillar == null)
            {
                return LogInvalid($"기둥 참조가 비어 있습니다. pillarIndex={i}");
            }
            if (!pillar.TryValidate(out string error))
            {
                return LogInvalid($"pillarIndex={i}, reason={error}");
            }
            if ((_pillarLayerMask.value & (1 << pillar.gameObject.layer)) == 0)
            {
                return LogInvalid($"기둥 레이어가 Pillar LayerMask에 없습니다. id={pillar.PillarId}");
            }
            startCount += pillar.PillarType == BeamPillarType.BEAM_PILLAR_START ? 1 : 0;
            endCount += pillar.PillarType == BeamPillarType.BEAM_PILLAR_END ? 1 : 0;
            for (int otherIndex = 0; otherIndex < i; otherIndex++)
            {
                if (_pillars[otherIndex].PillarId == pillar.PillarId)
                {
                    return LogInvalid($"중복 기둥 ID입니다. id={pillar.PillarId}");
                }
            }
        }
        if (startCount != 1 || endCount != 1)
        {
            return LogInvalid($"시작과 끝 기둥은 각각 하나여야 합니다. start={startCount}, end={endCount}");
        }
        return true;
    }

    private bool LogInvalid(string reason)
    {
        Debug.LogError($"[BeamPuzzleController] 퍼즐 데이터를 사용할 수 없습니다. {reason}", this);
        return false;
    }

    private void HandlePopupClosed()
    {
        if (_state != BeamPuzzleState.BEAM_PUZZLE_IN_PROGRESS)
        {
            return;
        }
        ReleasePlayerInput();
        HideBeamViews();
        for (int i = 0; i < _pillars.Length; i++)
        {
            _pillars[i].ResetState();
        }
        _isGoalConnected = false;
        _state = BeamPuzzleState.BEAM_PUZZLE_WAITING;
    }

    private void AcquirePlayerInput()
    {
        if (_ownsPlayerInputLock)
        {
            return;
        }
        _playerInteractionController.SetCanInteract(false);
        _playerMoveController.SetCanMove(false);
        _ownsPlayerInputLock = true;
    }

    private void ReleasePlayerInput()
    {
        if (!_ownsPlayerInputLock)
        {
            return;
        }
        _ownsPlayerInputLock = false;
        _playerInteractionController?.SetCanInteract(true);
        _playerMoveController?.SetCanMove(true);
    }

    private void ApplyStructureState(bool isSolved)
    {
        _unsolvedStructure.SetActive(!isSolved);
        _solvedStructure.SetActive(isSolved);
        _interactionCollider.enabled = !isSolved;
    }
}
