//=============================================================================
// File:        MuseumPhase6SceneValidator.cs
// Description: 유물 전시대의 개수·고유 ID·표시 참조를 Scene 전체에서 검증한다.
// History:     08/30/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class MuseumPhase6SceneValidator
{
    private const int REQUIRED_PEDESTAL_COUNT = 20;

    internal static bool TryValidate(out int pedestalCount, out string message)
    {
        pedestalCount = 0;
        message = string.Empty;
        Dictionary<string, string> ids = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game" });
        for (int i = 0; i < guids.Length; i++)
            if (!TryInspect(AssetDatabase.GUIDToAssetPath(guids[i]), ids, out message)) return false;
        pedestalCount = ids.Count;
        if (ids.Count != 0 && ids.Count != REQUIRED_PEDESTAL_COUNT)
            return Fail($"전시대는 미배치 상태이거나 정확히 {REQUIRED_PEDESTAL_COUNT}개여야 합니다. actual={ids.Count}", out message);
        return true;
    }

    private static bool TryInspect(string path, Dictionary<string, string> ids,
        out string message)
    {
        Scene scene = default;
        message = string.Empty;
        try
        {
            scene = EditorSceneManager.OpenPreviewScene(path);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                MuseumRelicPedestal[] values =
                    roots[i].GetComponentsInChildren<MuseumRelicPedestal>(true);
                for (int j = 0; j < values.Length; j++)
                {
                    string id = values[j].PedestalId;
                    if (string.IsNullOrWhiteSpace(id))
                        return Fail($"{path}: Pedestal ID가 빈 컴포넌트가 있습니다.", out message);
                    if (ids.TryGetValue(id, out string previous))
                        return Fail($"pedestal_id='{id}' 중복: {previous}, {path}", out message);
                    SerializedProperty renderer = new SerializedObject(values[j])
                        .FindProperty("_displayRenderer");
                    if (renderer?.objectReferenceValue == null)
                        return Fail($"{path}: pedestal_id='{id}'의 표시 Renderer가 없습니다.", out message);
                    ids.Add(id, path);
                }
            }
            return true;
        }
        catch (Exception exception) { return Fail($"Scene 검사 실패: {path}\n{exception.Message}", out message); }
        finally { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static bool Fail(string value, out string message)
    { message = value; return false; }
}
