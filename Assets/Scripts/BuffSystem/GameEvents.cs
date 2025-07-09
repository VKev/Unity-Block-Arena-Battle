using UnityEngine;
using System;
using System.Collections.Generic;
using buffSystem;
using Skill; // Assuming this is your buff system namespace


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
    public static event Action<List<Buff>> OnActiveBuffsChanged;
    public static event Action<int> OnChestCollected;
    public static event Action OnOrbSpawnRequested;
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnScoreChanged;
    public static event Action<float> OnSpeedChange;
    public static event Action OnStopOrbSpawnRequested;
    
    // Player stats events
    public static event Action<int> OnDamageChanged;
    public static event Action<float> OnMoveSpeedChanged;
    public static event Action<float> OnAttackSpeedChanged;
    public static event Action<float> OnArmorChanged;


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

    public static void RequestOrbSpawn()
    {
        OnOrbSpawnRequested?.Invoke();
        Debug.Log("GameEvents: OnOrbSpawnRequested event fired.");
    }

    public static void RequestClearAllOrbs()
    {
        OnStopOrbSpawnRequested?.Invoke();
        Debug.Log("GameEvents: OnClearAllOrbsRequested event fired.");
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

    public static void TriggerActiveBuffsChanged(List<Buff> currentActiveBuffs)
    {
        OnActiveBuffsChanged?.Invoke(currentActiveBuffs);
        UnityEngine.Debug.Log("GameEvents: OnActiveBuffsChanged event fired.");
    }

    public static void TriggerGoldChanged(int newGoldAmount)
    {
        OnGoldChanged?.Invoke(newGoldAmount);
    }

    public static void TriggerScoreChanged(int newScoreAmount)
    {
        OnScoreChanged?.Invoke(newScoreAmount);
    }

    public static void TriggerSpeedChange(float newSpeed)
    {
        OnSpeedChange?.Invoke(newSpeed);
        UnityEngine.Debug.Log($"GameEvents: Speed changed to {newSpeed}.");
    }
    
    // Player stats trigger methods
    public static void TriggerDamageChanged(int newDamage)
    {
        OnDamageChanged?.Invoke(newDamage);
        UnityEngine.Debug.Log($"GameEvents: Damage changed to {newDamage}.");
    }
    
    public static void TriggerMoveSpeedChanged(float newMoveSpeed)
    {
        OnMoveSpeedChanged?.Invoke(newMoveSpeed);
        UnityEngine.Debug.Log($"GameEvents: Move speed changed to {newMoveSpeed}.");
    }
    
    public static void TriggerAttackSpeedChanged(float newAttackSpeed)
    {
        OnAttackSpeedChanged?.Invoke(newAttackSpeed);
        UnityEngine.Debug.Log($"GameEvents: Attack speed changed to {newAttackSpeed}.");
    }
    
    public static void TriggerArmorChanged(float newArmor)
    {
        OnArmorChanged?.Invoke(newArmor);
        UnityEngine.Debug.Log($"GameEvents: Armor changed to {newArmor}.");
    }
}