//=============================================================================
// File: PausePopupView.cs
// Description: 일시정지 메뉴와 하위 팝업의 표시 상태만 관리한다.
// History: 10/16/2026 + 설정·업적 표시 책임 분리 -- Codex
//=============================================================================
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>입력 라우터에 IPausePopupView로 전달하는 팝업 표시 컴포넌트입니다.</summary>
[DisallowMultipleComponent]
public sealed class PausePopupView : MonoBehaviour, IPausePopupView
{
    /// <summary>팝업의 표시 상태입니다.</summary>
    public enum Page
    {
        POPUP_CLOSED = 0,
        POPUP_PAUSE,
        POPUP_SETTINGS,
        POPUP_ACHIEVEMENTS,
        POPUP_EXIT
    }

    //========================================
    // 직렬화 필드
    //========================================
    [Tooltip("기본 톱니 버튼입니다.")]
    [SerializeField] private GameObject _launcher;
    [Tooltip("기존 HUD 버튼을 사용하면 끕니다.")]
    [SerializeField] private bool _showLauncherButton = true;
    [Tooltip("일시정지 UI의 최상위 루트입니다.")]
    [SerializeField] private GameObject _pauseRoot;
    [Tooltip("하위 창을 열 때 입력을 차단할 메뉴입니다.")]
    [SerializeField] private CanvasGroup _pauseMenu;
    [Tooltip("하위 팝업의 배경과 창을 포함하는 루트입니다.")]
    [SerializeField] private GameObject _modalRoot;
    [Tooltip("설정 창입니다.")]
    [SerializeField] private GameObject _settingsPanel;
    [Tooltip("업적 창입니다.")]
    [SerializeField] private GameObject _achievementsPanel;
    [Tooltip("나가기 확인 창입니다.")]
    [SerializeField] private GameObject _exitPanel;
    [Tooltip("메뉴를 처음 열었을 때 선택할 버튼입니다.")]
    [SerializeField] private Selectable _menuFocus;
    [Tooltip("설정 창의 첫 입력 대상입니다.")]
    [SerializeField] private Selectable _settingsFocus;
    [Tooltip("업적 창의 첫 입력 대상입니다.")]
    [SerializeField] private Button _achievementsCloseButton;
    [Tooltip("나가기 확인 창에서 먼저 선택할 취소 버튼입니다.")]
    [SerializeField] private Button _exitCancelButton;
    [Tooltip("실제 일시정지 처리는 외부에서 연결합니다.")]
    [SerializeField] private UnityEvent<bool> _visibilityChanged = new UnityEvent<bool>();
    [Tooltip("실제 종료 처리는 외부에서 연결합니다.")]
    [SerializeField] private UnityEvent _exitRequested = new UnityEvent();

    // 전체 팝업을 닫을 때 복원할 선택 대상입니다.
    private GameObject _previousSelection;
    // 하위 창에서 돌아올 때 복원할 메뉴 선택입니다.
    private GameObject _menuSelection;

    /// <summary>현재 표시 중인 창입니다.</summary>
    public Page CurrentPage { get; private set; }
    /// <summary>팝업이 열려 있는지 반환합니다.</summary>
    public bool IsOpen => CurrentPage != Page.POPUP_CLOSED;
    /// <summary>전체 팝업이 열리거나 닫힐 때만 발생합니다.</summary>
    public UnityEvent<bool> VisibilityChanged => _visibilityChanged;
    /// <summary>확인 버튼에서 발생하는 종료 요청입니다.</summary>
    public UnityEvent ExitRequested => _exitRequested;

    // 편집 중 활성화한 창도 플레이 시작 시에는 닫습니다.
    private void Awake() => CloseAll();
    private void OnDisable()
    {
        if (IsOpen) CloseAll();
    }

    //=============================================================================
    // Description: Cancel 입력을 처리한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>닫힘 → 메뉴 → 닫힘, 하위 창 → 메뉴 순서로 처리합니다.</summary>
    public void HandleCancel()
    {
        if (!IsOpen) OpenPauseMenu();
        else if (CurrentPage == Page.POPUP_PAUSE) CloseAll();
        else ReturnToPauseMenu();
    }

    //=============================================================================
    // Description: 전체 팝업 표시를 전환한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>기존 HUD 버튼에서 호출할 열기·닫기 진입점입니다.</summary>
    public void TogglePauseMenu()
    {
        if (IsOpen) CloseAll();
        else OpenPauseMenu();
    }

    //=============================================================================
    // Description: 기본 버튼 표시를 정한다. Parameters: visible. Return: void.
    //=============================================================================
    /// <summary>기존 HUD와 중복되지 않도록 기본 톱니 버튼을 숨길 수 있습니다.</summary>
    public void SetLauncherVisible(bool visible)
    {
        _showLauncherButton = visible;
        if (_launcher != null) _launcher.SetActive(visible && !IsOpen);
    }

    //=============================================================================
    // Description: 메뉴를 표시한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>하위 창에서 돌아오면 이전 메뉴 선택을 복원합니다.</summary>
    public void OpenPauseMenu() => ShowPage(Page.POPUP_PAUSE, _menuSelection);

    //=============================================================================
    // Description: 설정 창을 표시한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>설정 창으로 이동합니다.</summary>
    public void ShowSettings() => ShowPage(Page.POPUP_SETTINGS, Target(_settingsFocus));

    //=============================================================================
    // Description: 업적 창을 표시한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>표시 전용 업적 목록으로 이동합니다.</summary>
    public void ShowAchievements() => ShowPage(Page.POPUP_ACHIEVEMENTS, Target(_achievementsCloseButton));

    //=============================================================================
    // Description: 종료 확인 창을 표시한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>취소 버튼을 먼저 선택해 나가기 확인 창을 엽니다.</summary>
    public void ShowExitConfirmation() => ShowPage(Page.POPUP_EXIT, Target(_exitCancelButton));

    //=============================================================================
    // Description: 메뉴로 복귀한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>하위 창의 닫기·취소 버튼에서 호출합니다.</summary>
    public void ReturnToPauseMenu() => OpenPauseMenu();

    //=============================================================================
    // Description: 모든 팝업을 닫는다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>실제 게임 시간이나 플레이어 입력은 변경하지 않습니다.</summary>
    public void CloseAll() => ShowPage(Page.POPUP_CLOSED);

    //=============================================================================
    // Description: 종료 요청만 발행한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>실제 종료 동작은 연결된 구독자가 결정합니다.</summary>
    public void RequestExit()
    {
        if (CurrentPage == Page.POPUP_EXIT) _exitRequested?.Invoke();
    }

    //=============================================================================
    // Description: 표시·입력·선택을 함께 전환한다. Parameters: page/selection. Return: void.
    //=============================================================================
    private void ShowPage(Page page, GameObject selection = null)
    {
        if (_pauseRoot == null || _pauseMenu == null || _modalRoot == null) return;
        bool wasOpen = IsOpen;
        bool modal = page != Page.POPUP_CLOSED && page != Page.POPUP_PAUSE;
        EventSystem events = EventSystem.current;
        if (!wasOpen && page != Page.POPUP_CLOSED)
            _previousSelection = events != null ? events.currentSelectedGameObject : null;
        if (CurrentPage == Page.POPUP_PAUSE && modal)
            _menuSelection = events != null ? events.currentSelectedGameObject : null;
        CurrentPage = page;
        _pauseRoot.SetActive(IsOpen);
        _modalRoot.SetActive(modal);
        _pauseMenu.interactable = _pauseMenu.blocksRaycasts = !modal;
        if (_settingsPanel != null) _settingsPanel.SetActive(page == Page.POPUP_SETTINGS);
        if (_achievementsPanel != null) _achievementsPanel.SetActive(page == Page.POPUP_ACHIEVEMENTS);
        if (_exitPanel != null) _exitPanel.SetActive(page == Page.POPUP_EXIT);
        if (_launcher != null) _launcher.SetActive(_showLauncherButton && !IsOpen);
        if (events != null && (wasOpen || IsOpen))
            events.SetSelectedGameObject(IsOpen ? selection ?? Target(_menuFocus) : _previousSelection);
        if (wasOpen != IsOpen) _visibilityChanged?.Invoke(IsOpen);
    }

    // Unity에서 파괴된 참조도 null로 처리합니다.
    private static GameObject Target(Selectable control) => control != null ? control.gameObject : null;
}
