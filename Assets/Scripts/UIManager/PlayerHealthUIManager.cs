using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealthUIManager : NetworkBehaviour
{
    public static PlayerHealthUIManager Instance;

    [SerializeField] private Transform healthBarListRoot;
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private GameCountdownUI countdownUI;

    private Dictionary<ulong, HealthBarUI> healthBars = new();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("[PlayerHealthUIManager] OnNetworkSpawn called");
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        NetworkCountdownManager.Instance.StartCountdown(30f, IsServer);
    }

    private void OnClientConnected(ulong clientId)
    {
        foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
        {
            AddHealthBarClientRpc(player.ClientId, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }

        AddHealthBarClientRpc(clientId);
    }

    [ClientRpc]
    private void AddHealthBarClientRpc(ulong clientId, ClientRpcParams rpcParams = default)
    {
        if (!healthBars.ContainsKey(clientId))
        {
            var barGO = Instantiate(healthBarPrefab, healthBarListRoot);
            var bar = barGO.GetComponent<HealthBarUI>();
            healthBars[clientId] = bar;
            bar.SetFill(1f);
        }
    }

    public void UpdateHealth(ulong clientId, float percent)
    {
        if (healthBars.TryGetValue(clientId, out var bar))
        {
            bar.SetFill(percent);
        }
    }
}