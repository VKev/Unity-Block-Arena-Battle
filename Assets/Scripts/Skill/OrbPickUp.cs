using Player;
using Unity.Netcode;
using UnityEngine;
using System;

namespace Skill
{
    public class OrbPickup : NetworkBehaviour
    {
        public SkillType skillType;
        
        private Rigidbody rb;
        private const float customGravity = 4f;

        public event Action OnDespawned;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false; // Disable Unity's gravity to use our custom gravity
            }
        }

        private void FixedUpdate()
        {
            if (rb != null)
            {
                // Apply custom gravity
                rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            if (other.CompareTag("Player"))
            {
                PlayerSkillE playerSkill = other.GetComponent<PlayerSkillE>();
                if (playerSkill != null)
                {
                    bool collected = playerSkill.CollectOrb(skillType);
                    if (collected)
                    {
                        GetComponent<NetworkObject>().Despawn();
                    }
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            OnDespawned?.Invoke(); 
        }
    }
}