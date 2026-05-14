using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    private GameManager gm;
    private float lastPenaltyTime = 0f;
    private float penaltyCooldown = 2f; // Don't spam penalties if we stay touching something

    void Start()
    {
        gm = Object.FindFirstObjectByType<GameManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gm != null && gm.isGameActive && Time.time > lastPenaltyTime + penaltyCooldown)
        {
            // 1. Check for Pedestrians (High Penalty)
            if (collision.gameObject.GetComponentInParent<PedestrianWalker>() != null)
            {
                gm.AddPenalty(100, "PEDESTRIAN COLLISION!");
                lastPenaltyTime = Time.time;
            }
            // 2. Check for Other Cars (Medium Penalty)
            else if (collision.gameObject.GetComponentInParent<SimpleTrafficCar>() != null)
            {
                gm.AddPenalty(50, "Car Collision!");
                lastPenaltyTime = Time.time;
            }
            // 3. General Obstacles (Small Penalty)
            else if (collision.relativeVelocity.magnitude > 3f)
            {
                gm.AddPenalty(20, "Obstacle Collision");
                lastPenaltyTime = Time.time;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gm != null && gm.isGameActive && Time.time > lastPenaltyTime + penaltyCooldown)
        {
            if (other.GetComponentInParent<PedestrianWalker>() != null)
            {
                gm.AddPenalty(100, "PEDESTRIAN COLLISION!");
                lastPenaltyTime = Time.time;
            }
            else if (other.GetComponentInParent<SimpleTrafficCar>() != null)
            {
                gm.AddPenalty(50, "Car Collision!");
                lastPenaltyTime = Time.time;
            }
        }
    }
}
