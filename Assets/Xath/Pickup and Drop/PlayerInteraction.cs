using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float raycastDistance = 3f;
    public KeyCode interactKey = KeyCode.E;
    public Transform cameraTransform;

    [Header("Debug")]
    public bool showDebugRay = true;

    // References
    private InventoryManager inventoryManager;
    private Item currentItem;
    private ItemChest currentChest;

    // UI Reference
    private InteractionUI interactionUI;

    private void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        interactionUI = FindObjectOfType<InteractionUI>();

        if (inventoryManager == null)
        {
            Debug.LogError("No InventoryManager found in the scene!");
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleItemDetection();
        HandleInteraction();
    }

    private void HandleItemDetection()
    {
        // Cast a ray from the camera forward to detect items
        RaycastHit hit;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.yellow);
        }

        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            // Check if we hit an item
            Item item = hit.collider.GetComponent<Item>();

            if (item != null)
            {
                currentItem = item;
                if (interactionUI != null)
                {
                    interactionUI.ShowPickupPrompt(true, "Press E to pick up " + item.itemName);
                    interactionUI.ShowDropPrompt(false);
                }
            }
            else
            {
                currentItem = null;
                if (interactionUI != null && currentChest == null)
                {
                    interactionUI.ShowPickupPrompt(false);
                }
            }
        }
        else
        {
            currentItem = null;
            if (interactionUI != null && currentChest == null)
            {
                interactionUI.ShowPickupPrompt(false);
            }
        }
    }

    private void HandleInteraction()
    {
        // Handle interaction input
        if (Input.GetKeyDown(interactKey))
        {
            // First priority: pick up an item if looking at one
            if (currentItem != null)
            {
                PickupItem(currentItem);
            }
            // Second priority: interact with chest if inside one
            else if (currentChest != null)
            {
                DropItem();
            }
        }
    }

    private void PickupItem(Item item)
    {
        if (inventoryManager != null)
        {
            inventoryManager.AddItem(item);
            item.OnPickup();

            if (interactionUI != null)
            {
                interactionUI.ShowPickupPrompt(false);
            }
        }
    }

    private void DropItem()
    {
        if (inventoryManager != null && inventoryManager.GetInventory().Count > 0)
        {
            // For simplicity, drop the last item picked up
            Item itemToDrop = inventoryManager.GetInventory()[inventoryManager.GetInventory().Count - 1];
            inventoryManager.RemoveItem(itemToDrop);

            if (currentChest != null)
            {
                currentChest.ReceiveItem(itemToDrop);
                inventoryManager.MarkItemAsDeposited(itemToDrop.itemID);
            }
        }
    }

    // Methods to handle chest interaction
    public void SetCurrentChest(ItemChest chest)
    {
        currentChest = chest;
        if (interactionUI != null)
        {
            interactionUI.ShowDropPrompt(true, "Press E to drop an item in the chest");
            interactionUI.ShowPickupPrompt(false);
        }
    }

    public void ClearCurrentChest()
    {
        currentChest = null;
        if (interactionUI != null)
        {
            interactionUI.ShowDropPrompt(false);
        }
    }
}