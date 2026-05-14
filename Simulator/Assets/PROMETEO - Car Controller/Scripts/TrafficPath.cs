using UnityEngine;
using System.Collections.Generic;

public class TrafficPath : MonoBehaviour
{
    public bool snapToGround = true;
    public LayerMask roadLayer; 

    private List<Transform> cachedWaypoints;

    public List<Transform> GetWaypoints()
    {
        if (cachedWaypoints != null && cachedWaypoints.Count > 0 && !Application.isEditor)
        {
            return cachedWaypoints;
        }

        List<Transform> points = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (snapToGround && Application.isPlaying)
            {
                RaycastHit hit;
                if (Physics.Raycast(child.position + Vector3.up * 20f, Vector3.down, out hit, 100f, roadLayer))
                {
                    child.position = hit.point + Vector3.up * 0.5f;
                }
            }
            points.Add(child);
        }

        // Waypoints follow their natural hierarchy order (child order in Unity)
        cachedWaypoints = points;
        return points;
    }

    private void OnDrawGizmos()
    {
        // Get sorted waypoints for the gizmo display
        List<Transform> waypoints = GetWaypoints();

        if (waypoints.Count < 2) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            // Color-code: green for start, red for last, cyan for others
            if (i == 0)
                Gizmos.color = Color.green;
            else if (i == waypoints.Count - 1)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.cyan;

            Gizmos.DrawSphere(waypoints[i].position, 1.5f);
            Gizmos.DrawWireSphere(waypoints[i].position, 1.8f);

            // Draw connection lines
            Gizmos.color = Color.yellow;
            if (i < waypoints.Count - 1)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
            else
            {
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange for loop-back
                Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
            }
        }
    }
}
