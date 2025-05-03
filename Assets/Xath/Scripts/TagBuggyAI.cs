using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class TagBuggyAI : MonoBehaviour
{
    public Transform player;                // Reference to the player's transform
    public float moveSpeed = 5.0f;          // How fast the bunny moves
    public float stoppingDistance = 0.5f;   // How close the bunny gets before stopping
    public float followHeight = 0.5f;       // Height offset from ground to position the bunny
    public LayerMask groundLayers;          // Layers that are considered "ground" for raycasting
    public float maxRaycastDistance = 50f;  // How far to raycast for ground detection

    private NavMeshAgent navAgent;          // Reference to the NavMeshAgent component
    private bool isInsideZone = false;      // Whether this AI is inside the activation zone
    private Vector3 targetPosition;         // Current position to move toward
    private bool isLookingAtGround = false; // Whether player is looking at ground
    private Vector3 groundHitPoint;         // The point on ground player is looking at

    // Animation variables
    private Animator animator;               // Reference to the animator
    private int isMovingHash;                // Hashed animator parameter for performance

    // Hopping variables
    public float hopHeight = 0.5f;           // How high the bunny hops
    public float hopSpeed = 8.0f;            // How fast the bunny hops 
    private bool isHopping = false;
    private Vector3 startPosition;
    private float hopProgress = 0f;

    // Audio variables
    public AudioClip[] hopSounds;           // Array of hopping sound effects
    private AudioSource audioSource;        // Reference to the AudioSource component

    // Debug variables
    public bool showDebugRays = true;

    void Start()
    {
        // Get required components
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();

        // Add AudioSource component if not present
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // Make sound 3D
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 20.0f;
            audioSource.volume = 0.7f;
            audioSource.pitch = Random.Range(0.9f, 1.1f); // Slightly randomize pitch for variety
        }

        // Cache the animator parameter hash for improved performance
        if (animator != null)
        {
            isMovingHash = Animator.StringToHash("isMoving");
        }
        else
        {
            Debug.LogWarning("Animator component not found on bunny! Animations won't work.");
        }

        // Set up NavMeshAgent
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.updateRotation = true;

            // Important: Let the AI handle vertical movement for hopping
            navAgent.updatePosition = false;
        }
        else
        {
            Debug.LogError("NavMeshAgent component not found on bunny! Please add one.");
        }

        // Initialize the target position to current position
        targetPosition = transform.position;

        // Register with event manager
        EventManager2 eventManager = FindFirstObjectByType<EventManager2>();
        if (eventManager != null)
        {
            eventManager.RegisterTagBuggy(this);
        }
        else
        {
            Debug.LogWarning("EventManager2 not found in scene. TagBuggyAI will work without zone restrictions.");
            isInsideZone = true; // Default to active if no manager found
        }

        // Start hopping coroutine
        StartCoroutine(HopCoroutine());
    }

    void Update()
    {
        if (player == null || navAgent == null) return;

        // Only process AI logic if inside the activation zone
        if (!isInsideZone)
        {
            // Make sure the agent is stopped when outside zone
            navAgent.isStopped = true;
            UpdateAnimation(false);
            return;
        }

        // Check if player is looking at the ground
        CheckIfLookingAtGround();

        // Determine target position based on player's gaze
        DetermineTargetPosition();

        // Move towards target position
        MoveTowardsTarget();

        // Draw debug information
        if (showDebugRays)
        {
            DrawDebugInfo();
        }

        // Update NavMesh agent's position (since we've set updatePosition to false)
        if (!isHopping && !navAgent.isStopped)
        {
            transform.position = navAgent.nextPosition;
        }
    }

    void CheckIfLookingAtGround()
    {
        // Get the camera
        Camera playerCamera = GetPlayerCamera();
        if (playerCamera == null) return;

        // Cast a ray from the camera center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Check if ray hits ground
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, groundLayers))
        {
            isLookingAtGround = true;
            groundHitPoint = hit.point;

            // Adjust hit point slightly above ground
            groundHitPoint.y += followHeight;

            // Debug visualization of hit point
            if (showDebugRays)
            {
                Debug.DrawLine(ray.origin, hit.point, Color.green);
                Debug.DrawRay(hit.point, Vector3.up * followHeight, Color.cyan);
            }
        }
        else
        {
            isLookingAtGround = false;

            // Debug visualization of ray miss
            if (showDebugRays)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.red);
            }
        }
    }

    void DetermineTargetPosition()
    {
        // Get the event manager to check zone boundaries
        EventManager2 eventManager = FindFirstObjectByType<EventManager2>();

        if (isLookingAtGround)
        {
            // First check if the ground hit point is inside the zone
            bool isHitPointInZone = true; // Default to true

            if (eventManager != null)
            {
                isHitPointInZone = eventManager.IsPointInsideZone(groundHitPoint);
            }

            if (isHitPointInZone)
            {
                // Move to where player is looking on ground (inside zone)
                targetPosition = groundHitPoint;
            }
            else
            {
                // Point is outside zone, use default follow behavior instead
                Vector3 defaultPos = player.position + (player.right * 1.5f) - (player.forward * 2f);
                defaultPos.y = Mathf.Max(defaultPos.y, NavMesh.SamplePosition(defaultPos, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position.y + followHeight : defaultPos.y);

                // Check if default position is in zone
                bool isDefaultPosInZone = true;
                if (eventManager != null)
                {
                    isDefaultPosInZone = eventManager.IsPointInsideZone(defaultPos);
                }

                if (isDefaultPosInZone)
                {
                    targetPosition = defaultPos;
                }
                else
                {
                    // Stay where we are since both target positions are outside the zone
                    targetPosition = transform.position;
                }

                // Debug visualization for out-of-zone hit point
                if (showDebugRays)
                {
                    Debug.DrawLine(groundHitPoint, groundHitPoint + Vector3.up * 2f, Color.red);
                }
            }
        }
        else
        {
            // Default follow behavior - behind and to the side of player
            Vector3 defaultPos = player.position + (player.right * 1.5f) - (player.forward * 2f);
            defaultPos.y = Mathf.Max(defaultPos.y, NavMesh.SamplePosition(defaultPos, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position.y + followHeight : defaultPos.y);

            // Check if the position is inside the zone
            bool isDefaultPosInZone = true;
            if (eventManager != null)
            {
                isDefaultPosInZone = eventManager.IsPointInsideZone(defaultPos);
            }

            if (isDefaultPosInZone)
            {
                targetPosition = defaultPos;
            }
            else
            {
                // Stay put or find a point inside the zone
                targetPosition = transform.position;
            }
        }
    }

    void MoveTowardsTarget()
    {
        // Resume navigation
        navAgent.isStopped = false;

        // Set destination to target position
        navAgent.SetDestination(targetPosition);

        // Update animation based on whether we're actually moving
        float speed = navAgent.velocity.magnitude;
        bool isMoving = speed > 0.1f;

        // Update animator
        UpdateAnimation(isMoving);

        // Debug visualization of movement target
        if (showDebugRays)
        {
            Debug.DrawLine(transform.position, targetPosition, Color.yellow);
        }
    }

    // Helper method to update the animator
    void UpdateAnimation(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool(isMovingHash, isMoving);
        }
    }

    // Coroutine for hopping animation
    IEnumerator HopCoroutine()
    {
        while (true)
        {
            // Only hop if we're moving and inside zone
            if (!navAgent.isStopped && navAgent.velocity.magnitude > 0.5f && isInsideZone)
            {
                // Start a hop
                startPosition = transform.position;
                isHopping = true;
                hopProgress = 0f;

                // Play hop sound at the start of the hop
                PlayRandomHopSound();

                // Animate the hop
                while (hopProgress < 1.0f)
                {
                    hopProgress += Time.deltaTime * hopSpeed;

                    // Calculate hop trajectory using a sine curve
                    float verticalOffset = Mathf.Sin(hopProgress * Mathf.PI) * hopHeight;

                    // Apply it to the actual position
                    Vector3 hopPosition = navAgent.nextPosition;
                    hopPosition.y += verticalOffset;
                    transform.position = hopPosition;

                    yield return null;
                }

                // Play landing sound at the end of the hop
                if (hopProgress >= 1.0f)
                {
                    PlayRandomHopSound(true); // Pass true to indicate this is a landing sound
                }

                isHopping = false;

                // Wait briefly between hops
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                // If not moving, make sure we're at the NavMesh position
                if (!isHopping)
                {
                    transform.position = navAgent.nextPosition;
                }

                // Check again after a short delay
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    // Play a random hop sound effect
    void PlayRandomHopSound(bool isLanding = false)
    {
        if (audioSource == null || hopSounds == null || hopSounds.Length == 0) return;

        // Select a random sound from the array
        int randomIndex = Random.Range(0, hopSounds.Length);
        AudioClip soundToPlay = hopSounds[randomIndex];

        if (soundToPlay != null)
        {
            // Adjust pitch and volume based on whether it's a takeoff or landing sound
            if (isLanding)
            {
                audioSource.pitch = Random.Range(0.7f, 0.9f); // Lower pitch for landing
                audioSource.volume = 0.8f;
            }
            else
            {
                audioSource.pitch = Random.Range(0.9f, 1.2f); // Higher pitch for takeoff
                audioSource.volume = 0.6f;
            }

            // Play the sound
            audioSource.PlayOneShot(soundToPlay);
        }
    }

    void DrawDebugInfo()
    {
        // Draw a line to show the bunny's path
        if (navAgent.hasPath)
        {
            DrawPath(navAgent.path);
        }

        // Draw current state indicator
        if (isLookingAtGround)
        {
            // Draw a sphere at the ground hit point
            Debug.DrawLine(transform.position, groundHitPoint, Color.green);
            DebugDrawCircle(groundHitPoint, 0.5f, Color.cyan);
        }
        else
        {
            // Draw a line to the default target
            Debug.DrawLine(transform.position, targetPosition, Color.blue);
        }
    }

    // Helper method to get player camera
    Camera GetPlayerCamera()
    {
        // Try to find camera in player's children
        Camera cam = player.GetComponentInChildren<Camera>();

        // If not found, try to get main camera
        if (cam == null)
        {
            cam = Camera.main;
        }

        return cam;
    }

    // Helper method to draw the NavMesh path
    void DrawPath(NavMeshPath path)
    {
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.blue);
        }
    }

    // Helper method to draw a circle for debug visualization
    void DebugDrawCircle(Vector3 center, float radius, Color color)
    {
        int segments = 36;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            Debug.DrawLine(point1, point2, color);
        }
    }

    // Public method to set zone status - called by EventManager2
    public void SetInsideZone(bool inside)
    {
        isInsideZone = inside;

        // Optional: Stop the agent if it's leaving the zone
        if (!inside && navAgent != null)
        {
            navAgent.isStopped = true;
        }
    }
}