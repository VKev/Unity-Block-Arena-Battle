using UnityEngine;
using playerStat; 

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

    
    private void OnCollisionEnter(Collision collision)
    {
        PlayerBaseStats player = null;

       
        if (collision.gameObject.TryGetComponent<PlayerBaseStats>(out player))
        {
            Debug.Log($"Chest: Player (identified by PlayerBaseStats script) collided! Publishing chest collected event for {goldAmount} gold.", this);

        
            GameEvents.TriggerChestCollected(goldAmount);

            itemLife.OnItemCollected();
        }
    }
}