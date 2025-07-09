using Unity.Netcode;
using UnityEngine;

namespace Player
{
    public class ShieldFollower : NetworkBehaviour
    {
        private Transform targetTransform;
        private ulong ownerClientId;
        public Vector3 offset = Vector3.zero;
        public float followSpeed = 10f;
        
        public void SetTarget(Transform target, ulong clientId)
        {
            targetTransform = target;
            ownerClientId = clientId;
        }
        
        void Update()
        {
            if (targetTransform != null)
            {
                // Smoothly follow the target player
                Vector3 targetPosition = targetTransform.position + offset;
                transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
                
                // Optional: rotate to match player rotation
                transform.rotation = targetTransform.rotation;
            }
            else
            {
                // Try to find the target player if we lost reference
                FindTargetPlayer();
            }
        }
        
        private void FindTargetPlayer()
        {
            if (NetworkManager.Singleton == null) return;
            
            // Find the player with the matching client ID
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (client.Key == ownerClientId)
                {
                    var playerObject = client.Value.PlayerObject;
                    if (playerObject != null)
                    {
                        targetTransform = playerObject.transform;
                        break;
                    }
                }
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // If we don't have a target yet, try to find it
            if (targetTransform == null)
            {
                FindTargetPlayer();
            }
        }
    }
} 