using UnityEngine;
using System.Collections.Generic;

public class SimpleTrafficCar : MonoBehaviour
{
    public GameObject pathObject;
    private TrafficPath path;
    public LayerMask roadLayer; 
    public float speed = 10f;
    public float rotationSpeed = 10f;
    public float stopDistance = 10f;
    public float waypointThreshold = 5f;
    
    [Header("Following Distance")]
    [Tooltip("Minimum distance to maintain behind another car. Cars will stop if closer than this.")]
    public float minFollowDistance = 5f;
    [Tooltip("Distance at which cars start slowing down for the vehicle ahead.")]
    public float slowDownDistance = 12f;
    
    [Header("Wheels")]
    public Transform[] wheels;
    public float wheelRotationMultiplier = 200f;
    public Vector3 wheelRotationAxis = new Vector3(1, 0, 0); // Default to X axis
    [Tooltip("Check this if the wheels orbit the car in a giant circle instead of spinning in place.")]
    public bool fixPivotIssues = true;

    [Header("Realism (Human-like Driving)")]
    public bool useRandomBraking = true;
    [Range(0.1f, 0.5f)]
    public float brakingIntensity = 0.2f; // How much they slow down
    public float variationSpeed = 0.5f;   // How fast the speed fluctuates
    
    
    
    private Vector3[] wheelVisualCenters;
    private int currentWaypointIndex = 0;

    private bool isStopped = false;
    private float currentSpeedMultiplier = 1f; // 1 = full speed, 0 = stopped
    private float noiseMultiplier = 1f;
    private float noiseOffset;
    private List<Transform> waypoints;
    private float randomSpeedOffset;

    [Header("Natural Spacing")]
    [Tooltip("Maximum random delay (in seconds) before this car starts driving. Creates natural gaps between cars.")]
    public float maxStartDelay = 5f;
    private float startDelayTimer;
    private bool hasStarted = false;

    void Start()
    {
        if (pathObject != null)
        {
            path = pathObject.GetComponent<TrafficPath>();
        }

        // Calculate visual centers for broken pivots
        if (wheels != null)
        {
            wheelVisualCenters = new Vector3[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                {
                    Renderer r = wheels[i].GetComponentInChildren<Renderer>();
                    if (r != null)
                    {
                        // Store the center of the mesh geometry in local space
                        wheelVisualCenters[i] = wheels[i].InverseTransformPoint(r.bounds.center);
                    }
                }
            }
        }

        randomSpeedOffset = Random.Range(-2f, 2f);
        noiseOffset = Random.Range(0f, 1000f); // Unique starting point for noise

        // Random start delay for natural spacing between cars
        startDelayTimer = Random.Range(0f, maxStartDelay);
        hasStarted = false;
        SetVisibility(false); // Hide car until it starts
        
        // Snap to road at start (Ignoring Triggers)
        RaycastHit hit;
        int startLayerMask = roadLayer.value & ~(1 << gameObject.layer);
        if (Physics.Raycast(transform.position + Vector3.up * 20f, Vector3.down, out hit, 100f, startLayerMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + Vector3.up * 0.2f;
        }

        if (path != null)
        {
            waypoints = path.GetWaypoints();
            FindNearestWaypoint(); 
        }
    }

    void FindNearestWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        float closestDistance = Mathf.Infinity;
        for (int i = 0; i < waypoints.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentWaypointIndex = i;
            }
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count == 0) 
        {
            if (path != null) waypoints = path.GetWaypoints();
            return;
        }

        // Wait for staggered start delay
        if (!hasStarted)
        {
            startDelayTimer -= Time.deltaTime;
            if (startDelayTimer <= 0f)
            {
                hasStarted = true;
                SetVisibility(true);
            }
            else
            {
                return; // Don't drive yet
            }
        }

        CheckForObstacles();
        CalculateRandomBraking();

        if (!isStopped)
        {
            DriveAlongPath();
        }
    }

    void SetVisibility(bool visible)
    {
        // Hide/show all renderers on this car
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }

    void CalculateRandomBraking()
    {
        if (!useRandomBraking)
        {
            noiseMultiplier = 1f;
            return;
        }

        // Use Perlin noise to get a smooth "wave" of values between 0 and 1
        float noise = Mathf.PerlinNoise(Time.time * variationSpeed, noiseOffset);
        
        // Map the noise to a range like [1.0 - intensity, 1.0]
        // This means the car will mostly drive at 100% but occasionally dip down
        noiseMultiplier = Mathf.Lerp(1f - brakingIntensity, 1f, noise);
    }

    bool IsObstacle(Collider col)
    {
        // 1. Check by tag (fastest)
        if (col.CompareTag("Player")) return true;
        
        // 2. Check for specific components
        if (col.GetComponentInParent<SimpleTrafficCar>() != null) return true;
        if (col.GetComponentInParent<PedestrianWalker>() != null) return true;
        
        // 3. Check Layer (e.g., if we added an "Obstacle" layer)
        // If it's not the road and not a trigger, and not us, it's likely an obstacle
        if (!col.isTrigger && ((1 << col.gameObject.layer) & roadLayer) == 0)
        {
            // If it's a static object like a wall or building
            return true;
        }
        
        return false;
    }

    void CheckForObstacles()
    {
        // --- Priority 0: Wide pedestrian detection (catches side-crossers) ---
        if (CheckForPedestrians()) return;

        RaycastHit hit;
        // Sight-line at 0.7m height, 0.5m forward to catch side-collisions better
        Vector3 rayStart = transform.position + (Vector3.up * 0.7f) + (transform.forward * 0.5f);
        
        // Wider spherecast (0.5f) for better peripheral vision
        if (Physics.SphereCast(rayStart, 0.5f, transform.forward, out hit, slowDownDistance))
        {
            // Ignore ourselves
            if (hit.collider.transform.root == transform.root) 
            {
                isStopped = false;
                currentSpeedMultiplier = 1f;
                return;
            }

            // --- Priority 1: Traffic Lights ---
            StopZoneDetector detector = hit.collider.GetComponentInParent<StopZoneDetector>();
            if (detector != null && detector.trafficLight != null)
            {
                if (detector.trafficLight.IsRed())
                {
                    if (hit.distance < 12.0f) // Slightly increased range
                    {
                        isStopped = true;
                        currentSpeedMultiplier = 0f;
                        return;
                    }
                }
                else
                {
                    isStopped = false;
                    currentSpeedMultiplier = 1f;
                    return;
                }
            }

            // --- Priority 2: Hill Fix (Ignore Road) ---
            // If we hit the road itself, ignore it so we don't brake for hills
            if (((1 << hit.collider.gameObject.layer) & roadLayer) != 0)
            {
                // It's a road, treat as clear path
                isStopped = false;
                currentSpeedMultiplier = 1f;
                return;
            }

            // --- Priority 3: Other vehicles/pedestrians ---
            if (IsObstacle(hit.collider))
            {
                float dist = hit.distance;
                
                if (dist < minFollowDistance)
                {
                    isStopped = true;
                    currentSpeedMultiplier = 0f;
                    return;
                }
                
                float t = Mathf.InverseLerp(minFollowDistance, slowDownDistance, dist);
                currentSpeedMultiplier = Mathf.Clamp(t, 0.5f, 1f); 
                isStopped = false;
                return;
            }
        }
        
        // Path is clear
        isStopped = false;
        currentSpeedMultiplier = 1f;
    }

    /// <summary>
    /// Checks for pedestrians in a wide area in front of the car.
    /// The original forward SphereCast was too narrow to catch pedestrians crossing from the side.
    /// This uses OverlapSphere to detect any pedestrian near the car's front.
    /// Returns true if a pedestrian was found and the car should stop/slow down.
    /// </summary>
    bool CheckForPedestrians()
    {
        float pedestrianDetectRadius = 6f;  // Wide radius to catch side-crossers
        float pedestrianDetectRange = 15f;  // How far ahead to check

        // Check a point ahead of the car
        Vector3 checkCenter = transform.position + transform.forward * (pedestrianDetectRange * 0.5f) + Vector3.up * 0.5f;
        
        Collider[] nearbyObjects = Physics.OverlapSphere(checkCenter, pedestrianDetectRadius);
        
        foreach (Collider col in nearbyObjects)
        {
            // Skip ourselves
            if (col.transform.root == transform.root) continue;
            
            PedestrianWalker pedestrian = col.GetComponentInParent<PedestrianWalker>();
            if (pedestrian != null)
            {
                float distToPedestrian = Vector3.Distance(transform.position, pedestrian.transform.position);
                
                if (distToPedestrian < minFollowDistance)
                {
                    // Very close — full stop!
                    isStopped = true;
                    currentSpeedMultiplier = 0f;
                    return true;
                }
                else if (distToPedestrian < pedestrianDetectRange)
                {
                    // Approaching — slow down
                    float t = Mathf.InverseLerp(minFollowDistance, pedestrianDetectRange, distToPedestrian);
                    currentSpeedMultiplier = Mathf.Clamp(t, 0.3f, 1f);
                    isStopped = false;
                    return true;
                }
            }
        }
        
        return false;
    }

    void DriveAlongPath()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        
        // --- New Smooth Height Logic ---
        // We create a bitmask that ignores the layer the car is currently on
        int layerMask = roadLayer.value & ~(1 << gameObject.layer);
        RaycastHit roadHit;
        
        // We cast a ray down to find the road, ignoring our own layer and TRIGGERS
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out roadHit, 5f, layerMask, QueryTriggerInteraction.Ignore))
        {
            float targetY = roadHit.point.y + 0.2f;
            // Instead of jumping to the height, we slide toward it smoothly
            float newY = Mathf.MoveTowards(transform.position.y, targetY, 10f * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        Vector3 targetPos = targetWaypoint.position;

        targetPos.y = transform.position.y;

        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }

        float currentSpeed = (speed + randomSpeedOffset) * currentSpeedMultiplier * noiseMultiplier;
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

        // Rotate wheels to simulate movement
        if (wheels != null)
        {
            float rotationAmount = currentSpeed * Time.deltaTime * wheelRotationMultiplier;
            for (int i = 0; i < wheels.Length; i++)
            {
                Transform wheel = wheels[i];
                if (wheel != null)
                {
                    if (fixPivotIssues)
                    {
                        // Rotate around the physical center of the mesh, ignoring the broken pivot
                        Vector3 worldCenter = wheel.TransformPoint(wheelVisualCenters[i]);
                        Vector3 worldAxis = wheel.TransformDirection(wheelRotationAxis);
                        wheel.RotateAround(worldCenter, worldAxis, rotationAmount);
                    }
                    else
                    {
                        // Standard rotation
                        wheel.Rotate(wheelRotationAxis, rotationAmount, Space.Self);
                    }
                }
            }
        }

        float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                                          new Vector3(targetPos.x, 0, targetPos.z));

        // Check if the waypoint is already behind us
        Vector3 toWaypoint = (targetPos - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toWaypoint);

        bool reached = false;

        // SMART SKIP: Only skip if it's behind us AND we are reasonably close to it.
        // This stops the car from jumping to a far-away point and driving through buildings.
        if (dot < 0f && distance < waypointThreshold * 2.0f) 
        {
            reached = true;
        }
        else if (distance < waypointThreshold)
        {
            reached = true;
        }
        // ---------------------------------------------------

        if (reached)
        {
            AdvanceWaypoint();
        }
    }

    void AdvanceWaypoint()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Count)
            currentWaypointIndex = 0;
    }
}
