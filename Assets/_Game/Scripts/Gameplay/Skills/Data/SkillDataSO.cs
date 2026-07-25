//=============================================================================
// File:        SkillDataSO.cs
//
// Description: 스킬의 고정 정보와 유적 재건 기여도를 보관한다.
//
// History:     07/22/2026 + 기술 재건 데이터 추가 -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using UnityEngine;

//=============================================================================
//
// Synopsis:   SkillDataSO;
//                - 스킬 ID와 표시 정보 정의
//                - 사용 분류와 재건 기여도 정의
//                - 플레이 중 변경되는 획득·반납 상태는 보관하지 않음
//
//=============================================================================
[CreateAssetMenu(menuName = "_Game/Skill/SkillData", fileName = "SkillDataSO")]
public sealed class SkillDataSO : ScriptableObject
{
    //========================================
    // 직렬화 필드
    //========================================

    [Header("Identity")]
    [Tooltip("저장과 조회에 사용하는 중복되지 않는 스킬 ID입니다.")]
    [SerializeField]
    private string _skillId;

    [Tooltip("스킬 이름을 현재 언어로 표시할 때 사용하는 Localization Key입니다.")]
    [SerializeField]
    private string _displayNameKey;

    [Tooltip("스킬 설명을 현재 언어로 표시할 때 사용하는 Localization Key입니다.")]
    [SerializeField]
    private string _descriptionKey;

    [Tooltip("스킬 선택 UI에 표시할 아이콘입니다.")]
    [SerializeField]
    private Sprite _icon;

    [Header("Gameplay")]
    [Tooltip("스킬의 사용 방식 분류입니다.")]
    [SerializeField]
    private SkillType _skillType = SkillType.SKILL_NONE;

    [Tooltip("스킬 사용 시 플레이어가 전환될 행동 상태입니다.")]
    [SerializeField]
    private SkillActionState _skillActionState = SkillActionState.STATE_NONE;

    [Header("Restoration")]
    [Tooltip("스킬을 획득하는 챕터 번호입니다.")]
    [Min(0)]
    [SerializeField]
    private int _chapter;

    [Tooltip("스킬을 되돌려놓을 때 증가하는 유적 재건도입니다.")]
    [Min(0)]
    [SerializeField]
    private int _restoreValue;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>저장과 조회에 사용하는 스킬 ID입니다.</summary>
    public string SkillId => _skillId;

    /// <summary>스킬 이름의 Localization Key입니다.</summary>
    public string DisplayNameKey => _displayNameKey;

    /// <summary>스킬 설명의 Localization Key입니다.</summary>
    public string DescriptionKey => _descriptionKey;

    /// <summary>스킬 선택 UI에 표시할 아이콘입니다.</summary>
    public Sprite Icon => _icon;

    /// <summary>스킬의 사용 방식 분류입니다.</summary>
    public SkillType SkillType => _skillType;

    /// <summary>스킬 사용 시 전환될 행동 상태입니다.</summary>
    public SkillActionState SkillActionState => _skillActionState;

    /// <summary>스킬을 획득하는 챕터 번호입니다.</summary>
    public int Chapter => _chapter;

    /// <summary>스킬을 되돌려놓을 때 증가하는 재건도입니다.</summary>
    public int RestoreValue => _restoreValue;
}
