using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 씬의 UI는 편집 중에만 렌더링을 숨기고, 프리팹 편집과 플레이는 원래 상태를 사용한다.
// CanvasRenderer의 임시 알파를 사용하므로 씬/프리팹의 활성화 값은 변경하지 않는다.
[InitializeOnLoad]
internal static class SceneUiEditModeVisibility
{
    private static readonly Dictionary<CanvasRenderer, float> OriginalAlphas =
        new Dictionary<CanvasRenderer, float>();
    private static readonly List<CanvasRenderer> ReleasedRenderers = new List<CanvasRenderer>();
    private static bool _isRefreshing;

    static SceneUiEditModeVisibility()
    {
        EditorApplication.update += RefreshVisibility;
        Canvas.willRenderCanvases += RefreshVisibility;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += RestoreVisibility;
        EditorApplication.quitting += RestoreVisibility;
    }

    internal static void RefreshVisibility()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestoreVisibility();
                return;
            }

            ReleasedRenderers.Clear();
            foreach (KeyValuePair<CanvasRenderer, float> entry in OriginalAlphas)
            {
                if (ShouldHide(entry.Key)) continue;
                if (entry.Key != null) entry.Key.SetAlpha(entry.Value);
                ReleasedRenderers.Add(entry.Key);
            }
            foreach (CanvasRenderer renderer in ReleasedRenderers) OriginalAlphas.Remove(renderer);

            CanvasRenderer[] renderers = Object.FindObjectsByType<CanvasRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CanvasRenderer renderer in renderers)
            {
                if (!ShouldHide(renderer)) continue;

                float alpha = renderer.GetAlpha();
                if (Mathf.Approximately(alpha, 0f)) continue;
                if (!OriginalAlphas.ContainsKey(renderer)) OriginalAlphas.Add(renderer, alpha);
                renderer.SetAlpha(0f);
            }
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    internal static bool ShouldHide(CanvasRenderer renderer)
    {
        if (renderer == null || EditorUtility.IsPersistent(renderer)) return false;

        var scene = renderer.gameObject.scene;
        return scene.IsValid()
            && scene.isLoaded
            && !EditorSceneManager.IsPreviewScene(scene)
            && PrefabStageUtility.GetPrefabStage(renderer.gameObject) == null;
    }

    internal static void RestoreVisibility()
    {
        foreach (KeyValuePair<CanvasRenderer, float> entry in OriginalAlphas)
        {
            if (entry.Key != null) entry.Key.SetAlpha(entry.Value);
        }
        OriginalAlphas.Clear();
        ReleasedRenderers.Clear();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode
            || state == PlayModeStateChange.EnteredPlayMode)
        {
            RestoreVisibility();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += RefreshVisibility;
        }
    }
}
