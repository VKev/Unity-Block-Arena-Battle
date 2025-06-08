using UnityEngine;
using Unity.Netcode;

namespace PlayerStateMachine
{
    public class PlayerDamageTester : NetworkBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log($"[PlayerDamageTester] F key pressed by player {OwnerClientId}");
                TryDamageTarget();
            }
            
        }

        private void TryDamageTarget()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

            if (hits.Length == 0)
            {
                Debug.LogWarning("[PlayerDamageTester] Raycast did not hit anything");
                return;
            }

            Debug.Log($"[PlayerDamageTester] Raycast hit {hits.Length} object(s)");

            foreach (var hit in hits)
            {
                bool damaged = false;

                var totalHealth = hit.collider.GetComponentInParent<TotalHealth>();
                if (totalHealth != null && totalHealth != GetComponent<TotalHealth>())
                {
                    totalHealth.RequestDamage(20f);
                    Debug.Log("[PlayerDamageTester] Damaged TotalHealth");
                    damaged = true;
                }

                var phaseHealth = hit.collider.GetComponentInParent<PhaseHealth>();
                if (phaseHealth != null && phaseHealth != GetComponent<PhaseHealth>())
                {
                    phaseHealth.TakeDamage(10f); // You’ll need to create this method
                    Debug.Log("[PlayerDamageTester] Damaged PhaseHealth");
                    damaged = true;
                }

                if (!damaged)
                {
                    Debug.Log($"[PlayerDamageTester] Skipped {hit.collider.name} (no valid health script)");
                }
                else
                {
                    Debug.Log($"[PlayerDamageTester] Skipped {hit.collider.name} (no TotalHealth or self)");
                }
            }
        }
    }
}