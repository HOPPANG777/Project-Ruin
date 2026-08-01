//=============================================================================
// File:        AltarPresenter.cs
// Description: 제단 기술 반환, 저장과 플레이어 조작 잠금 흐름을 조정한다.
// History:     07/22/2026 + Created -- Codex
//              08/01/2026 + 표시 책임을 AltarView로 분리 -- Codex
//=============================================================================
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
//=============================================================================
// Synopsis:   AltarPresenter;
//                - 반환 가능한 기술 조회와 한 번만 반환 처리
//                - 저장 완료까지 제단 UI와 플레이어 조작 잠금 관리
//=============================================================================
[DisallowMultipleComponent]
public sealed class AltarPresenter : MonoBehaviour
{
    [Header("View")]
    [Tooltip("기술 목록과 확인 팝업 표시를 담당하는 제단 View입니다.")]
    [SerializeField] private AltarView _view;
    [Header("Player Lock")]
    [Tooltip("제단 UI가 열린 동안 상호작용을 막을 플레이어 컨트롤러입니다.")]
    [SerializeField] private PlayerInteractionController _playerInteractionController;
    [Tooltip("제단 UI가 열린 동안 이동을 막을 플레이어 컨트롤러입니다.")]
    [SerializeField] private PlayerMoveController _playerMoveController;

    private readonly List<TechnologyDataSO> _returnableTechnologies =
        new List<TechnologyDataSO>();
    private IRelicRestorationService _restorationService;
    private IProgressService _progressService;
    private ILocalizationService _localizationService;
    private TechnologyDataSO _selectedTechnology;
    private bool _isBusy;
    private bool _isOpen;

    private void OnDestroy()
    {
        if (_isOpen || _isBusy)
        {
            SetPlayerControl(true);
        }
    }

    /// <summary>제단 UI가 사용할 도메인, 저장, 현지화 서비스를 연결합니다.</summary>
    public void Init(IRelicRestorationService restorationService,
        IProgressService progressService, ILocalizationService localizationService)
    {
        _restorationService = restorationService;
        _progressService = progressService;
        _localizationService = localizationService;
        _selectedTechnology = null;
        _isBusy = false;
        _isOpen = false;
        if (_view == null)
        {
            Debug.LogWarning("[AltarPresenter] AltarView가 연결되지 않았습니다.");
            return;
        }
        _view.Init(SelectTechnology, Close, ConfirmSelection, CancelSelection);
    }

    /// <summary>상호작용한 제단에서 반환 가능한 기술 UI를 엽니다.</summary>
    public void Open(AltarInteractable altar)
    {
        if (_restorationService == null || _progressService == null
            || _localizationService == null || _view == null || altar == null)
        {
            Debug.LogWarning("[AltarPresenter] 초기화 또는 필수 View 연결이 완료되지 않았습니다.");
            return;
        }
        if (_view.IsOpen || _isBusy)
        {
            return;
        }
        _selectedTechnology = null;
        RefreshTechnologyList();
        if (!_view.TryShow())
        {
            return;
        }
        _isOpen = true;
        SetPlayerControl(false);
    }

    private void RefreshTechnologyList()
    {
        _restorationService.FillReturnableTechnologies(_returnableTechnologies);
        _view.RefreshTechnologies(_returnableTechnologies, _localizationService);
    }

    private void SelectTechnology(int technologyIndex)
    {
        if (_isBusy || technologyIndex < 0
            || technologyIndex >= _returnableTechnologies.Count)
        {
            return;
        }
        _selectedTechnology = _returnableTechnologies[technologyIndex];
        _view.SetConfirmationVisible(true);
    }

    private void CancelSelection()
    {
        if (_isBusy)
        {
            return;
        }
        _selectedTechnology = null;
        _view.SetConfirmationVisible(false);
    }

    private void ConfirmSelection()
    {
        if (_isBusy || _selectedTechnology == null)
        {
            return;
        }
        ReturnSelectedTechnologyAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTask ReturnSelectedTechnologyAsync(CancellationToken cancellationToken)
    {
        _isBusy = true;
        TechnologyDataSO selectedTechnology = _selectedTechnology;
        if (!_restorationService.TryReturnTechnology(selectedTechnology.TechnologyId,
            out RelicRestorationResult result, out RelicRestorationFailure failure))
        {
            Debug.LogWarning($"[AltarPresenter] 기술 반환 실패: "
                + $"id={selectedTechnology.TechnologyId}, failure={failure}");
            _selectedTechnology = null;
            _isBusy = false;
            _view.SetConfirmationVisible(false);
            RefreshTechnologyList();
            return;
        }
        _view.Hide();
        _isOpen = false;
        if (result.ShouldPlayEnding)
        {
            Debug.Log("[AltarPresenter] 모든 기술 반환이 완료되었습니다.");
        }
        try
        {
            if (!await _progressService.SaveAsync(cancellationToken))
            {
                Debug.LogWarning("[AltarPresenter] 기술 반환 상태 저장을 완료하지 못했습니다.");
            }
        }
        finally
        {
            _selectedTechnology = null;
            _isBusy = false;
            SetPlayerControl(true);
        }
    }

    private void Close()
    {
        if (_view == null || !_view.IsOpen || _isBusy)
        {
            return;
        }
        _selectedTechnology = null;
        _view.Hide();
        _isOpen = false;
        SetPlayerControl(true);
    }

    private void SetPlayerControl(bool canControl)
    {
        _playerInteractionController?.SetCanInteract(canControl);
        _playerMoveController?.SetCanMove(canControl);
    }
}
