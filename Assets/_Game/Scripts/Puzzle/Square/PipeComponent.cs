using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//=============================================================================
// File:        PipeComponent.cs
//
// Description: 파이프 타일의 회전, 드래그 입력과 프리팹에 붙인 Off/On 표시를 담당한다.
//
// History:     07/26/2026 + Created -- VicZero
//              08/21/2026 + Off/On Image 프리팹 전환 -- VicZero
//
//=============================================================================
public class PipeComponent : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Visuals")]
    [Tooltip("연결 전 파이프 Image입니다. 프리팹 자식 Off에 연결합니다.")]
    [SerializeField]
    private Image _offImage;

    [Tooltip("연결 후 파이프 Image입니다. 프리팹 자식 On에 연결합니다.")]
    [SerializeField]
    private Image _onImage;

    [Tooltip("Sprite 기본 각도가 논리 방향과 다를 때 더할 시계 방향 90도 횟수입니다.")]
    [SerializeField]
    private int _visualRotationOffset;

    public PipeData Data { get; private set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsWatered { get; set; }

    private PipeManager _ownerSpawner;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>타일 데이터와 격자 위치를 저장하고 프리팹 비주얼을 맞춥니다.</summary>
    public void Setup(PipeData tileData, int row, int column, PipeManager spawner)
    {
        Data = tileData;
        Row = row;
        Col = column;
        _ownerSpawner = spawner;

        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        UpdateVisualRotation();
        UpdateVisualColor();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_ownerSpawner != null && _ownerSpawner.IsClear)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Rotate();
        }
    }

    public void Rotate()
    {
        if (Data.type == PipeType.Empty || Data.type == PipeType.Start || Data.type == PipeType.End)
        {
            return;
        }

        PipeData updatedData = Data;
        updatedData.rotationIndex = (updatedData.rotationIndex + 1) % 4;
        Data = updatedData;

        UpdateVisualRotation();
        if (_ownerSpawner != null)
        {
            _ownerSpawner.OnTileStateChanged();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_ownerSpawner != null && !_ownerSpawner.IsClear && eventData.button == PointerEventData.InputButton.Left)
        {
            _ownerSpawner.OnTileBeginDrag(this, eventData.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ownerSpawner != null && !_ownerSpawner.IsClear && eventData.button == PointerEventData.InputButton.Left)
        {
            _ownerSpawner.OnTileDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ownerSpawner != null && eventData.button == PointerEventData.InputButton.Left)
        {
            _ownerSpawner.OnTileEndDrag();
        }
    }

    /// <summary>물 연결 여부에 따라 프리팹의 Off/On Image를 켭니다.</summary>
    public void UpdateVisualColor()
    {
        bool showOn = IsWatered;

        if (_onImage != null)
        {
            _onImage.enabled = showOn || _offImage == null;
            _onImage.gameObject.SetActive(showOn || _offImage == null);
        }

        if (_offImage != null)
        {
            _offImage.enabled = !showOn;
            _offImage.gameObject.SetActive(!showOn);
        }
    }

    private void UpdateVisualRotation()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_rectTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] RectTransform 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        int visualRotation = (Data.rotationIndex + _visualRotationOffset) & 3;
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, -visualRotation * 90f);
    }
}
