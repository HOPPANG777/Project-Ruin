using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>프리팹에 배치한 문양의 색과 실제 방향을 정답 판정에 제공한다.</summary>
public sealed class CirclePattern : MonoBehaviour
{
    public enum SymbolColor { Yellow, Red, Green, Blue, Purple, Orange, Navy }

    [Serializable]
    public struct Symbol
    {
        public RectTransform icon;
        public SymbolColor color;
    }

    [Tooltip("각 문양 Image의 RectTransform과 색상을 연결합니다.")]
    [SerializeField] private Symbol[] _symbols = Array.Empty<Symbol>();

    public IReadOnlyList<Symbol> Symbols => _symbols;

    public Vector3 GetDirection(Symbol symbol)
    {
        return symbol.icon != null ? symbol.icon.position - transform.position : Vector3.zero;
    }

    public bool HasAlignedSymbol(SymbolColor color, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return false;

        foreach (Symbol symbol in _symbols)
        {
            Vector3 candidate = GetDirection(symbol);
            if (symbol.color == color && candidate.sqrMagnitude > 0.0001f
                && Vector3.Angle(direction, candidate) < 0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
