using UnityEngine;

public class ItemLife : MonoBehaviour
{
    public SpawItem spawner { get; set; } // Reference to the spawner that created this item

    [Header("Item Lifetime")]
    [Tooltip("Time in seconds before this item automatically destroys itself. Set to 0 for no automatic destruction.")]
    [SerializeField]private float lifeTime = 0f; // Default to 0, meaning no auto-destruction

  
    private void Start()
    {
        if (lifeTime > 0f)
        {
            
            Destroy(gameObject, lifeTime);
            Debug.Log($"ItemLife: '{gameObject.name}' will self-destruct in {lifeTime} seconds.", this);
        }
      
    }

    /// <summary>
    /// Call this method from interaction scripts when the item is collected or used.
    /// </summary>
    public void OnItemCollected()
    {
        Debug.Log("ItemLife: Item collected! Destroying.", this);
        // This also calls OnDestroy() after the GameObject is destroyed.
        Destroy(gameObject);
    }

    /// <summary>
    /// This method is a Unity callback, called automatically AFTER the GameObject has been destroyed.
    /// It's used for cleanup or notifying other systems.
    /// </summary>
    private void OnDestroy()
    {
        // Ensure the spawner exists and is still active in the hierarchy before notifying it.
        // This prevents errors if the spawner itself is being destroyed or the scene is unloading.
        if (spawner != null && spawner.gameObject.activeInHierarchy)
        {
            spawner.ItemDestroyed();
            Debug.Log($"ItemLife: OnDestroy callback executed. Notified spawner.", this); // Added a log for clarity
        }
    }
}