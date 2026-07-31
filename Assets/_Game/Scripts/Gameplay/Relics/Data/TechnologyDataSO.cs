//=============================================================================
// File:        TechnologyDataSO.cs
//
// Description: 문명 재건에 사용하는 수집 기술의 고정 정보를 보관한다.
//
// History:     07/31/2026 + Created -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using UnityEngine;

//=============================================================================
//
// Synopsis:   TechnologyDataSO;
//                - 기술 ID와 표시 정보 정의
//                - 획득 챕터와 재건 기여도 정의
//                - 플레이 중 변경되는 획득·반납 상태는 보관하지 않음
//
//=============================================================================
[CreateAssetMenu(
    menuName = "_Game/Relics/TechnologyData",
    fileName = "TechnologyDataSO")]
public sealed class TechnologyDataSO : ScriptableObject
{
    //========================================
    // 직렬화 필드
    //========================================

    [Header("Identity")]
    [Tooltip("저장과 조회에 사용하는 중복되지 않는 기술 ID입니다.")]
    [SerializeField]
    private string _technologyId;

    [Tooltip("기술 이름을 현재 언어로 표시할 때 사용하는 Localization Key입니다.")]
    [SerializeField]
    private string _displayNameKey;

    [Tooltip("기술 설명을 현재 언어로 표시할 때 사용하는 Localization Key입니다.")]
    [SerializeField]
    private string _descriptionKey;

    [Tooltip("기술 목록과 획득 UI에 표시할 아이콘입니다.")]
    [SerializeField]
    private Sprite _icon;

    [Header("Restoration")]
    [Tooltip("기술을 획득하는 챕터 번호입니다.")]
    [Min(0)]
    [SerializeField]
    private int _chapter;

    [Tooltip("기술을 반환할 때 증가하는 문명 재건도입니다.")]
    [Min(0)]
    [SerializeField]
    private int _restoreValue;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>저장과 조회에 사용하는 기술 ID입니다.</summary>
    public string TechnologyId => _technologyId;

    /// <summary>기술 이름의 Localization Key입니다.</summary>
    public string DisplayNameKey => _displayNameKey;

    /// <summary>기술 설명의 Localization Key입니다.</summary>
    public string DescriptionKey => _descriptionKey;

    /// <summary>기술 목록과 획득 UI에 표시할 아이콘입니다.</summary>
    public Sprite Icon => _icon;

    /// <summary>기술을 획득하는 챕터 번호입니다.</summary>
    public int Chapter => _chapter;

    /// <summary>기술 반환 시 증가하는 문명 재건도입니다.</summary>
    public int RestoreValue => _restoreValue;
}
