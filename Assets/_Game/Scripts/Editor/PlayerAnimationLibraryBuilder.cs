//=============================================================================
// File:        PlayerAnimationLibraryBuilder.cs
//
// Description: 플레이어 스프라이트 160개를 정렬해 재생 데이터 애셋을 만든다.
//
// History:     09/28/2026 + Created -- Codex
//=============================================================================

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>24개 폴더의 모든 플레이어 프레임을 공용 애니메이션 애셋으로 조립합니다.</summary>
public static class PlayerAnimationLibraryBuilder
{
    private const string SPRITE_FOLDER = "Assets/_Game/Sprites/PlayerSprite";
    private const string ANIMATION_FOLDER = "Assets/_Game/Animations";
    private const string LIBRARY_PATH = ANIMATION_FOLDER + "/PlayerAnimationLibrary.asset";

    // 순서는 PlayerFacing 열거형과 같습니다.
    private static readonly string[] IDLE_DIRECTIONS =
    {
        "Right", "DiagonalFrontRight", "Front", "DiagonalFrontLeft",
        "Left", "DiagonalBackLeft", "Back", "DiagonalBackRight"
    };

    private static readonly string[] MOVE_DIRECTIONS =
    {
        "right", "diagonal front right", "front", "diagonal front left",
        "left", "diagonal back left", "back", "diagonal back right"
    };

    [InitializeOnLoadMethod]
    private static void ScheduleInitialBuild()
    {
        PlayerAnimationLibrarySO library =
            AssetDatabase.LoadAssetAtPath<PlayerAnimationLibrarySO>(LIBRARY_PATH);
        if (library != null && library.IsComplete)
            return;

        EditorApplication.delayCall += BuildWhenReady;
    }

    private static void BuildWhenReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildWhenReady;
            return;
        }

        try
        {
            Build();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[PlayerAnimationLibraryBuilder] {exception.Message}");
        }
    }

    //=============================================================================
    // PlayerAnimationLibraryBuilder.Build
    //=============================================================================
    // Description: 각 방향의 모든 프레임을 검증하고 공용 애셋에 저장한다.
    // Parameters:  None.
    // Return:      void
    //=============================================================================
    /// <summary>플레이어 애니메이션 데이터 애셋을 생성하거나 갱신합니다.</summary>
    [MenuItem("Tools/Project Ruin/Player/Build Animation Library")]
    public static void Build()
    {
        Sprite[] idle = new Sprite[PlayerAnimationLibrarySO.DIRECTION_COUNT
            * PlayerAnimationLibrarySO.IDLE_FRAME_COUNT];
        Sprite[] walk = new Sprite[PlayerAnimationLibrarySO.DIRECTION_COUNT
            * PlayerAnimationLibrarySO.MOVE_FRAME_COUNT];
        Sprite[] run = new Sprite[PlayerAnimationLibrarySO.DIRECTION_COUNT
            * PlayerAnimationLibrarySO.MOVE_FRAME_COUNT];

        for (int direction = 0; direction < PlayerAnimationLibrarySO.DIRECTION_COUNT; direction++)
        {
            CopyFrames("Idle_" + IDLE_DIRECTIONS[direction],
                PlayerAnimationLibrarySO.IDLE_FRAME_COUNT, idle, direction);
            CopyFrames(MOVE_DIRECTIONS[direction] + " walk",
                PlayerAnimationLibrarySO.MOVE_FRAME_COUNT, walk, direction);
            CopyFrames(MOVE_DIRECTIONS[direction] + " run",
                PlayerAnimationLibrarySO.MOVE_FRAME_COUNT, run, direction);
        }

        if (!AssetDatabase.IsValidFolder(ANIMATION_FOLDER))
            AssetDatabase.CreateFolder("Assets/_Game", "Animations");

        PlayerAnimationLibrarySO library =
            AssetDatabase.LoadAssetAtPath<PlayerAnimationLibrarySO>(LIBRARY_PATH);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<PlayerAnimationLibrarySO>();
            AssetDatabase.CreateAsset(library, LIBRARY_PATH);
        }

        library.SetFrames(idle, walk, run);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerAnimationLibraryBuilder] 24개 애니메이션, 160개 프레임을 연결했습니다.");
    }

    private static void CopyFrames(string folderName, int expectedCount,
        Sprite[] destination, int direction)
    {
        string directory = Path.Combine(Application.dataPath,
            "_Game/Sprites/PlayerSprite", folderName);
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        string[] paths = Directory.GetFiles(directory, "*.png", SearchOption.TopDirectoryOnly);
        Array.Sort(paths, StringComparer.Ordinal);
        if (paths.Length != expectedCount)
            throw new InvalidOperationException(
                $"{folderName}: 예상 {expectedCount}개, 실제 {paths.Length}개입니다.");

        for (int frame = 0; frame < paths.Length; frame++)
        {
            string assetPath = SPRITE_FOLDER + "/" + folderName + "/"
                + Path.GetFileName(paths[frame]);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Single
                || importer.maxTextureSize != 1024)
            {
                throw new InvalidOperationException($"스프라이트 Import 설정이 올바르지 않습니다: {assetPath}");
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                throw new InvalidOperationException($"스프라이트를 불러올 수 없습니다: {assetPath}");

            destination[direction * expectedCount + frame] = sprite;
        }
    }
}
