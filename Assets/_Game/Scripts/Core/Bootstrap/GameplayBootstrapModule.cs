//=============================================================================
// File:        GameplayBootstrapModule.cs
//
// Description: 전역 Gameplay Controller 참조와 단계별 초기화를 소유한다.
//
// History:     09/23/2026 + GameRoot Gameplay 책임 분리 -- Codex
//
//=============================================================================

using System;
using UnityEngine;

/// <summary>Inventory, Skill, Codex, Quest, Dialogue, Shop 조립을 소유합니다.</summary>
[Serializable]
internal sealed class GameplayBootstrapModule
{
    [Tooltip("기술 획득·반납과 유적 재건 상태를 관리하는 컨트롤러입니다.")]
    [SerializeField] private RelicRestorationController _relicRestorationController;

    [Tooltip("도감 영구 발견 상태와 기술 해제 상태를 관리하는 컨트롤러입니다.")]
    [SerializeField] private CodexController _codexController;

    [Tooltip("환경 스킬의 등록 정보와 영구 획득 상태를 관리하는 컨트롤러입니다.")]
    [SerializeField] private SkillController _skillController;

    [Tooltip("퀘스트 상태 전환과 목표 및 보상 처리를 관리하는 컨트롤러입니다.")]
    [SerializeField] private QuestController _questController;

    [Tooltip("아이템 보관과 중첩 및 소모를 관리하는 컨트롤러입니다.")]
    [SerializeField] private InventoryController _inventoryController;

    [Tooltip("상점 재고와 구매·판매 거래를 관리하는 컨트롤러입니다.")]
    [SerializeField] private ShopController _shopController;

    [Tooltip("NPC 대화 그래프 진행과 대화 UI를 관리하는 컨트롤러입니다.")]
    [SerializeField] private DialogueController _dialogueController;

    /// <summary>유적 재건 시스템의 외부 공개 계약입니다.</summary>
    internal IRelicRestorationService RelicRestoration => _relicRestorationController;
    /// <summary>도감 발견과 해제 상태 시스템의 외부 공개 계약입니다.</summary>
    internal ICodexService Codex => _codexController;
    /// <summary>환경 스킬 획득 상태 시스템의 외부 공개 계약입니다.</summary>
    internal ISkillService Skill => _skillController;
    /// <summary>퀘스트 시스템의 외부 공개 계약입니다.</summary>
    internal IQuestService Quest => _questController;
    /// <summary>인벤토리 시스템의 외부 공개 계약입니다.</summary>
    internal IInventoryService Inventory => _inventoryController;
    /// <summary>상점 재고와 거래를 관리하는 컨트롤러입니다.</summary>
    internal ShopController Shop => _shopController;
    /// <summary>대화 시스템의 외부 공개 계약입니다.</summary>
    internal IDialogueService Dialogue => _dialogueController;

    /// <summary>Progress 복구 후 유적 재건 캐시를 초기화합니다.</summary>
    internal bool TryInitializeRestoration(
        IGameDataRepository gameData, IProgressService progress)
    {
        if (_relicRestorationController == null)
        {
            return true;
        }

        if (gameData?.CodexData != null && progress != null
            && _relicRestorationController.Init(gameData.CodexData, progress))
        {
            return true;
        }

        Debug.LogError("[GameplayBootstrapModule] 유적 재건 초기화에 실패했습니다.");
        return false;
    }

    /// <summary>Progress에 저장된 환경 스킬 상태를 초기화합니다.</summary>
    internal bool TryInitializeSkill(IProgressService progress)
    {
        if (_skillController == null)
        {
            return true;
        }

        if (_skillController.Init(progress))
        {
            return true;
        }

        Debug.LogError("[GameplayBootstrapModule] 환경 스킬 초기화에 실패했습니다.");
        return false;
    }

    /// <summary>저장된 인벤토리를 복구합니다.</summary>
    internal bool TryInitializeInventory(
        IGameDataRepository gameData,
        ILocalizationService localization,
        IProgressService progress)
    {
        if (_inventoryController == null)
        {
            return true;
        }

        if (gameData == null || progress == null)
        {
            Debug.LogError("[GameplayBootstrapModule] 인벤토리 초기화 의존성이 누락됐습니다.");
            return false;
        }

        _inventoryController.Init(gameData, localization, progress);
        return true;
    }

    /// <summary>인벤토리 복구 후 도감의 구버전 보유 아이템을 동기화합니다.</summary>
    internal bool TryInitializeCodex(
        ICodexDataRepository data, IProgressService progress)
    {
        if (_codexController == null)
        {
            return true;
        }

        if (_codexController.Init(
                data, _inventoryController, progress, _relicRestorationController))
        {
            return true;
        }

        Debug.LogError("[GameplayBootstrapModule] 도감 초기화에 실패했습니다.");
        return false;
    }

    /// <summary>게임 데이터와 인벤토리·Progress를 상점에 연결합니다.</summary>
    internal bool TryInitializeShop(
        IGameDataRepository gameData, IProgressService progress)
    {
        if (_shopController == null)
        {
            return true;
        }

        if (gameData == null || _inventoryController == null || progress == null)
        {
            Debug.LogError("[GameplayBootstrapModule] 상점 초기화 의존성이 누락됐습니다.");
            return false;
        }

        _shopController.Init(gameData, _inventoryController, progress);
        return true;
    }

    /// <summary>Quest를 먼저 준비한 뒤 이에 의존하는 Dialogue를 초기화합니다.</summary>
    internal bool TryInitializeQuestAndDialogue(
        IGameDataRepository gameData,
        ILocalizationService localization,
        IProgressService progress)
    {
        if ((_questController != null || _dialogueController != null)
            && (gameData == null || progress == null))
        {
            Debug.LogError("[GameplayBootstrapModule] Quest 또는 Dialogue 초기화 의존성이 누락됐습니다.");
            return false;
        }

        if (_questController != null)
        {
            _questController.Init(
                gameData, progress, _inventoryController, _relicRestorationController);
        }

        if (_dialogueController != null)
        {
            _dialogueController.Init(gameData, localization, progress, _questController);
        }
        return true;
    }
}
