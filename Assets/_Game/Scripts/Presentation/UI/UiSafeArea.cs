//=============================================================================
// File: UiSafeArea.cs
// Description: 화면 영역이 변경된 경우에만 UI 앵커를 갱신한다.
// History: 10/16/2026 + 타이틀 표시 책임에서 분리 -- Codex
//=============================================================================
using UnityEngine;

/// <summary>같은 오브젝트의 RectTransform에 기기의 Safe Area를 적용합니다.</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class UiSafeArea : MonoBehaviour
{
    // 자신의 UI 참조를 한 번만 조회해 보관합니다.
    private RectTransform _rect;
    // 마지막으로 적용한 Safe Area입니다.
    private Rect _lastArea;
    // 마지막으로 적용한 화면 크기입니다.
    private Vector2Int _lastSize;

    private void Awake() => _rect = GetComponent<RectTransform>();

    // 화면 상태가 그대로면 RectTransform을 다시 계산하지 않습니다.
    private void Update()
    {
        if (_rect == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (_lastArea == area && _lastSize == size) return;
        _lastArea = area;
        _lastSize = size;
        _rect.anchorMin = area.min / (Vector2)size;
        _rect.anchorMax = area.max / (Vector2)size;
        _rect.offsetMin = _rect.offsetMax = Vector2.zero;
    }
}
