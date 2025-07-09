using playerStat;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PlayerStateMachine
{
    public class PhaseHealth : NetworkBehaviour
    {
        private float maxHealth = 100f;

        private readonly NetworkVariable<float> currentHealth =
            new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> isDead =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerBaseStats playerStats;
        private WorldPhaseHealthUI worldUI;
        private DamageVignetteEffect damageVignette;

        private RaycastWeapon weapon;
        private static readonly List<PhaseHealth> AllDeadPlayers = new();
        private static readonly List<PhaseHealth> AllPlayers = new();

        public override void OnNetworkSpawn()
        {
            playerStats = GetComponent<PlayerBaseStats>();
            weapon = GetComponent<RaycastWeapon>();
            
            // Add this player to the all players list
            if (!AllPlayers.Contains(this))
                AllPlayers.Add(this);
            
            // Try to find DamageVignetteEffect on this GameObject first, then in children
            damageVignette = GetComponent<DamageVignetteEffect>();
            if (damageVignette == null)
            {
                damageVignette = GetComponentInChildren<DamageVignetteEffect>();
            }
            
            Debug.Log($"[PhaseHealth] DamageVignetteEffect component found: {damageVignette != null} for client {OwnerClientId}");

            // Find WorldPhaseHealthUI using tag (only for the owner)
            if (IsOwner)
            {
                FindHealthBarUI();
                
                // If not found immediately, try again in a few frames (for timing issues)
                if (worldUI == null)
                {
                    StartCoroutine(DelayedHealthBarSearch());
                }
            }

            if (playerStats != null)
                maxHealth = playerStats.MaxHP;

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
                isDead.Value = false;
            }

            worldUI?.SetHealth(1f);
            currentHealth.OnValueChanged += OnHealthChanged;
            isDead.OnValueChanged += OnDeadStateChanged;

            UpdateUI();
            UpdateVisibility();
        }

        public override void OnNetworkDespawn()
        {
            AllDeadPlayers.Remove(this);
            AllPlayers.Remove(this);
            currentHealth.OnValueChanged -= OnHealthChanged;
            isDead.OnValueChanged -= OnDeadStateChanged;
        }

        private void OnHealthChanged(float previousValue, float newValue)
        {
            Debug.Log($"[PhaseHealth] Health changed from {previousValue} to {newValue} for client {OwnerClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}");
            
            UpdateUI();
            
            // Trigger damage vignette effect if health decreased and this is the owner
            if (IsOwner && newValue < previousValue)
            {
                Debug.Log($"[PhaseHealth] Health decreased for owner - triggering vignette effect");
                
                if (damageVignette != null)
                {
                    damageVignette.TriggerDamageEffect(newValue, maxHealth);
                    Debug.Log($"[PhaseHealth] Vignette effect triggered successfully");
                }
                else
                {
                    Debug.LogWarning($"[PhaseHealth] DamageVignette component not found on owner client {OwnerClientId}");
                }
            }
        }

        public void TakeDamage(float dmg)
        {
            TakeDamage(dmg, null);
        }

        public void TakeDamage(float dmg, ulong? attackerClientId = null)
        {
            Debug.Log($"[PhaseHealth] TakeDamage called with {dmg} damage for client {OwnerClientId}, attacker: {attackerClientId}, IsServer: {IsServer}");
            
            // Check if player is invincible
            var playerSkill = GetComponent<Player.PlayerSkillE>();
            if (playerSkill != null && playerSkill.IsInvincible())
            {
                Debug.Log($"[PhaseHealth] Player {OwnerClientId} is invincible, ignoring damage!");
                return;
            }
            
            if (IsServer) 
            {
                Debug.Log($"[PhaseHealth] Applying damage directly on server");
                ApplyDamage(dmg, attackerClientId);
            }
            else 
            {
                Debug.Log($"[PhaseHealth] Sending damage RPC to server");
                if (attackerClientId.HasValue)
                    TakeDamageServerRpc(dmg, attackerClientId.Value);
                else
                    TakeDamageServerRpc(dmg);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void TakeDamageServerRpc(float dmg, ServerRpcParams _ = default) 
        {
            Debug.Log($"[PhaseHealth] TakeDamageServerRpc received - {dmg} damage for client {OwnerClientId}");
            ApplyDamage(dmg);
        }

        [ServerRpc(RequireOwnership = false)]
        private void TakeDamageServerRpc(float dmg, ulong attackerClientId, ServerRpcParams _ = default) 
        {
            Debug.Log($"[PhaseHealth] TakeDamageServerRpc received - {dmg} damage for client {OwnerClientId} from attacker {attackerClientId}");
            ApplyDamage(dmg, attackerClientId);
        }

        private void ApplyDamage(float dmg)
        {
            ApplyDamage(dmg, null);
        }

        private void ApplyDamage(float dmg, ulong? attackerClientId = null)
        {
            Debug.Log($"[PhaseHealth] === APPLYING DAMAGE ===");
            Debug.Log($"[PhaseHealth] Target: Client {OwnerClientId}, Raw damage: {dmg}, Current HP: {currentHealth.Value}, Attacker: {attackerClientId}");
            Debug.Log($"[PhaseHealth] IsServer: {IsServer}, IsOwner: {IsOwner}, IsHost: {IsHost}");
            
            // Double-check invincibility on server (for extra safety)
            var playerSkill = GetComponent<Player.PlayerSkillE>();
            if (playerSkill != null && playerSkill.IsInvincible())
            {
                Debug.Log($"[PhaseHealth] Player {OwnerClientId} is invincible, ignoring damage in ApplyDamage!");
                return;
            }
            
            if (currentHealth.Value <= 0f) 
            {
                Debug.Log($"[PhaseHealth] Player already dead, ignoring damage");
                return;
            }

            // Debug PlayerBaseStats availability
            Debug.Log($"[PhaseHealth] PlayerBaseStats null: {playerStats == null}");
            if (playerStats != null)
            {
                Debug.Log($"[PhaseHealth] PlayerStats - IsOwner: {playerStats.IsOwner}, IsServer: {playerStats.IsServer}");
                Debug.Log($"[PhaseHealth] PlayerStats - ExtraHP: {playerStats.CurrentExtraHP}/{playerStats.MaxExtraHP}, Armor: {playerStats.Armor}");
            }

            // Get armor reduction from PlayerBaseStats
            float armorReduction = 0f;
            if (playerStats != null)
            {
                armorReduction = Mathf.Clamp01(playerStats.Armor / 100f);
            }
            
            int reducedDamage = Mathf.RoundToInt(dmg * (1f - armorReduction));
            Debug.Log($"[PhaseHealth] Damage after armor ({playerStats?.Armor ?? 0}%): {reducedDamage}");
            
            // Check Extra HP first - This code should only run on server
            if (!IsServer)
            {
                Debug.LogError("[PhaseHealth] ApplyDamage called on non-server! This should not happen.");
                return;
            }
            
            if (playerStats != null)
            {
                Debug.Log($"[PhaseHealth] Before damage - ExtraHP: {playerStats.CurrentExtraHP}/{playerStats.MaxExtraHP}, HP: {currentHealth.Value}/{maxHealth}");
                Debug.Log($"[PhaseHealth] PlayerStats network state - IsOwner: {playerStats.IsOwner}, IsServer: {playerStats.IsServer}");
                
                int remainingDamage = reducedDamage;
                
                // FIRST: Damage Extra HP if available
                if (playerStats.CurrentExtraHP > 0 && remainingDamage > 0)
                {
                    Debug.Log($"[PhaseHealth] *** EXTRA HP DAMAGE LOGIC TRIGGERED ***");
                    Debug.Log($"[PhaseHealth] Current Extra HP: {playerStats.CurrentExtraHP}, Remaining damage: {remainingDamage}");
                    Debug.Log($"[PhaseHealth] Attempting to reduce Extra HP by {remainingDamage}");
                    
                    int extraHPDamageApplied = playerStats.ReduceExtraHP(remainingDamage);
                    remainingDamage -= extraHPDamageApplied;
                    
                    Debug.Log($"[PhaseHealth] *** EXTRA HP DAMAGE RESULT ***");
                    Debug.Log($"[PhaseHealth] Extra HP absorbed {extraHPDamageApplied} damage -> ExtraHP: {playerStats.CurrentExtraHP}/{playerStats.MaxExtraHP}, Remaining damage: {remainingDamage}");
                }
                else
                {
                    Debug.Log($"[PhaseHealth] *** NO EXTRA HP DAMAGE ***");
                    Debug.Log($"[PhaseHealth] Extra HP: {playerStats.CurrentExtraHP}, Damage: {remainingDamage}");
                    if (playerStats.CurrentExtraHP <= 0)
                        Debug.Log($"[PhaseHealth] Reason: No Extra HP available");
                    if (remainingDamage <= 0)
                        Debug.Log($"[PhaseHealth] Reason: No damage to apply");
                }
                
                // SECOND: Apply remaining damage to normal HP
                if (remainingDamage > 0)
                {
                    float newHealth = Mathf.Max(0f, currentHealth.Value - remainingDamage);
                    Debug.Log($"[PhaseHealth] Normal HP took {remainingDamage} damage -> HP: {newHealth}/{maxHealth}");
                    currentHealth.Value = newHealth;
                }
                else
                {
                    Debug.Log($"[PhaseHealth] All damage absorbed by Extra HP! Normal HP untouched.");
                }
                
                Debug.Log($"[PhaseHealth] Final state - ExtraHP: {playerStats.CurrentExtraHP}/{playerStats.MaxExtraHP}, HP: {currentHealth.Value}/{maxHealth}");
            }
            else
            {
                // Fallback if no PlayerBaseStats found
                Debug.LogWarning($"[PhaseHealth] No PlayerBaseStats found for client {OwnerClientId}, applying damage directly to HP");
                float newHealth = Mathf.Max(0f, currentHealth.Value - reducedDamage);
                currentHealth.Value = newHealth;
            }

            if (currentHealth.Value == 0f)
            {
                Debug.Log($"[PhaseHealth] Player died, calling OnDeath");
                OnDeath(attackerClientId);
            }
            
            Debug.Log($"[PhaseHealth] === DAMAGE APPLIED ===");
        }

        private void OnDeath(ulong? attackerClientId)
        {
            if (IsServer)
            {
                isDead.Value = true;
                TotalHealth.NotifyPlayerDeath(OwnerClientId);
                
                // Award score to the attacker if we're in fighting phase
                if (attackerClientId.HasValue && attackerClientId.Value != OwnerClientId)
                {
                    AwardKillScore(attackerClientId.Value);
                }
            }
                
        }

        private void AwardKillScore(ulong killerClientId)
        {
            // Check if we're in fighting phase before awarding score
            if (NetworkCountdownManager.Instance != null && 
                NetworkCountdownManager.Instance.GetCurrentPhase() == GamePhase.FightPhase)
            {
                // Find the killer's PlayerBaseStats and award score via RPC
                foreach (var player in FindObjectsOfType<PlayerBaseStats>())
                {
                    if (player.OwnerClientId == killerClientId)
                    {
                        // Use ClientRpc to tell the specific client to award themselves score
                        var clientRpcParams = new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new[] { killerClientId }
                            }
                        };
                        
                        player.AwardKillScoreClientRpc(100, clientRpcParams);
                        Debug.Log($"[PhaseHealth] Sent kill score RPC to client {killerClientId} for killing {OwnerClientId}");
                        break;
                    }
                }
            }
            else
            {
                Debug.Log($"[PhaseHealth] Not in fighting phase, no score awarded for kill");
            }
        }

        private void OnDeadStateChanged(bool previousValue, bool newValue)
        {
            UpdateVisibility();

            if (newValue && !AllDeadPlayers.Contains(this))
                AllDeadPlayers.Add(this);
            else if (!newValue)
                AllDeadPlayers.Remove(this);
                
            // Clear vignette when player dies or respawns
            if (IsOwner && damageVignette != null)
            {
                if (newValue)
                    damageVignette.ClearVignette();
                else
                    damageVignette.UpdateHealthVignette(currentHealth.Value / maxHealth);
            }
        }

        private void UpdateVisibility()
        {
            bool shouldBeVisible = !isDead.Value;

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.enabled = shouldBeVisible;

            var colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
                collider.isTrigger = !shouldBeVisible;

            if (weapon != null)
                weapon.enabled = shouldBeVisible;

            Debug.Log($"[PhaseHealth] Player {OwnerClientId} visible: {shouldBeVisible}");
        }

        private void UpdateUI()
        {
            float pct = currentHealth.Value / maxHealth;
            Debug.Log($"[PhaseHealth] UpdateUI called - Health: {currentHealth.Value}/{maxHealth} = {pct:F2}% for client {OwnerClientId}, WorldUI: {worldUI != null}");
            
            if (worldUI != null)
            {
                worldUI.SetHealth(pct);
                Debug.Log($"[PhaseHealth] UI updated successfully to {pct:F2}%");
            }
        }

        public static void RestoreAllDeadPlayers()
        {
            Debug.Log($"[PhaseHealth] Restoring {AllDeadPlayers.Count} dead players");
            for (int i = AllDeadPlayers.Count - 1; i >= 0; i--)
            {
                var ph = AllDeadPlayers[i];
                if (ph == null)
                {
                    AllDeadPlayers.RemoveAt(i);
                    continue;
                }

                if (ph.IsServer)
                {
                    ph.currentHealth.Value = ph.maxHealth;
                    ph.isDead.Value = false;
                }
            }
        }
        
        public static void RestoreAllPlayersHealth()
        {
            Debug.Log($"[PhaseHealth] Restoring health for all {AllPlayers.Count} players");
            for (int i = AllPlayers.Count - 1; i >= 0; i--)
            {
                var ph = AllPlayers[i];
                if (ph == null)
                {
                    AllPlayers.RemoveAt(i);
                    continue;
                }

                if (ph.IsServer)
                {
                    ph.currentHealth.Value = ph.maxHealth;
                    ph.isDead.Value = false;
                    Debug.Log($"[PhaseHealth] Restored health for player {ph.OwnerClientId} to {ph.maxHealth}");
                }
            }
        }

        public bool IsPlayerDead() => isDead.Value;
        public float GetHealthPercentage() => currentHealth.Value / maxHealth;
        public float GetCurrentHealth() => currentHealth.Value;

        private void FindHealthBarUI()
        {
            GameObject healthBarGO = GameObject.FindGameObjectWithTag("PlayerHealthBar");
            if (healthBarGO != null)
            {
                worldUI = healthBarGO.GetComponent<WorldPhaseHealthUI>();
                if (worldUI != null)
                {
                    Debug.Log($"[PhaseHealth] Found PlayerHealthBar UI for owner client {OwnerClientId}");
                }
                else
                {
                    Debug.LogWarning($"[PhaseHealth] GameObject with tag 'PlayerHealthBar' found but no WorldPhaseHealthUI component!");
                }
            }
            else
            {
                Debug.LogWarning($"[PhaseHealth] No GameObject with tag 'PlayerHealthBar' found for client {OwnerClientId}");
            }
        }

        private System.Collections.IEnumerator DelayedHealthBarSearch()
        {
            int attempts = 0;
            const int maxAttempts = 10;
            
            while (worldUI == null && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.5f); // Wait half a second between attempts
                attempts++;
                
                Debug.Log($"[PhaseHealth] Delayed search attempt {attempts}/{maxAttempts} for PlayerHealthBar UI (client {OwnerClientId})");
                FindHealthBarUI();
                
                if (worldUI != null)
                {
                    Debug.Log($"[PhaseHealth] Successfully found PlayerHealthBar UI on delayed attempt {attempts} for client {OwnerClientId}");
                    worldUI.SetHealth(currentHealth.Value / maxHealth); // Update with current health
                    break;
                }
            }
            
            if (worldUI == null)
            {
                Debug.LogError($"[PhaseHealth] Failed to find PlayerHealthBar UI after {maxAttempts} attempts for client {OwnerClientId}");
            }
        }
    }
}
