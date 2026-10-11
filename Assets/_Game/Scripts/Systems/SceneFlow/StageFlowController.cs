using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

//=============================================================================
// File:        StageFlowController.cs
// Description: 스테이지 저장, 이전 씬 해제와 목적지 씬 로드를 순서대로 처리합니다.
// History:     10/11/2026 + 마지막 스테이지 씬 언로드 보호 및 직접 실행 상태 식별 -- Codex
//              10/11/2026 + 스테이지 언로드 전 플레이어 보존 및 목적지 씬 이관 -- Codex
//==============================================================================
// NOTE:
//  - 싱글톤이 아닙니다. 외부 접근은 반드시 GameRoot.Instance.StageFlow(IStageFlowService) 경유.
//  - 코루틴 금지. 모든 비동기는 UniTask + GetCancellationTokenOnDestroy() 사용.
//  - 책임 한정: 퍼즐 연산/UI 렌더/플레이어 세부 상태 제어는 본 클래스에 두지 않습니다.
//    실제 데이터 Capture/복구는 이벤트(OnStageCaptureRequested, OnStageRestoreRequested)로 외부에 위임합니다.
//==============================================================================
[DisallowMultipleComponent]
public class StageFlowController : MonoBehaviour, IStageFlowService
{
    //==============================================================================
    // 직렬화 필드 (인스펙터 노출)
    //==============================================================================

    [Header("Transition")]
    [Tooltip("씬 전환 시 시각적 연출(페이드, 로딩 UI)을 담당하는 Presenter입니다.")]
    [SerializeField]
    private SceneTransitionPresenter _sceneTransitionPresenter;

    [Header("Scene")]
    [Tooltip("시스템 매니저가 상주하는 상시 씬의 이름입니다. (언로드 대상에서 제외)")]
    [SerializeField]
    private string _persistentSceneName = "Scene_Persistent";

    [Header("Scene")]
    [Tooltip("씬 ID/스테이지 번호와 실제 Unity 씬 이름을 매핑하는 카탈로그입니다.")]
    [SerializeField]
    private SceneCatalogSO _sceneCatalog;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 전환 파이프라인이 진행 중인지 여부를 담는 내부 플래그입니다.
    private bool _isTransitioning;

    // 시각 연출 서비스에 대한 캐시 참조입니다. (인터페이스 경유 접근용)
    private ISceneTransitionService _sceneTransition;

    // 씬 전환 직전 Dirty 진행 상태를 저장할 서비스입니다.
    private IProgressService _progressService;

    // 현재 로드되어 있는 스테이지 번호입니다. 스테이지가 없으면 -1입니다.
    private int _currentStageNumber = -1;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>현재 스테이지 전환이 진행 중인지 여부입니다.</summary>
    public bool IsTransitioning
    {
        get => _isTransitioning;
        private set => _isTransitioning = value;
    }

    /// <summary>현재 로드되어 있는 스테이지 번호입니다. 없으면 -1입니다.</summary>
    public int CurrentStageNumber
    {
        get => _currentStageNumber;
    }

    //==============================================================================
    // 이벤트 (데이터 Capture/복구 위임)
    //==============================================================================

    /// <summary>
    /// 스테이지 언로드 직전 현재 씬 상태를 저장해야 할 때 발생하는 이벤트입니다.
    /// </summary>
    public event Action<int> OnStageCaptureRequested;

    /// <summary>
    /// 새 스테이지 로드 직후 데이터 복구가 필요할 때 발생하는 이벤트입니다.
    /// 실제 스폰/플레이어 배치 로직은 이 이벤트를 구독한 외부에서 처리합니다.
    /// </summary>
    public event Action<SceneTransferData> OnStageRestoreRequested;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>
    /// 직렬화된 Presenter 참조를 인터페이스 멤버로 캐시합니다.
    /// </summary>
    private void Awake()
    {
        _sceneTransition = _sceneTransitionPresenter;
    }

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다. 저장 서비스와 Presenter를 준비합니다.
    /// </summary>
    /// <param name="progressService">씬 전환 체크포인트에서 사용할 진행 상태 서비스입니다.</param>
    public void Init(IProgressService progressService)
    {
        _progressService = progressService;
        UpdateCurrentStageNumber(SceneManager.GetActiveScene());

        // Presenter가 연결되지 않았으면 초기화할 대상이 없으므로 종료합니다.
        if (_sceneTransition == null)
        {
            return;
        }

        _sceneTransition.Init();
    }

    //==============================================================================
    // 공개 API (IStageFlowService)
    //==============================================================================

    //=============================================================================
    // StageFlowController.FadeThroughAsync
    //=============================================================================
    // Description: 화면을 암전한 동안 지정한 작업을 실행한 뒤 화면을 복귀시킨다.
    //
    // Parameters:  ( Action coveredAction )
    //
    // Return:      UniTask — 페이드 전환 작업
    //
    //=============================================================================
    /// <summary>화면을 암전한 동안 지정한 작업을 실행한 뒤 화면을 복귀시킵니다.</summary>
    /// <param name="coveredAction">화면이 완전히 가려진 상태에서 실행할 작업입니다.</param>
    /// <returns>페이드 전환 완료를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask FadeThroughAsync(Action coveredAction)
    {
        if (coveredAction == null)
        {
            Debug.LogWarning("[StageFlowController] 암전 중 실행할 작업이 없습니다.");
            return;
        }

        if (!TryBeginTransition())
        {
            return;
        }

        CancellationToken token = this.GetCancellationTokenOnDestroy();

        try
        {
            await _sceneTransition.FadeInAsync(token);

            try
            {
                coveredAction.Invoke();
            }
            finally
            {
                // 암전 중 작업이 실패해도 화면이 검게 고정되지 않도록 복귀를 보장합니다.
                await _sceneTransition.FadeOutAsync(token);
            }
        }
        finally
        {
            IsTransitioning = false;
        }
    }

    /// <summary>
    /// 지정한 스테이지 번호로 전환합니다. (카탈로그 조회 → 페이드 인 → 언로드 → Additive 로드 → 데이터 복구 → 페이드 아웃)
    /// </summary>
    /// <param name="stageNumber">전환할 스테이지 번호입니다. 카탈로그에서 실제 씬 이름을 조회합니다.</param>
    /// <param name="data">다음 씬으로 전달/보존할 데이터입니다.</param>
    /// <returns>전환 파이프라인 완료를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask ChangeStageAsync(int stageNumber, SceneTransferData data)
    {
        // 음수 스테이지 번호는 잘못된 입력이므로 안전하게 차단합니다.
        if (stageNumber < 0)
        {
            Debug.LogWarning($"[StageFlowController] 잘못된 스테이지 번호입니다: {stageNumber}");
            return;
        }

        // 씬 카탈로그가 연결되지 않으면 실제 씬 이름을 찾을 수 없으므로 전환을 중단합니다.
        if (_sceneCatalog == null)
        {
            Debug.LogWarning("[StageFlowController] SceneCatalogSO가 연결되지 않았습니다.");
            return;
        }

        // 스테이지 번호로 실제 씬 엔트리를 조회합니다.
        if (!_sceneCatalog.TryGetByStageNumber(stageNumber, out SceneCatalogSO.SceneEntry entry))
        {
            Debug.LogWarning($"[StageFlowController] 등록되지 않은 스테이지 번호입니다: {stageNumber}");
            return;
        }

        // 오브젝트 파괴 시 비동기 흐름이 함께 취소되도록 토큰을 연동합니다.
        CancellationToken token = this.GetCancellationTokenOnDestroy();

        // 스테이지는 상시 씬을 유지한 채 Additive 방식으로 전환합니다.
        await RunTransitionAsync(entry.SceneName, LoadSceneMode.Additive, data, stageNumber, token);
    }

    /// <summary>
    /// 등록된 씬 ID를 기준으로 일반 씬(타이틀/로비/엔딩 등)으로 전환합니다.
    /// </summary>
    /// <param name="sceneId">씬 카탈로그에 등록된 안정적인 씬 ID입니다. (예: "TITLE")</param>
    /// <param name="data">다음 씬으로 전달/보존할 데이터입니다. 필요 없으면 null을 전달합니다.</param>
    /// <returns>전환 파이프라인 완료를 await 할 수 있는 UniTask입니다.</returns>
    public async UniTask ChangeSceneAsync(string sceneId, SceneTransferData data)
    {
        // 비어 있는 씬 ID는 조회할 수 없으므로 안전하게 차단합니다.
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            Debug.LogWarning("[StageFlowController] 비어 있는 씬 ID로 전환할 수 없습니다.");
            return;
        }

        // 씬 카탈로그가 연결되지 않으면 실제 씬 이름을 찾을 수 없으므로 전환을 중단합니다.
        if (_sceneCatalog == null)
        {
            Debug.LogWarning("[StageFlowController] SceneCatalogSO가 연결되지 않았습니다.");
            return;
        }

        // 안정적인 씬 ID로 실제 씬 엔트리를 조회합니다.
        if (!_sceneCatalog.TryGetById(sceneId, out SceneCatalogSO.SceneEntry entry))
        {
            Debug.LogWarning($"[StageFlowController] 등록되지 않은 씬 ID입니다: {sceneId}");
            return;
        }

        // 오브젝트 파괴 시 비동기 흐름이 함께 취소되도록 토큰을 연동합니다.
        CancellationToken token = this.GetCancellationTokenOnDestroy();

        // 타이틀/로비/엔딩 같은 일반 씬은 Single 방식으로 전체 교체합니다.
        await RunTransitionAsync(entry.SceneName, LoadSceneMode.Single, data, -1, token);
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>
    /// 씬 로드 방식에 맞춰 공통 씬 전환 파이프라인을 실행합니다.
    /// </summary>
    /// <param name="sceneName">Unity에 등록된 실제 씬 이름입니다.</param>
    /// <param name="loadMode">씬 로드 방식입니다. 스테이지는 Additive, 일반 씬은 Single을 사용합니다.</param>
    /// <param name="data">복구에 사용할 전달 데이터입니다.</param>
    /// <param name="destinationStageNumber">Additive 전환 시 목적지 스테이지 번호입니다. 해당 없으면 -1입니다.</param>
    /// <param name="token">취소 토큰입니다.</param>
    private async UniTask RunTransitionAsync(
        string sceneName,
        LoadSceneMode loadMode,
        SceneTransferData data,
        int destinationStageNumber,
        CancellationToken token)
    {
        // 비어 있는 씬 이름은 Unity 로드 요청이 실패하므로 사전에 차단합니다.
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[StageFlowController] 비어 있는 씬 이름으로 전환할 수 없습니다.");
            return;
        }

        // 로드할 수 없는 목적지 때문에 현재 스테이지부터 사라지는 상황을 막습니다.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[StageFlowController] 빌드에 등록되지 않았거나 로드할 수 없는 씬입니다: {sceneName}");
            return;
        }

        if (!TryBeginTransition())
        {
            return;
        }

        Scene previousScene = SceneManager.GetActiveScene();
        Scene transitionScene = default;
        GameObject transferredPlayer = null;
        try
        {
            // 1) 화면 암전: alpha 0 → 1 (로딩 UI는 아직 비활성).
            await _sceneTransition.FadeInAsync(token);

            // 2) Additive 스테이지 전환: 떠나기 전 현재 스테이지 상태를 Capture합니다.
            if (loadMode == LoadSceneMode.Additive)
            {
                UpdateCurrentStageNumber(previousScene);
                ExecuteStageCapture();
            }

            // Dirty가 없다면 실제 파일 쓰기 없이 즉시 통과합니다.
            if (_progressService != null)
            {
                await _progressService.SaveAsync(token);
            }

            if (loadMode == LoadSceneMode.Additive)
            {
                transferredPlayer = PreserveStagePlayer(previousScene);
                transitionScene = CreateTransitionSceneIfNeeded(previousScene);
                await UnloadSceneIfNeededAsync(previousScene, token);
            }

            // 3) 새 씬 로드: AsyncOperation을 Presenter가 모니터링하며 로딩 UI를 단계적으로 노출합니다.
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, loadMode);
            await _sceneTransition.MonitorTransitionAsync(loadOperation, token);

            // Additive 모드라면 방금 로드한 씬을 액티브 씬으로 지정합니다.
            if (loadMode == LoadSceneMode.Additive)
            {
                SetActiveSceneIfLoaded(sceneName, transferredPlayer);
            }

            // 4) Additive 전환 시 현재 스테이지 번호를 갱신합니다.
            if (loadMode == LoadSceneMode.Additive && destinationStageNumber >= 0)
            {
                _currentStageNumber = destinationStageNumber;
            }
            else if (loadMode == LoadSceneMode.Single)
            {
                _currentStageNumber = -1;
            }

            await ReleaseTransitionSceneAsync(transitionScene);

            // 5) 데이터 복구: 새 씬이 완전히 로드된 직후 복구 절차를 실행합니다.
            ExecuteDataRestoration(data, destinationStageNumber);

            // 6) 화면 복귀: alpha 1 → 0.
            await _sceneTransition.FadeOutAsync(token);
        }
        finally
        {
            try
            {
                // 실패나 취소가 발생해도 다른 로드된 씬이 있으면 임시 씬을 정리합니다.
                await ReleaseTransitionSceneAsync(transitionScene);
            }
            finally
            {
                IsTransitioning = false;
            }
        }
    }

    /// <summary>
    /// 전환 중복과 Presenter 연결 상태를 검사하고 전환 상태를 선점합니다.
    /// </summary>
    /// <returns>새 전환을 시작할 수 있으면 true입니다.</returns>
    private bool TryBeginTransition()
    {
        // 먼저 시작한 전환만 상태를 선점해 다중 트리거의 경쟁 상태를 막습니다.
        if (_isTransitioning)
        {
            return false;
        }

        if (_sceneTransition == null)
        {
            Debug.LogWarning("[StageFlowController] SceneTransitionPresenter가 연결되지 않았습니다.");
            return false;
        }

        IsTransitioning = true;
        return true;
    }

    // 플레이어만 잠시 보존하여 맵은 제거하고 캐릭터의 체력·입력·카메라 참조는 유지합니다.
    private GameObject PreserveStagePlayer(Scene previousScene)
    {
        if (!previousScene.IsValid() || !previousScene.isLoaded) return null;
        foreach (GameObject root in previousScene.GetRootGameObjects())
        {
            UnitPlayer player = root.GetComponentInChildren<UnitPlayer>();
            if (player == null) continue;
            player.transform.SetParent(null, true);
            DontDestroyOnLoad(player.gameObject);
            return player.gameObject;
        }
        return null;
    }

    /// <summary>
    /// 전환 시작 시 기록한 이전 씬이 상시 씬이 아니라면 비동기로 언로드합니다.
    /// </summary>
    /// <param name="previousScene">떠나는 스테이지 씬입니다.</param>
    /// <param name="token">취소 토큰입니다.</param>
    private async UniTask UnloadSceneIfNeededAsync(Scene previousScene, CancellationToken token)
    {
        // 상시 씬(Scene_Persistent)은 시스템 매니저가 머무는 곳이므로 절대 언로드하지 않습니다.
        if (previousScene.name == _persistentSceneName)
        {
            return;
        }

        // 유효하게 로드된 씬일 때만 언로드를 시도합니다.
        if (!previousScene.IsValid() || !previousScene.isLoaded)
        {
            return;
        }

        // 이전 액티브 씬을 비동기로 내리고, 긴 언로드 시 Presenter가 로딩 UI를 표시합니다.
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(previousScene);
        if (unloadOperation == null)
        {
            throw new InvalidOperationException($"[StageFlowController] 이전 씬을 언로드할 수 없습니다: {previousScene.name}");
        }
        await _sceneTransition.MonitorTransitionAsync(unloadOperation, token);
    }

    // DontDestroyOnLoad의 GameRoot만 있어도 Unity는 마지막 일반 씬의 언로드를 허용하지 않습니다.
    private Scene CreateTransitionSceneIfNeeded(Scene previousScene)
    {
        if (!previousScene.IsValid() || !previousScene.isLoaded
            || previousScene.name == _persistentSceneName || SceneManager.sceneCount > 1)
        {
            return default;
        }
        return SceneManager.CreateScene("__StageFlowTransition_" + GetInstanceID());
    }

    // 이미 시작한 언로드는 전환 토큰이 취소되더라도 끝까지 정리합니다.
    private static async UniTask ReleaseTransitionSceneAsync(Scene transitionScene)
    {
        if (!transitionScene.IsValid() || !transitionScene.isLoaded || SceneManager.sceneCount <= 1)
            return;
        AsyncOperation operation = SceneManager.UnloadSceneAsync(transitionScene);
        if (operation != null) await operation.ToUniTask();
    }

    // Chapter 씬을 직접 실행한 첫 전환에서도 떠나는 스테이지 상태를 저장합니다.
    private void UpdateCurrentStageNumber(Scene scene)
    {
        if (scene.IsValid() && _sceneCatalog != null
            && _sceneCatalog.TryGetBySceneName(scene.name, out SceneCatalogSO.SceneEntry entry)
            && entry.SceneCategory == SceneCategory.STAGE)
        {
            _currentStageNumber = entry.StageNumber;
        }
    }

    /// <summary>
    /// 지정한 이름의 씬이 로드되어 있으면 액티브 씬으로 설정합니다.
    /// </summary>
    /// <param name="sceneName">액티브로 지정할 씬 이름입니다.</param>
    /// <param name="transferredPlayer">이전 씬에서 보존한 플레이어입니다.</param>
    private void SetActiveSceneIfLoaded(string sceneName, GameObject transferredPlayer)
    {
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);

        // 정상적으로 로드되었을 때만 액티브 씬으로 전환합니다. (라이팅/인스턴스 기준 씬)
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
            if (transferredPlayer != null)
                SceneManager.MoveGameObjectToScene(transferredPlayer, loadedScene);
        }
    }

    /// <summary>
    /// 스테이지 언로드 직전 현재 스테이지 상태 Capture 절차를 실행합니다.
    /// </summary>
    private void ExecuteStageCapture()
    {
        // 현재 스테이지가 없으면 Capture할 대상이 없습니다.
        if (_currentStageNumber < 0)
        {
            return;
        }

        OnStageCaptureRequested?.Invoke(_currentStageNumber);
    }

    /// <summary>
    /// 새 스테이지 로드 직후 데이터 복구 절차를 실행합니다.
    /// </summary>
    /// <param name="data">복구에 사용할 전달 데이터입니다.</param>
    /// <param name="destinationStageNumber">목적지 스테이지 번호입니다.</param>
    private void ExecuteDataRestoration(SceneTransferData data, int destinationStageNumber)
    {
        // Additive 스테이지 전환이 아니면 Restore 이벤트를 발생시키지 않습니다.
        if (destinationStageNumber < 0)
        {
            return;
        }

        // handoff 데이터가 없으면 목적지 스테이지 번호만 담은 임시 데이터를 생성합니다.
        if (data == null)
        {
            data = new SceneTransferData();
        }

        // enteringStageNumber가 비어 있으면 목적지 스테이지 번호로 채웁니다.
        if (data.enteringStageNumber < 0)
        {
            data.enteringStageNumber = destinationStageNumber;
        }

        // 실제 복구 로직은 본 컨트롤러의 책임이 아닙니다.
        // 결합도를 낮추기 위해 구독자(SceneStateBridge 등)에게 이벤트로 위임합니다.
        OnStageRestoreRequested?.Invoke(data);
    }
}
