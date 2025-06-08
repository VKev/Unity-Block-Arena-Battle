using UnityEngine;
using System.Collections; // Required for IEnumerator and Coroutines
using System.Collections.Generic; // Required for List<T>
using System; // Required for Action

public class SpawItem : MonoBehaviour
{
    [Header("Item Prefabs")]
    // Drag your different item prefabs (e.g., chest prefab, other item prefabs) here in the Inspector
    public List<GameObject> itemPrefabs;

    [Header("Spawning Settings")]
    [Tooltip("Total number of items to spawn when TriggerSpawnItems is called.")]
    public int maxItems = 5;
    [Tooltip("Time between each individual item spawn. Set to 0 for instant bulk spawn of all maxItems.")]
    public float spawnInterval = 0f;

    // ---  SPAWN AREA PARAMETERS ---
    public Vector3 centerSpawnPoint = new Vector3(8.41f, 2.495f, 0.58f); //  center

    [Tooltip("How far out from the Center Spawn Point items can spawn on the X and Z axes.")]
    public float spawnRadiusXZ = 10f;
    [Tooltip("The minimum Y offset from the Center Spawn Point's Y for spawning.")]
    public float spawnOffsetYMin = 0.1f; // Spawn very slightly above the center Y to ensure it's above ground.

    [Tooltip("The maximum Y offset from the Center Spawn Point's Y for spawning.")]
    public float spawnOffsetYMax = 2f; // Allow for some vertical variation above the ground.


    
    public Vector3 spawnAreaMin; // Minimum X,Y,Z for spawning
    public Vector3 spawnAreaMax;   // Maximum X,Y,Z for spawning

    private int currentItemCount = 0; // Tracks how many items are currently active in the scene
    private Coroutine currentSpawnRoutine; // To manage the spawning coroutine if interval spawning is active

    void Awake()
    {
        // Calculate the actual min/max bounds based on the center and radius/offsets
        CalculateSpawnBounds();

        if (itemPrefabs == null || itemPrefabs.Count == 0)
        {
            Debug.LogError("SpawItem: No item prefabs assigned! Please assign prefabs to the 'Item Prefabs' list in the Inspector.", this);
            enabled = false;
        }
    }

    void OnValidate()
    {
        // OnValidate is called in the editor when the script is loaded or a value is changed in the Inspector.
        // This allows us to see the calculated spawnAreaMin/Max in the Inspector immediately.
        CalculateSpawnBounds();
    }

    private void CalculateSpawnBounds()
    {
        // X and Z bounds are +/- spawnRadiusXZ from the centerSpawnPoint
        spawnAreaMin.x = centerSpawnPoint.x - spawnRadiusXZ;
        spawnAreaMax.x = centerSpawnPoint.x + spawnRadiusXZ;

        spawnAreaMin.z = centerSpawnPoint.z - spawnRadiusXZ;
        spawnAreaMax.z = centerSpawnPoint.z + spawnRadiusXZ;

        // Y bounds are based on offsets from the centerSpawnPoint's Y
        spawnAreaMin.y = centerSpawnPoint.y + spawnOffsetYMin;
        spawnAreaMax.y = centerSpawnPoint.y + spawnOffsetYMax;
    }


    void OnEnable()
    {
        // Ensure GameEvents is properly defined elsewhere 
        // e.g., public static class GameEvents { public static event Action OnItemsSpawnRequested; }
        GameEvents.OnItemsSpawnRequested += TriggerSpawnItems;
        Debug.Log("SpawItem: Subscribed to OnItemsSpawnRequested event.", this);
    }

    // Unsubscribe from events when this component is disabled or destroyed
    void OnDisable()
    {
        GameEvents.OnItemsSpawnRequested -= TriggerSpawnItems;
        Debug.Log("SpawItem: Unsubscribed from OnItemsSpawnRequested event.", this);

        if (currentSpawnRoutine != null)
        {
            StopCoroutine(currentSpawnRoutine);
            currentSpawnRoutine = null;
        }
    }

    /// <summary>
    /// This method is called by the GameEvents.OnItemsSpawnRequested event.
    /// It initiates the spawning process: either instant bulk spawn or interval-based spawn.
    /// </summary>
    public void TriggerSpawnItems()
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0)
        {
            Debug.LogError("TriggerSpawnItems: Cannot spawn, no item prefabs assigned.", this);
            return;
        }

        // Stop any existing spawn routine to prevent conflicts if TriggerSpawnItems is called multiple times
        if (currentSpawnRoutine != null)
        {
            StopCoroutine(currentSpawnRoutine);
            currentSpawnRoutine = null;
        }

        // Reset current count if you want to always spawn 'maxItems' from scratch each time this is called.
        currentItemCount = 0;

        if (spawnInterval <= 0f) // If interval is 0 or negative, spawn all instantly in one frame
        {
            for (int i = 0; i < maxItems; i++)
            {
                SpawnSingleItemInstance();
            }
            Debug.Log($"SpawItem: Spawned {maxItems} items instantly.", this);
        }
        else // If interval is positive, spawn with a delay using a coroutine
        {
            currentSpawnRoutine = StartCoroutine(SpawnItemsWithIntervalRoutine());
            Debug.Log($"SpawItem: Starting to spawn {maxItems} items with a {spawnInterval} second interval.", this);
        }
    }

    /// <summary>
    /// Coroutine to spawn items one by one with a delay.
    /// </summary>
    private IEnumerator SpawnItemsWithIntervalRoutine()
    {
        for (int i = 0; i < maxItems; i++)
        {
            if (currentItemCount < maxItems)
            {
                SpawnSingleItemInstance();
            }
            else
            {
                Debug.Log("SpawItem: Max items reached during interval spawning. Stopping routine.", this);
                yield break; // Exit the coroutine
            }
            yield return new WaitForSeconds(spawnInterval); // Wait for the specified interval
        }
        Debug.Log("SpawItem: Finished spawning all items via interval routine.", this);
        currentSpawnRoutine = null;
    }

    /// <summary>
    /// Handles the instantiation of a single item at a random position.
    /// </summary>
    private void SpawnSingleItemInstance()
    {
        // Safety check to avoid spawning too many if somehow currentItemCount goes over max
        if (currentItemCount >= maxItems)
        {
            Debug.LogWarning("SpawItem: Attempted to spawn single item, but max items already reached. Ignoring.", this);
            return;
        }

        // Generate a random position within the defined area
        float randomX = UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomY = UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        float randomZ = UnityEngine.Random.Range(spawnAreaMin.z, spawnAreaMax.z);

        Vector3 spawnPosition = new Vector3(randomX, randomY, randomZ);

        // Randomly select an item prefab from the list
        int randomIndex = UnityEngine.Random.Range(0, itemPrefabs.Count);
        GameObject selectedPrefab = itemPrefabs[randomIndex];

        // Instantiate the selected item prefab at the random position
        GameObject newItem = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        currentItemCount++; // Increment count as soon as it's created

        // Add ItemLife component to the new item and link it back to this spawner
        // This is crucial for the spawner to know when an item is destroyed
        ItemLife itemLife = newItem.AddComponent<ItemLife>();
        itemLife.spawner = this;
        Debug.Log($"SpawItem: Spawned '{selectedPrefab.name}' at {spawnPosition}. Current active items: {currentItemCount}.", this);
    }

    /// <summary>
    /// Called by the ItemLife script when an item is destroyed to decrement the active count.
    /// </summary>
    public void ItemDestroyed()
    {
        currentItemCount--;
        Debug.Log($"SpawItem: Item destroyed. Current active items: {currentItemCount}.", this);
    }
}