//=============================================================================
// File:        Ch2BeamPuzzles.cs
// Description: 챕터2 빛잇기 퍼즐을 기존 Beam 부품(회전 기둥)으로 배치합니다.
// History:     11/28/2026 + Created
//=============================================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>PDF의 조각상 위치를 기둥으로 옮긴다. 조각상 이동 기믹은 기존 Beam이 지원하지 않아 참고 표시만 둔다.</summary>
public static class Ch2BeamPuzzles
{
    private const string Dir = "Assets/_Game/Prefabs/Puzzle/Beam/";

    /// <summary>빛잇기 하나의 PDF 좌표 데이터입니다. 좌표는 PDF 표시 좌표(px)입니다.</summary>
    public struct Spec
    {
        public string Key, Label, Id;
        public Vector4 Frame;
        public Vector2 Origin;
        public float Scale;
        public Vector2 Light;
        public Vector2[] Nodes;        // 연결 대상 조각상(옮길 위치 포함)
        public Vector2[] MovedFrom;    // 움직여야 하는 조각상 시작 위치(참고용)
        public Vector2 DoorAt, DoorSize;
        public Vector2? Reward;
    }

    public static void Build(Transform group, Spec spec)
    {
        Vector2 center = new Vector2((spec.Frame.x + spec.Frame.z) / 2f, (spec.Frame.y + spec.Frame.w) / 2f);
        System.Func<Vector2, Vector2> map = p => spec.Origin + new Vector2((p.x - center.x) * spec.Scale, -(p.y - center.y) * spec.Scale);
        Vector2 frameSize = new Vector2((spec.Frame.z - spec.Frame.x) * spec.Scale, (spec.Frame.w - spec.Frame.y) * spec.Scale);
        GameObject root = Ch2Util.NewChild(group, spec.Key + "_" + spec.Label, Vector2.zero);
        GameObject guide = Ch2Util.Block(root.transform, "퍼즐영역_가이드(임시)", spec.Origin, frameSize,
            new Color(1f, 1f, 1f, 0.12f), false);
        guide.GetComponent<SpriteRenderer>().sortingOrder = -1;

        Vector2[] points = new Vector2[spec.Nodes.Length + 1];
        points[0] = map(spec.Light);
        for (int i = 0; i < spec.Nodes.Length; i++) points[i + 1] = map(spec.Nodes[i]);
        List<Vector2> route = BuildRoute(points, spec.Id);
        int count = route.Count;
        BeamPillarView[] pillars = new BeamPillarView[count];
        float maxStep = 0f;
        for (int i = 0; i < count; i++)
        {
            Vector2 at = route[i];
            bool isStart = i == 0, isEnd = i == count - 1;
            string prefab = isStart ? "Beam_시작 기둥.prefab" : isEnd ? "Beam_끝 기둥.prefab" : "Beam_2방향 기둥.prefab";
            string id = isStart ? "START" : isEnd ? "END" : $"P{i:00}";
            GameObject pillar = Ch2Util.Spawn(Dir + prefab, root.transform, "기둥_" + id, at);
            pillars[i] = pillar.GetComponent<BeamPillarView>();
            Ch2Util.SetString(pillars[i], "_settings._id", id);
            if (!isEnd)
            {
                Vector2 delta = route[i + 1] - at;
                maxStep = Mathf.Max(maxStep, delta.magnitude);
                SerializedSet(pillars[i], isStart ? 0f : 45f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
            Ch2Util.Record(group.name, $"{spec.Key}_기둥{id}", at, Vector2.one * 2f);
        }

        GameObject door = Ch2Util.Block(root.transform, "출구문_" + spec.Key, spec.DoorAt, spec.DoorSize,
            new Color(0.9f, 0.1f, 0.1f), true);
        Vector2 padAt = map(new Vector2(spec.Frame.x + 30f, spec.Frame.w - 30f));
        GameObject pad = Ch2Util.Spawn(Dir + "Beam_초기화 발판.prefab", root.transform, "초기화발판", padAt);
        Ch2Util.Record(group.name, spec.Key + "_초기화발판", padAt, Vector2.one * 3f);

        GameObject manager = Ch2Util.Spawn(Dir + "Beam_관리자.prefab", root.transform, "관리자_" + spec.Id, spec.Origin);
        BeamPuzzleController beam = manager.GetComponent<BeamPuzzleController>();
        Ch2Util.SetString(beam, "_objectId", spec.Id);
        Ch2Util.SetRefs(beam, "_pillars", pillars);
        Ch2Util.SetRefs(beam, "_resetPads", new Object[] { pad.GetComponent<BeamResetPad>() });
        Ch2Util.SetRefs(beam, "_exitObstacles", new Object[] { door });
        Ch2Util.SetFloat(beam, "_maxBeamDistance", Mathf.Ceil(maxStep) + 8f);
        WorldPuzzleSceneState state = manager.AddComponent<WorldPuzzleSceneState>();
        Ch2Util.SetRef(state, "_source", beam);
        Ch2Util.SetInt(state, "_stageNumber", Ch2Util.StageNumber);

        for (int i = 0; i < spec.MovedFrom.Length; i++)
        {
            Ch2Util.Block(root.transform, $"[참고_기믹미지원]움직이는조각상_{i + 1}", map(spec.MovedFrom[i]),
                new Vector2(2f, 2f), new Color(0.9f, 0.3f, 0.1f, 0.8f), false);
        }
        if (spec.Reward.HasValue)
        {
            Ch2Util.Block(root.transform, "보상표시_" + spec.Key, spec.Reward.Value, new Vector2(2f, 2f),
                new Color(0.25f, 0.55f, 0.2f), false, 45f);
        }
    }

    // 시작 기둥 설정: 초기 회전과 출력 각도(다음 기둥 방향의 절대 각도). 중간 기둥은 입력 1번으로 0도가 되어 정답이 된다.
    private static void SerializedSet(BeamPillarView pillar, float initialRotation, float outputAngle)
    {
        UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(pillar);
        so.FindProperty("_settings._initialRotationDegrees").floatValue = initialRotation;
        UnityEditor.SerializedProperty outputs = so.FindProperty("_settings._outputAnglesDegrees");
        outputs.arraySize = 1;
        outputs.GetArrayElementAtIndex(0).floatValue = outputAngle;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private const float MinClearance = 2.5f;
    private const float MaxStep = 26f;

    // 모든 조각상을 한 번씩 지나는 가장 짧은 경로를 찾는다. 어떤 구간도 다른 기둥에 가려지면 안 된다.
    // points[0]은 빛(시작 기둥)이다. 해가 없으면 배치를 중단한다.
    private static List<Vector2> BuildRoute(Vector2[] points, string id)
    {
        int n = points.Length;
        bool[,] open = new bool[n, n];
        for (int a = 0; a < n; a++)
        {
            for (int b = 0; b < n; b++) open[a, b] = a != b && IsClear(points, a, b);
        }
        int[] best = null;
        float bestLength = float.MaxValue;
        int[] path = new int[n];
        bool[] used = new bool[n];
        path[0] = 0;
        used[0] = true;
        Search(points, open, path, used, 1, 0f, ref best, ref bestLength);
        if (best == null) throw new System.InvalidOperationException($"{id}: 다른 기둥을 가리지 않는 광선 경로가 없습니다.");
        List<Vector2> route = new List<Vector2>(n);
        for (int i = 0; i < n; i++) route.Add(points[best[i]]);
        return route;
    }

    private static void Search(Vector2[] p, bool[,] open, int[] path, bool[] used, int depth, float length,
        ref int[] best, ref float bestLength)
    {
        if (length >= bestLength) return;
        if (depth == p.Length)
        {
            if (Ch2BeamRouteCheck.NoShortcut(p, path, MaxStep + 8f)) { best = (int[])path.Clone(); bestLength = length; }
            return;
        }
        for (int next = 0; next < p.Length; next++)
        {
            if (used[next] || !open[path[depth - 1], next]) continue;
            used[next] = true;
            path[depth] = next;
            Search(p, open, path, used, depth + 1, length + Vector2.Distance(p[path[depth - 1]], p[next]), ref best, ref bestLength);
            used[next] = false;
        }
    }

    private static bool IsClear(Vector2[] p, int from, int to)
    {
        if (Vector2.Distance(p[from], p[to]) > MaxStep) return false;
        Vector2 ab = p[to] - p[from];
        for (int k = 0; k < p.Length; k++)
        {
            if (k == from || k == to) continue;
            float t = Mathf.Clamp01(Vector2.Dot(p[k] - p[from], ab) / ab.sqrMagnitude);
            if (Vector2.Distance(p[k], p[from] + ab * t) <= MinClearance) return false;
        }
        return true;
    }
}
