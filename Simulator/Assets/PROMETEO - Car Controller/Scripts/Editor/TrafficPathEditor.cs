using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(TrafficPath))]
public class TrafficPathEditor : Editor
{
    private bool showHandles = true;
    private float handleSize = 1.0f;
    private float snapHeight = 0.5f; // Height above road surface

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TrafficPath trafficPath = (TrafficPath)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Path Editor Tools", EditorStyles.boldLabel);

        // --- Snap Settings ---
        EditorGUILayout.BeginVertical("box");
        snapHeight = EditorGUILayout.FloatField("Snap Height Above Road", snapHeight);

        if (GUILayout.Button("Snap ALL Waypoints to Ground", GUILayout.Height(30)))
        {
            SnapAllWaypointsToGround(trafficPath, snapHeight);
        }

        if (GUILayout.Button("Snap Selected Waypoint to Ground", GUILayout.Height(25)))
        {
            SnapSelectedWaypointToGround(snapHeight);
        }
        EditorGUILayout.EndVertical();

        // --- Visibility Settings ---
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical("box");
        showHandles = EditorGUILayout.Toggle("Show Move Handles", showHandles);
        handleSize = EditorGUILayout.Slider("Handle Size", handleSize, 0.3f, 3.0f);
        EditorGUILayout.EndVertical();

        // --- Utility Buttons ---
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("Select All Waypoints", GUILayout.Height(25)))
        {
            SelectAllWaypoints(trafficPath);
        }

        if (GUILayout.Button("Add Waypoint at End", GUILayout.Height(25)))
        {
            AddWaypoint(trafficPath);
        }

        if (GUILayout.Button("Reverse Path Direction", GUILayout.Height(25)))
        {
            ReversePath(trafficPath);
        }

        EditorGUILayout.EndVertical();

        // --- Info ---
        EditorGUILayout.Space(5);
        int count = trafficPath.transform.childCount;
        EditorGUILayout.HelpBox($"This path has {count} waypoints.\nSelect the Path object and use the Scene view handles to move individual waypoints.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        if (!showHandles) return;

        TrafficPath trafficPath = (TrafficPath)target;

        for (int i = 0; i < trafficPath.transform.childCount; i++)
        {
            Transform child = trafficPath.transform.GetChild(i);

            // Draw a label with the waypoint index
            Handles.color = Color.white;
            Handles.Label(child.position + Vector3.up * 2f, $"WP {i}", EditorStyles.boldLabel);

            // Draw a move handle for each waypoint
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(child.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(child, "Move Waypoint");
                child.position = newPos;
                EditorUtility.SetDirty(child);
            }

            // Draw a visible disc at each waypoint
            Handles.color = new Color(0f, 1f, 1f, 0.3f); // Cyan, semi-transparent
            Handles.DrawSolidDisc(child.position, Vector3.up, handleSize);

            // Draw the outline
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(child.position, Vector3.up, handleSize);

            // Draw connection lines
            if (i < trafficPath.transform.childCount - 1)
            {
                Transform next = trafficPath.transform.GetChild(i + 1);
                Handles.color = Color.yellow;
                Handles.DrawLine(child.position, next.position, 2f);

                // Draw arrow at midpoint to show direction
                Vector3 midpoint = (child.position + next.position) / 2f;
                Vector3 direction = (next.position - child.position).normalized;
                Handles.color = Color.yellow;
                Handles.ArrowHandleCap(0, midpoint, Quaternion.LookRotation(direction), 1.5f, EventType.Repaint);
            }
            else
            {
                // Loop back to start
                Transform first = trafficPath.transform.GetChild(0);
                Handles.color = new Color(1f, 0.5f, 0f); // Orange for loop-back
                Handles.DrawDottedLine(child.position, first.position, 5f);
            }
        }
    }

    private void SnapAllWaypointsToGround(TrafficPath trafficPath, float height)
    {
        int snappedCount = 0;
        for (int i = 0; i < trafficPath.transform.childCount; i++)
        {
            Transform child = trafficPath.transform.GetChild(i);
            if (SnapToGround(child, height))
            {
                snappedCount++;
            }
        }

        EditorUtility.SetDirty(trafficPath);
        Debug.Log($"[TrafficPath] Snapped {snappedCount}/{trafficPath.transform.childCount} waypoints to ground.");
    }

    private void SnapSelectedWaypointToGround(float height)
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.transform.parent != null && go.transform.parent.GetComponent<TrafficPath>() != null)
            {
                Undo.RecordObject(go.transform, "Snap Waypoint to Ground");
                SnapToGround(go.transform, height);
                EditorUtility.SetDirty(go.transform);
            }
        }
    }

    private bool SnapToGround(Transform waypoint, float height)
    {
        Undo.RecordObject(waypoint, "Snap Waypoint to Ground");

        RaycastHit hit;
        // Cast from high above downward
        Vector3 origin = waypoint.position + Vector3.up * 100f;

        if (Physics.Raycast(origin, Vector3.down, out hit, 200f))
        {
            waypoint.position = new Vector3(waypoint.position.x, hit.point.y + height, waypoint.position.z);
            return true;
        }
        else
        {
            // If no ground found, try setting Y to 0
            Debug.LogWarning($"[TrafficPath] No ground found under waypoint '{waypoint.name}'. Setting Y to {height}.");
            waypoint.position = new Vector3(waypoint.position.x, height, waypoint.position.z);
            return false;
        }
    }

    private void SelectAllWaypoints(TrafficPath trafficPath)
    {
        List<GameObject> waypoints = new List<GameObject>();
        for (int i = 0; i < trafficPath.transform.childCount; i++)
        {
            waypoints.Add(trafficPath.transform.GetChild(i).gameObject);
        }
        Selection.objects = waypoints.ToArray();
    }

    private void AddWaypoint(TrafficPath trafficPath)
    {
        GameObject newWaypoint = new GameObject($"Waypoint ({trafficPath.transform.childCount})");
        Undo.RegisterCreatedObjectUndo(newWaypoint, "Add Waypoint");

        newWaypoint.transform.SetParent(trafficPath.transform);

        // Place it near the last waypoint, or at the path's position
        if (trafficPath.transform.childCount > 1)
        {
            Transform last = trafficPath.transform.GetChild(trafficPath.transform.childCount - 2);
            newWaypoint.transform.position = last.position + last.forward * 5f;
        }
        else
        {
            newWaypoint.transform.localPosition = Vector3.zero;
        }

        Selection.activeGameObject = newWaypoint;
        EditorUtility.SetDirty(trafficPath);
    }

    private void ReversePath(TrafficPath trafficPath)
    {
        int childCount = trafficPath.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            trafficPath.transform.GetChild(childCount - 1).SetSiblingIndex(i);
        }

        // Rename waypoints to match new order
        for (int i = 0; i < trafficPath.transform.childCount; i++)
        {
            Undo.RecordObject(trafficPath.transform.GetChild(i).gameObject, "Reverse Path");
            trafficPath.transform.GetChild(i).gameObject.name = $"Waypoint ({i})";
        }

        EditorUtility.SetDirty(trafficPath);
        Debug.Log("[TrafficPath] Path direction reversed.");
    }
}
