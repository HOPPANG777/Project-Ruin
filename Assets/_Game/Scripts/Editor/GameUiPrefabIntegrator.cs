//=============================================================================
// File:        GameUiPrefabIntegrator.cs
// Description: 생성된 인벤토리와 도감 UI를 공용 GameRoot 프리팹에 한 번만 연결한다.
// History:     08/23/2026 + Created -- Codex
//=============================================================================

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class GameUiPrefabIntegrator
{
    private const string GameRootPath = "Assets/_Game/Prefabs/GameRoot.prefab";
    private const string CodexPath = "Assets/_Game/Prefabs/UI/CodexBook.prefab";

    static GameUiPrefabIntegrator() => EditorApplication.delayCall += Schedule;

    private static void Schedule() => EditorApplication.delayCall += Integrate;

    [MenuItem("Tools/Project Ruin/Codex/Integrate Game UI Into GameRoot")]
    internal static void Integrate()
    {
        GameObject codexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CodexPath);
        GameObject inventoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            InventoryUiPrefabBuilder.InventoryPath);
        if (codexPrefab == null || inventoryPrefab == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(GameRootPath) == null) return;

        GameObject contents = PrefabUtility.LoadPrefabContents(GameRootPath);
        try
        {
            GameObject codex = FindChild(contents.transform, "CodexBook")
                ?? Instantiate(codexPrefab, contents.transform);
            GameObject inventory = FindChild(contents.transform, "InventoryBook")
                ?? Instantiate(inventoryPrefab, contents.transform);
            InventoryPresenter inventoryPresenter = inventory.GetComponent<InventoryPresenter>();
            InventoryNavigationBridge bridge = inventory.GetComponent<InventoryNavigationBridge>();
            CodexNavigationPresenter navigation = codex.GetComponent<CodexNavigationPresenter>();
            GameRoot gameRoot = contents.GetComponent<GameRoot>();
            CodexUiElementFactory.SetReference(gameRoot, "_inventoryPresenter", inventoryPresenter);
            CodexUiElementFactory.SetReference(navigation, "_inventoryPresenter", inventoryPresenter);
            CodexUiElementFactory.SetReference(navigation, "_inventoryBridge", bridge);
            CodexUiElementFactory.SetReference(navigation, "_inventoryRootCanvasGroup",
                bridge.RootCanvasGroup);
            CodexUiElementFactory.SetReference(navigation, "_inventoryCodexButton", bridge.CodexButton);
            PrefabUtility.SaveAsPrefabAsset(contents, GameRootPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static GameObject Instantiate(GameObject prefab, Transform parent)
        => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);

    private static GameObject FindChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child.gameObject;
        }
        return null;
    }
}
