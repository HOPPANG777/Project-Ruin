//=============================================================================
// File:        CodexUiPhase2Tests.cs
// Description: Phase 2 도감 UI의 빈 데이터와 반복 생명주기 안전성을 검증한다.
// History:     08/22/2026 + Created -- Codex
//=============================================================================

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

[TestFixture]
public sealed class CodexUiPhase2Tests
{
    [Test]
    public void DisplayDataStartsLockedAndPreservesIngredientOrder()
    {
        CodexEntryDisplayData first = new CodexEntryDisplayData(
            string.Empty, null, "First", "Description", "Location", null, true);
        CodexEntryDisplayData second = new CodexEntryDisplayData(
            string.Empty, null, "Second", "Description", "Location", null, true);
        CodexEntryDisplayData alcohol = new CodexEntryDisplayData(
            "CODEX_ALCOHOL", null, "Alcohol", "Description", string.Empty,
            new[] { first, second });

        Assert.That(alcohol.IsUnlocked, Is.False);
        Assert.That(alcohol.Ingredients.Count, Is.EqualTo(2));
        Assert.That(alcohol.Ingredients[0].DisplayName, Is.EqualTo("First"));
        Assert.That(first.IsUnlocked, Is.True);
    }

    [Test]
    public void EntryViewHandlesMissingIconAndClearsCallbacks()
    {
        GameObject root = new GameObject(nameof(EntryViewHandlesMissingIconAndClearsCallbacks));
        CodexEntryView view = root.AddComponent<CodexEntryView>();
        CodexEntryDisplayData data = new CodexEntryDisplayData(
            "CODEX_RELIC", null, "Relic", "Long description", string.Empty);
        int clickCount = 0;
        view.Bind(data, CodexEntryLayout.CODEX_ENTRY_RELIC, null,
            _ => clickCount++, null, null);
        PointerEventData pointer = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left
        };

        Assert.DoesNotThrow(() => view.OnPointerClick(pointer));
        Assert.That(clickCount, Is.EqualTo(1));
        view.Clear();
        Assert.DoesNotThrow(() => view.OnPointerClick(pointer));
        Assert.That(clickCount, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(root);
    }

    [Test]
    public void EmptyViewCanOpenCloseAndSwitchRepeatedly()
    {
        GameObject root = new GameObject(nameof(EmptyViewCanOpenCloseAndSwitchRepeatedly));
        CodexView view = root.AddComponent<CodexView>();

        Assert.DoesNotThrow(() =>
        {
            view.Init();
            for (int i = 0; i < 5; i++)
            {
                view.Show();
                view.BindLeftPage(CodexCategory.CODEX_CATEGORY_RELIC,
                    Array.Empty<CodexEntryDisplayData>(), null, null, null);
                view.BindLeftPage(CodexCategory.CODEX_CATEGORY_ALCOHOL,
                    Array.Empty<CodexEntryDisplayData>(), null, null, null);
                view.BindTechnologyPage(Array.Empty<CodexEntryDisplayData>(), null, null, null);
                view.Hide();
            }
        });
        UnityEngine.Object.DestroyImmediate(root);
    }
}
