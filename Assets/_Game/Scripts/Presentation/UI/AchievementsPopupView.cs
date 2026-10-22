//=============================================================================
// File: AchievementsPopupView.cs
// Description: 업적 목록의 표시 참조만 제공한다.
// History: 10/16/2026 + 창 전환 책임에서 분리 -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>업적 판정 없이 외부에서 목록을 채울 수 있는 표시 컴포넌트입니다.</summary>
public sealed class AchievementsPopupView : MonoBehaviour
{
    [Tooltip("업적 목록의 스크롤 영역입니다.")]
    [SerializeField] private ScrollRect _scroll;
    [Tooltip("현재 배치된 업적 표시 슬롯입니다.")]
    [SerializeField] private AchievementSlotView[] _slots = Array.Empty<AchievementSlotView>();
    [Tooltip("업적 창 상단의 요약 텍스트입니다.")]
    [SerializeField] private TMP_Text _summary;

    /// <summary>표시 내용을 채울 슬롯 목록입니다.</summary>
    public IReadOnlyList<AchievementSlotView> Slots => _slots;
    /// <summary>동적으로 추가한 슬롯을 배치할 스크롤입니다.</summary>
    public ScrollRect Scroll => _scroll;

    //=============================================================================
    // Description: 요약 문구를 표시한다. Parameters: value. Return: void.
    //=============================================================================
    /// <summary>해금 수나 진행률 계산은 호출부에서 수행합니다.</summary>
    public void SetSummary(string value)
    {
        if (_summary != null) _summary.text = value ?? string.Empty;
    }
}
