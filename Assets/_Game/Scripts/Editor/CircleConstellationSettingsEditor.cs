//=============================================================================
// File:        CircleConstellationSettingsEditor.cs
// Description: 바깥→안쪽 링 편집과 설정 오류를 한글 Inspector에 표시합니다.
// History:     10/07/2026 + Created -- Codex
//=============================================================================
using UnityEditor;
using UnityEngine;

/// <summary>기존 프리팹에서 문양·Image·Transform을 편집하도록 안내합니다.</summary>
[CustomEditor(typeof(CircleConstellationSettingsSO))]
public sealed class CircleConstellationSettingsEditor : Editor
{
    /// <summary>각 링의 실제 수정 순서와 필수 참조 오류를 보여줍니다.</summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("1. 바깥→안쪽 링을 선택합니다.\n2. 문양 프리팹을 열고 CirclePattern 목록의 ID·개수를 편집합니다.\n3. 각 Image의 기존 그림과 RectTransform 위치·각도를 편집합니다.\n4. 이곳에서 정답 ID·방향·초기 회전·회전 간격을 설정합니다.\nCSV ring_order는 안쪽부터이며 자동으로 대응됩니다.", MessageType.Info);
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
        var settings = (CircleConstellationSettingsSO)target;
        if (settings.OuterToInner == null || settings.OuterToInner.Count == 0)
            EditorGUILayout.HelpBox("링 설정이 없습니다.", MessageType.Error);
        else for (int i = 0; i < settings.OuterToInner.Count; i++)
        {
            CircleConstellationSettingsSO.Ring ring = settings.OuterToInner[i];
            if (ring == null) EditorGUILayout.HelpBox($"바깥에서 {i + 1}번째 링 설정이 없습니다.", MessageType.Error);
            else if (!ring.TryValidate(out string error))
                EditorGUILayout.HelpBox($"바깥에서 {i + 1}번째 링: {error}", MessageType.Error);
        }
        EditorGUILayout.HelpBox("실제 Anchor·링 크기에서 정답 방향에 스냅 가능한지는 UI를 열 때 검사합니다. 오류 시 Console에 표시되며 클리어할 수 없습니다.", MessageType.Info);
    }
}
