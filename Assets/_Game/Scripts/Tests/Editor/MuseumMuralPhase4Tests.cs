//=============================================================================
// File:        MuseumMuralPhase4Tests.cs
// Description: 벽화 표시 복원과 연출 참조 누락 안전성을 검증한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public sealed class MuseumMuralPhase4Tests
{
    private GameObject _root;
    private MuseumMural _mural;
    private GameObject _covered;
    private GameObject _revealed;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject(nameof(MuseumMuralPhase4Tests));
        _root.AddComponent<BoxCollider2D>().isTrigger = true;
        LogAssert.Expect(LogType.Error,
            $"[MuseumMural] {nameof(MuseumMuralPhase4Tests)}: Mural ID가 비어 있습니다.");
        LogAssert.Expect(LogType.Warning,
            $"[MuseumMural] {nameof(MuseumMuralPhase4Tests)}: 가림 또는 원본 표시 참조가 누락됐습니다.");
        _mural = _root.AddComponent<MuseumMural>();
        _covered = new GameObject("Covered");
        _covered.transform.SetParent(_root.transform);
        _revealed = new GameObject("Revealed");
        _revealed.transform.SetParent(_root.transform);
        SetField("_muralId", "MURAL_TEST");
        SetField("_coveredRoot", _covered);
        SetField("_revealedRoot", _revealed);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    [Test]
    public void LoadedMuralAppliesFinalStateWithoutPlayingEffect()
    {
        FakeMuseum museum = new FakeMuseum { IsRevealed = true };
        _mural.Bind(museum);
        Assert.That(_covered.activeSelf, Is.False);
        Assert.That(_revealed.activeSelf, Is.True);
        Assert.That(museum.RevealCalls, Is.Zero);
    }

    [Test]
    public void ReenabledMuralRestoresSavedFinalStateWithoutRevealAttempt()
    {
        FakeMuseum museum = new FakeMuseum { IsRevealed = true };
        _mural.Bind(museum);
        _root.SetActive(false);
        _root.SetActive(true);
        Assert.That(_covered.activeSelf, Is.False);
        Assert.That(_revealed.activeSelf, Is.True);
        Assert.That(museum.RevealCalls, Is.Zero);
    }

    [Test]
    public void MissingParticleDoesNotBlockSingleRevealState()
    {
        FakeMuseum museum = new FakeMuseum();
        _mural.Bind(museum);
        Assert.That(_mural.TryReveal(), Is.True);
        Assert.That(_mural.TryReveal(), Is.False);
        Assert.That(museum.IsRevealed, Is.True);
        Assert.That(_covered.activeSelf, Is.False);
        Assert.That(_revealed.activeSelf, Is.True);
        Assert.That(museum.RevealCalls, Is.EqualTo(2));
    }

    private void SetField(string fieldName, object value)
    {
        FieldInfo field = typeof(MuseumMural).GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(_mural, value);
    }

    private sealed class FakeMuseum : IMuseumService
    {
        internal bool IsRevealed { get; set; }
        internal int RevealCalls { get; private set; }
        public int MuralCount => 1;
        public int AltarDestinationCount => 0;
        public bool TryGetMural(string id, out MuseumMuralDefinition mural)
        { mural = new MuseumMuralDefinition(id, 0, "TECH_TEST"); return true; }
        public bool TryGetAltarDestination(string id, out AltarDestinationDefinition altar)
        { altar = null; return false; }
        public bool TryCreateAltarTransferData(string id, out SceneTransferData transferData)
        { transferData = null; return false; }
        public bool IsMuralRevealed(string id) => IsRevealed;
        public bool TryRevealMural(string id, out MuseumRevealFailure failure)
        {
            RevealCalls++;
            if (IsRevealed)
            {
                failure = MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_ALREADY_REVEALED;
                return false;
            }
            IsRevealed = true;
            failure = MuseumRevealFailure.MUSEUM_REVEAL_FAILURE_NONE;
            return true;
        }
    }
}
