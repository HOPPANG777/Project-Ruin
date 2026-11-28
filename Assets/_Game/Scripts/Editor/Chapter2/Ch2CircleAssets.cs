//=============================================================================
// File:        Ch2CircleAssets.cs
// Description: 챕터2 원형판 별자리 설정 에셋과 링 프리팹(편집 시작점)을 만듭니다.
// History:     11/28/2026 + Created
//=============================================================================
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>챕터1 a 퍼즐과 같은 방식: 기존 CirclePattern 복제 + 정답 ID 지정 + 설정 SO.</summary>
public static class Ch2CircleAssets
{
    private const string SourceDir = "Assets/_Game/Scripts/Puzzle/Circle/Temp/";
    private const string OutDir = "Assets/_Game/Data/Chapter2/";
    private static readonly int[] StepMultipliers = { 1, 3, 5, 2 };

    /// <summary>바깥→안쪽 정답·패턴으로 설정 SO를 만들고 경로를 반환합니다.</summary>
    public static string Build(string puzzleId, string[] answersOuterToInner, int[] patternOuterToInner)
    {
        Directory.CreateDirectory(OutDir);
        int count = answersOuterToInner.Length;
        GameObject[] prefabs = new GameObject[count];
        float[] initial = new float[count];
        for (int i = 0; i < count; i++)
        {
            string path = $"{OutDir}{puzzleId}_Ring{i}_{answersOuterToInner[i]}.prefab";
            initial[i] = BuildRing(SourceDir + $"CirclePattern_{patternOuterToInner[i]}.prefab", path,
                puzzleId, i, answersOuterToInner[i], StepMultipliers[i % StepMultipliers.Length]);
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        string settingsPath = $"{OutDir}{puzzleId}_ConstellationSettings.asset";
        CircleConstellationSettingsSO settings = AssetDatabase.LoadAssetAtPath<CircleConstellationSettingsSO>(settingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<CircleConstellationSettingsSO>();
            AssetDatabase.CreateAsset(settings, settingsPath);
        }

        SerializedObject so = new SerializedObject(settings);
        SerializedProperty rings = so.FindProperty("_outerToInner");
        rings.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            SerializedProperty ring = rings.GetArrayElementAtIndex(i);
            ring.FindPropertyRelative("_patternPrefab").objectReferenceValue = prefabs[i];
            ring.FindPropertyRelative("_answerId").stringValue = answersOuterToInner[i];
            ring.FindPropertyRelative("_alignmentDegrees").floatValue = 90f;
            ring.FindPropertyRelative("_initialRotationDegrees").floatValue = initial[i];
            ring.FindPropertyRelative("_rotationStepDegrees").floatValue = 45f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        return settingsPath;
    }

    // 정답 문양이 12시 방향으로 45도 스냅 가능한 첫 문양을 고르고, 풀린 상태가 아닌 초기 회전을 돌려준다.
    private static float BuildRing(string sourcePath, string targetPath, string puzzleId, int ring,
        string answer, int stepMultiplier)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(temp, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        CirclePattern pattern = temp.GetComponent<CirclePattern>();
        SerializedObject so = new SerializedObject(pattern);
        SerializedProperty symbols = so.FindProperty("_symbols");
        int answerIndex = -1;
        float answerAngle = 90f;
        for (int i = 0; i < symbols.arraySize; i++)
        {
            RectTransform icon = (RectTransform)symbols.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue;
            Vector3 direction = icon.position - temp.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float remainder = Mathf.Abs(Mathf.DeltaAngle(90f, angle) / 45f);
            if (answerIndex < 0 && Mathf.Abs(remainder - Mathf.Round(remainder)) < 0.001f)
            {
                answerIndex = i;
                answerAngle = angle;
            }
        }
        if (answerIndex < 0)
        {
            Debug.LogWarning($"[Ch2CircleAssets] 45도 스냅 가능한 문양이 없습니다. {puzzleId} ring={ring}");
            answerIndex = 0;
        }
        for (int i = 0; i < symbols.arraySize; i++)
        {
            symbols.GetArrayElementAtIndex(i).FindPropertyRelative("constellationId").stringValue =
                i == answerIndex ? answer : $"DRAFT_{puzzleId}_R{ring}_{i}";
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(temp, targetPath);
        Object.DestroyImmediate(temp);
        float solved = Mathf.DeltaAngle(0f, 90f - answerAngle);
        return Mathf.DeltaAngle(0f, solved + 45f * stepMultiplier);
    }
}
