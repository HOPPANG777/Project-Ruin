//=============================================================================
// File:        CodexNavigationPhase3Tests.cs
// Description: 현재 프리팹을 변경하지 않고 도감·인벤토리 전환 및 입력 잠금을 검증
// History:     08/31/2026 + 재생성 없는 읽기 전용 회귀 검사로 변경 -- Codex
//=============================================================================
using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.TestTools;

[TestFixture]
public sealed class CodexNavigationPhase3Tests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string BookPath = "Assets/_Game/Prefabs/UI/CodexBook.prefab";
    private const string InventoryPath = "Assets/_Game/Prefabs/UI/InventoryBook.prefab";
    private const string GameRootPath = "Assets/_Game/Prefabs/GameRoot.prefab";

    private GameObject _codexRoot;
    private GameObject _inventoryRoot;
    private GameObject _playerRoot;
    private CodexPresenter _codex;
    private CodexNavigationPresenter _navigation;
    private InventoryPresenter _inventory;
    private InventoryNavigationBridge _bridge;
    private PlayerInteractionController _interaction;
    private PlayerMoveController _movement;
    private Button _hudButton;
    private Button _closeButton;
    private Button _inventoryButton;
    private Button _mapButton;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        _codexRoot = CreateInstance(BookPath);
        _inventoryRoot = CreateInstance(InventoryPath);
        _codex = _codexRoot.GetComponent<CodexPresenter>();
        _navigation = _codexRoot.GetComponent<CodexNavigationPresenter>();
        _inventory = _inventoryRoot.GetComponent<InventoryPresenter>();
        _bridge = _inventoryRoot.GetComponent<InventoryNavigationBridge>();
        Assert.That(_codex, Is.Not.Null);
        Assert.That(_navigation, Is.Not.Null);
        Assert.That(_inventory, Is.Not.Null);
        Assert.That(_bridge, Is.Not.Null);

        SetField(_codex, "_codex", new Phase3CodexServiceStub());
        _codexRoot.GetComponent<CodexView>().Init();
        _inventory.Init(new Phase3InventoryServiceStub());
        _playerRoot = new GameObject("Phase3TestPlayer");
        _interaction = _playerRoot.AddComponent<PlayerInteractionController>();
        _movement = _playerRoot.AddComponent<PlayerMoveController>();
        SetField(_navigation, "_playerInteractionController", _interaction);
        SetField(_navigation, "_playerMoveController", _movement);
        _navigation.Init(_inventory, _bridge.RootCanvasGroup, _bridge.CodexButton);
        _hudButton = Find(_codexRoot, "CodexHudButton").GetComponent<Button>();
        _closeButton = Find(_codexRoot, "BookRoot/CloseButton").GetComponent<Button>();
        _inventoryButton = Find(_codexRoot, "BookRoot/InventoryButton").GetComponent<Button>();
        _mapButton = Find(_codexRoot, "BookRoot/MapButton").GetComponent<Button>();
        ReadyNextTransition();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_codexRoot != null) Object.DestroyImmediate(_codexRoot);
        if (_inventoryRoot != null) Object.DestroyImmediate(_inventoryRoot);
        if (_playerRoot != null) Object.DestroyImmediate(_playerRoot);
        yield return new ExitPlayMode();
    }

    [Test]
    public void GeneratedPrefabsAndGameRootAreWired()
    {
        GameObject book = AssetDatabase.LoadAssetAtPath<GameObject>(BookPath);
        GameObject inventory = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPath);
        GameObject gameRoot = AssetDatabase.LoadAssetAtPath<GameObject>(GameRootPath);
        Assert.That(book.GetComponent<CodexNavigationPresenter>(), Is.Not.Null);
        Assert.That(inventory.GetComponent<InventoryNavigationBridge>(), Is.Not.Null);
        Assert.That(gameRoot.transform.Find("CodexBook"), Is.Not.Null);
        Assert.That(gameRoot.transform.Find("InventoryBook"), Is.Not.Null);
        SerializedObject rootData = new SerializedObject(gameRoot.GetComponent<GameRoot>());
        Assert.That(rootData.FindProperty("_inventoryPresenter").objectReferenceValue, Is.Not.Null);
        Assert.That(_inventoryButton.interactable, Is.True);
        Assert.That(_mapButton.interactable, Is.False);
    }

    [Test]
    public void SameFrameDuplicateTransitionIsBlockedAndCloseRestoresControl()
    {
        _hudButton.onClick.Invoke();
        Assert.That(_codex.IsOpen, Is.True);
        Assert.That(_interaction.CanInteract, Is.False);
        Assert.That(_movement.CanMove, Is.False);

        _closeButton.onClick.Invoke();
        Assert.That(_codex.IsOpen, Is.True);

        ReadyNextTransition();
        _closeButton.onClick.Invoke();
        Assert.That(_codex.IsOpen, Is.False);
        Assert.That(_interaction.CanInteract, Is.True);
        Assert.That(_movement.CanMove, Is.True);
    }

    [Test]
    public void InventoryAndCodexSwitchWithoutOverlapOrLockGap()
    {
        _hudButton.onClick.Invoke();
        ReadyNextTransition();
        _inventoryButton.onClick.Invoke();
        Assert.That(_codex.IsOpen, Is.False);
        Assert.That(IsInventoryVisible(), Is.True);
        Assert.That(_movement.CanMove, Is.False);

        ReadyNextTransition();
        _bridge.CodexButton.onClick.Invoke();
        Assert.That(IsInventoryVisible(), Is.False);
        Assert.That(_codex.IsOpen, Is.True);
        Assert.That(_movement.CanMove, Is.False);

        ReadyNextTransition();
        _inventoryButton.onClick.Invoke();
        _inventory.Close();
        InvokePrivate(_navigation, "SyncPlayerLock");
        Assert.That(IsInventoryVisible(), Is.False);
        Assert.That(_movement.CanMove, Is.True);
        Assert.That(_interaction.CanInteract, Is.True);
    }

    [Test]
    public void CapsLockPressTogglesOnlyOncePerFrame()
    {
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        var originalSettings = InputSystem.settings;
        var settings = Object.Instantiate(originalSettings);
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = settings;
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.CapsLock));
            typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(InputUpdateType) }, null)
                .Invoke(null, new object[] { InputUpdateType.Dynamic });
            InvokePrivate(_navigation, "Update");
            InvokePrivate(_navigation, "Update");
            Assert.That(_codex.IsOpen, Is.True);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = originalSettings;
            Object.DestroyImmediate(settings);
        }
    }

    [Test]
    public void DisableAndDestroyRestoreExactPreviousPlayerState()
    {
        _interaction.SetCanInteract(false);
        _movement.SetCanMove(true);
        _navigation.OpenCodex();
        _navigation.enabled = false;
        Assert.That(_interaction.CanInteract, Is.False);
        Assert.That(_movement.CanMove, Is.True);

        _navigation.enabled = true;
        ReadyNextTransition();
        _navigation.OpenCodex();
        Object.DestroyImmediate(_navigation);
        _navigation = null;
        Assert.That(_interaction.CanInteract, Is.False);
        Assert.That(_movement.CanMove, Is.True);
    }

    [Test]
    public void MissingMapIsDisabledAndSafe()
    {
        Assert.That(_mapButton.interactable, Is.False);
        Assert.DoesNotThrow(() => _mapButton.onClick.Invoke());
    }

    private bool IsInventoryVisible() => _bridge.RootCanvasGroup.interactable
        && _bridge.RootCanvasGroup.alpha > 0.5f;

    private void ReadyNextTransition() => SetField(_navigation, "_transitionFrame", -1);

    private static GameObject CreateInstance(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null, path);
        return Object.Instantiate(prefab);
    }

    private static Transform Find(GameObject root, string path)
    {
        Transform result = root.transform.Find(path);
        Assert.That(result, Is.Not.Null, path);
        return result;
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, PrivateInstance);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name, PrivateInstance);
        Assert.That(method, Is.Not.Null, name);
        method.Invoke(target, null);
    }
}
