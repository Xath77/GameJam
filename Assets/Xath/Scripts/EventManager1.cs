using System.Collections.Generic;
using UnityEngine;

public class EventManager1 : MonoBehaviour
{
    public Transform player;               // Reference to the player transform
    private BoxCollider zoneCollider;
    private List<BacklookerAI> registeredBacklookers = new List<BacklookerAI>();
    private bool isPlayerInZone = false;   // Tracks if the player is in the zone

    void Start()
    {
        // Get the box collider that defines the zone
        zoneCollider = GetComponent<BoxCollider>();

        if (zoneCollider == null)
        {
            Debug.LogError("EventManager1 requires a BoxCollider component!");
        }
        else
        {
            // Make sure the collider is a trigger
            zoneCollider.isTrigger = true;
        }

        if (player == null)
        {
            Debug.LogError("EventManager1 requires a player reference! Please assign it in the inspector.");
        }
    }

    void Update()
    {
        if (player == null || zoneCollider == null) return;

        // Check if player is in zone
        isPlayerInZone = IsPointInsideZone(player.position);

        // Check each registered Backlooker to see if it's in the zone
        foreach (BacklookerAI backlooker in registeredBacklookers)
        {
            if (backlooker != null)
            {
                // AI only works if BOTH player and AI are in the zone
                bool isAIInZone = IsPointInsideZone(backlooker.transform.position);
                bool shouldActivate = isPlayerInZone && isAIInZone;

                backlooker.SetInsideZone(shouldActivate);
            }
        }
    }

    // Register a backlooker AI with this manager
    public void RegisterBacklooker(BacklookerAI backlooker)
    {
        if (!registeredBacklookers.Contains(backlooker))
        {
            registeredBacklookers.Add(backlooker);

            // Set initial zone state
            if (backlooker != null && zoneCollider != null && player != null)
            {
                bool isAIInZone = IsPointInsideZone(backlooker.transform.position);
                bool isPlayerInZoneNow = IsPointInsideZone(player.position);
                bool shouldActivate = isPlayerInZoneNow && isAIInZone;

                backlooker.SetInsideZone(shouldActivate);
            }
        }
    }

    // Check if a point is inside the zone defined by the box collider
    private bool IsPointInsideZone(Vector3 point)
    {
        if (zoneCollider == null) return false;

        // Convert point to local space
        Vector3 localPoint = transform.InverseTransformPoint(point);

        // Get the collider's local bounds
        Vector3 halfSize = zoneCollider.size * 0.5f;

        // Check if the point is inside the bounds
        return (localPoint.x >= -halfSize.x && localPoint.x <= halfSize.x &&
                localPoint.y >= -halfSize.y && localPoint.y <= halfSize.y &&
                localPoint.z >= -halfSize.z && localPoint.z <= halfSize.z);
    }

    // Optional: Add OnDrawGizmos to visualize the zone in the editor
    void OnDrawGizmos()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Semi-transparent green
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            // Draw wireframe
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}