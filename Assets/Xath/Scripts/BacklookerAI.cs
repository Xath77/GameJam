using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class BacklookerAI : MonoBehaviour
{
    public GameObject JumpScare;
    public Transform player;                // Reference to the player's transform
    public float moveSpeed = 3.5f;          // How fast the enemy moves
    public float jumpScareDistance = 2.0f;  // Distance at which the enemy triggers a jumpscare
    public float fieldOfViewAngle = 90f;    // Player's field of view angle in degrees
    public float stoppingDistance = 1.0f;   // How close the enemy gets before stopping

    private bool isInPlayerView = false;
    private Vector3 lastKnownPlayerPosition;
    private NavMeshAgent navAgent;          // Reference to the NavMeshAgent component
    private bool isInsideZone = false;      // Whether this AI is inside the activation zone

    // Animation variables
    private Animator animator;               // Reference to the animator
    private int isMovingHash;                // Hashed animator parameter for performance
    public float animationDampTime = 0.1f;   // Smoothing for animation transitions

    // Vision detection variables
    [SerializeField] private LayerMask visionLayers = -1;  // Default to all layers
    [SerializeField] private float viewCheckFrequency = 0.1f;  // How often to check player view (seconds)
    private float viewCheckTimer = 0;
    private Transform playerCamera;  // Reference to the actual camera

    // Debug variables
    public bool showDebugRays = true;

    [Tooltip("All footstep sfx to be randomized while moving.")]
    [SerializeField] private AudioClip[] m_footstepSFX = new AudioClip[] { };

    [Tooltip("How fast footstep sfx will be played while walking?")]
    [SerializeField] private float m_footstepsRate = 0.5f;

    [Tooltip("How fast footstep sfx will be played while running?")]
    [SerializeField] private float m_footstepsRunRate = 0.3f;

    

    // Private class members.
    private AudioSource m_footstepSource = null;
    private float m_lastFootstep = 0.0f;
    void Awake()
    {
        // Try to find the player camera
        if (player != null)
        {
            // Look for camera in player's children
            playerCamera = player.GetComponentInChildren<Camera>()?.transform;

            // If not found as child, try to find main camera
            if (playerCamera == null)
            {
                playerCamera = Camera.main?.transform;
            }

            // Log warning if still not found
            if (playerCamera == null)
            {
                Debug.LogWarning("BacklookerAI: Could not find player camera. Using player transform instead.");
                playerCamera = player;
            }
            else
            {
                Debug.Log("BacklookerAI: Using camera " + playerCamera.name + " for view detection");
            }
        }

        m_footstepSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Get required components
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();

        // Cache the animator parameter hash for improved performance
        if (animator != null)
        {
            isMovingHash = Animator.StringToHash("isMoving");
        }
        else
        {
            Debug.LogWarning("Animator component not found on enemy! Animations won't work.");
        }

        // Set up NavMeshAgent
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.updateRotation = true;
        }
        else
        {
            Debug.LogError("NavMeshAgent component not found on enemy! Please add one.");
        }

        // Initialize the last known position
        if (player != null)
        {
            lastKnownPlayerPosition = player.position;
        }

        // Register with event manager
        EventManager1 eventManager = FindFirstObjectByType<EventManager1>();
        if (eventManager != null)
        {
            eventManager.RegisterBacklooker(this);
        }
        else
        {
            Debug.LogWarning("EventManager1 not found in scene. BacklookerAI will work without zone restrictions.");
            isInsideZone = true; // Default to active if no manager found
        }
    }

    void Update()
    {
        PlayFootstepSFX();

        if (player == null || navAgent == null) return;

        // Only process AI logic if inside the activation zone
        if (!isInsideZone)
        {
            // Make sure the agent is stopped when outside zone
            navAgent.isStopped = true;

            // Set animation to idle when outside zone
            UpdateAnimation(false);
            return;
        }

        // Check if the player is looking at the enemy
        CheckIfPlayerIsLooking();

        // Move the enemy if the player isn't looking
        if (!isInPlayerView)
        {
            MoveTowardsPlayer();
        }
        else
        {
            // Stop moving when being watched - CRITICAL: this line must work
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero; // Force velocity to zero

            // Set animation to idle when being watched
            UpdateAnimation(false);

            // Debug stop confirmation
            Debug.DrawRay(transform.position, Vector3.up * 5f, Color.red);
        }

        // Check for jumpscare
        CheckForJumpscare();

        // Draw debug information
        if (showDebugRays)
        {
            DrawDebugInfo();
        }
    }

    void CheckIfPlayerIsLooking()
    {
        // Throttle check frequency to avoid performance hit
        viewCheckTimer += Time.deltaTime;
        if (viewCheckTimer < viewCheckFrequency) return;
        viewCheckTimer = 0;

        // Use player's camera forward if available, otherwise use player transform
        Transform viewSource = playerCamera != null ? playerCamera : player;

        if (viewSource == null) return;

        // Get direction from camera to enemy center
        Vector3 directionToEnemy = transform.position - viewSource.position;
        float distance = directionToEnemy.magnitude;
        directionToEnemy.Normalize();

        // Calculate angle between camera's forward and direction to enemy
        float angle = Vector3.Angle(viewSource.forward, directionToEnemy);

        // Debug visualization
        Debug.DrawRay(viewSource.position, viewSource.forward * 5f, Color.blue, viewCheckFrequency);
        Debug.DrawRay(viewSource.position, directionToEnemy * 5f, Color.yellow, viewCheckFrequency);

        // Check if enemy is in field of view
        if (angle < fieldOfViewAngle * 0.5f)
        {
            // Cast a ray to check for obstacles
            RaycastHit hit;
            if (Physics.Raycast(viewSource.position, directionToEnemy, out hit, distance * 1.1f, visionLayers))
            {
                // Check if the ray hit this enemy or any of its children
                if (hit.transform == transform || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform))
                {
                    SetPlayerIsLooking(true);
                    return;
                }
            }
        }

        // Player is not looking at the enemy
        SetPlayerIsLooking(false);
    }

    // Helper method to set looking state and handle debugging
    void SetPlayerIsLooking(bool looking)
    {
        isInPlayerView = looking;

        if (looking)
        {
            // Make sure agent is stopped right away
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
            }

            Debug.DrawRay(transform.position, Vector3.up * 3f, Color.red, viewCheckFrequency);
        }
        else
        {
            lastKnownPlayerPosition = player.position;
            Debug.DrawRay(transform.position, Vector3.up * 3f, Color.green, viewCheckFrequency);
        }
    }

    void MoveTowardsPlayer()
    {
        // Double-check we're not being looked at
        if (isInPlayerView)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            UpdateAnimation(false);
            return;
        }

        // Resume navigation
        navAgent.isStopped = false;

        // Set destination to player's last known position
        navAgent.SetDestination(lastKnownPlayerPosition);

        // Update animation based on whether we're actually moving
        // Only play walk animation if we're actually moving
        float speed = navAgent.velocity.magnitude;
        UpdateAnimation(speed > 0.1f);

        // Debug movement confirmation
        Debug.DrawRay(transform.position, Vector3.up * 5f, Color.green);
    }

    // Helper method to update the animator
    void UpdateAnimation(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool(isMovingHash, isMoving);

            if (isMoving == false)
            {
                animator.speed = 0f;
            }
            else
                animator.speed = 1f;
        }
    }

    void CheckForJumpscare()
    {
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // If close enough, trigger jumpscare
        if (distanceToPlayer <= jumpScareDistance)
        {
            TriggerJumpscare();
        }
    }

    void TriggerJumpscare()
    {
        // Debug jumpscare - replace with your actual jumpscare implementation
        Debug.Log("JUMPSCARE TRIGGERED!");

        JumpScare.SetActive(true);

        // You can add additional jumpscare elements here
        // For example:
        // - Play a sound
        // - Flash the screen
        // - Shake the camera
        // - Trigger a game event
    }

    void DrawDebugInfo()
    {
        // Draw a line to show the enemy's path
        if (navAgent.hasPath)
        {
            DrawPath(navAgent.path);
        }

        // Draw a ray to show if enemy is being watched
        if (isInPlayerView)
        {
            Debug.DrawLine(player.position, transform.position, Color.red);
        }
        else
        {
            Debug.DrawLine(player.position, transform.position, Color.green);
        }

        // Draw jumpscare radius
        DebugDrawCircle(transform.position, jumpScareDistance, Color.red);
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

    // Public method to set zone status - called by EventManager1
    public void SetInsideZone(bool inside)
    {
        isInsideZone = inside;

        // Optional: Stop the agent if it's leaving the zone
        if (!inside && navAgent != null)
        {
            navAgent.isStopped = true;
        }
    }

    private void PlayFootstepSFX()
        {
            if (navAgent.isStopped == false)
            {
                if (Time.time > m_lastFootstep + m_footstepsRate)
                {
                    m_lastFootstep = Time.time;
                    m_footstepSource.PlayOneShot(m_footstepSFX[Random.Range(0, m_footstepSFX.Length)]);
                }
            }
            // else if (m_isRunning)
            // {
            //     if (Time.time > m_lastFootstep + m_footstepsRunRate)
            //     {
            //         m_lastFootstep = Time.time;
            //         m_footstepSource.PlayOneShot(m_footstepSFX[Random.Range(0, m_footstepSFX.Length)]);
            //     }
            // }
        }
}