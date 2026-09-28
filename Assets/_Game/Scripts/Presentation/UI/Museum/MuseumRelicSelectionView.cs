//=============================================================================
// File:        MuseumRelicSelectionView.cs
// Description: 유물 전용 선택 목록과 현재 전시 상태를 표시한다.
// History:     08/30/2026 + Created -- Codex
//=============================================================================
//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>고정 슬롯을 재사용하여 유물 선택과 회수 상태만 표시합니다.</summary>
[DisallowMultipleComponent]
public sealed class MuseumRelicSelectionView : MonoBehaviour
{
    [Tooltip("유물 전용 선택 UI 전체의 최상위 오브젝트입니다.")]
    [SerializeField] private GameObject _panelRoot;
    [Tooltip("현재 배치 가능한 유물이 없을 때 표시할 오브젝트입니다.")]
    [SerializeField] private GameObject _emptyRoot;
    [Tooltip("현재 전시 수와 전체 유물 수를 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text _progressText;
    [Tooltip("선택한 전시대에 현재 배치된 유물 이름을 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text _currentRelicText;
    [Tooltip("최대 유물 종류 수만큼 미리 배치해 재사용할 선택 버튼입니다.")]
    [SerializeField] private MuseumRelicItemButton[] _itemButtons =
        Array.Empty<MuseumRelicItemButton>();
    [Tooltip("현재 전시 유물을 인벤토리로 회수하는 버튼입니다.")]
    [SerializeField] private Button _recoverButton;
    [Tooltip("유물 전용 선택 UI를 닫는 버튼입니다.")]
    [SerializeField] private Button _closeButton;
    // 회수 버튼에서 Presenter로 전달할 요청입니다.
    private Action _recoverRequested;
    // 닫기 버튼에서 Presenter로 전달할 요청입니다.
    private Action _closeRequested;
    // 입력을 다시 켤 때 복원할 회수 가능 상태입니다.
    private bool _canRecover;

    /// <summary>유물 전용 선택 UI가 현재 열려 있는지 여부입니다.</summary>
    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    // 프리팹 편집 시에는 보이게 저장하고, 실행 시 첫 프레임 전에 닫습니다.
    private void Awake() => Hide();

    private void OnDestroy()
    {
        _recoverButton?.onClick.RemoveListener(HandleRecover);
        _closeButton?.onClick.RemoveListener(HandleClose);
    }

    //=============================================================================
    // Init
    // Description: 선택·회수·닫기 콜백을 중복 없이 연결합니다.
    // Parameters: Action<int> selected, Action recover, Action close
    // Return: void
    //=============================================================================
    /// <summary>선택·회수·닫기 콜백을 중복 없이 연결합니다.</summary>
    public void Init(Action<int> selected, Action recover, Action close)
    {
        _recoverRequested = recover;
        _closeRequested = close;
        _recoverButton?.onClick.RemoveListener(HandleRecover);
        _closeButton?.onClick.RemoveListener(HandleClose);
        _recoverButton?.onClick.AddListener(HandleRecover);
        _closeButton?.onClick.AddListener(HandleClose);
        for (int i = 0; i < _itemButtons.Length; i++) _itemButtons[i]?.Init(i, selected);
        Hide();
    }

    //=============================================================================
    // TryShow
    // Description: 필수 루트가 연결됐을 때 유물 선택 UI를 표시합니다.
    // Parameters: None.
    // Return: bool
    //=============================================================================
    /// <summary>필수 루트가 연결됐을 때 유물 선택 UI를 표시합니다.</summary>
    public bool TryShow()
    {
        if (_panelRoot == null) return false;
        _panelRoot.SetActive(true);
        return true;
    }

    //=============================================================================
    // Hide
    // Description: 유물 선택 UI를 숨깁니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
    /// <summary>유물 선택 UI를 숨깁니다.</summary>
    public void Hide() => _panelRoot?.SetActive(false);

    //=============================================================================
    // Refresh
    // Description: 현재 전시 진행도와 배치 가능한 유물 버튼을 갱신합니다.
    // Parameters: int displayedCount, int totalCount, string currentName, IReadOnlyList<string> itemIds, IInventoryService inventory
    // Return: void
    //=============================================================================
    /// <summary>현재 전시 진행도와 배치 가능한 유물 버튼을 갱신합니다.</summary>
    public void Refresh(int displayedCount, int totalCount, string currentName,
        IReadOnlyList<string> itemIds, IInventoryService inventory)
    {
        _progressText?.SetText("전시 {0} / {1}", displayedCount, totalCount);
        if (_currentRelicText != null)
            _currentRelicText.text = string.IsNullOrEmpty(currentName)
                ? "현재 전시: 없음" : $"현재 전시: {currentName}";
        _canRecover = !string.IsNullOrEmpty(currentName);
        SetInputEnabled(true);
        _emptyRoot?.SetActive(itemIds.Count == 0);
        int visible = Mathf.Min(itemIds.Count, _itemButtons.Length);
        for (int i = 0; i < _itemButtons.Length; i++)
        {
            MuseumRelicItemButton button = _itemButtons[i];
            if (button == null) continue;
            if (i >= visible || !inventory.TryGetItemDisplay(
                    itemIds[i], out InventoryItemDisplayData display)) button.Hide();
            else button.Show(display, inventory.GetItemCount(itemIds[i]));
        }
        if (itemIds.Count > _itemButtons.Length)
            Debug.LogWarning("[MuseumRelicSelectionView] 고정 유물 버튼 수가 부족합니다.", this);
    }

    //=============================================================================
    // SetInputEnabled
    // Description: 배치 처리 중 선택·회수·닫기 입력 가능 여부를 설정합니다.
    // Parameters: bool value
    // Return: void
    //=============================================================================
    /// <summary>배치 처리 중 선택·회수·닫기 입력 가능 여부를 설정합니다.</summary>
    public void SetInputEnabled(bool value)
    {
        for (int i = 0; i < _itemButtons.Length; i++) _itemButtons[i]?.SetInteractable(value);
        if (_recoverButton != null) _recoverButton.interactable = _canRecover && value;
        if (_closeButton != null) _closeButton.interactable = value;
    }

    // 회수 버튼에서 Presenter로 전달할 요청입니다.
    private void HandleRecover() => _recoverRequested?.Invoke();
    // 닫기 버튼에서 Presenter로 전달할 요청입니다.
    private void HandleClose() => _closeRequested?.Invoke();
}
