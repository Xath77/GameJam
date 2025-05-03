using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EventManager2 : MonoBehaviour
{
    public Transform player;               // Reference to the player transform
    private BoxCollider zoneCollider;
    private List<TagBuggyAI> registeredTagBuggies = new List<TagBuggyAI>();
    private bool isPlayerInZone = false;   // Tracks if the player is in the zone

    void Start()
    {
        // Get the box collider that defines the zone
        zoneCollider = GetComponent<BoxCollider>();

        if (zoneCollider == null)
        {
            Debug.LogError("EventManager2 requires a BoxCollider component!");
        }
        else
        {
            // Make sure the collider is a trigger
            zoneCollider.isTrigger = true;
        }

        if (player == null)
        {
            Debug.LogError("EventManager2 requires a player reference! Please assign it in the inspector.");
        }
    }

    void Update()
    {
        if (player == null || zoneCollider == null) return;

        // Check if player is in zone
        isPlayerInZone = IsPointInsideZone(player.position);

        // Check each registered TagBuggy to see if it's in the zone
        foreach (TagBuggyAI buggy in registeredTagBuggies)
        {
            if (buggy != null)
            {
                // AI only works if BOTH player and AI are in the zone
                bool isAIInZone = IsPointInsideZone(buggy.transform.position);
                bool shouldActivate = isPlayerInZone && isAIInZone;

                buggy.SetInsideZone(shouldActivate);
            }
        }
    }

    // Register a TagBuggy AI with this manager
    public void RegisterTagBuggy(TagBuggyAI buggy)
    {
        if (!registeredTagBuggies.Contains(buggy))
        {
            registeredTagBuggies.Add(buggy);

            // Set initial zone state
            if (buggy != null && zoneCollider != null && player != null)
            {
                bool isAIInZone = IsPointInsideZone(buggy.transform.position);
                bool isPlayerInZoneNow = IsPointInsideZone(player.position);
                bool shouldActivate = isPlayerInZoneNow && isAIInZone;

                buggy.SetInsideZone(shouldActivate);
            }
        }
    }

    // Check if a point is inside the zone defined by the box collider
    public bool IsPointInsideZone(Vector3 point)
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
            Gizmos.color = new Color(0, 0.5f, 1, 0.3f); // Semi-transparent blue
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            // Draw wireframe
            Gizmos.color = new Color(0, 0.5f, 1, 1); // Blue
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}