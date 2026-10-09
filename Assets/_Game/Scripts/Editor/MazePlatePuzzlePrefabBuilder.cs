using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 기존 부품 프리팹을 연결하여 맵 배치용 미로 퍼즐을 저장합니다.
public static class MazePlatePuzzlePrefabBuilder
{
    public const string PrefabPath = "Assets/_Game/Prefabs/Maze/MazePlatePuzzle.prefab";
    private const string Folder = "Assets/_Game/Prefabs/Maze/";

    [MenuItem("Tools/Project Ruin/Maze/Create Plate Puzzle Prefab")]
    public static void CreatePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            return;
        }

        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject root = new GameObject("MazePlatePuzzle", typeof(Grid));
            SceneManager.MoveGameObjectToScene(root, preview);
            MazePlatePuzzleController controller = root.AddComponent<MazePlatePuzzleController>();
            SerializedObject puzzle = new SerializedObject(controller);
            puzzle.FindProperty("_objectId").stringValue = "MAZE_PLATE_PUZZLE_TEMPLATE";
            puzzle.ApplyModifiedPropertiesWithoutUndo();

            Place("MazePuzzlePlate", "Plate_A", root.transform, new Vector2(-1, 2));
            Place("MazePuzzlePlate", "Plate_B", root.transform, new Vector2(1, 2));
            Place("MazePushableStone", "Stone_A", root.transform, new Vector2(-1, 0));
            Place("MazePushableStone", "Stone_B", root.transform, new Vector2(1, 0));
            CreateTrigger(root.transform, "BeginTrigger", new Vector2(0, -2),
                MazePlatePuzzleTriggerAction.MAZE_PLATE_TRIGGER_BEGIN, new Color(0.2f, 0.65f, 0.9f));
            CreateTrigger(root.transform, "ResetTrigger", new Vector2(-3, -2),
                MazePlatePuzzleTriggerAction.MAZE_PLATE_TRIGGER_RESET, new Color(0.9f, 0.65f, 0.2f));

            GameObject closed = Place("MazeWall", "GateClosed", root.transform, new Vector2(0, 4));
            closed.transform.localScale = new Vector3(3, 1, 1);
            GameObject opened = Place("MazeWall", "GateOpen", root.transform, new Vector2(0, 4));
            // 닫힌 문의 벽과 구분되는 통과 가능한 완료 표시입니다.
            UnityEngine.Object.DestroyImmediate(opened.GetComponent<BoxCollider2D>());
            opened.layer = 0;
            opened.transform.localScale = new Vector3(3, 0.2f, 1);
            opened.GetComponentInChildren<SpriteRenderer>().color = new Color(0.2f, 0.8f, 0.4f);
            opened.SetActive(false);
            puzzle.Update();
            puzzle.FindProperty("_unsolvedResultObject").objectReferenceValue = closed;
            puzzle.FindProperty("_solvedResultObject").objectReferenceValue = opened;
            puzzle.ApplyModifiedPropertiesWithoutUndo();
            MazePlatePuzzleControllerEditor.ConnectChildren(controller);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            if (!success) throw new InvalidOperationException("미로 발판 퍼즐 프리팹 저장 실패");
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Debug.Log("MazePlatePuzzle 프리팹 생성 완료. 맵에 배치하고 퍼즐별 Object Id를 설정하세요.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static GameObject Place(string prefabName, string name, Transform parent, Vector2 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + prefabName + ".prefab");
        if (prefab == null) throw new InvalidOperationException("미로 부품 프리팹 없음: " + prefabName);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = position;
        return instance;
    }

    private static void CreateTrigger(Transform parent, string name, Vector2 position,
        MazePlatePuzzleTriggerAction action, Color color)
    {
        GameObject marker = Place("MazeWall", name, parent, position);
        marker.layer = 0;
        marker.GetComponent<BoxCollider2D>().isTrigger = true;
        marker.GetComponentInChildren<SpriteRenderer>().color = color;
        MazePlatePuzzleTrigger trigger = marker.AddComponent<MazePlatePuzzleTrigger>();
        SerializedObject serialized = new SerializedObject(trigger);
        serialized.FindProperty("_action").intValue = (int)action;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
