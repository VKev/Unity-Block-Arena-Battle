using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TotalHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private NetworkVariable<float> finalHealth = new(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static Dictionary<ulong, TotalHealth> AllPlayers = new();
    private static List<ulong> deathOrder = new();
    private static bool gameEnded = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            finalHealth.Value = maxHealth;
            if (!AllPlayers.ContainsKey(OwnerClientId))
                AllPlayers.Add(OwnerClientId, this);
        }

        finalHealth.OnValueChanged += OnHealthChanged;
        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        finalHealth.OnValueChanged -= OnHealthChanged;
        AllPlayers.Remove(OwnerClientId);
    }

    private void OnHealthChanged(float oldVal, float newVal)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        float percent = finalHealth.Value / maxHealth;
        PlayerHealthUIManager.Instance?.UpdateHealth(OwnerClientId, percent);
    }

    public void RequestDamage(float amount)
    {
        TakeDamageServerRpc(amount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float amount, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[TotalHealth] {senderClientId} requests to damage {OwnerClientId} for {amount}");

        // Check if player is invincible
        var playerSkill = GetComponent<Player.PlayerSkillE>();
        if (playerSkill != null && playerSkill.IsInvincible())
        {
            Debug.Log($"[TotalHealth] Player {OwnerClientId} is invincible, ignoring damage!");
            return;
        }

        finalHealth.Value = Mathf.Max(0, finalHealth.Value - amount);
    }

    // ✅ Called from PhaseHealth when player dies
    public static void NotifyPlayerDeath(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer || gameEnded) return;

        if (deathOrder.Contains(clientId)) return;

        // Only during FightPhase
        if (NetworkCountdownManager.Instance?.GetCurrentPhase() != GamePhase.FightPhase) return;

        deathOrder.Add(clientId);

        Debug.Log($"[TotalHealth] Player {clientId} placed at rank {5 - deathOrder.Count}");

        int place = deathOrder.Count;
        float[] damageByPlace = { 40f, 30f, 20f }; // 4th = 40, 3rd = 30, 2nd = 20
        if (place <= damageByPlace.Length)
        {
            float dmg = damageByPlace[place - 1];
            if (AllPlayers.TryGetValue(clientId, out var player))
            {
                player.finalHealth.Value = Mathf.Max(0f, player.finalHealth.Value - dmg);
            }
        }

        CheckForGameOver();
    }

    private static void CheckForGameOver()
    {
        var alive = AllPlayers.Values.Where(p => p.finalHealth.Value > 0).ToList();
        if (alive.Count == 1)
        {
            gameEnded = true;
            Debug.Log($"[TotalHealth] GAME OVER — Winner: {alive[0].OwnerClientId}");
            // TODO: Trigger end screen or victory animation here
        }
    }

    public float GetTotalHealth() => finalHealth.Value;
}
