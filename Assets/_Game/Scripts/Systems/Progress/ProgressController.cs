//=============================================================================
// File:        ProgressController.cs
//
// Description: 게임 진행 상태를 메모리에 보관하고 ES3 파일로 저장·불러온다.
//
// History:     07/22/2026 + ES3 persistence added -- Codex
//
//=============================================================================

//========================================
// System Includes
//========================================
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

//=============================================================================
//
// Synopsis:   ProgressController;
//                - 플래그와 씬 오브젝트 상태의 단일 런타임 저장소
//                - Dirty 상태만 ES3 파일에 비동기 저장
//
//=============================================================================
[DisallowMultipleComponent]
public sealed class ProgressController : MonoBehaviour, IProgressService, IProgressStateTransactionService
{
    //========================================
    // 상수
    //========================================

    private const string DEFAULT_SAVE_FILE_PATH = "ProjectRuinSave.es3";
    private const string SAVE_KEY_PROGRESS_DATA = "progress.data";

    //========================================
    // 내부 타입
    //========================================

    /// <summary>스테이지 번호와 오브젝트 ID를 묶은 메모리 조회 키입니다.</summary>
    private readonly struct ObjectStateKey : IEquatable<ObjectStateKey>
    {
        /// <summary>키를 생성합니다.</summary>
        public ObjectStateKey(int stageNumber, string objectId)
        {
            StageNumber = stageNumber;
            ObjectId = objectId;
        }

        /// <summary>스테이지 번호입니다.</summary>
        public int StageNumber { get; }

        /// <summary>씬 오브젝트 ID입니다.</summary>
        public string ObjectId { get; }

        /// <summary>두 키가 같은 스테이지와 오브젝트를 가리키는지 비교합니다.</summary>
        public bool Equals(ObjectStateKey other) => StageNumber == other.StageNumber
            && string.Equals(ObjectId, other.ObjectId, StringComparison.Ordinal);

        /// <summary>박싱 없이 Dictionary 키 비교를 지원합니다.</summary>
        public override bool Equals(object obj) => obj is ObjectStateKey other && Equals(other);

        /// <summary>스테이지와 오브젝트 ID로 해시 값을 생성합니다.</summary>
        public override int GetHashCode() =>
            unchecked((StageNumber * 397) ^ StringComparer.Ordinal.GetHashCode(ObjectId));
    }

    /// <summary>ES3에 기록할 씬 오브젝트 상태 한 건입니다.</summary>
    [Serializable]
    private sealed class ObjectStateSaveEntry
    {
        /// <summary>스테이지 번호입니다.</summary>
        public int StageNumber;

        /// <summary>씬 오브젝트 ID입니다.</summary>
        public string ObjectId;

        /// <summary>직렬화된 오브젝트 상태입니다.</summary>
        public string StateJson;
    }

    /// <summary>한 번의 ES3 쓰기로 저장할 전체 진행 상태입니다.</summary>
    [Serializable]
    private sealed class ProgressSaveData
    {
        /// <summary>스토리와 재건 상태를 포함한 전역 플래그입니다.</summary>
        public Dictionary<string, bool> GlobalFlags = new Dictionary<string, bool>();

        /// <summary>스테이지별 씬 오브젝트 상태입니다.</summary>
        public List<ObjectStateSaveEntry> ObjectStates = new List<ObjectStateSaveEntry>();

        /// <summary>인벤토리와 상점 등 전역 시스템의 직렬화 상태입니다.</summary>
        public Dictionary<string, string> SystemStates = new Dictionary<string, string>();
    }

    //========================================
    // 직렬화 필드
    //========================================

    [Header("Save")]
    [Tooltip("PersistentDataPath 아래에 생성할 ES3 세이브 파일 이름입니다.")]
    [SerializeField]
    private string _saveFilePath = DEFAULT_SAVE_FILE_PATH;

    //========================================
    // 내부 상태
    //========================================

    private readonly Dictionary<string, bool> _globalFlags =
        new Dictionary<string, bool>(StringComparer.Ordinal);

    private readonly Dictionary<ObjectStateKey, string> _objectStates =
        new Dictionary<ObjectStateKey, string>();

    private readonly Dictionary<string, string> _systemStates =
        new Dictionary<string, string>(StringComparer.Ordinal);

    // 저장 중 값이 바뀌어도 최신 Dirty 상태를 유지하기 위한 변경 번호입니다.
    private int _changeVersion;

    // 변경 없는 디스크 쓰기를 건너뛰기 위한 상태입니다.
    private bool _isDirty;

    // 동시에 두 파일 작업이 실행되는 것을 차단합니다.
    private bool _isIoRunning;

    //========================================
    // 프로퍼티
    //========================================

    /// <summary>메모리 상태가 마지막 디스크 저장 이후 변경되었는지 여부입니다.</summary>
    public bool IsDirty => _isDirty;

    /// <summary>타이틀에서 불러올 수 있는 기존 Progress 저장 Key가 있는지 확인합니다.</summary>
    public bool HasSavedData
    {
        get
        {
            try
            {
                ES3.Init();
                return ES3.KeyExists(SAVE_KEY_PROGRESS_DATA, CreateSaveSettings());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ProgressController] 저장 기록 확인에 실패했습니다. {exception.Message}");
                return false;
            }
        }
    }

    /// <summary>실제 저장의 Dirty 검사 전에 현재 씬 상태를 메모리에 수집하는 이벤트입니다.</summary>
    public event Action BeforeSaveRequested;
    /// <summary>실제 불러오기를 마친 뒤 현재 씬 상태를 보상 없이 복원하는 이벤트입니다.</summary>
    public event Action StateLoaded;

    //******************************************************************************
    //
    // Public Member Functions
    //
    //******************************************************************************

    /// <summary>ES3 파일에서 진행 상태를 비동기로 불러옵니다.</summary>
    /// <param name="cancellationToken">오브젝트 생명주기와 연결된 취소 토큰입니다.</param>
    /// <returns>불러오기에 성공하면 true입니다.</returns>
    public async UniTask<bool> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_isIoRunning)
        {
            return false;
        }

        _isIoRunning = true;

        try
        {
            // ES3의 Unity 경로와 타입 정보를 메인 스레드에서 먼저 준비합니다.
            ES3.Init();
            ES3Settings settings = CreateSaveSettings();
            ProgressSaveData saveData = await UniTask.RunOnThreadPool(
                () => LoadSaveData(settings),
                true,
                cancellationToken);

            ApplySaveData(saveData);
            _changeVersion = 0;
            _isDirty = false;
            StateLoaded?.Invoke();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ProgressController] 불러오기에 실패했습니다. {exception.Message}");
            return false;
        }
        finally
        {
            _isIoRunning = false;
        }
    }

    /// <summary>변경된 진행 상태를 ES3 파일에 비동기로 저장합니다.</summary>
    /// <param name="cancellationToken">오브젝트 생명주기와 연결된 취소 토큰입니다.</param>
    /// <returns>저장 또는 Dirty가 없는 저장 건너뛰기에 성공하면 true입니다.</returns>
    public async UniTask<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_isIoRunning)
        {
            return false;
        }

        _isIoRunning = true;

        try
        {
            // 수집 중 완료 보상에서 다시 Save를 호출해도 파일 작업이 중첩되지 않습니다.
            BeforeSaveRequested?.Invoke();
            if (!_isDirty) return true;
            ES3.Init();
            ES3Settings settings = CreateSaveSettings();

            // 저장 도중 새 변경이 생기면 최신 스냅샷까지 이어서 저장합니다.
            while (_isDirty)
            {
                int savedVersion = _changeVersion;
                ProgressSaveData saveData = CreateSaveData();

                await UniTask.RunOnThreadPool(
                    () => ES3.Save(SAVE_KEY_PROGRESS_DATA, saveData, settings),
                    true,
                    cancellationToken);

                if (savedVersion == _changeVersion)
                {
                    _isDirty = false;
                }
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ProgressController] 저장에 실패했습니다. {exception.Message}");
            return false;
        }
        finally
        {
            _isIoRunning = false;
        }
    }

    /// <summary>전역 플래그 값을 메모리에 저장합니다.</summary>
    /// <param name="flagId">플래그 고유 ID입니다.</param>
    /// <param name="value">저장할 값입니다.</param>
    public void SetFlag(string flagId, bool value)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return;
        }

        if (_globalFlags.TryGetValue(flagId, out bool currentValue) && currentValue == value)
        {
            return;
        }

        _globalFlags[flagId] = value;
        MarkDirty();
    }

    /// <summary>전역 플래그 값을 조회합니다.</summary>
    /// <param name="flagId">플래그 고유 ID입니다.</param>
    /// <returns>저장된 값이 있으면 해당 값, 없으면 false입니다.</returns>
    public bool GetFlag(string flagId)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return false;
        }

        return _globalFlags.TryGetValue(flagId, out bool value) && value;
    }

    /// <summary>전역 시스템의 직렬화 상태를 메모리에 저장합니다.</summary>
    public void SetSystemState(string stateId, string stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateId) || string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }

        if (_systemStates.TryGetValue(stateId, out string currentState)
            && string.Equals(currentState, stateJson, StringComparison.Ordinal))
        {
            return;
        }

        _systemStates[stateId] = stateJson;
        MarkDirty();
    }

    //=============================================================================
    // Description: 외부 콜백 없이 두 상태를 함께 적용하여 부분 저장을 방지합니다.
    // Parameters: firstId/firstJson, secondId/secondJson — 서로 다른 시스템 상태.
    // Return: 두 상태를 적용했으면 true, 입력/적용 실패 시 false.
    //=============================================================================
    /// <summary>인벤토리와 연동 도메인의 상태를 원자적으로 메모리에 적용합니다.</summary>
    public bool TrySetSystemStates(string firstId, string firstJson, string secondId, string secondJson)
    {
        if (string.IsNullOrWhiteSpace(firstId) || string.IsNullOrWhiteSpace(firstJson)
            || string.IsNullOrWhiteSpace(secondId) || string.IsNullOrWhiteSpace(secondJson)
            || firstId == secondId) return false;
        bool hadFirst = _systemStates.TryGetValue(firstId, out string previousFirst);
        bool hadSecond = _systemStates.TryGetValue(secondId, out string previousSecond);
        try
        {
            _systemStates[firstId] = firstJson;
            _systemStates[secondId] = secondJson;
        }
        catch (Exception)
        {
            if (hadFirst) _systemStates[firstId] = previousFirst;
            else _systemStates.Remove(firstId);
            if (hadSecond) _systemStates[secondId] = previousSecond;
            else _systemStates.Remove(secondId);
            return false;
        }
        if (previousFirst != firstJson || previousSecond != secondJson) MarkDirty();
        return true;
    }

    /// <summary>전역 시스템의 직렬화 상태를 메모리에서 조회합니다.</summary>
    public bool TryGetSystemState(string stateId, out string stateJson)
    {
        stateJson = null;
        return !string.IsNullOrWhiteSpace(stateId)
            && _systemStates.TryGetValue(stateId, out stateJson);
    }

    /// <summary>특정 스테이지의 오브젝트 상태를 메모리에 저장합니다.</summary>
    /// <param name="stageNumber">스테이지 번호입니다.</param>
    /// <param name="objectId">씬 오브젝트 고유 ID입니다.</param>
    /// <param name="stateJson">직렬화된 상태 문자열입니다.</param>
    public void SetObjectState(int stageNumber, string objectId, string stateJson)
    {
        if (stageNumber < 0
            || string.IsNullOrWhiteSpace(objectId)
            || string.IsNullOrWhiteSpace(stateJson))
        {
            return;
        }

        ObjectStateKey key = new ObjectStateKey(stageNumber, objectId);

        if (_objectStates.TryGetValue(key, out string currentState) && currentState == stateJson)
        {
            return;
        }

        _objectStates[key] = stateJson;
        MarkDirty();
    }

    /// <summary>특정 스테이지의 오브젝트 상태를 조회합니다.</summary>
    /// <param name="stageNumber">스테이지 번호입니다.</param>
    /// <param name="objectId">씬 오브젝트 고유 ID입니다.</param>
    /// <param name="stateJson">조회된 상태 문자열입니다.</param>
    /// <returns>저장된 상태가 있으면 true입니다.</returns>
    public bool TryGetObjectState(int stageNumber, string objectId, out string stateJson)
    {
        stateJson = null;

        if (stageNumber < 0 || string.IsNullOrWhiteSpace(objectId))
        {
            return false;
        }

        ObjectStateKey key = new ObjectStateKey(stageNumber, objectId);
        return _objectStates.TryGetValue(key, out stateJson);
    }

    /// <summary>새 게임을 위해 모든 진행 상태를 비웁니다.</summary>
    public void Reset()
    {
        _globalFlags.Clear();
        _objectStates.Clear();
        _systemStates.Clear();
        MarkDirty();
    }

    //******************************************************************************
    //
    // Private Member Functions
    //
    //******************************************************************************

    private ES3Settings CreateSaveSettings()
    {
        string filePath = string.IsNullOrWhiteSpace(_saveFilePath)
            ? DEFAULT_SAVE_FILE_PATH
            : _saveFilePath;
        return new ES3Settings(filePath);
    }

    private ProgressSaveData CreateSaveData()
    {
        ProgressSaveData saveData = new ProgressSaveData();

        foreach (KeyValuePair<string, bool> flag in _globalFlags)
        {
            saveData.GlobalFlags.Add(flag.Key, flag.Value);
        }

        foreach (KeyValuePair<ObjectStateKey, string> state in _objectStates)
        {
            saveData.ObjectStates.Add(new ObjectStateSaveEntry
            {
                StageNumber = state.Key.StageNumber,
                ObjectId = state.Key.ObjectId,
                StateJson = state.Value
            });
        }

        foreach (KeyValuePair<string, string> state in _systemStates)
        {
            saveData.SystemStates.Add(state.Key, state.Value);
        }

        return saveData;
    }

    private void ApplySaveData(ProgressSaveData saveData)
    {
        _globalFlags.Clear();
        _objectStates.Clear();
        _systemStates.Clear();

        if (saveData.GlobalFlags != null)
        {
            foreach (KeyValuePair<string, bool> flag in saveData.GlobalFlags)
            {
                if (!string.IsNullOrWhiteSpace(flag.Key))
                {
                    _globalFlags[flag.Key] = flag.Value;
                }
            }
        }

        if (saveData.ObjectStates == null)
        {
            saveData.ObjectStates = new List<ObjectStateSaveEntry>();
        }

        for (int i = 0; i < saveData.ObjectStates.Count; i++)
        {
            ObjectStateSaveEntry entry = saveData.ObjectStates[i];

            if (entry == null
                || entry.StageNumber < 0
                || string.IsNullOrWhiteSpace(entry.ObjectId)
                || string.IsNullOrWhiteSpace(entry.StateJson))
            {
                continue;
            }

            ObjectStateKey key = new ObjectStateKey(entry.StageNumber, entry.ObjectId);
            _objectStates[key] = entry.StateJson;
        }

        if (saveData.SystemStates == null)
        {
            return;
        }

        foreach (KeyValuePair<string, string> state in saveData.SystemStates)
        {
            if (!string.IsNullOrWhiteSpace(state.Key) && !string.IsNullOrWhiteSpace(state.Value))
            {
                _systemStates[state.Key] = state.Value;
            }
        }
    }

    private void MarkDirty()
    {
        _changeVersion++;
        _isDirty = true;
    }

    private static ProgressSaveData LoadSaveData(ES3Settings settings)
    {
        if (!ES3.KeyExists(SAVE_KEY_PROGRESS_DATA, settings))
        {
            return new ProgressSaveData();
        }

        return ES3.Load<ProgressSaveData>(SAVE_KEY_PROGRESS_DATA, settings)
            ?? new ProgressSaveData();
    }
}
