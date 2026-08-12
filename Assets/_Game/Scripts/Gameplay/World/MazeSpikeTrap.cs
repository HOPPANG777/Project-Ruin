//=============================================================================
// File:        MazeSpikeTrap.cs
//
// Description: 진행 중인 미로에서 설정된 주기로 가시를 넣고 꺼냅니다.
//
// History:     08/08/2026 + Created -- Codex
//
//=============================================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class MazeSpikeTrap : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private MazePlatePuzzleController _puzzle;
    [Header("Cycle")]
    [SerializeField] private bool _startExtended;
    [Min(0.05f)]
    [SerializeField] private float _extendedDuration = 1.0f;
    [Min(0.05f)]
    [SerializeField] private float _retractedDuration = 1.0f;
    [Header("View")]
    [SerializeField] private GameObject _retractedObject;
    [Tooltip("피해 타일을 자식으로 포함하는 돌출 가시 오브젝트입니다.")]
    [SerializeField] private GameObject _extendedObject;
    [SerializeField] private MazeHazardTile _damageTile;

    private bool _isSetupValid;
    private bool _isRunning;
    private bool _isExtended;
    private float _elapsedTime;

    private void Awake()
    {
        _isSetupValid = TryValidateSetup(true);
        if (_isSetupValid)
        {
            SetExtended(false);
        }
    }

    private void OnEnable()
    {
        if (!_isSetupValid)
        {
            return;
        }
        _puzzle.StateChanged += ApplyPuzzleState;
        ApplyPuzzleState(_puzzle.State);
    }

    private void OnDisable()
    {
        if (_puzzle != null)
        {
            _puzzle.StateChanged -= ApplyPuzzleState;
        }
        _isRunning = false;
        if (_isSetupValid)
        {
            SetExtended(false);
        }
    }

    private void Update()
    {
        if (!_isRunning)
        {
            return;
        }

        _elapsedTime += Time.deltaTime;
        float currentDuration = _isExtended ? _extendedDuration : _retractedDuration;
        if (_elapsedTime < currentDuration)
        {
            return;
        }
        _elapsedTime = 0.0f;
        SetExtended(!_isExtended);
    }

    private void ApplyPuzzleState(MazePlatePuzzleState state)
    {
        _isRunning = state == MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS;
        _elapsedTime = 0.0f;
        SetExtended(_isRunning && _startExtended);
    }

    private void SetExtended(bool isExtended)
    {
        _isExtended = isExtended;
        _retractedObject.SetActive(!isExtended);
        _extendedObject.SetActive(isExtended);
    }

    private bool TryValidateSetup(bool logIssue)
    {
        string issue = string.Empty;
        if (_puzzle == null || _retractedObject == null || _extendedObject == null
            || _damageTile == null || _retractedObject == _extendedObject
            || _retractedObject == gameObject || _extendedObject == gameObject)
        {
            issue = "퍼즐, 별도 자식 가시 View와 피해 타일이 필요합니다.";
        }
        else if (float.IsNaN(_extendedDuration) || float.IsInfinity(_extendedDuration)
            || float.IsNaN(_retractedDuration) || float.IsInfinity(_retractedDuration)
            || _extendedDuration < 0.05f || _retractedDuration < 0.05f)
        {
            issue = "가시 돌출·수납 시간은 각각 0.05초 이상이어야 합니다.";
        }
        else if (_damageTile.transform != _extendedObject.transform
            && !_damageTile.transform.IsChildOf(_extendedObject.transform))
        {
            issue = "피해 타일은 돌출 가시 오브젝트에 포함되어야 합니다.";
        }
        else if (!_damageTile.TryValidate(false, out issue))
        {
            issue = $"가시 피해 타일 설정이 올바르지 않습니다. reason={issue}";
        }
        if (issue.Length == 0)
        {
            return true;
        }
        if (logIssue)
        {
            Debug.LogError($"[MazeSpikeTrap] {name}: {issue}", this);
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate() => TryValidateSetup(false);
#endif
}
