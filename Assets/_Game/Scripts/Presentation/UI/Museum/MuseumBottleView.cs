//=============================================================================
// File:        MuseumBottleView.cs
// Description: 술병 선택 목록, 진행도와 시각적 채움 UI를 표시한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MuseumBottleView : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("술병 UI 전체의 최상위 오브젝트입니다.")]
    [SerializeField] private GameObject _panelRoot;
    [Tooltip("현재 투입 가능한 술이 없을 때 표시합니다.")]
    [SerializeField] private GameObject _emptyRoot;
    [Tooltip("술병이 완성됐을 때 표시합니다.")]
    [SerializeField] private GameObject _completedRoot;
    [Header("Progress")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _progressText;
    [Tooltip("Image Type을 Filled로 설정한 술병 채움 이미지입니다.")]
    [SerializeField] private Image _fillImage;
    [Header("Selection")]
    [Tooltip("최대 허용 술 수만큼 미리 배치해 재사용할 버튼입니다.")]
    [SerializeField] private MuseumBottleItemButton[] _itemButtons =
        Array.Empty<MuseumBottleItemButton>();
    [SerializeField] private Button _closeButton;

    private Action _closeRequested;

    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    private void OnDestroy() => _closeButton?.onClick.RemoveListener(HandleClose);

    /// <summary>고정 술 버튼과 닫기 입력을 Presenter에 연결합니다.</summary>
    public void Init(Action<int> itemSelected, Action closeRequested)
    {
        _closeButton?.onClick.RemoveListener(HandleClose);
        _closeRequested = closeRequested;
        _closeButton?.onClick.AddListener(HandleClose);
        for (int i = 0; i < _itemButtons.Length; i++)
            _itemButtons[i]?.Init(i, itemSelected);
        Hide();
    }

    public bool TryShow()
    {
        if (_panelRoot == null)
        {
            Debug.LogWarning("[MuseumBottleView] Panel Root가 연결되지 않았습니다.", this);
            return false;
        }
        _panelRoot.SetActive(true);
        return true;
    }

    public void Hide() => _panelRoot?.SetActive(false);

    /// <summary>술병 제목, 진행도와 현재 보유한 투입 후보를 갱신합니다.</summary>
    public void Refresh(string title, MuseumBottleProgress progress,
        IReadOnlyList<MuseumBottleItemDefinition> items, IInventoryService inventory)
    {
        if (_titleText != null) _titleText.text = title ?? string.Empty;
        _progressText?.SetText("{0} / {1}", progress.ContributedCount, progress.RequiredCount);
        if (_fillImage != null) _fillImage.fillAmount = progress.FillRatio;
        _completedRoot?.SetActive(progress.IsCompleted);
        _emptyRoot?.SetActive(!progress.IsCompleted && items.Count == 0);
        int visibleCount = Mathf.Min(items.Count, _itemButtons.Length);
        for (int i = 0; i < _itemButtons.Length; i++)
        {
            MuseumBottleItemButton button = _itemButtons[i];
            if (button == null) continue;
            if (i >= visibleCount || !inventory.TryGetItemDisplay(
                    items[i].AlcoholItemId, out InventoryItemDisplayData display))
            {
                button.Hide();
                continue;
            }
            button.Show(display, inventory.GetItemCount(items[i].AlcoholItemId));
        }
        if (items.Count > _itemButtons.Length)
            Debug.LogWarning("[MuseumBottleView] 고정 술 버튼 수가 CSV 요구 수보다 적습니다.", this);
    }

    public void SetSelectionEnabled(bool value)
    {
        for (int i = 0; i < _itemButtons.Length; i++)
            _itemButtons[i]?.SetInteractable(value);
    }

    private void HandleClose() => _closeRequested?.Invoke();
}
