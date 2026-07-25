//=============================================================================
// File:        AltarPresenter.cs
//
// Description: 제단 기술 목록, 확인 팝업, 반납 저장과 조작 잠금을 조정한다.
//
// History:     07/22/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//=============================================================================
//
// Synopsis:   AltarPresenter;
//                - 고정 버튼 풀에 반납 가능한 기술 표시
//                - 선택 확인 후 도메인 상태 변경과 저장 요청
//                - UI가 열린 동안 플레이어 이동·상호작용 차단
//
//=============================================================================
[DisallowMultipleComponent]
public sealed class AltarPresenter : MonoBehaviour
{
    //========================================
    // 내부 데이터
    //========================================

    [Serializable] private sealed class ButtonBinding
    {
        [Tooltip("이 기술 버튼 슬롯의 최상위 오브젝트입니다.")]
        [SerializeField] internal GameObject _root;

        [Tooltip("기술을 선택하는 버튼입니다.")]
        [SerializeField] internal Button _button;

        [Tooltip("기술 아이콘을 표시하는 이미지입니다.")]
        [SerializeField] private Image _icon;

        [Tooltip("현지화된 기술 이름을 표시하는 텍스트입니다.")]
        [SerializeField] private TMP_Text _nameText;

        [Tooltip("기술을 반납했을 때 증가하는 재건도를 표시하는 텍스트입니다.")]
        [SerializeField] private TMP_Text _stateText;

        internal void Show(SkillDataSO skill, string displayName)
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            if (_icon != null)
            {
                _icon.sprite = skill.Icon;
                _icon.enabled = skill.Icon != null;
            }

            if (_nameText != null)
            {
                _nameText.text = displayName;
            }

            if (_stateText != null)
            {
                _stateText.text = string.Concat("+", skill.RestoreValue.ToString(), "%");
            }
        }

    }

    //========================================
    // 직렬화 필드
    //========================================

    [Header("View")]
    [Tooltip("제단 UI 전체를 표시하거나 숨기는 최상위 오브젝트입니다.")]
    [SerializeField] private GameObject _panelRoot;

    [Tooltip("반납 가능한 기술 버튼을 묶는 오브젝트입니다.")]
    [SerializeField] private GameObject _skillListRoot;

    [Tooltip("반납 가능한 기술이 없을 때 표시할 빈 상태 오브젝트입니다.")]
    [SerializeField] private GameObject _noReturnableRoot;

    [Tooltip("선택한 기술 반납을 확인하는 팝업 오브젝트입니다.")]
    [SerializeField] private GameObject _confirmPopupRoot;

    [Header("Buttons")]
    [Tooltip("최대 기술 수만큼 미리 배치해 재사용할 고정 버튼 목록입니다.")]
    [SerializeField] private ButtonBinding[] _skillButtons = Array.Empty<ButtonBinding>();

    [Tooltip("제단 UI를 닫는 버튼입니다.")]
    [SerializeField] private Button _closeButton;

    [Tooltip("선택한 기술 반납을 확정하는 버튼입니다.")]
    [SerializeField] private Button _confirmButton;

    [Tooltip("기술 반납 확인을 취소하는 버튼입니다.")]
    [SerializeField] private Button _cancelConfirmButton;

    [Header("Player Lock")]
    [Tooltip("제단 UI가 열린 동안 상호작용을 막을 플레이어 컨트롤러입니다.")]
    [SerializeField] private PlayerInteractionController _playerInteractionController;

    [Tooltip("제단 UI가 열린 동안 이동을 막을 플레이어 컨트롤러입니다.")]
    [SerializeField] private PlayerMoveController _playerMoveController;

    //========================================
    // 내부 상태
    //========================================

    private readonly List<SkillDataSO> _returnableSkills = new List<SkillDataSO>();
    private IRelicRestorationService _restorationService;
    private IProgressService _progressService;
    private ILocalizationService _localizationService;
    private UnityAction[] _skillButtonActions;
    private SkillDataSO _selectedSkill;
    private bool _isBusy;

    //******************************************************************************
    //
    // Unity Lifecycle
    //
    //******************************************************************************

    /// <summary>버튼 이벤트를 해제하고 플레이어 조작 잠금을 복구합니다.</summary>
    private void OnDestroy()
    {
        UnregisterButtonListeners();

        if ((_panelRoot != null && _panelRoot.activeSelf) || _isBusy)
        {
            SetPlayerControl(true);
        }
    }

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    //=============================================================================
    // AltarPresenter.Init
    //=============================================================================
    // Description: 제단 UI가 사용할 도메인, 저장, 현지화 서비스를 연결한다.
    //
    // Parameters:  ( IRelicRestorationService restorationService,
    //                IProgressService progressService,
    //                ILocalizationService localizationService )
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>제단 UI가 사용할 도메인, 저장, 현지화 서비스를 연결합니다.</summary>
    /// <param name="restorationService">기술 반납 상태를 관리하는 서비스입니다.</param>
    /// <param name="progressService">확정된 반납 상태를 저장하는 서비스입니다.</param>
    /// <param name="localizationService">기술 이름을 현재 언어로 변환하는 서비스입니다.</param>
    public void Init(
        IRelicRestorationService restorationService,
        IProgressService progressService,
        ILocalizationService localizationService)
    {
        _restorationService = restorationService;
        _progressService = progressService;
        _localizationService = localizationService;
        _selectedSkill = null;
        _isBusy = false;

        RegisterButtonListeners();
        SetViewActive(false);
    }

    //=============================================================================
    // AltarPresenter.Open
    //=============================================================================
    // Description: 상호작용한 제단에서 반납 가능한 기술 UI를 연다.
    //
    // Parameters:  ( AltarInteractable altar )
    //
    // Return:      void
    //
    //=============================================================================
    /// <summary>상호작용한 제단에서 반납 가능한 기술 UI를 엽니다.</summary>
    /// <param name="altar">현재 상호작용한 제단입니다.</param>
    public void Open(AltarInteractable altar)
    {
        if (_restorationService == null || _progressService == null || _localizationService == null
            || altar == null || _panelRoot == null)
        {
            Debug.LogWarning("[AltarPresenter] 초기화 또는 필수 View 연결이 완료되지 않았습니다.");
            return;
        }

        if (_panelRoot.activeSelf || _isBusy)
        {
            return;
        }

        _selectedSkill = null;
        RefreshSkillList();
        SetPlayerControl(false);
        SetViewActive(true);
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    /// <summary>버튼 클릭 이벤트를 한 번 연결합니다.</summary>
    private void RegisterButtonListeners()
    {
        UnregisterButtonListeners();

        if (_skillButtons == null)
        {
            return;
        }

        _skillButtonActions = new UnityAction[_skillButtons.Length];

        for (int i = 0; i < _skillButtons.Length; i++)
        {
            ButtonBinding binding = _skillButtons[i];

            if (binding == null || binding._button == null)
            {
                continue;
            }

            int skillIndex = i;
            UnityAction action = () => SelectSkill(skillIndex);
            _skillButtonActions[i] = action;
            binding._button.onClick.AddListener(action);
        }

        _closeButton?.onClick.AddListener(Close);
        _confirmButton?.onClick.AddListener(ConfirmSelection);
        _cancelConfirmButton?.onClick.AddListener(CancelSelection);
    }

    /// <summary>연결한 버튼 클릭 이벤트를 해제합니다.</summary>
    private void UnregisterButtonListeners()
    {
        if (_skillButtons != null && _skillButtonActions != null)
        {
            int count = Mathf.Min(_skillButtons.Length, _skillButtonActions.Length);

            for (int i = 0; i < count; i++)
            {
                ButtonBinding binding = _skillButtons[i];
                UnityAction action = _skillButtonActions[i];

                if (binding != null && binding._button != null && action != null)
                {
                    binding._button.onClick.RemoveListener(action);
                }
            }
        }

        _closeButton?.onClick.RemoveListener(Close);
        _confirmButton?.onClick.RemoveListener(ConfirmSelection);
        _cancelConfirmButton?.onClick.RemoveListener(CancelSelection);
        _skillButtonActions = null;
    }

    /// <summary>반납 가능한 기술 목록을 재사용 버퍼와 고정 버튼에 반영합니다.</summary>
    private void RefreshSkillList()
    {
        _restorationService.FillReturnableSkills(_returnableSkills);
        int buttonCount = _skillButtons != null ? _skillButtons.Length : 0;

        for (int i = 0; i < buttonCount; i++)
        {
            ButtonBinding binding = _skillButtons[i];

            if (binding == null)
            {
                continue;
            }

            if (i >= _returnableSkills.Count)
            {
                binding._root?.SetActive(false);
                continue;
            }

            SkillDataSO skill = _returnableSkills[i];
            binding.Show(skill, _localizationService.GetText(skill.DisplayNameKey));
        }

        _skillListRoot?.SetActive(_returnableSkills.Count > 0);
        _noReturnableRoot?.SetActive(_returnableSkills.Count == 0);

        if (_returnableSkills.Count > buttonCount)
        {
            Debug.LogWarning("[AltarPresenter] 고정 기술 버튼 수가 반납 가능한 기술 수보다 적습니다.");
        }
    }

    /// <summary>선택한 기술의 확인 팝업을 엽니다.</summary>
    private void SelectSkill(int skillIndex)
    {
        if (_isBusy || skillIndex < 0 || skillIndex >= _returnableSkills.Count)
        {
            return;
        }

        _selectedSkill = _returnableSkills[skillIndex];
        _confirmPopupRoot?.SetActive(true);
    }

    /// <summary>선택을 취소하고 도메인 상태 변경 없이 확인 팝업만 닫습니다.</summary>
    private void CancelSelection()
    {
        if (_isBusy)
        {
            return;
        }

        _selectedSkill = null;
        _confirmPopupRoot?.SetActive(false);
    }

    /// <summary>선택한 기술 반납과 체크포인트 저장을 시작합니다.</summary>
    private void ConfirmSelection()
    {
        if (_isBusy || _selectedSkill == null)
        {
            return;
        }

        ReturnSelectedSkillAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>기술을 한 번만 반납하고 확정된 진행 상태를 저장합니다.</summary>
    private async UniTask ReturnSelectedSkillAsync(CancellationToken cancellationToken)
    {
        _isBusy = true;
        SkillDataSO selectedSkill = _selectedSkill;

        if (!_restorationService.TryReturnSkill(selectedSkill.SkillId, out _, out RelicRestorationFailure failure))
        {
            Debug.LogWarning($"[AltarPresenter] 기술 반납에 실패했습니다. SkillId={selectedSkill.SkillId}, Failure={failure}");
            _isBusy = false;
            _selectedSkill = null;
            _confirmPopupRoot?.SetActive(false);
            RefreshSkillList();
            return;
        }

        SetViewActive(false);

        try
        {
            if (!await _progressService.SaveAsync(cancellationToken))
            {
                Debug.LogWarning("[AltarPresenter] 기술 반납 상태 저장을 완료하지 못했습니다.");
            }
        }
        finally
        {
            _selectedSkill = null;
            _isBusy = false;
            SetPlayerControl(true);
        }
    }

    /// <summary>저장 중이 아닐 때 제단 UI를 닫고 조작을 복구합니다.</summary>
    private void Close()
    {
        if (_panelRoot == null || !_panelRoot.activeSelf || _isBusy)
        {
            return;
        }

        _selectedSkill = null;
        SetViewActive(false);
        SetPlayerControl(true);
    }

    /// <summary>제단 UI의 최상위 오브젝트와 확인 팝업 상태를 설정합니다.</summary>
    private void SetViewActive(bool isActive)
    {
        _confirmPopupRoot?.SetActive(false);
        _panelRoot?.SetActive(isActive);
    }

    /// <summary>기존 대화 잠금 패턴과 같은 API로 이동과 상호작용을 설정합니다.</summary>
    private void SetPlayerControl(bool canControl)
    {
        _playerInteractionController?.SetCanInteract(canControl);
        _playerMoveController?.SetCanMove(canControl);
    }
}
