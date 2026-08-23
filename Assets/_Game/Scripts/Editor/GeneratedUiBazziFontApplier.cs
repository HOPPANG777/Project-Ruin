using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class GeneratedUiBazziFontApplier
{
    private const string FontPath = "Assets/TextMesh Pro/Fonts/Bazzi SDF.asset";
    private const string PrefabRoot = "Assets/_Game/Prefabs/";
    private static readonly HashSet<string> pendingPaths =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static bool isScheduled;

    static GeneratedUiBazziFontApplier()
    {
        ScheduleAll();
    }

    [MenuItem("Tools/Project Ruin/UI/Replace Prefab LiberationSans With Bazzi")]
    internal static void ScheduleAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            pendingPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
        Schedule();
    }

    internal static void ScheduleImported(IEnumerable<string> importedAssets)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.StartsWith(PrefabRoot, StringComparison.OrdinalIgnoreCase)
                && assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                pendingPaths.Add(assetPath);
        }
        Schedule();
    }

    private static void Schedule()
    {
        if (isScheduled || pendingPaths.Count == 0)
            return;

        isScheduled = true;
        EditorApplication.delayCall += ApplyPending;
    }

    private static void ApplyPending()
    {
        isScheduled = false;
        TMP_FontAsset bazziFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (bazziFont == null)
        {
            Debug.LogError($"[Project Ruin UI] TMP font asset was not found: {FontPath}");
            pendingPaths.Clear();
            return;
        }

        string[] paths = new string[pendingPaths.Count];
        pendingPaths.CopyTo(paths);
        pendingPaths.Clear();

        foreach (string prefabPath in paths)
            ApplyToPrefab(prefabPath, bazziFont);
    }

    private static void ApplyToPrefab(string prefabPath, TMP_FontAsset bazziFont)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            return;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            bool changed = false;
            foreach (TMP_Text text in prefabRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                TMP_FontAsset currentFont = text.font;
                if (currentFont == null
                    || !currentFont.name.StartsWith("LiberationSans", StringComparison.OrdinalIgnoreCase))
                    continue;

                text.font = bazziFont;
                EditorUtility.SetDirty(text);
                changed = true;
            }

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}

internal sealed class GeneratedUiBazziFontPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        GeneratedUiBazziFontApplier.ScheduleImported(importedAssets);
        GeneratedUiBazziFontApplier.ScheduleImported(movedAssets);
    }
}
