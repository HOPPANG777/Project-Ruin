//=============================================================================
// File: BeamPillarSettings.cs
// Description: Inspector 및 향후 데이터 공급에서 공유할 순수 기둥 설정을 보관한다.
// History: 10/06/2026 + Created -- Codex
//=============================================================================
using System;
using UnityEngine;

/// <summary>입사각에 관계없이 출력 개수를 결정하는 중계 기둥 종류입니다.</summary>
public enum BeamPillarType
{
    BEAM_PILLAR_START = 0,
    BEAM_PILLAR_TWO_WAY,
    BEAM_PILLAR_FOUR_WAY,
    BEAM_PILLAR_EIGHT_WAY,
    BEAM_PILLAR_END
}

/// <summary>입력, 충돌체, 표시 객체를 포함하지 않는 기둥 설정입니다.</summary>
[Serializable]
public sealed class BeamPillarSettings
{
    [Tooltip("이 관리자 내부에서 중복되지 않는 기둥 ID입니다.")]
    [SerializeField] private string _id;
    [Tooltip("시작·2방향·4방향·8방향·끝 중계 종류입니다.")]
    [SerializeField] private BeamPillarType _type;
    [Tooltip("플레이어가 누르기 입력으로 회전할 수 있는지 결정합니다.")]
    [SerializeField] private bool _isRotatable;
    [Tooltip("초기 월드 XY 각도입니다. 0도는 오른쪽이고 90도는 위쪽입니다.")]
    [SerializeField] private float _initialRotationDegrees;
    [Tooltip("입력 한 번마다 시계방향으로 회전할 양수 각도입니다.")]
    [SerializeField] private float _rotationStepDegrees = 45f;
    [Tooltip("초기 방향에 더할 개별 로컬 출력 각도입니다。간격은 자유롭게 설정합니다.")]
    [SerializeField] private float[] _outputAnglesDegrees = new float[0];

    /// <summary>퍼즐 내부 고유 ID입니다.</summary>
    public string Id => _id;
    /// <summary>기둥 종류입니다.</summary>
    public BeamPillarType Type => _type;
    /// <summary>회전 입력 허용 설정입니다.</summary>
    public bool IsRotatable => _isRotatable;
    /// <summary>초기 월드 각도입니다.</summary>
    public float InitialRotationDegrees => _initialRotationDegrees;
    /// <summary>한 번의 시계방향 회전 각도입니다.</summary>
    public float RotationStepDegrees => _rotationStepDegrees;
    /// <summary>설정된 출력 개수입니다.</summary>
    public int OutputCount => _outputAnglesDegrees?.Length ?? 0;

    /// <summary>Unity Inspector 직렬화용 기본 생성자입니다.</summary>
    public BeamPillarSettings() { }

    /// <summary>외부 데이터 공급에서도 같은 설정을 사용할 수 있는 생성자입니다.</summary>
    public BeamPillarSettings(string id, BeamPillarType type, bool rotatable,
        float initialRotation, float rotationStep, params float[] outputAngles)
    {
        _id = id;
        _type = type;
        _isRotatable = rotatable;
        _initialRotationDegrees = initialRotation;
        _rotationStepDegrees = rotationStep;
        _outputAnglesDegrees = outputAngles == null ? null : (float[])outputAngles.Clone();
    }

    /// <summary>개별 출력의 로컬 각도를 반환합니다.</summary>
    public float GetOutputAngle(int index) => _outputAnglesDegrees[index];

    /// <summary>ID, 종류별 출력 수와 모든 각도의 유효성을 검증합니다.</summary>
    public bool TryValidate(out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(_id)) error = "기둥 ID가 비어 있습니다.";
        else if (!IsFinite(_initialRotationDegrees) || !IsFinite(_rotationStepDegrees) || _rotationStepDegrees <= 0f)
            error = "초기 회전각과 양수 회전 단위를 확인하세요.";
        else
        {
            int expected;
            switch (_type)
            {
                case BeamPillarType.BEAM_PILLAR_START: expected = -1; break;
                case BeamPillarType.BEAM_PILLAR_TWO_WAY: expected = 1; break;
                case BeamPillarType.BEAM_PILLAR_FOUR_WAY: expected = 3; break;
                case BeamPillarType.BEAM_PILLAR_EIGHT_WAY: expected = 7; break;
                case BeamPillarType.BEAM_PILLAR_END: expected = 0; break;
                default: error = "알 수 없는 기둥 종류입니다."; return false;
            }
            if (_outputAnglesDegrees == null || (expected < 0 ? OutputCount < 1 : OutputCount != expected))
                error = "기둥 종류와 출력 개수가 일치하지 않습니다.";
            else for (int i = 0; i < OutputCount; i++)
                if (!IsFinite(_outputAnglesDegrees[i])) { error = "유효하지 않은 출력 각도입니다."; break; }
        }
        return error.Length == 0;
    }

    /// <summary>NaN 및 무한대를 제외하는 설정·저장 데이터 공통 검사입니다.</summary>
    public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
