//=============================================================================
// File:        SkillController.cs
// Description: 환경 스킬의 등록 정보와 영구 획득 상태를 관리한다.
// History:     07/31/2026 + Created -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
// 환경 스킬 획득이 실패한 이유입니다.
public enum SkillAcquireFailure
{
    SKILL_FAILURE_NONE = 0,
    SKILL_FAILURE_NOT_INITIALIZED,
    SKILL_FAILURE_INVALID_ID,
    SKILL_FAILURE_NOT_FOUND,
    SKILL_FAILURE_ALREADY_ACQUIRED
}

// 환경 스킬 등록 정보와 획득 상태의 외부 계약입니다.
public interface ISkillService
{
    // 레거시 Technology Key와 겹치지 않도록 환경 스킬 전용 영역을 사용합니다.
    const string SKILL_ACQUIRED_FLAG_PREFIX = "skill.environment.acquired.";
    /// <summary>등록된 환경 스킬인지 확인합니다.</summary>
    bool IsKnownSkill(string skillId);
    /// <summary>환경 스킬 획득 여부를 확인합니다.</summary>
    bool IsAcquired(string skillId);
    /// <summary>ID에 해당하는 환경 스킬 데이터를 찾습니다.</summary>
    bool TryGetSkill(string skillId, out SkillDataSO skill);
    /// <summary>등록된 환경 스킬을 처음 획득한 상태로 변경합니다.</summary>
    bool TryAcquireSkill(string skillId, out SkillAcquireFailure failure);
    /// <summary>획득한 환경 스킬을 재사용 목록에 채웁니다.</summary>
    int FillAcquiredSkills(List<SkillDataSO> buffer);
}

// 환경 스킬 ID 캐시와 Progress 획득 플래그의 단일 진입점입니다.
[DisallowMultipleComponent]
public sealed class SkillController : MonoBehaviour, ISkillService
{
    private readonly struct SkillRecord
    {
        internal readonly SkillDataSO Skill;
        internal readonly string AcquiredFlagId;
        internal SkillRecord(SkillDataSO skill)
        {
            Skill = skill;
            AcquiredFlagId = string.Concat(
                ISkillService.SKILL_ACQUIRED_FLAG_PREFIX, skill.SkillId);
        }
    }

    // 직렬화 필드
    [Header("Skills")]
    [Tooltip("플레이어가 영구 획득할 수 있는 환경 스킬 목록입니다.")]
    [SerializeField] private SkillDataSO[] _skills = Array.Empty<SkillDataSO>();

    // ID와 저장 Key는 조회 때 다시 만들지 않도록 초기화에서 캐시합니다.
    private readonly Dictionary<string, SkillRecord> _skillById =
        new Dictionary<string, SkillRecord>(StringComparer.Ordinal);
    private IProgressService _progressService;
    private bool _isInitialized;

    /// <summary>Progress를 주입하고 등록된 환경 스킬을 검증·캐시합니다.</summary>
    public bool Init(IProgressService progressService)
    {
        _isInitialized = false;
        _progressService = progressService;
        _skillById.Clear();
        if (_progressService == null)
        {
            Debug.LogWarning("[SkillController] Progress가 연결되지 않았습니다.");
            return false;
        }
        if (_skills == null || _skills.Length == 0)
        {
            Debug.LogWarning("[SkillController] 등록된 환경 스킬이 없습니다.");
            return false;
        }
        for (int i = 0; i < _skills.Length; i++)
        {
            SkillDataSO skill = _skills[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            {
                Debug.LogWarning($"[SkillController] 스킬 {i}번 항목의 참조 또는 ID가 비어 있습니다.");
                return false;
            }
            if (_skillById.ContainsKey(skill.SkillId))
            {
                Debug.LogWarning($"[SkillController] 중복 스킬 ID가 있습니다. id={skill.SkillId}");
                return false;
            }
            _skillById.Add(skill.SkillId, new SkillRecord(skill));
        }
        _isInitialized = true;
        return true;
    }
    /// <summary>등록된 환경 스킬인지 확인합니다.</summary>
    public bool IsKnownSkill(string skillId) => TryGetRecord(skillId, out _, out _);
    /// <summary>환경 스킬 획득 여부를 확인합니다.</summary>
    public bool IsAcquired(string skillId) =>
        TryGetRecord(skillId, out SkillRecord record, out _)
        && _progressService.GetFlag(record.AcquiredFlagId);
    /// <summary>ID에 해당하는 환경 스킬 데이터를 찾습니다.</summary>
    public bool TryGetSkill(string skillId, out SkillDataSO skill)
    {
        bool found = TryGetRecord(skillId, out SkillRecord record, out _);
        skill = found ? record.Skill : null;
        return found;
    }
    /// <summary>등록된 환경 스킬을 처음 획득한 상태로 변경합니다.</summary>
    public bool TryAcquireSkill(string skillId, out SkillAcquireFailure failure)
    {
        if (!TryGetRecord(skillId, out SkillRecord record, out failure)) return false;
        if (_progressService.GetFlag(record.AcquiredFlagId))
        {
            failure = SkillAcquireFailure.SKILL_FAILURE_ALREADY_ACQUIRED;
            return false;
        }
        _progressService.SetFlag(record.AcquiredFlagId, true);
        failure = SkillAcquireFailure.SKILL_FAILURE_NONE;
        return true;
    }
    /// <summary>획득한 환경 스킬을 전달받은 재사용 목록에 채웁니다.</summary>
    public int FillAcquiredSkills(List<SkillDataSO> buffer)
    {
        if (buffer == null) return 0;
        buffer.Clear();
        if (!_isInitialized) return 0;
        for (int i = 0; i < _skills.Length; i++)
        {
            SkillRecord record = _skillById[_skills[i].SkillId];
            if (_progressService.GetFlag(record.AcquiredFlagId)) buffer.Add(record.Skill);
        }
        return buffer.Count;
    }
    private bool TryGetRecord(string skillId, out SkillRecord record,
        out SkillAcquireFailure failure)
    {
        record = default;
        if (!_isInitialized)
        {
            failure = SkillAcquireFailure.SKILL_FAILURE_NOT_INITIALIZED;
            return false;
        }
        if (string.IsNullOrWhiteSpace(skillId))
        {
            failure = SkillAcquireFailure.SKILL_FAILURE_INVALID_ID;
            return false;
        }
        if (!_skillById.TryGetValue(skillId, out record))
        {
            failure = SkillAcquireFailure.SKILL_FAILURE_NOT_FOUND;
            return false;
        }
        failure = SkillAcquireFailure.SKILL_FAILURE_NONE;
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate() => ValidateRegisteredSkills(false);
    [ContextMenu("등록 스킬 검증")]
    private void ValidateRegisteredSkills() => ValidateRegisteredSkills(true);
    private void ValidateRegisteredSkills(bool isManualValidation)
    {
        if (_skills == null || _skills.Length == 0)
        {
            LogValidationIssue("등록된 환경 스킬이 없습니다.", isManualValidation);
            return;
        }
        for (int i = 0; i < _skills.Length; i++)
        {
            SkillDataSO skill = _skills[i];
            if (skill == null)
            {
                LogValidationIssue($"스킬 목록 {i}번 참조가 비어 있습니다.", isManualValidation);
                continue;
            }
            if (string.IsNullOrWhiteSpace(skill.SkillId))
            {
                LogValidationIssue($"스킬 목록 {i}번 ID가 비어 있습니다.", isManualValidation);
                continue;
            }
            for (int compareIndex = i + 1; compareIndex < _skills.Length; compareIndex++)
            {
                SkillDataSO other = _skills[compareIndex];
                if (other != null && skill.SkillId == other.SkillId)
                {
                    LogValidationIssue($"중복 스킬 ID가 있습니다. id={skill.SkillId}", isManualValidation);
                    break;
                }
            }
        }
    }
    private void LogValidationIssue(string message, bool isError)
    {
        if (isError) Debug.LogError($"[SkillController] {name}: {message}", this);
        else Debug.LogWarning($"[SkillController] {name}: {message}", this);
    }
#endif
}
