//=============================================================================
// File:        CodexMuseumInteractionPhase8Tests.cs
// Description: 실제 ScrollRect 이벤트·반복 슬롯 재사용 및 Physics2D 벽화 Trigger 검증
// History:     09/01/2026 + Created -- Codex
//=============================================================================
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class CodexMuseumInteractionPhase8Tests
{
    private readonly List<GameObject> _created = new List<GameObject>();

    /// <summary>사용자 씬과 분리된 PlayMode에서 실제 컴포넌트 동작을 검사합니다.</summary>
    [UnitySetUp]
    public IEnumerator EnterRuntime()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    /// <summary>생성한 테스트 오브젝트만 제거하고 편집 모드로 돌아갑니다.</summary>
    [UnityTearDown]
    public IEnumerator ExitRuntime()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
            if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
        yield return new ExitPlayMode();
    }

    /// <summary>반복 열기·닫기에 슬롯이 증가하지 않으며 실제 스크롤 이벤트가 내용을 이동시킵니다.</summary>
    [Test]
    public void ScrollEventMovesRealContentAndReopeningReusesSlots()
    {
        var book = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/UI/CodexBook.prefab"));
        _created.Add(book);
        var view = book.GetComponent<CodexView>();
        var scroll = book.transform.Find("BookRoot/LeftPage/RelicScroll").GetComponent<ScrollRect>();
        var events = Create("EventSystem").AddComponent<EventSystem>();
        var entries = new List<CodexEntryDisplayData>();
        for (int i = 0; i < 24; i++)
            entries.Add(new CodexEntryDisplayData("P8_" + i, null, "Relic", "Description", string.Empty));
        view.Init();
        view.Show();
        view.BindLeftPage(CodexCategory.CODEX_CATEGORY_RELIC, entries, null, null, null);
        var ids = new HashSet<int>();
        for (int i = 0; i < scroll.content.childCount; i++)
            ids.Add(scroll.content.GetChild(i).GetInstanceID());
        for (int i = 0; i < 50; i++)
        {
            view.Hide();
            view.Show();
            view.BindLeftPage(CodexCategory.CODEX_CATEGORY_RELIC, entries, null, null, null);
        }
        Assert.That(scroll.content.childCount, Is.EqualTo(24));
        for (int i = 0; i < scroll.content.childCount; i++)
            Assert.That(ids.Contains(scroll.content.GetChild(i).GetInstanceID()), Is.True);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        scroll.verticalNormalizedPosition = 1;
        Canvas.ForceUpdateCanvases();
        Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
        Vector2 before = scroll.content.anchoredPosition;
        var pointer = new PointerEventData(events) { scrollDelta = Vector2.down * 3 };
        ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.scrollHandler);
        Assert.That(scroll.content.anchoredPosition, Is.Not.EqualTo(before));
        Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(1));
    }

    /// <summary>Collider 진입은 기술 반환 뒤에만 개봉하고, 재진입은 재개봉하지 않습니다.</summary>
    [Test]
    public void ActualPhysicsTriggerRequiresReturnAndRevealsOnce()
    {
        var muralObject = Create("Phase8Mural");
        muralObject.AddComponent<BoxCollider2D>().isTrigger = true;
        LogAssert.Expect(LogType.Error, "[MuseumMural] Phase8Mural: Mural ID가 비어 있습니다.");
        LogAssert.Expect(LogType.Warning, "[MuseumMural] Phase8Mural: 가림 또는 원본 표시 참조가 누락됐습니다.");
        var mural = muralObject.AddComponent<MuseumMural>();
        var covered = Create("Covered");
        var revealed = Create("Revealed");
        Set(mural, "_muralId", "MURAL_P8");
        Set(mural, "_coveredRoot", covered);
        Set(mural, "_revealedRoot", revealed);
        var service = new TriggerMuseum();
        mural.Bind(service);
        var player = Create("PhysicsPlayer");
        player.transform.position = Vector3.left * 5;
        player.AddComponent<UnitPlayer>();
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        player.AddComponent<BoxCollider2D>();
        var previousMode = Physics2D.simulationMode;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            Step(body, Vector2.zero);
            Assert.That(service.Attempts, Is.EqualTo(1));
            Assert.That(covered.activeSelf, Is.True);
            service.Returned = true;
            Assert.That(revealed.activeSelf, Is.False, "반환만으로 자동 개봉하면 안 됩니다.");
            Step(body, Vector2.left * 5);
            Step(body, Vector2.zero);
            Assert.That(revealed.activeSelf, Is.True);
            Assert.That(service.Reveals, Is.EqualTo(1));
            Step(body, Vector2.left * 5);
            Step(body, Vector2.zero);
            Assert.That(service.Reveals, Is.EqualTo(1));
        }
        finally { Physics2D.simulationMode = previousMode; }
    }

    private GameObject Create(string name)
    {
        var value = new GameObject(name);
        _created.Add(value);
        return value;
    }

    private static void Step(Rigidbody2D body, Vector2 position)
    {
        body.position = position;
        Physics2D.SyncTransforms();
        Physics2D.Simulate(Time.fixedDeltaTime);
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    // 반환 판정 자체는 Phase 4 도메인 테스트에서 검증합니다. 여기서는 실제 Trigger 전달을 확인합니다.
    private sealed class TriggerMuseum : IMuseumService
    {
        internal bool Returned;
        internal int Attempts;
        internal int Reveals;
        public int MuralCount => 1;
        public int AltarDestinationCount => 0;
        public bool IsMuralRevealed(string id) => Reveals > 0;
        public bool TryGetMural(string id, out MuseumMuralDefinition mural)
        { mural = new MuseumMuralDefinition(id, 1, "TECH_P8"); return true; }
        public bool TryGetAltarDestination(string id, out AltarDestinationDefinition value)
        { value = null; return false; }
        public bool TryCreateAltarTransferData(string id, out SceneTransferData value)
        { value = null; return false; }
        public bool TryRevealMural(string id, out MuseumRevealFailure failure)
        {
            Attempts++;
            if (!Returned) failure = MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_TECHNOLOGY_NOT_RETURNED;
            else if (Reveals > 0) failure = MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_ALREADY_REVEALED;
            else failure = MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_NONE;
            if (failure != MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_NONE) return false;
            Reveals++;
            return true;
        }
    }
}
