//=============================================================================
// File:        MuseumRewardRoomPhase7IntegrationTests.cs
// Description: 실제 도감·인벤토리·전시·방 Controller와 물리 탐지·입력 연결을 검증한다.
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public sealed partial class MuseumRewardRoomPhase7SceneTests
{
    private GameObject _servicesRoot;
    private MuseumRelicTestData _data;
    private MuseumRelicMilestoneRepository _rewards;
    private InventoryController _inventory;
    private CodexController _codex;
    private MuseumRelicDisplayController _displays;
    private MuseumRewardRoomController _realRoom;

    [UnityTest]
    public IEnumerator ActualPhysicsAndKeyboardClaimOnceAfterDiscoveryAndExhibition()
    {
        CreateRealServices(new MuseumBottleTestProgress());
        DiscoverAllReal();
        for (int i = 1; i <= 20; i++) Assert.That(_displays.TryPlace("P" + i, RelicId(i), out _), Is.True);
        Assert.That(_door.enabled, Is.False);
        Assert.That(_reward.enabled, Is.True);
        _view.gameObject.layer = LayerMask.NameToLayer("Interactable");
        // 배치 모드에는 GameView 포커스가 없으므로 입력 설정 복제본을 테스트 동안만 사용합니다.
        var originalSettings = InputSystem.settings;
        var testSettings = Object.Instantiate(originalSettings);
        bool originalRunInBackground = Application.runInBackground;
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        InputSystem.settings = testSettings;
        Application.runInBackground = true;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = actions.AddActionMap("RoomTest");
        var action = map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
        var actionReference = InputActionReference.Create(action);
        GameObject playerObject = Child("ActualInputPlayer");
        playerObject.SetActive(false);
        var player = playerObject.AddComponent<PlayerInteractionController>();
        Set(player, "_interactionLayers", (LayerMask)(1 << _view.gameObject.layer));
        Set(player, "_interactAction", actionReference);
        Set(player, "_interactRange", 1.0f);
        playerObject.SetActive(true);
        try
        {
            yield return null;
            Physics2D.SyncTransforms();
            // GameView가 없는 배치 모드에서도 게임 입력 버퍼와 실제 플레이어 탐지 경로를 검증합니다.
            player.SetCanInteract(false);
            PressInteract(player, keyboard);
            Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.Zero);
            player.SetCanInteract(true);
            player.transform.position = new Vector3(20, 0, 0);
            Physics2D.SyncTransforms();
            PressInteract(player, keyboard);
            Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.Zero);
            player.transform.position = _view.transform.position;
            Physics2D.SyncTransforms();
            Assert.That(Physics2D.OverlapPoint(_view.transform.position, 1 << _view.gameObject.layer),
                Is.SameAs(_view.GetComponent<BoxCollider2D>()), "실제 보검 Collider가 탐지돼야 합니다.");
            var filter = (ContactFilter2D)typeof(PlayerInteractionController).GetField("_contactFilter",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            Assert.That(filter.layerMask.value, Is.EqualTo(1 << _view.gameObject.layer), "Awake 마스크 연결");
            PressInteract(player, keyboard);
            Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.EqualTo(1));
            PressInteract(player, keyboard);
            Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.EqualTo(1));
            Assert.That(_reward.enabled, Is.False);
            Assert.That(_view.GetComponent<BoxCollider2D>().enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
            InputSystem.RemoveDevice(keyboard);
            Object.DestroyImmediate(actionReference);
            Object.DestroyImmediate(actions);
            InputSystem.settings = originalSettings;
            Object.DestroyImmediate(testSettings);
            Application.runInBackground = originalRunInBackground;
        }
    }

    [Test]
    public void BindBeforeInitializationRestoresDoorAndMuralWhenServicesBecomeReady()
    {
        _museum.DefinitionsReady = false;
        var progress = new MuseumBottleTestProgress();
        CreateRealServices(progress, false);
        Assert.That(_door.enabled, Is.True);
        var revealed = (GameObject)typeof(MuseumMural).GetField("_revealedRoot",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_mural);
        revealed.SetActive(false);
        DiscoverAllReal();
        _museum.DefinitionsReady = true;
        Assert.That(_realRoom.Init(_data, _codex, progress, _displays), Is.True);
        Assert.That(_door.enabled, Is.False);
        Assert.That(revealed.activeSelf, Is.True);
        Assert.That(_museum.RevealCalls, Is.Zero);
    }

    [Test]
    public void ActualSceneRejectsPickupWhenOneExhibitIsRecovered()
    {
        CreateRealServices(new MuseumBottleTestProgress());
        DiscoverAllReal();
        for (int i = 1; i <= 20; i++) Assert.That(_displays.TryPlace("P" + i, RelicId(i), out _), Is.True);
        Assert.That(_displays.TryRecover("P20", out _), Is.True);
        _view.SetInteractionContext(_player, null);
        Assert.That(_view.TryInteract(out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_MILESTONE_NOT_ELIGIBLE));
        Assert.That(_realRoom.IsOpen, Is.True);
        Assert.That(_inventory.GetItemCount("TEST_SWORD"), Is.Zero);
    }

    // 테스트 정의만 구성하며 제작용 CSV, Quest 또는 GameRoot 저장 경로를 변경하지 않습니다.
    private void CreateRealServices(IProgressService progress, bool initializeRoom = true)
    {
        _data = new MuseumRelicTestData();
        for (int i = 1; i <= 20; i++) _data.AddRelic(RelicId(i), i);
        _data.AddItem("TEST_BONUS");
        _data.AddItem("TEST_SWORD");
        var definitions = new[]
        {
            new MuseumRelicMilestoneDefinition("TEST_TEN", 10,
                MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_GRANT_IMMEDIATELY, "TEST_BONUS", 1),
            new MuseumRelicMilestoneDefinition("TEST_ALL", 20,
                MuseumRelicRewardMode.MUSEUM_RELIC_REWARD_ENABLE_ROOM_PICKUP, "TEST_SWORD", 1)
        };
        Assert.That(MuseumRelicMilestoneRepository.TryCreate(definitions, _data, out _rewards, out _), Is.True);
        _servicesRoot = Child("ActualServices");
        _inventory = _servicesRoot.AddComponent<InventoryController>();
        _codex = _servicesRoot.AddComponent<CodexController>();
        _displays = _servicesRoot.AddComponent<MuseumRelicDisplayController>();
        _realRoom = _servicesRoot.AddComponent<MuseumRewardRoomController>();
        _view.Bind(_realRoom, _museum);
        _inventory.Init(_data, null, progress);
        Assert.That(_displays.Init(_rewards, _data, _inventory, progress), Is.True);
        for (int i = 1; i <= 20; i++) _displays.RegisterPedestal("P" + i);
        Assert.That(_codex.Init(_data, _inventory, progress, null), Is.True);
        if (initializeRoom) Assert.That(_realRoom.Init(_data, _codex, progress, _displays), Is.True);
    }

    private void DiscoverAllReal()
    {
        for (int i = 1; i <= 20; i++) Assert.That(_inventory.TryAddItem(RelicId(i), 1), Is.True);
    }

    private static string RelicId(int index) => "TEST_RELIC_" + index;

    private static void PressInteract(PlayerInteractionController player, Keyboard keyboard)
    {
        // 공개 Update()는 포커스 없는 에디터에서 Editor 버퍼를 선택하므로 Dynamic 단계를 지정합니다.
        var update = typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(InputUpdateType) }, null);
        Assert.That(update, Is.Not.Null);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        update.Invoke(null, new object[] { InputUpdateType.Dynamic });
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
        update.Invoke(null, new object[] { InputUpdateType.Dynamic });
        Assert.That(keyboard.eKey.isPressed, Is.True, "가상 키보드 E 입력");
        typeof(PlayerInteractionController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(player, null);
    }
}
