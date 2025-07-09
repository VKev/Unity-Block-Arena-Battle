using UnityEngine;
using System.Collections.Generic;
using buffSystem;
using System;
using System.Collections;
using Player;
using Unity.Netcode;
using UnityEngine.Networking;
using LoginSystem;

namespace playerStat
{
    public class PlayerBaseStats : NetworkBehaviour
    {
        // === Base stats ===
        [SerializeField] private int _maxHP = 100;
        public int MaxHP => _maxHP;

        [SerializeField] private int _currentHP;
        public int CurrentHP => _currentHP;

        [SerializeField] private int _maxExtraHP = 100;
        public int MaxExtraHP => _maxExtraHP;

        [SerializeField] private NetworkVariable<int> _currentExtraHP;
        public int CurrentExtraHP => _currentExtraHP.Value;

        [SerializeField] private int _baseDamage = 20;
        public int BaseDamage => _baseDamage;

        [SerializeField] private float _baseMoveSpeed = 500f;
        public float BaseMoveSpeed => _baseMoveSpeed;

        [SerializeField] private NetworkVariable<int> _gold = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public int Gold => _gold.Value;

        [SerializeField] private NetworkVariable<int> _score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public int Score => _score.Value;

        [SerializeField] private float _baseAttackSpeed = 1f;
        public float BaseAttackSpeed => _baseAttackSpeed;

        [SerializeField] private float _baseArmor = 0f;
        public float BaseArmor => _baseArmor;

        // === Buff-influenced multipliers ===
        [SerializeField] public float DamageMultiplier { get; set; } = 1f;
        [SerializeField] public float SpeedMultiplier { get; set; } = 1f;
        [SerializeField] public int BonusGoldPerRound { get; set; } = 0;
        [SerializeField] public float AttackSpeedMultiplier { get; set; } = 1f;
        [SerializeField] public float ArmorBonus { get; set; } = 0f;

        // === Runtime calculated stats ===
        [SerializeField] public int Damage => Mathf.RoundToInt(_baseDamage * DamageMultiplier);
        [SerializeField] public float MoveSpeed => _baseMoveSpeed * SpeedMultiplier;
        [SerializeField] public float AttackSpeed => _baseAttackSpeed * AttackSpeedMultiplier;
        [SerializeField] public float Armor => _baseArmor + ArmorBonus;

        // === Active buffs ===
        [SerializeField] private List<Buff> _activeBuffs = new List<Buff>();
        public List<Buff> ActiveBuffs => _activeBuffs;

        // === Player Identity ===
        [SerializeField] private string _playerUsername = "";
        public string PlayerUsername => _playerUsername;

        private void Awake()
        {
            _currentHP = _maxHP;
            
            // Initialize NetworkVariable for Extra HP
            _currentExtraHP = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            
            // Ensure _maxExtraHP is set to 100 (in case serialized value is still 0)
            if (_maxExtraHP <= 0)
            {
                _maxExtraHP = 100;
                Debug.Log("[ExtraHP] MaxExtraHP was 0, setting to 100");
            }
            
            Debug.Log($"player spawn - MaxExtraHP: {_maxExtraHP}, CurrentExtraHP will be set in OnNetworkSpawn");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Initialize Extra HP
            if (IsServer)
            {
                _currentExtraHP.Value = 0; // Start with 0 Extra HP
                Debug.Log($"[ExtraHP] Server initialized Extra HP to 0 for client {OwnerClientId}");
            }
            
            // Subscribe to value changes only for the local player
            if (IsOwner)
            {
                // Store the current logged-in username for this player instance
                _playerUsername = LoginRegisterManager.GetSavedUsername();
                Debug.Log($"[PlayerBaseStats] Player {OwnerClientId} username set to: '{_playerUsername}'");
                
                _gold.OnValueChanged += OnGoldChanged;
                _score.OnValueChanged += OnScoreChanged;
                _currentExtraHP.OnValueChanged += OnExtraHPChanged;
                GameEvents.OnBuffChosen += HandleBuffChosen;
                Debug.Log("PlayerBaseStats subscribed to events.");
                
                // Trigger initial displays for this player
                GameEvents.TriggerGoldChanged(_gold.Value);
                GameEvents.TriggerScoreChanged(_score.Value);
                GameEvents.TriggerDamageChanged(Damage);
                GameEvents.TriggerMoveSpeedChanged(MoveSpeed);
                GameEvents.TriggerAttackSpeedChanged(AttackSpeed);
                GameEvents.TriggerArmorChanged(Armor);
                TriggerExtraHPRefresh();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (IsOwner)
            {
                _gold.OnValueChanged -= OnGoldChanged;
                _score.OnValueChanged -= OnScoreChanged;
                _currentExtraHP.OnValueChanged -= OnExtraHPChanged;
                GameEvents.OnBuffChosen -= HandleBuffChosen;
                Debug.Log("PlayerBaseStats unsubscribed from events.");
            }
        }

        private void OnGoldChanged(int previousValue, int newValue)
        {
            // Only trigger UI update for the local player who owns this stats
            if (IsOwner)
            {
                GameEvents.TriggerGoldChanged(newValue);
                Debug.Log($"[Gold] Local player gold changed: {previousValue} -> {newValue}");
            }
        }

        private void OnScoreChanged(int previousValue, int newValue)
        {
            // Only trigger UI update for the local player who owns this stats
            if (IsOwner)
            {
                GameEvents.TriggerScoreChanged(newValue);
                Debug.Log($"[Score] Local player score changed: {previousValue} -> {newValue}");
            }
        }

        private void OnExtraHPChanged(int previousValue, int newValue)
        {
            // Only trigger UI update for the local player who owns this stats
            if (IsOwner)
            {
                Debug.Log($"[ExtraHP] NetworkVariable changed: {previousValue} -> {newValue}, triggering UI refresh");
                TriggerExtraHPRefresh();
            }
        }



        private void HandleBuffChosen(object sender, BuffChosenEventArgs e)
        {
            Buff chosenBuff = e.ChosenBuff;
            ApplyBuff(chosenBuff);
            Debug.Log($"[PlayerBaseStats] Received and applied buff via event: {chosenBuff.Name}");
        }

        // === Gold methods ===
        public void AddGold(int amount)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("AddGold: Only the owner can modify their gold.");
                return;
            }
            
            _gold.Value += amount;
            Debug.Log($"[Gold] +{amount} -> Total: {_gold.Value}");
        }

        public void SpendGold(int amount)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("SpendGold: Only the owner can modify their gold.");
                return;
            }
            
            _gold.Value = Mathf.Max(0, _gold.Value - amount);
            Debug.Log($"[Gold] Spent {amount} -> Total: {_gold.Value}");
        }

        public void AddEndRoundGold()
        {
            if (!IsOwner)
            {
                Debug.LogWarning("AddEndRoundGold: Only the owner can modify their gold.");
                return;
            }
            
            AddGold(5 + BonusGoldPerRound);
        }

        // === Score methods ===
        public void AddScore(int amount)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("AddScore: Only the owner can modify their score.");
                return;
            }
            
            _score.Value += amount;
            Debug.Log($"[Score] +{amount} -> Total: {_score.Value}");
            
            // Send score update to API
            StartCoroutine(UpdateScoreOnServer(amount));
        }

        public void SetScore(int amount)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("SetScore: Only the owner can modify their score.");
                return;
            }
            
            _score.Value = amount;
            Debug.Log($"[Score] Set to: {_score.Value}");
        }

        public void AwardKillScore(int killScore = 100)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("AwardKillScore: Only the owner can modify their score.");
                return;
            }
            
            AddScore(killScore);
            Debug.Log($"[Score] Kill awarded {killScore} points -> Total: {_score.Value}");
        }

        [ClientRpc]
        public void AwardKillScoreClientRpc(int killScore, ClientRpcParams rpcParams = default)
        {
            if (!IsOwner) return; // Only the owner should process this
            
            AddScore(killScore);
            Debug.Log($"[Score] Kill awarded via RPC {killScore} points -> Total: {_score.Value}");
        }

        // === Health methods ===
        public void Heal(int amount)
        {
            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
            Debug.Log($"[HP] Healed {amount} -> CurrentHP: {_currentHP}");
        }

        // === Extra HP methods ===
        public void AddExtraHP(int amount)
        {
            if (IsServer)
            {
                // If we're on the server, apply directly
                ApplyExtraHPIncrease(amount);
            }
            else
            {
                // If we're on a client, send RPC to server
                Debug.Log($"[ExtraHP] Client requesting {amount} Extra HP via RPC");
                AddExtraHPServerRpc(amount);
            }
        }

        [ServerRpc(RequireOwnership = true)]
        private void AddExtraHPServerRpc(int amount)
        {
            Debug.Log($"[ExtraHP] Server received RPC to add {amount} Extra HP for client {OwnerClientId}");
            ApplyExtraHPIncrease(amount);
        }

        private void ApplyExtraHPIncrease(int amount)
        {
            int oldValue = _currentExtraHP.Value;
            _currentExtraHP.Value = Mathf.Min(_maxExtraHP, _currentExtraHP.Value + amount);
            int actualAdded = _currentExtraHP.Value - oldValue;
            
            Debug.Log($"[ExtraHP] Added {actualAdded} Extra HP -> {_currentExtraHP.Value}/{_maxExtraHP}");
            
            // UI will update automatically via OnExtraHPChanged when NetworkVariable syncs
        }

        public void SetMaxExtraHP(int amount)
        {
            _maxExtraHP = Mathf.Max(0, amount);
            Debug.Log($"[ExtraHP] Max Extra HP set to: {_maxExtraHP}");
        }

        public void RestoreExtraHP(int amount)
        {
            if (!IsServer) return;
            _currentExtraHP.Value = Mathf.Min(_maxExtraHP, _currentExtraHP.Value + amount);
        }

        public void RestoreFullExtraHP()
        {
            if (!IsServer) return;
            _currentExtraHP.Value = _maxExtraHP;
        }

        // Method for damage system to directly reduce Extra HP (used by PhaseHealth)
        // This should only be called from server (PhaseHealth.ApplyDamage runs on server)
        public int ReduceExtraHP(int damageAmount)
        {
            Debug.Log($"[ExtraHP] ReduceExtraHP called - Client {OwnerClientId}, Damage: {damageAmount}, CurrentExtraHP: {_currentExtraHP.Value}");
            Debug.Log($"[ExtraHP] IsOwner: {IsOwner}, IsServer: {IsServer}, IsHost: {IsHost}");
            
            // This method should only be called from server context (PhaseHealth.ApplyDamage)
            if (!IsServer)
            {
                Debug.LogError($"[ExtraHP] ReduceExtraHP called from non-server context! This should not happen.");
                return 0;
            }
            
            if (_currentExtraHP.Value <= 0) 
            {
                Debug.Log($"[ExtraHP] No Extra HP to reduce, returning 0");
                return 0;
            }
            
            int actualDamage = Mathf.Min(_currentExtraHP.Value, damageAmount);
            int oldExtraHP = _currentExtraHP.Value;
            _currentExtraHP.Value -= actualDamage;
            
            Debug.Log($"[ExtraHP] Server reduced Extra HP from {oldExtraHP} to {_currentExtraHP.Value} (damage applied: {actualDamage})");
            
            // UI will update automatically via OnExtraHPChanged when NetworkVariable syncs
            
            return actualDamage;
        }

        [ClientRpc]
        private void TriggerExtraHPRefreshClientRpc()
        {
            if (IsOwner)
            {
                Debug.Log($"[ExtraHP] ClientRpc triggering UI refresh for owner");
                TriggerExtraHPRefresh();
            }
        }

        // === Buff application ===
        public void ApplyBuff(Buff buff)
        {
            if (!_activeBuffs.Contains(buff))
            {
                _activeBuffs.Add(buff);

                buff.ApplyEffect?.Invoke(this);

                SpeedMultiplier = Mathf.Max(SpeedMultiplier, 0.1f);

                // Trigger all stat change events
                if (IsOwner)
                {
                    GameEvents.TriggerActiveBuffsChanged(_activeBuffs);
                    GameEvents.TriggerSpeedChange(MoveSpeed);
                    GameEvents.TriggerDamageChanged(Damage);
                    GameEvents.TriggerMoveSpeedChanged(MoveSpeed);
                    GameEvents.TriggerAttackSpeedChanged(AttackSpeed);
                    GameEvents.TriggerArmorChanged(Armor);
                }
                Debug.Log(MoveSpeed);
            }
            else
            {
                Debug.Log($"[Buff] {buff.Name} is already active on {gameObject.name}.");
            }
        }



        public bool IsInvincible()
        {
            var skill = GetComponent<Player.PlayerSkillE>();
            return skill != null && skill.IsInvincible();
        }

        public void ResetMultipliers()
        {
            DamageMultiplier = 1f;
            SpeedMultiplier = 1f;
            AttackSpeedMultiplier = 1f;
            ArmorBonus = 0f;
            BonusGoldPerRound = 0;
            Debug.Log($"[Stats] Multipliers reset for {gameObject.name}.");
        }

        public void TriggerStatsRefresh()
        {
            if (IsOwner)
            {
                GameEvents.TriggerDamageChanged(Damage);
                GameEvents.TriggerMoveSpeedChanged(MoveSpeed);
                GameEvents.TriggerAttackSpeedChanged(AttackSpeed);
                GameEvents.TriggerArmorChanged(Armor);
                Debug.Log($"[Stats] Stats refresh triggered for {gameObject.name}.");
            }
        }

        public void TriggerExtraHPRefresh()
        {
            Debug.Log($"[ExtraHP] TriggerExtraHPRefresh called, IsOwner: {IsOwner}");
            
            if (IsOwner)
            {
                // Calculate Extra HP percentage for UI
                float extraHPPercent = _maxExtraHP > 0 ? (float)_currentExtraHP.Value / _maxExtraHP : 0f;
                Debug.Log($"[ExtraHP] Calculated percentage: {extraHPPercent} ({_currentExtraHP.Value}/{_maxExtraHP})");
                
                // Try multiple ways to find WorldPhaseHealthUI
                var healthUI = GetComponentInChildren<PlayerStateMachine.WorldPhaseHealthUI>();
                if (healthUI == null)
                {
                    Debug.Log("[ExtraHP] Not found in children, trying in parent...");
                    healthUI = GetComponentInParent<PlayerStateMachine.WorldPhaseHealthUI>();
                }
                if (healthUI == null)
                {
                    Debug.Log("[ExtraHP] Not found in parent, searching in scene...");
                    healthUI = FindObjectOfType<PlayerStateMachine.WorldPhaseHealthUI>();
                }
                
                if (healthUI != null)
                {
                    Debug.Log($"[ExtraHP] Found WorldPhaseHealthUI on: {healthUI.gameObject.name}");
                    healthUI.SetExtraHP(extraHPPercent);
                    Debug.Log($"[ExtraHP] UI updated: {_currentExtraHP.Value}/{_maxExtraHP} ({extraHPPercent:P1}) = {extraHPPercent * 100f}%");
                }
                else
                {
                    Debug.LogWarning($"[ExtraHP] WorldPhaseHealthUI not found anywhere for {gameObject.name}");
                    
                    // List all WorldPhaseHealthUI in scene for debugging
                    var allHealthUIs = FindObjectsOfType<PlayerStateMachine.WorldPhaseHealthUI>();
                    Debug.Log($"[ExtraHP] Found {allHealthUIs.Length} WorldPhaseHealthUI components in scene:");
                    foreach (var ui in allHealthUIs)
                    {
                        Debug.Log($"  - {ui.gameObject.name} (parent: {ui.transform.parent?.name ?? "none"})");
                    }
                }
            }
            else
            {
                Debug.Log("[ExtraHP] Not owner, skipping UI update");
            }
        }

        private IEnumerator UpdateScoreOnServer(int scoreToAdd)
        {
            // Use the stored username for this specific player instance
            if (string.IsNullOrEmpty(_playerUsername))
            {
                Debug.LogWarning($"[PlayerStats] No username stored for player {OwnerClientId}, cannot update score on server");
                yield break;
            }

            Debug.Log($"[PlayerStats] Using stored username for score update: '{_playerUsername}' (Player {OwnerClientId})");

            // Get API base URL from LoginRegisterManager
            string apiUrl = "";
            if (LoginRegisterManager.Instance != null)
            {
                // Construct API URL manually using the same logic as LoginRegisterManager
                string serverIP = LoginRegisterManager.Instance.GetServerIP();
                apiUrl = $"https://{serverIP}:7170/api/User/add-score";
            }
            else
            {
                Debug.LogWarning("[PlayerStats] LoginRegisterManager.Instance not found, cannot update score on server");
                yield break;
            }

            // Create request data
            ScoreUpdateRequest scoreData = new ScoreUpdateRequest
            {
                username = _playerUsername,
                score = scoreToAdd
            };

            string jsonData = JsonUtility.ToJson(scoreData);
            Debug.Log($"[PlayerStats] Sending score update to server: {jsonData}");

            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                // Add certificate handler for SSL bypass
                request.certificateHandler = new AcceptAllCertificatesSignedHandler();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[PlayerStats] Score update successful: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.LogError($"[PlayerStats] Score update failed: {request.error}");
                }
            }
        }
    }

    // Data class for score update API request
    [System.Serializable]
    public class ScoreUpdateRequest
    {
        public string username;
        public float score;
    }
}
