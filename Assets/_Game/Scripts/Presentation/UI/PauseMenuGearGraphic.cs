//=============================================================================
// File: PauseMenuGearGraphic.cs
// Description: 크기에 맞춰 톱니바퀴 UI를 그린다.
// History: 10/16/2026 + 상수 명명 및 계산 정리 -- Codex
//=============================================================================
using UnityEngine;
using UnityEngine.UI;

/// <summary>래스터 아이콘 자산 없이 UI 크기에 맞춰 그리는 톱니바퀴 버튼입니다.</summary>
[AddComponentMenu("UI/Project Ruin/Pause Menu Gear")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PauseMenuGearGraphic : MaskableGraphic
{
    // 톱니 8개를 그릴 원 둘레 분할 수입니다.
    private const int SEGMENT_COUNT = 64;

    // Graphic이 변경될 때만 메쉬를 재구성합니다.
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.47f;
        float angleStep = Mathf.PI * 2 / SEGMENT_COUNT;
        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            float angle = i * angleStep;
            float outer = (i % 8 >= 2 && i % 8 <= 5) ? radius : radius * 0.78f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVert(center + direction * radius * 0.38f, color, Vector2.zero);
            mesh.AddVert(center + direction * outer, color, Vector2.zero);
        }
        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            int next = (i + 1) % SEGMENT_COUNT;
            mesh.AddTriangle(i * 2, next * 2, i * 2 + 1);
            mesh.AddTriangle(i * 2 + 1, next * 2, next * 2 + 1);
        }
    }
}
