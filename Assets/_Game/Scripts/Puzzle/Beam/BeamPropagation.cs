//=============================================================================
// File:        BeamPropagation.cs
//
// Description: Physics2D 광선과 고정 큐로 빛줄기 연결 경로를 계산한다.
//
// History:     08/04/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using UnityEngine;

//=============================================================================
//
// Synopsis:   BeamSegment;
//                - 한 출력 광선의 월드 시작점과 최근접 종료점
//=============================================================================
public readonly struct BeamSegment
{
    /// <summary>기둥 표면의 빛 시작점입니다.</summary>
    public Vector2 Start { get; }
    /// <summary>최근접 충돌 또는 최대 거리의 빛 종료점입니다.</summary>
    public Vector2 End { get; }

    /// <summary>재사용 경로 결과에 저장할 빛 선분을 생성합니다.</summary>
    public BeamSegment(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }
}

//=============================================================================
//
// Synopsis:   BeamPropagation;
//                - 기둥당 출력을 한 번만 처리하는 반복형 그래프 탐색
//                - 큐, 방문 상태와 선분 결과를 계산마다 재사용
//=============================================================================
public sealed class BeamPropagation
{
    private const float RAY_ORIGIN_EPSILON = 0.001f;

    private readonly BeamPillarView[] _pillars;
    private readonly Dictionary<Collider2D, int> _pillarIndices;
    private readonly int[] _queue;
    private readonly bool[] _visited;
    private readonly bool[] _connected;
    private readonly BeamSegment[] _segments;
    private readonly int _hitLayerMask;
    private readonly float _maxDistance;
    private readonly int _startIndex;

    private int _segmentCount;

    /// <summary>마지막 계산에서 생성된 유효 선분 수입니다.</summary>
    public int SegmentCount => _segmentCount;
    /// <summary>초기화 때 고정된 최대 선분 수입니다.</summary>
    public int SegmentCapacity => _segments.Length;

    /// <summary>검증된 기둥과 충돌 설정으로 재사용 전파 상태를 생성합니다.</summary>
    public BeamPropagation(BeamPillarView[] pillars, LayerMask pillarLayerMask,
        LayerMask obstacleLayerMask, float maxDistance)
    {
        _pillars = pillars;
        _queue = new int[pillars.Length];
        _visited = new bool[pillars.Length];
        _connected = new bool[pillars.Length];
        _pillarIndices = new Dictionary<Collider2D, int>(pillars.Length);
        _hitLayerMask = pillarLayerMask.value | obstacleLayerMask.value;
        _maxDistance = maxDistance;
        int startIndex = -1;
        int segmentCapacity = 0;
        for (int i = 0; i < pillars.Length; i++)
        {
            _pillarIndices.Add(pillars[i].PillarCollider, i);
            segmentCapacity += pillars[i].OutputCount;
            if (pillars[i].PillarType == BeamPillarType.BEAM_PILLAR_START)
            {
                startIndex = i;
            }
        }
        _startIndex = startIndex;
        _segments = new BeamSegment[segmentCapacity];
    }

    /// <summary>현재 기둥 회전으로 전체 경로를 다시 계산합니다.</summary>
    public bool TryRecalculate(out bool goalReached)
    {
        Array.Clear(_visited, 0, _visited.Length);
        Array.Clear(_connected, 0, _connected.Length);
        _segmentCount = 0;
        goalReached = false;
        if (_startIndex < 0)
        {
            return false;
        }

        int head = 0;
        int tail = 0;
        _queue[tail++] = _startIndex;
        _visited[_startIndex] = true;
        _connected[_startIndex] = true;
        while (head < tail)
        {
            int sourceIndex = _queue[head++];
            BeamPillarView source = _pillars[sourceIndex];
            for (int outputIndex = 0; outputIndex < source.OutputCount; outputIndex++)
            {
                if (!TryTraceOutput(sourceIndex, source, outputIndex,
                    ref tail, ref goalReached))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>마지막 계산에서 해당 기둥에 빛이 닿았는지 반환합니다.</summary>
    public bool IsConnected(int pillarIndex)
    {
        return pillarIndex >= 0 && pillarIndex < _connected.Length
            && _connected[pillarIndex];
    }

    /// <summary>마지막 계산의 재사용 선분을 인덱스로 반환합니다.</summary>
    public BeamSegment GetSegment(int segmentIndex)
    {
        return _segments[segmentIndex];
    }

    private bool TryTraceOutput(int sourceIndex, BeamPillarView source, int outputIndex,
        ref int tail, ref bool goalReached)
    {
        float radians = source.GetOutputAngleDegrees(outputIndex) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        Vector2 center = source.transform.position;
        Vector2 surface = source.PillarCollider.ClosestPoint(center + direction * _maxDistance);
        Vector2 rayOrigin = surface + direction * RAY_ORIGIN_EPSILON;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, _maxDistance, _hitLayerMask);
        Vector2 end = hit.collider == null ? surface + direction * _maxDistance : hit.point;
        if (_segmentCount >= _segments.Length)
        {
            return false;
        }
        _segments[_segmentCount++] = new BeamSegment(surface, end);
        if (hit.collider == null || !_pillarIndices.TryGetValue(hit.collider, out int targetIndex))
        {
            return true;
        }
        if (targetIndex == sourceIndex)
        {
            return false;
        }

        _connected[targetIndex] = true;
        BeamPillarView target = _pillars[targetIndex];
        if (target.PillarType == BeamPillarType.BEAM_PILLAR_END)
        {
            goalReached = true;
            return true;
        }
        if (!_visited[targetIndex])
        {
            _visited[targetIndex] = true;
            _queue[tail++] = targetIndex;
        }
        return true;
    }
}
