using UnityEngine;
using System;
using System.Collections.Generic;
using buffSystem; // Assuming this is your buff system namespace


public class BuffChosenEventArgs : EventArgs
{
    public Buff ChosenBuff { get; private set; }

    public BuffChosenEventArgs(Buff chosenBuff)
    {
        ChosenBuff = chosenBuff;
    }
}

public static class GameEvents
{
    public static event Action OnItemsSpawnRequested;
    public static event Action<List<Buff>> OnRequestBuffSelectionUI;
    public static event EventHandler<BuffChosenEventArgs> OnBuffChosen;
    public static event Action<int> OnChestCollected;

    public static void RequestItemsSpawn()
    {
        OnItemsSpawnRequested?.Invoke();
        Debug.Log("GameEvents: OnItemsSpawnRequested event fired.");
    }

    public static void RequestBuffSelectionUI(List<Buff> buffsToDisplay)
    {
        OnRequestBuffSelectionUI?.Invoke(buffsToDisplay);
        Debug.Log("GameEvents: OnRequestBuffSelectionUI event fired.");
    }

    public static void TriggerBuffChosen(Buff chosenBuff)
    {
        OnBuffChosen?.Invoke(null, new BuffChosenEventArgs(chosenBuff));
        UnityEngine.Debug.Log($"EventManager: Buff chosen event triggered for {chosenBuff.Name}.");
    }

    public static void TriggerChestCollected(int amount)
    {
        OnChestCollected?.Invoke(amount);
        UnityEngine.Debug.Log($"EventManager: Chest collected event triggered for chest amount {amount}.");
    }
}