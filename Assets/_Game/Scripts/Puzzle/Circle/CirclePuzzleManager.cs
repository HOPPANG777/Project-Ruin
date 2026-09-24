using System.Collections.Generic;
using UnityEngine;

//=============================================================================
// File:        CirclePuzzleManager.cs
//
// Description: CSV 원형 퍼즐 정의로 중첩 링을 생성하고 정렬 상태를 판정한다.
//
// History:     07/26/2026 + CSV Definition 기반 생성으로 전환 -- VicZero
//
//=============================================================================
public sealed class UiCirclePuzzleManager : BasePuzzleManager
{
    [Header("UI References")]
    [Tooltip("생성한 원형 링이 배치될 UI 부모입니다.")]
    [SerializeField]
    private RectTransform _gridParent;

    [Tooltip("UiCircleComponent가 포함된 공용 원형 링 프리팹입니다.")]
    [SerializeField]
    private GameObject _singleRingPrefab;

    [Header("Pattern Catalog")]
    [Tooltip("puzzle_circle_rings.csv의 pattern_index가 가리키는 문양 프리팹 목록입니다.")]
    [SerializeField]
    private List<GameObject> _ringPrefabs = new List<GameObject>();

    [Header("Ring Sprite Catalog")]
    [Tooltip("안쪽부터 ring_order 순서로 사용할 링 PNG 목록입니다.")]
    [SerializeField]
    private List<Sprite> _ringSprites = new List<Sprite>();

    [Tooltip("원본 PNG 크기를 Canvas 크기로 변환할 배율입니다.")]
    [SerializeField]
    [Min(0.01f)]
    private float _ringSpriteScale = 0.8f;

    [Header("Scale")]
    [Tooltip("가장 안쪽 링에 적용할 시작 스케일입니다.")]
    [SerializeField]
    private float _startScale = 1f;

    [Tooltip("바깥쪽 링으로 이동할 때마다 더할 스케일입니다.")]
    [SerializeField]
    private float _scaleStep = 0.4f;

    private readonly List<UiCircleComponent> _spawnedRings = new List<UiCircleComponent>();

    /// <summary>CSV에서 현재 원형 퍼즐을 조회하고 중첩 링을 다시 생성합니다.</summary>
    public override void InitializePuzzle()
    {
        ClearSpawnedRings();
        IsClear = false;

        if (_gridParent == null || _singleRingPrefab == null)
        {
            Debug.LogError("[UiCirclePuzzleManager] Grid Parent 또는 Single Ring Prefab이 연결되지 않았습니다.");
            return;
        }

        if (!TryGetPuzzleDefinition(
            PuzzleType.PUZZLE_TYPE_CIRCLE,
            out PuzzleDefinition puzzleDefinition))
        {
            return;
        }

        if (!ArePatternIndicesValid(puzzleDefinition.CircleRings))
        {
            return;
        }

        for (int i = 0; i < puzzleDefinition.CircleRings.Count; i++)
        {
            PuzzleCircleRingDefinition ringDefinition = puzzleDefinition.CircleRings[i];
            GameObject ringObject = Instantiate(_singleRingPrefab, _gridParent);
            ringObject.name = $"UI_CircleRing_{ringDefinition.RingOrder}";

            RectTransform ringRect = ringObject.GetComponent<RectTransform>();
            Sprite ringSprite = ringDefinition.RingOrder >= 0
                && ringDefinition.RingOrder < _ringSprites.Count
                ? _ringSprites[ringDefinition.RingOrder]
                : null;

            if (ringRect != null)
            {
                if (ringSprite != null)
                {
                    ringRect.sizeDelta = ringSprite.rect.size * _ringSpriteScale;
                    ringRect.localScale = Vector3.one;
                }
                else
                {
                    float scale = _startScale + ringDefinition.RingOrder * _scaleStep;
                    ringRect.localScale = new Vector3(scale, scale, 1f);
                }
            }

            UiCircleComponent ringComponent = ringObject.GetComponent<UiCircleComponent>();

            if (ringComponent == null)
            {
                Debug.LogError("[UiCirclePuzzleManager] 공용 링 프리팹에 UiCircleComponent가 없습니다.");
                Destroy(ringObject);
                ClearSpawnedRings();
                return;
            }

            GameObject patternPrefab = _ringPrefabs[ringDefinition.PatternIndex];
            ringComponent.SetRingSprite(ringSprite);
            ringComponent.InitRingState(
                ringDefinition.Segments,
                ringDefinition.InitialOffset,
                patternPrefab,
                this);
            ringObject.transform.SetAsFirstSibling();
            _spawnedRings.Add(ringComponent);
        }

        Debug.Log(
            $"[UiCirclePuzzleManager] CSV 퍼즐 배치 완료. id={PuzzleId}, rings={_spawnedRings.Count}");
    }

    /// <summary>생성된 모든 링이 정답 각도에 정렬됐는지 검사합니다.</summary>
    public override void CheckClearCondition()
    {
        if (_spawnedRings.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _spawnedRings.Count; i++)
        {
            if (!_spawnedRings[i].IsAligned())
            {
                return;
            }
        }

        IsClear = true;
        OnPuzzleClear();
    }

    /// <summary>원형 퍼즐 클리어 이벤트를 외부에 전달합니다.</summary>
    protected override void OnPuzzleClear()
    {
        Debug.Log($"[UiCirclePuzzleManager] 원형 퍼즐을 클리어했습니다. id={PuzzleId}");
        TriggerClearEvent();
    }

    private bool ArePatternIndicesValid(IReadOnlyList<PuzzleCircleRingDefinition> rings)
    {
        for (int i = 0; i < rings.Count; i++)
        {
            int patternIndex = rings[i].PatternIndex;

            if (patternIndex >= 0
                && patternIndex < _ringPrefabs.Count
                && _ringPrefabs[patternIndex] != null)
            {
                continue;
            }

            Debug.LogError(
                $"[UiCirclePuzzleManager] CSV pattern_index에 대응하는 문양 프리팹이 없습니다. "
                + $"id={PuzzleId}, pattern_index={patternIndex}");
            return false;
        }

        return true;
    }

    private void ClearSpawnedRings()
    {
        for (int i = 0; i < _spawnedRings.Count; i++)
        {
            if (_spawnedRings[i] != null)
            {
                Destroy(_spawnedRings[i].gameObject);
            }
        }

        _spawnedRings.Clear();
    }
}
