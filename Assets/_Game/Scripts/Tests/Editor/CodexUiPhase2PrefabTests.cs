//=============================================================================
// File:        CodexUiPhase2PrefabTests.cs
// Description: 실제 도감 프리팹의 풀, 탭, Tooltip과 잠금 Material을 검증한다.
// History:     08/22/2026 + Created -- Codex
//=============================================================================

using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[TestFixture]
public sealed class CodexUiPhase2PrefabTests
{
    private const string BOOK_PATH = "Assets/_Game/Prefabs/UI/CodexBook.prefab";
    private const string MATERIAL_PATH = "Assets/_Game/Prefabs/UI/CodexUiGrayscale.mat";
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
            Object.DestroyImmediate(_createdObjects[i]);
        _createdObjects.Clear();
    }

    [Test]
    public void PrefabHasRequiredReferences()
    {
        GameObject root = CreateBook();
        Assert.That(root.GetComponent<CodexView>().IsConfigured, Is.True);
        Assert.That(root.GetComponent<CodexPresenter>(), Is.Not.Null);
    }

    [Test]
    public void PoolReusesViewsAfterCountShrinksAndGrows()
    {
        GameObject root = CreateBook();
        CodexView view = root.GetComponent<CodexView>();
        Transform content = Find(root, "BookRoot/LeftPage/RelicScroll/Viewport/Content");
        List<CodexEntryDisplayData> entries = CreateEntries(5);
        view.Init();
        view.BindLeftPage(CodexCategory.CODEX_CATEGORY_RELIC, entries, null, null, null);
        HashSet<int> originalIds = GetChildIds(content);

        view.BindLeftPage(CodexCategory.CODEX_CATEGORY_RELIC,
            entries.GetRange(0, 2), null, null, null);
        Assert.That(content.childCount, Is.EqualTo(5));
        Assert.That(CountActiveChildren(content), Is.EqualTo(2));

        view.BindLeftPage(CodexCategory.CODEX_CATEGORY_RELIC, entries, null, null, null);
        Assert.That(GetChildIds(content), Is.EquivalentTo(originalIds));
        Assert.That(CountActiveChildren(content), Is.EqualTo(5));
    }

    [Test]
    public void RapidLeftPageChangesKeepTechnologyViews()
    {
        GameObject root = CreateBook();
        CodexView view = root.GetComponent<CodexView>();
        Transform content = Find(root, "BookRoot/RightPage/TechnologyScroll/Viewport/Content");
        view.Init();
        view.BindTechnologyPage(CreateEntries(3), null, null, null);

        for (int i = 0; i < 20; i++)
        {
            CodexCategory category = i % 2 == 0
                ? CodexCategory.CODEX_CATEGORY_RELIC
                : CodexCategory.CODEX_CATEGORY_ALCOHOL;
            view.BindLeftPage(category, CreateEntries(i % 4), null, null, null);
        }

        Assert.That(content.childCount, Is.EqualTo(3));
        Assert.That(CountActiveChildren(content), Is.EqualTo(3));
    }

    [Test]
    public void RepeatedInitRegistersTabEventOnce()
    {
        GameObject root = CreateBook();
        CodexView view = root.GetComponent<CodexView>();
        Button relicTab = Find(root, "BookRoot/LeftPage/RelicTab").GetComponent<Button>();
        int requestCount = 0;
        view.CategoryRequested += _ => requestCount++;

        view.Init();
        view.Init();
        view.Init();
        relicTab.onClick.Invoke();

        Assert.That(requestCount, Is.EqualTo(1));
    }

    [Test]
    public void HoverReusesTooltipForLockedAndLongUnlockedText()
    {
        GameObject root = CreateBook();
        CodexView view = root.GetComponent<CodexView>();
        Transform content = Find(root, "BookRoot/LeftPage/RelicScroll/Viewport/Content");
        Transform tooltip = Find(root, "BookRoot/Tooltip");
        TMP_Text name = Find(root, "BookRoot/Tooltip/Name").GetComponent<TMP_Text>();
        TMP_Text description = Find(root, "BookRoot/Tooltip/Description").GetComponent<TMP_Text>();
        string longText = new string('가', 300);
        CodexEntryDisplayData data = new CodexEntryDisplayData(
            "CODEX_RELIC", null, "긴 유물 이름", longText, string.Empty);
        PointerEventData pointer = new PointerEventData(null) { position = new Vector2(1910, 1070) };
        view.Init();
        view.Show();
        BindHover(view, data);
        CodexEntryView entry = content.GetChild(0).GetComponent<CodexEntryView>();

        entry.OnPointerEnter(pointer);
        Assert.That(tooltip.gameObject.activeSelf, Is.True);
        Assert.That(name.text, Is.EqualTo("???"));
        Assert.That(description.text, Is.EqualTo("?"));

        data.IsUnlocked = true;
        BindHover(view, data);
        entry.OnPointerEnter(pointer);
        Assert.That(name.text, Is.EqualTo("긴 유물 이름"));
        Assert.That(description.text, Is.EqualTo(longText));
        Assert.That(description.textWrappingMode, Is.Not.EqualTo(TextWrappingModes.NoWrap));
        entry.OnPointerExit(pointer);
        Assert.That(tooltip.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void LockedTechnologyUsesSharedGrayscaleMaterial()
    {
        GameObject root = CreateBook();
        CodexView view = root.GetComponent<CodexView>();
        Transform content = Find(root, "BookRoot/RightPage/TechnologyScroll/Viewport/Content");
        Material expected = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        view.Init();
        view.BindTechnologyPage(CreateEntries(1), null, null, null);
        Image icon = content.GetChild(0).Find("Icon").GetComponent<Image>();
        Assert.That(expected, Is.Not.Null);
        Assert.That(icon.material, Is.SameAs(expected));
    }

    private GameObject CreateBook()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BOOK_PATH);
        Assert.That(prefab, Is.Not.Null);
        GameObject root = Object.Instantiate(prefab);
        _createdObjects.Add(root);
        return root;
    }

    private static List<CodexEntryDisplayData> CreateEntries(int count)
    {
        List<CodexEntryDisplayData> entries = new List<CodexEntryDisplayData>(count);
        for (int i = 0; i < count; i++)
            entries.Add(new CodexEntryDisplayData("ENTRY_" + i, null,
                "Name " + i, "Description " + i, string.Empty));
        return entries;
    }

    private static void BindHover(CodexView view, CodexEntryDisplayData data)
    {
        view.BindLeftPage(CodexCategory.CODEX_CATEGORY_RELIC,
            new[] { data }, null,
            (entry, pointer) => view.ShowTooltip(entry.Data, pointer),
            _ => view.HideTooltip());
    }

    private static Transform Find(GameObject root, string path)
    {
        Transform result = root.transform.Find(path);
        Assert.That(result, Is.Not.Null, path);
        return result;
    }

    private static HashSet<int> GetChildIds(Transform root)
    {
        HashSet<int> ids = new HashSet<int>();
        for (int i = 0; i < root.childCount; i++) ids.Add(root.GetChild(i).GetInstanceID());
        return ids;
    }

    private static int CountActiveChildren(Transform root)
    {
        int count = 0;
        for (int i = 0; i < root.childCount; i++)
            if (root.GetChild(i).gameObject.activeSelf) count++;
        return count;
    }
}
