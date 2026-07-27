using UnityEngine;
using UnityEngine.UI;

//=============================================================================
// File:        UiPuzzlePopup.cs
//
// Description: 퍼즐 팝업 열기, CSV 저장소 주입 및 클리어 후 지연 닫기를 관리한다.
//
// History:     07/26/2026 + CSV 저장소 주입과 Update 타이머 적용 -- VicZero
//
//=============================================================================
public sealed class UiPuzzlePopup : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("퍼즐 화면 전체를 표시하거나 숨길 최상위 UI 오브젝트입니다.")]
    [SerializeField]
    private GameObject _popupContainer;

    [Tooltip("퍼즐 팝업을 수동으로 닫는 버튼입니다.")]
    [SerializeField]
    private Button _closeButton;

    [Header("Puzzle")]
    [Tooltip("이 팝업이 표시하고 초기화할 퍼즐 매니저입니다.")]
    [SerializeField]
    private BasePuzzleManager _uiPuzzleManager;

    [Header("Clear")]
    [Tooltip("퍼즐 클리어 후 팝업을 자동으로 닫기까지 기다릴 시간입니다.")]
    [SerializeField]
    private float _closeDelaySeconds = 3f;

    private bool _isCloseScheduled;
    private float _closeAtUnscaledTime;

    /// <summary>이 팝업이 직접 초기화하는 퍼즐 매니저입니다.</summary>
    public BasePuzzleManager PuzzleManager => _uiPuzzleManager;

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(ClosePopup);
        }

        if (_uiPuzzleManager != null)
        {
            _uiPuzzleManager.OnPuzzleClearedEvent += OnPuzzleCleared;
        }

        ClosePopup();
    }

    private void Update()
    {
        if (!_isCloseScheduled || Time.unscaledTime < _closeAtUnscaledTime)
        {
            return;
        }

        _isCloseScheduled = false;
        ClosePopup();
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(ClosePopup);
        }

        if (_uiPuzzleManager != null)
        {
            _uiPuzzleManager.OnPuzzleClearedEvent -= OnPuzzleCleared;
        }
    }

    /// <summary>GameRoot의 CSV 저장소를 퍼즐 매니저에 주입하고 팝업을 엽니다.</summary>
    public void OpenPuzzlePopup()
    {
        _isCloseScheduled = false;

        if (_popupContainer == null || _uiPuzzleManager == null)
        {
            Debug.LogError("[UiPuzzlePopup] Popup Container 또는 Puzzle Manager가 연결되지 않았습니다.");
            return;
        }

        if (!GameRoot.TryGetInstance(out GameRoot gameRoot) || gameRoot.GameData == null)
        {
            Debug.LogError("[UiPuzzlePopup] GameRoot의 CSV 게임 데이터 초기화가 아직 완료되지 않았습니다.");
            return;
        }

        _uiPuzzleManager.Init(gameRoot.GameData);
        _popupContainer.SetActive(true);
        _uiPuzzleManager.enabled = true;
        _uiPuzzleManager.InitializePuzzle();
    }

    /// <summary>진행 중인 자동 닫기를 취소하고 퍼즐 팝업을 즉시 닫습니다.</summary>
    public void ClosePopup()
    {
        _isCloseScheduled = false;

        if (_popupContainer != null)
        {
            _popupContainer.SetActive(false);
        }

        if (_uiPuzzleManager != null)
        {
            _uiPuzzleManager.enabled = false;
        }
    }

    private void OnPuzzleCleared()
    {
        if (_uiPuzzleManager != null)
        {
            _uiPuzzleManager.enabled = false;
        }

        _isCloseScheduled = true;
        _closeAtUnscaledTime = Time.unscaledTime + Mathf.Max(0f, _closeDelaySeconds);
    }
}
