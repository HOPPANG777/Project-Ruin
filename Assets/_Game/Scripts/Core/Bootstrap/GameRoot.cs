using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
//==============================================================================
// 기능: class::GameRoot
// 설명: 전역 컨트롤러의 참조와 초기화 순서를 관리하는 프로젝트 유일 싱글턴
//==============================================================================
public sealed class GameRoot : Singleton<GameRoot>
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================
    [Header("Controller")]
    [Tooltip("카메라 이동과 추적을 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private CameraController _cameraController;
    [Tooltip("CSV TextAsset을 읽어 게임 데이터 저장소를 생성하는 컨트롤러입니다.")]
    [SerializeField]
    private GameDataController _gameDataController;
    [Tooltip("게임 진행 상태와 플래그를 보관하는 컨트롤러입니다.")]
    [SerializeField]
    private ProgressController _progressController;
    [Tooltip("기술 획득·반납과 유적 재건 상태를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private RelicRestorationController _relicRestorationController;
    [Tooltip("도감 영구 발견 상태와 기술 해제 상태를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private CodexController _codexController;
    [Tooltip("환경 스킬의 등록 정보와 영구 획득 상태를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private SkillController _skillController;
    [Tooltip("제단 기술 목록과 반납 확인 UI를 중개하는 Presenter입니다.")]
    [SerializeField]
    private AltarPresenter _altarPresenter;
    [Tooltip("퀘스트 상태 전환과 목표 및 보상 처리를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private QuestController _questController;
    [Tooltip("아이템 보관과 중첩 및 소모를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private InventoryController _inventoryController;
    [Tooltip("인벤토리 UI와 선택 입력을 중개하는 Presenter입니다.")]
    [SerializeField]
    private InventoryPresenter _inventoryPresenter;
    [Tooltip("ITEM_COIN 아이콘과 현재 수량을 표시하는 HUD Presenter입니다.")]
    [SerializeField]
    private CurrencyHudPresenter _currencyHudPresenter;
    [Tooltip("상점 재고와 구매·판매 거래를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private ShopController _shopController;
    [Tooltip("상점 목록, 설명과 거래 창 입력을 중개하는 Presenter입니다.")]
    [SerializeField]
    private ShopPresenter _shopPresenter;
    [Tooltip("NPC 대화 그래프 진행과 대화 UI를 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private DialogueController _dialogueController;
    [Tooltip("씬과 스테이지 전환 흐름을 관리하는 컨트롤러입니다.")]
    [SerializeField]
    private StageFlowController _stageFlowController;
    [Tooltip("씬 상태 Capture 및 Restore 이벤트를 진행 상태와 연결하는 브릿지입니다.")]
    [SerializeField]
    private SceneStateBridge _sceneStateBridge;

    //==============================================================================
    // 공개 프로퍼티
    //==============================================================================
    /// <summary>카메라 이동과 추적을 관리하는 컨트롤러입니다.</summary>
    public CameraController CameraController => _cameraController;
    /// <summary>CSV에서 초기화된 읽기 전용 게임 데이터 저장소입니다.</summary>
    public IGameDataRepository GameData => _gameDataController != null
        ? _gameDataController.Repository
        : null;
    /// <summary>CSV 문장 Key를 현재 언어 문자열로 변환하는 계약입니다.</summary>
    public ILocalizationService Localization => _gameDataController;
    /// <summary>게임 진행 상태 시스템의 외부 공개 계약입니다.</summary>
    public IProgressService Progress => _progressController;
    /// <summary>유적 재건 시스템의 외부 공개 계약입니다.</summary>
    public IRelicRestorationService RelicRestoration => _relicRestorationController;
    /// <summary>도감 발견과 해제 상태 시스템의 외부 공개 계약입니다.</summary>
    public ICodexService Codex => _codexController;
    /// <summary>환경 스킬 획득 상태 시스템의 외부 공개 계약입니다.</summary>
    public ISkillService Skill => _skillController;
    /// <summary>퀘스트 시스템의 외부 공개 계약입니다.</summary>
    public IQuestService Quest => _questController;
    /// <summary>인벤토리 시스템의 외부 공개 계약입니다.</summary>
    public IInventoryService Inventory => _inventoryController;
    /// <summary>상점 재고와 거래를 관리하는 컨트롤러입니다.</summary>
    public ShopController Shop => _shopController;
    /// <summary>대화 시스템의 외부 공개 계약입니다.</summary>
    public IDialogueService Dialogue => _dialogueController;
    /// <summary>스테이지 전환 시스템의 외부 공개 계약입니다.</summary>
    public IStageFlowService StageFlow => _stageFlowController;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 게임 데이터와 전역 컨트롤러를 의존 순서에 맞춰 초기화합니다.
    /// </summary>
    private void Start()
    {
        InitializeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 저장 데이터를 먼저 불러온 뒤 나머지 전역 컨트롤러를 순서대로 초기화합니다.
    /// </summary>
    /// <param name="cancellationToken">GameRoot 생명주기와 연결된 취소 토큰입니다.</param>
    private async UniTask InitializeAsync(CancellationToken cancellationToken)
    {
        if (!TryInitializeGameData())
        {
            return;
        }

        if (_progressController != null)
        {
            await _progressController.LoadAsync(cancellationToken);
        }

        if (_relicRestorationController != null && _progressController != null)
        {
            _relicRestorationController.Init(GameData.CodexData, _progressController);
        }

        if (_skillController != null && _progressController != null)
        {
            _skillController.Init(_progressController);
        }

        if (_altarPresenter != null)
        {
            _altarPresenter.Init(
                _relicRestorationController,
                _progressController,
                _gameDataController);
        }

        if (_inventoryController != null)
        {
            _inventoryController.Init(GameData, _gameDataController, _progressController);
        }

        if (_codexController != null)
        {
            _codexController.Init(
                GameData.CodexData,
                _inventoryController,
                _progressController,
                _relicRestorationController);
        }

        if (_inventoryPresenter != null)
        {
            _inventoryPresenter.Init(_inventoryController);
        }

        if (_currencyHudPresenter != null)
        {
            _currencyHudPresenter.Init(_inventoryController);
        }

        if (_shopController != null)
        {
            _shopController.Init(GameData, _inventoryController, _progressController);
        }

        if (_shopPresenter != null)
        {
            _shopPresenter.Init(_shopController, _inventoryController, _dialogueController);
        }

        if (_questController != null)
        {
            _questController.Init(
                GameData,
                _progressController,
                _inventoryController,
                _relicRestorationController);
        }

        if (_dialogueController != null)
        {
            _dialogueController.Init(
                GameData,
                _gameDataController,
                _progressController,
                _questController);
        }

        if (_stageFlowController == null)
        {
            Debug.LogWarning("[GameRoot] StageFlowController가 연결되지 않았습니다.");
            return;
        }
        _stageFlowController.Init(_progressController);

        if (_sceneStateBridge != null && _progressController != null)
        {
            _sceneStateBridge.Init(_stageFlowController, _progressController);
        }

        if (_cameraController == null)
        {
            Debug.LogWarning("[GameRoot] CameraController가 연결되지 않았습니다.");
            return;
        }
        _cameraController.Init();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// CSV 게임 데이터 컨트롤러를 가장 먼저 초기화합니다.
    /// </summary>
    /// <returns>검증된 게임 데이터 저장소를 생성했으면 true입니다.</returns>
    private bool TryInitializeGameData()
    {
        if (_gameDataController == null)
        {
            Debug.LogError("[GameRoot] GameDataController가 연결되지 않았습니다.");
            return false;
        }

        if (_gameDataController.TryInit(out GameDataCsvLoadError loadError))
        {
            return true;
        }

        string errorMessage = loadError != null
            ? loadError.ToString()
            : "알 수 없는 CSV 게임 데이터 초기화 오류입니다.";
        Debug.LogError($"[GameRoot] 게임 데이터 초기화에 실패했습니다. {errorMessage}");
        return false;
    }
}
