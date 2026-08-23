//=============================================================================
// File:        AltarView.cs
// Description: 제단 기술 버튼 풀, 빈 상태와 확인 팝업 표시를 담당한다.
// History:     08/01/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
//=============================================================================
// Synopsis:   AltarView;
//                - 반환 가능한 기술을 고정 버튼에 표시
//                - 제단 패널과 확인 팝업의 표시 상태 관리
//=============================================================================
[DisallowMultipleComponent]
public sealed class AltarView : MonoBehaviour
{
    [Serializable]
    private sealed class TechnologyButtonBinding
    {
        [Tooltip("이 기술 버튼 슬롯의 최상위 오브젝트입니다.")]
        [SerializeField] internal GameObject _root;
        [Tooltip("기술을 선택하는 버튼입니다.")]
        [SerializeField] internal Button _button;
        [Tooltip("기술 아이콘을 표시하는 이미지입니다.")]
        [SerializeField] private Image _icon;
        [Tooltip("현지화된 기술 이름을 표시하는 텍스트입니다.")]
        [SerializeField] private TMP_Text _nameText;
        [Tooltip("기술 반환 시 증가하는 재건도를 표시하는 텍스트입니다.")]
        [SerializeField] private TMP_Text _restoreText;

        internal void Show(TechnologyDefinition technology, string displayName, Sprite icon)
        {
            _root?.SetActive(true);
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }
            if (_nameText != null)
            {
                _nameText.text = displayName;
            }
            _restoreText?.SetText("+{0}%", technology.RestoreValue);
        }
        internal void Hide() => _root?.SetActive(false);
    }

    [Header("Panel")]
    [Tooltip("제단 UI 전체를 표시하거나 숨기는 최상위 오브젝트입니다.")]
    [SerializeField] private GameObject _panelRoot;
    [Tooltip("반환 가능한 기술 버튼을 묶는 오브젝트입니다.")]
    [SerializeField] private GameObject _technologyListRoot;
    [Tooltip("반환 가능한 기술이 없을 때 표시할 빈 상태 오브젝트입니다.")]
    [SerializeField] private GameObject _noReturnableRoot;
    [Tooltip("선택한 기술 반환을 확인하는 팝업 오브젝트입니다.")]
    [SerializeField] private GameObject _confirmPopupRoot;
    [Header("Visual")]
    [Tooltip("technologies.csv의 icon_id를 Sprite로 연결하는 카탈로그입니다.")]
    [SerializeField] private ItemVisualCatalogSO _visualCatalog;
    [Header("Buttons")]
    [Tooltip("최대 기술 수만큼 미리 배치해 재사용할 고정 버튼 목록입니다.")]
    [SerializeField] private TechnologyButtonBinding[] _technologyButtons =
        Array.Empty<TechnologyButtonBinding>();
    [Tooltip("제단 UI를 닫는 버튼입니다.")]
    [SerializeField] private Button _closeButton;
    [Tooltip("선택한 기술 반환을 확정하는 버튼입니다.")]
    [SerializeField] private Button _confirmButton;
    [Tooltip("기술 반환 확인을 취소하는 버튼입니다.")]
    [SerializeField] private Button _cancelConfirmButton;

    private UnityAction[] _technologyButtonActions;
    private Action<int> _technologySelected;
    private Action _closeRequested;
    private Action _confirmRequested;
    private Action _cancelRequested;

    /// <summary>제단 패널이 현재 열려 있는지 여부입니다.</summary>
    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    private void OnDestroy() => UnregisterListeners();

    /// <summary>고정 버튼 입력을 Presenter 콜백에 한 번 연결합니다.</summary>
    public void Init(Action<int> technologySelected, Action closeRequested,
        Action confirmRequested, Action cancelRequested)
    {
        UnregisterListeners();
        _technologySelected = technologySelected;
        _closeRequested = closeRequested;
        _confirmRequested = confirmRequested;
        _cancelRequested = cancelRequested;
        RegisterListeners();
        Hide();
    }

    /// <summary>제단 패널을 열 수 있으면 표시합니다.</summary>
    public bool TryShow()
    {
        if (_panelRoot == null)
        {
            Debug.LogWarning("[AltarView] Panel Root가 연결되지 않았습니다.");
            return false;
        }
        SetConfirmationVisible(false);
        _panelRoot.SetActive(true);
        return true;
    }

    /// <summary>제단 패널과 확인 팝업을 숨깁니다.</summary>
    public void Hide()
    {
        SetConfirmationVisible(false);
        _panelRoot?.SetActive(false);
    }

    /// <summary>반환 가능한 기술을 고정 버튼 풀과 빈 상태에 반영합니다.</summary>
    public void RefreshTechnologies(IReadOnlyList<TechnologyDefinition> technologies,
        ILocalizationService localizationService)
    {
        if (technologies == null || localizationService == null)
        {
            return;
        }
        int buttonCount = _technologyButtons != null ? _technologyButtons.Length : 0;
        for (int i = 0; i < buttonCount; i++)
        {
            TechnologyButtonBinding binding = _technologyButtons[i];
            if (binding == null)
            {
                continue;
            }
            if (i >= technologies.Count)
            {
                binding.Hide();
                continue;
            }
            TechnologyDefinition technology = technologies[i];
            Sprite icon = null;
            if (_visualCatalog != null && !string.IsNullOrWhiteSpace(technology.IconId))
            {
                _visualCatalog.TryGetIcon(technology.IconId, out icon);
            }
            binding.Show(technology, localizationService.GetText(technology.DisplayNameKey), icon);
        }
        _technologyListRoot?.SetActive(technologies.Count > 0);
        _noReturnableRoot?.SetActive(technologies.Count == 0);
        if (technologies.Count > buttonCount)
        {
            Debug.LogWarning("[AltarView] 고정 기술 버튼 수가 반환 가능한 기술 수보다 적습니다.");
        }
    }

    /// <summary>기술 반환 확인 팝업의 표시 여부를 설정합니다.</summary>
    public void SetConfirmationVisible(bool isVisible) =>
        _confirmPopupRoot?.SetActive(isVisible);

    private void RegisterListeners()
    {
        int buttonCount = _technologyButtons != null ? _technologyButtons.Length : 0;
        _technologyButtonActions = new UnityAction[buttonCount];
        for (int i = 0; i < buttonCount; i++)
        {
            TechnologyButtonBinding binding = _technologyButtons[i];
            if (binding == null || binding._button == null)
            {
                continue;
            }
            int technologyIndex = i;
            UnityAction action = () => _technologySelected?.Invoke(technologyIndex);
            _technologyButtonActions[i] = action;
            binding._button.onClick.AddListener(action);
        }
        _closeButton?.onClick.AddListener(HandleClose);
        _confirmButton?.onClick.AddListener(HandleConfirm);
        _cancelConfirmButton?.onClick.AddListener(HandleCancel);
    }

    private void UnregisterListeners()
    {
        if (_technologyButtons != null && _technologyButtonActions != null)
        {
            int count = Mathf.Min(_technologyButtons.Length, _technologyButtonActions.Length);
            for (int i = 0; i < count; i++)
            {
                TechnologyButtonBinding binding = _technologyButtons[i];
                UnityAction action = _technologyButtonActions[i];
                if (binding != null && binding._button != null && action != null)
                {
                    binding._button.onClick.RemoveListener(action);
                }
            }
        }
        _closeButton?.onClick.RemoveListener(HandleClose);
        _confirmButton?.onClick.RemoveListener(HandleConfirm);
        _cancelConfirmButton?.onClick.RemoveListener(HandleCancel);
        _technologyButtonActions = null;
    }

    private void HandleClose() => _closeRequested?.Invoke();
    private void HandleConfirm() => _confirmRequested?.Invoke();
    private void HandleCancel() => _cancelRequested?.Invoke();
}
