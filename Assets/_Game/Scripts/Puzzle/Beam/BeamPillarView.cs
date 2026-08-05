//=============================================================================
// File:        BeamPillarView.cs
//
// Description: 빛줄기 기둥의 직렬화 데이터, 회전 입력과 연결 표시를 담당한다.
//
// History:     08/04/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//=============================================================================
//
// Synopsis:   BeamPillarType;
//                - 빛줄기 기둥의 역할과 출력 개수 규칙
//=============================================================================
public enum BeamPillarType
{
    BEAM_PILLAR_START = 0,
    BEAM_PILLAR_TWO_WAY,
    BEAM_PILLAR_FOUR_WAY,
    BEAM_PILLAR_EIGHT_WAY,
    BEAM_PILLAR_END
}

//=============================================================================
//
// Synopsis:   BeamPillarView;
//                - 기둥별 로컬 출력 각도와 회전 단계 보관
//                - 연결 상태 표시와 포인터 입력 전달
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BeamPillarView : MonoBehaviour, IPointerClickHandler
{
    [Header("Identity")]
    [Tooltip("퍼즐 안에서 이 기둥을 구분하는 고유 ID입니다.")]
    [SerializeField] private string _pillarId;
    [Tooltip("시작, 중간 분기 또는 끝 역할을 결정하는 기둥 종류입니다.")]
    [SerializeField] private BeamPillarType _pillarType;

    [Header("Rotation")]
    [Tooltip("진행 중 포인터 입력으로 이 기둥을 회전할 수 있는지 결정합니다.")]
    [SerializeField] private bool _isRotatable;
    [Tooltip("퍼즐을 열거나 닫을 때 복원할 Z축 최초 각도입니다.")]
    [SerializeField] private float _initialRotationDegrees;
    [Tooltip("한 번 입력할 때 시계방향으로 회전할 각도입니다.")]
    [Min(0f), SerializeField] private float _rotationStepDegrees = 45f;
    [Tooltip("기둥 회전 0도를 기준으로 한 각 출력의 로컬 각도입니다.")]
    [SerializeField] private float[] _outputAnglesDegrees = new float[0];

    [Header("View")]
    [Tooltip("연결 또는 끊김 색상을 표시할 기둥 Image입니다.")]
    [SerializeField] private Image _pillarImage;
    [Tooltip("빛이 닿지 않은 기둥의 색상입니다.")]
    [SerializeField] private Color _disconnectedColor = Color.gray;
    [Tooltip("빛이 닿은 기둥의 색상입니다.")]
    [SerializeField] private Color _connectedColor = Color.white;

    private BeamPuzzleController _owner;
    private Collider2D _pillarCollider;
    private int _pillarIndex;
    private int _rotationStepIndex;
    private bool _isConnected;

    /// <summary>퍼즐 안에서 사용하는 고유 기둥 ID입니다.</summary>
    public string PillarId => _pillarId;
    /// <summary>이 기둥의 역할과 출력 개수 규칙입니다.</summary>
    public BeamPillarType PillarType => _pillarType;
    /// <summary>현재 기둥을 회전할 수 있는지 여부입니다.</summary>
    public bool IsRotatable => _isRotatable;
    /// <summary>이 기둥이 가진 출력 방향 수입니다.</summary>
    public int OutputCount => _outputAnglesDegrees == null ? 0 : _outputAnglesDegrees.Length;
    /// <summary>광선 충돌에 사용하는 캐시된 Collider입니다.</summary>
    public Collider2D PillarCollider => _pillarCollider;
    /// <summary>현재 빛 연결 여부입니다.</summary>
    public bool IsConnected => _isConnected;

    /// <summary>컨트롤러와 고정 배열 인덱스를 연결하고 최초 상태로 초기화합니다.</summary>
    public void Initialize(BeamPuzzleController owner, int pillarIndex)
    {
        _owner = owner;
        _pillarIndex = pillarIndex;
        TryGetComponent(out _pillarCollider);
        ResetState();
    }

    /// <summary>현재 회전이 반영된 월드 출력 각도를 반환합니다.</summary>
    public float GetOutputAngleDegrees(int outputIndex)
    {
        return transform.eulerAngles.z + _outputAnglesDegrees[outputIndex];
    }

    /// <summary>회전 가능한 기둥을 한 단계 시계방향으로 회전합니다.</summary>
    public bool TryRotateClockwise()
    {
        if (!_isRotatable)
        {
            return false;
        }

        _rotationStepIndex++;
        ApplyRotation();
        return true;
    }

    /// <summary>회전, 연결 표시를 직렬화된 최초 상태로 복원합니다.</summary>
    public void ResetState()
    {
        _rotationStepIndex = 0;
        ApplyRotation();
        SetConnected(false);
    }

    /// <summary>기둥 연결 상태와 표시 색상을 함께 변경합니다.</summary>
    public void SetConnected(bool isConnected)
    {
        _isConnected = isConnected;
        if (_pillarImage != null)
        {
            _pillarImage.color = isConnected ? _connectedColor : _disconnectedColor;
        }
    }

    /// <summary>포인터 입력을 소유 컨트롤러의 회전 요청으로 전달합니다.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        _owner?.TryRotatePillar(_pillarIndex);
    }

    internal bool TryValidate(out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(_pillarId))
        {
            error = "기둥 ID가 비어 있습니다.";
            return false;
        }
        if (_pillarImage == null || !TryGetComponent(out _pillarCollider))
        {
            error = "Image 또는 Collider2D가 연결되지 않았습니다.";
            return false;
        }
        if (!IsFinite(_initialRotationDegrees)
            || (_isRotatable && (!IsFinite(_rotationStepDegrees) || _rotationStepDegrees <= 0f)))
        {
            error = "최초 회전 또는 회전 단위가 올바르지 않습니다.";
            return false;
        }
        if (!HasValidOutputs())
        {
            error = "기둥 종류와 출력 각도 개수가 일치하지 않거나 각도가 유효하지 않습니다.";
            return false;
        }
        return true;
    }

    private bool HasValidOutputs()
    {
        int expectedCount = GetExpectedOutputCount();
        if (expectedCount == int.MinValue)
        {
            return false;
        }
        if (_outputAnglesDegrees == null
            || (expectedCount < 0 ? _outputAnglesDegrees.Length == 0 : _outputAnglesDegrees.Length != expectedCount))
        {
            return false;
        }
        for (int i = 0; i < _outputAnglesDegrees.Length; i++)
        {
            if (!IsFinite(_outputAnglesDegrees[i]))
            {
                return false;
            }
        }
        return true;
    }

    private int GetExpectedOutputCount()
    {
        switch (_pillarType)
        {
            case BeamPillarType.BEAM_PILLAR_START: return -1;
            case BeamPillarType.BEAM_PILLAR_TWO_WAY: return 1;
            case BeamPillarType.BEAM_PILLAR_FOUR_WAY: return 3;
            case BeamPillarType.BEAM_PILLAR_EIGHT_WAY: return 7;
            case BeamPillarType.BEAM_PILLAR_END: return 0;
            default: return int.MinValue;
        }
    }

    private void ApplyRotation()
    {
        transform.localRotation = Quaternion.Euler(
            0f, 0f, _initialRotationDegrees - _rotationStepIndex * _rotationStepDegrees);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
