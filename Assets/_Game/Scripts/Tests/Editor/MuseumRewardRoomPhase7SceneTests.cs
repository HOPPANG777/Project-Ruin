//=============================================================================
// File:        MuseumRewardRoomPhase7SceneTests.cs
// Description: 빈 테스트 씬의 PlayMode에서 최종 방 표시·입력·벽화 배치를 검증한다.
// History:     08/31/2026 + Created -- Codex
//=============================================================================
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class MuseumRewardRoomPhase7SceneTests
{
    private GameObject _root;
    private MuseumRewardRoom _view;
    private RoomStub _room;
    private MuseumStub _museum;
    private PlayerInteractionController _player;
    private BoxCollider2D _door;
    private Renderer _reward;
    private MuseumMural _mural;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        _root = new GameObject(nameof(MuseumRewardRoomPhase7SceneTests));
        var pickup = Child("Pickup");
        pickup.AddComponent<BoxCollider2D>().isTrigger = true;
        _view = pickup.AddComponent<MuseumRewardRoom>();
        var bounds = Child("RoomBounds").AddComponent<BoxCollider2D>();
        bounds.isTrigger = true;
        bounds.size = new Vector2(10, 10);
        _door = Child("Door").AddComponent<BoxCollider2D>();
        var doorRenderer = _door.gameObject.AddComponent<SpriteRenderer>();
        _reward = pickup.AddComponent<SpriteRenderer>();
        var muralObject = Child("Mural");
        muralObject.AddComponent<BoxCollider2D>().isTrigger = true;
        // AddComponent 직후, Inspector 참조를 주입하기 전의 기존 OnValidate 로그입니다.
        LogAssert.Expect(LogType.Error, "[MuseumMural] Mural: Mural ID가 비어 있습니다.");
        LogAssert.Expect(LogType.Warning, "[MuseumMural] Mural: 가림 또는 원본 표시 참조가 누락됐습니다.");
        _mural = muralObject.AddComponent<MuseumMural>();
        Set(_mural, "_muralId", "TEST_MURAL_3");
        Set(_mural, "_coveredRoot", Child("Covered"));
        Set(_mural, "_revealedRoot", Child("Revealed"));
        Set(_view, "_rewardMilestoneId", "TEST_ALL");
        Set(_view, "_roomBounds", bounds);
        Set(_view, "_doorBlocker", _door);
        Set(_view, "_closedDoorRenderer", doorRenderer);
        Set(_view, "_rewardRenderer", _reward);
        Set(_view, "_chapter3Mural", _mural);
        _player = Child("Player").AddComponent<PlayerInteractionController>();
        _room = new RoomStub();
        _museum = new MuseumStub();
        _view.Bind(_room, _museum);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(_root);
        yield return new ExitPlayMode();
    }

    [Test]
    public void OpenClaimAndReenableRestoreDoorAndRewardWithoutRevealingMural()
    {
        Assert.That(_door.enabled, Is.True);
        Assert.That(_reward.enabled, Is.False);
        _room.Open();
        Assert.That(_door.enabled, Is.False);
        Assert.That(_reward.enabled, Is.True);
        _view.SetInteractionContext(_player, null);
        Assert.That(_view.TryInteract(out _), Is.True);
        Assert.That(_reward.enabled, Is.False);
        _view.gameObject.SetActive(false);
        _view.gameObject.SetActive(true);
        Assert.That(_door.enabled, Is.False);
        Assert.That(_reward.enabled, Is.False);
        Assert.That(_museum.RevealCalls, Is.Zero);
    }

    [Test]
    public void OutsidePlayerAndMissingContextNeverReachRewardService()
    {
        _room.Open();
        Assert.That(_view.TryInteract(out _), Is.False);
        _player.transform.position = new Vector3(20, 0, 0);
        _view.SetInteractionContext(_player, null);
        Assert.That(_view.TryInteract(out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_PLAYER_OUTSIDE_ROOM));
        Assert.That(_room.ClaimCalls, Is.Zero);
    }

    [Test]
    public void WrongChapterOrOutsideMuralFailsClosed()
    {
        _museum.Chapter = 2;
        _room.Open();
        Assert.That(_door.enabled, Is.True);
        _view.SetInteractionContext(_player, null);
        Assert.That(_view.TryInteract(out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_INVALID_ROOM_SETUP));
        _museum.Chapter = 3;
        _mural.transform.position = new Vector3(20, 0, 0);
        Assert.That(_view.TryValidateLayout(out _), Is.False);
    }

    [Test]
    public void DisabledViewUnsubscribesAndNewServiceBindingRestoresState()
    {
        _view.gameObject.SetActive(false);
        _room.Open();
        Assert.That(_door.enabled, Is.True);
        _view.gameObject.SetActive(true);
        Assert.That(_door.enabled, Is.False);
        _view.Bind(new RoomStub(), _museum);
        Assert.That(_door.enabled, Is.True);
        _room.Open();
        Assert.That(_door.enabled, Is.True);
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(_root.transform);
        return child;
    }

    private static void Set(Object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private sealed class RoomStub : IMuseumRewardRoomService
    {
        public event System.Action StateChanged;
        public bool IsOpen { get; private set; }
        public int ClaimCalls { get; private set; }
        private bool _claimed;
        public void Open()
        {
            IsOpen = true;
            StateChanged?.Invoke();
        }
        public bool TryOpen(out MuseumRelicDisplayFailure failure)
        {
            failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NONE;
            return IsOpen;
        }
        public bool TryGetRewardStatus(string id, out MuseumRelicMilestoneStatus status)
        {
            status = new MuseumRelicMilestoneStatus(true, _claimed);
            return true;
        }
        public bool TryClaimReward(string id, out MuseumRelicDisplayFailure failure)
        {
            ClaimCalls++;
            failure = MuseumRelicDisplayFailure.MUSEUM_RELIC_FAILURE_NONE;
            if (!IsOpen || _claimed) return false;
            _claimed = true;
            StateChanged?.Invoke();
            return true;
        }
    }

    private sealed class MuseumStub : IMuseumService
    {
        internal int Chapter { get; set; } = 3;
        internal bool DefinitionsReady { get; set; } = true;
        internal bool IsRevealed { get; set; } = true;
        internal int RevealCalls { get; private set; }
        public int MuralCount => 1;
        public int AltarDestinationCount => 0;
        public bool TryGetMural(string id, out MuseumMuralDefinition mural)
        {
            mural = new MuseumMuralDefinition(id, Chapter, "TEST_TECH");
            return DefinitionsReady;
        }
        public bool IsMuralRevealed(string id) => IsRevealed;
        public bool TryRevealMural(string id, out MuseumRevealFailure failure)
        {
            RevealCalls++;
            failure = MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_ALREADY_REVEALED;
            if (IsRevealed) return false;
            IsRevealed = true;
            failure = MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_NONE;
            return true;
        }
        public bool TryGetAltarDestination(string id, out AltarDestinationDefinition altar)
        {
            altar = null;
            return false;
        }
        public bool TryCreateAltarTransferData(string id, out SceneTransferData transferData)
        {
            transferData = null;
            return false;
        }
    }
}
