//=============================================================================
// File:        MazePlatePuzzleTrigger.cs
//
// Description: 플레이어 진입으로 미로 발판 퍼즐을 시작하거나 초기화합니다.
//
// History:     08/06/2026 + Created -- Codex
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;

public enum MazePlatePuzzleTriggerAction
{
    MAZE_PLATE_TRIGGER_BEGIN = 0,
    MAZE_PLATE_TRIGGER_RESET
}

//=============================================================================
//
// Synopsis:   MazePlatePuzzleTrigger;
//                - 시작 지점과 초기화 발판의 공통 플레이어 Trigger
//                - 성공한 상태 전환의 연출 이벤트를 한 번 전달
//
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class MazePlatePuzzleTrigger : MonoBehaviour
{
    [Header("Puzzle")]
    [Tooltip("시작 또는 초기화 요청을 받을 미로 발판 퍼즐입니다.")]
    [SerializeField] private MazePlatePuzzleController _puzzle;
    [Tooltip("플레이어 진입 시 실행할 시작 또는 초기화 동작입니다.")]
    [SerializeField] private MazePlatePuzzleTriggerAction _action;
    [Tooltip("상태 전환에 성공했을 때 발판 연출을 실행합니다.")]
    [SerializeField] private UnityEvent _onTriggered;

    private Collider2D _trigger;
    private bool _isSetupValid;

    private void Reset()
    {
        if (TryGetComponent(out Collider2D trigger))
        {
            trigger.isTrigger = true;
        }
    }

    private void Awake()
    {
        _isSetupValid = TryValidate(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isSetupValid || other.GetComponentInParent<UnitPlayer>() == null)
        {
            return;
        }

        bool succeeded = _action == MazePlatePuzzleTriggerAction.MAZE_PLATE_TRIGGER_BEGIN
            ? _puzzle.TryBeginPuzzle()
            : _puzzle.TryResetPuzzle();
        if (succeeded)
        {
            _onTriggered?.Invoke();
        }
    }

    private bool TryValidate(bool logIssue)
    {
        TryGetComponent(out _trigger);
        string issue = string.Empty;
        if (_puzzle == null || _trigger == null || !_trigger.isTrigger)
        {
            issue = "퍼즐 참조가 없거나 Collider2D가 Trigger로 설정되지 않았습니다.";
        }
        else if (_action != MazePlatePuzzleTriggerAction.MAZE_PLATE_TRIGGER_BEGIN
            && _action != MazePlatePuzzleTriggerAction.MAZE_PLATE_TRIGGER_RESET)
        {
            issue = "지원하지 않는 Trigger Action입니다.";
        }
        if (issue.Length == 0)
        {
            return true;
        }
        if (logIssue)
        {
            Debug.LogError($"[MazePlatePuzzleTrigger] {name}: {issue}", this);
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate() => TryValidate(false);
#endif
}
