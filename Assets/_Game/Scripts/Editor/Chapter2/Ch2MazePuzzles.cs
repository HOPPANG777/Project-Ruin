//=============================================================================
// File:        Ch2MazePuzzles.cs
// Description: 챕터2 발판(돌 밀기) 퍼즐의 컨트롤러·발판·돌·문·초기화 지점을 배치합니다.
// History:     11/28/2026 + Created
//=============================================================================
using UnityEngine;

/// <summary>PDF 미로 이미지의 오브젝트 위치를 비례 축소해 기존 MazePlatePuzzleController로 구성한다.</summary>
public sealed class Ch2MazePuzzles
{
    private const string Dir = "Assets/_Game/Prefabs/Maze/";
    private const float PieceScale = 3f;

    /// <summary>미로 하나의 PDF 좌표 데이터입니다. 좌표는 PDF 표시 좌표(px)입니다.</summary>
    public struct Spec
    {
        public string Key, Label, Id;
        public Vector4 Frame;          // x0, y0, x1, y1
        public Vector2 Origin;         // 미로 중심이 놓일 로컬 타일 좌표
        public float Scale;            // px → 타일
        public Vector2 Start, Door;
        public Vector2[] Stones, Plates, Chests;
    }

    public static void Build(Transform group, Spec spec)
    {
        Vector2 center = new Vector2((spec.Frame.x + spec.Frame.z) / 2f, (spec.Frame.y + spec.Frame.w) / 2f);
        System.Func<Vector2, Vector2> map = p => spec.Origin + new Vector2((p.x - center.x) * spec.Scale, -(p.y - center.y) * spec.Scale);
        Vector2 frameSize = new Vector2((spec.Frame.z - spec.Frame.x) * spec.Scale, (spec.Frame.w - spec.Frame.y) * spec.Scale);
        GameObject root = Ch2Util.NewChild(group, spec.Key + "_" + spec.Label, Vector2.zero);

        GameObject guide = Ch2Util.Block(root.transform, "미로영역_가이드(임시_벽은_PD작업)", spec.Origin, frameSize,
            new Color(1f, 1f, 1f, 0.12f), false);
        guide.GetComponent<SpriteRenderer>().sortingOrder = -1;

        GameObject controller = Ch2Util.Spawn(Dir + "MazePlatePuzzleController.prefab", root.transform,
            "컨트롤러_" + spec.Id, spec.Origin);
        MazePlatePuzzleController puzzle = controller.GetComponent<MazePlatePuzzleController>();
        Ch2Util.SetString(puzzle, "_objectId", spec.Id);
        Ch2Util.SetBool(puzzle, "_beginOnPlatePress", true);

        PushableStone[] stones = new PushableStone[spec.Stones.Length];
        for (int i = 0; i < stones.Length; i++)
        {
            Vector2 at = map(spec.Stones[i]);
            GameObject stone = Ch2Util.Spawn(Dir + "MazePushableStone.prefab", root.transform, $"돌_{i + 1}", at);
            stone.transform.localScale = Vector3.one * PieceScale;
            stones[i] = stone.GetComponent<PushableStone>();
            Ch2Util.SetString(stones[i], "_objectId", $"{spec.Id}_STONE_{i}");
            Ch2Util.SetString(stones[i], "_puzzleId", spec.Id);
            Ch2Util.Record(group.name, $"{spec.Key}_돌{i + 1}", at, Vector2.one * 2.5f);
        }

        MazePuzzlePlate[] plates = new MazePuzzlePlate[spec.Plates.Length];
        bool[] stonePlate = AssignStonePlates(spec);
        for (int i = 0; i < plates.Length; i++)
        {
            Vector2 at = map(spec.Plates[i]);
            string prefab = stonePlate[i] ? "MazeStonePlate.prefab" : "MazePlayerPlate.prefab";
            string label = stonePlate[i] ? "돌발판" : "플레이어발판";
            GameObject plate = Ch2Util.Spawn(Dir + prefab, root.transform, $"{label}_{i + 1}", at);
            plate.transform.localScale = Vector3.one * PieceScale;
            // 플레이어/돌 발판 프리팹에는 챕터0 단독 발판용 컨트롤러가 내장돼 있다. 공용 컨트롤러에 등록하므로 제거한다.
            Object.DestroyImmediate(plate.GetComponent<MazePlatePuzzleController>());
            plates[i] = plate.GetComponent<MazePuzzlePlate>();
            Ch2Util.Record(group.name, $"{spec.Key}_{label}{i + 1}", at, Vector2.one * 3f);
        }
        Ch2Util.SetRefs(puzzle, "_plates", plates);
        Ch2Util.SetRefs(puzzle, "_stones", stones);

        Vector2 doorAt = map(spec.Door);
        Vector2 doorSize = new Vector2(Mathf.Max(45f * spec.Scale, 6f), 2f);
        GameObject door = Ch2Util.Block(root.transform, "문_" + spec.Key, doorAt, doorSize, new Color(0.9f, 0.1f, 0.1f), true);
        GameObject opened = Ch2Util.Block(root.transform, "열린문표시_" + spec.Key, doorAt, doorSize,
            new Color(0.2f, 0.9f, 0.3f, 0.35f), false);
        Ch2Util.SetRef(puzzle, "_unsolvedResultObject", door);
        Ch2Util.SetRef(puzzle, "_solvedResultObject", opened);

        Vector2 startAt = map(spec.Start);
        Ch2Util.Block(root.transform, "시작지점(임시)", startAt + new Vector2(0f, 4f), new Vector2(2f, 2f),
            new Color(0.1f, 0.2f, 1f), false, 45f);
        GameObject reset = Ch2Util.NewChild(root.transform, "초기화지점_돌이_막히면_밟기", startAt);
        reset.AddComponent<BoxCollider2D>().isTrigger = true;
        reset.GetComponent<BoxCollider2D>().size = new Vector2(3f, 3f);
        MazePlatePuzzleTrigger trigger = reset.AddComponent<MazePlatePuzzleTrigger>();
        Ch2Util.SetRef(trigger, "_puzzle", puzzle);
        Ch2Util.SetInt(trigger, "_action", 1);
        Ch2Util.Record(group.name, spec.Key + "_초기화", startAt, new Vector2(3f, 3f));
        BuildChests(root.transform, spec, map);
    }

    // 돌마다 가장 가까운 발판을 돌 발판으로 지정하고, 남은 발판은 플레이어 발판이 된다.
    private static bool[] AssignStonePlates(Spec spec)
    {
        bool[] result = new bool[spec.Plates.Length];
        for (int s = 0; s < spec.Stones.Length; s++)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int p = 0; p < result.Length; p++)
            {
                float distance = (spec.Plates[p] - spec.Stones[s]).sqrMagnitude;
                if (!result[p] && distance < bestDistance) { best = p; bestDistance = distance; }
            }
            if (best >= 0) result[best] = true;
        }
        return result;
    }

    private static void BuildChests(Transform root, Spec spec, System.Func<Vector2, Vector2> map)
    {
        for (int i = 0; i < spec.Chests.Length; i++)
        {
            Ch2Util.Block(root, $"보상표시_{spec.Key}_{i + 1}", map(spec.Chests[i]), new Vector2(2f, 2f),
                new Color(0.25f, 0.55f, 0.2f), false, 45f);
        }
    }
}
