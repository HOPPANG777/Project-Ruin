//=============================================================================
// File: ISettingsPopupView.cs
// Description: 실제 설정 시스템이 주입받는 표시·변경 알림 계약이다.
// History: 10/16/2026 + Created -- Codex
//=============================================================================
using UnityEngine.Events;

/// <summary>설정 시스템과 UI의 연결 경계입니다.</summary>
public interface ISettingsPopupView
{
    /// <summary>BGM 값 변경 알림입니다.</summary>
    UnityEvent<float> BgmVolumeChanged { get; }
    /// <summary>효과음 값 변경 알림입니다.</summary>
    UnityEvent<float> EffectsVolumeChanged { get; }
    /// <summary>화면 밝기 값 변경 알림입니다.</summary>
    UnityEvent<float> BrightnessChanged { get; }
    /// <summary>이벤트를 재발행하지 않고 0–1 설정 값을 표시합니다.</summary>
    void SetSettingsValues(float bgm, float effects, float brightness);
}
