//=============================================================================
// File:        Ch2BeamRouteCheck.cs
// Description: 빛잇기 경로가 "전부 눌러야만 클리어"인지 눌림 조합 전체로 시뮬레이션합니다.
// History:     11/28/2026 + Created
//=============================================================================
using UnityEngine;

/// <summary>빌더 전용. 기존 BeamPropagation과 같은 규칙(가장 가까운 수신 기둥만 맞힘)을 기하로 재현한다.</summary>
public static class Ch2BeamRouteCheck
{
    private const float HitRadius = 0.6f;     // 기둥 몸체 반경 0.4 + 여유

    // 중간 기둥을 누른/안 누른 모든 조합에서 끝 기둥은 "전부 눌렀을 때만" 닿아야 한다.
    // 안 돌린 기둥은 45도 틀어져 쏘므로, 우연히 다른 기둥(특히 끝 기둥)을 맞히는 지름길을 걸러낸다.
    public static bool NoShortcut(Vector2[] p, int[] route, float maxDistance)
    {
        int middle = route.Length - 2;
        for (int mask = 0; mask < (1 << middle); mask++)
        {
            bool reached = Reaches(p, route, mask, maxDistance);
            if (reached != (mask == (1 << middle) - 1)) return false;
        }
        return true;
    }

    private static bool Reaches(Vector2[] p, int[] route, int pressedMask, float maxDistance)
    {
        int n = route.Length;
        bool[] seen = new bool[n];
        int[] stack = new int[n];
        int top = 0;
        stack[top++] = 0;
        seen[0] = true;
        while (top > 0)
        {
            int k = stack[--top];
            if (k == n - 1) continue;
            Vector2 delta = p[route[k + 1]] - p[route[k]];
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            bool pressed = k == 0 || (pressedMask >> (k - 1) & 1) == 1;
            float radians = (pressed ? angle : angle + 45f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            int hit = -1;
            float nearest = float.MaxValue;
            for (int other = 0; other < n; other++)
            {
                if (other == k) continue;
                Vector2 to = p[route[other]] - p[route[k]];
                float along = Vector2.Dot(to, direction);
                float across = Mathf.Abs(to.x * direction.y - to.y * direction.x);
                if (along > 0f && along <= maxDistance && across <= HitRadius && along < nearest) { nearest = along; hit = other; }
            }
            if (hit >= 0 && !seen[hit]) { seen[hit] = true; stack[top++] = hit; }
        }
        return seen[n - 1];
    }
}
