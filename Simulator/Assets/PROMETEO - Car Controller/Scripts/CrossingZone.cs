using UnityEngine;

/// <summary>
/// Place this script on an invisible trigger collider on the sidewalk at each end of a zebra crossing.
/// When a pedestrian enters this zone, it tells them they have finished crossing the road.
/// 
/// SETUP:
/// 1. Create an empty GameObject on the sidewalk at the END of the crossing.
/// 2. Add a Box Collider to it and check "Is Trigger".
/// 3. Size the collider to span the width of the sidewalk.
/// 4. Add this script to it.
/// 5. Tag the GameObject as "CrossingZone" (create this tag if it doesn't exist).
/// </summary>
public class CrossingZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PedestrianWalker pedestrian = other.GetComponent<PedestrianWalker>();
        if (pedestrian != null)
        {
            pedestrian.FinishCrossing();
        }
    }
}
