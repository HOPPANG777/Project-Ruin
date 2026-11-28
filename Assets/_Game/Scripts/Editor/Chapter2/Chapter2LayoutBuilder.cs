//=============================================================================
// File:        Chapter2LayoutBuilder.cs
// Description: PDF 챕터2 배치대로 Grid_2.prefab에 퍼즐 16개를 구성합니다. 다시 실행해도 같은 결과입니다.
// History:     11/28/2026 + Created
//=============================================================================
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>마을 9개 + 유적 1층 6개 + 유적 2층 1개. 기존 Ch2_Puzzles* 그룹만 지우고 다시 만든다.</summary>
public static class Chapter2LayoutBuilder
{
    private const string GridPath = "Assets/_Game/Prefabs/Map/Grid_2.prefab";
    private const string DataDir = "Assets/_Game/Data/Chapter2/";

    [MenuItem("Tools/Project Ruin/Chapter 2/퍼즐 배치 생성 (Grid_2)")]
    public static void Build()
    {
        Ch2Util.LayoutLog.Clear();
        string circleVillage = Ch2CircleAssets.Build("CIRCLE_STAGE_03",
            new[] { "PLACEHOLDER_OUTER", "PLACEHOLDER_2", "PLACEHOLDER_3", "PLACEHOLDER_INNER" }, new[] { 3, 2, 1, 0 });
        string circleRuin1 = Ch2CircleAssets.Build("CIRCLE_STAGE_04",
            new[] { "GEMINI", "VIRGO", "PISCES" }, new[] { 2, 1, 0 });
        string circleRuin6 = Ch2CircleAssets.Build("CIRCLE_STAGE_05",
            new[] { "LEO", "LIBRA", "CAPRICORN", "SAGITTARIUS" }, new[] { 3, 2, 1, 0 });
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject grid = PrefabUtility.LoadPrefabContents(GridPath);
        try
        {
            Transform village = Group(grid, "Tilemap2", "Ch2_Puzzles_Village");
            Transform ruin1F = Group(grid, "Chapter1_ruin", "Ch2_Puzzles_Ruin1F");
            Transform ruin2F = Group(grid, "Chapter1_ruin2f", "Ch2_Puzzles_Ruin2F");
            BuildVillage(village, circleVillage);
            BuildRuin1F(ruin1F, circleRuin1, circleRuin6);
            BuildRuin2F(ruin2F);
            PrefabUtility.SaveAsPrefabAsset(grid, GridPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(grid);
        }
        File.WriteAllLines("Logs/Chapter2Layout.txt", Ch2Util.LayoutLog);
        Debug.Log($"[Chapter2LayoutBuilder] 완료. 배치 기록 {Ch2Util.LayoutLog.Count}개 → Logs/Chapter2Layout.txt");
    }

    /// <summary>배치 모드 진입점: Unity.exe -batchmode -executeMethod Chapter2LayoutBuilder.BuildBatch</summary>
    public static void BuildBatch()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    private static Transform Group(GameObject grid, string tilemapName, string groupName)
    {
        Transform tilemap = Ch2Util.FindDeep(grid.transform, tilemapName);
        for (int i = tilemap.childCount - 1; i >= 0; i--)
        {
            if (tilemap.GetChild(i).name.StartsWith(Ch2Util.RootName)) UnityEngine.Object.DestroyImmediate(tilemap.GetChild(i).gameObject);
        }
        return Ch2Util.NewChild(tilemap, groupName, Vector2.zero).transform;
    }

    private static void BuildVillage(Transform g, string circleSettings)
    {
        Ch2UiPuzzles.BuildVillage(g, "V01", "물줄기", "PIPE", "PIPE_STAGE_08", 406f, 262f, null);
        Ch2UiPuzzles.BuildVillage(g, "V03", "등불", "LIGHT", "LIGHT_STAGE_02", 473f, 563f, null);
        Ch2UiPuzzles.BuildVillage(g, "V04", "등불", "LIGHT", "LIGHT_STAGE_03", 609f, 510f, null);
        Ch2UiPuzzles.BuildVillage(g, "V06", "물줄기", "PIPE", "PIPE_STAGE_09", 862f, 416f, null);
        Ch2UiPuzzles.BuildVillage(g, "V07", "물줄기", "PIPE", "PIPE_STAGE_10", 780f, 319f, null);
        Ch2UiPuzzles.BuildVillage(g, "V09", "원형판", "CIRCLE", "CIRCLE_STAGE_03", 878f, 356f, circleSettings);
        Maze(g, "V02", "발판", 486f, 295f, new Vector4(1312, 221, 1662, 474), new Vector2(1452, 452), new Vector2(1663, 246),
            V(1413, 291), V(1337, 394, 1513, 401), V(1595, 335));
        Maze(g, "V05", "발판", 803f, 502f, new Vector4(306, 760, 624, 998), new Vector2(332, 791), new Vector2(594, 760),
            V(331, 883, 573, 967), V(609, 829, 609, 919), V(613, 875, 473, 986));
        Maze(g, "V08", "발판", 673f, 268f, new Vector4(1328, 772, 1646, 1082), new Vector2(1500, 1054), new Vector2(1366, 773),
            V(1420, 961), V(1345, 980, 1487, 985), V(1555, 835));
    }

    private static void BuildRuin1F(Transform g, string circle1, string circle6)
    {
        Ch2UiPuzzles.Build(g, "R01", "원형판", "CIRCLE", "CIRCLE_STAGE_04", "CH2_RUIN_1F_R01", new Vector2(2384f, 356f),
            new Vector2(2408.5f, 356f), new Vector2(2f, 7f), null, circle1);
        Ch2UiPuzzles.Build(g, "R02", "등불", "LIGHT", "LIGHT_STAGE_04", "CH2_RUIN_1F_R02", new Vector2(2364f, 361f),
            new Vector2(2354f, 356f), new Vector2(2f, 7f), null, null);
        Ch2MazePuzzles.Build(g, new Ch2MazePuzzles.Spec
        {
            Key = "R03", Label = "발판", Id = "CH2_RUIN_1F_R03_MAZE", Frame = new Vector4(436, 708, 910, 990),
            Origin = new Vector2(2344.5f, 372f), Scale = 0.045f, Start = new Vector2(691, 973), Door = new Vector2(504, 707),
            Stones = V(605, 906, 858, 873), Plates = V(514, 846, 889, 730), Chests = V(697, 778)
        });
        Ch2UiPuzzles.Build(g, "R04", "물줄기", "PIPE", "PIPE_STAGE_11", "CH2_RUIN_1F_R04", new Vector2(2379f, 377f),
            new Vector2(2384.5f, 380.5f), new Vector2(7f, 2f), null, null);
        Ch2BeamPuzzles.Build(g, new Ch2BeamPuzzles.Spec
        {
            Key = "R05", Label = "빛잇기", Id = "CH2_RUIN_1F_R05_BEAM", Frame = new Vector4(1232, 702, 1731, 999),
            Origin = new Vector2(2384.5f, 395f), Scale = 0.075f, Light = new Vector2(1391, 790),
            Nodes = V(1320, 781, 1362, 867, 1278, 849, 1447, 911, 1342, 934, 1505, 818, 1505, 894, 1459, 735, 1584, 842, 1530, 748),
            MovedFrom = V(1263, 969, 1657, 951), DoorAt = new Vector2(2369.5f, 411f), DoorSize = new Vector2(4f, 2f),
            Reward = new Vector2(2404f, 402f)
        });
        Ch2UiPuzzles.Build(g, "R06", "원형판", "CIRCLE", "CIRCLE_STAGE_05", "CH2_RUIN_1F_R06", new Vector2(2426f, 380f),
            new Vector2(2437f, 388.5f), new Vector2(8f, 2f), null, circle6);
    }

    private static void BuildRuin2F(Transform g)
    {
        Ch2BeamPuzzles.Build(g, new Ch2BeamPuzzles.Spec
        {
            Key = "R07", Label = "빛잇기", Id = "CH2_RUIN_2F_R07_BEAM", Frame = new Vector4(1132, 78, 1693, 440),
            Origin = new Vector2(729f, -652f), Scale = 0.1f, Light = new Vector2(1187, 141),
            Nodes = V(1287, 121, 1382, 157, 1299, 303, 1464, 283, 1593, 259, 1556, 391, 1197, 220),
            MovedFrom = V(1245, 410), DoorAt = new Vector2(751f, -636f), DoorSize = new Vector2(4f, 2f),
            Reward = new Vector2(751f, -626f)
        });
    }

    private static void Maze(Transform g, string key, string label, float px, float py, Vector4 frame,
        Vector2 start, Vector2 door, Vector2[] stones, Vector2[] plates, Vector2[] chests)
    {
        Vector2 origin = Ch2UiPuzzles.Village(px, py) + Ch2UiPuzzles.VillageDir(px, py) * 24f;
        Ch2MazePuzzles.Build(g, new Ch2MazePuzzles.Spec
        {
            Key = key, Label = label, Id = $"CH2_VILLAGE_{key}_MAZE", Frame = frame, Origin = origin, Scale = 0.07f,
            Start = start, Door = door, Stones = stones, Plates = plates, Chests = chests
        });
    }

    private static Vector2[] V(params float[] xy)
    {
        Vector2[] result = new Vector2[xy.Length / 2];
        for (int i = 0; i < result.Length; i++) result[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
        return result;
    }
}
