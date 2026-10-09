//=============================================================================
// File: BeamPuzzleController.cs
// Description: 명시적으로 등록한 월드 Beam 부품의 진행, 초기화, 저장과 완료를 소유한다.
// History: 10/06/2026 + 팝업 의존 제거 및 전체 진행 상태 저장 -- Codex
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>광선이 꺼진 대기, 조작 진행, 최종 경로를 유지하는 완료 상태입니다.</summary>
public enum BeamPuzzleState
{
    BEAM_PUZZLE_WAITING = 0,
    BEAM_PUZZLE_IN_PROGRESS,
    BEAM_PUZZLE_COMPLETED
}

/// <summary>공통 부모나 플레이어 입력 잠금 없이 등록된 부품만 관리하는 기존 Beam 시스템입니다.</summary>
[DisallowMultipleComponent]
public sealed class BeamPuzzleController : BasePuzzleManager, ISceneStateContributor, ISceneStatePreparation
{
    private const int SAVE_VERSION = 1;
    [Header("Identity")]
    [Tooltip("같은 스테이지의 다른 저장 오브젝트와 중복되지 않는 영구 저장 ID입니다.")]
    [SerializeField] private string _objectId;
    [Header("Explicit Parts")]
    [Tooltip("퍼즐 기둥을 직접 등록합니다. 시작·끝 각각 하나가 필요하며 부모는 자유롭습니다.")]
    [SerializeField] private BeamPillarView[] _pillars = new BeamPillarView[0];
    [Tooltip("이 퍼즐을 초기화하는 진입 발판을 직접 등록합니다.")]
    [SerializeField] private BeamResetPad[] _resetPads = new BeamResetPad[0];
    [Tooltip("진행 저장·초기화·완료 잠금을 적용할 기존 밀 수 있는 돌입니다.")]
    [SerializeField] private PushableStone[] _stones = new PushableStone[0];
    [Tooltip("이 관리자가 독립 저장 대신 상태를 소유할 기존 부술 수 있는 벽입니다.")]
    [SerializeField] private BreakableWall[] _walls = new BreakableWall[0];
    [Tooltip("레이어로 광선을 차단하는 고정 장애물의 몸체 충돌체를 직접 등록합니다.")]
    [SerializeField] private Collider2D[] _fixedObstacles = new Collider2D[0];
    [Tooltip("해결 후 비활성화할 출구입니다. 관리자나 부품의 부모를 등록하지 마세요.")]
    [SerializeField] private GameObject[] _exitObstacles = new GameObject[0];
    [Header("Beam Collision")]
    [Tooltip("수신 충돌체와 모든 광선 차단 몸체의 레이어입니다. 일반 Trigger와 플레이어는 계산에서 제외합니다.")]
    [SerializeField] private LayerMask _beamCollisionMask = (1 << 8) | (1 << 9);
    [Tooltip("각 출력 광선의 최대 월드 거리입니다.")]
    [SerializeField] private float _maxBeamDistance = 20f;
    [Header("Beam View")]
    [Tooltip("현재 렌더 파이프라인에 맞는 머티리얼을 연결한 LineRenderer 프리팹입니다.")]
    [SerializeField] private LineRenderer _beamPrefab;
    [Tooltip("생성한 광선 표시의 부모입니다. 비어 있으면 관리자 아래에 생성합니다.")]
    [SerializeField] private Transform _beamParent;
    [Tooltip("모든 광선의 색상입니다.")]
    [SerializeField] private Color _beamColor = new Color(1f, 0.85f, 0.2f);
    [Tooltip("광선의 월드 굵기입니다.")]
    [SerializeField] private float _beamWidth = 0.07f;
    [Tooltip("광선 정렬 레이어 이름입니다.")]
    [SerializeField] private string _beamSortingLayer = "Object";
    [Tooltip("광선 정렬 순서입니다.")]
    [SerializeField] private int _beamSortingOrder = 5;
    [Tooltip("광선 표시의 월드 Z 위치입니다.")]
    [SerializeField] private float _beamZ;
    [Tooltip("실제로 처음 해결한 경우에만 호출하는 보상·완료 이벤트입니다.")]
    [SerializeField] private UnityEvent _onSolved = new UnityEvent();

    // 원래 씬 배치 스냅샷은 복원 후에도 재캡처하지 않습니다.
    private bool _initialized;
    private bool _beamActive;
    private bool _isGoalConnected;
    private bool[] _initialExitActive;
    private BeamPuzzleState _state;
    private BeamPropagation _propagation;
    private LineRenderer[] _beamViews;
    private BeamSegment[] _finalSegments;

    /// <summary>CSV 없이 Inspector 설정으로 동작합니다.</summary>
    public override bool RequiresGameData => false;
    /// <summary>현재 퍼즐 생명주기입니다.</summary>
    public BeamPuzzleState State => _state;
    /// <summary>씬 저장 ID입니다.</summary>
    public string ObjectId => _objectId;
    /// <summary>광선 점등 여부입니다.</summary>
    public bool BeamActive => _beamActive;

    // Awake에 의존하지 않고 Restore가 Start보다 먼저 와도 같은 경로로 준비합니다.
    private void Start() => InitializePuzzle();
    private void LateUpdate() => RefreshLivePath();

    /// <summary>최초 상태와 소유권을 준비합니다. 이미 준비한 퍼즐의 진행은 보존합니다.</summary>
    public override void InitializePuzzle() => EnsureInitialized();
    /// <inheritdoc/>
    public void PrepareSceneState() => EnsureInitialized();

    private bool EnsureInitialized()
    {
        if (_initialized) return true;
        var issues = new List<string>();
        CollectValidationIssues(issues);
        if (issues.Count > 0)
        {
            Debug.LogError("[BeamPuzzleController] 설정 오류: " + string.Join(" / ", issues), this);
            return false;
        }
        for (int i = 0; i < _pillars.Length; i++) if (!_pillars[i].TryBind(this)) return false;
        for (int i = 0; i < _resetPads.Length; i++) if (!_resetPads[i].TryBind(this)) return false;
        for (int i = 0; i < _stones.Length; i++) if (!_stones[i].TryBindBeamPuzzle(this)) return false;
        for (int i = 0; i < _walls.Length; i++) if (!_walls[i].TryBindBeamPuzzle(this)) return false;
        _initialExitActive = new bool[_exitObstacles.Length];
        for (int i = 0; i < _exitObstacles.Length; i++) _initialExitActive[i] = _exitObstacles[i].activeSelf;
        _propagation = new BeamPropagation(_pillars, _beamCollisionMask, _maxBeamDistance);
        _beamViews = new LineRenderer[_propagation.SegmentCapacity];
        for (int i = 0; i < _beamViews.Length; i++)
        {
            LineRenderer view = Instantiate(_beamPrefab, _beamParent != null ? _beamParent : transform);
            view.name = "BeamView_" + i;
            view.useWorldSpace = true;
            view.positionCount = 2;
            view.startColor = view.endColor = _beamColor;
            view.startWidth = view.endWidth = _beamWidth;
            view.sortingLayerName = _beamSortingLayer;
            view.sortingOrder = _beamSortingOrder;
            view.enabled = false;
            _beamViews[i] = view;
        }
        _initialized = true;
        return true;
    }

    /// <summary>회전 가능한 등록 기둥을 즉시 한 단계 돌리고 첫 입력에서 광선을 켭니다.</summary>
    public bool TryRotatePillar(BeamPillarView pillar)
    {
        if (!EnsureInitialized() || IsClear || pillar == null || pillar.Owner != this
            || Array.IndexOf(_pillars, pillar) < 0 || !pillar.RotateClockwise(this)) return false;
        _beamActive = true;
        _state = BeamPuzzleState.BEAM_PUZZLE_IN_PROGRESS;
        RecalculatePath(true);
        return true;
    }

    /// <summary>기존 코드 호출을 위한 배열 인덱스 회전 진입점입니다. 저장은 ID로만 대응합니다.</summary>
    public bool TryRotatePillar(int index) => _pillars != null && index >= 0 && index < _pillars.Length && TryRotatePillar(_pillars[index]);

    /// <summary>돌 이동·벽 파괴 후에도 입력 없이 미완료 경로를 갱신합니다.</summary>
    public void RefreshLivePath()
    {
        if (_initialized && _beamActive && !IsClear) RecalculatePath(true);
    }

    private void RecalculatePath(bool allowCompletion)
    {
        Physics2D.SyncTransforms();
        if (!_propagation.TryRecalculate(out _isGoalConnected)) return;
        for (int i = 0; i < _pillars.Length; i++) _pillars[i].SetConnected(_propagation.IsConnected(_pillars[i]));
        DisplaySegments(null);
        if (allowCompletion) CheckClearCondition();
    }

    /// <summary>실제 진행 중 끝 기둥 도달을 한 번만 완료로 전환합니다.</summary>
    public override void CheckClearCondition()
    {
        if (!_beamActive || IsClear || _state != BeamPuzzleState.BEAM_PUZZLE_IN_PROGRESS || !_isGoalConnected) return;
        IsClear = true;
        _state = BeamPuzzleState.BEAM_PUZZLE_COMPLETED;
        _finalSegments = new BeamSegment[_propagation.SegmentCount];
        for (int i = 0; i < _finalSegments.Length; i++) _finalSegments[i] = _propagation.GetSegment(i);
        OnPuzzleClear();
    }

    /// <summary>최종 광선을 유지하고 등록 부품을 잠근 뒤 출구를 엽니다.</summary>
    protected override void OnPuzzleClear()
    {
        SetPartsCompleted(true);
        SetExitState(true);
        _onSolved?.Invoke();
        TriggerClearEvent();
    }

    /// <summary>미완료 퍼즐의 모든 부품과 진행을 원래 씬 설정으로 복귀시키고 소등합니다.</summary>
    public bool TryResetPuzzle()
    {
        if (!EnsureInitialized() || IsClear) return false;
        RestoreInitialParts();
        return true;
    }

    private void RestoreInitialParts()
    {
        IsClear = false;
        _beamActive = _isGoalConnected = false;
        _state = BeamPuzzleState.BEAM_PUZZLE_WAITING;
        _finalSegments = null;
        SetPartsCompleted(false);
        for (int i = 0; i < _pillars.Length; i++) _pillars[i].ResetState();
        for (int i = 0; i < _stones.Length; i++) _stones[i].ResetBeamState();
        for (int i = 0; i < _walls.Length; i++) _walls[i].ResetBeamState();
        SetExitState(false);
        DisplaySegments(null);
        Physics2D.SyncTransforms();
    }

    private void SetPartsCompleted(bool completed)
    {
        for (int i = 0; i < _stones.Length; i++) _stones[i].SetPuzzleCompleted(completed);
        for (int i = 0; i < _walls.Length; i++) _walls[i].SetPuzzleCompleted(completed);
    }

    private void SetExitState(bool completed)
    {
        for (int i = 0; i < _exitObstacles.Length; i++) _exitObstacles[i].SetActive(!completed && _initialExitActive[i]);
    }

    private void DisplaySegments(BeamSegment[] frozen)
    {
        int count = !_beamActive ? 0 : frozen != null ? frozen.Length : _propagation.SegmentCount;
        for (int i = 0; i < _beamViews.Length; i++)
        {
            _beamViews[i].enabled = i < count;
            if (i >= count) continue;
            BeamSegment segment = frozen == null ? _propagation.GetSegment(i) : frozen[i];
            _beamViews[i].SetPosition(0, new Vector3(segment.Start.x, segment.Start.y, _beamZ));
            _beamViews[i].SetPosition(1, new Vector3(segment.End.x, segment.End.y, _beamZ));
        }
    }

    /// <summary>초기화 후에도 명시적인 대기 상태를 저장해 이전 진행값이 남지 않게 합니다.</summary>
    public string CaptureState()
    {
        if (!EnsureInitialized()) return string.Empty;
        RefreshLivePath();
        var data = new SaveData { Version = SAVE_VERSION, State = (int)_state, BeamActive = _beamActive,
            Pillars = new PillarState[_pillars.Length], Stones = new StoneState[_stones.Length], Walls = new WallState[_walls.Length] };
        for (int i = 0; i < _pillars.Length; i++) data.Pillars[i] = new PillarState
            { Id = _pillars[i].PillarId, Rotation = _pillars[i].RotationDegrees, Connected = _pillars[i].IsConnected };
        for (int i = 0; i < _stones.Length; i++) data.Stones[i] = new StoneState
            { Id = _stones[i].ObjectId, Position = _stones[i].transform.position, Rotation = _stones[i].transform.eulerAngles.z };
        for (int i = 0; i < _walls.Length; i++) data.Walls[i] = new WallState { Id = _walls[i].ObjectId, Broken = _walls[i].IsBroken };
        if (_finalSegments != null)
        {
            data.FinalSegments = new SegmentState[_finalSegments.Length];
            for (int i = 0; i < _finalSegments.Length; i++) data.FinalSegments[i] = new SegmentState
                { Start = _finalSegments[i].Start, End = _finalSegments[i].End };
        }
        return JsonUtility.ToJson(data);
    }

    /// <summary>저장 상태를 ID로 복원하며 재계산 중 완료·보상 이벤트를 호출하지 않습니다.</summary>
    public void ApplyState(string stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson) || !EnsureInitialized()) return;
        if (bool.TryParse(stateJson, out bool legacyCompleted))
        {
            RestoreInitialParts();
            if (!legacyCompleted) return;
            _beamActive = true;
            RecalculatePath(false);
            _finalSegments = new BeamSegment[_propagation.SegmentCount];
            for (int i = 0; i < _finalSegments.Length; i++) _finalSegments[i] = _propagation.GetSegment(i);
            ApplyCompletionWithoutEvents();
            return;
        }
        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(stateJson); }
        catch (ArgumentException) { WarnInvalidState(); return; }
        if (!ValidateSaveData(data)) { WarnInvalidState(); return; }
        RestoreInitialParts();
        for (int i = 0; i < _pillars.Length; i++)
            for (int j = 0; j < data.Pillars.Length; j++)
                if (_pillars[i].PillarId == data.Pillars[j].Id)
                { _pillars[i].SetRotation(data.Pillars[j].Rotation); _pillars[i].SetConnected(data.Pillars[j].Connected); break; }
        for (int i = 0; i < _stones.Length; i++)
            for (int j = 0; j < data.Stones.Length; j++)
                if (_stones[i].ObjectId == data.Stones[j].Id)
                { _stones[i].RestoreWorldPose(data.Stones[j].Position, data.Stones[j].Rotation); break; }
        for (int i = 0; i < _walls.Length; i++)
            for (int j = 0; j < data.Walls.Length; j++)
                if (_walls[i].ObjectId == data.Walls[j].Id) { _walls[i].RestoreBeamState(data.Walls[j].Broken); break; }
        _beamActive = data.BeamActive;
        _state = (BeamPuzzleState)data.State;
        if (_state == BeamPuzzleState.BEAM_PUZZLE_COMPLETED)
        {
            _finalSegments = new BeamSegment[data.FinalSegments.Length];
            for (int i = 0; i < _finalSegments.Length; i++) _finalSegments[i] = new BeamSegment(data.FinalSegments[i].Start, data.FinalSegments[i].End);
            ApplyCompletionWithoutEvents();
        }
        else if (_beamActive) RecalculatePath(false);
    }

    private void ApplyCompletionWithoutEvents()
    {
        IsClear = true;
        _state = BeamPuzzleState.BEAM_PUZZLE_COMPLETED;
        SetPartsCompleted(true);
        SetExitState(true);
        DisplaySegments(_finalSegments);
    }

    private void WarnInvalidState() => Debug.LogWarning("[BeamPuzzleController] 저장 상태를 읽을 수 없습니다. id=" + _objectId, this);

    private bool ValidateSaveData(SaveData data)
    {
        if (data == null || data.Version != SAVE_VERSION || data.State < 0 || data.State > 2
            || data.BeamActive != (data.State != 0) || data.Pillars == null || data.Stones == null || data.Walls == null
            || (data.State == 2 && (data.FinalSegments == null || data.FinalSegments.Length > _beamViews.Length))) return false;
        var ids = new HashSet<string>();
        for (int i = 0; i < data.Pillars.Length; i++)
            if (data.Pillars[i] == null || string.IsNullOrWhiteSpace(data.Pillars[i].Id) || !ids.Add(data.Pillars[i].Id)
                || !BeamPillarSettings.IsFinite(data.Pillars[i].Rotation)) return false;
        ids.Clear();
        for (int i = 0; i < data.Stones.Length; i++)
            if (data.Stones[i] == null || string.IsNullOrWhiteSpace(data.Stones[i].Id) || !ids.Add(data.Stones[i].Id)
                || !Finite(data.Stones[i].Position) || !BeamPillarSettings.IsFinite(data.Stones[i].Rotation)) return false;
        ids.Clear();
        for (int i = 0; i < data.Walls.Length; i++)
            if (data.Walls[i] == null || string.IsNullOrWhiteSpace(data.Walls[i].Id) || !ids.Add(data.Walls[i].Id)) return false;
        if (data.FinalSegments != null) for (int i = 0; i < data.FinalSegments.Length; i++)
            if (data.FinalSegments[i] == null || !Finite(data.FinalSegments[i].Start) || !Finite(data.FinalSegments[i].End)) return false;
        return true;
    }

    private static bool Finite(Vector2 value) => BeamPillarSettings.IsFinite(value.x) && BeamPillarSettings.IsFinite(value.y);

    /// <summary>Inspector와 편집 도구에서 설정 문제를 모아 확인합니다. 부품은 직접 등록합니다.</summary>
    public void CollectValidationIssues(List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(_objectId)) issues.Add("저장 ID가 비어 있습니다.");
        if (!BeamPillarSettings.IsFinite(_maxBeamDistance) || _maxBeamDistance <= 0f
            || !BeamPillarSettings.IsFinite(_beamWidth) || _beamWidth <= 0f || !BeamPillarSettings.IsFinite(_beamZ)) issues.Add("광선 거리·굵기·Z 값이 올바르지 않습니다.");
        if (_beamCollisionMask.value == 0) issues.Add("광선 LayerMask가 비어 있습니다.");
        if (_beamPrefab == null || _beamPrefab.sharedMaterial == null) issues.Add("LineRenderer와 Material이 필요합니다.");
        if (_pillars == null || _pillars.Length == 0) issues.Add("기둥을 직접 등록하세요.");
        if (_resetPads == null || _resetPads.Length == 0) issues.Add("초기화 발판을 등록하세요.");
        var parts = new HashSet<UnityEngine.Object>();
        var ids = new HashSet<string>();
        int starts = 0, ends = 0;
        if (_pillars != null) for (int i = 0; i < _pillars.Length; i++)
        {
            BeamPillarView pillar = _pillars[i];
            if (!CheckPart(pillar, parts, issues)) continue;
            if (!pillar.TryValidate(out string error)) issues.Add(pillar.name + ": " + error);
            if (!ids.Add(pillar.PillarId)) issues.Add("중복 기둥 ID: " + pillar.PillarId);
            CheckLayer(pillar.PillarCollider, issues);
            CheckLayer(pillar.BodyCollider, issues);
            if (pillar.Owner != null && pillar.Owner != this) issues.Add("다른 관리자와 공유한 기둥: " + pillar.name);
            starts += pillar.PillarType == BeamPillarType.BEAM_PILLAR_START ? 1 : 0;
            ends += pillar.PillarType == BeamPillarType.BEAM_PILLAR_END ? 1 : 0;
        }
        if (starts != 1 || ends != 1) issues.Add("시작·끝 기둥은 각각 하나여야 합니다.");
        if (_resetPads != null) for (int i = 0; i < _resetPads.Length; i++)
        {
            BeamResetPad pad = _resetPads[i];
            if (!CheckPart(pad, parts, issues)) continue;
            if (pad.Trigger == null || !pad.Trigger.isTrigger || !pad.Trigger.enabled
                || pad.Trigger.GetComponent<BeamResetPad>() != pad) issues.Add("발판 Trigger 참조가 올바르지 않습니다: " + pad.name);
            if (pad.Owner != null && pad.Owner != this) issues.Add("공유 발판: " + pad.name);
        }
        ids.Clear();
        if (_stones != null) for (int i = 0; i < _stones.Length; i++)
        {
            PushableStone stone = _stones[i];
            if (!CheckPart(stone, parts, issues)) continue;
            CheckId(stone.ObjectId, ids, issues);
            CheckBlocker(stone.BodyCollider, issues);
            if (stone.BeamOwner != null && stone.BeamOwner != this) issues.Add("공유 돌: " + stone.name);
        }
        if (_walls != null) for (int i = 0; i < _walls.Length; i++)
        {
            BreakableWall wall = _walls[i];
            if (!CheckPart(wall, parts, issues)) continue;
            CheckId(wall.ObjectId, ids, issues);
            CheckBlocker(wall.BodyCollider, issues);
            if (wall.BeamOwner != null && wall.BeamOwner != this) issues.Add("공유 벽: " + wall.name);
        }
        if (_stones == null || _walls == null || _fixedObstacles == null || _exitObstacles == null) issues.Add("부품 배열이 null입니다.");
        if (_fixedObstacles != null) for (int i = 0; i < _fixedObstacles.Length; i++)
            if (CheckPart(_fixedObstacles[i], parts, issues)) CheckBlocker(_fixedObstacles[i], issues);
        if (_exitObstacles != null) for (int i = 0; i < _exitObstacles.Length; i++)
        {
            GameObject exit = _exitObstacles[i];
            if (!CheckPart(exit, parts, issues)) continue;
            if (Contains(exit.transform, transform) || (_beamParent != null && Contains(exit.transform, _beamParent))) issues.Add("출구가 관리자 또는 광선 표시의 부모입니다.");
            if (_beamPrefab != null && _beamPrefab.gameObject.scene.IsValid() && Contains(exit.transform, _beamPrefab.transform)) issues.Add("출구가 광선 템플릿의 부모입니다.");
            if (_pillars != null) for (int j = 0; j < _pillars.Length; j++) if (_pillars[j] != null && Contains(exit.transform, _pillars[j].transform)) issues.Add("출구가 기둥의 부모입니다.");
            if (_resetPads != null) for (int j = 0; j < _resetPads.Length; j++) if (_resetPads[j] != null && Contains(exit.transform, _resetPads[j].transform)) issues.Add("출구가 발판의 부모입니다.");
            if (_stones != null) for (int j = 0; j < _stones.Length; j++) if (_stones[j] != null && Contains(exit.transform, _stones[j].transform)) issues.Add("출구가 돌의 부모입니다.");
            if (_walls != null) for (int j = 0; j < _walls.Length; j++) if (_walls[j] != null && Contains(exit.transform, _walls[j].transform)) issues.Add("출구가 벽의 부모입니다.");
            Collider2D[] colliders = exit.GetComponentsInChildren<Collider2D>(true);
            if (colliders.Length == 0) issues.Add("출구에 몸체 충돌체가 없습니다.");
            for (int j = 0; j < colliders.Length; j++) if (!colliders[j].isTrigger) CheckBlocker(colliders[j], issues);
        }
        // 검증할 때만 관리자를 비교합니다. 소속은 항상 명시 참조로 결정합니다.
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == this || behaviour.gameObject.scene != gameObject.scene) continue;
            if (behaviour is ISceneStateContributor contributor && !string.IsNullOrWhiteSpace(_objectId)
                && contributor.ObjectId == _objectId) issues.Add("중복 저장 ID: " + _objectId);
            if (behaviour is BeamPuzzleController other && SharesPart(other, parts))
                issues.Add("여러 관리자에 공유 등록한 부품: " + other.name);
        }
    }

    private static bool Contains(Transform parent, Transform child) => child == parent || child.IsChildOf(parent);
    private static bool CheckPart(UnityEngine.Object part, HashSet<UnityEngine.Object> parts, List<string> issues)
    {
        if (part == null) { issues.Add("누락된 부품 참조가 있습니다."); return false; }
        if (!parts.Add(PartKey(part))) { issues.Add("중복 등록: " + part.name); return false; }
        return true;
    }
    private static UnityEngine.Object PartKey(UnityEngine.Object part) => part is Component component ? component.gameObject : part;
    private static void CheckId(string id, HashSet<string> ids, List<string> issues)
    { if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) issues.Add("돌/벽 ID가 비었거나 중복됩니다: " + id); }
    private void CheckLayer(Collider2D collider, List<string> issues)
    { if (collider == null || (_beamCollisionMask.value & (1 << collider.gameObject.layer)) == 0) issues.Add("몸체/수신 충돌체가 광선 LayerMask에 없습니다."); }
    private void CheckBlocker(Collider2D collider, List<string> issues)
    {
        CheckLayer(collider, issues);
        if (collider != null && collider.isTrigger) issues.Add("장애물 몸체는 비 Trigger여야 합니다.");
    }
    private bool SharesPart(BeamPuzzleController other, HashSet<UnityEngine.Object> parts)
    {
        if (other._pillars != null) for (int i = 0; i < other._pillars.Length; i++) if (parts.Contains(PartKey(other._pillars[i]))) return true;
        if (other._resetPads != null) for (int i = 0; i < other._resetPads.Length; i++) if (parts.Contains(PartKey(other._resetPads[i]))) return true;
        if (other._stones != null) for (int i = 0; i < other._stones.Length; i++) if (parts.Contains(PartKey(other._stones[i]))) return true;
        if (other._walls != null) for (int i = 0; i < other._walls.Length; i++) if (parts.Contains(PartKey(other._walls[i]))) return true;
        if (other._fixedObstacles != null) for (int i = 0; i < other._fixedObstacles.Length; i++) if (parts.Contains(PartKey(other._fixedObstacles[i]))) return true;
        if (other._exitObstacles != null) for (int i = 0; i < other._exitObstacles.Length; i++) if (parts.Contains(PartKey(other._exitObstacles[i]))) return true;
        return false;
    }

    // Unity JSON용 DTO입니다. 배열 순서 대신 ID를 복원 키로 사용합니다.
    [Serializable] private sealed class SaveData
    {
        // 저장 형식 버전입니다.
        public int Version;
        // 대기/진행/완료 상태입니다.
        public int State;
        // 광선 활성 여부입니다.
        public bool BeamActive;
        // ID별 기둥 회전 및 표시입니다.
        public PillarState[] Pillars;
        // ID별 돌 월드 위치 및 회전입니다.
        public StoneState[] Stones;
        // ID별 벽 파괴 상태입니다.
        public WallState[] Walls;
        // 출구가 열린 후에도 유지하는 최종 경로입니다.
        public SegmentState[] FinalSegments;
    }
    // 한 기둥의 영구 대응 데이터입니다.
    [Serializable] private sealed class PillarState
    {
        public string Id; // 퍼즐 내부 기둥 ID입니다.
        public float Rotation; // 월드 출력 회전각입니다.
        public bool Connected; // 완료 시 유지할 연결 표시입니다.
    }
    // 한 돌의 영구 대응 데이터입니다.
    [Serializable] private sealed class StoneState
    {
        public string Id; // 돌 ID입니다.
        public Vector2 Position; // 월드 XY 위치입니다.
        public float Rotation; // 월드 Z 회전각입니다.
    }
    // 한 벽의 영구 대응 데이터입니다.
    [Serializable] private sealed class WallState
    {
        public string Id; // 벽 ID입니다.
        public bool Broken; // 파괴 여부입니다.
    }
    // 한 최종 광선의 표시 데이터입니다.
    [Serializable] private sealed class SegmentState
    {
        public Vector2 Start; // 월드 시작점입니다.
        public Vector2 End; // 월드 종료점입니다.
    }
}
