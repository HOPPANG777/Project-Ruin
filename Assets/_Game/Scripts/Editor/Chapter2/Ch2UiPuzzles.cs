//=============================================================================
// File:        Ch2UiPuzzles.cs
// Description: 챕터2 UI 퍼즐(물줄기·등불·원형판)의 조작대·문·보상 표시를 배치합니다.
// History:     11/28/2026 + Created
//=============================================================================
using UnityEngine;

/// <summary>PDF 번호 순서와 같은 이름으로 UI 퍼즐 배치를 만든다.</summary>
public static class Ch2UiPuzzles
{
    private const string PipePrefab = "Assets/_Game/Prefabs/Puzzle/PipePuzzleInteractable.prefab";
    private const string LightPrefab = "Assets/_Game/Prefabs/Puzzle/LightPuzzleInteractable.prefab";
    private const string CirclePrefab = "Assets/_Game/Prefabs/Puzzle/CirclePuzzleInteractable.prefab";
    private static readonly Color ConsoleColor = new Color(0.3f, 0.75f, 1f, 0.9f);
    private static readonly Color DoorColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    private static readonly Color RewardColor = new Color(0.25f, 0.55f, 0.2f, 1f);

    /// <summary>PDF 마을 개략도(표시 좌표)를 Tilemap2 로컬 좌표로 바꾼다. 임시 배율 0.25입니다.</summary>
    public static Vector2 Village(float px, float py)
        => new Vector2(-30f + (px - 578f) * 0.25f, -722f - (py - 405f) * 0.25f);

    /// <summary>가장 가까운 마을 중심에서 번호표 쪽으로 향하는 단위 방향입니다.</summary>
    public static Vector2 VillageDir(float px, float py)
    {
        Vector2 a = new Vector2(505f, 410f);
        Vector2 b = new Vector2(676f, 413f);
        Vector2 p = new Vector2(px, py);
        Vector2 hub = (p - a).sqrMagnitude < (p - b).sqrMagnitude ? a : b;
        Vector2 d = p - hub;
        return new Vector2(d.x, -d.y).normalized;
    }

    /// <summary>마을 퍼즐 하나의 조작대·문·보상 표시를 만든다.</summary>
    public static void BuildVillage(Transform group, string key, string label, string kind, string puzzleId,
        float px, float py, string settings)
    {
        Vector2 dir = VillageDir(px, py);
        Vector2 console = Village(px, py);
        Vector2 doorSize = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? new Vector2(2f, 8f) : new Vector2(8f, 2f);
        Build(group, key, label, kind, puzzleId, "CH2_VILLAGE_" + key, console,
            console + dir * 7f, doorSize, console + dir * 14f, settings);
    }

    /// <summary>유적 퍼즐 하나의 조작대·문을 만든다. 보상 표시는 선택입니다.</summary>
    public static void Build(Transform group, string key, string label, string kind, string puzzleId,
        string saveId, Vector2 console, Vector2 doorPos, Vector2 doorSize, Vector2? reward, string settings)
    {
        GameObject root = Ch2Util.NewChild(group, key + "_" + label, Vector2.zero);
        string path = kind == "PIPE" ? PipePrefab : kind == "LIGHT" ? LightPrefab : CirclePrefab;
        GameObject console_ = Ch2Util.Spawn(path, root.transform, "조작대_" + puzzleId, console);
        console_.GetComponent<BoxCollider2D>().size = new Vector2(4f, 4f);
        Ch2Util.SetString(console_.GetComponent<UiPuzzleInteractable>(), "_puzzleId", puzzleId);
        Ch2Util.SetInt(console_.GetComponent<UiPuzzleInteractable>(), "_chapterIndex", 2);
        if (!string.IsNullOrEmpty(settings))
        {
            Ch2Util.SetRef(console_.GetComponent<UiPuzzleInteractable>(), "_constellationSettings",
                UnityEditor.AssetDatabase.LoadAssetAtPath<CircleConstellationSettingsSO>(settings));
        }
        Ch2Util.Block(console_.transform, "표시(임시_교체)", Vector2.zero, new Vector2(3f, 3f), ConsoleColor, false);
        Ch2Util.Record(group.name, key + "_조작대", console, new Vector2(4f, 4f));

        GameObject door = Ch2Util.Block(root.transform, "문_" + key, doorPos, doorSize, DoorColor, true);
        UiPuzzleSceneState state = console_.AddComponent<UiPuzzleSceneState>();
        Ch2Util.SetString(state, "_objectId", saveId);
        Ch2Util.SetInt(state, "_stageNumber", Ch2Util.StageNumber);
        Ch2Util.SetRef(state, "_closedDoor", door);

        if (reward.HasValue)
        {
            Ch2Util.Block(root.transform, "보상표시_" + key, reward.Value, new Vector2(2f, 2f), RewardColor, false, 45f);
        }
    }
}
