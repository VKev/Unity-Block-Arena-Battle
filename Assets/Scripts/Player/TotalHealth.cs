using Unity.Netcode;
using UnityEngine;

public class TotalHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private NetworkVariable<float> finalHealth = new(
        100f);


    public override void OnNetworkSpawn()
    {
        if (IsServer)
            finalHealth.Value = maxHealth;

        finalHealth.OnValueChanged += OnHealthChanged;
        UpdateUI();
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

        // Optional: log who is trying to do damage
        Debug.Log($"TakeDamageServerRpc called by {senderClientId}");

        finalHealth.Value = Mathf.Max(0, finalHealth.Value - amount);
    }

}