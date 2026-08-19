using NUnit.Framework;
using UnityEditor;
using UnityEngine;

//==============================================================================
// 기능: class::GameDataCsvRegressionTests
// 설명: 프로젝트의 실제 런타임 CSV와 필수 오류 처리를 검증하는 에디터 회귀 테스트
//==============================================================================
[TestFixture]
public sealed class GameDataCsvRegressionTests
{
    // 실제 런타임 CSV TextAsset이 저장된 프로젝트 디렉터리입니다.
    private const string CSV_DIRECTORY_PATH = "Assets/_Game/Data/Csv/";

    // 필수 CSV 누락 오류에서 사용하는 원본 이름입니다.
    private const string MISSING_SOURCE_NAME = "(missing)";

    // 잘못된 헤더 테스트에서 사용하는 정상 화자 ID 헤더입니다.
    private const string VALID_ACTOR_ID_HEADER = "actor_id";

    // 잘못된 헤더 테스트에서 정상 헤더를 대체하는 잘못된 이름입니다.
    private const string INVALID_ACTOR_ID_HEADER = "actor_code";

    // 라이트 퍼즐 참조 오류 테스트에서 사용하는 존재하지 않는 타일 ID입니다.
    private const string INVALID_LIGHT_AFFECTED_IDS = "1|2|999";

    // CSV로 전환해 제거한 레거시 ScriptableObject 타입 이름 목록입니다.
    private static readonly string[] LEGACY_TYPE_NAMES =
    {
        "DialogueActorSO",
        "DialogueGraphSO",
        "QuestDataSO",
        "ItemDataSO",
        "TechnologyDataSO",
        "RelicRestorationCatalogSO",
        "LightPuzzleParser",
        "PipeParser",
        "CirclePuzzleParser",
        "PuzzleLevelData"
    };

    // CSV로 전환해 제거한 레거시 에셋 경로 목록입니다.
    private static readonly string[] LEGACY_ASSET_PATHS =
    {
        "Assets/_Game/Data/DialogueData/TestNPC/PlayerDialogueActor.asset",
        "Assets/_Game/Data/DialogueData/TestNPC/TestDialogueActor.asset",
        "Assets/_Game/Data/DialogueData/TestNPC/TestDialogueGraph.asset",
        "Assets/_Game/Data/QuestData/QuestData.asset",
        "Assets/_Game/Data/InventoryData/Coin.asset",
        "Assets/_Game/Data/InventoryData/RepairKit.asset",
        "Assets/_Game/Data/RelicCatalog/RelicRestorationCatalog.asset",
        "Assets/_Game/Data/PuzzleData/Light/Stage01.txt",
        "Assets/_Game/Data/PuzzleData/Pipe/Stage01.txt",
        "Assets/_Game/Data/PuzzleData/Circle/Stage01.txt"
    };

    //==============================================================================
    // 회귀 테스트
    //==============================================================================

    /// <summary>
    /// GameRoot가 사용하는 실제 CSV가 퍼즐을 포함한 유효한 게임 데이터 저장소로 변환되는지 검증합니다.
    /// </summary>
    [Test]
    public void RuntimeCsvFilesCreateRepository()
    {
        GameDataCsvSource source = CreateRuntimeSource();
        bool didLoad = GameDataCsvLoader.TryLoad(
            source,
            out GameDataRepository repository,
            out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.True, loadError?.ToString());
        Assert.That(repository, Is.Not.Null);
        Assert.That(repository.DialogueActorCount, Is.GreaterThan(0));
        Assert.That(repository.DialogueGraphCount, Is.GreaterThan(0));
        Assert.That(repository.ItemCount, Is.GreaterThan(0));
        Assert.That(repository.QuestCount, Is.GreaterThan(0));
        Assert.That(repository.LocalizationEntryCount, Is.GreaterThan(0));
        Assert.That(repository.PuzzleCount, Is.EqualTo(3));
        Assert.That(repository.TryGetPuzzle("PIPE_STAGE_01", out PuzzleDefinition pipePuzzle), Is.True);
        Assert.That(pipePuzzle.PipeTiles.Count, Is.EqualTo(16));
        Assert.That(repository.TryGetPuzzle("LIGHT_STAGE_01", out PuzzleDefinition lightPuzzle), Is.True);
        Assert.That(lightPuzzle.LightTiles.Count, Is.EqualTo(9));
        Assert.That(repository.TryGetPuzzle("CIRCLE_STAGE_01", out PuzzleDefinition circlePuzzle), Is.True);
        Assert.That(circlePuzzle.CircleRings.Count, Is.EqualTo(4));
        Assert.That(repository.TryGetItem("ITEM_COIN", out ItemDefinition coin), Is.True);
        Assert.That(coin.ItemType, Is.EqualTo(ItemType.ITEM_QUEST));
        Assert.That(coin.CanSell, Is.False);
        Assert.That(repository.TryGetQuest("QUEST_RESTORE_MURAL", out QuestDefinition quest), Is.True);
        Assert.That(HasItemReward(quest, "ITEM_COIN", 100), Is.True);
        Assert.That(repository.ShopCount, Is.Zero);
        Assert.That(repository.CodexData, Is.Not.Null);
        Assert.That(repository.CodexData.TechnologyCount, Is.Zero);
        Assert.That(repository.CodexData.CodexEntryCount, Is.Zero);
    }

    [Test]
    public void ValidCodexCsvCreatesCrossReferencedRepository()
    {
        GameDataCsvSource source = CreateCodexTestSource();

        bool didLoad = GameDataCsvLoader.TryLoad(
            source, out GameDataRepository repository, out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.True, loadError?.ToString());
        Assert.That(repository.CodexData.TechnologyCount, Is.EqualTo(2));
        Assert.That(repository.CodexData.CodexEntryCount, Is.EqualTo(3));
        Assert.That(repository.CodexData.CodexEntries[0].Category,
            Is.EqualTo(CodexCategory.CODEX_CATEGORY_RELIC));
        Assert.That(repository.CodexData.CodexEntries[2].Category,
            Is.EqualTo(CodexCategory.CODEX_CATEGORY_TECHNOLOGY));
        Assert.That(repository.CodexData.TryGetTechnology("TECH_TEST_A", out TechnologyDefinition technology), Is.True);
        Assert.That(technology.RestoreValue, Is.EqualTo(40));
        Assert.That(repository.CodexData.TryGetCodexIngredients(
            "CODEX_TEST_ALCOHOL", out var ingredients), Is.True);
        Assert.That(ingredients.Count, Is.EqualTo(1));
    }

    [Test]
    public void InvalidCodexCategoryIsRejected()
    {
        GameDataCsvSource source = CreateCodexTestSource();
        GameDataCsvFile invalidEntries = new GameDataCsvFile(
            "codex_entries.csv", source.CodexEntries.Text.Replace("RELIC", "UNKNOWN"));
        source = CopySourceWithCodex(source, source.Technologies,
            source.RestorationStages, invalidEntries, source.CodexIngredients);

        bool didLoad = GameDataCsvLoader.TryLoad(source, out _, out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError.SourceName, Is.EqualTo("codex_entries.csv"));
        Assert.That(loadError.ColumnName, Is.EqualTo("category"));
    }

    [Test]
    public void InvalidTechnologyRestoreTotalIsRejected()
    {
        GameDataCsvSource source = CreateCodexTestSource();
        GameDataCsvFile invalidTechnologies = new GameDataCsvFile(
            "technologies.csv", source.Technologies.Text.Replace(",60", ",50"));
        source = CopySourceWithCodex(source, invalidTechnologies,
            source.RestorationStages, source.CodexEntries, source.CodexIngredients);

        bool didLoad = GameDataCsvLoader.TryLoad(source, out _, out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        StringAssert.Contains("합계는 100", loadError.Message);
    }

    [Test]
    public void DuplicateCodexEntryIdIsRejected()
    {
        GameDataCsvSource source = CreateCodexTestSource();
        GameDataCsvFile invalidEntries = new GameDataCsvFile(
            "codex_entries.csv",
            source.CodexEntries.Text
                + "\nCODEX_TEST_RELIC,RELIC,1,ITEM_TEST_INGREDIENT,1");
        source = CopySourceWithCodex(source, source.Technologies,
            source.RestorationStages, invalidEntries, source.CodexIngredients);

        bool didLoad = GameDataCsvLoader.TryLoad(source, out _, out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError.ColumnName, Is.EqualTo("CodexEntry"));
        StringAssert.Contains("Entry ID", loadError.Message);
    }

    [Test]
    public void MissingCodexItemReferenceIsRejected()
    {
        GameDataCsvSource source = CreateCodexTestSource();
        GameDataCsvFile invalidEntries = new GameDataCsvFile(
            "codex_entries.csv",
            source.CodexEntries.Text.Replace("ITEM_TEST_RELIC", "ITEM_UNKNOWN"));
        source = CopySourceWithCodex(source, source.Technologies,
            source.RestorationStages, invalidEntries, source.CodexIngredients);

        bool didLoad = GameDataCsvLoader.TryLoad(source, out _, out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError.ColumnName, Is.EqualTo("CodexEntry"));
        StringAssert.Contains("원본 ID", loadError.Message);
    }

    /// <summary>
    /// 필수 Dialogue Actors CSV가 없으면 저장소 생성을 거부하고 누락 오류를 반환하는지 검증합니다.
    /// </summary>
    [Test]
    public void MissingRequiredCsvReturnsMissingSourceError()
    {
        GameDataCsvSource runtimeSource = CreateRuntimeSource();
        GameDataCsvSource missingSource = CopySourceWithDialogueActors(runtimeSource, null);
        bool didLoad = GameDataCsvLoader.TryLoad(
            missingSource,
            out _,
            out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError, Is.Not.Null);
        Assert.That(loadError.SourceName, Is.EqualTo(MISSING_SOURCE_NAME));
    }

    /// <summary>
    /// 필수 Items CSV가 없으면 저장소 생성을 거부하는지 검증합니다.
    /// </summary>
    [Test]
    public void MissingItemsCsvReturnsMissingSourceError()
    {
        GameDataCsvSource runtimeSource = CreateRuntimeSource();
        GameDataCsvSource missingSource = CopySourceWithItems(runtimeSource, null);

        bool didLoad = GameDataCsvLoader.TryLoad(
            missingSource,
            out _,
            out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError, Is.Not.Null);
        Assert.That(loadError.SourceName, Is.EqualTo(MISSING_SOURCE_NAME));
    }

    /// <summary>
    /// 확정 스키마와 다른 Dialogue Actors 헤더를 첫 번째 레코드에서 거부하는지 검증합니다.
    /// </summary>
    [Test]
    public void InvalidDialogueActorHeaderReturnsHeaderError()
    {
        GameDataCsvSource runtimeSource = CreateRuntimeSource();
        string invalidText = runtimeSource.DialogueActors.Text.Replace(
            VALID_ACTOR_ID_HEADER,
            INVALID_ACTOR_ID_HEADER);
        GameDataCsvFile invalidActors = new GameDataCsvFile(
            runtimeSource.DialogueActors.SourceName,
            invalidText);
        GameDataCsvSource invalidSource = CopySourceWithDialogueActors(
            runtimeSource,
            invalidActors);
        bool didLoad = GameDataCsvLoader.TryLoad(
            invalidSource,
            out _,
            out GameDataCsvLoadError loadError);

        Assert.That(invalidText, Is.Not.EqualTo(runtimeSource.DialogueActors.Text));
        Assert.That(didLoad, Is.False);
        Assert.That(loadError, Is.Not.Null);
        Assert.That(loadError.SourceName, Is.EqualTo(runtimeSource.DialogueActors.SourceName));
        Assert.That(loadError.RecordNumber, Is.EqualTo(1));
    }

    /// <summary>
    /// 라이트 퍼즐이 존재하지 않는 영향 타일 ID를 참조하면 저장소 생성을 거부하는지 검증합니다.
    /// </summary>
    [Test]
    public void InvalidLightAffectedTileReferenceReturnsCsvError()
    {
        GameDataCsvSource runtimeSource = CreateRuntimeSource();
        string invalidText = runtimeSource.PuzzleLightTiles.Text.Replace(
            "1|2|4",
            INVALID_LIGHT_AFFECTED_IDS);
        GameDataCsvFile invalidLightTiles = new GameDataCsvFile(
            runtimeSource.PuzzleLightTiles.SourceName,
            invalidText);
        GameDataCsvSource invalidSource = CopySourceWithPuzzleLightTiles(
            runtimeSource,
            invalidLightTiles);

        bool didLoad = GameDataCsvLoader.TryLoad(
            invalidSource,
            out _,
            out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError, Is.Not.Null);
        Assert.That(loadError.SourceName, Is.EqualTo(runtimeSource.PuzzleLightTiles.SourceName));
        StringAssert.Contains("존재하지 않는 라이트 타일 ID", loadError.Message);
    }

    /// <summary>
    /// 유효한 상점 CSV가 표시 순서로 정렬된 상점 정의를 생성하는지 검증합니다.
    /// </summary>
    [Test]
    public void ValidShopCsvCreatesOrderedRepository()
    {
        GameDataCsvSource source = CreateShopTestSource(
            "shop_id,currency_item_id\nSHOP_TEST,ITEM_COIN",
            "shop_id,item_id,display_order,initial_stock\nSHOP_TEST,ITEM_REPAIR_KIT,2,0\nSHOP_TEST,ITEM_TEST_TOOL,0,3");

        bool didLoad = GameDataCsvLoader.TryLoad(
            source, out GameDataRepository repository, out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.True, loadError?.ToString());
        Assert.That(repository.ShopCount, Is.EqualTo(1));
        Assert.That(repository.TryGetShop("SHOP_TEST", out ShopDefinition shop), Is.True);
        Assert.That(shop.CurrencyItemId, Is.EqualTo("ITEM_COIN"));
        Assert.That(shop.Stock.Count, Is.EqualTo(2));
        Assert.That(shop.Stock[0].ItemId, Is.EqualTo("ITEM_TEST_TOOL"));
        Assert.That(shop.Stock[1].InitialStock, Is.Zero);
    }

    [TestCase(
        "shop_id,currency_item_id\nSHOP_TEST,ITEM_COIN\nSHOP_TEST,ITEM_COIN",
        "shop_id,item_id,display_order,initial_stock",
        "shop_id")]
    [TestCase(
        "shop_id,currency_item_id\nSHOP_TEST,ITEM_COIN",
        "shop_id,item_id,display_order,initial_stock\nSHOP_UNKNOWN,ITEM_REPAIR_KIT,0,1",
        "shop_id")]
    [TestCase(
        "shop_id,currency_item_id\nSHOP_TEST,ITEM_COIN",
        "shop_id,item_id,display_order,initial_stock\nSHOP_TEST,ITEM_REPAIR_KIT,0,-1",
        "initial_stock")]
    [TestCase(
        "shop_id,currency_item_id\nSHOP_TEST,ITEM_COIN",
        "shop_id,item_id,display_order,initial_stock\nSHOP_TEST,ITEM_REPAIR_KIT,0,1\nSHOP_TEST,ITEM_TEST_TOOL,0,1",
        "display_order")]
    [TestCase(
        "shop_id,currency_item_id\nSHOP_TEST,ITEM_COIN",
        "shop_id,item_id,display_order,initial_stock\nSHOP_TEST,ITEM_REPAIR_KIT,0,1\nSHOP_TEST,ITEM_REPAIR_KIT,1,1",
        "item_id")]
    public void InvalidShopCsvIsRejected(string shopsText, string stockText, string expectedColumn)
    {
        bool didLoad = ShopCsvDefinitionLoader.TryLoad(
            new GameDataCsvFile("shops.csv", shopsText),
            new GameDataCsvFile("shop_stock.csv", stockText),
            out _,
            out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError, Is.Not.Null);
        Assert.That(loadError.ColumnName, Is.EqualTo(expectedColumn));
    }

    /// <summary>
    /// 상점 화폐와 상품의 잘못된 아이템 참조를 저장소 생성 전에 거부하는지 검증합니다.
    /// </summary>
    [TestCase(
        "shop_id,currency_item_id\nSHOP_TEST,ITEM_UNKNOWN",
        "shop_id,item_id,display_order,initial_stock",
        "Shop")]
    [TestCase(
        "shop_id,currency_item_id\nSHOP_TEST,ITEM_COIN",
        "shop_id,item_id,display_order,initial_stock\nSHOP_TEST,ITEM_COIN,0,1",
        "ShopStock")]
    [TestCase(
        "shop_id,currency_item_id\nSHOP_TEST,ITEM_COIN",
        "shop_id,item_id,display_order,initial_stock\nSHOP_TEST,ITEM_UNKNOWN,0,1",
        "ShopStock")]
    public void InvalidShopItemReferenceIsRejected(
        string shopsText,
        string stockText,
        string expectedDataType)
    {
        GameDataCsvSource source = CreateShopTestSource(shopsText, stockText);

        bool didLoad = GameDataCsvLoader.TryLoad(source, out _, out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError, Is.Not.Null);
        Assert.That(loadError.ColumnName, Is.EqualTo(expectedDataType));
    }

    /// <summary>
    /// 판매 가능 아이템에 0 가격을 지정하면 저장소 생성을 거부하는지 검증합니다.
    /// </summary>
    [Test]
    public void SellableZeroPriceItemIsRejected()
    {
        GameDataCsvSource source = CreateRuntimeSource();
        string invalidText = source.Items.Text.Replace(
            "ITEM_REPAIR_KIT,item.repair_kit.name,item.repair_kit.description,,0,false,ITEM_QUEST,99",
            "ITEM_REPAIR_KIT,item.repair_kit.name,item.repair_kit.description,,0,true,ITEM_QUEST,99");
        source = CopySourceWithItems(source, new GameDataCsvFile("items.csv", invalidText));

        bool didLoad = GameDataCsvLoader.TryLoad(source, out _, out GameDataCsvLoadError loadError);

        Assert.That(didLoad, Is.False);
        Assert.That(loadError.ColumnName, Is.EqualTo("Item"));
    }

    /// <summary>
    /// CSV 전환이 끝난 레거시 ScriptableObject 타입이 런타임 어셈블리에 없는지 검증합니다.
    /// </summary>
    [Test]
    public void LegacyGameDataTypesAreRemoved()
    {
        System.Reflection.Assembly runtimeAssembly = typeof(GameDataController).Assembly;

        for (int i = 0; i < LEGACY_TYPE_NAMES.Length; i++)
        {
            System.Type legacyType = runtimeAssembly.GetType(LEGACY_TYPE_NAMES[i]);

            Assert.That(
                legacyType,
                Is.Null,
                $"제거된 레거시 타입이 다시 추가되었습니다. type={LEGACY_TYPE_NAMES[i]}");
        }
    }

    /// <summary>
    /// 제거된 레거시 에셋이 프로젝트에 다시 포함되지 않았는지 검증합니다.
    /// </summary>
    [Test]
    public void LegacyGameDataAssetsAreRemoved()
    {
        for (int i = 0; i < LEGACY_ASSET_PATHS.Length; i++)
        {
            Object legacyAsset = AssetDatabase.LoadAssetAtPath<Object>(LEGACY_ASSET_PATHS[i]);

            Assert.That(
                legacyAsset,
                Is.Null,
                $"제거된 레거시 에셋이 다시 추가되었습니다. path={LEGACY_ASSET_PATHS[i]}");
        }
    }

    //==============================================================================
    // 테스트 데이터 구성
    //==============================================================================

    /// <summary>
    /// 프로젝트의 실제 런타임 CSV TextAsset을 전체 게임 데이터 원본 묶음으로 생성합니다.
    /// </summary>
    /// <returns>실제 런타임 CSV 원본 묶음입니다.</returns>
    private static GameDataCsvSource CreateRuntimeSource()
    {
        return new GameDataCsvSource(
            LoadRequiredCsv("dialogue_actors.csv"),
            LoadRequiredCsv("dialogue_portraits.csv"),
            LoadRequiredCsv("dialogue_graphs.csv"),
            LoadRequiredCsv("dialogue_nodes.csv"),
            LoadRequiredCsv("dialogue_choices.csv"),
            LoadRequiredCsv("items.csv"),
            LoadRequiredCsv("quests.csv"),
            LoadRequiredCsv("quest_objectives.csv"),
            LoadRequiredCsv("quest_rewards.csv"),
            LoadRequiredCsv("quest_consume_items.csv"),
            LoadRequiredCsv("localization.csv"),
            LoadRequiredCsv("npc_dialogue_bindings.csv"),
            LoadRequiredCsv("npc_quest_dialogue_routes.csv"),
            LoadRequiredCsv("puzzles.csv"),
            LoadRequiredCsv("puzzle_light_tiles.csv"),
            LoadRequiredCsv("puzzle_pipe_tiles.csv"),
            LoadRequiredCsv("puzzle_circle_rings.csv"),
            LoadRequiredCsv("shops.csv"),
            LoadRequiredCsv("shop_stock.csv"),
            LoadRequiredCsv("technologies.csv"),
            LoadRequiredCsv("restoration_stages.csv"),
            LoadRequiredCsv("codex_entries.csv"),
            LoadRequiredCsv("codex_ingredients.csv"));
    }

    private static bool HasItemReward(QuestDefinition quest, string itemId, int amount)
    {
        if (quest == null)
        {
            return false;
        }

        for (int i = 0; i < quest.Rewards.Count; i++)
        {
            QuestRewardDefinition reward = quest.Rewards[i];

            if (reward != null && reward.ItemId == itemId && reward.ItemAmount == amount)
            {
                return true;
            }
        }

        return false;
    }

    private static GameDataCsvSource CreateShopTestSource(string shopsText, string stockText)
    {
        GameDataCsvSource source = CreateRuntimeSource();
        string itemText = source.Items.Text.Replace(
            "ITEM_REPAIR_KIT,item.repair_kit.name,item.repair_kit.description,,0,false,ITEM_QUEST,99",
            "ITEM_REPAIR_KIT,item.repair_kit.name,item.repair_kit.description,,25,true,ITEM_QUEST,99");
        itemText = itemText.TrimEnd()
            + "\nITEM_TEST_TOOL,item.repair_kit.name,item.repair_kit.description,,50,true,ITEM_MISC,10";
        source = CopySourceWithItems(source, new GameDataCsvFile("items.csv", itemText));
        return CopySourceWithShops(
            source,
            new GameDataCsvFile("shops.csv", shopsText),
            new GameDataCsvFile("shop_stock.csv", stockText));
    }

    private static GameDataCsvSource CreateCodexTestSource()
    {
        GameDataCsvSource source = CreateRuntimeSource();
        string itemText = source.Items.Text.TrimEnd()
            + "\nITEM_TEST_RELIC,item.coin.name,item.coin.description,,0,false,ITEM_RELIC_COLLECTIBLE,1"
            + "\nITEM_TEST_ALCOHOL,item.coin.name,item.coin.description,,0,false,ITEM_ALCOHOL_COLLECTIBLE,1"
            + "\nITEM_TEST_INGREDIENT,item.coin.name,item.coin.description,,0,false,ITEM_MISC,99";
        source = CopySourceWithItems(source, new GameDataCsvFile("items.csv", itemText));
        GameDataCsvFile technologies = new GameDataCsvFile(
            "technologies.csv",
            "technology_id,display_name_key,description_text_key,operation_text_key,icon_id,derived_skill_id,chapter,restore_value\n"
            + "TECH_TEST_A,item.coin.name,item.coin.description,item.coin.name,,,1,40\n"
            + "TECH_TEST_B,item.coin.name,item.coin.description,item.coin.name,,,2,60");
        GameDataCsvFile stages = new GameDataCsvFile(
            "restoration_stages.csv", "stage,required_restore\n1,40\n2,100");
        GameDataCsvFile entries = new GameDataCsvFile(
            "codex_entries.csv",
            "entry_id,category,display_order,source_id,chapter\n"
            + "CODEX_TEST_TECH,TECHNOLOGY,0,TECH_TEST_A,1\n"
            + "CODEX_TEST_ALCOHOL,ALCOHOL,0,ITEM_TEST_ALCOHOL,1\n"
            + "CODEX_TEST_RELIC,RELIC,0,ITEM_TEST_RELIC,1");
        GameDataCsvFile ingredients = new GameDataCsvFile(
            "codex_ingredients.csv",
            "entry_id,ingredient_order,item_id,location_text_key\n"
            + "CODEX_TEST_ALCOHOL,0,ITEM_TEST_INGREDIENT,item.coin.name");
        return CopySourceWithCodex(source, technologies, stages, entries, ingredients);
    }

    /// <summary>
    /// 런타임 CSV 원본 묶음을 복사하면서 Dialogue Actors 원본만 교체합니다.
    /// </summary>
    /// <param name="source">복사할 런타임 CSV 원본 묶음입니다.</param>
    /// <param name="dialogueActors">교체할 Dialogue Actors 원본입니다.</param>
    /// <returns>Dialogue Actors 원본이 교체된 CSV 원본 묶음입니다.</returns>
    private static GameDataCsvSource CopySourceWithDialogueActors(
        GameDataCsvSource source,
        GameDataCsvFile dialogueActors)
    {
        return new GameDataCsvSource(
            dialogueActors,
            source.DialoguePortraits,
            source.DialogueGraphs,
            source.DialogueNodes,
            source.DialogueChoices,
            source.Items,
            source.Quests,
            source.QuestObjectives,
            source.QuestRewards,
            source.QuestConsumeItems,
            source.Localization,
            source.NpcDialogueBindings,
            source.NpcQuestDialogueRoutes,
            source.Puzzles,
            source.PuzzleLightTiles,
            source.PuzzlePipeTiles,
            source.PuzzleCircleRings,
            source.Shops,
            source.ShopStock,
            source.Technologies,
            source.RestorationStages,
            source.CodexEntries,
            source.CodexIngredients);
    }

    /// <summary>
    /// 런타임 CSV 원본을 복사하면서 Items 원본만 교체합니다.
    /// </summary>
    private static GameDataCsvSource CopySourceWithItems(
        GameDataCsvSource source,
        GameDataCsvFile items)
    {
        return new GameDataCsvSource(
            source.DialogueActors,
            source.DialoguePortraits,
            source.DialogueGraphs,
            source.DialogueNodes,
            source.DialogueChoices,
            items,
            source.Quests,
            source.QuestObjectives,
            source.QuestRewards,
            source.QuestConsumeItems,
            source.Localization,
            source.NpcDialogueBindings,
            source.NpcQuestDialogueRoutes,
            source.Puzzles,
            source.PuzzleLightTiles,
            source.PuzzlePipeTiles,
            source.PuzzleCircleRings,
            source.Shops,
            source.ShopStock,
            source.Technologies,
            source.RestorationStages,
            source.CodexEntries,
            source.CodexIngredients);
    }

    /// <summary>
    /// 런타임 CSV 원본 묶음을 복사하면서 라이트 퍼즐 타일 원본만 교체합니다.
    /// </summary>
    private static GameDataCsvSource CopySourceWithPuzzleLightTiles(
        GameDataCsvSource source,
        GameDataCsvFile puzzleLightTiles)
    {
        return new GameDataCsvSource(
            source.DialogueActors,
            source.DialoguePortraits,
            source.DialogueGraphs,
            source.DialogueNodes,
            source.DialogueChoices,
            source.Items,
            source.Quests,
            source.QuestObjectives,
            source.QuestRewards,
            source.QuestConsumeItems,
            source.Localization,
            source.NpcDialogueBindings,
            source.NpcQuestDialogueRoutes,
            source.Puzzles,
            puzzleLightTiles,
            source.PuzzlePipeTiles,
            source.PuzzleCircleRings,
            source.Shops,
            source.ShopStock,
            source.Technologies,
            source.RestorationStages,
            source.CodexEntries,
            source.CodexIngredients);
    }

    private static GameDataCsvSource CopySourceWithShops(
        GameDataCsvSource source,
        GameDataCsvFile shops,
        GameDataCsvFile shopStock)
    {
        return new GameDataCsvSource(
            source.DialogueActors,
            source.DialoguePortraits,
            source.DialogueGraphs,
            source.DialogueNodes,
            source.DialogueChoices,
            source.Items,
            source.Quests,
            source.QuestObjectives,
            source.QuestRewards,
            source.QuestConsumeItems,
            source.Localization,
            source.NpcDialogueBindings,
            source.NpcQuestDialogueRoutes,
            source.Puzzles,
            source.PuzzleLightTiles,
            source.PuzzlePipeTiles,
            source.PuzzleCircleRings,
            shops,
            shopStock,
            source.Technologies,
            source.RestorationStages,
            source.CodexEntries,
            source.CodexIngredients);
    }

    private static GameDataCsvSource CopySourceWithCodex(
        GameDataCsvSource source,
        GameDataCsvFile technologies,
        GameDataCsvFile stages,
        GameDataCsvFile entries,
        GameDataCsvFile ingredients)
    {
        return new GameDataCsvSource(
            source.DialogueActors, source.DialoguePortraits, source.DialogueGraphs,
            source.DialogueNodes, source.DialogueChoices, source.Items, source.Quests,
            source.QuestObjectives, source.QuestRewards, source.QuestConsumeItems,
            source.Localization, source.NpcDialogueBindings, source.NpcQuestDialogueRoutes,
            source.Puzzles, source.PuzzleLightTiles, source.PuzzlePipeTiles,
            source.PuzzleCircleRings, source.Shops, source.ShopStock,
            technologies, stages, entries, ingredients);
    }

    /// <summary>
    /// 프로젝트 경로에서 필수 CSV TextAsset을 읽어 순수 C# CSV 원본으로 변환합니다.
    /// </summary>
    /// <param name="fileName">읽을 CSV 파일 이름입니다.</param>
    /// <returns>파일 이름과 전체 문자열이 포함된 CSV 원본입니다.</returns>
    private static GameDataCsvFile LoadRequiredCsv(string fileName)
    {
        string assetPath = $"{CSV_DIRECTORY_PATH}{fileName}";
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

        Assert.That(textAsset, Is.Not.Null, $"필수 CSV TextAsset을 찾을 수 없습니다. path={assetPath}");
        return new GameDataCsvFile(textAsset.name, textAsset.text);
    }
}
