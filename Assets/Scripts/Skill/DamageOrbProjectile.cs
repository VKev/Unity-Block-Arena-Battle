using PlayerStateMachine;
using Unity.Netcode;
using UnityEngine;

namespace Skill
{
    public class DamageProjectile : NetworkBehaviour
    {
        public int damageAmount = 20;
        public float lifeTime = 1.2f;
        public float projectileSpeed = 20f;
        public ulong ownerClientId;
        
        [Header("Bomb Settings")]
        [Tooltip("Bomb prefab to spawn when projectile hits something")]
        public GameObject bombPrefab;
        [Tooltip("Whether to spawn bomb on any collision or only on player hit")]
        public bool spawnBombOnAnyHit = true;
        
        [Header("Explosion Settings")]
        [Tooltip("Damage dealt to players in explosion radius")]
        public int explosionDamage = 20;
        [Tooltip("Radius of the explosion effect")]
        public float explosionRadius = 5f;
        [Tooltip("Force applied to push players back")]
        public float explosionForce = 0.02f ;
        
        private Rigidbody rb;
        private Vector3 direction;
        private bool hasExploded = false; // Prevent multiple explosions

        private void Start()
        {
            if (IsServer)
            {
                Destroy(gameObject, lifeTime);
            }
            
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            // Configure rigidbody for projectile movement
            rb.useGravity = false;
            rb.linearDamping = 0f;
        }
        
        public void SetDirection(Vector3 shootDirection)
        {
            direction = shootDirection.normalized;
            if (rb != null)
            {
                rb.linearVelocity = direction * projectileSpeed;
            }
        }
        
        private void FixedUpdate()
        {
            if (IsServer && rb != null && direction != Vector3.zero)
            {
                // Ensure consistent velocity in case of collisions
                rb.linearVelocity = direction * projectileSpeed;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer) return;

            bool isPlayerHit = collision.gameObject.CompareTag("Player");
            
            // Handle player damage if hit
            if (isPlayerHit)
            {
                var targetHealth = collision.gameObject.GetComponent<PhaseHealth>();

                if (targetHealth != null)
                {
                    if (targetHealth.IsPlayerDead())
                    {
                        Debug.Log("[Projectile] Target already dead — no damage");
                    }
                    else if (NetworkCountdownManager.Instance != null &&
                        NetworkCountdownManager.Instance.GetCurrentPhase() == GamePhase.FightPhase)
                    {
                        float oldHealth = targetHealth.GetHealthPercentage();

                        targetHealth.TakeDamage(damageAmount, ownerClientId);

                        Debug.Log($"[Projectile] Dealt {damageAmount} damage to {collision.gameObject.name}");

                        if (targetHealth.IsPlayerDead())
                        {
                            NetworkCountdownManager.ReportKill(ownerClientId, targetHealth.OwnerClientId);
                        }
                    }
                    else
                    {
                        Debug.Log("[Projectile] Damage ignored — not in Fight Phase");
                    }
                }
            }

            // Spawn bomb on hit (either player hit or any collision based on settings)
            if (bombPrefab != null && (spawnBombOnAnyHit || isPlayerHit))
            {
                SpawnBomb(collision.contacts[0].point);
            }

            DespawnSelf();
        }
        
        private void SpawnBomb(Vector3 hitPosition)
        {
            if (bombPrefab != null)
            {
                // Spawn bomb prefab (visual effect) at the hit position
                GameObject bomb = Instantiate(bombPrefab, hitPosition, Quaternion.identity);
                
                Debug.Log($"[Projectile] Spawned bomb visual effect at position: {hitPosition}");
                
                // IMMEDIATE EXPLOSION - no delay!
                DoExplosion(hitPosition);
                
                // Auto-destroy the bomb visual effect after 0.5 seconds
                Destroy(bomb, 1f);
                
                // If the bomb has a NetworkObject component, we might need to spawn it on the network
                var bombNetworkObject = bomb.GetComponent<NetworkObject>();
                if (bombNetworkObject != null)
                {
                    bombNetworkObject.Spawn();
                }
            }
        }
        
        private void DoExplosion(Vector3 explosionPosition)
        {
            // Prevent multiple explosions from same projectile
            if (hasExploded) 
            {
                Debug.Log("[Bomb] Already exploded, skipping duplicate explosion");
                return;
            }
            hasExploded = true;
            
            Debug.Log($"[Bomb] Exploding at position {explosionPosition} with radius {explosionRadius}");
            
            // Handle damage on server only (authoritative)
            if (IsServer)
            {
                // Find all players in explosion radius
                Collider[] playersInRange = Physics.OverlapSphere(explosionPosition, explosionRadius);
                
                foreach (var collider in playersInRange)
                {
                    if (collider.CompareTag("Player"))
                    {
                        HandlePlayerDamage(collider.gameObject, explosionPosition);
                    }
                }
            }
            
            // Apply force on all clients (including host)
            Debug.Log($"[Bomb] Calling ClientRpc with force: {explosionForce}, radius: {explosionRadius}");
            ApplyExplosionForceClientRpc(explosionPosition, explosionRadius, explosionForce);
        }
        
        private void HandlePlayerDamage(GameObject player, Vector3 explosionPosition)
        {
            Debug.Log($"[Bomb] Player {player.name} in explosion - Fixed Damage: {explosionDamage}");
            
            // Apply FIXED damage (no distance falloff)
            var playerHealth = player.GetComponent<PhaseHealth>();
            if (playerHealth != null)
            {
                if (!playerHealth.IsPlayerDead() && 
                    NetworkCountdownManager.Instance != null &&
                    NetworkCountdownManager.Instance.GetCurrentPhase() == GamePhase.FightPhase)
                {
                                            playerHealth.TakeDamage(explosionDamage, ownerClientId);
                    
                    Debug.Log($"[Bomb] Dealt {explosionDamage} FIXED damage to {player.name}");
                    
                    if (playerHealth.IsPlayerDead())
                    {
                        NetworkCountdownManager.ReportKill(ownerClientId, playerHealth.OwnerClientId);
                    }
                }
                else
                {
                    Debug.Log($"[Bomb] No damage dealt to {player.name} - dead or not in fight phase");
                }
            }
        }
        
        [ClientRpc]
        private void ApplyExplosionForceClientRpc(Vector3 explosionPosition, float radius, float force)
        {
            Debug.Log($"[Bomb] ClientRpc received - Position: {explosionPosition}, Radius: {radius}, Force: {force}");
            
            // Find all players in explosion radius on this client
            Collider[] playersInRange = Physics.OverlapSphere(explosionPosition, radius);
            
            foreach (var collider in playersInRange)
            {
                if (collider.CompareTag("Player"))
                {
                    // Calculate distance and direction for force application
                    Vector3 directionToPlayer = (collider.transform.position - explosionPosition).normalized;
                    float distanceToPlayer = Vector3.Distance(explosionPosition, collider.transform.position);
                    
                    // Calculate force falloff
                    float forceMultiplier = Mathf.Clamp01(1f - (distanceToPlayer / radius));
                    float actualForce = force * forceMultiplier;
                    
                    Debug.Log($"[Bomb] Applying force {actualForce:F2} to {collider.name} on client");
                    
                    // Apply pushback force
                    var playerRb = collider.GetComponent<Rigidbody>();
                    if (playerRb != null)
                    {
                        Vector3 forceDirection = directionToPlayer;
                        forceDirection.y = Mathf.Max(forceDirection.y, 0.3f); // Add some upward force
                        
                        playerRb.AddForce(forceDirection * actualForce, ForceMode.Impulse);
                        
                        Debug.Log($"[Bomb] Applied force {actualForce:F2} to {collider.name} in direction {forceDirection}");
                    }
                }
            }
        }

        private void DespawnSelf()
        {
            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
            else
                Destroy(gameObject);
        }
    }
}