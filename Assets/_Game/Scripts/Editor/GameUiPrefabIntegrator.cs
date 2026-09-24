//=============================================================================
// File:        GameUiPrefabIntegrator.cs
// Description: 생성된 인벤토리와 도감 UI를 공용 GameRoot 프리팹에 한 번만 연결한다.
// History:     08/23/2026 + Created -- Codex
//              09/01/2026 + GameRoot 도감 참조 연결 및 변경 없는 프리팹 저장 생략 -- Codex
//              09/23/2026 + UI Bootstrap Module 직렬화 경로 반영 -- Codex
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
        if (IsConnected(AssetDatabase.LoadAssetAtPath<GameObject>(GameRootPath))) return;

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
            CodexUiElementFactory.SetReference(
                gameRoot, "_uiModule._inventoryPresenter", inventoryPresenter);
            CodexUiElementFactory.SetReference(
                gameRoot, "_uiModule._codexNavigationPresenter", navigation);
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

    // 이미 연결된 프리팹은 컴파일·테스트마다 재저장하지 않습니다.
    internal static bool IsConnected(GameObject prefab)
    {
        var root = prefab.GetComponent<GameRoot>();
        var inventory = prefab.GetComponentInChildren<InventoryNavigationBridge>(true);
        var navigation = prefab.GetComponentInChildren<CodexNavigationPresenter>(true);
        if (root == null || inventory == null || navigation == null) return false;
        return Matches(root, "_uiModule._inventoryPresenter", inventory.Presenter)
            && Matches(root, "_uiModule._codexNavigationPresenter", navigation)
            && Matches(navigation, "_inventoryPresenter", inventory.Presenter)
            && Matches(navigation, "_inventoryBridge", inventory)
            && Matches(navigation, "_inventoryRootCanvasGroup", inventory.RootCanvasGroup)
            && Matches(navigation, "_inventoryCodexButton", inventory.CodexButton);
    }

    private static bool Matches(Object target, string field, Object expected) =>
        expected != null && new SerializedObject(target).FindProperty(field)?.objectReferenceValue == expected;

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
