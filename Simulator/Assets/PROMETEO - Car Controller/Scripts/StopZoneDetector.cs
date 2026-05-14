using UnityEngine;
using TMPro;
using System.Collections;

public class StopZoneDetector : MonoBehaviour
{
    public TrafficController trafficLight;
    public GameObject uiWarning; 
    
    private void Start()
    {
        if (uiWarning == null)
        {
            Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach(Canvas c in allCanvases)
            {
                if(c.gameObject.name == "StopWarning")
                {
                    uiWarning = c.gameObject;
                    break;
                }
            }
        }

        if (uiWarning != null) uiWarning.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckViolation(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Also check while staying, in case the light turns red while they are inside
        CheckViolation(other);
    }

    private void CheckViolation(Collider other)
    {
        // Use root or InParent to find the car controller
        PrometeoCarController player = other.GetComponentInParent<PrometeoCarController>();
        
        if (player != null)
        {
            if (trafficLight != null && trafficLight.IsRed())
            {
                // PENALTY SYSTEM CALL
                GameManager gm = Object.FindFirstObjectByType<GameManager>();
                if (gm != null)
                {
                    // This will handle the points AND the UI message automatically
                    gm.AddPenalty(20, "Red Light Violation");
                }
            }
        }
    }
}
