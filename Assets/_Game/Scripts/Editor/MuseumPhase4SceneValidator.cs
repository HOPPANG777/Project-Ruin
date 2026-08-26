//=============================================================================
// File:        MuseumPhase4SceneValidator.cs
// Description: 박물관 CSV ID, SceneCatalog, Scene 배치와 SpawnPoint를 교차 검증한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class MuseumPhase4SceneValidator
{
    internal static bool TryValidate(IReadOnlyList<MuseumMuralDefinition> murals,
        IReadOnlyList<AltarDestinationDefinition> altars, SceneCatalogSO catalog,
        out string message)
    {
        message = string.Empty;
        if (catalog == null)
            return Fail("SceneCatalog이 없습니다.", out message);
        if (murals.Count == 0 && altars.Count == 0)
            return true;

        HashSet<string> expectedMurals = CreateMuralIds(murals);
        HashSet<string> expectedAltars = CreateAltarIds(altars);
        Dictionary<string, string> muralLocations = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> altarLocations = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> spawnsByScene =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game" });
        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            if (!TryInspectScene(path, muralLocations, altarLocations,
                    spawnsByScene, out message))
                return false;
        }

        if (!ValidateBindings(expectedMurals, muralLocations, "mural_id", out message)
            || !ValidateBindings(expectedAltars, altarLocations, "altar_id", out message))
            return false;

        for (int i = 0; i < altars.Count; i++)
        {
            AltarDestinationDefinition altar = altars[i];
            if (!catalog.TryGetByStageNumber(altar.StageNumber, out SceneCatalogSO.SceneEntry entry))
                return Fail($"altar_id={altar.AltarId}: stage_number={altar.StageNumber}가 SceneCatalog에 없습니다.", out message);
            if (!spawnsByScene.TryGetValue(entry.SceneName, out HashSet<string> spawnIds))
                return Fail($"altar_id={altar.AltarId}: 목적지 Scene을 찾을 수 없습니다: {entry.SceneName}", out message);
            if (!spawnIds.Contains(altar.SpawnPointId))
                return Fail($"altar_id={altar.AltarId}: {entry.SceneName}에 SpawnPoint '{altar.SpawnPointId}'가 없습니다.", out message);
        }

        return true;
    }

    private static bool TryInspectScene(string path,
        Dictionary<string, string> muralLocations, Dictionary<string, string> altarLocations,
        Dictionary<string, HashSet<string>> spawnsByScene, out string message)
    {
        message = string.Empty;
        Scene scene = default;
        try
        {
            scene = EditorSceneManager.OpenPreviewScene(path);
            if (spawnsByScene.ContainsKey(scene.name))
                return Fail($"동일한 Scene 이름이 중복됩니다: {scene.name}", out message);

            HashSet<string> spawnIds = new HashSet<string>(StringComparer.Ordinal);
            spawnsByScene.Add(scene.name, spawnIds);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (!CollectIds(roots[i], path, muralLocations, altarLocations,
                        spawnIds, out message))
                    return false;
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

    private static bool CollectIds(GameObject root, string scenePath,
        Dictionary<string, string> muralLocations, Dictionary<string, string> altarLocations,
        HashSet<string> spawnIds, out string message)
    {
        MuseumMural[] murals = root.GetComponentsInChildren<MuseumMural>(true);
        for (int i = 0; i < murals.Length; i++)
        {
            string id = ReadId(murals[i], "_muralId");
            if (!AddUnique(muralLocations, id, scenePath, "mural_id", out message)) return false;
        }
        MuseumSceneTransitionPad[] altars =
            root.GetComponentsInChildren<MuseumSceneTransitionPad>(true);
        for (int i = 0; i < altars.Length; i++)
        {
            string id = ReadId(altars[i], "_altarId");
            if (!AddUnique(altarLocations, id, scenePath, "altar_id", out message)) return false;
        }
        SpawnPoint[] spawns = root.GetComponentsInChildren<SpawnPoint>(true);
        for (int i = 0; i < spawns.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(spawns[i].SpawnPointId))
                spawnIds.Add(spawns[i].SpawnPointId);
        }
        message = string.Empty;
        return true;
    }

    private static bool ValidateBindings(HashSet<string> expected,
        Dictionary<string, string> actual, string type, out string message)
    {
        foreach (string id in expected)
        {
            if (!actual.ContainsKey(id))
                return Fail($"CSV {type}='{id}'에 대응하는 Scene 컴포넌트가 없습니다.", out message);
        }
        foreach (string id in actual.Keys)
        {
            if (!expected.Contains(id))
                return Fail($"Scene {type}='{id}'가 CSV에 없습니다.", out message);
        }
        message = string.Empty;
        return true;
    }

    private static bool AddUnique(Dictionary<string, string> values, string id,
        string path, string type, out string message)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Fail($"{path}: 비어 있는 {type} 컴포넌트가 있습니다.", out message);
        if (values.TryGetValue(id, out string previous))
            return Fail($"{type}='{id}'가 중복 배치됐습니다: {previous}, {path}", out message);
        values.Add(id, path);
        message = string.Empty;
        return true;
    }

    private static string ReadId(UnityEngine.Object target, string fieldName) =>
        new SerializedObject(target).FindProperty(fieldName)?.stringValue ?? string.Empty;

    private static HashSet<string> CreateMuralIds(IReadOnlyList<MuseumMuralDefinition> values)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < values.Count; i++) ids.Add(values[i].MuralId);
        return ids;
    }

    private static HashSet<string> CreateAltarIds(IReadOnlyList<AltarDestinationDefinition> values)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < values.Count; i++) ids.Add(values[i].AltarId);
        return ids;
    }

    private static bool Fail(string error, out string message)
    {
        message = error;
        return false;
    }
}
