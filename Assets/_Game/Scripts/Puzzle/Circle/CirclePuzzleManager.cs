//=============================================================================
// File:        CirclePuzzleManager.cs
// Description: 기존 CSV 링·조작을 재사용하며 배치별 선택적 별자리 정답을 적용합니다.
// History:     07/26/2026 + CSV Definition 기반 생성 -- VicZero
//              10/07/2026 + 별자리 설정 선택 적용 -- Codex
//=============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>설정이 없는 기존 원형 퍼즐은 색상 판정을 유지합니다.</summary>
public sealed class UiCirclePuzzleManager : BasePuzzleManager
{
    [Header("UI References")]
    [Tooltip("생성한 원형 링이 배치될 UI 부모입니다.")]
    [SerializeField] private RectTransform _gridParent;
    [Tooltip("기존 색상 퍼즐의 정답 표시입니다. 별자리 설정은 링별 방향을 사용합니다.")]
    [SerializeField] private RectTransform _targetPointer;
    [Tooltip("0~3장 배경 Image입니다. 스프라이트는 프리팹에 설정합니다.")]
    [SerializeField] private Image[] _chapterBackgrounds;
    [Tooltip("기존 UiCircleComponent 회전·드래그·스냅을 제공하는 공용 링입니다.")]
    [SerializeField] private GameObject _singleRingPrefab;
    [Header("Pattern Catalog")]
    [Tooltip("기존 CSV pattern_index가 가리키는 문양 프리팹 목록입니다.")]
    [SerializeField] private List<GameObject> _ringPrefabs = new List<GameObject>();
    [Tooltip("안쪽부터 ring_order 순서로 사용할 기존 링 PNG입니다.")]
    [SerializeField] private List<Sprite> _ringSprites = new List<Sprite>();
    [Tooltip("원본 PNG 크기를 Canvas 크기로 변환할 배율입니다.")]
    [SerializeField, Min(0.01f)] private float _ringSpriteScale = 0.8f;
    [Header("Scale")]
    [Tooltip("PNG가 없을 때 가장 안쪽 링의 시작 스케일입니다.")]
    [SerializeField] private float _startScale = 1f;
    [Tooltip("PNG가 없을 때 바깥 링마다 더할 스케일입니다.")]
    [SerializeField] private float _scaleStep = 0.4f;

    // 공용 팝업에서 다른 배치를 열 때 설정을 교체하며 생성된 링과 함께 검증합니다.
    private CircleConstellationSettingsSO _constellationSettings;
    private bool _configurationValid;
    private int _chapterIndex = 1;
    private readonly List<UiCircleComponent> _spawnedRings = new List<UiCircleComponent>();

    /// <summary>요청한 배치의 설정만 적용합니다. null이면 기존 색상 판정입니다.</summary>
    public void SetConstellationSettings(CircleConstellationSettingsSO settings) => _constellationSettings = settings;

    /// <summary>기존 챕터 배경 선택을 유지합니다.</summary>
    public override void SetChapterIndex(int chapterIndex)
    {
        _chapterIndex = Mathf.Clamp(chapterIndex, 0, 3);
        for (int i = 0; _chapterBackgrounds != null && i < _chapterBackgrounds.Length; i++)
        {
            if (_chapterBackgrounds[i] != null) _chapterBackgrounds[i].enabled = i == _chapterIndex;
        }
    }

    //=============================================================================
    // Description: CSV 크기와 배치 설정으로 기존 링을 생성합니다.
    // Parameters: None. Return: void.
    //=============================================================================
    /// <summary>잘못된 설정이면 생성과 정답 판정을 중단합니다.</summary>
    public override void InitializePuzzle()
    {
        ClearSpawnedRings();
        IsClear = false;
        _configurationValid = false;
        if (_gridParent == null || _singleRingPrefab == null)
        {
            Debug.LogError("[UiCirclePuzzleManager] Grid Parent 또는 Single Ring Prefab이 연결되지 않았습니다.");
            return;
        }
        if (!TryGetPuzzleDefinition(PuzzleType.PUZZLE_TYPE_CIRCLE, out PuzzleDefinition definition)) return;
        if (_constellationSettings != null)
        {
            if (!_constellationSettings.TryValidate(definition.CircleRings, out string error))
            {
                Debug.LogError("[UiCirclePuzzleManager] 별자리 설정 오류: " + error, this);
                return;
            }
        }
        else if (!ArePatternIndicesValid(definition.CircleRings)) return;

        for (int i = 0; i < definition.CircleRings.Count; i++)
        {
            PuzzleCircleRingDefinition data = definition.CircleRings[i];
            CircleConstellationSettingsSO.Ring setting = _constellationSettings != null
                ? _constellationSettings.GetForRingOrder(data.RingOrder) : null;
            GameObject ringObject = Instantiate(_singleRingPrefab, _gridParent);
            ringObject.name = $"UI_CircleRing_{data.RingOrder}";
            Sprite sprite = data.RingOrder >= 0 && data.RingOrder < _ringSprites.Count
                ? _ringSprites[data.RingOrder] : null;
            RectTransform rect = ringObject.GetComponent<RectTransform>();
            if (rect != null && sprite != null)
            {
                rect.sizeDelta = sprite.rect.size * _ringSpriteScale;
                rect.localScale = Vector3.one;
            }
            else if (rect != null) rect.localScale = Vector3.one * (_startScale + data.RingOrder * _scaleStep);
            if (!ringObject.TryGetComponent(out UiCircleComponent ring))
            {
                Debug.LogError("[UiCirclePuzzleManager] 공용 링 프리팹에 UiCircleComponent가 없습니다.");
                if (Application.isPlaying) Destroy(ringObject);
                else DestroyImmediate(ringObject);
                ClearSpawnedRings();
                return;
            }
            ring.SetRingSprite(sprite);
            ring.InitRingState(setting != null ? setting.Segments : data.Segments,
                setting != null ? setting.InitialOffset : data.InitialOffset,
                setting != null ? setting.PatternPrefab : _ringPrefabs[data.PatternIndex], this);
            ringObject.transform.SetAsFirstSibling();
            _spawnedRings.Add(ring);
            if (setting != null && !ring.Pattern.CanAlignConstellation(setting.AnswerId,
                Direction(setting.AlignmentDegrees), setting.RotationStepDegrees))
            {
                Debug.LogError("[UiCirclePuzzleManager] 정답 문양이 지정 방향에 스냅되지 않습니다. 위치 또는 회전 간격을 수정하세요.", this);
                ClearSpawnedRings();
                return;
            }
        }
        _configurationValid = true;
    }

    //=============================================================================
    // Description: 선택된 설정의 ID·방향 또는 기존 색상 정렬을 검사합니다.
    // Parameters: None. Return: void.
    //=============================================================================
    /// <summary>별자리 정답은 바깥→안쪽 목록을 CSV 순서로 변환해 검사합니다.</summary>
    public override void CheckClearCondition()
    {
        if (IsClear || _spawnedRings.Count == 0) return;
        bool aligned;
        if (_constellationSettings != null)
        {
            aligned = _configurationValid && _spawnedRings.Count == _constellationSettings.OuterToInner.Count;
            for (int i = 0; aligned && i < _spawnedRings.Count; i++)
            {
                CircleConstellationSettingsSO.Ring setting = _constellationSettings.GetForRingOrder(i);
                CirclePattern pattern = _spawnedRings[i].Pattern;
                aligned = pattern != null && pattern.HasAlignedConstellation(setting.AnswerId, Direction(setting.AlignmentDegrees));
            }
        }
        else aligned = HasMatchingColor();
        if (!aligned) return;
        IsClear = true;
        OnPuzzleClear();
    }

    private bool HasMatchingColor()
    {
        CirclePattern reference = _spawnedRings[0].Pattern;
        if (reference == null) return false;
        Transform center = _gridParent != null ? _gridParent : _spawnedRings[0].transform.parent;
        Vector3 direction = _targetPointer != null ? _targetPointer.position - center.position : center.up;
        for (int symbol = 0; symbol < reference.Symbols.Count; symbol++)
        {
            bool aligned = true;
            for (int i = 0; i < _spawnedRings.Count; i++)
            {
                CirclePattern pattern = _spawnedRings[i].Pattern;
                if (pattern != null && pattern.HasAlignedSymbol(reference.Symbols[symbol].color, direction)) continue;
                aligned = false;
                break;
            }
            if (aligned) return true;
        }
        return false;
    }

    /// <summary>최초 클리어 이벤트를 기존 팝업에 전달합니다.</summary>
    protected override void OnPuzzleClear() => TriggerClearEvent();

    private Vector3 Direction(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return _gridParent.TransformDirection(new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0));
    }

    private bool ArePatternIndicesValid(IReadOnlyList<PuzzleCircleRingDefinition> rings)
    {
        for (int i = 0; i < rings.Count; i++)
        {
            int index = rings[i].PatternIndex;
            if (index >= 0 && index < _ringPrefabs.Count && _ringPrefabs[index] != null
                && _ringPrefabs[index].TryGetComponent(out CirclePattern pattern) && pattern.Symbols.Count > 0) continue;
            Debug.LogError($"[UiCirclePuzzleManager] 문양 프리팹 또는 CirclePattern의 문양 연결이 없습니다. pattern_index={index}");
            return false;
        }
        return true;
    }

    private void ClearSpawnedRings()
    {
        for (int i = 0; i < _spawnedRings.Count; i++)
        {
            if (_spawnedRings[i] == null) continue;
            if (Application.isPlaying) Destroy(_spawnedRings[i].gameObject);
            else DestroyImmediate(_spawnedRings[i].gameObject);
        }
        _spawnedRings.Clear();
    }
}
