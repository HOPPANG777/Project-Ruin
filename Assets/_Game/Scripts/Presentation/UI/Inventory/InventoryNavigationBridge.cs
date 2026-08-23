//=============================================================================
// File:        InventoryNavigationBridge.cs
// Description: 인벤토리 창 전환에 필요한 기존 UI 참조만 공개한다.
// History:     08/23/2026 + Created -- Codex
//=============================================================================

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InventoryNavigationBridge : MonoBehaviour
{
    [Tooltip("열기와 닫기를 수행할 기존 인벤토리 Presenter입니다.")]
    [SerializeField] private InventoryPresenter _presenter;
    [Tooltip("인벤토리의 실제 표시 상태를 나타내는 최상위 CanvasGroup입니다.")]
    [SerializeField] private CanvasGroup _rootCanvasGroup;
    [Tooltip("인벤토리에서 도감으로 이동하는 오른쪽 버튼입니다.")]
    [SerializeField] private Button _codexButton;

    public InventoryPresenter Presenter => _presenter;
    public CanvasGroup RootCanvasGroup => _rootCanvasGroup;
    public Button CodexButton => _codexButton;
}
