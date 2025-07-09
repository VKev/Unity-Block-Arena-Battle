using PlayerStateMachine;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using Player;

public enum GamePhase
{
    WaitingToStart,
    BuffPhase,
    SafePhase,
    FightPhase
}

public class NetworkCountdownManager : NetworkBehaviour
{
    public static NetworkCountdownManager Instance;

    [Header("Phase Durations")]
    [SerializeField] private float initialCountdown = 30f;
    [SerializeField] private float buffPhaseTime = 15f;
    [SerializeField] private float safePhaseTime = 30f;
    [SerializeField] private float fightPhaseTime = 60f;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showPhaseUI = true;
    [SerializeField] private bool debugStartWithFightPhase = false;

    private readonly NetworkVariable<float> timeRemaining = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<GamePhase> currentPhase = new(
        GamePhase.WaitingToStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ✅ Kill feed structure with timeout
    private class KillFeedEntry
    {
        public string Message;
        public float TimeShown;
    }

    private static readonly List<KillFeedEntry> killFeed = new();
    private const float killFeedDuration = 5f;

    public static event Action<GamePhase> OnPhaseChanged;
    public static event Action<GamePhase, float> OnPhaseTimeUpdate;

    public float GetTimeRemaining() => timeRemaining.Value;
    public GamePhase GetCurrentPhase() => currentPhase.Value;
    public string GetPhaseDisplayName() => GetPhaseDisplayName(currentPhase.Value);
    public float GetFightPhaseDuration() => fightPhaseTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple NetworkCountdownManager instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (debugStartWithFightPhase)
            {
                currentPhase.Value = GamePhase.FightPhase;
                timeRemaining.Value = fightPhaseTime;
                
                if (enableDebugLogs)
                    Debug.Log($"[CountdownManager] DEBUG MODE: Starting directly in Fight Phase for {fightPhaseTime}s");
                    
                // Trigger fight phase start immediately
                HandleFightPhaseStart();
            }
            else
            {
                currentPhase.Value = GamePhase.WaitingToStart;
                timeRemaining.Value = initialCountdown;
                
                if (enableDebugLogs)
                    Debug.Log($"[CountdownManager] Server initialized - Starting in {initialCountdown}s");
            }
        }

        currentPhase.OnValueChanged += OnPhaseValueChanged;
        timeRemaining.OnValueChanged += OnTimeValueChanged;

        if (enableDebugLogs)
            Debug.Log($"[CountdownManager] Client spawned - Current phase: {currentPhase.Value}");
    }

    public override void OnNetworkDespawn()
    {
        currentPhase.OnValueChanged -= OnPhaseValueChanged;
        timeRemaining.OnValueChanged -= OnTimeValueChanged;
    }

    private void Update()
    {
        // Host-only: update countdown
        if (IsServer)
        {
            timeRemaining.Value = Mathf.Max(0f, timeRemaining.Value - Time.deltaTime);
            if (timeRemaining.Value <= 0f)
                AdvancePhase();
        }

        // All clients: remove expired kill feed entries
        killFeed.RemoveAll(entry => Time.time - entry.TimeShown > killFeedDuration);
    }

    private void AdvancePhase()
    {
        if (debugStartWithFightPhase)
        {
            // Debug mode: cycle between Fight and Safe phases only
            switch (currentPhase.Value)
            {
                case GamePhase.FightPhase:
                    TransitionTo(GamePhase.SafePhase, safePhaseTime);
                    break;
                case GamePhase.SafePhase:
                    TransitionTo(GamePhase.FightPhase, fightPhaseTime);
                    break;
                default:
                    // Fallback to fight phase if in an unexpected state
                    TransitionTo(GamePhase.FightPhase, fightPhaseTime);
                    break;
            }
        }
        else
        {
            // Normal mode: full phase cycle
            switch (currentPhase.Value)
            {
                case GamePhase.WaitingToStart:
                    TransitionTo(GamePhase.SafePhase, safePhaseTime);
                    break;
                case GamePhase.SafePhase:
                    TransitionTo(GamePhase.BuffPhase, buffPhaseTime);
                    break;
                case GamePhase.BuffPhase:
                    TransitionTo(GamePhase.FightPhase, fightPhaseTime);
                    break;
                case GamePhase.FightPhase:
                    TransitionTo(GamePhase.SafePhase, safePhaseTime);
                    break;
            }
        }
    }

    private void TransitionTo(GamePhase nextPhase, float duration)
    {
        GamePhase previousPhase = currentPhase.Value;
        currentPhase.Value = nextPhase;
        timeRemaining.Value = duration;

        if (enableDebugLogs)
            Debug.Log($"[CountdownManager] Phase transition: {previousPhase} → {nextPhase} ({duration}s)");

        switch (nextPhase)
        {
            case GamePhase.SafePhase:
                HandleSafePhaseStart();
                break;
            case GamePhase.FightPhase:
                HandleFightPhaseStart();
                break;
            case GamePhase.BuffPhase:
                HandleBuffPhaseStart();
                break;
        }

        if (previousPhase == GamePhase.FightPhase && nextPhase != GamePhase.FightPhase)
        {
            GameEvents.RequestClearAllOrbs();
        }
    }

    private void HandleSafePhaseStart()
    {
        if (enableDebugLogs)
            Debug.Log("[CountdownManager] Safe Phase: Restoring all players' health");

        PhaseHealth.RestoreAllPlayersHealth();
        NotifySafePhaseStartedClientRpc();
        GameEvents.RequestItemsSpawn(); 
    }

    private void HandleFightPhaseStart()
    {
        if (enableDebugLogs)
            Debug.Log("[CountdownManager] Fight Phase: Combat enabled");
        GameEvents.RequestOrbSpawn(); 
        NotifyFightPhaseStartedClientRpc();
    }

    private void HandleBuffPhaseStart()
    {
        if (enableDebugLogs)
            Debug.Log("[CountdownManager] Buff Phase: Prepare for combat");

        NotifyBuffPhaseStartedClientRpc();
    }

    private void OnPhaseValueChanged(GamePhase previousValue, GamePhase newValue)
    {
        if (enableDebugLogs)
            Debug.Log($"[CountdownManager] Phase changed on client: {previousValue} → {newValue}");

        OnPhaseChanged?.Invoke(newValue);
    }

    private void OnTimeValueChanged(float previousValue, float newValue)
    {
        OnPhaseTimeUpdate?.Invoke(currentPhase.Value, newValue);
    }

    // ✅ Call this when a player kills another
    public static void ReportKill(ulong killerId, ulong victimId)
    {
        string message = $"Player {killerId} killed Player {victimId}";

        if (Instance != null && Instance.IsServer)
        {
            Instance.BroadcastKillClientRpc(message);

            // Host displays it immediately
            killFeed.Insert(0, new KillFeedEntry { Message = message, TimeShown = Time.time });
        }
    }

    // ✅ Sync kill message to clients (but not host to avoid duplicate)
    [ClientRpc]
    private void BroadcastKillClientRpc(string message)
    {
        if (IsServer) return; // host already added it

        killFeed.Insert(0, new KillFeedEntry { Message = message, TimeShown = Time.time });
    }

    [ClientRpc]
    private void NotifySafePhaseStartedClientRpc()
    {
        if (enableDebugLogs)
            Debug.Log("[CountdownManager] Safe phase started on client");
    }

    [ClientRpc]
    private void NotifyFightPhaseStartedClientRpc()
    {
        if (enableDebugLogs)
            Debug.Log("[CountdownManager] Fight phase started on client");
    }

    [ClientRpc]
    private void NotifyBuffPhaseStartedClientRpc()
    {
        if (enableDebugLogs)
            Debug.Log("[CountdownManager] Buff phase started on client");
    }

    public static string GetPhaseDisplayName(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.WaitingToStart => "Waiting to Start",
            GamePhase.BuffPhase => "Buff Phase",
            GamePhase.SafePhase => "Safe Phase",
            GamePhase.FightPhase => "Fight Phase",
            _ => "Unknown Phase"
        };
    }

    public string GetFormattedTimeRemaining()
    {
        int minutes = Mathf.FloorToInt(timeRemaining.Value / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining.Value % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public float GetPhaseProgress()
    {
        float totalTime = currentPhase.Value switch
        {
            GamePhase.WaitingToStart => initialCountdown,
            GamePhase.BuffPhase => buffPhaseTime,
            GamePhase.SafePhase => safePhaseTime,
            GamePhase.FightPhase => fightPhaseTime,
            _ => 1f
        };

        return 1f - (timeRemaining.Value / totalTime);
    }

    [ContextMenu("Force Next Phase")]
    public void ForceNextPhase()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can force phase changes");
            return;
        }

        timeRemaining.Value = 0f;
        Debug.Log("[CountdownManager] Forced phase change");
    }

    [ContextMenu("Force Safe Phase")]
    public void ForceSafePhase()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can force phase changes");
            return;
        }

        TransitionTo(GamePhase.SafePhase, safePhaseTime);
        Debug.Log("[CountdownManager] Forced to Safe Phase");
    }

    [ContextMenu("Reset to Start")]
    public void ResetToStart()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can reset phases");
            return;
        }

        TransitionTo(GamePhase.WaitingToStart, initialCountdown);
        Debug.Log("[CountdownManager] Reset to start");
    }

    void OnGUI()
    {
        if (!showPhaseUI) return;

        // GUI.Box(new Rect(10, 10, 300, 180), "");

        // GUI.Label(new Rect(15, 60, 280, 20), "Kill Feed:");
        // for (int i = 0; i < killFeed.Count; i++)
        // {
        //     GUI.Label(new Rect(25, 120 + i * 18, 260, 20), killFeed[i].Message);
        // }
    }
}
