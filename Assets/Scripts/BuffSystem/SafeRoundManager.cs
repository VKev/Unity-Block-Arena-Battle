using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using buffSystem;

public class SafeRoundManager : MonoBehaviour
{
    private GamePhase lastPhase = GamePhase.WaitingToStart; // or any default

    private void Awake()
    {
    }


    private void Update()
    {
        if (NetworkCountdownManager.Instance == null) return;

        GamePhase current = NetworkCountdownManager.Instance.GetCurrentPhase();

        // Only trigger when entering BuffPhase from a different phase
        if (current == GamePhase.BuffPhase && lastPhase != GamePhase.BuffPhase)
        {
            RequestBuffSelection();
        }

        lastPhase = current;
    }


    public void RequestBuffSelection()
    {
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