//=============================================================================
// File: BeamPropagation.cs
// Description: 등록한 수신 충돌체로 중계·분기 경로를 계산하고 최근접 차단점을 선택한다.
// History: 10/06/2026 + 월드 충돌, Trigger 제외, 복수 퍼즐 분리 -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>광선 표시에서 사용하는 월드 선분입니다.</summary>
public readonly struct BeamSegment
{
    /// <summary>기둥 중심의 시작점입니다.</summary>
    public Vector2 Start { get; }
    /// <summary>최근접 유효 충돌 또는 최대 거리의 종료점입니다.</summary>
    public Vector2 End { get; }
    /// <summary>표시할 선분을 생성합니다.</summary>
    public BeamSegment(Vector2 start, Vector2 end) { Start = start; End = end; }
}

/// <summary>각 기둥을 한 번만 방문하는 반복형 중계 탐색입니다. 물리 반사는 계산하지 않습니다.</summary>
public sealed class BeamPropagation
{
    private readonly BeamPillarView[] _pillars;
    private readonly Dictionary<Collider2D, int> _receiverIndices = new Dictionary<Collider2D, int>();
    private readonly Dictionary<Collider2D, int> _bodyIndices = new Dictionary<Collider2D, int>();
    private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>(32);
    private readonly int[] _queue;
    private readonly bool[] _visited;
    private readonly bool[] _connected;
    private readonly BeamSegment[] _segments;
    private readonly ContactFilter2D _filter;
    private readonly float _maxDistance;
    private readonly int _startIndex;
    private int _segmentCount;

    /// <summary>마지막 계산에서 생성된 선분 수입니다.</summary>
    public int SegmentCount => _segmentCount;
    /// <summary>모든 기둥의 출력 합이며 순환해도 이 용량을 넘지 않습니다.</summary>
    public int SegmentCapacity => _segments.Length;

    /// <summary>명시적으로 등록한 기둥만 연결할 계산기를 준비합니다.</summary>
    public BeamPropagation(BeamPillarView[] pillars, LayerMask collisionMask, float maxDistance)
    {
        _pillars = pillars;
        _maxDistance = maxDistance;
        _filter = new ContactFilter2D { useLayerMask = true, layerMask = collisionMask, useTriggers = true };
        _queue = new int[pillars.Length];
        _visited = new bool[pillars.Length];
        _connected = new bool[pillars.Length];
        int startIndex = -1;
        int capacity = 0;
        for (int i = 0; i < pillars.Length; i++)
        {
            _receiverIndices.Add(pillars[i].PillarCollider, i);
            _bodyIndices.Add(pillars[i].BodyCollider, i);
            capacity += pillars[i].OutputCount;
            if (pillars[i].PillarType == BeamPillarType.BEAM_PILLAR_START) startIndex = i;
        }
        _startIndex = startIndex;
        _segments = new BeamSegment[capacity];
    }

    /// <summary>현재 배치에서 광선 경로와 연결 상태를 다시 계산합니다.</summary>
    public bool TryRecalculate(out bool goalReached)
    {
        Array.Clear(_visited, 0, _visited.Length);
        Array.Clear(_connected, 0, _connected.Length);
        _segmentCount = 0;
        goalReached = false;
        if (_startIndex < 0) return false;
        int head = 0;
        int tail = 0;
        _queue[tail++] = _startIndex;
        _visited[_startIndex] = _connected[_startIndex] = true;
        while (head < tail)
        {
            int sourceIndex = _queue[head++];
            BeamPillarView source = _pillars[sourceIndex];
            for (int output = 0; output < source.OutputCount; output++)
            {
                float radians = source.GetOutputAngleDegrees(output) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                Vector2 origin = source.PillarCollider.bounds.center;
                int hitCount = Physics2D.Raycast(origin, direction, _filter, _hits, _maxDistance);
                float nearest = float.PositiveInfinity;
                Vector2 end = origin + direction * _maxDistance;
                int target = -1;
                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    RaycastHit2D hit = _hits[hitIndex];
                    Collider2D collider = hit.collider;
                    if (collider == null || hit.distance >= nearest) continue;
                    bool receiver = _receiverIndices.TryGetValue(collider, out int receiverIndex);
                    if (receiver && receiverIndex == sourceIndex) continue;
                    // 명시한 수신면과 별도인 몸체는 해당 수신면의 앞을 가리지 않습니다.
                    if (!receiver && _bodyIndices.ContainsKey(collider)) continue;
                    if (collider.isTrigger && !receiver) continue;
                    if (collider.GetComponentInParent<PlayerInteractionController>() != null
                        || collider.GetComponentInParent<UnitPlayer>() != null) continue;
                    nearest = hit.distance;
                    end = hit.point;
                    target = receiver ? receiverIndex : -1;
                }
                _segments[_segmentCount++] = new BeamSegment(origin, end);
                if (target < 0) continue;
                _connected[target] = true;
                if (_pillars[target].PillarType == BeamPillarType.BEAM_PILLAR_END)
                { goalReached = true; continue; }
                if (_visited[target]) continue;
                _visited[target] = true;
                _queue[tail++] = target;
            }
        }
        return true;
    }

    /// <summary>마지막 경로에서 해당 기둥이 연결됐는지 조회합니다.</summary>
    public bool IsConnected(int index) => index >= 0 && index < _connected.Length && _connected[index];
    /// <summary>Inspector 배열을 재정렬해도 같은 수신 부품의 연결 표시를 반환합니다.</summary>
    public bool IsConnected(BeamPillarView pillar) => pillar != null
        && _receiverIndices.TryGetValue(pillar.PillarCollider, out int index) && _connected[index];
    /// <summary>마지막 계산의 선분을 조회합니다.</summary>
    public BeamSegment GetSegment(int index) => _segments[index];
}
