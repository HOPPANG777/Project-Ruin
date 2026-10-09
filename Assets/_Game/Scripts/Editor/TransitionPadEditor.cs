using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneTransitionPad))]
public sealed class SceneTransitionPadEditor : TransitionPadEditor { }

[CustomEditor(typeof(PositionTransitionPad))]
public sealed class PositionTransitionPadEditor : TransitionPadEditor { }

public abstract class TransitionPadEditor : Editor
{
    private SceneAsset _selectedScene;
    private int _lastStage = int.MinValue;
    private string _setupError;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), new GUIContent("스크립트"));
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            if (target is PositionTransitionPad) DrawPositionDestination();
            else DrawSceneDestination();
            serializedObject.ApplyModifiedProperties();
            DrawTrigger();
        }
    }

    private void DrawPositionDestination()
    {
        PositionTransitionPad pad = (PositionTransitionPad)target;
        EditorGUILayout.HelpBox("발판을 맵에 배치한 뒤, 자식 '도착 위치'를 원하는 곳으로 옮기세요.\n플레이어가 발판을 밟으면 화면이 어두워진 동안 이동합니다.", MessageType.Info);
        SerializedProperty property = serializedObject.FindProperty("_destination");
        EditorGUI.BeginChangeCheck();
        Transform chosen = (Transform)EditorGUILayout.ObjectField(
            new GUIContent("도착 위치", "같은 맵에 있는 오브젝트를 연결할 수도 있습니다."),
            property.objectReferenceValue, typeof(Transform), !EditorUtility.IsPersistent(pad));
        if (EditorGUI.EndChangeCheck()) property.objectReferenceValue = chosen;
        Transform destination = property.objectReferenceValue as Transform;
        if (destination == null)
        {
            EditorGUILayout.HelpBox("도착 위치가 없습니다. 아래 버튼으로 만들 수 있습니다.", MessageType.Error);
            using (new EditorGUI.DisabledScope(EditorUtility.IsPersistent(pad)))
            {
                if (GUILayout.Button("도착 위치 만들고 연결하기"))
                {
                    GameObject marker = new GameObject(TransitionPadMapTools.DestinationName);
                    Undo.RegisterCreatedObjectUndo(marker, "도착 위치 만들기");
                    Undo.SetTransformParent(marker.transform, pad.transform, "도착 위치 연결");
                    marker.transform.localPosition = new Vector3(3f, 0f, 0f);
                    property.objectReferenceValue = marker.transform;
                }
            }
            return;
        }
        if (destination.gameObject.scene != pad.gameObject.scene)
            EditorGUILayout.HelpBox("같은 맵 안의 도착 위치를 연결하세요.", MessageType.Error);
        else if (Contains(pad.GetComponent<BoxCollider2D>(), destination.position))
            EditorGUILayout.HelpBox("도착 위치가 이 발판 안에 있습니다. 발판 밖으로 옮기세요.", MessageType.Warning);
        if (GUILayout.Button("도착 위치 선택해서 옮기기"))
        {
            Selection.activeGameObject = destination.gameObject;
            SceneView.FrameLastActiveSceneView();
        }
    }

    private void DrawSceneDestination()
    {
        EditorGUILayout.HelpBox("1. 목적지 맵을 선택하고 '목적지 맵 연결'을 누르세요.\n2. 도착 지점을 선택하세요. 지점이 없으면 목적지 맵에서 Hierarchy 메뉴로 추가할 수 있습니다.", MessageType.Info);
        SerializedProperty stage = serializedObject.FindProperty("_destinationStageNumber");
        SerializedProperty spawn = serializedObject.FindProperty("_targetSpawnPointId");
        SceneCatalogSO catalog = TransitionPadSceneSetup.FindCatalog();
        if (_lastStage != stage.intValue)
        {
            _selectedScene = TransitionPadSceneSetup.FindScene(catalog, stage.intValue);
            _lastStage = stage.intValue;
        }
        EditorGUI.BeginChangeCheck();
        _selectedScene = (SceneAsset)EditorGUILayout.ObjectField("목적지 맵", _selectedScene, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            _setupError = null;
            if (_selectedScene == null)
            {
                stage.intValue = -1;
                spawn.stringValue = string.Empty;
                _lastStage = -1;
            }
        }

        if (catalog == null)
        {
            EditorGUILayout.HelpBox("GameRoot의 SceneCatalog가 연결되어 있지 않습니다. 담당 개발자에게 연결을 요청하세요.", MessageType.Error);
            return;
        }
        bool connected = _selectedScene != null
            && catalog.TryGetByStageNumber(stage.intValue, out var entry)
            && entry.SceneName == _selectedScene.name;
        string path = _selectedScene != null ? AssetDatabase.GetAssetPath(_selectedScene) : string.Empty;
        bool inBuild = EditorBuildSettings.scenes.Any(item => item.enabled && item.path == path);
        if (!connected || !inBuild)
        {
            using (new EditorGUI.DisabledScope(_selectedScene == null))
            {
                if (GUILayout.Button("목적지 맵 연결 (맵 목록에 자동 등록)"))
                {
                    try
                    {
                        stage.intValue = TransitionPadSceneSetup.RegisterDestination(_selectedScene, catalog);
                        if (!connected) spawn.stringValue = string.Empty;
                        _lastStage = stage.intValue;
                        serializedObject.ApplyModifiedProperties();
                        _setupError = null;
                        GUIUtility.ExitGUI();
                    }
                    catch (ExitGUIException) { throw; }
                    catch (Exception exception) { _setupError = exception.Message; }
                }
            }
            EditorGUILayout.HelpBox(_selectedScene == null
                ? "목적지 맵이 아직 설정되지 않았습니다."
                : "연결 버튼을 누르면 SceneCatalog와 빌드 씬 목록에 이 맵을 등록합니다.", MessageType.Warning);
            if (!string.IsNullOrEmpty(_setupError)) EditorGUILayout.HelpBox(_setupError, MessageType.Error);
            return;
        }

        try
        {
            TransitionPadSceneSetup.SpawnOption[] points = TransitionPadSceneSetup.ReadSpawnPoints(path);
            if (points.GroupBy(point => point.Id).Any(group => group.Count() > 1))
                EditorGUILayout.HelpBox("목적지 맵에 같은 ID의 도착 지점이 여러 개 있습니다. 복제한 지점의 ID를 새로 만들어 주세요.", MessageType.Error);
            string[] ids = new[] { string.Empty }.Concat(points.Select(point => point.Id)).ToArray();
            string[] labels = new[] { "맵에 배치된 플레이어 시작 위치" }.Concat(points.Select(point => point.Label)).ToArray();
            int current = Array.IndexOf(ids, spawn.stringValue ?? string.Empty);
            // 사라진 지점을 다시 그리는 것만으로 기존 ID를 지우지 않습니다.
            if (current < 0)
            {
                ids = ids.Concat(new[] { spawn.stringValue }).ToArray();
                labels = labels.Concat(new[] { "찾을 수 없는 지점: " + spawn.stringValue }).ToArray();
                current = ids.Length - 1;
                EditorGUILayout.HelpBox("연결했던 도착 지점을 찾을 수 없습니다. 목록에서 다시 선택하세요.", MessageType.Error);
            }
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("도착 지점", current, labels);
            if (EditorGUI.EndChangeCheck()) spawn.stringValue = ids[selected];
            if (points.Length == 0)
                EditorGUILayout.HelpBox("지정된 도착 지점이 없습니다. 목적지 맵에서 Hierarchy 우클릭 → Project Ruin → 이동 발판 → 다른 맵에서 도착할 지점을 추가하고 맵을 저장하세요.", MessageType.Info);
        }
        catch (Exception exception)
        {
            EditorGUILayout.HelpBox("도착 지점 목록을 읽지 못했습니다: " + exception.Message, MessageType.Error);
        }
    }

    private void DrawTrigger()
    {
        BoxCollider2D collider = ((TransitionPadBase)target).GetComponent<BoxCollider2D>();
        if (collider == null) return;
        EditorGUILayout.Space();
        SerializedObject trigger = new SerializedObject(collider);
        EditorGUILayout.PropertyField(trigger.FindProperty("m_Size"), new GUIContent("이동 영역 크기"));
        SerializedProperty size = trigger.FindProperty("m_Size");
        Vector2 clamped = Vector2.Max(size.vector2Value, new Vector2(0.05f, 0.05f));
        if (clamped != size.vector2Value) size.vector2Value = clamped;
        trigger.ApplyModifiedProperties();
        if (!collider.enabled || !collider.isTrigger)
        {
            EditorGUILayout.HelpBox("이동 영역이 꺼져 있거나 트리거 설정이 해제되어 있습니다.", MessageType.Warning);
            if (GUILayout.Button("이동 영역 정상 설정으로 복구"))
            {
                Undo.RecordObject(collider, "이동 영역 복구");
                collider.enabled = true;
                collider.isTrigger = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }
        }
    }

    // 편집 모드에서도 Physics2D 동기화 없이 회전/크기가 적용된 영역을 검사합니다.
    public static bool Contains(BoxCollider2D collider, Vector3 worldPosition)
    {
        if (collider == null) return false;
        Vector2 local = collider.transform.InverseTransformPoint(worldPosition);
        Vector2 distance = local - collider.offset;
        return Mathf.Abs(distance.x) <= collider.size.x * 0.5f
            && Mathf.Abs(distance.y) <= collider.size.y * 0.5f;
    }

    private void OnSceneGUI()
    {
        if (EditorApplication.isPlaying || !(target is PositionTransitionPad)) return;
        Transform destination = new SerializedObject(target).FindProperty("_destination").objectReferenceValue as Transform;
        if (destination == null || destination.gameObject.scene != ((Component)target).gameObject.scene) return;
        EditorGUI.BeginChangeCheck();
        Vector3 position = Handles.PositionHandle(destination.position, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(destination, "도착 위치 이동");
        destination.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
    }
}

[CustomEditor(typeof(SpawnPoint))]
public sealed class TransitionSpawnPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SpawnPoint point = (SpawnPoint)target;
        EditorGUILayout.HelpBox("이 오브젝트를 플레이어가 도착할 곳으로 옮기고, Hierarchy에서 알아보기 쉬운 이름으로 바꾸세요.\n맵을 저장하면 다른 맵 이동 발판의 '도착 지점' 목록에 나타납니다.", MessageType.Info);
        SerializedProperty id = serializedObject.FindProperty("_spawnPointId");
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(id, new GUIContent("도착 지점 ID"));
        bool missing = string.IsNullOrWhiteSpace(id.stringValue);
        bool duplicate = point.gameObject.scene.IsValid() && point.gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<SpawnPoint>(true))
            .Any(other => other != point && other.SpawnPointId == id.stringValue);
        if (missing || duplicate)
            EditorGUILayout.HelpBox(missing ? "도착 지점 ID를 먼저 만들어 주세요." : "같은 맵에 ID가 중복되어 있습니다. 복제한 지점에서 새 ID를 만드세요.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || EditorUtility.IsPersistent(point)))
        {
            if (GUILayout.Button(missing ? "도착 지점 ID 만들기" : "새 ID 만들기 (복제한 지점에서 사용)"))
                id.stringValue = TransitionPadMapTools.NewSpawnId();
        }
        if (!missing)
            EditorGUILayout.HelpBox("이미 다른 발판에서 사용하는 지점의 ID를 바꾸면, 해당 발판의 도착 지점도 다시 선택해야 합니다.", MessageType.None);
        serializedObject.ApplyModifiedProperties();
    }
}
