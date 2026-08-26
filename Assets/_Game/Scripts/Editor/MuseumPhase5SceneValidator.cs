//=============================================================================
// File:        MuseumPhase5SceneValidator.cs
// Description: 술병 CSV ID와 Scene 상호작용 컴포넌트를 1:1 검증한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class MuseumPhase5SceneValidator
{
    internal static bool TryValidate(IReadOnlyList<MuseumBottleDefinition> bottles,
        out string message)
    {
        message = string.Empty;
        if (bottles == null) return Fail("술병 정의가 없습니다.", out message);
        if (bottles.Count == 0) return true;
        HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < bottles.Count; i++) expected.Add(bottles[i].BottleId);
        Dictionary<string, string> actual = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game" });
        for (int i = 0; i < guids.Length; i++)
            if (!TryInspectScene(AssetDatabase.GUIDToAssetPath(guids[i]), actual, out message))
                return false;
        foreach (string id in expected)
            if (!actual.ContainsKey(id))
                return Fail($"bottle_id='{id}'에 대응하는 Scene 컴포넌트가 없습니다.", out message);
        foreach (string id in actual.Keys)
            if (!expected.Contains(id))
                return Fail($"Scene bottle_id='{id}'가 CSV에 없습니다.", out message);
        return true;
    }

    private static bool TryInspectScene(string path, Dictionary<string, string> actual,
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
                MuseumBottleInteractable[] values =
                    roots[i].GetComponentsInChildren<MuseumBottleInteractable>(true);
                for (int j = 0; j < values.Length; j++)
                {
                    string id = values[j].BottleId;
                    if (string.IsNullOrWhiteSpace(id))
                        return Fail($"{path}: Bottle ID가 빈 컴포넌트가 있습니다.", out message);
                    if (actual.TryGetValue(id, out string previous))
                        return Fail($"bottle_id='{id}'가 중복 배치됐습니다: {previous}, {path}", out message);
                    actual.Add(id, path);
                }
            }
            return true;
        }
        catch (Exception exception)
        {
            return Fail($"Scene 검사 실패: {path}\n{exception.Message}", out message);
        }
        finally
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static bool Fail(string value, out string message)
    {
        message = value;
        return false;
    }
}
