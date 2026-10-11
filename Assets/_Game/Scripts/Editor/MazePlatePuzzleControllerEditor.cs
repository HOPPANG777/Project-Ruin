using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazePlatePuzzleController))]
public sealed class MazePlatePuzzleControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("사용할 발판은 Plates, 돌은 Stones에 직접 등록하세요. 플레이어 발판은 한 번 밟으면 유지되고, 바위 발판은 바위가 올라가 있는 동안 눌립니다. 모든 등록 발판의 조건을 충족하면 클리어됩니다. Begin On Plate Press를 켜면 시작 트리거가 필요 없습니다.", MessageType.Info);
        DrawDefaultInspector();
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (GUILayout.Button("새 퍼즐 ID 만들기 (복제 후 사용)"))
            {
                SerializedObject puzzle = new SerializedObject(target);
                puzzle.FindProperty("_objectId").stringValue = "MAZE_" + Guid.NewGuid().ToString("N");
                puzzle.ApplyModifiedProperties();
                UpdateRegisteredStoneIds((MazePlatePuzzleController)target);
            }
        }
    }

    // 직접 등록한 돌의 ID만 갱신하며, 기획자가 지정한 부품 목록은 유지합니다.
    private static void UpdateRegisteredStoneIds(MazePlatePuzzleController controller)
    {
        SerializedObject puzzle = new SerializedObject(controller);
        SerializedProperty stones = puzzle.FindProperty("_stones");
        for (int i = 0; i < stones.arraySize; i++)
        {
            PushableStone registered = stones.GetArrayElementAtIndex(i).objectReferenceValue as PushableStone;
            if (registered == null) continue;
            SerializedObject stone = new SerializedObject(registered);
            stone.FindProperty("_puzzleId").stringValue = controller.ObjectId;
            stone.FindProperty("_objectId").stringValue = controller.ObjectId + "_STONE_" + i;
            stone.ApplyModifiedProperties();
        }
    }
}
