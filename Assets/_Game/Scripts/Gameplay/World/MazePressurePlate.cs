//=============================================================================
// File:        MazePressurePlate.cs
//
// Description: 플레이어가 밟으면 연결된 미로 문을 한 번만 여는 압력 발판입니다.
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
// Synopsis:   MazePressurePlate;
//                - UnitPlayer의 발판 진입만 허용
//                - 발판과 문의 닫힘/열림 상태를 한 번만 전환
//
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MazePressurePlate : MonoBehaviour, ISceneStateContributor
{
    private const string OPENED_STATE_JSON = "true";

    [Header("Identity")]
    [Tooltip("씬 상태 저장에서 이 발판을 구분하는 고유 ID입니다.")]
    [SerializeField] private string _objectId;

    [Header("Plate")]
    [Tooltip("밟기 전 표시할 발판 오브젝트입니다.")]
    [SerializeField] private GameObject _raisedPlateObject;
    [Tooltip("밟은 뒤 표시할 발판 오브젝트입니다.")]
    [SerializeField] private GameObject _pressedPlateObject;

    [Header("Door")]
    [Tooltip("문이 닫혀 있을 때 표시할 오브젝트입니다.")]
    [SerializeField] private GameObject _closedDoorObject;
    [Tooltip("문이 열린 뒤 표시할 오브젝트입니다.")]
    [SerializeField] private GameObject _openDoorObject;
    [Tooltip("문이 닫혀 있을 때 플레이어를 막는 Collider2D입니다.")]
    [SerializeField] private Collider2D _doorBlocker;

    [Header("Result")]
    [Tooltip("문이 처음 열릴 때 한 번만 실행되는 결과 이벤트입니다.")]
    [SerializeField] private UnityEvent _onActivated;

    private BoxCollider2D _plateTrigger;
    private bool _isSetupValid;
    private bool _isActivated;

    /// <summary>발판이 이미 작동하여 문이 열린 상태인지 여부입니다.</summary>
    public bool IsActivated => _isActivated;
    /// <summary>씬 상태 저장에서 사용하는 고유 발판 ID입니다.</summary>
    public string ObjectId => _objectId;

    private void Reset()
    {
        if (TryGetComponent(out BoxCollider2D plateTrigger))
        {
            plateTrigger.isTrigger = true;
        }
    }

    private void Awake()
    {
        TryGetComponent(out _plateTrigger);
        _isSetupValid = TryValidateSetup(true);
        if (_isSetupValid)
        {
            ApplyActivatedState(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isSetupValid || _isActivated
            || other.GetComponentInParent<UnitPlayer>() == null)
        {
            return;
        }

        ActivateOnce();
    }

    /// <summary>문이 열린 최종 상태만 저장 문자열로 반환합니다.</summary>
    public string CaptureState()
    {
        return _isSetupValid && _isActivated ? OPENED_STATE_JSON : string.Empty;
    }

    /// <summary>저장된 문 개방 상태를 결과 이벤트 없이 복원합니다.</summary>
    public void ApplyState(string stateJson)
    {
        if (!_isSetupValid || string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }
        if (!bool.TryParse(stateJson, out bool isOpened))
        {
            Debug.LogWarning(
                $"[MazePressurePlate] 저장 상태를 읽을 수 없습니다. object={name}", this);
            return;
        }
        if (!isOpened || _isActivated)
        {
            return;
        }

        _isActivated = true;
        ApplyActivatedState(true);
    }

    private void ActivateOnce()
    {
        _isActivated = true;
        ApplyActivatedState(true);
        _onActivated?.Invoke();
    }

    private void ApplyActivatedState(bool isActivated)
    {
        _doorBlocker.enabled = !isActivated;
        _raisedPlateObject.SetActive(!isActivated);
        _pressedPlateObject.SetActive(isActivated);
        _closedDoorObject.SetActive(!isActivated);
        _openDoorObject.SetActive(isActivated);
    }

    private bool TryValidateSetup(bool logIssue)
    {
        string issue = null;
        if (string.IsNullOrWhiteSpace(_objectId))
        {
            issue = "ObjectId가 비어 있습니다.";
        }
        else if (_plateTrigger == null || !_plateTrigger.isTrigger)
        {
            issue = "BoxCollider2D가 Trigger로 설정되지 않았습니다.";
        }
        else if (_raisedPlateObject == null || _pressedPlateObject == null
            || _closedDoorObject == null || _openDoorObject == null
            || _doorBlocker == null)
        {
            issue = "필수 발판 또는 문 참조가 연결되지 않았습니다.";
        }
        else if (_raisedPlateObject == _pressedPlateObject
            || _closedDoorObject == _openDoorObject)
        {
            issue = "닫힘과 열림 상태에 같은 오브젝트를 사용할 수 없습니다.";
        }

        if (issue == null)
        {
            return true;
        }
        if (logIssue)
        {
            Debug.LogError($"[MazePressurePlate] {name}: {issue}", this);
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!TryGetComponent(out _plateTrigger))
        {
            return;
        }
        TryValidateSetup(false);
    }

    [ContextMenu("발판 설정 검증")]
    private void ValidateSetup()
    {
        TryGetComponent(out _plateTrigger);
        TryValidateSetup(true);
    }
#endif
}
