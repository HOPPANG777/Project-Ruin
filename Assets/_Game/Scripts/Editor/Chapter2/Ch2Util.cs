//=============================================================================
// File:        Ch2Util.cs
// Description: 챕터2 퍼즐 배치 빌더가 공유하는 오브젝트 생성·직렬화 헬퍼입니다.
// History:     11/28/2026 + Created
//=============================================================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>배치 빌더 전용 헬퍼. 런타임 코드에서 사용하지 않습니다.</summary>
public static class Ch2Util
{
    /// <summary>챕터2 퍼즐 저장 스테이지 번호입니다. SceneCatalog에 Chapter2가 등록되면 같은 값으로 맞춥니다.</summary>
    public const int StageNumber = 4;
    public const string RootName = "Ch2_Puzzles";
    private const string SpritePath = "Assets/_Game/Sprites/Chapter2Layout/Ch2_Marker.png";

    /// <summary>검증용 배치 기록입니다. (키, 이름, x, y, 너비, 높이)</summary>
    public static readonly List<string> LayoutLog = new List<string>();

    /// <summary>1유닛 크기의 흰색 임시 스프라이트를 반환합니다. 없으면 생성합니다.</summary>
    public static Sprite MarkerSprite()
    {
        if (!File.Exists(SpritePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));
            Texture2D texture = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(SpritePath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 4f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    /// <summary>이름이 같은 첫 번째 자손을 깊이 우선으로 찾습니다.</summary>
    public static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>부모 아래에 빈 오브젝트를 만들고 로컬 좌표를 지정합니다.</summary>
    public static GameObject NewChild(Transform parent, string name, Vector2 position)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(position.x, position.y, 0f);
        return child;
    }

    /// <summary>색이 있는 임시 사각형입니다. solid이면 MazeBlocker 레이어의 충돌체를 붙입니다.</summary>
    public static GameObject Block(Transform parent, string name, Vector2 position, Vector2 size,
        Color color, bool solid, float rotationZ = 0f)
    {
        GameObject block = NewChild(parent, name, position);
        block.transform.localScale = new Vector3(size.x, size.y, 1f);
        block.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = MarkerSprite();
        renderer.color = color;
        renderer.sortingLayerName = "Object";
        renderer.sortingOrder = 1;
        if (solid)
        {
            block.AddComponent<BoxCollider2D>().size = Vector2.one;
            int layer = LayerMask.NameToLayer("MazeBlocker");
            if (layer >= 0) block.layer = layer;
        }
        Record(parent.name, name, position, size);
        return block;
    }

    /// <summary>배치 검증 이미지를 만들기 위한 기록입니다.</summary>
    public static void Record(string key, string name, Vector2 position, Vector2 size)
    {
        LayoutLog.Add($"{key}|{name}|{position.x:F2}|{position.y:F2}|{size.x:F2}|{size.y:F2}");
    }

    public static void SetString(Object target, string property, string value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(property).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetInt(Object target, string property, int value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(property).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetFloat(Object target, string property, float value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(property).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetBool(Object target, string property, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(property).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetRef(Object target, string property, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>오브젝트 참조 배열 전체를 교체합니다.</summary>
    public static void SetRefs(Object target, string property, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty(property);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>프리팹 에셋을 부모 아래 인스턴스로 배치합니다.</summary>
    public static GameObject Spawn(string assetPath, Transform parent, string name, Vector2 position)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        instance.name = name;
        instance.transform.localPosition = new Vector3(position.x, position.y, 0f);
        return instance;
    }
}
