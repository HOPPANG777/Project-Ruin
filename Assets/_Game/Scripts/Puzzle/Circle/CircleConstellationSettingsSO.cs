//=============================================================================
// File:        CircleConstellationSettingsSO.cs
// Description: 기존 링·문양 프리팹을 사용하는 배치별 별자리 정답 설정입니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>표시는 바깥→안쪽이며 CSV의 안쪽→바깥 순서는 명시적으로 변환합니다.</summary>
[CreateAssetMenu(menuName = "Puzzle/원형 별자리 설정")]
public sealed class CircleConstellationSettingsSO : ScriptableObject
{
    /// <summary>하나의 링에 적용할 편집 가능한 정답과 회전 설정입니다.</summary>
    [Serializable]
    public sealed class Ring
    {
        [Tooltip("문양 목록은 이 프리팹의 CirclePattern에서 편집합니다. 개수는 자유롭습니다.")]
        [SerializeField] private GameObject _patternPrefab;
        [Tooltip("그림·색·파일 번호와 무관한 정답 별자리 ID입니다.")]
        [SerializeField] private string _answerId;
        [Tooltip("0도는 3시, 90도는 12시입니다. 링 부모의 로컬 방향입니다.")]
        [SerializeField] private float _alignmentDegrees = 90f;
        [Tooltip("초기 반시계 회전각입니다. 기존 스냅 간격의 정수배로 설정합니다.")]
        [SerializeField] private float _initialRotationDegrees;
        [Tooltip("기존 드래그 종료 스냅 간격입니다. 360도를 정수 개로 나눠야 합니다.")]
        [SerializeField] private float _rotationStepDegrees = 45f;

        /// <summary>편집할 문양 프리팹입니다.</summary>
        public GameObject PatternPrefab => _patternPrefab;
        /// <summary>명시적으로 등록한 정답 ID입니다.</summary>
        public string AnswerId => _answerId;
        /// <summary>부모 로컬 정답 방향입니다.</summary>
        public float AlignmentDegrees => _alignmentDegrees;
        /// <summary>기존 스냅과 실제 문양의 도달 가능성을 검사할 회전 간격입니다.</summary>
        public float RotationStepDegrees => _rotationStepDegrees;
        /// <summary>기존 링 조작의 회전 단계 수입니다.</summary>
        public int Segments => Mathf.RoundToInt(360f / _rotationStepDegrees);
        /// <summary>기존 링 초기화에 전달할 회전 단계입니다.</summary>
        public int InitialOffset => Mathf.RoundToInt(_initialRotationDegrees / _rotationStepDegrees);

        /// <summary>회전 설정과 정답 문양의 실제 도달 가능성을 검증합니다.</summary>
        public bool TryValidate(out string error)
        {
            error = string.Empty;
            if (!Finite(_alignmentDegrees) || !Finite(_initialRotationDegrees)
                || !Finite(_rotationStepDegrees) || _rotationStepDegrees <= 0f
                || _rotationStepDegrees > 360f || 360f / _rotationStepDegrees > 36000f
                || Mathf.Abs(360f / _rotationStepDegrees - Segments) > 0.001f
                || Mathf.Abs(_initialRotationDegrees / _rotationStepDegrees - InitialOffset) > 0.001f)
            {
                error = "방향·초기 회전·회전 간격을 확인하세요. 간격은 360도의 약수, 초기값은 간격의 정수배여야 합니다.";
                return false;
            }
            if (_patternPrefab == null || !_patternPrefab.TryGetComponent(out CirclePattern pattern))
            {
                error = "CirclePattern 문양 프리팹 참조가 없습니다.";
                return false;
            }
            return pattern.TryValidateConstellation(_answerId, out error);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [Header("바깥 → 안쪽 링 (CSV ring_order와 반대 순서)")]
    [Tooltip("CIRCLE_STAGE_01의 링을 바깥부터 편집합니다. 문양 개수나 목록을 코드에서 고정하지 않습니다.")]
    [SerializeField] private Ring[] _outerToInner = Array.Empty<Ring>();

    /// <summary>기획자가 보는 바깥→안쪽 링 목록입니다.</summary>
    public IReadOnlyList<Ring> OuterToInner => _outerToInner;

    /// <summary>CSV의 ring_order를 바깥→안쪽 설정으로 대응합니다.</summary>
    public Ring GetForRingOrder(int ringOrder) => _outerToInner[_outerToInner.Length - 1 - ringOrder];

    //=============================================================================
    // Description: CSV 링 수·순서 및 모든 설정을 검사합니다.
    // Parameters: rings — 기존 CSV 링 목록. Return: bool — 유효 여부.
    //=============================================================================
    /// <summary>누락·잘못된 설정은 정상 클리어로 처리하지 않습니다.</summary>
    public bool TryValidate(IReadOnlyList<PuzzleCircleRingDefinition> rings, out string error)
    {
        error = string.Empty;
        if (_outerToInner == null || rings == null || _outerToInner.Length != rings.Count || rings.Count == 0)
        {
            error = "바깥→안쪽 설정 수가 CSV 링 수와 다릅니다.";
            return false;
        }
        for (int i = 0; i < rings.Count; i++)
        {
            if (rings[i].RingOrder != i || GetForRingOrder(i) == null)
            {
                error = "CSV ring_order는 안쪽부터 연속이어야 하며 링 설정이 필요합니다.";
                return false;
            }
            if (!GetForRingOrder(i).TryValidate(out error))
            {
                error = $"바깥에서 {rings.Count - i}번째 링: {error}";
                return false;
            }
        }
        return true;
    }
}
