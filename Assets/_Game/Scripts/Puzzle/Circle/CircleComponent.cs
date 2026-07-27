using UnityEngine;
using UnityEngine.EventSystems;

//=============================================================================
// File:        CircleComponent.cs
//
// Description: 원형 퍼즐 링의 문양, 시작 회전, 드래그 및 단계 스냅을 처리한다.
//
// History:     07/26/2026 + CSV 시작 오프셋 기반 초기화로 전환 -- VicZero
//
//=============================================================================
public sealed class UiCircleComponent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Ring State")]
    [Tooltip("한 바퀴를 나누는 회전 단계 개수입니다.")]
    [SerializeField]
    private int _segments = 8;

    [Tooltip("정답 위치 0을 기준으로 현재 떨어진 회전 단계입니다.")]
    [SerializeField]
    private int _currentSegmentOffset;

    private RectTransform _rectTransform;
    private UiCirclePuzzleManager _ownerManager;
    private float _startMouseAngle;
    private float _startRingAngle;
    private float _initialAngle;
    private bool _isInitialized;

    /// <summary>한 회전 단계가 차지하는 각도입니다.</summary>
    public float AnglePerSegment => 360f / _segments;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (_isInitialized)
        {
            ApplyInitialRotation();
        }
    }

    /// <summary>CSV 링 정의와 문양 프리팹으로 원형 퍼즐 링을 초기화합니다.</summary>
    public void InitRingState(
        int totalSegments,
        int initialOffset,
        GameObject patternPrefab,
        UiCirclePuzzleManager manager)
    {
        _segments = totalSegments;
        _currentSegmentOffset = initialOffset;
        _ownerManager = manager;
        _isInitialized = false;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        if (patternPrefab != null)
        {
            GameObject patternObject = Instantiate(patternPrefab, transform);
            RectTransform patternRect = patternObject.GetComponent<RectTransform>();

            if (patternRect != null)
            {
                patternRect.anchoredPosition = Vector2.zero;
                patternRect.localRotation = Quaternion.identity;
                patternRect.localScale = Vector3.one;
            }
        }

        _initialAngle = _currentSegmentOffset * AnglePerSegment;
        _isInitialized = true;

        if (gameObject.activeInHierarchy)
        {
            ApplyInitialRotation();
        }
    }

    /// <summary>링의 화면 회전 각도를 변경합니다.</summary>
    public void SetVisualRotation(float angle)
    {
        if (_rectTransform != null)
        {
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>마우스 드래그를 시작할 때 기준 각도를 기록합니다.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_ownerManager != null && _ownerManager.IsClear)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localMousePosition);
        _startMouseAngle = Mathf.Atan2(localMousePosition.y, localMousePosition.x) * Mathf.Rad2Deg;
        _startRingAngle = _rectTransform.localEulerAngles.z;
    }

    /// <summary>마우스 각도 변화만큼 링을 연속 회전합니다.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_ownerManager != null && _ownerManager.IsClear)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localMousePosition);
        float currentMouseAngle = Mathf.Atan2(localMousePosition.y, localMousePosition.x) * Mathf.Rad2Deg;
        SetVisualRotation(_startRingAngle + currentMouseAngle - _startMouseAngle);
    }

    /// <summary>드래그 종료 각도를 가장 가까운 회전 단계에 맞추고 정답을 검사합니다.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToSegment();
        _ownerManager?.CheckClearCondition();
    }

    /// <summary>현재 화면 각도를 가장 가까운 정수 회전 단계에 맞춥니다.</summary>
    public void SnapToSegment()
    {
        if (_rectTransform == null)
        {
            return;
        }

        float currentAngle = _rectTransform.localEulerAngles.z % 360f;

        if (currentAngle < 0f)
        {
            currentAngle += 360f;
        }

        int closestSegment = Mathf.RoundToInt(currentAngle / AnglePerSegment);

        if (closestSegment >= _segments)
        {
            closestSegment = 0;
        }

        _currentSegmentOffset = closestSegment;

        if (_currentSegmentOffset > _segments / 2)
        {
            _currentSegmentOffset -= _segments;
        }

        SetVisualRotation(closestSegment * AnglePerSegment);
    }

    /// <summary>링이 정답 오프셋 0에 정렬됐는지 반환합니다.</summary>
    public bool IsAligned()
    {
        return _currentSegmentOffset == 0;
    }

    private void ApplyInitialRotation()
    {
        SetVisualRotation(_initialAngle);
    }
}
