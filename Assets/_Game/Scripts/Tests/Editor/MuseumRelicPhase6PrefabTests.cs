//=============================================================================
// File:        MuseumRelicPhase6PrefabTests.cs
// Description: Phase 6 전용 UI의 고정 슬롯·외부 이동 차단·Bazzi 회귀 테스트
// History:     08/30/2026 + Created -- Codex
//              08/31/2026 + 빈 테스트 씬에서 실제 PlayMode 생명주기 검증 -- Codex
//=============================================================================
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public sealed class MuseumRelicPhase6PrefabTests
{
    private const string UI_PATH = "Assets/_Game/Prefabs/UI/MuseumRelicUi.prefab";
    private const string BAZZI_PATH = "Assets/TextMesh Pro/Fonts/Bazzi SDF.asset";

    //=============================================================================
    // Description: 편집 중 씬·저장 데이터와 분리한 상태에서 실제 생명주기를 활성화합니다.
    // Parameters: None.
    // Return: IEnumerator — Unity Test Framework의 PlayMode 전환 지시.
    //=============================================================================
    /// <summary>테스트 전용 빈 씬에서 실제 OnDisable/OnDestroy 호출을 검증합니다.</summary>
    [UnitySetUp]
    public IEnumerator EnterRuntime()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    //=============================================================================
    // Description: 테스트가 끝나면 편집 모드로 돌아갑니다. 런타임 코루틴은 추가하지 않습니다.
    // Parameters: None.
    // Return: IEnumerator — Unity Test Framework의 EditMode 복귀 지시.
    //=============================================================================
    /// <summary>성공·실패와 무관하게 테스트의 PlayMode를 종료합니다.</summary>
    [UnityTearDown]
    public IEnumerator ExitRuntime()
    {
        yield return new ExitPlayMode();
    }

    [Test]
    public void DedicatedUiHasTwentySlotsBazziAndNoCodexOrMapNavigation()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI_PATH);
        TMP_FontAsset bazzi = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BAZZI_PATH);
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<MuseumRelicItemButton>(true).Length,
            Is.EqualTo(20));
        TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++) Assert.That(texts[i].font, Is.SameAs(bazzi));
        Assert.That(FindChild(prefab.transform, "Codex"), Is.Null);
        Assert.That(FindChild(prefab.transform, "Map"), Is.Null);
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            MuseumRelicPresenter presenter = instance.GetComponent<MuseumRelicPresenter>();
            FindChild(instance.transform, "Panel").gameObject.SetActive(true);
            Assert.That(presenter.BlocksExternalNavigation, Is.True);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DedicatedUiBlocksInventoryAndRestoresPlayerInput(bool reinitialize)
    {
        Assert.That(Application.isPlaying, Is.True, "입력 잠금 해제는 실제 PlayMode에서 검증해야 합니다.");
        GameObject ui = Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>(UI_PATH));
        GameObject inventoryObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/UI/InventoryBook.prefab"));
        GameObject codexObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/UI/CodexBook.prefab"));
        GameObject player = new GameObject("Player");
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            InventoryPresenter inventoryPresenter =
                inventoryObject.GetComponent<InventoryPresenter>();
            MuseumBottleTestInventory inventory = new MuseumBottleTestInventory();
            inventoryPresenter.Init(inventory);
            PlayerMoveController move = player.AddComponent<PlayerMoveController>();
            PlayerInteractionController interaction =
                player.AddComponent<PlayerInteractionController>();
            MuseumRelicPresenter presenter = ui.GetComponent<MuseumRelicPresenter>();
            presenter.Init(new MuseumRelicTestDisplayService(), inventory, inventoryPresenter);
            CodexPresenter codex = codexObject.GetComponent<CodexPresenter>();
            typeof(CodexPresenter).GetField("_codex", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(codex, new Phase3CodexServiceStub());
            codexObject.GetComponent<CodexView>().Init();
            CodexNavigationPresenter navigation = codexObject.GetComponent<CodexNavigationPresenter>();
            InventoryNavigationBridge bridge = inventoryObject.GetComponent<InventoryNavigationBridge>();
            Assert.That(navigation.Init(inventoryPresenter, bridge.RootCanvasGroup, bridge.CodexButton), Is.True);

            presenter.Open("P01", interaction, move);

            Assert.That(inventoryPresenter.IsExternalInputBlocked, Is.True);
            inventoryPresenter.Open();
            Assert.That(inventoryPresenter.IsOpen, Is.False);
            Assert.That(interaction.CanInteract, Is.False);
            Assert.That(move.CanMove, Is.False);
            navigation.OpenCodex();
            navigation.OpenInventory();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab, Key.CapsLock));
            InputSystem.Update();
            typeof(InventoryPresenter).GetMethod("HandleKeyboardInput", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(inventoryPresenter, null);
            typeof(CodexNavigationPresenter).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(navigation, null);
            Assert.That(inventoryPresenter.IsOpen, Is.False);
            Assert.That(codex.IsOpen, Is.False);
            Assert.That(move.CanMove, Is.False);

            ui.GetComponent<MuseumRelicSelectionView>().SetInputEnabled(false);
            if (reinitialize) presenter.Init(new MuseumRelicTestDisplayService(), inventory, inventoryPresenter);
            else ui.SetActive(false);
            Assert.That(inventoryPresenter.IsExternalInputBlocked, Is.False);
            Assert.That(interaction.CanInteract, Is.True);
            Assert.That(move.CanMove, Is.True);
            inventoryPresenter.Open();
            Assert.That(inventoryPresenter.IsOpen, Is.True);
            inventoryPresenter.Close();
            ui.SetActive(true);
            presenter.Open("P01", interaction, move);
            var close = FindChild(ui.transform, "Close").GetComponent<UnityEngine.UI.Button>();
            Assert.That(close.interactable, Is.True);
            close.onClick.Invoke();
            Assert.That(presenter.IsOpen, Is.False);
            Assert.That(move.CanMove, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(ui);
            Object.DestroyImmediate(inventoryObject);
            Object.DestroyImmediate(codexObject);
            Object.DestroyImmediate(player);
            InputSystem.RemoveDevice(keyboard);
        }
    }

    private static Transform FindChild(Transform root, string name)
    {
        Transform[] values = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < values.Length; i++) if (values[i].name == name) return values[i];
        return null;
    }
}
