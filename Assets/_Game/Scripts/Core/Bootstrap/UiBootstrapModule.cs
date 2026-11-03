//=============================================================================
// File:        UiBootstrapModule.cs
//
// Description: 전역 Presenter 참조, 단계별 UI 연결과 플레이어 메뉴 바인딩을 소유한다.
//
// History:     09/23/2026 + GameRoot UI 책임 분리 -- Codex
//              10/22/2026 + 술 제조 Presenter 연결 추가 -- Claude
//
//=============================================================================

using System;
using UnityEngine;

/// <summary>전역 UI Presenter 초기화와 활성 플레이어 메뉴 연결을 소유합니다.</summary>
[Serializable]
internal sealed class UiBootstrapModule
{
    [Tooltip("제단 기술 목록과 반납 확인 UI를 중개하는 Presenter입니다.")]
    [SerializeField] private AltarPresenter _altarPresenter;

    [Tooltip("술병의 투입 가능한 술 선택 UI를 중개하는 Presenter입니다.")]
    [SerializeField] private MuseumBottlePresenter _museumBottlePresenter;

    [Tooltip("전시대의 유물 전용 선택 UI를 중개하는 Presenter입니다.")]
    [SerializeField] private MuseumRelicPresenter _museumRelicPresenter;

    [Tooltip("인벤토리 UI와 선택 입력을 중개하는 Presenter입니다.")]
    [SerializeField] private InventoryPresenter _inventoryPresenter;

    [Tooltip("초기화 완료 후 한 번 연결할 도감 입력과 창 전환 Presenter입니다.")]
    [SerializeField] private CodexNavigationPresenter _codexNavigationPresenter;

    [Tooltip("ITEM_COIN 아이콘과 현재 수량을 표시하는 HUD Presenter입니다.")]
    [SerializeField] private CurrencyHudPresenter _currencyHudPresenter;

    [Tooltip("상점 목록, 설명과 거래 창 입력을 중개하는 Presenter입니다.")]
    [SerializeField] private ShopPresenter _shopPresenter;

    [Tooltip("술 제조 팝업 입력을 중개하는 Presenter입니다.")]
    [SerializeField] private BrewingPresenter _brewingPresenter;

    /// <summary>씬 술병 상호작용이 사용할 선택 UI Presenter입니다.</summary>
    internal MuseumBottlePresenter MuseumBottlePresenter => _museumBottlePresenter;
    /// <summary>씬 전시대가 사용할 유물 전용 선택 UI Presenter입니다.</summary>
    internal MuseumRelicPresenter MuseumRelicPresenter => _museumRelicPresenter;
    /// <summary>술 제조 팝업을 여는 Presenter입니다.</summary>
    internal BrewingPresenter BrewingPresenter => _brewingPresenter;

    /// <summary>기술 반환 UI에 재건·Progress·Localization 서비스를 연결합니다.</summary>
    internal void InitializeAltar(
        IRelicRestorationService restoration,
        IProgressService progress,
        ILocalizationService localization)
    {
        _altarPresenter?.Init(restoration, progress, localization);
    }

    /// <summary>술병 선택 UI에 박물관·인벤토리·Localization 서비스를 연결합니다.</summary>
    internal void InitializeMuseumBottle(
        IMuseumBottleService bottles,
        IInventoryService inventory,
        ILocalizationService localization)
    {
        _museumBottlePresenter?.Init(bottles, inventory, localization);
    }

    /// <summary>인벤토리 복구 뒤 인벤토리, 유물 선택, 화폐 HUD를 연결합니다.</summary>
    internal void InitializeInventoryAndMuseum(
        IInventoryService inventory,
        IMuseumRelicDisplayService relicDisplays)
    {
        _inventoryPresenter?.Init(inventory);
        _museumRelicPresenter?.Init(relicDisplays, inventory, _inventoryPresenter);
        _currencyHudPresenter?.Init(inventory);
    }

    /// <summary>상점 UI에 거래·인벤토리·대화 서비스를 연결합니다.</summary>
    internal void InitializeShop(
        ShopController shop,
        IInventoryService inventory,
        IDialogueService dialogue)
    {
        _shopPresenter?.Init(shop, inventory, dialogue);
    }

    /// <summary>술 제조 UI에 제조 서비스와 인벤토리를 연결합니다.</summary>
    internal void InitializeBrewing(IBrewingService brewing, IInventoryService inventory)
    {
        _brewingPresenter?.Init(brewing, inventory);
    }

    /// <summary>필수 도감 UI와 인벤토리 전환 연결을 마지막 단계에서 활성화합니다.</summary>
    internal bool TryInitializeCodex(
        ICodexDataRepository data,
        ICodexService codex,
        IInventoryService inventory,
        ILocalizationService localization)
    {
        if (_codexNavigationPresenter != null
            && _codexNavigationPresenter.TryInitialize(data, codex, inventory, localization))
        {
            return true;
        }

        Debug.LogError("[UiBootstrapModule] 도감 UI 초기화 연결이 누락되거나 유효하지 않습니다.");
        return false;
    }

    /// <summary>활성 플레이어가 제공한 입력 참조를 도감 메뉴에 전달합니다.</summary>
    internal void BindMenuPlayer(
        PlayerInteractionController interaction, PlayerMoveController movement)
    {
        _codexNavigationPresenter?.BindPlayer(interaction, movement);
    }

    /// <summary>현재 연결과 일치하는 플레이어만 도감 메뉴에서 해제합니다.</summary>
    internal void UnbindMenuPlayer(
        PlayerInteractionController interaction, PlayerMoveController movement)
    {
        _codexNavigationPresenter?.UnbindPlayer(interaction, movement);
    }
}
