//=============================================================================
// File:        MazePuzzlePlate.cs
//
// Description: 플레이어 일회성 발판과 바위 점유 발판의 입력을 처리합니다.
//
// History:     08/06/2026 + Created -- Codex
//              10/11/2026 + 발판 종류 구분 및 실제 몸체 충돌 감지 -- Codex
//
//=============================================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>기존 공용 발판, 플레이어 일회성 발판과 바위 전용 발판을 구분합니다.</summary>
public enum MazePuzzlePlateMode
{
    [InspectorName("플레이어 또는 바위 (점유 중)")]
    MAZE_PLATE_ANY_OCCUPANT = 0,
    [InspectorName("플레이어가 한 번 밟기")]
    MAZE_PLATE_PLAYER_ONCE,
    [InspectorName("바위로 누르기")]
    MAZE_PLATE_STONE_ONLY
}

//=============================================================================
//
// Synopsis:   MazePuzzlePlate;
//                - 실제 몸체 Collider의 점유와 플레이어 최초 입력 유지
//                - 눌림 상태가 바뀔 때만 퍼즐 Controller에 전달
//
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class MazePuzzlePlate : MonoBehaviour
{
    [Header("Plate")]
    [Tooltip("Player Once는 플레이어가 한 번 밟으면 유지되고, Stone Only는 바위가 올라가 있는 동안 눌립니다.")]
    [SerializeField] private MazePuzzlePlateMode _mode;

    [Header("View")]
    [Tooltip("발판의 눌림 조건이 충족되지 않았을 때 표시할 오브젝트입니다.")]
    [SerializeField] private GameObject _releasedObject;
    [Tooltip("발판의 눌림 조건이 충족되었을 때 표시할 오브젝트입니다.")]
    [SerializeField] private GameObject _pressedObject;

    // Stay 콜백에서도 같은 충돌체를 중복 집계하지 않습니다.
    private readonly HashSet<Collider2D> _occupantColliders = new HashSet<Collider2D>();
    private MazePlatePuzzleController _owner;
    private Collider2D _plateTrigger;
    private bool _isSetupValid;

    /// <summary>발판 종류에 따른 눌림 조건이 충족되었는지 나타냅니다.</summary>
    public bool IsPressed { get; private set; }
    /// <summary>이 발판에 허용된 입력 종류입니다.</summary>
    public MazePuzzlePlateMode Mode => _mode;

    private void Awake()
    {
        // 컨트롤러보다 먼저 Awake가 실행되거나 부품만 배치한 경우에도 표시를 준비합니다.
        if (!_isSetupValid) Initialize(null);
    }

    private void Reset()
    {
        if (TryGetComponent(out Collider2D plateTrigger))
        {
            plateTrigger.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isSetupValid || !CanPress(other) || !_occupantColliders.Add(other))
        {
            return;
        }

        if (!IsPressed)
        {
            SetPressed(true, true);
        }
    }

    // 초기화 후에도 겹쳐 있는 몸체는 다음 물리 프레임에 다시 감지합니다.
    private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_isSetupValid || !_occupantColliders.Remove(other))
        {
            return;
        }

        if (_mode != MazePuzzlePlateMode.MAZE_PLATE_PLAYER_ONCE
            && _occupantColliders.Count == 0)
        {
            SetPressed(false, true);
        }
    }

    /// <summary>소유 Controller를 연결하고 발판을 초기 상태로 복원합니다.</summary>
    internal void Initialize(MazePlatePuzzleController owner)
    {
        _owner = owner;
        _isSetupValid = TryValidate(out string issue);
        if (!_isSetupValid)
        {
            Debug.LogError($"[MazePuzzlePlate] {name}: {issue}", this);
            return;
        }
        ResetPlate();
    }

    /// <summary>점유 기록과 표시를 눌리지 않은 최초 상태로 되돌립니다.</summary>
    internal void ResetPlate()
    {
        _occupantColliders.Clear();
        SetPressed(false, false);
    }

    /// <summary>필수 View와 Trigger Collider 설정을 검사합니다.</summary>
    internal bool TryValidate(out string issue)
    {
        TryGetComponent(out _plateTrigger);
        issue = string.Empty;
        if (_mode < MazePuzzlePlateMode.MAZE_PLATE_ANY_OCCUPANT
            || _mode > MazePuzzlePlateMode.MAZE_PLATE_STONE_ONLY)
        {
            issue = "지원하지 않는 발판 종류입니다.";
        }
        else if (_plateTrigger == null || !_plateTrigger.isTrigger)
        {
            issue = "Collider2D가 Trigger로 설정되지 않았습니다.";
        }
        else if (_releasedObject == null || _pressedObject == null)
        {
            issue = "발판의 해제 또는 눌림 View가 연결되지 않았습니다.";
        }
        else if (_releasedObject == _pressedObject)
        {
            issue = "해제와 눌림 View는 서로 다른 오브젝트여야 합니다.";
        }
        return issue.Length == 0;
    }

    private void SetPressed(bool isPressed, bool notifyOwner)
    {
        IsPressed = isPressed;
        _releasedObject.SetActive(!isPressed);
        _pressedObject.SetActive(isPressed);
        if (notifyOwner)
        {
            _owner?.NotifyPlateStateChanged();
        }
    }

    private bool CanPress(Collider2D other)
    {
        // 돌의 넓은 상호작용 센서는 바위가 발판 위에 도착한 것으로 취급하지 않습니다.
        if (other == null || other.isTrigger) return false;
        if (_mode == MazePuzzlePlateMode.MAZE_PLATE_PLAYER_ONCE)
            return other.GetComponentInParent<UnitPlayer>() != null;
        if (_mode == MazePuzzlePlateMode.MAZE_PLATE_STONE_ONLY)
            return other.GetComponentInParent<PushableStone>() != null;
        return other.GetComponentInParent<UnitPlayer>() != null
            || other.GetComponentInParent<PushableStone>() != null;
    }

#if UNITY_EDITOR
    private void OnValidate() => TryValidate(out _);
#endif
}
