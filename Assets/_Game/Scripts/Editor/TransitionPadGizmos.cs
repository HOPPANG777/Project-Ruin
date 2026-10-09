using UnityEditor;
using UnityEngine;

public static class TransitionPadGizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
    private static void DrawPad(TransitionPadBase pad, GizmoType gizmoType)
    {
        if (!(pad is SceneTransitionPad) && !(pad is PositionTransitionPad)) return;
        Color color = pad is PositionTransitionPad ? new Color(0.15f, 0.65f, 1f) : new Color(0.7f, 0.35f, 1f);
        BoxCollider2D collider = pad.GetComponent<BoxCollider2D>();
        if (collider == null) return;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = pad.transform.localToWorldMatrix;
        Gizmos.color = new Color(color.r, color.g, color.b, 0.15f);
        Vector3 size = new Vector3(collider.size.x, collider.size.y, 0.01f);
        Gizmos.DrawCube(collider.offset, size);
        Gizmos.color = color;
        Gizmos.DrawWireCube(collider.offset, size);
        Gizmos.matrix = previous;
        Handles.Label(pad.transform.TransformPoint(collider.offset) + Vector3.up * 0.65f,
            pad is PositionTransitionPad ? "같은 맵 이동" : "다른 맵 이동");

        if (!(pad is PositionTransitionPad)) return;
        Transform destination = new SerializedObject(pad).FindProperty("_destination").objectReferenceValue as Transform;
        if (destination == null || destination.gameObject.scene != pad.gameObject.scene) return;
        Color previousColor = Handles.color;
        Handles.color = color;
        Handles.DrawDottedLine(pad.transform.position, destination.position, 5f);
        DrawDestination(destination.position, "도착 위치", color);
        Handles.color = previousColor;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
    private static void DrawSpawn(SpawnPoint spawn, GizmoType gizmoType)
    {
        DrawDestination(spawn.Position, "도착 지점: " + spawn.name, new Color(0.2f, 0.9f, 0.45f));
    }

    private static void DrawDestination(Vector3 position, string label, Color color)
    {
        Color previous = Handles.color;
        Handles.color = color;
        Handles.DrawWireDisc(position, Vector3.forward, 0.35f);
        Handles.DrawLine(position - Vector3.right * 0.5f, position + Vector3.right * 0.5f);
        Handles.DrawLine(position - Vector3.up * 0.5f, position + Vector3.up * 0.5f);
        Handles.Label(position + Vector3.up * 0.6f, label);
        Handles.color = previous;
    }
}
