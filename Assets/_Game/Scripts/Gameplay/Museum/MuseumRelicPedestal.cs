//=============================================================================
// File:        MuseumRelicPedestal.cs
// Description: 씬 전시대 상호작용과 전시 유물 아이콘 표현을 연결한다.
// History:     08/30/2026 + Created -- Codex
//=============================================================================
//========================================
// System Includes
//========================================
using UnityEngine;

/// <summary>씬 전시대를 도메인에 직접 등록하고 상호작용 호출자와 UI를 연결합니다.</summary>
[DisallowMultipleComponent]
public sealed class MuseumRelicPedestal : MonoBehaviour, IInteractable, IInteractionContextReceiver
{
    [Tooltip("씬과 저장에서 유지할 전시대 고유 ID입니다.")]
    [SerializeField] private string _pedestalId;
    [Tooltip("현재 전시 유물 아이콘을 월드에 표시할 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer _displayRenderer;

    // 전시 상태 조회·변경 및 이벤트를 제공하는 서비스입니다.
    private IMuseumRelicDisplayService _displays;
    // 실제 상호작용 호출자가 전달한 플레이어 참조입니다.
    private PlayerInteractionController _playerInteractionController;
    // 실제 상호작용 호출자의 이동 잠금 대상입니다.
    private PlayerMoveController _playerMoveController;

    /// <summary>씬과 저장에서 사용하는 전시대 고유 ID입니다.</summary>
    public string PedestalId => _pedestalId;

    private void OnEnable() => TryBind();
    private void Start() => TryBind();

    private void OnDisable()
    {
        if (_displays != null) _displays.ExhibitChanged -= HandleExhibitChanged;
        _displays = null;
        _playerInteractionController = null;
        _playerMoveController = null;
    }

    //=============================================================================
    // SetInteractionContext
    // Description: 유물 UI가 잠글 플레이어 입력 컨트롤러를 전달받습니다.
    // Parameters: PlayerInteractionController interactionController, PlayerMoveController moveController
    // Return: void
    //=============================================================================
    /// <summary>유물 UI가 잠글 플레이어 입력 컨트롤러를 전달받습니다.</summary>
    public void SetInteractionContext(PlayerInteractionController interactionController,
        PlayerMoveController moveController)
    {
        _playerInteractionController = interactionController;
        _playerMoveController = moveController;
    }

    //=============================================================================
    // Interact
    // Description: 이 전시대의 유물 전용 선택 UI를 엽니다.
    // Parameters: None.
    // Return: void
    //=============================================================================
    /// <summary>이 전시대의 유물 전용 선택 UI를 엽니다.</summary>
    public void Interact()
    {
        if (!TryBind() || !GameRoot.TryGetInstance(out GameRoot root)
            || root.MuseumRelicPresenter == null)
        {
            Debug.LogWarning($"[MuseumRelicPedestal] 전시 UI가 준비되지 않았습니다. id={_pedestalId}", this);
            return;
        }
        root.MuseumRelicPresenter.Open(
            _pedestalId, _playerInteractionController, _playerMoveController);
    }

    private bool TryBind()
    {
        if (_displays != null) return true;
        if (!GameRoot.TryGetInstance(out GameRoot root) || root.MuseumRelicDisplays == null)
            return false;
        _displays = root.MuseumRelicDisplays;
        _displays.RegisterPedestal(_pedestalId);
        _displays.ExhibitChanged += HandleExhibitChanged;
        RefreshVisual();
        return true;
    }

    private void HandleExhibitChanged(string pedestalId)
    {
        if (pedestalId == _pedestalId) RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (_displayRenderer == null || _displays == null) return;
        Sprite sprite = null;
        if (_displays.TryGetDisplayedRelic(_pedestalId, out string relicId)
            && GameRoot.TryGetInstance(out GameRoot root) && root.Inventory != null
            && root.Inventory.TryGetItemDisplay(relicId, out InventoryItemDisplayData display))
            sprite = display.Icon;
        _displayRenderer.sprite = sprite;
        _displayRenderer.enabled = sprite != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_pedestalId))
            Debug.LogError($"[MuseumRelicPedestal] {name}: Pedestal ID가 비어 있습니다.", this);
        if (_displayRenderer == null)
            Debug.LogWarning($"[MuseumRelicPedestal] {name}: 전시 SpriteRenderer가 없습니다.", this);
    }
#endif
}
