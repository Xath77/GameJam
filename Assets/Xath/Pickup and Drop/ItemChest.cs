using UnityEngine;

public class ItemChest : MonoBehaviour
{
    [Header("Chest Settings")]
    public float interactionDistance = 2.5f;

    private void OnTriggerStay(Collider other)
    {
        // Check if the player entered the trigger
        if (other.CompareTag("Player"))
        {
            // Notify the player interaction system that they're in range of a chest
            PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();
            if (playerInteraction != null)
            {
                playerInteraction.SetCurrentChest(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Clear the reference when player leaves
        if (other.CompareTag("Player"))
        {
            PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();
            if (playerInteraction != null)
            {
                playerInteraction.ClearCurrentChest();
            }
        }
    }

    // Called when an item is dropped into the chest
    public void ReceiveItem(Item item)
    {
        // Restore the item to the scene
        item.gameObject.SetActive(true);

        // Place the item inside or near the chest
        item.transform.position = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
    }
}