//=============================================================================
// File:        RelicRestorationController.cs
// Description: 기술 획득·반환을 검증하고 현재 문명 재건 상태를 계산한다.
// History:     07/22/2026 + Created -- Codex
//              07/31/2026 + Technology 용어와 저장 Key 캐시 적용 -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
//=============================================================================
// Synopsis:   RelicRestorationController;
//                - 기술 획득·반환 상태와 재건도 계산의 단일 진입점
//=============================================================================
[DisallowMultipleComponent]
public sealed class RelicRestorationController : MonoBehaviour, IRelicRestorationService
{
    private readonly struct TechnologyRecord
    {
        internal readonly TechnologyDataSO Technology;
        internal readonly string AcquiredFlagId;
        internal readonly string ReturnedFlagId;

        internal TechnologyRecord(TechnologyDataSO technology)
        {
            Technology = technology;
            AcquiredFlagId = string.Concat(
                IRelicRestorationService.TECHNOLOGY_ACQUIRED_FLAG_PREFIX, technology.TechnologyId);
            ReturnedFlagId = string.Concat(
                IRelicRestorationService.TECHNOLOGY_RETURNED_FLAG_PREFIX, technology.TechnologyId);
        }
    }
    //========================================
    // 직렬화 필드
    //========================================
    [Header("Data")]
    [Tooltip("기술, 복원 단계와 제단 목적지를 정의한 재건 Catalog입니다.")]
    [SerializeField] private RelicRestorationCatalogSO _catalog;

    // 저장 Key 문자열은 초기화에서 한 번만 생성합니다.
    private readonly Dictionary<string, TechnologyRecord> _technologyById =
        new Dictionary<string, TechnologyRecord>(StringComparer.Ordinal);
    private IProgressService _progressService;
    private int _currentRestore;
    private int _restoreStage;
    private bool _isInitialized;

    /// <summary>현재 문명 재건도입니다.</summary>
    public int CurrentRestore => _currentRestore;
    /// <summary>최대 문명 재건도입니다.</summary>
    public int MaxRestore => _catalog != null ? _catalog.MaxRestore : 0;
    /// <summary>현재 문명 복원 단계입니다.</summary>
    public int RestoreStage => _restoreStage;

    /// <summary>진행 상태를 주입하고 기술 ID와 저장 Key 캐시를 구성합니다.</summary>
    public bool Init(IProgressService progressService)
    {
        ResetState(progressService);
        if (_progressService == null || _catalog == null)
        {
            Debug.LogWarning("[RelicRestorationController] Progress 또는 Catalog가 연결되지 않았습니다.");
            return false;
        }
        if (!_catalog.TryValidate(out string errorMessage))
        {
            Debug.LogWarning($"[RelicRestorationController] Catalog 검증 실패: {errorMessage}");
            return false;
        }
        IReadOnlyList<TechnologyDataSO> technologies = _catalog.Technologies;
        for (int i = 0; i < technologies.Count; i++)
        {
            TechnologyDataSO technology = technologies[i];
            _technologyById.Add(technology.TechnologyId, new TechnologyRecord(technology));
        }
        _isInitialized = true;
        RefreshRestoreState();
        return true;
    }

    /// <summary>Catalog에 등록된 기술인지 확인합니다.</summary>
    public bool IsKnownTechnology(string technologyId) =>
        TryGetRecord(technologyId, out _, out _);

    /// <summary>반환 가능한 기술을 전달받은 재사용 목록에 채웁니다.</summary>
    public int FillReturnableTechnologies(List<TechnologyDataSO> buffer)
    {
        if (buffer == null) return 0;
        buffer.Clear();
        if (!_isInitialized) return 0;

        IReadOnlyList<TechnologyDataSO> technologies = _catalog.Technologies;
        for (int i = 0; i < technologies.Count; i++)
        {
            TechnologyRecord record = _technologyById[technologies[i].TechnologyId];
            if (GetAcquired(record) && !GetReturned(record)) buffer.Add(record.Technology);
        }
        return buffer.Count;
    }

    /// <summary>기술 획득 여부를 확인합니다.</summary>
    public bool IsAcquired(string technologyId) =>
        TryGetRecord(technologyId, out TechnologyRecord record, out _) && GetAcquired(record);

    /// <summary>기술 반환 여부를 확인합니다.</summary>
    public bool IsReturned(string technologyId) =>
        TryGetRecord(technologyId, out TechnologyRecord record, out _) && GetReturned(record);

    /// <summary>등록된 기술을 처음 획득한 상태로 변경합니다.</summary>
    public bool TryAcquireTechnology(string technologyId, out RelicRestorationFailure failure)
    {
        if (!TryGetRecord(technologyId, out TechnologyRecord record, out failure)) return false;
        if (GetAcquired(record))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_TECHNOLOGY_ALREADY_ACQUIRED;
            return false;
        }
        _progressService.SetFlag(record.AcquiredFlagId, true);
        failure = RelicRestorationFailure.RESTORATION_FAILURE_NONE;
        return true;
    }

    /// <summary>획득한 기술을 한 번만 반환하고 재건 상태를 갱신합니다.</summary>
    public bool TryReturnTechnology(string technologyId, out RelicRestorationResult result,
        out RelicRestorationFailure failure)
    {
        result = default;
        if (!TryGetRecord(technologyId, out TechnologyRecord record, out failure)) return false;
        if (!GetAcquired(record))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_TECHNOLOGY_NOT_ACQUIRED;
            return false;
        }
        if (GetReturned(record))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_TECHNOLOGY_ALREADY_RETURNED;
            return false;
        }
        int previousRestore = _currentRestore;
        int previousStage = _restoreStage;
        _progressService.SetFlag(record.ReturnedFlagId, true);
        RefreshRestoreState();
        bool shouldPlayEnding = previousRestore < MaxRestore && _currentRestore == MaxRestore;
        result = new RelicRestorationResult(record.Technology, previousRestore, _currentRestore,
            previousStage, _restoreStage, shouldPlayEnding);
        failure = RelicRestorationFailure.RESTORATION_FAILURE_NONE;
        return true;
    }

    private void ResetState(IProgressService progressService)
    {
        _isInitialized = false;
        _progressService = progressService;
        _technologyById.Clear();
        _currentRestore = 0;
        _restoreStage = 0;
    }

    private bool TryGetRecord(string technologyId, out TechnologyRecord record,
        out RelicRestorationFailure failure)
    {
        record = default;
        if (!_isInitialized)
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_NOT_INITIALIZED;
            return false;
        }
        if (string.IsNullOrWhiteSpace(technologyId))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_INVALID_TECHNOLOGY_ID;
            return false;
        }
        if (!_technologyById.TryGetValue(technologyId, out record))
        {
            failure = RelicRestorationFailure.RESTORATION_FAILURE_TECHNOLOGY_NOT_FOUND;
            return false;
        }
        failure = RelicRestorationFailure.RESTORATION_FAILURE_NONE;
        return true;
    }

    private void RefreshRestoreState()
    {
        int restoreValue = 0;
        IReadOnlyList<TechnologyDataSO> technologies = _catalog.Technologies;
        for (int i = 0; i < technologies.Count; i++)
        {
            TechnologyRecord record = _technologyById[technologies[i].TechnologyId];
            if (GetReturned(record)) restoreValue += record.Technology.RestoreValue;
        }
        _currentRestore = Mathf.Clamp(restoreValue, 0, MaxRestore);
        _restoreStage = 0;
        IReadOnlyList<RelicRestorationCatalogSO.RestorationStageEntry> stages = _catalog.Stages;
        for (int i = 0; i < stages.Count && _currentRestore >= stages[i].RequiredRestore; i++)
        {
            _restoreStage = stages[i].Stage;
        }
    }

    private bool GetAcquired(TechnologyRecord record) => _progressService.GetFlag(record.AcquiredFlagId);
    private bool GetReturned(TechnologyRecord record) => _progressService.GetFlag(record.ReturnedFlagId);
}
