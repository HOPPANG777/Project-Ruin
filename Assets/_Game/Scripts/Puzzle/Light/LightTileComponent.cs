using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UiLightTileComponent : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual")]
    [Tooltip("타일 상태를 표시할 Image입니다. 비어 있으면 같은 오브젝트에서 찾습니다.")]
    [SerializeField] private Image _tileImage;
    [Tooltip("꺼진 상태에 사용할 PNG Sprite입니다.")]
    [SerializeField] private Sprite _offSprite;
    [Tooltip("켜진 상태에 사용할 PNG Sprite입니다.")]
    [SerializeField] private Sprite _onSprite;

    public int Id { get; private set; }
    public bool IsOn { get; private set; }

    private Sprite _fallbackSprite;
    private UiLightPuzzleManager ownerManager;

    public void Init(int tileId, bool initialState, UiLightPuzzleManager manager)
    {
        Id = tileId;
        IsOn = initialState;
        ownerManager = manager;
        if (_tileImage == null)
        {
            _tileImage = GetComponent<Image>();
        }

        _fallbackSprite = _tileImage != null ? _tileImage.sprite : null;
        UpdateVisual();
    }

    public void ToggleState()
    {
        IsOn = !IsOn;
        UpdateVisual();
    }

    // 유니티 UI 전용 클릭 이벤트 인터페이스 구현
    public void OnPointerClick(PointerEventData eventData)
    {
        // 좌클릭 시에만 작동하며, 이미 클리어된 상태면 조작 차단
        if (eventData.button == PointerEventData.InputButton.Left && ownerManager != null && !ownerManager.IsClear)
        {
            ownerManager.OnTileClicked(Id);
        }
    }

    private void UpdateVisual()
    {
        if (_tileImage == null)
        {
            return;
        }

        Sprite stateSprite = IsOn ? _onSprite : _offSprite;

        if (stateSprite != null)
        {
            _tileImage.sprite = stateSprite;
            _tileImage.color = Color.white;
            _tileImage.preserveAspect = true;
            return;
        }

        _tileImage.sprite = _fallbackSprite;
        _tileImage.color = IsOn ? Color.yellow : new Color(0.2f, 0.2f, 0.2f);
    }
}
