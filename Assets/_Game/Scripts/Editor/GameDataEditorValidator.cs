using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//==============================================================================
// 기능: class::GameDataEditorValidator
// 설명: GameRoot에 연결된 CSV와 Unity 에셋 참조를 에디터에서 통합 검증하는 진입점
//==============================================================================
internal static class GameDataEditorValidator
{
    // 검증할 실제 런타임 GameRoot 프리팹 경로입니다.
    private const string GAME_ROOT_PREFAB_PATH = "Assets/_Game/Prefabs/GameRoot.prefab";

    // 검증 로그를 다른 시스템 로그와 구분하는 접두사입니다.
    private const string LOG_PREFIX = "[GameDataEditorValidator]";

    // GameRoot의 게임 데이터 컨트롤러 직렬화 필드 이름입니다.
    private const string PROPERTY_GAME_DATA_CONTROLLER = "_gameDataController";

    // GameRoot의 대화 컨트롤러 직렬화 필드 이름입니다.
    private const string PROPERTY_DIALOGUE_CONTROLLER = "_dialogueController";

    // GameRoot의 인벤토리 컨트롤러 직렬화 필드 이름입니다.
    private const string PROPERTY_INVENTORY_CONTROLLER = "_inventoryController";

    // DialogueController의 비주얼 카탈로그 직렬화 필드 이름입니다.
    private const string PROPERTY_VISUAL_CATALOG = "_visualCatalog";

    // InventoryController의 아이템 비주얼 카탈로그 직렬화 필드 이름입니다.
    private const string PROPERTY_ITEM_VISUAL_CATALOG = "_visualCatalog";

    // 아이템 비주얼 카탈로그의 아이콘 목록 직렬화 필드 이름입니다.
    private const string PROPERTY_ITEM_ICONS = "_icons";

    // 아이템 비주얼 항목의 아이콘 ID 직렬화 필드 이름입니다.
    private const string PROPERTY_ICON_ID = "_iconId";

    // 비주얼 카탈로그의 초상화 목록 직렬화 필드 이름입니다.
    private const string PROPERTY_PORTRAIT_SPRITES = "_portraitSprites";

    // 비주얼 카탈로그의 배경 목록 직렬화 필드 이름입니다.
    private const string PROPERTY_BACKGROUND_SPRITES = "_backgroundSprites";

    // 비주얼 에셋 항목의 ID 직렬화 필드 이름입니다.
    private const string PROPERTY_ASSET_ID = "_assetId";

    // 비주얼 에셋 항목의 Sprite 직렬화 필드 이름입니다.
    private const string PROPERTY_SPRITE = "_sprite";

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 실제 GameRoot 프리팹에 연결된 CSV와 Unity 에셋 참조를 모두 검증합니다.
    /// </summary>
    /// <returns>오류와 경고가 수집된 검증 보고서입니다.</returns>
    public static GameDataValidationReport Validate()
    {
        GameDataValidationReport report = new GameDataValidationReport();
        GameObject gameRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GAME_ROOT_PREFAB_PATH);

        if (gameRootPrefab == null)
        {
            report.AddError(GAME_ROOT_PREFAB_PATH, "GameRoot 프리팹을 찾을 수 없습니다.");
            return report;
        }

        GameRoot gameRoot = gameRootPrefab.GetComponent<GameRoot>();

        if (gameRoot == null)
        {
            report.AddError(GAME_ROOT_PREFAB_PATH, "GameRoot 컴포넌트를 찾을 수 없습니다.", gameRootPrefab);
            return report;
        }

        SerializedObject gameRootSerializedObject = new SerializedObject(gameRoot);

        if (!TryCreateCsvSource(
            gameRootSerializedObject,
            report,
            out GameDataCsvSource csvSource,
            out GameDataController gameDataController))
        {
            return report;
        }

        if (!GameDataCsvLoader.TryLoad(csvSource, out _, out GameDataCsvLoadError loadError))
        {
            AddCsvLoadError(report, loadError, gameDataController);
            return report;
        }

        if (!TryLoadUnityReferenceDefinitions(
            csvSource,
            report,
            gameDataController,
            out List<DialogueActorDefinition> actors,
            out List<DialogueGraphDefinition> graphs,
            out List<ItemDefinition> items))
        {
            return report;
        }

        ValidateDialogueVisuals(gameRootSerializedObject, actors, graphs, report);
        ValidateItemVisuals(gameRootSerializedObject, items, report);
        return report;
    }

    /// <summary>
    /// 검증 보고서의 모든 문제와 요약을 Unity Console에 출력합니다.
    /// </summary>
    /// <param name="report">출력할 검증 보고서입니다.</param>
    /// <param name="logSuccess">문제가 없을 때 성공 로그를 출력할지 여부입니다.</param>
    public static void LogReport(GameDataValidationReport report, bool logSuccess)
    {
        if (report == null)
        {
            Debug.LogError($"{LOG_PREFIX} 검증 보고서가 없습니다.");
            return;
        }

        for (int i = 0; i < report.Issues.Count; i++)
        {
            GameDataValidationIssue issue = report.Issues[i];
            string message = $"{LOG_PREFIX} source={issue.SourceName}, message={issue.Message}";

            if (issue.Severity == GameDataValidationSeverity.ERROR)
            {
                Debug.LogError(message, issue.Context);
            }
            else
            {
                Debug.LogWarning(message, issue.Context);
            }
        }

        if (report.HasIssues)
        {
            Debug.Log(
                $"{LOG_PREFIX} 검증 완료. errors={report.ErrorCount}, warnings={report.WarningCount}");
        }
        else if (logSuccess)
        {
            Debug.Log($"{LOG_PREFIX} 모든 CSV와 Unity 에셋 참조가 유효합니다.");
        }
    }

    //==============================================================================
    // CSV 원본 구성
    //==============================================================================

    /// <summary>
    /// GameRoot에 실제 연결된 GameDataController에서 전체 CSV 원본 묶음을 생성합니다.
    /// </summary>
    /// <param name="gameRootSerializedObject">GameRoot 직렬화 데이터입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    /// <param name="csvSource">생성된 전체 CSV 원본 묶음입니다.</param>
    /// <param name="gameDataController">검증한 게임 데이터 컨트롤러입니다.</param>
    /// <returns>게임 데이터 컨트롤러와 모든 필수 CSV가 연결되어 있으면 true입니다.</returns>
    private static bool TryCreateCsvSource(
        SerializedObject gameRootSerializedObject,
        GameDataValidationReport report,
        out GameDataCsvSource csvSource,
        out GameDataController gameDataController)
    {
        csvSource = null;
        gameDataController = GetObjectReference<GameDataController>(
            gameRootSerializedObject,
            PROPERTY_GAME_DATA_CONTROLLER);

        if (gameDataController == null)
        {
            report.AddError(GAME_ROOT_PREFAB_PATH, "GameDataController가 GameRoot에 연결되지 않았습니다.");
            return false;
        }

        SerializedObject controllerSerializedObject = new SerializedObject(gameDataController);
        bool hasAllRequiredFiles = true;

        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_dialogueActorsCsv",
            "dialogue_actors.csv",
            report,
            out GameDataCsvFile dialogueActors);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_dialoguePortraitsCsv",
            "dialogue_portraits.csv",
            report,
            out GameDataCsvFile dialoguePortraits);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_dialogueGraphsCsv",
            "dialogue_graphs.csv",
            report,
            out GameDataCsvFile dialogueGraphs);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_dialogueNodesCsv",
            "dialogue_nodes.csv",
            report,
            out GameDataCsvFile dialogueNodes);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_dialogueChoicesCsv",
            "dialogue_choices.csv",
            report,
            out GameDataCsvFile dialogueChoices);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_itemsCsv",
            "items.csv",
            report,
            out GameDataCsvFile items);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_questsCsv",
            "quests.csv",
            report,
            out GameDataCsvFile quests);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_questObjectivesCsv",
            "quest_objectives.csv",
            report,
            out GameDataCsvFile questObjectives);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_questRewardsCsv",
            "quest_rewards.csv",
            report,
            out GameDataCsvFile questRewards);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_questConsumeItemsCsv",
            "quest_consume_items.csv",
            report,
            out GameDataCsvFile questConsumeItems);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_localizationCsv",
            "localization.csv",
            report,
            out GameDataCsvFile localization);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_puzzlesCsv",
            "puzzles.csv",
            report,
            out GameDataCsvFile puzzles);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_puzzleLightTilesCsv",
            "puzzle_light_tiles.csv",
            report,
            out GameDataCsvFile puzzleLightTiles);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_puzzlePipeTilesCsv",
            "puzzle_pipe_tiles.csv",
            report,
            out GameDataCsvFile puzzlePipeTiles);
        hasAllRequiredFiles &= TryGetRequiredCsvFile(
            controllerSerializedObject,
            "_puzzleCircleRingsCsv",
            "puzzle_circle_rings.csv",
            report,
            out GameDataCsvFile puzzleCircleRings);

        if (!hasAllRequiredFiles)
        {
            return false;
        }

        GameDataCsvFile npcDialogueBindings = GetOptionalCsvFile(
            controllerSerializedObject,
            "_npcDialogueBindingsCsv");
        GameDataCsvFile npcQuestDialogueRoutes = GetOptionalCsvFile(
            controllerSerializedObject,
            "_npcQuestDialogueRoutesCsv");
        GameDataCsvFile shops = GetOptionalCsvFile(
            controllerSerializedObject,
            "_shopsCsv");
        GameDataCsvFile shopStock = GetOptionalCsvFile(
            controllerSerializedObject,
            "_shopStockCsv");

        csvSource = new GameDataCsvSource(
            dialogueActors,
            dialoguePortraits,
            dialogueGraphs,
            dialogueNodes,
            dialogueChoices,
            items,
            quests,
            questObjectives,
            questRewards,
            questConsumeItems,
            localization,
            npcDialogueBindings,
            npcQuestDialogueRoutes,
            puzzles,
            puzzleLightTiles,
            puzzlePipeTiles,
            puzzleCircleRings,
            shops,
            shopStock);
        return true;
    }

    /// <summary>
    /// 직렬화 필드에서 필수 CSV TextAsset을 조회하고 순수 C# 원본으로 변환합니다.
    /// </summary>
    /// <param name="serializedObject">CSV 필드를 가진 직렬화 오브젝트입니다.</param>
    /// <param name="propertyName">CSV 직렬화 필드 이름입니다.</param>
    /// <param name="expectedFileName">오류에 표시할 예상 CSV 파일 이름입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    /// <param name="csvFile">변환된 CSV 원본입니다.</param>
    /// <returns>내용이 있는 CSV TextAsset을 찾았으면 true입니다.</returns>
    private static bool TryGetRequiredCsvFile(
        SerializedObject serializedObject,
        string propertyName,
        string expectedFileName,
        GameDataValidationReport report,
        out GameDataCsvFile csvFile)
    {
        csvFile = null;
        TextAsset textAsset = GetObjectReference<TextAsset>(serializedObject, propertyName);

        if (textAsset == null)
        {
            report.AddError(expectedFileName, "필수 CSV TextAsset이 GameDataController에 연결되지 않았습니다.", serializedObject.targetObject);
            return false;
        }

        if (string.IsNullOrWhiteSpace(textAsset.text))
        {
            report.AddError(expectedFileName, "필수 CSV 파일의 내용이 비어 있습니다.", textAsset);
            return false;
        }

        csvFile = new GameDataCsvFile(textAsset.name, textAsset.text);
        return true;
    }

    /// <summary>
    /// 직렬화 필드에 선택 CSV TextAsset이 연결되어 있으면 순수 C# 원본으로 변환합니다.
    /// </summary>
    /// <param name="serializedObject">CSV 필드를 가진 직렬화 오브젝트입니다.</param>
    /// <param name="propertyName">CSV 직렬화 필드 이름입니다.</param>
    /// <returns>연결된 CSV 원본이며 연결이 없으면 null입니다.</returns>
    private static GameDataCsvFile GetOptionalCsvFile(SerializedObject serializedObject, string propertyName)
    {
        TextAsset textAsset = GetObjectReference<TextAsset>(serializedObject, propertyName);

        return textAsset != null
            ? new GameDataCsvFile(textAsset.name, textAsset.text)
            : null;
    }

    /// <summary>
    /// CSV 로더 오류를 에디터 검증 보고서 오류로 변환합니다.
    /// </summary>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    /// <param name="loadError">변환할 CSV 로더 오류입니다.</param>
    /// <param name="context">오류와 관련된 Unity 오브젝트입니다.</param>
    private static void AddCsvLoadError(
        GameDataValidationReport report,
        GameDataCsvLoadError loadError,
        Object context)
    {
        if (loadError == null)
        {
            report.AddError("CSV", "알 수 없는 원인으로 CSV 로드에 실패했습니다.", context);
            return;
        }

        string location = $"record={loadError.RecordNumber}, line={loadError.LineNumber}, column={loadError.ColumnName}";
        report.AddError(loadError.SourceName, $"{loadError.Message} ({location})", context);
    }

    //==============================================================================
    // Unity 참조 검증용 Definition 구성
    //==============================================================================

    /// <summary>
    /// Unity 비주얼 참조 검증에 필요한 대화 및 아이템 Definition 목록을 생성합니다.
    /// </summary>
    /// <param name="csvSource">검증된 전체 CSV 원본 묶음입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    /// <param name="context">CSV를 소유한 게임 데이터 컨트롤러입니다.</param>
    /// <param name="actors">생성된 대화 화자 목록입니다.</param>
    /// <param name="graphs">생성된 대화 그래프 목록입니다.</param>
    /// <param name="items">생성된 아이템 목록입니다.</param>
    /// <returns>참조 검증용 Definition을 모두 생성했으면 true입니다.</returns>
    private static bool TryLoadUnityReferenceDefinitions(
        GameDataCsvSource csvSource,
        GameDataValidationReport report,
        Object context,
        out List<DialogueActorDefinition> actors,
        out List<DialogueGraphDefinition> graphs,
        out List<ItemDefinition> items)
    {
        actors = null;
        graphs = null;
        items = null;

        if (!DialogueCsvDefinitionLoader.TryLoad(csvSource, out actors, out graphs, out GameDataCsvLoadError dialogueError))
        {
            AddCsvLoadError(report, dialogueError, context);
            return false;
        }

        if (!ItemCsvDefinitionLoader.TryLoad(csvSource.Items, out items, out GameDataCsvLoadError itemError))
        {
            AddCsvLoadError(report, itemError, context);
            return false;
        }

        return true;
    }

    //==============================================================================
    // 대화 비주얼 검증
    //==============================================================================

    /// <summary>
    /// CSV의 초상화와 배경 ID가 DialogueVisualCatalog의 Sprite에 연결되는지 검증합니다.
    /// </summary>
    /// <param name="gameRootSerializedObject">GameRoot 직렬화 데이터입니다.</param>
    /// <param name="actors">검증할 대화 화자 목록입니다.</param>
    /// <param name="graphs">검증할 대화 그래프 목록입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    private static void ValidateDialogueVisuals(
        SerializedObject gameRootSerializedObject,
        IReadOnlyList<DialogueActorDefinition> actors,
        IReadOnlyList<DialogueGraphDefinition> graphs,
        GameDataValidationReport report)
    {
        DialogueController dialogueController = GetObjectReference<DialogueController>(
            gameRootSerializedObject,
            PROPERTY_DIALOGUE_CONTROLLER);

        if (dialogueController == null)
        {
            report.AddError(GAME_ROOT_PREFAB_PATH, "DialogueController가 GameRoot에 연결되지 않았습니다.");
            return;
        }

        SerializedObject controllerSerializedObject = new SerializedObject(dialogueController);
        DialogueVisualCatalogSO visualCatalog = GetObjectReference<DialogueVisualCatalogSO>(
            controllerSerializedObject,
            PROPERTY_VISUAL_CATALOG);

        if (visualCatalog == null)
        {
            report.AddWarning("DialogueVisualCatalog", "비주얼 카탈로그가 연결되지 않아 초상화와 배경이 폴백으로 표시됩니다.", dialogueController);
            return;
        }

        ValidateVisualCatalogEntries(visualCatalog, report);

        HashSet<string> missingPortraitIds = new HashSet<string>(System.StringComparer.Ordinal);
        HashSet<string> missingBackgroundIds = new HashSet<string>(System.StringComparer.Ordinal);

        for (int actorIndex = 0; actorIndex < actors.Count; actorIndex++)
        {
            DialogueActorDefinition actor = actors[actorIndex];

            for (int portraitIndex = 0; portraitIndex < actor.Portraits.Count; portraitIndex++)
            {
                DialoguePortraitDefinition portrait = actor.Portraits[portraitIndex];

                if (string.IsNullOrWhiteSpace(portrait.SpriteId)
                    || visualCatalog.TryGetPortraitSprite(portrait.SpriteId, out _)
                    || !missingPortraitIds.Add(portrait.SpriteId))
                {
                    continue;
                }

                report.AddWarning(
                    "dialogue_portraits.csv",
                    $"sprite_id가 비주얼 카탈로그에 없습니다. sprite_id={portrait.SpriteId}",
                    visualCatalog);
            }
        }

        for (int graphIndex = 0; graphIndex < graphs.Count; graphIndex++)
        {
            DialogueGraphDefinition graph = graphs[graphIndex];
            ValidateBackgroundReference(
                graph.BackgroundId,
                "dialogue_graphs.csv",
                visualCatalog,
                missingBackgroundIds,
                report);

            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                ValidateBackgroundReference(
                    graph.Nodes[nodeIndex].BackgroundId,
                    "dialogue_nodes.csv",
                    visualCatalog,
                    missingBackgroundIds,
                    report);
            }
        }
    }

    /// <summary>
    /// 비주얼 카탈로그의 빈 ID와 Sprite 누락 및 중복 ID를 검증합니다.
    /// </summary>
    /// <param name="visualCatalog">검증할 비주얼 카탈로그입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    private static void ValidateVisualCatalogEntries(
        DialogueVisualCatalogSO visualCatalog,
        GameDataValidationReport report)
    {
        SerializedObject catalogSerializedObject = new SerializedObject(visualCatalog);
        ValidateVisualCatalogList(
            catalogSerializedObject.FindProperty(PROPERTY_PORTRAIT_SPRITES),
            "Portrait",
            visualCatalog,
            report);
        ValidateVisualCatalogList(
            catalogSerializedObject.FindProperty(PROPERTY_BACKGROUND_SPRITES),
            "Background",
            visualCatalog,
            report);
    }

    /// <summary>
    /// 비주얼 카탈로그의 직렬화 목록 한 종류를 검증합니다.
    /// </summary>
    /// <param name="entriesProperty">검증할 비주얼 항목 목록입니다.</param>
    /// <param name="categoryName">문제에 표시할 비주얼 종류 이름입니다.</param>
    /// <param name="visualCatalog">검증할 비주얼 카탈로그입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    private static void ValidateVisualCatalogList(
        SerializedProperty entriesProperty,
        string categoryName,
        DialogueVisualCatalogSO visualCatalog,
        GameDataValidationReport report)
    {
        if (entriesProperty == null || !entriesProperty.isArray)
        {
            report.AddWarning("DialogueVisualCatalog", $"{categoryName} 목록을 읽을 수 없습니다.", visualCatalog);
            return;
        }

        HashSet<string> assetIds = new HashSet<string>(System.StringComparer.Ordinal);

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            SerializedProperty assetIdProperty = entryProperty.FindPropertyRelative(PROPERTY_ASSET_ID);
            SerializedProperty spriteProperty = entryProperty.FindPropertyRelative(PROPERTY_SPRITE);
            string assetId = assetIdProperty != null ? assetIdProperty.stringValue.Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(assetId))
            {
                report.AddWarning("DialogueVisualCatalog", $"{categoryName} 목록 {i}번 항목의 에셋 ID가 비어 있습니다.", visualCatalog);
                continue;
            }

            if (!assetIds.Add(assetId))
            {
                report.AddWarning("DialogueVisualCatalog", $"{categoryName} 에셋 ID가 중복되었습니다. assetId={assetId}", visualCatalog);
            }

            if (spriteProperty == null || spriteProperty.objectReferenceValue == null)
            {
                report.AddWarning("DialogueVisualCatalog", $"{categoryName} Sprite가 연결되지 않았습니다. assetId={assetId}", visualCatalog);
            }
        }
    }

    /// <summary>
    /// CSV 배경 ID 한 개가 비주얼 카탈로그에 등록되어 있는지 검증합니다.
    /// </summary>
    /// <param name="backgroundId">검증할 CSV 배경 ID입니다.</param>
    /// <param name="sourceName">배경 ID가 작성된 CSV 이름입니다.</param>
    /// <param name="visualCatalog">배경 Sprite를 조회할 카탈로그입니다.</param>
    /// <param name="missingBackgroundIds">이미 보고한 누락 배경 ID 집합입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    private static void ValidateBackgroundReference(
        string backgroundId,
        string sourceName,
        DialogueVisualCatalogSO visualCatalog,
        HashSet<string> missingBackgroundIds,
        GameDataValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(backgroundId)
            || visualCatalog.TryGetBackgroundSprite(backgroundId, out _)
            || !missingBackgroundIds.Add(backgroundId))
        {
            return;
        }

        report.AddWarning(
            sourceName,
            $"background_id가 비주얼 카탈로그에 없습니다. background_id={backgroundId}",
            visualCatalog);
    }

    //==============================================================================
    // 아이템 비주얼 검증
    //==============================================================================

    /// <summary>
    /// items.csv의 icon_id가 ItemVisualCatalog의 Sprite에 연결되는지 검증합니다.
    /// </summary>
    /// <param name="gameRootSerializedObject">GameRoot 직렬화 데이터입니다.</param>
    /// <param name="items">검증할 아이템 목록입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    private static void ValidateItemVisuals(
        SerializedObject gameRootSerializedObject,
        IReadOnlyList<ItemDefinition> items,
        GameDataValidationReport report)
    {
        InventoryController inventoryController = GetObjectReference<InventoryController>(
            gameRootSerializedObject,
            PROPERTY_INVENTORY_CONTROLLER);

        if (inventoryController == null)
        {
            report.AddError(
                GAME_ROOT_PREFAB_PATH,
                "InventoryController가 GameRoot에 연결되지 않았습니다.",
                gameRootSerializedObject.targetObject);
            return;
        }

        SerializedObject controllerSerializedObject = new SerializedObject(inventoryController);
        ItemVisualCatalogSO visualCatalog = GetObjectReference<ItemVisualCatalogSO>(
            controllerSerializedObject,
            PROPERTY_ITEM_VISUAL_CATALOG);

        if (visualCatalog == null)
        {
            report.AddWarning(
                "ItemVisualCatalog",
                "아이템 비주얼 카탈로그가 연결되지 않아 아이콘이 표시되지 않습니다.",
                inventoryController);
            return;
        }

        ValidateItemVisualCatalogEntries(visualCatalog, report);
        HashSet<string> missingIconIds = new HashSet<string>(System.StringComparer.Ordinal);

        for (int i = 0; i < items.Count; i++)
        {
            ItemDefinition item = items[i];

            if (item == null
                || string.IsNullOrWhiteSpace(item.IconId)
                || visualCatalog.TryGetIcon(item.IconId, out _)
                || !missingIconIds.Add(item.IconId))
            {
                continue;
            }

            report.AddWarning(
                "items.csv",
                $"icon_id가 아이템 비주얼 카탈로그에 없습니다. icon_id={item.IconId}",
                visualCatalog);
        }
    }

    /// <summary>
    /// 아이템 비주얼 카탈로그의 빈 ID, 중복 ID, Sprite 누락을 검증합니다.
    /// </summary>
    /// <param name="visualCatalog">검증할 아이템 비주얼 카탈로그입니다.</param>
    /// <param name="report">문제를 추가할 검증 보고서입니다.</param>
    private static void ValidateItemVisualCatalogEntries(
        ItemVisualCatalogSO visualCatalog,
        GameDataValidationReport report)
    {
        SerializedObject catalogSerializedObject = new SerializedObject(visualCatalog);
        SerializedProperty iconsProperty = catalogSerializedObject.FindProperty(PROPERTY_ITEM_ICONS);

        if (iconsProperty == null || !iconsProperty.isArray)
        {
            report.AddWarning("ItemVisualCatalog", "아이콘 목록을 읽을 수 없습니다.", visualCatalog);
            return;
        }

        HashSet<string> iconIds = new HashSet<string>(System.StringComparer.Ordinal);

        for (int i = 0; i < iconsProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = iconsProperty.GetArrayElementAtIndex(i);
            SerializedProperty iconIdProperty = entryProperty.FindPropertyRelative(PROPERTY_ICON_ID);
            SerializedProperty spriteProperty = entryProperty.FindPropertyRelative(PROPERTY_SPRITE);
            string iconId = iconIdProperty != null ? iconIdProperty.stringValue.Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(iconId))
            {
                report.AddWarning("ItemVisualCatalog", $"아이콘 목록 {i}번 항목의 ID가 비어 있습니다.", visualCatalog);
                continue;
            }

            if (!iconIds.Add(iconId))
            {
                report.AddWarning("ItemVisualCatalog", $"아이콘 ID가 중복되었습니다. icon_id={iconId}", visualCatalog);
            }

            if (spriteProperty == null || spriteProperty.objectReferenceValue == null)
            {
                report.AddWarning(
                    "ItemVisualCatalog",
                    $"아이콘 Sprite가 연결되지 않았습니다. icon_id={iconId}",
                    visualCatalog);
            }
        }
    }

    //==============================================================================
    // 직렬화 조회
    //==============================================================================

    /// <summary>
    /// Unity 직렬화 필드에서 지정한 타입의 오브젝트 참조를 조회합니다.
    /// </summary>
    /// <typeparam name="T">조회할 Unity 오브젝트 타입입니다.</typeparam>
    /// <param name="serializedObject">필드를 소유한 직렬화 오브젝트입니다.</param>
    /// <param name="propertyName">조회할 직렬화 필드 이름입니다.</param>
    /// <returns>조회된 오브젝트이며 필드나 참조가 없으면 null입니다.</returns>
    private static T GetObjectReference<T>(SerializedObject serializedObject, string propertyName)
        where T : Object
    {
        if (serializedObject == null || string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);

        return property != null ? property.objectReferenceValue as T : null;
    }
}
