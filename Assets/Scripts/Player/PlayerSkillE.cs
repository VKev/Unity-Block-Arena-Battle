using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using playerStat;
using Skill;
using UnityEngine.Serialization;
using PlayerStateMachine;

namespace Player
{
    public class PlayerSkillE : NetworkBehaviour
    {
        public int maxSlots = 3;
        private Queue<SkillType> orbSlots = new Queue<SkillType>();
        public GameObject trapPrefab;
        public GameObject trapOrbPrefab;
        public event Action OnOrbSlotsChangedEvent;
        public bool IsLocalPlayer() => IsOwner;

        public bool IsInvincible() => isInvincible.Value;

        public Transform shootPoint;
        private PlayerBaseStats playerStats;
        private Rigidbody rb;
        private float pushBackTimer = 0f;
        private Vector3 pushBackForce = Vector3.zero;

        public GameObject speedOrbPrefab;
        public GameObject stunOrbPrefab;
        public GameObject damageOrbPrefab;
        public GameObject pushBackOrbPrefab;
        public GameObject pushBackEffectPrefab; // Prefab to spawn at push back location
        public GameObject invincibleOrbPrefab;
        public GameObject invincibleEffectPrefab;
        public GameObject speedBoostEffectPrefab;
        private GameObject currentInvincibleEffect;
        private GameObject currentSpeedBoostEffect;

        public GameObject stunProjectilePrefab;
        public GameObject damageProjectilePrefab;

        private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private float invincibleTimer = 0f;
        private float speedBoostTimer = 0f;
        private float speedReductionTimer = 0f;
        public NetworkList<int> networkOrbSlots = new NetworkList<int>();

        private Vector3 moveInput;
        public float pushBackDuration = 0.3f;
        [FormerlySerializedAs("IsStunned")] public NetworkVariable<bool> isStunned = new NetworkVariable<bool>(false);
        public static event Action<PlayerSkillE> OnPlayerSkillSpawned;


        void Start()
        {
            playerStats = GetComponent<PlayerBaseStats>();
            rb = GetComponent<Rigidbody>();
            networkOrbSlots.OnListChanged += OnOrbSlotsChanged;
        }

        public override void OnDestroy()
        {
            networkOrbSlots.OnListChanged -= OnOrbSlotsChanged;
            NetworkCountdownManager.OnPhaseChanged -= OnPhaseChanged;
        }

        public int GetOrbCount()
        {
            return networkOrbSlots.Count;
        }

        public void ClearAllOrbs()
        {
            networkOrbSlots.Clear();
            orbSlots.Clear();
        }

        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            OnPlayerSkillSpawned?.Invoke(this);
            
            // Subscribe to phase changes to clear orbs when fight phase ends
            NetworkCountdownManager.OnPhaseChanged += OnPhaseChanged;
        }


        void OnOrbSlotsChanged(NetworkListEvent<int> changeEvent)
        {
            orbSlots.Clear();
            foreach (var orbInt in networkOrbSlots)
            {
                orbSlots.Enqueue((SkillType)orbInt);
            }

            if (IsOwner)
            {
                Debug.Log("[PlayerSkillE] OnOrbSlotsChanged for local player.");
                OnOrbSlotsChangedEvent?.Invoke();
            }
        }
        
        private void OnPhaseChanged(GamePhase newPhase)
        {
            // Clear orbs when fight phase ends
            if (newPhase != GamePhase.FightPhase)
            {
                if (IsServer)
                {
                    ClearAllOrbs();
                    Debug.Log($"[PlayerSkillE] Cleared orbs for player {OwnerClientId} - phase changed to {newPhase}");
                }
            }
        }



        void Update()
        {
            if (!IsOwner) return;

            HandleTimers();
            // GetInput();

            if (Input.GetKeyDown(KeyCode.E))
                UseNextOrbServerRpc();
        }

        void FixedUpdate()
        {
            if (!IsOwner) return;

            if (pushBackTimer > 0)
            {
                pushBackTimer -= Time.fixedDeltaTime;
                return;
            }

            // if (isStunned.Value)
            // {
            //     rb.linearVelocity = Vector3.zero;
            //     return;
            // }


            if (moveInput.magnitude > 0.1f)
            {
                Vector3 moveVelocity = moveInput * playerStats.MoveSpeed;
                rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
            }
            else if (new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude > 0.1f)
            {
                // Nếu đang có velocity lớn, giảm dần về 0 (damping)
                Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                Vector3 damping = Vector3.Lerp(horizontalVel, Vector3.zero, 0.2f);
                rb.linearVelocity = new Vector3(damping.x, rb.linearVelocity.y, damping.z);
            }
        }


        void HandleTimers()
        {
            if (speedBoostTimer > 0)
            {
                speedBoostTimer -= Time.deltaTime;
                if (speedBoostTimer <= 0)
                {
                    // Only reset speed if no reduction is active
                    if (speedReductionTimer <= 0)
                    {
                        playerStats.SpeedMultiplier = 1f;
                    }
                    Debug.Log("Speed boost ended.");
                    if (currentSpeedBoostEffect != null)
                        Destroy(currentSpeedBoostEffect);
                }
            }

            if (speedReductionTimer > 0)
            {
                speedReductionTimer -= Time.deltaTime;
                if (speedReductionTimer <= 0)
                {
                    // Only reset speed if no boost is active
                    if (speedBoostTimer <= 0)
                    {
                        playerStats.SpeedMultiplier = 1f;
                    }
                    Debug.Log("Speed reduction ended.");

                    // Clear blue vignette effect
                    var damageVignette = GetComponent<DamageVignetteEffect>();
                    if (damageVignette != null)
                    {
                        damageVignette.ClearSpeedReductionEffect();
                    }
                }
            }

            if (invincibleTimer > 0)
            {
                invincibleTimer -= Time.deltaTime;
                if (invincibleTimer <= 0)
                {
                    if (IsServer) isInvincible.Value = false;
                    Debug.Log("Invincibility ended via timer.");
                    if (currentInvincibleEffect != null)
                        Destroy(currentInvincibleEffect);
                }
            }
        }

        // void GetInput()
        // {
        //     if (isStunned.Value)  
        //     {
        //         moveInput = Vector3.zero;
        //         return;
        //     }
        //
        //     float moveH = Input.GetAxis("Horizontal");
        //     float moveV = Input.GetAxis("Vertical");
        //     moveInput = new Vector3(moveH, 0, moveV).normalized;
        // }


        public bool CollectOrb(SkillType type)
        {
            if (!IsServer) return false;

            // If it's a speed orb, apply speed boost immediately instead of storing it
            if (type == SkillType.SpeedBoost)
            {
                Debug.Log($"Speed orb collected - applying immediate speed boost!");
                ApplySpeedBoostClientRpc(OwnerClientId);
                return true;
            }

            if (networkOrbSlots.Count < maxSlots)
            {
                networkOrbSlots.Add((int)type);
                Debug.Log($"Collected orb: {type}. Total: {networkOrbSlots.Count}");
                return true;
            }
            else
            {
                Debug.Log("Orb slots full!");
                return false;
            }
        }

        [ClientRpc]
        void ApplySpeedBoostClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            Debug.Log("[ApplySpeedBoostClientRpc] Applying speed boost to client!");
            UseSpeedBoost();
        }

        [ClientRpc]
        void ApplySpeedReductionClientRpc(ClientRpcParams rpcParams = default)
        {
            Debug.Log("[ApplySpeedReductionClientRpc] Applying speed reduction!");
            UseSpeedReduction();
        }
        
        void UseSpeedReduction()
        {
            Debug.Log("[UseSpeedReduction] Speed reduced by half!");

            // Apply 0.5x speed multiplier (half speed)
            playerStats.SpeedMultiplier = 0.5f;

            speedReductionTimer = 5f; // Duration of speed reduction

            // Trigger blue vignette effect
            var damageVignette = GetComponent<DamageVignetteEffect>();
            if (damageVignette != null)
            {
                damageVignette.TriggerSpeedReductionEffect();
            }
        }

        [ServerRpc]
        void UseNextOrbServerRpc(ServerRpcParams rpcParams = default)
        {
            if (networkOrbSlots.Count == 0)
            {
                Debug.Log("No orb to use!");
                return;
            }

            SkillType type = (SkillType)networkOrbSlots[0];
            networkOrbSlots.RemoveAt(0);

            Debug.Log($"Used orb: {type}. Remaining: {networkOrbSlots.Count}");
            UseOrbClientRpc(type, OwnerClientId);
        }


        [ClientRpc]
        void UseOrbClientRpc(SkillType type, ulong ownerClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != ownerClientId) return;

            switch (type)
            {
                case SkillType.SpeedBoost: UseSpeedBoost(); break;
                // case SkillType.StunProjectile: UseStunProjectile(); break;
                case SkillType.Trap: UseTrap(); break;
                case SkillType.DamageProjectile: UseDamageProjectile(); break;
                case SkillType.PushBack: UsePushBack(); break;
                case SkillType.Invincible: UseInvincible(); break;
            }
        }

        void UseTrap()
        {
            Debug.Log("Trap placed!");
            if (IsOwner) SpawnTrapOrbServerRpc();
        }

        void UseSpeedBoost()
        {
            Debug.Log("[UseSpeedBoost] Called!");

            // Apply 1.5x speed multiplier instead of 6f
            playerStats.SpeedMultiplier = 1.5f;

            speedBoostTimer = 5f;

            if (currentSpeedBoostEffect != null)
                Destroy(currentSpeedBoostEffect);

            currentSpeedBoostEffect = Instantiate(speedBoostEffectPrefab, transform);
        }


        // void UseStunProjectile()
        // {
        //     Debug.Log("Stun Projectile fired!");
        //     if (IsOwner) SpawnStunProjectileServerRpc();
        // }

        // [ServerRpc]
        // public void ApplyStunServerRpc(float duration)
        // {
        //     if (isStunned.Value) return;
        //
        //     isStunned.Value = true;
        //     StartCoroutine(StunCoroutine(duration));
        // }

        // private IEnumerator StunCoroutine(float duration)
        // {
        //     Debug.Log($"{gameObject.name} is stunned for {duration} seconds.");
        //     yield return new WaitForSeconds(duration);
        //     isStunned.Value = false;
        //     Debug.Log($"{gameObject.name} stun ended.");
        // }


        void UseDamageProjectile()
        {
            Debug.Log("Damage Projectile fired!");
            if (IsOwner) 
            {
                // Calculate aim direction similar to weapon system
                Vector3 aimDirection = GetAimDirection();
                SpawnDamageProjectileServerRpc(aimDirection);
            }
        }
        
        private Vector3 GetAimDirection()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Fallback to forward direction if no camera
                return transform.forward;
            }
            
            // Create ray from camera center (similar to RaycastWeapon)
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            Vector3 targetPoint;
            
            // Raycast to find aim point
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                targetPoint = hit.point;
            }
            else
            {
                // If no hit, aim far ahead
                targetPoint = ray.origin + ray.direction * 1000f;
            }
            
            // Calculate direction from shoot point to target
            Vector3 direction = (targetPoint - shootPoint.position).normalized;
            return direction;
        }

        public void UsePushBack()
        {
            if (!IsOwner) return;
            
            // Get aim direction similar to weapon system
            Vector3 aimDirection = GetAimDirection();
            Vector3 hitPosition = GetPushBackHitPosition(aimDirection);
            
            PushBackServerRpc(hitPosition);
        }
        
        private Vector3 GetPushBackHitPosition(Vector3 aimDirection)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Fallback to position in front of player
                return transform.position + transform.forward * 5f;
            }
            
            // Create ray from camera center (similar to RaycastWeapon)
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            
            // Raycast to find hit point
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                return hit.point;
            }
            else
            {
                // If no hit, place at a distance in front of camera
                return ray.origin + ray.direction * 20f;
            }
        }

        [ServerRpc]
        private void PushBackServerRpc(Vector3 skillOrigin)
        {
            float radius = 2.5f;
            float pushForce = 10f;
            float damage = 15f;

            // Spawn the push back effect prefab at the hit location
            if (pushBackEffectPrefab != null)
            {
                GameObject pushBackEffect = Instantiate(pushBackEffectPrefab, skillOrigin, Quaternion.identity);
                
                // Auto-destroy the effect after a short time
                StartCoroutine(DestroyEffectAfterDelay(pushBackEffect, 1.5f));
                
                // If the effect has a NetworkObject, spawn it over the network
                var networkObject = pushBackEffect.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Spawn();
                }
            }

            Collider[] hitColliders = Physics.OverlapSphere(skillOrigin, radius);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject == gameObject) continue;

                if (hitCollider.CompareTag("Player"))
                {
                    Vector3 pushDir = (hitCollider.transform.position - skillOrigin).normalized;
                    pushDir.y = 0;
                    Debug.Log($"Push back AOE hit: {hitCollider.name}");

                    var skillScript = hitCollider.GetComponent<PlayerSkillE>();
                    var targetHealth = hitCollider.GetComponent<PhaseHealth>();

                    if (skillScript != null)
                    {
                        var clientRpcParams = new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new[] { skillScript.OwnerClientId }
                            }
                        };

                        // Apply push back force
                        skillScript.ApplyPushBackToClientRpc(pushDir * pushForce, clientRpcParams);
                        
                        // Apply speed reduction (half speed)
                        skillScript.ApplySpeedReductionClientRpc(clientRpcParams);
                    }

                    // Apply damage
                    if (targetHealth != null)
                    {
                        targetHealth.TakeDamage(damage, OwnerClientId);
                        Debug.Log($"Push back dealt {damage} damage to {hitCollider.name}");
                    }
                }
            }
        }
        
        private IEnumerator DestroyEffectAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (effect != null)
            {
                var networkObject = effect.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Despawn();
                }
                else
                {
                    Destroy(effect);
                }
            }
        }
        
        [ServerRpc]
        void SpawnTrapOrbServerRpc()
        {
            Vector3 spawnPos = transform.position + transform.forward * 1.5f;
            
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out hit, 20f, LayerMask.GetMask("Ground")))
            {
                spawnPos.y = hit.point.y;
            }
            else
            {
                spawnPos.y = 0;
            }

            var trapOrb = Instantiate(trapPrefab, spawnPos, Quaternion.identity);
            
            var trapScript = trapOrb.GetComponent<Skill.Trap>();
            if (trapScript != null)
            {
                trapScript.ownerClientId = OwnerClientId;
            }

            var effect = Instantiate(invincibleEffectPrefab, trapOrb.transform);
            effect.transform.localPosition = Vector3.zero;

            trapOrb.GetComponent<NetworkObject>()?.Spawn();
        }




        [ClientRpc]
        private void ApplyPushBackToClientRpc(Vector3 pushForce, ClientRpcParams rpcParams = default)
        {
            Debug.Log($"[Client] ApplyPushBackToClientRpc called on {gameObject.name} with force {pushForce}");
            if (isStunned.Value) return;
            pushBackTimer = pushBackDuration;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(pushForce, ForceMode.Impulse);
                Debug.Log($"[Client] Force applied on {gameObject.name}");
            }
        }


        void UseInvincible()
        {
            invincibleTimer = 7f; // Changed to 7 seconds as requested
            Debug.Log("Invincibility activated!");

            // Spawn shield on server so all clients can see it
            // Auto-destroy is now handled in SpawnShieldServerRpc
            if (IsOwner) 
            {
                SpawnShieldServerRpc();
            }
        }
        
        private IEnumerator DestroyShieldAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Destroy shield on server so all clients see it disappear
            if (IsOwner && currentInvincibleEffect != null)
            {
                DestroyShieldServerRpc();
            }
            
            if (IsServer) isInvincible.Value = false;
            Debug.Log("Shield destroyed and invincibility ended.");
        }

        private IEnumerator AutoDestroyShieldAfterDelay(ulong shieldNetworkObjectId, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Find and destroy the specific shield by its NetworkObjectId
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shieldNetworkObjectId, out NetworkObject shieldNetworkObject))
            {
                if (shieldNetworkObject != null)
                {
                    shieldNetworkObject.Despawn();
                    Debug.Log($"Shield auto-destroyed after {delay} seconds");
                }
            }
            
            // Clear the reference if this was our current shield and end invincibility
            if (currentInvincibleEffect != null && 
                currentInvincibleEffect.GetComponent<NetworkObject>()?.NetworkObjectId == shieldNetworkObjectId)
            {
                currentInvincibleEffect = null;
                if (IsServer) isInvincible.Value = false;
                Debug.Log("Invincibility ended due to shield auto-destruction.");
            }
        }

        private IEnumerator AutoDestroyNonNetworkedShieldAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (currentInvincibleEffect != null)
            {
                Destroy(currentInvincibleEffect);
                currentInvincibleEffect = null;
                if (IsServer) isInvincible.Value = false;
                Debug.Log($"Non-networked shield auto-destroyed after {delay} seconds - invincibility ended");
            }
        }

        [ServerRpc]
        void SpawnShieldServerRpc()
        {
            // Set invincibility state on server
            isInvincible.Value = true;
            
            // Destroy existing shield if any
            if (currentInvincibleEffect != null)
            {
                if (currentInvincibleEffect.GetComponent<NetworkObject>() != null)
                {
                    currentInvincibleEffect.GetComponent<NetworkObject>().Despawn();
                }
                else
                {
                    Destroy(currentInvincibleEffect);
                }
            }

            // Spawn new shield at player position
            currentInvincibleEffect = Instantiate(invincibleEffectPrefab, transform.position, Quaternion.identity);
            
            // If the prefab has a NetworkObject, spawn it over the network
            var networkObject = currentInvincibleEffect.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                
                // Set up shield to follow this player on all clients
                SetupShieldFollowerClientRpc(networkObject.NetworkObjectId, OwnerClientId);
                
                // Start auto-destroy timer on server
                StartCoroutine(AutoDestroyShieldAfterDelay(networkObject.NetworkObjectId, 7f));
            }
            else
            {
                // Fallback: make it a child if no NetworkObject
                currentInvincibleEffect.transform.SetParent(transform);
                currentInvincibleEffect.transform.localPosition = Vector3.zero;
                
                // Start auto-destroy timer for non-networked shield
                StartCoroutine(AutoDestroyNonNetworkedShieldAfterDelay(7f));
            }
        }

        [ClientRpc]
        void SetupShieldFollowerClientRpc(ulong shieldNetworkObjectId, ulong targetClientId)
        {
            // Find the shield object by its NetworkObjectId
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shieldNetworkObjectId, out NetworkObject shieldNetworkObject))
            {
                var shieldFollower = shieldNetworkObject.GetComponent<ShieldFollower>();
                if (shieldFollower == null)
                {
                    shieldFollower = shieldNetworkObject.gameObject.AddComponent<ShieldFollower>();
                }
                
                // Find the correct target player on this client
                Transform targetTransform = FindPlayerTransformByClientId(targetClientId);
                if (targetTransform != null)
                {
                    shieldFollower.SetTarget(targetTransform, targetClientId);
                }
            }
        }

        private Transform FindPlayerTransformByClientId(ulong clientId)
        {
            if (NetworkManager.Singleton == null) return null;
            
            // Find the player with the matching client ID
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientData))
            {
                if (clientData.PlayerObject != null)
                {
                    return clientData.PlayerObject.transform;
                }
            }
            
            return null;
        }

        [ServerRpc]
        void DestroyShieldServerRpc()
        {
            if (currentInvincibleEffect != null)
            {
                var networkObject = currentInvincibleEffect.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Despawn();
                }
                else
                {
                    Destroy(currentInvincibleEffect);
                }
                currentInvincibleEffect = null;
            }
        }


        [ServerRpc]
        void SpawnDamageProjectileServerRpc(Vector3 aimDirection)
        {
            var projectile = Instantiate(damageProjectilePrefab, shootPoint.position, shootPoint.rotation);
            projectile.GetComponent<NetworkObject>()?.Spawn();

            var damageComp = projectile.GetComponent<Skill.DamageProjectile>();
            if (damageComp != null)
            {
                damageComp.ownerClientId = OwnerClientId;
                damageComp.SetDirection(aimDirection);
            }
        }

        // [ServerRpc]
        // void SpawnStunProjectileServerRpc()
        // {
        //     var projectile = Instantiate(stunProjectilePrefab, shootPoint.position, shootPoint.rotation);
        //     projectile.GetComponent<Rigidbody>().AddForce(shootPoint.forward * 500f);
        //     projectile.GetComponent<NetworkObject>()?.Spawn();
        // }
    }
}