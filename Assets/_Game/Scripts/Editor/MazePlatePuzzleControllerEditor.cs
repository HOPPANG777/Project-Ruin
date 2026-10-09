using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazePlatePuzzleController))]
public sealed class MazePlatePuzzleControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (GUILayout.Button("자식 발판 / 돌 / 트리거 / 함정 연결"))
            {
                ConnectChildren((MazePlatePuzzleController)target);
            }
            if (GUILayout.Button("새 퍼즐 ID 만들기 (복제 후 사용)"))
            {
                SerializedObject puzzle = new SerializedObject(target);
                puzzle.FindProperty("_objectId").stringValue = "MAZE_" + Guid.NewGuid().ToString("N");
                puzzle.ApplyModifiedProperties();
                ConnectChildren((MazePlatePuzzleController)target);
            }
        }
    }

    // 중첩된 다른 미로 퍼즐의 자식은 이 컨트롤러에 포함하지 않습니다.
    public static void ConnectChildren(MazePlatePuzzleController controller)
    {
        SerializedObject puzzle = new SerializedObject(controller);
        SetArray(puzzle.FindProperty("_plates"), FindOwned<MazePuzzlePlate>(controller));
        PushableStone[] stones = FindOwned<PushableStone>(controller);
        SetArray(puzzle.FindProperty("_stones"), stones);
        puzzle.ApplyModifiedProperties();

        for (int i = 0; i < stones.Length; i++)
        {
            SerializedObject stone = new SerializedObject(stones[i]);
            stone.FindProperty("_puzzleId").stringValue = controller.ObjectId;
            stone.FindProperty("_objectId").stringValue = controller.ObjectId + "_STONE_" + i;
            stone.ApplyModifiedProperties();
        }
        foreach (MazePlatePuzzleTrigger trigger in FindOwned<MazePlatePuzzleTrigger>(controller))
            SetOwner(trigger, controller);
        foreach (MazeFireEmitter fire in FindOwned<MazeFireEmitter>(controller))
            SetOwner(fire, controller);
        foreach (MazeSpikeTrap spike in FindOwned<MazeSpikeTrap>(controller))
            SetOwner(spike, controller);
    }

    private static T[] FindOwned<T>(MazePlatePuzzleController controller) where T : Component
    {
        return controller.GetComponentsInChildren<T>(true)
            .Where(child => child.GetComponentInParent<MazePlatePuzzleController>(true) == controller)
            .ToArray();
    }

    private static void SetArray(SerializedProperty property, UnityEngine.Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void SetOwner(UnityEngine.Object component, MazePlatePuzzleController controller)
    {
        SerializedObject serialized = new SerializedObject(component);
        serialized.FindProperty("_puzzle").objectReferenceValue = controller;
        serialized.ApplyModifiedProperties();
    }
}
