//=============================================================================
// File: BeamPuzzleEditor.cs
// Description: 기획자가 개별 부품을 직접 배치하고 필요한 값과 참조를 지정한다.
// History: 10/06/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>기획용 설정과 명시 참조 검증을 제공하는 관리자 Inspector입니다.</summary>
[CustomEditor(typeof(BeamPuzzleController))]
public sealed class BeamPuzzleEditor : Editor
{
    private bool _showAppearance;
    private bool _showEvents;
    private bool _showAdvanced;

    /// <inheritdoc/>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("1. 맵에 부품을 배치합니다.\n2. 저장 ID를 정합니다.\n3. 아래 목록에 이 퍼즐의 부품을 직접 끌어 넣습니다.", MessageType.Info);
        BeamInspectorFields.Field(serializedObject, "_objectId", "퍼즐 저장 ID", "예: CH01_BEAM_01. 같은 스테이지의 다른 관리자와 겹치지 않게 지정합니다.");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("필수 부품", EditorStyles.boldLabel);
        BeamInspectorFields.List(serializedObject, "_pillars", "기둥", "시작 1개와 끝 1개를 포함한 모든 기둥을 등록합니다.");
        BeamInspectorFields.List(serializedObject, "_resetPads", "초기화 발판", "최소 1개를 등록합니다. 플레이어가 밟으면 미완료 퍼즐을 초기화합니다.");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("선택 부품 · 사용하지 않으면 등록 수 0", EditorStyles.boldLabel);
        BeamInspectorFields.List(serializedObject, "_stones", "밀 수 있는 돌");
        BeamInspectorFields.List(serializedObject, "_walls", "부술 수 있는 벽");
        BeamInspectorFields.List(serializedObject, "_fixedObstacles", "고정 장애물", "장애물 루트의 Collider2D를 등록합니다.");
        BeamInspectorFields.List(serializedObject, "_exitObstacles", "출구 장애물", "완료하면 꺼질 독립 장애물 오브젝트를 등록합니다.");
        if (_showAppearance = EditorGUILayout.Foldout(_showAppearance, "광선 모양과 길이", true))
        {
            BeamInspectorFields.Field(serializedObject, "_beamColor", "광선 색상");
            BeamInspectorFields.Field(serializedObject, "_beamWidth", "광선 굵기");
            BeamInspectorFields.Field(serializedObject, "_maxBeamDistance", "광선 최대 길이");
            BeamInspectorFields.Field(serializedObject, "_beamSortingOrder", "광선 표시 순서", "같은 정렬 레이어에서 큰 숫자가 앞에 표시됩니다.");
        }
        if (_showEvents = EditorGUILayout.Foldout(_showEvents, "완료 시 추가 동작", true))
        {
            EditorGUILayout.HelpBox("출구 열기는 위 목록만 등록하면 됩니다. 추가 보상이나 연출이 있을 때만 이벤트를 연결하세요. 저장된 완료 상태를 불러올 때는 호출하지 않습니다.", MessageType.Info);
            BeamInspectorFields.Field(serializedObject, "_onSolved", "최초 완료 이벤트");
        }
        if (_showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 · 충돌과 표시 참조", true))
        {
            BeamInspectorFields.Field(serializedObject, "_beamCollisionMask", "광선 충돌 레이어");
            BeamInspectorFields.Field(serializedObject, "_beamPrefab", "광선 표시 프리팹");
            BeamInspectorFields.Field(serializedObject, "_beamParent", "생성된 광선의 부모 (선택)");
            BeamInspectorFields.Field(serializedObject, "_beamSortingLayer", "광선 정렬 레이어");
            BeamInspectorFields.Field(serializedObject, "_beamZ", "광선 Z 위치");
        }
        serializedObject.ApplyModifiedProperties();
        var issues = new List<string>();
        ((BeamPuzzleController)target).CollectValidationIssues(issues);
        for (int i = 0; i < issues.Count; i++) EditorGUILayout.HelpBox(issues[i], MessageType.Error);
        if (issues.Count == 0) EditorGUILayout.HelpBox("부품 연결과 저장 ID 확인 완료", MessageType.Info);
    }

    /// <summary>씬 부품을 자동 연결하거나 변경하지 않고 오류만 보고합니다.</summary>
    [MenuItem("Tools/Project Ruin/Beam/현재 맵 설정 검증")]
    public static void ValidateMap()
    {
        BeamPuzzleController[] controllers = Object.FindObjectsByType<BeamPuzzleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            var issues = new List<string>();
            controllers[i].CollectValidationIssues(issues);
            if (issues.Count == 0) Debug.Log("[Beam] 설정 검증 통과: " + controllers[i].name, controllers[i]);
            else Debug.LogError("[Beam] " + controllers[i].name + ": " + string.Join(" / ", issues), controllers[i]);
        }
    }
}

/// <summary>기둥의 ID, 각도, 그림을 먼저 표시하고 구조 참조는 접어 둡니다.</summary>
[CustomEditor(typeof(BeamPillarView))]
public sealed class BeamPillarEditor : Editor
{
    private static readonly string[] TYPE_NAMES = { "시작 기둥", "2방향 기둥 · 출력 1개", "4방향 기둥 · 출력 3개", "8방향 기둥 · 출력 7개", "끝 기둥 · 출력 없음" };
    private bool _showColors;
    private bool _showAdvanced;

    /// <inheritdoc/>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty settings = serializedObject.FindProperty("_settings");
        SerializedProperty type = settings.FindPropertyRelative("_type");
        int typeIndex = type.enumValueIndex;
        EditorGUILayout.LabelField(typeIndex >= 0 && typeIndex < TYPE_NAMES.Length ? TYPE_NAMES[typeIndex] : "기둥 종류 오류", EditorStyles.boldLabel);
        BeamInspectorFields.Field(serializedObject, "_settings._id", "기둥 ID", "예: START, P01, END. 이 퍼즐 내부에서 중복되지 않게 지정합니다.");
        BeamInspectorFields.Field(serializedObject, "_settings._isRotatable", "플레이어가 회전 가능");
        BeamInspectorFields.Field(serializedObject, "_settings._initialRotationDegrees", "처음 바라보는 각도 (°)");
        using (new EditorGUI.DisabledScope(!settings.FindPropertyRelative("_isRotatable").boolValue))
            BeamInspectorFields.Field(serializedObject, "_settings._rotationStepDegrees", "한 번 누르면 시계방향 (°)");
        EditorGUILayout.HelpBox("0° 오른쪽 / 90° 위 / 180° 왼쪽 / 270° 아래\n최종 출력 방향 = 기둥이 바라보는 각도 + 아래 출력 각도\n처음 바라보는 각도는 Transform의 Z 회전 대신 여기서 정합니다.", MessageType.Info);
        SerializedProperty outputs = settings.FindPropertyRelative("_outputAnglesDegrees");
        for (int i = 0; i < outputs.arraySize; i++)
            EditorGUILayout.PropertyField(outputs.GetArrayElementAtIndex(i), new GUIContent("출력 " + (i + 1) + "의 추가 각도 (°)"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("그림 교체", EditorStyles.boldLabel);
        BeamInspectorFields.Sprite((SpriteRenderer)serializedObject.FindProperty("_bodyRenderer").objectReferenceValue, "몸체 그림");
        BeamInspectorFields.ArrowSprites(serializedObject.FindProperty("_outputArrows"));
        if (_showColors = EditorGUILayout.Foldout(_showColors, "연결 표시 색상", true))
        {
            BeamInspectorFields.Field(serializedObject, "_disconnectedColor", "빛을 받기 전 몸체 색상");
            BeamInspectorFields.Field(serializedObject, "_connectedColor", "빛을 받은 몸체 색상");
            BeamInspectorFields.Field(serializedObject, "_arrowColor", "화살표 색상");
        }
        if (_showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 · 기둥 종류와 구조 참조", true))
        {
            EditorGUILayout.HelpBox("기본 부품의 연결은 준비되어 있습니다. 종류와 출력 수를 바꾸면 출력 화살표도 같은 수로 직접 구성해야 합니다.", MessageType.Info);
            EditorGUILayout.PropertyField(type, new GUIContent("기둥 종류"));
            EditorGUILayout.PropertyField(outputs, new GUIContent("출력 각도 배열"), true);
            BeamInspectorFields.Field(serializedObject, "_bodyCollider", "이동을 막는 몸체");
            BeamInspectorFields.Field(serializedObject, "_receiverCollider", "빛을 받는 충돌체");
            BeamInspectorFields.Field(serializedObject, "_interactionTrigger", "접근 감지 범위");
            BeamInspectorFields.Field(serializedObject, "_bodyRenderer", "몸체 표시 컴포넌트");
            BeamInspectorFields.Field(serializedObject, "_directionRoot", "방향 표시 부모");
            BeamInspectorFields.Field(serializedObject, "_outputArrows", "출력 화살표 컴포넌트");
        }
        serializedObject.ApplyModifiedProperties();
        if (!((BeamPillarView)target).TryValidate(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
    }
}

/// <summary>초기화 발판의 그림 교체와 수동 등록 안내를 제공합니다.</summary>
[CustomEditor(typeof(BeamResetPad))]
public sealed class BeamResetPadEditor : Editor
{
    private bool _showAdvanced;

    /// <inheritdoc/>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("맵에 배치한 뒤 관리자의 '초기화 발판' 목록에 직접 등록하세요. 플레이어가 밟으면 미완료 퍼즐을 처음 상태로 되돌립니다.", MessageType.Info);
        BeamInspectorFields.Sprite(((BeamResetPad)target).GetComponentInChildren<SpriteRenderer>(true), "발판 그림");
        if (_showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 · 감지 참조", true))
            BeamInspectorFields.Field(serializedObject, "_trigger", "발판 감지 범위");
        serializedObject.ApplyModifiedProperties();
    }
}

/// <summary>Beam 돌에서만 기획용 설정을 표시하고 기존 미로 Inspector는 유지합니다.</summary>
[CustomEditor(typeof(PushableStone))]
public sealed class BeamStoneEditor : Editor
{
    private bool _showAdvanced;

    /// <inheritdoc/>
    public override void OnInspectorGUI()
    {
        var stone = (PushableStone)target;
        if (!BeamInspectorFields.IsBeamPrefab(stone)) { DrawDefaultInspector(); return; }
        serializedObject.Update();
        EditorGUILayout.HelpBox("관리자의 '밀 수 있는 돌' 목록에 직접 등록하세요. 필요한 스킬을 가진 플레이어가 기존 상호작용 입력을 유지하면 밉니다.", MessageType.Info);
        BeamInspectorFields.Field(serializedObject, "_objectId", "돌 ID", "예: STONE01. 이 퍼즐의 돌·벽 사이에서 중복되지 않게 지정합니다.");
        BeamInspectorFields.Field(serializedObject, "_requiredSkill", "필요한 밀기 스킬");
        BeamInspectorFields.Field(serializedObject, "_pushSpeed", "밀리는 속도");
        BeamInspectorFields.Sprite(stone.GetComponentInChildren<SpriteRenderer>(true), "돌 몸체 그림");
        if (_showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 · 기존 돌 설정", true))
            DrawPropertiesExcluding(serializedObject, "m_Script", "_objectId", "_requiredSkill", "_pushSpeed");
        serializedObject.ApplyModifiedProperties();
    }
}

/// <summary>Beam 벽에서만 기획용 설정을 표시하고 기존 벽 Inspector는 유지합니다.</summary>
[CustomEditor(typeof(BreakableWall))]
public sealed class BeamWallEditor : Editor
{
    private bool _showAdvanced;

    /// <inheritdoc/>
    public override void OnInspectorGUI()
    {
        var wall = (BreakableWall)target;
        if (!BeamInspectorFields.IsBeamPrefab(wall)) { DrawDefaultInspector(); return; }
        serializedObject.Update();
        EditorGUILayout.HelpBox("관리자의 '부술 수 있는 벽' 목록에 직접 등록하세요. 필요한 스킬을 가진 플레이어가 기존 상호작용 입력을 누르면 파괴 준비를 시작합니다.", MessageType.Info);
        BeamInspectorFields.Field(serializedObject, "_objectId", "벽 ID", "예: WALL01. 이 퍼즐의 돌·벽 사이에서 중복되지 않게 지정합니다.");
        BeamInspectorFields.Field(serializedObject, "_requiredSkill", "필요한 파괴 스킬");
        BeamInspectorFields.Field(serializedObject, "_prepareDuration", "파괴 준비 시간 (초)");
        BeamInspectorFields.Sprite((SpriteRenderer)serializedObject.FindProperty("_wallRenderer").objectReferenceValue, "벽 그림");
        if (_showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 · 기존 벽 설정", true))
            DrawPropertiesExcluding(serializedObject, "m_Script", "_objectId", "_requiredSkill", "_prepareDuration");
        serializedObject.ApplyModifiedProperties();
    }
}

/// <summary>Beam Inspector의 수동 필드 편집을 위한 작은 표시 도우미입니다.</summary>
internal static class BeamInspectorFields
{
    internal static void Field(SerializedObject owner, string path, string label, string tooltip = null)
    {
        EditorGUILayout.PropertyField(owner.FindProperty(path), new GUIContent(label, tooltip), true);
    }

    internal static void List(SerializedObject owner, string path, string label, string tooltip = null)
    {
        SerializedProperty list = owner.FindProperty(path);
        list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, new GUIContent(label + " (" + list.arraySize + ")", tooltip), true);
        if (!list.isExpanded) return;
        EditorGUI.indentLevel++;
        int oldCount = list.arraySize;
        int count = Mathf.Max(0, EditorGUILayout.IntField("등록 수", oldCount));
        if (count != oldCount)
        {
            list.arraySize = count;
            // Unity 배열 확장 시 마지막 참조를 복제하지 않고 새 칸은 직접 등록할 수 있게 비웁니다.
            for (int i = oldCount; i < count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = null;
        }
        for (int i = 0; i < list.arraySize; i++)
            EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent(label + " " + (i + 1)));
        EditorGUI.indentLevel--;
    }

    internal static void Sprite(SpriteRenderer renderer, string label)
    {
        if (renderer == null) return;
        var visual = new SerializedObject(renderer);
        visual.Update();
        EditorGUILayout.PropertyField(visual.FindProperty("m_Sprite"), new GUIContent(label, "Sprite 에셋을 끌어 넣습니다. 충돌체와 회전 설정은 유지됩니다."));
        visual.ApplyModifiedProperties();
    }

    internal static void ArrowSprites(SerializedProperty arrows)
    {
        Sprite first = null;
        bool found = false;
        bool mixed = false;
        for (int i = 0; i < arrows.arraySize; i++)
        {
            var renderer = arrows.GetArrayElementAtIndex(i).objectReferenceValue as SpriteRenderer;
            if (renderer == null) continue;
            if (!found) { first = renderer.sprite; found = true; }
            else if (renderer.sprite != first) mixed = true;
        }
        if (!found) return;
        EditorGUI.showMixedValue = mixed;
        EditorGUI.BeginChangeCheck();
        var sprite = (Sprite)EditorGUILayout.ObjectField(new GUIContent("화살표 그림 (모든 출력)", "오른쪽을 향하는 Sprite를 지정합니다. 출력별로 바꾸려면 자식의 SpriteRenderer를 편집합니다."), first, typeof(Sprite), false);
        bool changed = EditorGUI.EndChangeCheck();
        EditorGUI.showMixedValue = false;
        if (!changed) return;
        for (int i = 0; i < arrows.arraySize; i++)
        {
            var renderer = arrows.GetArrayElementAtIndex(i).objectReferenceValue as SpriteRenderer;
            if (renderer == null) continue;
            var visual = new SerializedObject(renderer);
            visual.FindProperty("m_Sprite").objectReferenceValue = sprite;
            visual.ApplyModifiedProperties();
        }
    }

    internal static bool IsBeamPrefab(Component component)
    {
        string path = AssetDatabase.GetAssetPath(component);
        if (string.IsNullOrEmpty(path))
        {
            var stage = PrefabStageUtility.GetPrefabStage(component.gameObject);
            path = stage != null ? stage.assetPath : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component.gameObject);
        }
        return path != null && path.StartsWith(BeamPrefabBuilder.Folder + "/", System.StringComparison.Ordinal);
    }
}
