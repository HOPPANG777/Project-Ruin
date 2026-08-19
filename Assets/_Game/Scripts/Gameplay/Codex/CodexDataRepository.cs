//=============================================================================
// File:        CodexDataRepository.cs
//
// Description: 검증된 기술·재건·도감 정의를 ID 인덱스로 보관한다.
//
// History:     08/19/2026 + Created -- Codex
//=============================================================================

using System;
using System.Collections.Generic;

public sealed class CodexDataRepository : ICodexDataRepository
{
    public const int RESTORE_MAX = 100;

    private readonly TechnologyDefinition[] _technologies;
    private readonly RestorationStageDefinition[] _stages;
    private readonly CodexEntryDefinition[] _entries;
    private readonly Dictionary<string, TechnologyDefinition> _technologyById;
    private readonly Dictionary<string, CodexEntryDefinition> _entryById;
    private readonly Dictionary<string, CodexEntryDefinition> _entryBySource;
    private readonly Dictionary<string, IReadOnlyList<CodexIngredientDefinition>> _ingredientsByEntryId;

    public int MaxRestore => RESTORE_MAX;
    public int TechnologyCount => _technologies.Length;
    public int CodexEntryCount => _entries.Length;
    public IReadOnlyList<TechnologyDefinition> Technologies => _technologies;
    public IReadOnlyList<RestorationStageDefinition> RestorationStages => _stages;
    public IReadOnlyList<CodexEntryDefinition> CodexEntries => _entries;

    private CodexDataRepository(
        IReadOnlyList<TechnologyDefinition> technologies,
        IReadOnlyList<RestorationStageDefinition> stages,
        IReadOnlyList<CodexEntryDefinition> entries,
        IReadOnlyList<CodexIngredientDefinition> ingredients)
    {
        _technologies = Copy(technologies);
        _stages = Copy(stages);
        _entries = Copy(entries);
        _technologyById = new Dictionary<string, TechnologyDefinition>(StringComparer.Ordinal);
        _entryById = new Dictionary<string, CodexEntryDefinition>(StringComparer.Ordinal);
        _entryBySource = new Dictionary<string, CodexEntryDefinition>(StringComparer.Ordinal);
        _ingredientsByEntryId = new Dictionary<string, IReadOnlyList<CodexIngredientDefinition>>(StringComparer.Ordinal);

        for (int i = 0; i < _technologies.Length; i++)
        {
            _technologyById.Add(_technologies[i].TechnologyId, _technologies[i]);
        }

        for (int i = 0; i < _entries.Length; i++)
        {
            CodexEntryDefinition entry = _entries[i];
            _entryById.Add(entry.EntryId, entry);
            _entryBySource.Add(CreateSourceKey(entry.Category, entry.SourceId), entry);
        }

        Dictionary<string, List<CodexIngredientDefinition>> grouped =
            new Dictionary<string, List<CodexIngredientDefinition>>(StringComparer.Ordinal);

        for (int i = 0; i < ingredients.Count; i++)
        {
            CodexIngredientDefinition ingredient = ingredients[i];
            if (!grouped.TryGetValue(ingredient.EntryId, out List<CodexIngredientDefinition> list))
            {
                list = new List<CodexIngredientDefinition>();
                grouped.Add(ingredient.EntryId, list);
            }
            list.Add(ingredient);
        }

        foreach (KeyValuePair<string, List<CodexIngredientDefinition>> pair in grouped)
        {
            pair.Value.Sort((left, right) => left.IngredientOrder.CompareTo(right.IngredientOrder));
            _ingredientsByEntryId.Add(pair.Key, pair.Value.ToArray());
        }
    }

    /// <summary>교차 검증된 도감 데이터 저장소를 생성합니다.</summary>
    public static bool TryCreate(
        IReadOnlyList<TechnologyDefinition> technologies,
        IReadOnlyList<RestorationStageDefinition> stages,
        IReadOnlyList<CodexEntryDefinition> entries,
        IReadOnlyList<CodexIngredientDefinition> ingredients,
        IGameDataRepository gameData,
        out CodexDataRepository repository,
        out GameDataRepositoryBuildError buildError)
    {
        repository = null;
        if (!CodexDataValidator.TryValidate(
            technologies, stages, entries, ingredients, gameData, out buildError))
        {
            return false;
        }
        repository = new CodexDataRepository(technologies, stages, entries, ingredients);
        return true;
    }

    public bool TryGetTechnology(string technologyId, out TechnologyDefinition technology) =>
        _technologyById.TryGetValue(technologyId ?? string.Empty, out technology);

    public bool TryGetCodexEntry(string entryId, out CodexEntryDefinition entry) =>
        _entryById.TryGetValue(entryId ?? string.Empty, out entry);

    public bool TryGetCodexEntryBySource(
        CodexCategory category, string sourceId, out CodexEntryDefinition entry) =>
        _entryBySource.TryGetValue(CreateSourceKey(category, sourceId), out entry);

    public bool TryGetCodexIngredients(
        string entryId, out IReadOnlyList<CodexIngredientDefinition> ingredients) =>
        _ingredientsByEntryId.TryGetValue(entryId ?? string.Empty, out ingredients);

    private static string CreateSourceKey(CodexCategory category, string sourceId) =>
        string.Concat((int)category, ":", sourceId ?? string.Empty);

    private static T[] Copy<T>(IReadOnlyList<T> source)
    {
        int count = source != null ? source.Count : 0;
        T[] copy = new T[count];
        for (int i = 0; i < count; i++) copy[i] = source[i];
        return copy;
    }
}

