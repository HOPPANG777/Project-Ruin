//=============================================================================
// File: SettingsPopupView.cs
// Description: 세 설정 슬라이더의 표시 계약을 제공한다.
// History: 10/16/2026 + 팝업 전환 책임에서 분리 -- Codex
//=============================================================================
using UnityEngine;
using UnityEngine.Events;

/// <summary>외부 설정 시스템이 ISettingsPopupView로 주입받는 UI입니다.</summary>
public sealed class SettingsPopupView : MonoBehaviour, ISettingsPopupView
{
    [Tooltip("BGM 표시와 변경 이벤트입니다.")]
    [SerializeField] private SettingSliderView _bgm;
    [Tooltip("효과음 표시와 변경 이벤트입니다.")]
    [SerializeField] private SettingSliderView _effects;
    [Tooltip("화면 밝기 표시와 변경 이벤트입니다.")]
    [SerializeField] private SettingSliderView _brightness;

    /// <summary>BGM 변경 이벤트입니다.</summary>
    public UnityEvent<float> BgmVolumeChanged => _bgm != null ? _bgm.ValueChanged : null;
    /// <summary>효과음 변경 이벤트입니다.</summary>
    public UnityEvent<float> EffectsVolumeChanged => _effects != null ? _effects.ValueChanged : null;
    /// <summary>밝기 변경 이벤트입니다.</summary>
    public UnityEvent<float> BrightnessChanged => _brightness != null ? _brightness.ValueChanged : null;

    //=============================================================================
    // Description: 설정 표시를 동기화한다. Parameters: bgm/effects/brightness(0–1). Return: void.
    //=============================================================================
    /// <summary>실제 게임 설정을 적용하거나 저장하지 않습니다.</summary>
    public void SetSettingsValues(float bgm, float effects, float brightness)
    {
        if (_bgm != null) _bgm.SetValue(bgm);
        if (_effects != null) _effects.SetValue(effects);
        if (_brightness != null) _brightness.SetValue(brightness);
    }
}
