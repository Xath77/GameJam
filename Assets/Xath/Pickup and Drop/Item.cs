using UnityEngine;

public class Item : MonoBehaviour
{
    public GameObject JumpScare;
    [Header("Item Properties")]
    public string itemName = "Item";
    public int itemID = 0;
    public Sprite itemIcon;

    [Header("Interaction Settings")]
    public float interactionDistance = 2.5f;

    // Reference to the transform that will be saved in inventory when item is picked up
    [HideInInspector] public Transform itemTransform;

    private void Awake()
    {
        itemTransform = transform;
    }

    // This method will be called when the item is picked up
    public virtual void OnPickup()
    {
        // Hide the item when picked up
        gameObject.SetActive(false);

        if(itemID == 4)
        {
            Debug.Log("JUMPSCARE TRIGGERED!");
            JumpScare.SetActive(true);
        }
    }
}