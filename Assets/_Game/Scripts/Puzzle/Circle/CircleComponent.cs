using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//=============================================================================
// File:        CircleComponent.cs
//
// Description: 원형 퍼즐 링을 해상도와 무관한 UI 메시로 그리고,
//              화면에 보이는 링 범위에서만 드래그 입력을 처리한다.
//
// History:     07/26/2026 + CSV 시작 오프셋 기반 초기화로 전환 -- VicZero
//              07/29/2026 + 절차적 링 표시와 원형 입력 판정 추가 -- Codex
//              07/29/2026 + 화면 좌표 기반 누적 드래그 회전 적용 -- Codex
//
//=============================================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class UiCircleComponent : MaskableGraphic,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Ring Visual")]
    [Tooltip("원을 구성하는 면의 개수입니다. 값이 클수록 원이 부드럽게 보입니다.")]
    [SerializeField]
    [Min(16)]
    private int _visualResolution = 128;

    [Tooltip("화면에 보이는 원형 띠의 두께입니다.")]
    [SerializeField]
    [Min(1f)]
    private float _ringThickness = 14f;

    [Tooltip("잡기 편하도록 보이는 띠의 안쪽에 추가하는 입력 여유 범위입니다.")]
    [SerializeField]
    [Min(0f)]
    private float _raycastPadding = 6f;

    [Tooltip("PNG 링을 표시할 자식 Image입니다. Sprite가 없으면 절차적 링을 유지합니다.")]
    [SerializeField]
    private Image _ringImage;

    [Header("Ring State")]
    [Tooltip("한 바퀴를 나누는 회전 단계 개수입니다.")]
    [SerializeField]
    [Min(1)]
    private int _segments = 8;

    [Tooltip("정답 위치 0을 기준으로 현재 떨어진 회전 단계입니다.")]
    [SerializeField]
    private int _currentSegmentOffset;

    private UiCirclePuzzleManager _ownerManager;
    private float _previousMouseAngle;
    private float _dragRingAngle;
    private float _initialAngle;
    private bool _isInitialized;
    private bool _isDragging;

    /// <summary>한 회전 단계가 차지하는 각도를 반환한다.</summary>
    public float AnglePerSegment => 360f / _segments;

    /// <summary>PNG 링 Sprite를 적용하며, 누락되면 절차적 링 표시를 유지합니다.</summary>
    public void SetRingSprite(Sprite ringSprite)
    {
        if (_ringImage != null)
        {
            _ringImage.sprite = ringSprite;
            _ringImage.enabled = ringSprite != null;
            _ringImage.preserveAspect = true;
        }

        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_isInitialized)
        {
            ApplyInitialRotation();
        }
    }

    protected override void OnDisable()
    {
        _isDragging = false;
        base.OnDisable();
    }

    /// <summary>CSV 링 정의와 문양 프리팹으로 원형 퍼즐 링을 초기화한다.</summary>
    public void InitRingState(
        int totalSegments,
        int initialOffset,
        GameObject patternPrefab,
        UiCirclePuzzleManager manager)
    {
        _segments = Mathf.Max(1, totalSegments);
        _currentSegmentOffset = initialOffset;
        _ownerManager = manager;
        _isInitialized = false;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (_ringImage != null && child == _ringImage.transform)
            {
                continue;
            }

            Destroy(child.gameObject);
        }

        if (patternPrefab != null)
        {
            CreatePattern(patternPrefab);
        }

        _initialAngle = _currentSegmentOffset * AnglePerSegment;
        _isInitialized = true;

        if (gameObject.activeInHierarchy)
        {
            ApplyInitialRotation();
        }
    }

    /// <summary>링의 화면 회전 각도를 변경한다.</summary>
    public void SetVisualRotation(float angle)
    {
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>마우스 드래그 시작 시 기준 각도를 기록한다.</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = false;

        if (_ownerManager != null && _ownerManager.IsClear)
        {
            return;
        }

        if (!TryGetScreenMouseAngle(eventData, out float mouseAngle))
        {
            return;
        }

        _previousMouseAngle = mouseAngle;
        _dragRingAngle = rectTransform.localEulerAngles.z;
        _isDragging = true;
    }

    /// <summary>마우스의 각도 변화만큼 링을 계속 회전한다.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || (_ownerManager != null && _ownerManager.IsClear))
        {
            return;
        }

        if (!TryGetScreenMouseAngle(eventData, out float currentMouseAngle))
        {
            return;
        }

        // 회전하는 링 자신의 좌표를 사용하면 좌표계도 함께 돌아 입력이 앞뒤로 떨린다.
        // 화면상의 실제 마우스 각도 변화만 누적해 마우스 속도와 링 회전을 일치시킨다.
        float angleDelta = Mathf.DeltaAngle(_previousMouseAngle, currentMouseAngle);
        _dragRingAngle += angleDelta;
        _previousMouseAngle = currentMouseAngle;
        SetVisualRotation(_dragRingAngle);
    }

    /// <summary>드래그 종료 각도를 가장 가까운 회전 단계에 맞추고 정답을 검사한다.</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        SnapToSegment();
        _ownerManager?.CheckClearCondition();
    }

    /// <summary>현재 화면 각도를 가장 가까운 정수 회전 단계에 맞춘다.</summary>
    public void SnapToSegment()
    {
        float currentAngle = rectTransform.localEulerAngles.z % 360f;

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

    /// <summary>링이 정답 오프셋 0에 정렬됐는지 반환한다.</summary>
    public bool IsAligned()
    {
        return _currentSegmentOffset == 0;
    }

    /// <summary>보이는 링과 같은 원형 띠 범위에서만 포인터 입력을 허용한다.</summary>
    public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
    {
        if (!base.Raycast(screenPoint, eventCamera))
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenPoint,
            eventCamera,
            out Vector2 localPoint))
        {
            return false;
        }

        Rect ringRect = rectTransform.rect;
        float radius = Vector2.Distance(localPoint, ringRect.center);
        float outerRadius = Mathf.Min(ringRect.width, ringRect.height) * 0.5f;
        float innerRadius = Mathf.Max(0f, outerRadius - _ringThickness);

        return radius >= Mathf.Max(0f, innerRadius - _raycastPadding)
            && radius <= outerRadius;
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (_ringImage != null && _ringImage.enabled && _ringImage.sprite != null)
        {
            return;
        }

        Rect ringRect = rectTransform.rect;
        float outerRadius = Mathf.Min(ringRect.width, ringRect.height) * 0.5f;
        float innerRadius = Mathf.Max(0f, outerRadius - _ringThickness);

        if (outerRadius <= 0f || innerRadius >= outerRadius)
        {
            return;
        }

        int resolution = Mathf.Max(16, _visualResolution);
        Vector2 center = ringRect.center;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        for (int i = 0; i < resolution; i++)
        {
            float startAngle = i * Mathf.PI * 2f / resolution;
            float endAngle = (i + 1) * Mathf.PI * 2f / resolution;
            Vector2 startDirection = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle));
            Vector2 endDirection = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle));
            int vertexStart = vertexHelper.currentVertCount;

            vertex.position = center + startDirection * outerRadius;
            vertexHelper.AddVert(vertex);
            vertex.position = center + startDirection * innerRadius;
            vertexHelper.AddVert(vertex);
            vertex.position = center + endDirection * innerRadius;
            vertexHelper.AddVert(vertex);
            vertex.position = center + endDirection * outerRadius;
            vertexHelper.AddVert(vertex);

            vertexHelper.AddTriangle(vertexStart, vertexStart + 1, vertexStart + 2);
            vertexHelper.AddTriangle(vertexStart, vertexStart + 2, vertexStart + 3);
        }
    }

    private void CreatePattern(GameObject patternPrefab)
    {
        GameObject patternObject = Instantiate(patternPrefab, transform);
        RectTransform patternRect = patternObject.GetComponent<RectTransform>();

        if (patternRect == null)
        {
            return;
        }

        patternRect.anchoredPosition = Vector2.zero;
        patternRect.localRotation = Quaternion.identity;

        // 부모 링의 확대 배율을 상쇄해 모든 표식이 화면에서 같은 크기로 보이게 한다.
        float parentScale = Mathf.Abs(rectTransform.localScale.x);
        float inverseScale = parentScale > Mathf.Epsilon ? 1f / parentScale : 1f;
        patternRect.localScale = Vector3.one * inverseScale;
    }

    private bool TryGetScreenMouseAngle(PointerEventData eventData, out float mouseAngle)
    {
        Vector2 ringCenter = RectTransformUtility.WorldToScreenPoint(
            eventData.pressEventCamera,
            rectTransform.position);
        Vector2 mouseDirection = eventData.position - ringCenter;

        if (mouseDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            mouseAngle = 0f;
            return false;
        }

        mouseAngle = Mathf.Atan2(mouseDirection.y, mouseDirection.x) * Mathf.Rad2Deg;
        return true;
    }

    private void ApplyInitialRotation()
    {
        SetVisualRotation(_initialAngle);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        _visualResolution = Mathf.Max(16, _visualResolution);
        _ringThickness = Mathf.Max(1f, _ringThickness);
        _raycastPadding = Mathf.Max(0f, _raycastPadding);
        _segments = Mathf.Max(1, _segments);
        SetVerticesDirty();
    }
#endif
}
