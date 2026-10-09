//=============================================================================
// File: BeamPrefabBuilder.cs
// Description: 씬을 바꾸지 않고 개별 Beam 배치 부품과 임시 아트를 만든다.
// History: 10/06/2026 + Created -- Codex
//=============================================================================
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>기존 돌·벽 프리팹 Variant와 독립 Beam 부품을 Preview 씬에서 만듭니다.</summary>
public static class BeamPrefabBuilder
{
    /// <summary>모든 배치 부품을 제공하는 폴더입니다.</summary>
    public const string Folder = "Assets/_Game/Prefabs/Puzzle/Beam";
    private const string ART = Folder + "/Art";
    private const string STONE_SOURCE = "Assets/_Game/Prefabs/Maze/MazePushableStone.prefab";
    private const string WALL_SOURCE = "Assets/_Game/Prefabs/Maze/MazeWall.prefab";
    private static readonly string[] PREFAB_NAMES = {
        "BeamPuzzleController", "BeamStartPillar", "BeamTwoWayPillar", "BeamFourWayPillar",
        "BeamEightWayPillar", "BeamEndPillar", "BeamResetPad", "BeamLine",
        "BeamFixedObstacle", "BeamExitObstacle", "BeamPushableStone", "BeamBreakableWall"
    };
    private static readonly string[] DISPLAY_NAMES = {
        "Beam_관리자", "Beam_시작 기둥", "Beam_2방향 기둥", "Beam_4방향 기둥",
        "Beam_8방향 기둥", "Beam_끝 기둥", "Beam_초기화 발판", "Beam_광선 표시",
        "Beam_고정 장애물", "Beam_출구 장애물", "Beam_밀 수 있는 돌", "Beam_부술 수 있는 벽"
    };

    /// <summary>기존 영문 부품 키에 대응하는 에셋 경로입니다. 초기 에셋의 경로도 갱신 전까지 지원합니다.</summary>
    public static string GetPrefabPath(string partName)
    {
        int index = Array.IndexOf(PREFAB_NAMES, partName);
        if (index < 0) throw new ArgumentException("알 수 없는 Beam 부품: " + partName, nameof(partName));
        string path = Folder + "/" + DISPLAY_NAMES[index] + ".prefab";
        string legacyPath = Folder + "/" + partName + ".prefab";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && AssetDatabase.LoadAssetAtPath<GameObject>(legacyPath) != null ? legacyPath : path;
    }

    /// <summary>제공 부품의 파일·표시 이름과 컴포넌트 순서를 갱신합니다. GUID와 씬 부품 연결은 유지합니다.</summary>
    public static void UpdateDesignerPresentation()
    {
        for (int i = 0; i < PREFAB_NAMES.Length; i++)
        {
            string path = GetPrefabPath(PREFAB_NAMES[i]);
            if (Missing(PREFAB_NAMES[i])) continue;
            string designerPath = Folder + "/" + DISPLAY_NAMES[i] + ".prefab";
            if (path != designerPath)
            {
                string error = AssetDatabase.MoveAsset(path, designerPath);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                path = designerPath;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyDesignerPresentation(root, PREFAB_NAMES[i]);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success) throw new InvalidOperationException("Beam 부품 표시 갱신 실패: " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[BeamPrefabBuilder] 개별 부품 12개 기획용 표시 갱신 완료");
    }

    /// <summary>기존 결과를 덮어쓰지 않고 누락된 부품만 만듭니다. 기존 맵은 저장하지 않습니다.</summary>
    [MenuItem("Tools/Project Ruin/Beam/개별 부품 프리팹 만들기")]
    public static void CreatePrefabs()
    {
        Directory.CreateDirectory(ART);
        AssetDatabase.Refresh();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ART + "/BeamUnlit.mat");
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("프로젝트 URP 2D Unlit Shader를 찾을 수 없습니다.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, ART + "/BeamUnlit.mat");
        }
        Sprite arrow = CreateSprite("Arrow", 5);
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            if (Missing("BeamLine"))
            {
                GameObject root = Root(preview, "BeamLine");
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = line.endWidth = 0.07f;
                line.startColor = line.endColor = new Color(1f, 0.85f, 0.2f);
                line.sortingLayerName = "Object";
                line.sortingOrder = 5;
                line.enabled = false;
                Save(root);
            }
            for (int type = 0; type < 5; type++)
            {
                string name = new[] { "BeamStartPillar", "BeamTwoWayPillar", "BeamFourWayPillar", "BeamEightWayPillar", "BeamEndPillar" }[type];
                if (!Missing(name)) continue;
                GameObject root = Root(preview, name);
                root.layer = 9;
                Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Static;
                CircleCollider2D body = root.AddComponent<CircleCollider2D>();
                body.radius = 0.4f;
                GameObject detection = new GameObject("InteractionTrigger", typeof(CircleCollider2D));
                detection.transform.SetParent(root.transform, false);
                detection.layer = 8;
                CircleCollider2D trigger = detection.GetComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = 0.85f;
                GameObject direction = new GameObject("DirectionView");
                direction.transform.SetParent(root.transform, false);
                SpriteRenderer visual = Visual(direction.transform, "BodySprite", CreateSprite(name, type), material);
                BeamPillarView pillar = root.AddComponent<BeamPillarView>();
                float[][] outputs = { new[] { 0f }, new[] { 0f }, new[] { 0f, 90f, 180f },
                    new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f }, new float[0] };
                pillar.Configure(new BeamPillarSettings(type == 0 ? "START" : type == 4 ? "END" : "SET_PILLAR_ID",
                    (BeamPillarType)type, type > 0 && type < 4, 0f, 45f, outputs[type]));
                SerializedObject settings = new SerializedObject(pillar);
                settings.FindProperty("_bodyCollider").objectReferenceValue = body;
                settings.FindProperty("_receiverCollider").objectReferenceValue = body;
                settings.FindProperty("_interactionTrigger").objectReferenceValue = trigger;
                settings.FindProperty("_bodyRenderer").objectReferenceValue = visual;
                settings.FindProperty("_directionRoot").objectReferenceValue = direction.transform;
                SerializedProperty arrows = settings.FindProperty("_outputArrows");
                arrows.arraySize = outputs[type].Length;
                for (int i = 0; i < outputs[type].Length; i++)
                {
                    SpriteRenderer sprite = Visual(direction.transform, "Output_" + i, arrow, material);
                    sprite.sortingOrder = 12;
                    arrows.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
                }
                settings.ApplyModifiedPropertiesWithoutUndo();
                pillar.ResetState();
                Save(root);
            }
            if (Missing("BeamResetPad"))
            {
                GameObject root = Root(preview, "BeamResetPad");
                root.layer = 2;
                BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                BeamResetPad pad = root.AddComponent<BeamResetPad>();
                SetReference(pad, "_trigger", trigger);
                Visual(root.transform, "PadSprite", CreateSprite("ResetPad", 6), material);
                Save(root);
            }
            CreateWallVariant(preview, "BeamFixedObstacle", material, false);
            CreateWallVariant(preview, "BeamExitObstacle", material, false);
            CreateWallVariant(preview, "BeamBreakableWall", material, true);
            if (Missing("BeamPushableStone"))
            {
                GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(STONE_SOURCE), preview);
                root.name = "BeamPushableStone";
                var stone = new SerializedObject(root.GetComponent<PushableStone>());
                stone.FindProperty("_obstacleLayer").intValue = (1 << 8) | (1 << 9) | (1 << 10);
                stone.ApplyModifiedPropertiesWithoutUndo();
                Save(root);
            }
            if (Missing("BeamPuzzleController"))
            {
                GameObject root = Root(preview, "BeamPuzzleController");
                BeamPuzzleController controller = root.AddComponent<BeamPuzzleController>();
                SetReference(controller, "_beamPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath("BeamLine")).GetComponent<LineRenderer>());
                var settings = new SerializedObject(controller);
                settings.FindProperty("_objectId").stringValue = "SET_UNIQUE_BEAM_SAVE_ID";
                settings.ApplyModifiedPropertiesWithoutUndo();
                Save(root);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        AssetDatabase.SaveAssets();
        Debug.Log("[BeamPrefabBuilder] 개별 Beam 부품 12개 생성/확인 완료: " + Folder);
    }

    private static void CreateWallVariant(Scene preview, string name, Material material, bool breakable)
    {
        if (!Missing(name)) return;
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WALL_SOURCE), preview);
        root.name = name;
        SpriteRenderer sprite = root.GetComponentInChildren<SpriteRenderer>();
        sprite.sprite = CreateSprite(name, breakable ? 8 : 7);
        sprite.sharedMaterial = material;
        sprite.transform.localScale = Vector3.one;
        sprite.color = Color.white;
        if (breakable)
        {
            root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            GameObject detection = new GameObject("InteractionTrigger", typeof(CircleCollider2D));
            detection.transform.SetParent(root.transform, false);
            detection.layer = 8;
            CircleCollider2D trigger = detection.GetComponent<CircleCollider2D>();
            trigger.radius = 0.85f;
            trigger.isTrigger = true;
            BreakableWall wall = root.AddComponent<BreakableWall>();
            var settings = new SerializedObject(wall);
            settings.FindProperty("_objectId").stringValue = "SET_WALL_ID";
            settings.FindProperty("_wallRenderer").objectReferenceValue = sprite;
            settings.FindProperty("_interactionTrigger").objectReferenceValue = trigger;
            settings.FindProperty("_requiredSkill").objectReferenceValue = CreateBreakSkill();
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        Save(root);
    }

    private static SkillDataSO CreateBreakSkill()
    {
        string path = Folder + "/BeamBreakSkill.asset";
        SkillDataSO skill = AssetDatabase.LoadAssetAtPath<SkillDataSO>(path);
        if (skill != null) return skill;
        skill = ScriptableObject.CreateInstance<SkillDataSO>();
        var settings = new SerializedObject(skill);
        settings.FindProperty("_skillId").stringValue = "BEAM_BREAK_WALL";
        settings.FindProperty("_displayNameKey").stringValue = "skill.beam_break.name";
        settings.FindProperty("_descriptionKey").stringValue = "skill.beam_break.description";
        settings.FindProperty("_interactionPromptKey").stringValue = "skill.beam_break.prompt";
        settings.FindProperty("_skillType").enumValueIndex = (int)SkillType.SKILL_BREAK_OBSTACLE;
        settings.FindProperty("_skillActionState").enumValueIndex = (int)SkillActionState.SKILL_ACTION_PRESS;
        settings.FindProperty("_icon").objectReferenceValue = CreateSprite("BeamBreakableWall", 8);
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(skill, path);
        return skill;
    }

    private static Sprite CreateSprite(string name, int kind)
    {
        string path = ART + "/" + name + ".png";
        if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float px = (x + 0.5f - size / 2f) / 64f;
            float py = (y + 0.5f - size / 2f) / 64f;
            float r = Mathf.Sqrt(px * px + py * py);
            bool fill;
            Color color = Color.white;
            if (kind == 5) fill = px >= 0.32f && px < 0.86f && (Mathf.Abs(py) < 0.035f || (px > 0.64f && Mathf.Abs(py) < (0.86f - px) * 0.65f));
            else if (kind == 6) { fill = Mathf.Abs(px) < 0.46f && Mathf.Abs(py) < 0.46f; color = Mathf.Abs(px) < 0.27f && Mathf.Abs(py) < 0.27f ? new Color(1f, 0.7f, 0.15f) : new Color(0.35f, 0.22f, 0.08f); }
            else if (kind >= 7) { fill = Mathf.Abs(px) < 0.48f && Mathf.Abs(py) < 0.48f; color = kind == 8 && Mathf.Abs(px - py * 0.4f) < 0.025f ? Color.black : new Color(0.55f, 0.35f, 0.32f); }
            else
            {
                fill = kind == 2 ? Mathf.Abs(px) + Mathf.Abs(py) < 0.55f : kind == 3 ? Mathf.Max(Mathf.Abs(px), Mathf.Abs(py)) < 0.42f && Mathf.Abs(px) + Mathf.Abs(py) < 0.63f : r < 0.43f;
                if (kind == 0 && r < 0.22f) color = new Color(0.2f, 0.8f, 1f);
                if (kind == 4 && (r < 0.12f || (r > 0.22f && r < 0.29f))) color = new Color(1f, 0.25f, 0.3f);
                if (kind == 1 && Mathf.Abs(py) < 0.045f && Mathf.Abs(px) < 0.23f) color = Color.black;
                if (kind == 2 && (Mathf.Abs(px) < 0.035f || Mathf.Abs(py) < 0.035f)) color = Color.black;
                if (kind == 3 && (Mathf.Abs(px) < 0.025f || Mathf.Abs(py) < 0.025f || Mathf.Abs(Mathf.Abs(px) - Mathf.Abs(py)) < 0.025f)) color = Color.black;
            }
            pixels[y * size + x] = fill ? color : Color.clear;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64f;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject Root(Scene preview, string name)
    {
        var root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, preview);
        return root;
    }
    private static SpriteRenderer Visual(Transform parent, string name, Sprite sprite, Material material)
    {
        var child = new GameObject(name, typeof(SpriteRenderer));
        child.transform.SetParent(parent, false);
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Object";
        renderer.sortingOrder = 10;
        return renderer;
    }
    private static void SetReference(UnityEngine.Object component, string field, UnityEngine.Object value)
    {
        var settings = new SerializedObject(component);
        settings.FindProperty(field).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
    private static bool Missing(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(name)) == null;
    private static void Save(GameObject root)
    {
        string assetName = root.name;
        ApplyDesignerPresentation(root, assetName);
        PrefabUtility.SaveAsPrefabAsset(root, GetPrefabPath(assetName), out bool success);
        if (!success) throw new InvalidOperationException("Beam 부품 프리팹 저장 실패: " + assetName);
    }

    private static void ApplyDesignerPresentation(GameObject root, string assetName)
    {
        int index = Array.IndexOf(PREFAB_NAMES, assetName);
        if (index < 0) return;
        if (root.name == assetName) root.name = DISPLAY_NAMES[index];
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 1; i < children.Length; i++)
        {
            Transform child = children[i];
            switch (child.name)
            {
                case "DirectionView": child.name = "방향 표시"; break;
                case "BodySprite": child.name = "몸체 그림"; break;
                case "InteractionTrigger": child.name = "접근 감지 범위"; break;
                case "PadSprite": child.name = "발판 그림"; break;
                case "View": child.name = "몸체 표시"; break;
                case "MazeMotif": child.name = "장식 표시"; break;
                case "Sprite":
                    child.name = child.parent.name == "장식 표시" ? "장식 그림" :
                        assetName == "BeamPushableStone" ? "돌 그림" :
                        assetName == "BeamBreakableWall" ? "벽 그림" :
                        assetName == "BeamExitObstacle" ? "출구 그림" : "장애물 그림";
                    break;
                default:
                    if (child.name.StartsWith("Output_", StringComparison.Ordinal) && int.TryParse(child.name.Substring(7), out int output))
                        child.name = "출력 화살표 " + (output + 1);
                    break;
            }
        }
        // 기획용 설정을 물리 컴포넌트보다 먼저 보이게 합니다. Variant에서 허용하지 않는 순서는 유지합니다.
        MonoBehaviour settings = root.GetComponent<BeamPillarView>();
        if (settings == null) settings = root.GetComponent<BeamResetPad>();
        if (settings == null) settings = root.GetComponent<PushableStone>();
        if (settings == null) settings = root.GetComponent<BreakableWall>();
        if (settings == null) settings = root.GetComponent<BeamPuzzleController>();
        if (settings == null) return;
        while (UnityEditorInternal.ComponentUtility.MoveComponentUp(settings)) { }
    }
}
