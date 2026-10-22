//=============================================================================
// File: TitleScreenPresenter.cs
// Description: 타이틀 UI와 모달 표시를 담당하고 게임 진입을 계약에 위임한다.
// History: 10/16/2026 + 진입 처리 분리 및 의존성 주입 -- Codex
//=============================================================================
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>씬 전환이나 진행 저장의 구현을 알지 못하는 타이틀 표시부입니다.</summary>
[DisallowMultipleComponent]
public sealed class TitleScreenPresenter : MonoBehaviour
{
    [Tooltip("모달 표시 중 입력을 차단할 메뉴입니다.")]
    [SerializeField] private CanvasGroup _menu;
    [Tooltip("새 게임 메뉴 버튼입니다.")]
    [SerializeField] private Button _newGameButton;
    [Tooltip("불러오기 메뉴 버튼입니다.")]
    [SerializeField] private Button _loadGameButton;
    [Tooltip("모달과 배경을 포함한 루트입니다.")]
    [SerializeField] private GameObject _modalLayer;
    [Tooltip("새 게임 확인 창입니다.")]
    [SerializeField] private GameObject _newGamePanel;
    [Tooltip("저장 슬롯 선택 창입니다.")]
    [SerializeField] private GameObject _loadGamePanel;
    [Tooltip("새 게임 확인 창의 첫 선택 버튼입니다.")]
    [SerializeField] private Button _startButton;
    [Tooltip("저장이 없을 때 선택할 불러오기 닫기 버튼입니다.")]
    [SerializeField] private Button _loadGameCloseButton;
    [Tooltip("현재 저장 형식에서는 첫 번째 슬롯만 사용합니다.")]
    [SerializeField] private Button[] _saveSlotButtons;
    [Tooltip("각 슬롯의 저장 유무를 표시합니다.")]
    [SerializeField] private TMP_Text[] _saveSlotDescriptions;
    [Tooltip("게임 진입 실패를 표시합니다.")]
    [SerializeField] private TMP_Text _status;
    [Tooltip("진입 중 중복 입력을 차단할 루트입니다.")]
    [SerializeField] private GameObject _busyLayer;

    // TitleGameEntry가 주입하는 게임 진입 계약입니다.
    private ITitleGameService _gameService;
    // 비동기 진입 중 중복 클릭을 막습니다.
    private bool _isBusy;

    private void Start()
    {
        if (_busyLayer != null) _busyLayer.SetActive(false);
        CloseModal();
    }

    // ESC는 타이틀의 열린 모달에만 적용합니다.
    private void Update()
    {
        if (_modalLayer != null && _modalLayer.activeSelf && Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame) CloseModal();
    }

    //=============================================================================
    // Description: 게임 진입 계약을 주입한다. Parameters: gameService. Return: void.
    //=============================================================================
    /// <summary>실제 진입 처리와 테스트 대역을 동일한 방식으로 연결합니다.</summary>
    public void Init(ITitleGameService gameService) => _gameService = gameService;

    //=============================================================================
    // Description: 새 게임 확인 창을 표시한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>확인 전에는 저장 상태를 변경하지 않습니다.</summary>
    public void ShowNewGame() => ShowModal(_newGamePanel, _startButton);

    //=============================================================================
    // Description: 저장 슬롯을 표시한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>단일 저장만 첫 슬롯에 연결합니다.</summary>
    public void ShowLoadGame()
    {
        if (_isBusy || _saveSlotButtons == null || _saveSlotDescriptions == null) return;
        bool hasSave = _gameService != null && _gameService.HasSavedData;
        int count = Mathf.Min(_saveSlotButtons.Length, _saveSlotDescriptions.Length);
        for (int i = 0; i < count; i++)
        {
            bool available = i == 0 && hasSave;
            if (_saveSlotButtons[i] != null) _saveSlotButtons[i].interactable = available;
            if (_saveSlotDescriptions[i] != null)
                _saveSlotDescriptions[i].text = available ? "저장된 진행 불러오기" : "저장된 기록이 없습니다";
        }
        ShowModal(_loadGamePanel, hasSave && count > 0 ? _saveSlotButtons[0] : _loadGameCloseButton);
    }

    //=============================================================================
    // Description: 모달을 닫는다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>진입 작업 중에는 닫기 입력을 무시합니다.</summary>
    public void CloseModal()
    {
        if (_isBusy) return;
        bool wasLoad = _loadGamePanel != null && _loadGamePanel.activeSelf;
        if (_modalLayer != null) _modalLayer.SetActive(false);
        if (_newGamePanel != null) _newGamePanel.SetActive(false);
        if (_loadGamePanel != null) _loadGamePanel.SetActive(false);
        if (_menu != null) _menu.interactable = _menu.blocksRaycasts = true;
        SetStatus(string.Empty);
        Select(wasLoad ? _loadGameButton : _newGameButton);
    }

    //=============================================================================
    // Description: 새 게임 진입을 요청한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>중복 실행은 진입 메서드에서 차단합니다.</summary>
    public void StartNewGame() => EnterGameAsync(true).Forget();

    //=============================================================================
    // Description: 저장된 진행을 요청한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>저장이 있을 때만 불러오기 요청을 보냅니다.</summary>
    public void LoadSavedGame()
    {
        if (_gameService != null && _gameService.HasSavedData) EnterGameAsync(false).Forget();
    }

    //=============================================================================
    // Description: 종료를 요청한다. Parameters: None. Return: void.
    //=============================================================================
    /// <summary>종료의 구현은 주입된 서비스가 소유합니다.</summary>
    public void QuitGame()
    {
        if (!_isBusy) _gameService?.Quit();
    }

    // 표시 상태를 먼저 맞춘 뒤 입력 대상을 선택합니다.
    private void ShowModal(GameObject panel, Button focus)
    {
        if (_isBusy || panel == null || _modalLayer == null) return;
        if (_menu != null) _menu.interactable = _menu.blocksRaycasts = false;
        if (_newGamePanel != null) _newGamePanel.SetActive(panel == _newGamePanel);
        if (_loadGamePanel != null) _loadGamePanel.SetActive(panel == _loadGamePanel);
        _modalLayer.SetActive(true);
        SetStatus(string.Empty);
        Select(focus);
    }

    //=============================================================================
    // Description: 진입 요청과 입력 차단을 묶는다. Parameters: newGame. Return: UniTask.
    //=============================================================================
    private async UniTask EnterGameAsync(bool newGame)
    {
        if (_isBusy || _gameService == null) return;
        _isBusy = true;
        if (_busyLayer != null) _busyLayer.SetActive(true);
        try
        {
            bool entered = await _gameService.TryEnterAsync(newGame, this.GetCancellationTokenOnDestroy());
            if (!entered) SetStatus("게임을 시작하지 못했습니다. 다시 시도해 주세요.");
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            Debug.LogError($"[TitleScreenPresenter] 게임 진입 실패: {exception.Message}", this);
            if (this != null) SetStatus("게임을 시작하지 못했습니다. 다시 시도해 주세요.");
        }
        finally
        {
            if (this != null)
            {
                _isBusy = false;
                if (_busyLayer != null) _busyLayer.SetActive(false);
            }
        }
    }

    private void SetStatus(string value) { if (_status != null) _status.text = value; }
    private static void Select(Button button)
    {
        if (EventSystem.current != null && button != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
