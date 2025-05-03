using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // List to store references to picked up items
    private List<Item> inventory = new List<Item>();

    // Set to track which items have been deposited
    private HashSet<int> depositedItems = new HashSet<int>();

    // Events
    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChanged;

    public delegate void OnItemDeposited(int itemID);
    public event OnItemDeposited onItemDeposited;
    
    [SerializeField] private GameObject phase2init;
    [SerializeField] private GameObject phase3init;
    [SerializeField] private GameObject phase3Mission;
    [SerializeField] private GameObject phase3destroy;

    [Header("Inventory Settings")]
    public int maxInventorySize = 10;

    int count = 0;

    public void AddItem(Item item)
    {
        if (inventory.Count < maxInventorySize)
        {
            inventory.Add(item);
            Debug.Log("Picked up: " + item.itemName);

            if(item.itemName == "Skull")
            {
                Debug.Log("phase2init.SetActive(true);");
                phase2init.SetActive(true);
            }

            // Trigger the inventory changed event
            if (onInventoryChanged != null)
            {
                onInventoryChanged.Invoke();
            }
        }
        else
        {
            Debug.Log("Inventory is full!");
        }
    }

    public List<Item> GetInventory()
    {
        return inventory;
    }

    public bool HasItem(int itemID)
    {
        return inventory.Exists(item => item.itemID == itemID);
    }

    public bool HasItem(string itemName)
    {
        return inventory.Exists(item => item.itemName == itemName);
    }

    public void RemoveItem(Item item)
    {
        if (inventory.Contains(item))
        {
            inventory.Remove(item);

            // Trigger the inventory changed event
            if (onInventoryChanged != null)
            {
                onInventoryChanged.Invoke();
            }
        }
    }

    // For the chest drop system
    public Item GetLastItem()
    {
        if (inventory.Count > 0)
        {
            return inventory[inventory.Count - 1];
        }
        return null;
    }

    // Mark an item as deposited and trigger the event
    public void MarkItemAsDeposited(int itemID)
    {
        depositedItems.Add(itemID);
        Debug.Log("MarkItemAsDeposited called with ID: " + itemID);
        count++;
             if (count == 3)
            {
                Debug.Log("phase3init.SetActive(true);");
                Debug.Log("Destroy(phase3destroy);");
                phase3init.SetActive(true);
                phase3Mission.SetActive(true);
                Destroy(phase3destroy);
            }


        if (onItemDeposited != null)
        {
            onItemDeposited.Invoke(itemID);
        }
    }

    public bool IsItemDeposited(int itemID)
    {
        return depositedItems.Contains(itemID);
    }
}