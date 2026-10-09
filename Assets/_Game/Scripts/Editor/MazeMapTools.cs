using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 맵에서 직접 배치하거나 칠할 수 있는 미로 벽을 생성합니다.
public static class MazeMapTools
{
    [MenuItem("GameObject/Project Ruin/Maze/Wall Tilemap", false, 10)]
    private static void CreateWallTilemap(MenuCommand command)
    {
        GameObject parent = command.context as GameObject;
        Grid grid = parent != null ? parent.GetComponentInParent<Grid>() : null;
        if (grid == null)
        {
            GameObject gridObject = new GameObject("MazeGrid", typeof(Grid));
            Undo.RegisterCreatedObjectUndo(gridObject, "Create Maze Grid");
            GameObjectUtility.SetParentAndAlign(gridObject, parent);
            grid = gridObject.GetComponent<Grid>();
        }

        GameObject walls = new GameObject("MazeWalls", typeof(Tilemap),
            typeof(TilemapRenderer), typeof(TilemapCollider2D));
        Undo.RegisterCreatedObjectUndo(walls, "Create Maze Wall Tilemap");
        GameObjectUtility.SetParentAndAlign(walls, grid.gameObject);
        Configure(walls.GetComponent<Tilemap>());
        Selection.activeGameObject = walls;
    }

    [MenuItem("Tools/Project Ruin/Maze/Configure Selected Wall Tilemap")]
    private static void ConfigureSelectedWallTilemap()
    {
        Configure(Selection.activeGameObject.GetComponent<Tilemap>());
    }

    [MenuItem("Tools/Project Ruin/Maze/Configure Selected Wall Tilemap", true)]
    private static bool CanConfigureSelectedWallTilemap()
    {
        return Selection.activeGameObject != null
            && Selection.activeGameObject.GetComponent<Tilemap>() != null;
    }

    private static void Configure(Tilemap tilemap)
    {
        int layer = LayerMask.NameToLayer("MazeBlocker");
        if (layer < 0)
        {
            Debug.LogError("MazeBlocker 레이어를 먼저 추가해 주세요.", tilemap);
            return;
        }

        Undo.RecordObject(tilemap.gameObject, "Set Maze Wall Layer");
        tilemap.gameObject.layer = layer;
        TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider == null) collider = Undo.AddComponent<TilemapCollider2D>(tilemap.gameObject);
        Undo.RecordObject(collider, "Configure Maze Wall Collider");
        collider.enabled = true;
        collider.isTrigger = false;
        collider.compositeOperation = Collider2D.CompositeOperation.None;
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Set Maze Wall Sorting Layer");
            renderer.sortingLayerName = "Object";
        }
        EditorUtility.SetDirty(tilemap.gameObject);
        Debug.Log("미로 벽 설정 완료. Tile Palette에서 벽을 칠하세요. 사용하는 Tile의 Collider Type은 Grid 또는 Sprite여야 합니다.", tilemap);
    }
}
