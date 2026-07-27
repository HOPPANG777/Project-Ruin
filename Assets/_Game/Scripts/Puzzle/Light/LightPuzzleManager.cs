using System.Collections.Generic;
using UnityEngine;

//=============================================================================
// File:        LightPuzzleManager.cs
//
// Description: CSV 라이트 퍼즐 정의로 UI 타일을 생성하고 클릭 연쇄와 정답을 판정한다.
//
// History:     07/26/2026 + CSV Definition 기반 생성으로 전환 -- VicZero
//
//=============================================================================
public sealed class UiLightPuzzleManager : BasePuzzleManager
{
    [Header("UI References")]
    [Tooltip("생성한 라이트 타일이 배치될 UI 부모입니다.")]
    [SerializeField]
    private RectTransform _gridParent;

    [Tooltip("Image와 UiLightTileComponent가 포함된 라이트 타일 프리팹입니다.")]
    [SerializeField]
    private GameObject _tilePrefab;

    private readonly Dictionary<int, UiLightTileComponent> _spawnedTiles =
        new Dictionary<int, UiLightTileComponent>();

    private PuzzleDefinition _currentDefinition;

    /// <summary>CSV에서 현재 라이트 퍼즐을 조회하고 UI 타일을 다시 생성합니다.</summary>
    public override void InitializePuzzle()
    {
        ClearSpawnedTiles();
        IsClear = false;
        _currentDefinition = null;

        if (_gridParent == null || _tilePrefab == null)
        {
            Debug.LogError("[UiLightPuzzleManager] Grid Parent 또는 Tile Prefab이 연결되지 않았습니다.");
            return;
        }

        if (!TryGetPuzzleDefinition(
            PuzzleType.PUZZLE_TYPE_LIGHT,
            out PuzzleDefinition puzzleDefinition))
        {
            return;
        }

        _currentDefinition = puzzleDefinition;
        Vector3 startLocalPosition = new Vector3(
            -(puzzleDefinition.Columns - 1) * ItemSpacing / 2f,
            (puzzleDefinition.Rows - 1) * ItemSpacing / 2f,
            0f);

        for (int i = 0; i < puzzleDefinition.LightTiles.Count; i++)
        {
            PuzzleLightTileDefinition tileDefinition = puzzleDefinition.LightTiles[i];
            GameObject tileObject = Instantiate(_tilePrefab, _gridParent);
            tileObject.name = $"UI_LightTile_{tileDefinition.TileId}";

            RectTransform tileRect = tileObject.GetComponent<RectTransform>();
            tileRect.localPosition = startLocalPosition + new Vector3(
                tileDefinition.Column * ItemSpacing,
                -tileDefinition.Row * ItemSpacing,
                0f);

            UiLightTileComponent lightTile = tileObject.GetComponent<UiLightTileComponent>();

            if (lightTile == null)
            {
                Debug.LogError("[UiLightPuzzleManager] 라이트 타일 프리팹에 UiLightTileComponent가 없습니다.");
                Destroy(tileObject);
                ClearSpawnedTiles();
                return;
            }

            lightTile.Init(tileDefinition.TileId, tileDefinition.IsOn, this);
            _spawnedTiles.Add(tileDefinition.TileId, lightTile);
        }

        Debug.Log(
            $"[UiLightPuzzleManager] CSV 퍼즐 배치 완료. id={PuzzleId}, tiles={_spawnedTiles.Count}");
    }

    /// <summary>클릭된 타일의 CSV 영향 목록에 포함된 모든 타일 상태를 반전합니다.</summary>
    public void OnTileClicked(int clickedId)
    {
        if (_currentDefinition == null || IsClear)
        {
            return;
        }

        PuzzleLightTileDefinition clickedDefinition = FindTileDefinition(clickedId);

        if (clickedDefinition == null)
        {
            return;
        }

        for (int i = 0; i < clickedDefinition.AffectedTileIds.Count; i++)
        {
            int targetId = clickedDefinition.AffectedTileIds[i];

            if (_spawnedTiles.TryGetValue(targetId, out UiLightTileComponent targetTile))
            {
                targetTile.ToggleState();
            }
        }

        CheckClearCondition();
    }

    /// <summary>생성된 모든 라이트 타일이 켜졌는지 검사합니다.</summary>
    public override void CheckClearCondition()
    {
        if (_spawnedTiles.Count == 0)
        {
            return;
        }

        foreach (UiLightTileComponent tile in _spawnedTiles.Values)
        {
            if (!tile.IsOn)
            {
                return;
            }
        }

        IsClear = true;
        OnPuzzleClear();
    }

    /// <summary>라이트 퍼즐 클리어 이벤트를 외부에 전달합니다.</summary>
    protected override void OnPuzzleClear()
    {
        Debug.Log($"[UiLightPuzzleManager] 라이트 퍼즐을 클리어했습니다. id={PuzzleId}");
        TriggerClearEvent();
    }

    private PuzzleLightTileDefinition FindTileDefinition(int tileId)
    {
        for (int i = 0; i < _currentDefinition.LightTiles.Count; i++)
        {
            PuzzleLightTileDefinition definition = _currentDefinition.LightTiles[i];

            if (definition.TileId == tileId)
            {
                return definition;
            }
        }

        return null;
    }

    private void ClearSpawnedTiles()
    {
        foreach (UiLightTileComponent tile in _spawnedTiles.Values)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }

        _spawnedTiles.Clear();
    }
}
