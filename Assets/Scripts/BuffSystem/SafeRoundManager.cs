using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using buffSystem;

public class SafeRoundManager : MonoBehaviour
{
    // REMOVE THIS LINE: [SerializeField] private BuffSelectionUI buffSelectionUI;
    // The manager no longer directly controls the UI, it publishes an event.

    private void Awake()
    {

    }

    private void Start()
    {
        Debug.Log("GameManager: Game started. Initializing game flow...");
        Invoke("RequestBuffSelection", 3f);
        //Invoke("TriggerItemSpawnEvent", 3f);

    }

    public void RequestBuffSelection()
    {
        // REMOVE THIS BLOCK: No need to check buffSelectionUI reference
        // if (buffSelectionUI == null)
        // {
        //     Debug.LogError("GameManager: Cannot request buff selection, BuffSelectionUI reference is missing.");
        //     return;
        // }

        Debug.Log("GameManager: Preparing buffs for selection...");

        BuffTier randomTier = GetRandomTier();
        List<Buff> availableBuffs = BuffLibrary.GetBuffsByTier(randomTier);

        List<Buff> selectedBuffs = new List<Buff>();

        if (availableBuffs.Count >= 3)
        {
            selectedBuffs = availableBuffs.OrderBy(x => Random.value).Take(3).ToList();
        }

        // --- NEW: PUBLISH THE EVENT FOR THE UI TO SHOW ITSELF ---
        GameEvents.RequestBuffSelectionUI(selectedBuffs);
        Debug.Log("GameManager: RequestBuffSelectionUI event fired with generated choices.");

        Invoke("TriggerItemSpawnEvent", 0.5f);
    }

    private void TriggerItemSpawnEvent()
    {
        GameEvents.RequestItemsSpawn();
    }

    private BuffTier GetRandomTier()
    {
        int roll = Random.Range(0, 3);
        return (BuffTier)roll;
    }
}