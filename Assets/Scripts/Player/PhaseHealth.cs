using UnityEngine;
using Unity.Netcode;

namespace PlayerStateMachine
{
    public class PhaseHealth : NetworkBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        private NetworkVariable<float> currentHealth = new(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField] private WorldPhaseHealthUI worldUI;
        
        
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
                currentHealth.Value = maxHealth;

            
            
            worldUI?.SetHealth(1f);
            
            currentHealth.OnValueChanged += (oldVal, newVal) => UpdateUI();
            UpdateUI();
        }

        /// <summary>
        /// Called by any other script (client or server) to request damage.
        /// Clients will send a ServerRpc.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (IsServer)
            {
                ApplyDamage(amount);
            }
            else
            {
                TakeDamageServerRpc(amount);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void TakeDamageServerRpc(float amount, ServerRpcParams rpcParams = default)
        {
            ulong attackerClientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"[PhaseHealth] Damage requested by client {attackerClientId}");
            ApplyDamage(amount);
        }

        private void ApplyDamage(float amount)
        {
            if (currentHealth.Value <= 0) return;

            currentHealth.Value = Mathf.Max(0, currentHealth.Value - amount);
            Debug.Log($"[PhaseHealth] Damage applied. Current HP: {currentHealth.Value}/{maxHealth}");

            if (currentHealth.Value == 0)
            {
                OnDeath();
            }
        }

        private void UpdateUI()
        {
            float percent = currentHealth.Value / maxHealth;
            worldUI?.SetHealth(percent);
        }

        private void OnDeath()
        {
            Debug.Log("[PhaseHealth] Entity dead (phase HP = 0)");
            // TODO: trigger death logic, animation, notify manager, etc.
        }
    }
}
