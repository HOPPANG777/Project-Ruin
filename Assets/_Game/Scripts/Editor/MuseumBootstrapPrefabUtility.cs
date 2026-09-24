//=============================================================================
// File:        MuseumBootstrapPrefabUtility.cs
//
// Description: Museum Integrator가 동일한 Nested Prefab 경계를 재사용하게 한다.
//
// History:     09/23/2026 + Museum Nested Prefab 재실행 보장 -- Codex
//
//=============================================================================

using UnityEditor;
using UnityEngine;

/// <summary>Museum Module 인스턴스를 중복 없이 조회하거나 생성하는 Editor 전용 도구입니다.</summary>
internal static class MuseumBootstrapPrefabUtility
{
    internal const string PREFAB_PATH = "Assets/_Game/Prefabs/Bootstrap/MuseumModule.prefab";

    /// <summary>기존 Museum Module의 컴포넌트를 조회하고 없을 때만 Nested Prefab을 생성합니다.</summary>
    internal static T GetOrCreateComponent<T>(GameObject gameRoot) where T : Component
    {
        if (gameRoot == null)
        {
            return null;
        }

        T existing = gameRoot.GetComponentInChildren<T>(true);
        if (existing != null)
        {
            return existing;
        }

        GameObject modulePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (modulePrefab == null)
        {
            Debug.LogError($"[MuseumBootstrapPrefabUtility] 프리팹을 찾을 수 없습니다. path={PREFAB_PATH}");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(
            modulePrefab, gameRoot.transform) as GameObject;
        return instance != null ? instance.GetComponent<T>() : null;
    }
}
