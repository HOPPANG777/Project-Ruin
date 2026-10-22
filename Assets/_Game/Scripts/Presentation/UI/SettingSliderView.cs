//=============================================================================
// File: SettingSliderView.cs
// Description: 설정 슬라이더 하나의 수치 표시와 변경 이벤트를 담당한다.
// History: 10/16/2026 + 중복 슬라이더 처리 통합 -- Codex
//=============================================================================
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>실제 설정 적용 없이 0–1 입력과 백분율 표시만 처리합니다.</summary>
public sealed class SettingSliderView : MonoBehaviour
{
    [Tooltip("0–1 범위의 설정 슬라이더입니다.")]
    [SerializeField] private Slider _slider;
    [Tooltip("백분율을 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text _valueText;
    [Tooltip("실제 설정 시스템에서 구독할 변경 이벤트입니다.")]
    [SerializeField] private UnityEvent<float> _valueChanged = new UnityEvent<float>();

    /// <summary>실제 설정 적용을 연결할 이벤트입니다.</summary>
    public UnityEvent<float> ValueChanged => _valueChanged;

    // 초기 표시는 외부 설정을 변경하지 않습니다.
    private void Awake() { if (_slider != null) RefreshLabel(_slider.value); }

    //=============================================================================
    // Description: 외부 설정을 표시한다. Parameters: value(0–1). Return: void.
    //=============================================================================
    /// <summary>설정 동기화 시 변경 이벤트가 다시 발생하지 않게 합니다.</summary>
    public void SetValue(float value)
    {
        if (_slider == null) return;
        _slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        RefreshLabel(_slider.value);
    }

    //=============================================================================
    // Description: UI 변경을 전달한다. Parameters: value(0–1). Return: void.
    //=============================================================================
    /// <summary>Slider.onValueChanged에 연결하는 표시·이벤트 진입점입니다.</summary>
    public void OnValueChanged(float value)
    {
        value = Mathf.Clamp01(value);
        RefreshLabel(value);
        _valueChanged?.Invoke(value);
    }

    // 문자열 생성은 값이 변경될 때만 수행합니다.
    private void RefreshLabel(float value)
    {
        if (_valueText != null) _valueText.SetText("{0}%", Mathf.RoundToInt(value * 100));
    }
}
