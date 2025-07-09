using UnityEngine;
using playerStat;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    public int extraHPCost = 50;
    public int extraHPAmount = 20;

    [Header("Visual Effects")]
    public GameObject purchaseEffectPrefab;
    public Vector3 spawnOffset = Vector3.up;

    [Header("Interaction")]
    public float interactionDistance = 2f;
    public GameObject interactionPrompt;

    private PlayerBaseStats nearbyPlayerStats;
    private bool playerInRange = false;

    void Update()
    {
        CheckForNearbyPlayer();
        
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            BuyExtraHP();
        }
        
        // Show/hide interaction prompt
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(playerInRange);
        }
    }

    void CheckForNearbyPlayer()
    {
        // Find all players in scene
        PlayerBaseStats[] allPlayers = FindObjectsOfType<PlayerBaseStats>();
        PlayerBaseStats closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (PlayerBaseStats player in allPlayers)
        {
            // Only check local/owner players
            if (player.IsOwner)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= interactionDistance && distance < closestDistance)
                {
                    closestPlayer = player;
                    closestDistance = distance;
                }
            }
        }

        // Update nearby player
        bool wasInRange = playerInRange;
        nearbyPlayerStats = closestPlayer;
        playerInRange = nearbyPlayerStats != null;

        // Log when player enters/exits range
        if (playerInRange && !wasInRange)
        {
            Debug.Log($"[Shop] Player {nearbyPlayerStats.gameObject.name} entered shop range");
        }
        else if (!playerInRange && wasInRange)
        {
            Debug.Log($"[Shop] Player left shop range");
        }
    }

    void BuyExtraHP()
    {
        if (nearbyPlayerStats == null)
        {
            Debug.LogWarning("[Shop] No player in range to buy Extra HP");
            return;
        }

        // Check if player has enough gold
        if (nearbyPlayerStats.Gold >= extraHPCost)
        {
            // Deduct gold and add extra HP
            nearbyPlayerStats.SpendGold(extraHPCost);
            nearbyPlayerStats.AddExtraHP(extraHPAmount);
            
            Debug.Log($"[Shop] {nearbyPlayerStats.gameObject.name} bought {extraHPAmount} Extra HP for {extraHPCost} gold");
            Debug.Log($"[Shop] Player now has {nearbyPlayerStats.CurrentExtraHP}/{nearbyPlayerStats.MaxExtraHP} Extra HP and {nearbyPlayerStats.Gold} gold");
            
            // Spawn purchase effect
            SpawnPurchaseEffect();
        }
        else
        {
            Debug.LogWarning($"[Shop] {nearbyPlayerStats.gameObject.name} doesn't have enough gold! Need: {extraHPCost}, Have: {nearbyPlayerStats.Gold}");
        }
    }

    void SpawnPurchaseEffect()
    {
        if (purchaseEffectPrefab != null)
        {
            // Calculate spawn position (shop position + offset)
            Vector3 spawnPosition = transform.position + spawnOffset;
            
            // Spawn the effect prefab
            GameObject spawnedEffect = Instantiate(purchaseEffectPrefab, spawnPosition, Quaternion.identity);
            
            Debug.Log($"[Shop] Spawned purchase effect at {spawnPosition}");
            
            // Auto-destroy after 3 seconds
            Destroy(spawnedEffect, 3f);
        }
        else
        {
            Debug.LogWarning("[Shop] No purchase effect prefab assigned!");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}