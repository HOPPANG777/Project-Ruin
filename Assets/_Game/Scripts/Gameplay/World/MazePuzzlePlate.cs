//=============================================================================
// File:        MazePuzzlePlate.cs
//
// Description: 플레이어 또는 돌이 올라가 있는 동안만 눌리는 미로 퍼즐 발판입니다.
//
// History:     08/06/2026 + Created -- Codex
//
//=============================================================================

using System.Collections.Generic;
using UnityEngine;

//=============================================================================
//
// Synopsis:   MazePuzzlePlate;
//                - 플레이어와 돌의 다중 Collider 점유 수 추적
//                - 눌림 상태가 바뀔 때만 퍼즐 Controller에 전달
//
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class MazePuzzlePlate : MonoBehaviour
{
    [Header("View")]
    [Tooltip("아무도 올라가 있지 않을 때 표시할 발판 오브젝트입니다.")]
    [SerializeField] private GameObject _releasedObject;
    [Tooltip("플레이어 또는 돌이 올라가 있을 때 표시할 발판 오브젝트입니다.")]
    [SerializeField] private GameObject _pressedObject;

    private readonly Dictionary<int, int> _occupantColliderCounts
        = new Dictionary<int, int>(2);
    private MazePlatePuzzleController _owner;
    private Collider2D _plateTrigger;
    private bool _isSetupValid;

    /// <summary>현재 플레이어나 돌이 하나 이상 발판을 점유하고 있는지 나타냅니다.</summary>
    public bool IsPressed { get; private set; }

    private void Reset()
    {
        if (TryGetComponent(out Collider2D plateTrigger))
        {
            plateTrigger.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isSetupValid || !TryGetOccupantId(other, out int occupantId))
        {
            return;
        }

        bool wasPressed = _occupantColliderCounts.Count > 0;
        if (_occupantColliderCounts.TryGetValue(occupantId, out int colliderCount))
        {
            _occupantColliderCounts[occupantId] = colliderCount + 1;
        }
        else
        {
            _occupantColliderCounts.Add(occupantId, 1);
        }
        if (!wasPressed)
        {
            SetPressed(true, true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_isSetupValid || !TryGetOccupantId(other, out int occupantId)
            || !_occupantColliderCounts.TryGetValue(occupantId, out int colliderCount))
        {
            return;
        }

        if (colliderCount > 1)
        {
            _occupantColliderCounts[occupantId] = colliderCount - 1;
            return;
        }

        _occupantColliderCounts.Remove(occupantId);
        if (_occupantColliderCounts.Count == 0)
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
        _occupantColliderCounts.Clear();
        SetPressed(false, false);
    }

    /// <summary>필수 View와 Trigger Collider 설정을 검사합니다.</summary>
    internal bool TryValidate(out string issue)
    {
        TryGetComponent(out _plateTrigger);
        issue = string.Empty;
        if (_plateTrigger == null || !_plateTrigger.isTrigger)
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

    private static bool TryGetOccupantId(Collider2D other, out int occupantId)
    {
        UnitPlayer player = other.GetComponentInParent<UnitPlayer>();
        if (player != null)
        {
            occupantId = player.GetInstanceID();
            return true;
        }

        PushableStone stone = other.GetComponentInParent<PushableStone>();
        occupantId = stone == null ? 0 : stone.GetInstanceID();
        return stone != null;
    }

#if UNITY_EDITOR
    private void OnValidate() => TryValidate(out _);
#endif
}
