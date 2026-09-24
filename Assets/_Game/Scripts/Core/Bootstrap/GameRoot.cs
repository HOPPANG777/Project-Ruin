//=============================================================================
// File:        GameRoot.cs
//
// Description: 유일한 전역 진입점과 기능 모듈의 수명주기 시작을 제공한다.
//
// History:     09/01/2026 + 도감 초기화 및 플레이어 메뉴 참조 연결 -- Codex
//              09/23/2026 + 기능 모듈과 Bootstrapper로 초기화 책임 분리 -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System.Threading;

//========================================
// Project Includes
//========================================
using Cysharp.Threading.Tasks;
using UnityEngine;

//=============================================================================
//
// Synopsis:   GameRoot;
//                - 프로젝트의 유일한 전역 싱글턴 진입점
//                - 기존 공개 서비스 API와 GameRoot 수명주기를 유지
//=============================================================================
public sealed class GameRoot : Singleton<GameRoot>
{
    //========================================
    // 직렬화 필드
    //========================================

    [Header("Bootstrap Modules")]
    [Tooltip("게임 데이터와 Progress 저장 복구를 소유하는 Core 모듈입니다.")]
    [SerializeField] private CoreBootstrapModule _coreModule = new CoreBootstrapModule();

    [Tooltip("Inventory, Skill, Codex, Quest, Dialogue, Shop 조립을 소유하는 모듈입니다.")]
    [SerializeField] private GameplayBootstrapModule _gameplayModule =
        new GameplayBootstrapModule();

    [Tooltip("박물관 벽화, 술병, 유물 전시와 최종 보상 방 조립을 소유하는 모듈입니다.")]
    [SerializeField] private MuseumBootstrapModule _museumModule = new MuseumBootstrapModule();

    [Tooltip("전역 Presenter 초기화와 플레이어 메뉴 바인딩을 소유하는 UI 모듈입니다.")]
    [SerializeField] private UiBootstrapModule _uiModule = new UiBootstrapModule();

    [Tooltip("StageFlow, SceneStateBridge와 Camera 연결을 소유하는 모듈입니다.")]
    [SerializeField] private SceneFlowBootstrapModule _sceneFlowModule =
        new SceneFlowBootstrapModule();

    //========================================
    // 내부 상태
    //========================================

    private GameBootstrapper _bootstrapper;

    //========================================
    // 공개 프로퍼티
    //========================================

    /// <summary>카메라 이동과 추적을 관리하는 컨트롤러입니다.</summary>
    public CameraController CameraController => _sceneFlowModule?.CameraController;
    /// <summary>CSV에서 초기화된 읽기 전용 게임 데이터 저장소입니다.</summary>
    public IGameDataRepository GameData => _coreModule?.GameData;
    /// <summary>CSV 문장 Key를 현재 언어 문자열로 변환하는 계약입니다.</summary>
    public ILocalizationService Localization => _coreModule?.Localization;
    /// <summary>게임 진행 상태 시스템의 외부 공개 계약입니다.</summary>
    public IProgressService Progress => _coreModule?.Progress;
    /// <summary>유적 재건 시스템의 외부 공개 계약입니다.</summary>
    public IRelicRestorationService RelicRestoration => _gameplayModule?.RelicRestoration;
    /// <summary>도감 발견과 해제 상태 시스템의 외부 공개 계약입니다.</summary>
    public ICodexService Codex => _gameplayModule?.Codex;
    /// <summary>박물관 벽화와 제단 목적지 시스템의 외부 공개 계약입니다.</summary>
    public IMuseumService Museum => _museumModule?.Museum;
    /// <summary>술병 진행과 투입 시스템의 외부 공개 계약입니다.</summary>
    public IMuseumBottleService MuseumBottles => _museumModule?.MuseumBottles;
    /// <summary>씬 술병 상호작용이 사용할 선택 UI Presenter입니다.</summary>
    public MuseumBottlePresenter MuseumBottlePresenter => _uiModule?.MuseumBottlePresenter;
    /// <summary>유물 전시대와 마일스톤 시스템의 외부 공개 계약입니다.</summary>
    public IMuseumRelicDisplayService MuseumRelicDisplays => _museumModule?.MuseumRelicDisplays;
    /// <summary>최종 보상 방 개방과 보상 수령 시스템의 외부 공개 계약입니다.</summary>
    public IMuseumRewardRoomService MuseumRewardRoom => _museumModule?.MuseumRewardRoom;
    /// <summary>씬 전시대가 사용할 유물 전용 선택 UI Presenter입니다.</summary>
    public MuseumRelicPresenter MuseumRelicPresenter => _uiModule?.MuseumRelicPresenter;
    /// <summary>환경 스킬 획득 상태 시스템의 외부 공개 계약입니다.</summary>
    public ISkillService Skill => _gameplayModule?.Skill;
    /// <summary>퀘스트 시스템의 외부 공개 계약입니다.</summary>
    public IQuestService Quest => _gameplayModule?.Quest;
    /// <summary>인벤토리 시스템의 외부 공개 계약입니다.</summary>
    public IInventoryService Inventory => _gameplayModule?.Inventory;
    /// <summary>상점 재고와 거래를 관리하는 컨트롤러입니다.</summary>
    public ShopController Shop => _gameplayModule?.Shop;
    /// <summary>대화 시스템의 외부 공개 계약입니다.</summary>
    public IDialogueService Dialogue => _gameplayModule?.Dialogue;
    /// <summary>스테이지 전환 시스템의 외부 공개 계약입니다.</summary>
    public IStageFlowService StageFlow => _sceneFlowModule?.StageFlow;
    /// <summary>저장 복구와 모든 필수 시스템·도감 UI 연결이 완료됐는지 반환합니다.</summary>
    public bool IsInitialized => _bootstrapper != null && _bootstrapper.IsInitialized;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // GameRoot.BindMenuPlayer
    //=============================================================================
    // Description: 활성 플레이어 입력 참조를 UI 모듈에 전달한다.
    //
    // Parameters:  ( PlayerInteractionController interaction,
    //                PlayerMoveController movement )
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>활성 플레이어가 제공한 입력 참조를 도감에 전달합니다.</summary>
    public void BindMenuPlayer(
        PlayerInteractionController interaction, PlayerMoveController movement)
    {
        _uiModule?.BindMenuPlayer(interaction, movement);
    }

    //=============================================================================
    // GameRoot.UnbindMenuPlayer
    //=============================================================================
    // Description: 현재 UI 연결과 일치하는 플레이어만 해제한다.
    //
    // Parameters:  ( PlayerInteractionController interaction,
    //                PlayerMoveController movement )
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>현재 연결과 일치하는 플레이어만 해제합니다.</summary>
    public void UnbindMenuPlayer(
        PlayerInteractionController interaction, PlayerMoveController movement)
    {
        _uiModule?.UnbindMenuPlayer(interaction, movement);
    }

    //******************************************************************************
    //
    // Unity Lifecycle
    //
    //******************************************************************************

    /// <summary>GameRoot 생명주기 토큰으로 전체 초기화를 한 번 시작합니다.</summary>
    private void Start()
    {
        InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 전체 조립은 테스트에서도 실제 실패·취소 경로를 검증할 수 있도록 별도 메서드로 유지합니다.
    private async UniTask InitializeAsync(CancellationToken cancellationToken)
    {
        if (_bootstrapper == null)
        {
            _bootstrapper = new GameBootstrapper(
                _coreModule, _gameplayModule, _museumModule, _uiModule, _sceneFlowModule);
        }

        await _bootstrapper.InitializeAsync(cancellationToken);
    }
}
