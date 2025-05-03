using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public List<Image> itemSlotIcons; // References to the three item slot images
    public List<Image> itemCompletionTicks; // References to tick marks that appear over icons

    [Header("Visual Settings")]
    public float inactiveAlpha = 0.2f;
    public float activeAlpha = 1.0f;
    public float fadeSpeed = 5f;

    private InventoryManager inventoryManager;
    private Dictionary<int, int> itemIDToSlotMap = new Dictionary<int, int>();

    // Keep track of target alpha values
    private float[] targetAlphas;

    // Keep track of which items have been deposited
    private HashSet<int> depositedItems = new HashSet<int>();
    int count = 0;
    [SerializeField] private GameObject phase2init;
    [SerializeField] private GameObject phase3init;
    [SerializeField] private GameObject phase3destroy;


    private void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("No InventoryManager found in the scene!");
            return;
        }

        // Subscribe to inventory changes
        inventoryManager.onInventoryChanged += UpdateInventoryUI;
        inventoryManager.onItemDeposited += MarkItemAsDeposited;

        // Set up the slot mapping (assuming 3 fixed items with IDs 1, 2, 3)
        itemIDToSlotMap.Add(1, 0); // Item ID 1 goes to slot 0
        itemIDToSlotMap.Add(2, 1); // Item ID 2 goes to slot 1
        itemIDToSlotMap.Add(3, 2); // Item ID 3 goes to slot 2

        // Initialize target alphas
        targetAlphas = new float[itemSlotIcons.Count];
        for (int i = 0; i < targetAlphas.Length; i++)
        {
            targetAlphas[i] = inactiveAlpha;

            // Set initial alpha values
            if (itemSlotIcons[i] != null)
            {
                Color c = itemSlotIcons[i].color;
                c.a = inactiveAlpha;
                itemSlotIcons[i].color = c;
            }

            // Hide all ticks initially
            if (i < itemCompletionTicks.Count && itemCompletionTicks[i] != null)
            {
                itemCompletionTicks[i].gameObject.SetActive(false);
            }
        }

        // Initial UI update
        UpdateInventoryUI();
    }

    private void Update()
    {
        // Smoothly fade the icons
        for (int i = 0; i < itemSlotIcons.Count; i++)
        {
            if (itemSlotIcons[i] == null) continue;

            Color c = itemSlotIcons[i].color;

            if (Mathf.Abs(c.a - targetAlphas[i]) > 0.01f)
            {
                c.a = Mathf.Lerp(c.a, targetAlphas[i], Time.deltaTime * fadeSpeed);
                itemSlotIcons[i].color = c;
            }
        }
    }

    public void UpdateInventoryUI()
    {
        List<Item> inventory = inventoryManager.GetInventory();

        // Reset all slots to inactive
        for (int i = 0; i < targetAlphas.Length; i++)
        {
            targetAlphas[i] = inactiveAlpha;
        }

        // Set active slots based on inventory
        foreach (Item item in inventory)
        {
            if (itemIDToSlotMap.TryGetValue(item.itemID, out int slotIndex))
            {
                if (slotIndex >= 0 && slotIndex < itemSlotIcons.Count)
                {
                    targetAlphas[slotIndex] = activeAlpha;

                    // Update the icon image if it has one
                    if (item.itemIcon != null && itemSlotIcons[slotIndex] != null)
                    {
                        itemSlotIcons[slotIndex].sprite = item.itemIcon;
                    }
                }
            }
        }
    }

    // Called when an item is deposited in the chest
    public void MarkItemAsDeposited(int itemID)
{
    // Only process if the item hasn't already been deposited
    if (!depositedItems.Contains(itemID))
    {
        depositedItems.Add(itemID);

        // Show the tick mark for this item
        if (itemIDToSlotMap.TryGetValue(itemID, out int slotIndex))
        {
            if (slotIndex >= 0 && slotIndex < itemCompletionTicks.Count && itemCompletionTicks[slotIndex] != null)
            {
                itemCompletionTicks[slotIndex].gameObject.SetActive(true);
            }
        }
        Debug.Log(depositedItems.Count);
        int depositedCount = depositedItems.Count;

        if (depositedCount == 1)
        {
            Debug.Log("phase2init.SetActive(true);");
            phase2init.SetActive(true);
        }
        else if (depositedCount == 3)
        {
            Debug.Log("phase3init.SetActive(true);");
            Debug.Log("Destroy(phase3destroy);");
            phase3init.SetActive(true);
            Destroy(phase3destroy);
        }
    }
}

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (inventoryManager != null)
        {
            inventoryManager.onInventoryChanged -= UpdateInventoryUI;
            inventoryManager.onItemDeposited -= MarkItemAsDeposited;
        }
    }
}