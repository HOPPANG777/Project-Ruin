//=============================================================================
// File:        GameBootstrapper.cs
//
// Description: 기능 모듈을 기존 의존 순서대로 조립하고 초기화 상태를 관리한다.
//
// History:     09/23/2026 + GameRoot 초기화 조립 책임 분리 -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Threading;

//========================================
// Project Includes
//========================================
using Cysharp.Threading.Tasks;

//=============================================================================
//
// Synopsis:   GameBootstrapper;
//                - 기능 모듈의 전체 초기화 순서와 실패·취소·완료 상태를 관리
//=============================================================================
internal sealed class GameBootstrapper
{
    private readonly CoreBootstrapModule _coreModule;
    private readonly GameplayBootstrapModule _gameplayModule;
    private readonly MuseumBootstrapModule _museumModule;
    private readonly UiBootstrapModule _uiModule;
    private readonly SceneFlowBootstrapModule _sceneFlowModule;

    private bool _hasStarted;

    /// <summary>모든 필수 모듈과 도감 UI 초기화가 완료됐는지 반환합니다.</summary>
    internal bool IsInitialized { get; private set; }

    //=============================================================================
    // GameBootstrapper.GameBootstrapper
    //=============================================================================
    // Description: 직렬화된 기능 모듈을 전체 초기화 조립자에 연결한다.
    //
    // Parameters:  각 기능별 Bootstrap Module.
    //
    // Return:      N/A.
    //
    //=============================================================================
    internal GameBootstrapper(
        CoreBootstrapModule coreModule,
        GameplayBootstrapModule gameplayModule,
        MuseumBootstrapModule museumModule,
        UiBootstrapModule uiModule,
        SceneFlowBootstrapModule sceneFlowModule)
    {
        _coreModule = coreModule;
        _gameplayModule = gameplayModule;
        _museumModule = museumModule;
        _uiModule = uiModule;
        _sceneFlowModule = sceneFlowModule;
    }

    //=============================================================================
    // GameBootstrapper.InitializeAsync
    //=============================================================================
    // Description: 저장 복구와 기능 모듈을 기존 의존 순서대로 한 번만 초기화한다.
    //
    // Parameters:  ( CancellationToken cancellationToken )
    //
    // Return:      UniTask<bool> — 필수 초기화가 모두 완료되면 true
    //
    //=============================================================================
    /// <summary>저장 복구와 기능 모듈을 기존 의존 순서대로 한 번만 초기화합니다.</summary>
    /// <param name="cancellationToken">GameRoot 생명주기와 연결된 취소 토큰입니다.</param>
    /// <returns>필수 초기화가 모두 완료되면 true입니다.</returns>
    internal async UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        if (_hasStarted)
        {
            return IsInitialized;
        }

        _hasStarted = true;
        IsInitialized = false;

        if (_coreModule == null || _gameplayModule == null || _museumModule == null
            || _uiModule == null || _sceneFlowModule == null)
        {
            UnityEngine.Debug.LogError("[GameBootstrapper] 기능 모듈 연결이 누락됐습니다.");
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_coreModule.TryInitializeGameData()) return false;

            if (!await _coreModule.LoadProgressAsync(cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    UnityEngine.Debug.LogError(
                        "[GameBootstrapper] 진행 상태 불러오기에 실패하여 초기화를 중단합니다.");
                }
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!_gameplayModule.TryInitializeRestoration(
                    _coreModule.GameData, _coreModule.Progress)) return false;
            if (!_museumModule.TryInitializeMuseum(
                    _coreModule.GameData, _coreModule.Progress,
                    _gameplayModule.RelicRestoration)) return false;
            if (!_gameplayModule.TryInitializeSkill(_coreModule.Progress)) return false;

            _uiModule.InitializeAltar(
                _gameplayModule.RelicRestoration, _coreModule.Progress,
                _coreModule.Localization);

            if (!_gameplayModule.TryInitializeInventory(
                    _coreModule.GameData, _coreModule.Localization,
                    _coreModule.Progress)) return false;
            if (!_museumModule.TryInitializeBottles(
                    _coreModule.GameData, _gameplayModule.Inventory,
                    _coreModule.Progress)) return false;

            _uiModule.InitializeMuseumBottle(
                _museumModule.MuseumBottles, _gameplayModule.Inventory,
                _coreModule.Localization);

            if (!_museumModule.TryInitializeRelicDisplays(
                    _coreModule.GameData, _gameplayModule.Inventory,
                    _coreModule.Progress)) return false;
            if (!_gameplayModule.TryInitializeCodex(
                    _coreModule.GameData.CodexData, _coreModule.Progress)) return false;

            // Codex의 구버전 보유 아이템 동기화가 끝난 뒤에만 최종 방을 평가합니다.
            if (!_museumModule.TryInitializeRewardRoom(
                    _coreModule.GameData.CodexData, _gameplayModule.Codex,
                    _coreModule.Progress)) return false;

            _uiModule.InitializeInventoryAndMuseum(
                _gameplayModule.Inventory, _museumModule.MuseumRelicDisplays);

            if (!_gameplayModule.TryInitializeShop(
                    _coreModule.GameData, _coreModule.Progress)) return false;
            _uiModule.InitializeShop(
                _gameplayModule.Shop, _gameplayModule.Inventory,
                _gameplayModule.Dialogue);

            if (!_gameplayModule.TryInitializeQuestAndDialogue(
                    _coreModule.GameData, _coreModule.Localization,
                    _coreModule.Progress)) return false;
            if (!_sceneFlowModule.TryInitialize(_coreModule.Progress)) return false;
            if (!_uiModule.TryInitializeCodex(
                    _coreModule.GameData.CodexData, _gameplayModule.Codex,
                    _gameplayModule.Inventory, _coreModule.Localization)) return false;

            cancellationToken.ThrowIfCancellationRequested();
            IsInitialized = true;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
