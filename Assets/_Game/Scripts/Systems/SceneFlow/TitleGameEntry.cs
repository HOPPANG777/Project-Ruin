//=============================================================================
// File: TitleGameEntry.cs
// Description: 타이틀의 진행 복구·초기화와 게임 진입을 담당한다.
// History: 10/16/2026 + 표시 책임 분리 및 의존성 주입 -- Codex
//=============================================================================
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>GameRoot가 없는 타이틀 씬의 직렬화된 구성을 UI 계약에 주입합니다.</summary>
public sealed class TitleGameEntry : MonoBehaviour, ITitleGameService
{
    [Tooltip("게임 진입 계약을 주입할 타이틀 표시부입니다.")]
    [SerializeField] private TitleScreenPresenter _view;
    [Tooltip("타이틀 씬에서 사용할 진행 상태 구현체입니다.")]
    [SerializeField] private ProgressController _progress;
    [Tooltip("현재 저장 형식의 복원 시작 씬입니다.")]
    [SerializeField] private string _firstGameplayScene = "Chapter0";

    // 실행 중에는 진행 상태의 세부 구현을 사용하지 않습니다.
    private IProgressService _progressService;

    /// <summary>타이틀 진입 시 확인한 저장 유무입니다.</summary>
    public bool HasSavedData { get; private set; }

    // 타이틀은 GameRoot 생성 전이므로 씬의 구성 요소로 초기 연결합니다.
    private void Awake()
    {
        if (_progressService == null) Init(_progress, _progress != null && _progress.HasSavedData);
        if (_view != null) _view.Init(this);
    }

    //=============================================================================
    // Description: 진행 의존성을 주입한다. Parameters: progress/hasSavedData. Return: void.
    //=============================================================================
    /// <summary>테스트나 Bootstrap Module에서 대체 진행 서비스를 주입할 수 있습니다.</summary>
    public void Init(IProgressService progress, bool hasSavedData)
    {
        _progressService = progress;
        HasSavedData = progress != null && hasSavedData;
    }

    //=============================================================================
    // Description: 진행을 준비하고 씬을 연다. Parameters: newGame/cancellationToken. Return: UniTask&lt;bool&gt;.
    //=============================================================================
    /// <summary>씬 설정 검증이 끝나기 전에는 기존 진행을 초기화하지 않습니다.</summary>
    public async UniTask<bool> TryEnterAsync(bool newGame, CancellationToken cancellationToken)
    {
        if (_progressService == null || string.IsNullOrWhiteSpace(_firstGameplayScene)
            || !Application.CanStreamedLevelBeLoaded(_firstGameplayScene)) return false;
        cancellationToken.ThrowIfCancellationRequested();
        if (newGame) _progressService.Reset();
        bool ready = newGame
            ? await _progressService.SaveAsync(cancellationToken)
            : await _progressService.LoadAsync(cancellationToken);
        if (!ready) return false;
        HasSavedData = true;
        await SceneManager.LoadSceneAsync(_firstGameplayScene, LoadSceneMode.Single)
            .ToUniTask(cancellationToken: cancellationToken);
        return true;
    }

    //=============================================================================
    // Description: 실행을 종료한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>에디터에서는 플레이 모드만 종료합니다.</summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
