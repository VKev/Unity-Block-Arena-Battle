using UnityEngine;
using playerStat; // Ensure this namespace is correct for PlayerBaseStats

public class ChestInteraction : MonoBehaviour
{
    [Header("Chest Properties")]
    [SerializeField] public int goldAmount = 10; // Gold this chest gives

    private ItemLife itemLife;

    void Awake()
    {
        itemLife = GetComponent<ItemLife>();
        if (itemLife == null)
        {
            Debug.LogError("ChestInteraction: ItemLife component not found on this GameObject. Please add ItemLife script to the chest prefab.", this);
            enabled = false;
        }
    }

    // This method is now called when a physical collision occurs (both objects are solid)
    private void OnCollisionEnter(Collision collision)
    {
        PlayerBaseStats player = null;

        // Try to get the PlayerBaseStats component from the object that collided with the chest
        if (collision.gameObject.TryGetComponent<PlayerBaseStats>(out player))
        {
            Debug.Log($"Chest: Player (identified by PlayerBaseStats script) collided! Publishing chest collected event for {goldAmount} gold.", this);

            // Call your event trigger method (assuming GameEvents is correctly set up)
            GameEvents.TriggerChestCollected(goldAmount);

            // Now, tell this chest to destroy itself (handled by ItemLife)
            itemLife.OnItemCollected();
        }
    }
}