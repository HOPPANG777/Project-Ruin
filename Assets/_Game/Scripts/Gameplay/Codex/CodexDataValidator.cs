//=============================================================================
// File:        CodexDataValidator.cs
//
// Description: 기술·재건·도감 CSV의 중복과 교차 참조를 검증한다.
//
// History:     08/19/2026 + Created -- Codex
//=============================================================================

using System;
using System.Collections.Generic;

internal static class CodexDataValidator
{
    internal static bool TryValidate(
        IReadOnlyList<TechnologyDefinition> technologies,
        IReadOnlyList<RestorationStageDefinition> stages,
        IReadOnlyList<CodexEntryDefinition> entries,
        IReadOnlyList<CodexIngredientDefinition> ingredients,
        IGameDataRepository gameData,
        out GameDataRepositoryBuildError error)
    {
        error = null;
        if (technologies == null || stages == null || entries == null
            || ingredients == null || gameData == null)
        {
            return Fail("Codex", string.Empty, "도감 검증 입력이 없습니다.", out error);
        }
        return ValidateTechnologies(technologies, stages, gameData, out error)
            && ValidateEntries(entries, technologies, gameData, out error)
            && ValidateIngredients(entries, ingredients, gameData, out error);
    }

    private static bool ValidateTechnologies(
        IReadOnlyList<TechnologyDefinition> technologies,
        IReadOnlyList<RestorationStageDefinition> stages,
        IGameDataRepository gameData,
        out GameDataRepositoryBuildError error)
    {
        error = null;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int totalRestore = 0;
        for (int i = 0; i < technologies.Count; i++)
        {
            TechnologyDefinition technology = technologies[i];
            if (technology == null || !ids.Add(technology.TechnologyId))
                return Fail("Technology", technology?.TechnologyId, "기술 ID가 비었거나 중복됩니다.", out error);
            if (!HasText(gameData, technology.DisplayNameKey)
                || !HasText(gameData, technology.DescriptionTextKey)
                || !HasText(gameData, technology.OperationTextKey))
                return Fail("Technology", technology.TechnologyId, "기술 표시 문장 Key를 찾을 수 없습니다.", out error);
            if (technology.Chapter < 0 || technology.RestoreValue < 0
                || totalRestore > CodexDataRepository.RESTORE_MAX - technology.RestoreValue)
                return Fail("Technology", technology.TechnologyId, "챕터 또는 재건 기여도가 유효하지 않습니다.", out error);
            totalRestore += technology.RestoreValue;
        }
        if (technologies.Count == 0)
        {
            return stages.Count == 0
                || Fail("RestorationStage", string.Empty, "기술이 없을 때 복원 단계만 작성할 수 없습니다.", out error);
        }
        if (totalRestore != CodexDataRepository.RESTORE_MAX)
            return Fail("Technology", string.Empty, "기술 재건 기여도 합계는 100이어야 합니다.", out error);
        int previousRestore = 0;
        for (int i = 0; i < stages.Count; i++)
        {
            RestorationStageDefinition stage = stages[i];
            if (stage == null || stage.Stage != i + 1
                || stage.RequiredRestore <= previousRestore
                || stage.RequiredRestore > CodexDataRepository.RESTORE_MAX)
                return Fail("RestorationStage", (i + 1).ToString(), "단계 순서 또는 요구 재건도가 유효하지 않습니다.", out error);
            previousRestore = stage.RequiredRestore;
        }
        return stages.Count > 0 && previousRestore == CodexDataRepository.RESTORE_MAX
            || Fail("RestorationStage", string.Empty, "마지막 복원 단계 요구값은 100이어야 합니다.", out error);
    }

    private static bool ValidateEntries(
        IReadOnlyList<CodexEntryDefinition> entries,
        IReadOnlyList<TechnologyDefinition> technologies,
        IGameDataRepository gameData,
        out GameDataRepositoryBuildError error)
    {
        error = null;
        Dictionary<string, TechnologyDefinition> technologyById =
            new Dictionary<string, TechnologyDefinition>(StringComparer.Ordinal);
        for (int i = 0; i < technologies.Count; i++) technologyById.Add(technologies[i].TechnologyId, technologies[i]);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> sources = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> orders = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < entries.Count; i++)
        {
            CodexEntryDefinition entry = entries[i];
            if (entry == null || !ids.Add(entry.EntryId) || entry.DisplayOrder < 0 || entry.Chapter < 0)
                return Fail("CodexEntry", entry?.EntryId, "Entry ID, 순서 또는 챕터가 유효하지 않습니다.", out error);
            string sourceKey = string.Concat((int)entry.Category, ":", entry.SourceId);
            string orderKey = string.Concat((int)entry.Category, ":", entry.DisplayOrder);
            if (!sources.Add(sourceKey) || !orders.Add(orderKey))
                return Fail("CodexEntry", entry.EntryId, "같은 카테고리의 원본 또는 표시 순서가 중복됩니다.", out error);
            if (!ValidateEntrySource(entry, technologyById, gameData))
                return Fail("CodexEntry", entry.EntryId, "카테고리와 원본 ID 또는 챕터가 일치하지 않습니다.", out error);
        }
        return true;
    }

    private static bool ValidateIngredients(
        IReadOnlyList<CodexEntryDefinition> entries,
        IReadOnlyList<CodexIngredientDefinition> ingredients,
        IGameDataRepository gameData,
        out GameDataRepositoryBuildError error)
    {
        error = null;
        Dictionary<string, CodexEntryDefinition> entryById =
            new Dictionary<string, CodexEntryDefinition>(StringComparer.Ordinal);
        Dictionary<string, HashSet<int>> orders = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> items = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        for (int i = 0; i < entries.Count; i++) entryById.Add(entries[i].EntryId, entries[i]);
        for (int i = 0; i < ingredients.Count; i++)
        {
            CodexIngredientDefinition ingredient = ingredients[i];
            if (ingredient == null || !entryById.TryGetValue(ingredient.EntryId, out CodexEntryDefinition parent)
                || parent.Category != CodexCategory.CODEX_CATEGORY_ALCOHOL
                || ingredient.IngredientOrder < 0 || !gameData.TryGetItem(ingredient.ItemId, out _)
                || !HasText(gameData, ingredient.LocationTextKey))
                return Fail("CodexIngredient", ingredient?.EntryId, "재료 부모, 순서, Item 또는 위치 Key가 유효하지 않습니다.", out error);
            if (!orders.TryGetValue(parent.EntryId, out HashSet<int> parentOrders))
            {
                parentOrders = new HashSet<int>();
                items.Add(parent.EntryId, new HashSet<string>(StringComparer.Ordinal));
                orders.Add(parent.EntryId, parentOrders);
            }
            if (!parentOrders.Add(ingredient.IngredientOrder) || !items[parent.EntryId].Add(ingredient.ItemId))
                return Fail("CodexIngredient", parent.EntryId, "재료 순서 또는 Item이 중복됩니다.", out error);
        }
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Category == CodexCategory.CODEX_CATEGORY_ALCOHOL
                && !orders.ContainsKey(entries[i].EntryId))
                return Fail("CodexEntry", entries[i].EntryId, "술 도감 항목에는 재료가 한 개 이상 필요합니다.", out error);
        }
        return true;
    }

    private static bool ValidateEntrySource(
        CodexEntryDefinition entry,
        Dictionary<string, TechnologyDefinition> technologies,
        IGameDataRepository gameData)
    {
        if (entry.Category == CodexCategory.CODEX_CATEGORY_TECHNOLOGY)
            return technologies.TryGetValue(entry.SourceId, out TechnologyDefinition technology)
                && technology.Chapter == entry.Chapter;
        if (!gameData.TryGetItem(entry.SourceId, out ItemDefinition item)) return false;
        return entry.Category == CodexCategory.CODEX_CATEGORY_RELIC
            ? item.ItemType == ItemType.ITEM_RELIC_COLLECTIBLE
            : entry.Category == CodexCategory.CODEX_CATEGORY_ALCOHOL
                && item.ItemType == ItemType.ITEM_ALCOHOL_COLLECTIBLE;
    }

    private static bool HasText(IGameDataRepository gameData, string key) =>
        gameData.TryGetLocalizationEntry(key, out _);

    private static bool Fail(string type, string id, string message, out GameDataRepositoryBuildError error)
    {
        error = new GameDataRepositoryBuildError(type, id ?? string.Empty, message);
        return false;
    }
}

