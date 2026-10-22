//=============================================================================
// File: AchievementSlotView.cs
// Description: 업적 한 칸의 표시만 담당한다.
// History: 10/16/2026 + 참조 보호 및 표시 계약 정리 -- Codex
//=============================================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>업적 규칙과 독립적인 표시 슬롯입니다. 해금 판정과 보상 처리는 외부에서 수행합니다.</summary>
public sealed class AchievementSlotView : MonoBehaviour
{
    [Tooltip("업적 아이콘입니다. 없으면 숨깁니다.")]
    [SerializeField] private Image _icon;
    [Tooltip("업적 이름을 표시합니다.")]
    [SerializeField] private TMP_Text _title;
    [Tooltip("업적 설명을 표시합니다.")]
    [SerializeField] private TMP_Text _description;
    [Tooltip("호출부가 계산한 진행 문구를 표시합니다.")]
    [SerializeField] private TMP_Text _progressText;
    [Tooltip("0–1 진행률을 표시합니다.")]
    [SerializeField] private Image _progressFill;

    //=============================================================================
    // Description: 업적 표시를 갱신한다. Parameters: icon/title/description/progressText/progress. Return: void.
    //=============================================================================
    /// <summary>해금 판정이나 보상 처리 없이 전달받은 내용만 표시합니다.</summary>
    public void SetDisplay(Sprite icon, string title, string description, string progressText, float progress)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }
        if (_title != null) _title.text = title ?? string.Empty;
        if (_description != null) _description.text = description ?? string.Empty;
        if (_progressText != null) _progressText.text = progressText ?? string.Empty;
        if (_progressFill != null) _progressFill.fillAmount = Mathf.Clamp01(progress);
    }
}
