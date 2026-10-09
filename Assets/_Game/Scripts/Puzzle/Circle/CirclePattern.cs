using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//=============================================================================
// File:        CirclePattern.cs
// Description: 기존 색상 판정과 선택적 별자리 ID 판정에 실제 문양 방향을 제공합니다.
// History:     10/07/2026 + 명시적 별자리 ID·참조 검증 추가 -- Codex
//=============================================================================

/// <summary>프리팹에 배치한 문양의 색과 실제 방향을 정답 판정에 제공한다.</summary>
public sealed class CirclePattern : MonoBehaviour
{
    public enum SymbolColor { Yellow, Red, Green, Blue, Purple, Orange, Navy }

    [Serializable]
    public struct Symbol
    {
        [Tooltip("기존 Image의 RectTransform입니다. 그림·위치·각도는 프리팹에서 직접 수정합니다.")]
        public RectTransform icon;
        [Tooltip("기존 원형 퍼즐에서 사용하는 색상입니다. 별자리 판정에서는 사용하지 않습니다.")]
        public SymbolColor color;
        [Tooltip("이 문양의 별자리 ID입니다. 정답 설정의 ID와 정확히 같아야 합니다.")]
        public string constellationId;
    }

    [Tooltip("각 문양 Image의 RectTransform과 색상을 연결합니다.")]
    [SerializeField] private Symbol[] _symbols = Array.Empty<Symbol>();

    /// <summary>개수를 고정하지 않는 프리팹 문양 목록입니다.</summary>
    public IReadOnlyList<Symbol> Symbols => _symbols;

    /// <summary>문양의 실제 월드 방향입니다.</summary>
    public Vector3 GetDirection(Symbol symbol)
    {
        return symbol.icon != null ? symbol.icon.position - transform.position : Vector3.zero;
    }

    /// <summary>기존 색상 판정을 그대로 유지합니다.</summary>
    public bool HasAlignedSymbol(SymbolColor color, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return false;

        for (int i = 0; i < _symbols.Length; i++)
        {
            Symbol symbol = _symbols[i];
            Vector3 candidate = GetDirection(symbol);
            if (symbol.color == color && candidate.sqrMagnitude > 0.0001f
                && Vector3.Angle(direction, candidate) < 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>색이나 파일명이 아닌 등록된 ID와 실제 방향으로 정답을 판정합니다.</summary>
    public bool HasAlignedConstellation(string id, Vector3 direction)
    {
        if (string.IsNullOrWhiteSpace(id) || direction.sqrMagnitude < 0.0001f) return false;
        for (int i = 0; i < _symbols.Length; i++)
        {
            Vector3 candidate = GetDirection(_symbols[i]);
            if (_symbols[i].constellationId == id && candidate.sqrMagnitude > 0.0001f
                && Vector3.Angle(direction, candidate) < 0.5f) return true;
        }
        return false;
    }

    /// <summary>실제 링 크기·Anchor·부모 회전에 맞춰 정답 방향에 스냅할 수 있는지 검사합니다.</summary>
    public bool CanAlignConstellation(string id, Vector3 direction, float step)
    {
        if (direction.sqrMagnitude < 0.0001f) return false;
        float target = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        for (int i = 0; i < _symbols.Length; i++)
        {
            Vector3 candidate = GetDirection(_symbols[i]);
            if (_symbols[i].constellationId != id || candidate.sqrMagnitude < 0.0001f) continue;
            float turns = Mathf.DeltaAngle(Mathf.Atan2(candidate.y, candidate.x) * Mathf.Rad2Deg, target) / step;
            if (Mathf.Abs(turns - Mathf.Round(turns)) < 0.001f) return true;
        }
        return false;
    }

    /// <summary>필수 정답, 그림과 프리팹 내부 문양 참조를 검증합니다.</summary>
    public bool TryValidateConstellation(string answerId, out string error)
    {
        error = string.Empty;
        bool hasAnswer = false;
        if (string.IsNullOrWhiteSpace(answerId) || _symbols == null || _symbols.Length == 0)
            error = "정답 ID 또는 문양 목록이 없습니다.";
        else for (int i = 0; i < _symbols.Length; i++)
        {
            RectTransform icon = _symbols[i].icon;
            if (icon == null || !icon.IsChildOf(transform) || !icon.TryGetComponent(out Image image)
                || image.sprite == null || string.IsNullOrWhiteSpace(_symbols[i].constellationId))
            {
                error = $"문양 {i + 1}의 내부 참조·Image 그림·별자리 ID를 확인하세요.";
                return false;
            }
            hasAnswer |= _symbols[i].constellationId == answerId;
        }
        if (error.Length == 0 && !hasAnswer) error = "필수 정답 별자리 문양이 누락됐습니다.";
        return error.Length == 0;
    }
}
