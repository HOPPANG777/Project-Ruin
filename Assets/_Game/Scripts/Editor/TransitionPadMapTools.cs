using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 기획자는 Hierarchy 메뉴로 배치하거나 저장된 프리팹을 드래그하여 사용합니다.
public static class TransitionPadMapTools
{
    public const string Folder = "Assets/_Game/Prefabs/World";
    public const string ScenePadPath = Folder + "/SceneTransitionPad.prefab";
    public const string PositionPadPath = Folder + "/PositionTransitionPad.prefab";
    public const string SpawnPointPath = Folder + "/TransitionSpawnPoint.prefab";
    public const string DestinationName = "도착 위치 (이 오브젝트를 옮기세요)";

    [MenuItem("GameObject/Project Ruin/이동 발판/같은 맵 안에서 이동", false, 10)]
    private static void PlacePositionPad(MenuCommand command) => Place(PositionPadPath, command);

    [MenuItem("GameObject/Project Ruin/이동 발판/다른 맵으로 이동", false, 11)]
    private static void PlaceScenePad(MenuCommand command) => Place(ScenePadPath, command);

    [MenuItem("GameObject/Project Ruin/이동 발판/다른 맵에서 도착할 지점", false, 12)]
    private static void PlaceSpawnPoint(MenuCommand command)
    {
        GameObject instance = Place(SpawnPointPath, command);
        SerializedObject spawn = new SerializedObject(instance.GetComponent<SpawnPoint>());
        spawn.FindProperty("_spawnPointId").stringValue = NewSpawnId();
        spawn.ApplyModifiedProperties();
    }

    public static string NewSpawnId() => "SPAWN_" + Guid.NewGuid().ToString("N");

    private static GameObject Place(string path, MenuCommand command)
    {
        CreatePrefabs();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject parent = command.context as GameObject;
        Scene scene = parent != null ? parent.scene : SceneManager.GetActiveScene();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(instance, "이동 발판 배치");
        GameObjectUtility.SetParentAndAlign(instance, parent);
        Selection.activeGameObject = instance;
        return instance;
    }

    [MenuItem("Tools/Project Ruin/이동 발판/기본 프리팹 만들기")]
    public static void CreatePrefabs()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "World");

        // 기존 프리팹의 아트와 설정은 덮어쓰지 않습니다.
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PositionPadPath) == null)
            {
                GameObject root = CreatePad<PositionTransitionPad>(preview, "PositionTransitionPad",
                    new Color(0.15f, 0.65f, 1f, 0.65f));
                GameObject destination = new GameObject(DestinationName);
                destination.transform.SetParent(root.transform, false);
                destination.transform.localPosition = new Vector3(3f, 0f, 0f);
                SerializedObject pad = new SerializedObject(root.GetComponent<PositionTransitionPad>());
                pad.FindProperty("_destination").objectReferenceValue = destination.transform;
                pad.ApplyModifiedPropertiesWithoutUndo();
                Save(root, PositionPadPath);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ScenePadPath) == null)
                Save(CreatePad<SceneTransitionPad>(preview, "SceneTransitionPad",
                    new Color(0.7f, 0.35f, 1f, 0.65f)), ScenePadPath);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(SpawnPointPath) == null)
            {
                GameObject spawn = new GameObject("TransitionSpawnPoint", typeof(SpawnPoint));
                SceneManager.MoveGameObjectToScene(spawn, preview);
                Save(spawn, SpawnPointPath);
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static GameObject CreatePad<T>(Scene preview, string name, Color color)
        where T : TransitionPadBase
    {
        GameObject root = new GameObject(name, typeof(BoxCollider2D));
        SceneManager.MoveGameObjectToScene(root, preview);
        BoxCollider2D trigger = root.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;
        root.AddComponent<T>();
        // 목적지 설정을 Collider의 세부 항목보다 먼저 보여 줍니다.
        UnityEditorInternal.ComponentUtility.MoveComponentUp(root.GetComponent<T>());

        GameObject visual = new GameObject("발판 표시 (아트 교체 가능)", typeof(SpriteRenderer));
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (renderer.sprite == null)
            throw new InvalidOperationException("이동 발판에 사용할 기본 스프라이트를 찾을 수 없습니다.");
        Vector3 size = renderer.sprite.bounds.size;
        visual.transform.localScale = new Vector3(0.9f / size.x, 0.9f / size.y, 1f);
        renderer.color = color;
        renderer.sortingLayerName = "Object";
        renderer.sortingOrder = -10;
        return root;
    }

    private static void Save(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        if (!success) throw new InvalidOperationException("이동 발판 프리팹 저장 실패: " + path);
    }
}
