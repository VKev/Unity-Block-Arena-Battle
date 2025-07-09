using PlayerStateMachine;
using Unity.Netcode;
using UnityEngine;

namespace Skill
{
    public class Trap : NetworkBehaviour
    {
        public int damageAmount = 40;
        public ulong ownerClientId;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            if (other.CompareTag("Player"))
            {
                var playerNetworkObject = other.GetComponent<NetworkObject>();
                if (playerNetworkObject != null && playerNetworkObject.OwnerClientId == ownerClientId)
                {
                    return;
                }

                var targetHealth = other.GetComponent<PhaseHealth>();
                var attackerHealth = GetComponentInParent<PhaseHealth>();

                if (targetHealth != null)
                {
                    if (targetHealth.IsPlayerDead())
                    {
                        Debug.Log("[Trap] Target already dead — no damage");
                        return;
                    }

                    if (NetworkCountdownManager.Instance != null &&
                        NetworkCountdownManager.Instance.GetCurrentPhase() == GamePhase.FightPhase)
                    {
                        float oldHealth = targetHealth.GetHealthPercentage();

                        targetHealth.TakeDamage(damageAmount, ownerClientId);

                        Debug.Log($"[Trap] Dealt {damageAmount} damage to {other.name}");
                        
                        if (targetHealth.IsPlayerDead())
                        {
                            NetworkCountdownManager.ReportKill(ownerClientId, targetHealth.OwnerClientId);
                        }
                    }
                    else
                    {
                        Debug.Log("[Trap] Damage ignored — not in Fight Phase");
                    }
                }
                
                GetComponent<NetworkObject>()?.Despawn();
            }
        }
    }
}
