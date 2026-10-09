//=============================================================================
// File: BeamPillarView.cs
// Description: 월드 상호작용을 회전 요청으로 전달하고 몸체·방향 표시를 갱신한다.
// History: 10/06/2026 + 맵 직접 조작으로 전환 -- Codex
//=============================================================================
using UnityEngine;

/// <summary>이동 충돌과 광선 수신을 유지하면서 기존 누르기 입력을 받는 기둥입니다.</summary>
[DisallowMultipleComponent]
public sealed class BeamPillarView : MonoBehaviour, IInteractable
{
    [Tooltip("Inspector와 향후 외부 데이터 공급자가 공유하는 기둥 설정입니다.")]
    [SerializeField] private BeamPillarSettings _settings = new BeamPillarSettings();
    [Header("Colliders")]
    [Tooltip("플레이어 이동을 막는 비 Trigger 몸체입니다. 수신 충돌체와 같아도 됩니다.")]
    [SerializeField] private Collider2D _bodyCollider;
    [Tooltip("광선 연결 판정에 명시적으로 사용하는 충돌체입니다.")]
    [SerializeField] private Collider2D _receiverCollider;
    [Tooltip("기존 플레이어 입력 탐지에 사용하는 별도 주변 Trigger입니다.")]
    [SerializeField] private Collider2D _interactionTrigger;
    [Header("View")]
    [Tooltip("교체 가능한 기둥 몸체 스프라이트 렌더러입니다.")]
    [SerializeField] private SpriteRenderer _bodyRenderer;
    [Tooltip("몸체와 화살표만 회전하는 표시 루트입니다. 물리 충돌체를 넣지 마세요.")]
    [SerializeField] private Transform _directionRoot;
    [Tooltip("출력 개수만큼 연결할 화살표 렌더러입니다. 항상 표시됩니다.")]
    [SerializeField] private SpriteRenderer[] _outputArrows = new SpriteRenderer[0];
    [Tooltip("빛이 닿지 않은 기둥의 표시 색상입니다.")]
    [SerializeField] private Color _disconnectedColor = new Color(0.45f, 0.5f, 0.6f);
    [Tooltip("빛이 닿은 기둥의 표시 색상입니다.")]
    [SerializeField] private Color _connectedColor = new Color(0.4f, 1f, 0.8f);
    [Tooltip("항상 보이는 방향 화살표의 색상입니다.")]
    [SerializeField] private Color _arrowColor = new Color(1f, 0.8f, 0.2f);

    // 소유권과 런타임 각도는 설정 데이터에 쓰지 않아 초기 설정을 보존합니다.
    private BeamPuzzleController _owner;
    private float _rotationDegrees;
    private bool _isInitialized;
    private bool _isConnected;

    /// <summary>퍼즐 내부 고유 ID입니다.</summary>
    public string PillarId => _settings?.Id;
    /// <summary>수신과 출력 규칙입니다.</summary>
    public BeamPillarType PillarType => _settings != null ? _settings.Type : (BeamPillarType)(-1);
    /// <summary>설정에 따른 회전 허용 여부입니다.</summary>
    public bool IsRotatable => _settings != null && _settings.IsRotatable;
    /// <summary>독립적으로 설정된 출력 개수입니다.</summary>
    public int OutputCount => _settings?.OutputCount ?? 0;
    /// <summary>명시적으로 연결한 수신 충돌체입니다.</summary>
    public Collider2D PillarCollider => _receiverCollider;
    /// <summary>플레이어 이동 충돌체입니다.</summary>
    public Collider2D BodyCollider => _bodyCollider;
    /// <summary>상호작용 탐지 Trigger입니다.</summary>
    public Collider2D InteractionTrigger => _interactionTrigger;
    /// <summary>현재 월드 XY 각도입니다. 0도는 오른쪽입니다.</summary>
    public float RotationDegrees => _rotationDegrees;
    /// <summary>현재 광선 연결 표시입니다.</summary>
    public bool IsConnected => _isConnected;
    /// <summary>명시적으로 이 부품을 등록한 관리자입니다.</summary>
    public BeamPuzzleController Owner => _owner;

    /// <summary>설정 공급을 표시와 입력 처리에서 분리합니다. 초기화 전에 사용합니다.</summary>
    public void Configure(BeamPillarSettings settings)
    {
        if (_isInitialized || settings == null) return;
        _settings = settings;
    }

    /// <summary>다른 관리자와 공유하지 않고 최초 각도를 한 번만 적용합니다.</summary>
    public bool TryBind(BeamPuzzleController owner)
    {
        if (_owner != null && _owner != owner) return false;
        _owner = owner;
        if (!_isInitialized) { _isInitialized = true; ResetState(); }
        return true;
    }

    /// <summary>누르기 입력 한 번을 전달합니다. 홀드 계약은 구현하지 않습니다.</summary>
    public void Interact() => _owner?.TryRotatePillar(this);

    /// <summary>현재 회전과 출력별 로컬 각도를 더한 월드 광선 방향입니다.</summary>
    public float GetOutputAngleDegrees(int outputIndex) => _rotationDegrees + _settings.GetOutputAngle(outputIndex);

    internal bool RotateClockwise(BeamPuzzleController owner)
    {
        if (_owner != owner || !IsRotatable) return false;
        SetRotation(_rotationDegrees - _settings.RotationStepDegrees);
        return true;
    }

    /// <summary>저장 상태의 각도를 적용하며 클리어 이벤트를 발생시키지 않습니다.</summary>
    public void SetRotation(float degrees)
    {
        if (!BeamPillarSettings.IsFinite(degrees)) return;
        _rotationDegrees = Mathf.Repeat(degrees, 360f);
        RefreshView();
    }

    /// <summary>씬에 설정한 최초 각도와 끊김 표시로 돌아갑니다.</summary>
    public void ResetState() { SetRotation(_settings.InitialRotationDegrees); SetConnected(false); }

    /// <summary>연결 색상만 바꿉니다. 화살표와 충돌체는 유지합니다.</summary>
    public void SetConnected(bool connected)
    {
        _isConnected = connected;
        if (_bodyRenderer != null) _bodyRenderer.color = connected ? _connectedColor : _disconnectedColor;
    }

    /// <summary>입력 탐지 구조, 이동 충돌, 표시와 설정을 검증합니다.</summary>
    public bool TryValidate(out string error)
    {
        error = string.Empty;
        if (_settings == null) { error = "기둥 설정이 없습니다."; return false; }
        if (!_settings.TryValidate(out error)) return false;
        if (_bodyCollider == null || _receiverCollider == null || _interactionTrigger == null
            || _bodyCollider.isTrigger || !_bodyCollider.enabled || !_receiverCollider.enabled
            || !_interactionTrigger.isTrigger || !_interactionTrigger.enabled
            || _interactionTrigger == _receiverCollider || _interactionTrigger == _bodyCollider)
        {
            error = "몸체(비 Trigger), 수신 충돌체, 별도 상호작용 Trigger를 확인하세요.";
            return false;
        }
        if (!BelongsToPillar(_bodyCollider) || !BelongsToPillar(_receiverCollider) || !BelongsToPillar(_interactionTrigger))
        { error = "충돌체는 이 기둥 오브젝트 또는 자식이어야 합니다."; return false; }
        Rigidbody2D body = _interactionTrigger.attachedRigidbody;
        if (!ReferenceEquals(_interactionTrigger.GetComponent<IInteractable>(), this)
            && (body == null || !ReferenceEquals(body.GetComponent<IInteractable>(), this)))
        { error = "Trigger 자신 또는 attachedRigidbody에서 기둥 IInteractable을 찾을 수 없습니다."; return false; }
        if (_bodyCollider.attachedRigidbody != null && _bodyCollider.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
        { error = "기둥 몸체 Rigidbody는 Static 또는 Kinematic이어야 합니다."; return false; }
        if (_bodyCollider.attachedRigidbody != null && !_bodyCollider.attachedRigidbody.simulated)
        { error = "이동을 막으려면 기둥 몸체 Rigidbody의 Simulated가 켜져 있어야 합니다."; return false; }
        if (_bodyRenderer == null || _bodyRenderer.sprite == null || _directionRoot == null
            || _directionRoot == transform || !_directionRoot.IsChildOf(transform)
            || _outputArrows == null || _outputArrows.Length != OutputCount)
        { error = "몸체 Sprite와 별도 표시 루트, 출력 개수만큼의 화살표가 필요합니다."; return false; }
        if (_bodyCollider.transform.IsChildOf(_directionRoot) || _receiverCollider.transform.IsChildOf(_directionRoot))
        { error = "충돌체를 표시 루트 밖에 두세요."; return false; }
        for (int i = 0; i < OutputCount; i++)
        {
            if (_outputArrows[i] == null || _outputArrows[i].sprite == null || !_outputArrows[i].transform.IsChildOf(_directionRoot))
            { error = "모든 출력 화살표에 Sprite와 표시 루트 연결이 필요합니다."; return false; }
        }
        return true;
    }

    private bool BelongsToPillar(Collider2D collider) => collider.transform == transform || collider.transform.IsChildOf(transform);

    private void RefreshView()
    {
        // 부모가 서로 다르고 회전돼 있어도 데이터의 월드 각도와 표시가 일치합니다.
        if (_directionRoot != null) _directionRoot.rotation = Quaternion.Euler(0f, 0f, _rotationDegrees);
        if (_outputArrows == null) return;
        for (int i = 0; i < Mathf.Min(OutputCount, _outputArrows.Length); i++)
        {
            SpriteRenderer arrow = _outputArrows[i];
            if (arrow == null) continue;
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, _settings.GetOutputAngle(i));
            arrow.color = _arrowColor;
            arrow.enabled = true;
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || _settings == null) return;
        _rotationDegrees = _settings.InitialRotationDegrees;
        RefreshView();
        SetConnected(false);
    }
#endif
}
