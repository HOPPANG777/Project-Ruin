using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

//=============================================================================
// File:        SimplePuzzlePrefabBuilder.cs
//
// Description: ALL 씬의 검증된 UI를 공용 프리팹으로 묶고 맵 배치용 퍼즐
//              상호작용 프리팹 세 종류를 생성한다.
//
//=============================================================================
public static class SimplePuzzlePrefabBuilder
{
    private const string AUTO_BUILD_SESSION_KEY =
        "ProjectRuin.SimplePuzzlePrefabBuilder.AutoBuildScheduled";

    [InitializeOnLoadMethod]
    private static void ScheduleInitialBuild()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(UI_ROOT_PREFAB_PATH) != null
            || SessionState.GetBool(AUTO_BUILD_SESSION_KEY, false))
        {
            return;
        }

        SessionState.SetBool(AUTO_BUILD_SESSION_KEY, true);
        EditorApplication.delayCall += RunInitialBuild;
    }

    private static void RunInitialBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetBool(AUTO_BUILD_SESSION_KEY, false);
            return;
        }

        try
        {
            BuildSimplePuzzlePrefabs();
        }
        catch (Exception exception)
        {
            SessionState.SetBool(AUTO_BUILD_SESSION_KEY, false);
            Debug.LogException(exception);
        }
    }

    private const string SOURCE_SCENE_PATH = "Assets/_Game/Scenes/All.unity";
    private const string PREFAB_FOLDER = "Assets/_Game/Prefabs/Puzzle";
    private const string UI_ROOT_PREFAB_PATH =
        PREFAB_FOLDER + "/PuzzleUiRoot.prefab";
    private const string GAME_ROOT_PREFAB_PATH =
        "Assets/_Game/Prefabs/GameRoot.prefab";

    [MenuItem("Tools/Project Ruin/Puzzles/Build Simple Puzzle Prefabs")]
    public static void BuildSimplePuzzlePrefabs()
    {
        EnsureFolder("Assets/_Game/Prefabs", "Puzzle");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(UI_ROOT_PREFAB_PATH) == null)
        {
            BuildPuzzleUiRootPrefab();
        }
        BuildInteractablePrefab(
            "PipePuzzleInteractable",
            PuzzleType.PUZZLE_TYPE_PIPE,
            "PIPE_STAGE_01");
        BuildInteractablePrefab(
            "LightPuzzleInteractable",
            PuzzleType.PUZZLE_TYPE_LIGHT,
            "LIGHT_STAGE_01");
        BuildInteractablePrefab(
            "CirclePuzzleInteractable",
            PuzzleType.PUZZLE_TYPE_CIRCLE,
            "CIRCLE_STAGE_01");
        InstallPuzzleUiRootIntoGameRoot();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        UnityEngine.Object createdPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(UI_ROOT_PREFAB_PATH);
        Selection.activeObject = createdPrefab;
        EditorGUIUtility.PingObject(createdPrefab);

        Debug.Log(
            "[SimplePuzzlePrefabBuilder] PuzzleUiRoot과 세 종류의 맵 상호작용 프리팹을 생성했습니다.");
    }

    private static void BuildPuzzleUiRootPrefab()
    {
        Scene previewScene = EditorSceneManager.OpenPreviewScene(SOURCE_SCENE_PATH);

        try
        {
            GameObject sourceCanvas = RequireGameObject(previewScene, "PuzzleCanvas");
            UiPuzzlePopup pipePopup =
                RequireComponent<UiPuzzlePopup>(previewScene, "PipePuzzle_Popup");
            UiPuzzlePopup lightPopup =
                RequireComponent<UiPuzzlePopup>(previewScene, "LightPuzzle_Popup");
            UiPuzzlePopup circlePopup =
                RequireComponent<UiPuzzlePopup>(previewScene, "CirclePuzzle_Popup");

            GameObject root = new GameObject("PuzzleUiRoot");
            SceneManager.MoveGameObjectToScene(root, previewScene);

            sourceCanvas.transform.SetParent(root.transform, false);
            ReparentPuzzleManager(pipePopup, root.transform);
            ReparentPuzzleManager(lightPopup, root.transform);
            ReparentPuzzleManager(circlePopup, root.transform);

            pipePopup.ClosePopup();
            lightPopup.ClosePopup();
            circlePopup.ClosePopup();

            PuzzleUiRoot puzzleUiRoot = root.AddComponent<PuzzleUiRoot>();
            GameObject fallbackEventSystem = CreateFallbackEventSystem(root.transform);
            ConfigurePuzzleUiRoot(
                puzzleUiRoot,
                fallbackEventSystem,
                pipePopup,
                lightPopup,
                circlePopup);

            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, UI_ROOT_PREFAB_PATH, out success);
            if (!success)
            {
                throw new InvalidOperationException(
                    $"퍼즐 UI 프리팹 저장에 실패했습니다. path={UI_ROOT_PREFAB_PATH}");
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static void BuildInteractablePrefab(
        string prefabName,
        PuzzleType puzzleType,
        string puzzleId)
    {
        GameObject root = new GameObject(prefabName);
        try
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer < 0)
            {
                throw new InvalidOperationException(
                    "ProjectSettings/TagManager에 Interactable 레이어가 없습니다.");
            }

            root.layer = interactableLayer;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            UiPuzzleInteractable interactable =
                root.AddComponent<UiPuzzleInteractable>();
            SerializedObject serializedInteractable =
                new SerializedObject(interactable);
            serializedInteractable.FindProperty("_puzzleType").intValue =
                (int)puzzleType;
            serializedInteractable.FindProperty("_puzzleId").stringValue =
                puzzleId;
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PREFAB_FOLDER}/{prefabName}.prefab";
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, path, out success);
            if (!success)
            {
                throw new InvalidOperationException(
                    $"상호작용 프리팹 저장에 실패했습니다. path={path}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void InstallPuzzleUiRootIntoGameRoot()
    {
        GameObject puzzleUiPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(UI_ROOT_PREFAB_PATH);
        if (puzzleUiPrefab == null)
        {
            throw new InvalidOperationException(
                $"생성된 PuzzleUiRoot 프리팹을 찾을 수 없습니다. path={UI_ROOT_PREFAB_PATH}");
        }

        GameObject gameRoot = PrefabUtility.LoadPrefabContents(GAME_ROOT_PREFAB_PATH);
        try
        {
            PuzzleUiRoot existing =
                gameRoot.GetComponentInChildren<PuzzleUiRoot>(true);
            if (existing == null)
            {
                GameObject nestedUiRoot = (GameObject)PrefabUtility.InstantiatePrefab(
                    puzzleUiPrefab,
                    gameRoot.transform);
                nestedUiRoot.name = "PuzzleUiRoot";
                nestedUiRoot.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                nestedUiRoot.transform.localScale = Vector3.one;
            }

            PrefabUtility.SaveAsPrefabAsset(gameRoot, GAME_ROOT_PREFAB_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(gameRoot);
        }
    }

    private static GameObject CreateFallbackEventSystem(Transform parent)
    {
        GameObject eventSystemObject = new GameObject(
            "FallbackEventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject.transform.SetParent(parent, false);

        InputSystemUIInputModule inputModule =
            eventSystemObject.GetComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();

        return eventSystemObject;
    }

    private static void ConfigurePuzzleUiRoot(
        PuzzleUiRoot puzzleUiRoot,
        GameObject fallbackEventSystem,
        UiPuzzlePopup pipePopup,
        UiPuzzlePopup lightPopup,
        UiPuzzlePopup circlePopup)
    {
        SerializedObject serializedRoot = new SerializedObject(puzzleUiRoot);
        SerializedProperty bindings =
            serializedRoot.FindProperty("_puzzlePopups");
        bindings.arraySize = 3;

        ConfigureBinding(
            bindings.GetArrayElementAtIndex(0),
            PuzzleType.PUZZLE_TYPE_PIPE,
            pipePopup);
        ConfigureBinding(
            bindings.GetArrayElementAtIndex(1),
            PuzzleType.PUZZLE_TYPE_LIGHT,
            lightPopup);
        ConfigureBinding(
            bindings.GetArrayElementAtIndex(2),
            PuzzleType.PUZZLE_TYPE_CIRCLE,
            circlePopup);

        serializedRoot.FindProperty("_fallbackEventSystem").objectReferenceValue =
            fallbackEventSystem;
        serializedRoot.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBinding(
        SerializedProperty binding,
        PuzzleType puzzleType,
        UiPuzzlePopup popup)
    {
        binding.FindPropertyRelative("_puzzleType").intValue = (int)puzzleType;
        binding.FindPropertyRelative("_puzzlePopup").objectReferenceValue = popup;
    }

    private static void ReparentPuzzleManager(
        UiPuzzlePopup popup,
        Transform parent)
    {
        if (popup.PuzzleManager == null)
        {
            throw new InvalidOperationException(
                $"팝업의 Puzzle Manager가 비어 있습니다. popup={popup.name}");
        }

        popup.PuzzleManager.transform.SetParent(parent, false);
    }

    private static GameObject RequireGameObject(Scene scene, string objectName)
    {
        GameObject gameObject = FindGameObject(scene, objectName);
        if (gameObject == null)
        {
            throw new InvalidOperationException(
                $"ALL 씬에서 오브젝트를 찾을 수 없습니다. name={objectName}");
        }

        return gameObject;
    }

    private static T RequireComponent<T>(Scene scene, string objectName)
        where T : Component
    {
        GameObject gameObject = RequireGameObject(scene, objectName);
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException(
                $"필요한 컴포넌트가 없습니다. object={objectName}, component={typeof(T).Name}");
        }

        return component;
    }

    private static GameObject FindGameObject(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < transforms.Length; j++)
            {
                if (transforms[j].name == objectName)
                {
                    return transforms[j].gameObject;
                }
            }
        }

        return null;
    }

    private static void EnsureFolder(string parentFolder, string folderName)
    {
        string path = $"{parentFolder}/{folderName}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }
}
