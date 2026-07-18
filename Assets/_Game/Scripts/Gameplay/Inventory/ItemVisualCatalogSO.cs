using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ItemVisualAssetEntry
{
    [Tooltip("items.csv의 icon_id와 일치하는 에셋 ID입니다.")]
    [SerializeField]
    private string _iconId;

    [Tooltip("에셋 ID에 연결할 아이템 Sprite입니다.")]
    [SerializeField]
    private Sprite _sprite;

    public string IconId => _iconId;
    public Sprite Sprite => _sprite;
}

//==============================================================================
// 기능: class::ItemVisualCatalogSO
// 설명: items.csv의 icon_id를 Unity Sprite 참조로 변환하는 카탈로그
//==============================================================================
[CreateAssetMenu(fileName = "ItemVisualCatalog", menuName = "Ruin Conservator/Inventory/Visual Catalog")]
public sealed class ItemVisualCatalogSO : ScriptableObject
{
    [SerializeField]
    private List<ItemVisualAssetEntry> _icons = new List<ItemVisualAssetEntry>();

    private readonly Dictionary<string, Sprite> _spriteById = new Dictionary<string, Sprite>();
    private bool _isCacheBuilt;

    private void OnEnable()
    {
        _isCacheBuilt = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _isCacheBuilt = false;
    }
#endif

    public bool TryGetIcon(string iconId, out Sprite sprite)
    {
        EnsureCache();
        sprite = null;
        return !string.IsNullOrWhiteSpace(iconId)
            && _spriteById.TryGetValue(iconId, out sprite);
    }

    private void EnsureCache()
    {
        if (_isCacheBuilt)
        {
            return;
        }

        _spriteById.Clear();

        if (_icons == null)
        {
            _isCacheBuilt = true;
            return;
        }

        for (int i = 0; i < _icons.Count; i++)
        {
            ItemVisualAssetEntry entry = _icons[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.IconId) || entry.Sprite == null)
            {
                continue;
            }

            if (!_spriteById.TryAdd(entry.IconId, entry.Sprite))
            {
                Debug.LogWarning($"[ItemVisualCatalogSO] 중복 아이콘 ID를 건너뜁니다. iconId={entry.IconId}");
            }
        }

        _isCacheBuilt = true;
    }
}
