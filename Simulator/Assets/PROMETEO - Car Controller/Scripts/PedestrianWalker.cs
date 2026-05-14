using UnityEngine;
using System.Collections;

public class PedestrianWalker : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float obstacleDetectionDistance = 1.0f; // How close to an obstacle before turning
    public GameObject accidentUI;   // Drag your "StopWarning" or a new UI here

    [Header("Traffic Light (Optional)")]
    [Tooltip("Drag the nearest TrafficLight here. Leave empty if this pedestrian is not at a crossing.")]
    public TrafficController nearestTrafficLight;

    private Animator anim;
    private float targetRotationY;
    private bool isWaitingAtLight = false;
    private bool isCrossing = false; // Once true, pedestrian will finish crossing before stopping

    void Start()
    {
        anim = GetComponent<Animator>();
        targetRotationY = transform.eulerAngles.y;
    }

    void Update()
    {
        // CHECK TRAFFIC LIGHT (with commit-to-crossing safety)
        if (nearestTrafficLight != null)
        {
            if (isCrossing)
            {
                // Currently crossing the road — ignore the light and keep walking
                // The CrossingZone trigger on the sidewalk will call FinishCrossing()
                isWaitingAtLight = false;
            }
            else if (nearestTrafficLight.IsRed())
            {
                // Red for cars = safe to cross — start crossing!
                isWaitingAtLight = false;
                isCrossing = true;
            }
            else
            {
                // Green for cars = dangerous, stop and wait on the sidewalk
                isWaitingAtLight = true;
            }
        }
        else
        {
            // No traffic light assigned, walk freely
            isWaitingAtLight = false;
        }

        // 1. ANIMATION
        if (anim != null)
        {
            anim.SetBool("isWalking", !isWaitingAtLight);
            anim.SetFloat("Speed", isWaitingAtLight ? 0f : walkSpeed);
        }

        // If waiting at a traffic light, skip movement and obstacle detection
        if (isWaitingAtLight) return;

        // 2. SMOOTH MOVEMENT
        transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime);

        // 3. SMOOTH ROTATION
        float currentRotationY = Mathf.LerpAngle(transform.eulerAngles.y, targetRotationY, Time.deltaTime * 5f);
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);

        // 4. SMOOTH GROUND SNAPPING
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 2f))
        {
            if (!groundHit.collider.isTrigger)
            {
                float targetY = groundHit.point.y;
                // Move towards the ground height smoothly
                float newY = Mathf.MoveTowards(transform.position.y, targetY, Time.deltaTime * 5f);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }

        // 5. OBSTACLE DETECTION (Gradual Turn)
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, obstacleDetectionDistance))
        {
            if (!hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            {
                // Instead of snapping 180, set a new target rotation
                targetRotationY += 180f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")))
        {
            Debug.Log("<color=red><b>ACCIDENT! DRIVE SAFELY!</b></color>");
            if (accidentUI != null)
            {
                StopAllCoroutines();
                StartCoroutine(ShowWarning());
            }
        }
    }

    IEnumerator ShowWarning()
    {
        accidentUI.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        accidentUI.SetActive(false);
    }

    // This stops the error from the Starter Assets animations
    public void OnFootstep(AnimationEvent animationEvent)
    {
        // We can add footstep sounds here later!
    }

    /// <summary>
    /// Called by the CrossingZone trigger when the pedestrian reaches the sidewalk.
    /// Resets the crossing state so the pedestrian can obey traffic lights again.
    /// </summary>
    public void FinishCrossing()
    {
        isCrossing = false;
    }
}
