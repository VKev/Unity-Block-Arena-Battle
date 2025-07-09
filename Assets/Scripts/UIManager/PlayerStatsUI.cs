using UnityEngine;
using TMPro;
using Unity.Netcode;
using playerStat;

namespace PlayerUI
{
    public class PlayerStatsUI : MonoBehaviour
    {
        [Header("Player Stat Text Components")]
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text moveSpeedText;
        [SerializeField] private TMP_Text attackSpeedText;
        [SerializeField] private TMP_Text armorText;
        
        [Header("UI Settings")]
        [SerializeField] private bool showStatsOnStart = false;
        
        private bool statsVisible = false;

        private void Awake()
        {
            // Always start with stats hidden
            SetStatsVisibility(false);
        }
        
        private void Update()
        {
            // Check for Tab key input
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ShowStats();
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                HideStats();
            }
        }

        private void OnEnable()
        {
            // Subscribe to stat change events
            GameEvents.OnDamageChanged += UpdateDamageDisplay;
            GameEvents.OnMoveSpeedChanged += UpdateMoveSpeedDisplay;
            GameEvents.OnAttackSpeedChanged += UpdateAttackSpeedDisplay;
            GameEvents.OnArmorChanged += UpdateArmorDisplay;
            
            // Don't auto-show stats - wait for Tab key press
        }
        
        private void RequestStatsRefresh()
        {
            // Find the local player's PlayerBaseStats and trigger a refresh
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                var localClientId = NetworkManager.Singleton.LocalClientId;
                var allPlayers = FindObjectsOfType<PlayerBaseStats>();
                
                foreach (var player in allPlayers)
                {
                    if (player.OwnerClientId == localClientId && player.IsOwner)
                    {
                        player.TriggerStatsRefresh();
                        Debug.Log("[PlayerStatsUI] Requested stats refresh for local player");
                        break;
                    }
                }
            }
        }
        
        private void ShowStats()
        {
            if (!statsVisible)
            {
                SetStatsVisibility(true);
                RequestStatsRefresh(); // Refresh stats when showing
                statsVisible = true;
                Debug.Log("[PlayerStatsUI] Stats shown (Tab pressed)");
            }
        }
        
        private void HideStats()
        {
            if (statsVisible)
            {
                SetStatsVisibility(false);
                statsVisible = false;
                Debug.Log("[PlayerStatsUI] Stats hidden (Tab released)");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from stat change events
            GameEvents.OnDamageChanged -= UpdateDamageDisplay;
            GameEvents.OnMoveSpeedChanged -= UpdateMoveSpeedDisplay;
            GameEvents.OnAttackSpeedChanged -= UpdateAttackSpeedDisplay;
            GameEvents.OnArmorChanged -= UpdateArmorDisplay;
        }

        private void UpdateDamageDisplay(int damage)
        {
            if (damageText != null)
            {
                damageText.gameObject.SetActive(true);
                damageText.text = $"Damage: {damage}";
            }
        }

        private void UpdateMoveSpeedDisplay(float moveSpeed)
        {
            if (moveSpeedText != null)
            {
                moveSpeedText.gameObject.SetActive(true);
                moveSpeedText.text = $"Speed: {moveSpeed:F1}";
            }
        }

        private void UpdateAttackSpeedDisplay(float attackSpeed)
        {
            if (attackSpeedText != null)
            {
                attackSpeedText.gameObject.SetActive(true);
                attackSpeedText.text = $"Attack Speed: {attackSpeed:F2}x";
            }
        }

        private void UpdateArmorDisplay(float armor)
        {
            if (armorText != null)
            {
                armorText.gameObject.SetActive(true);
                armorText.text = $"Armor: {armor:F1}%";
            }
        }

        public void SetStatsVisibility(bool visible)
        {
            if (damageText != null) damageText.gameObject.SetActive(visible);
            if (moveSpeedText != null) moveSpeedText.gameObject.SetActive(visible);
            if (attackSpeedText != null) attackSpeedText.gameObject.SetActive(visible);
            if (armorText != null) armorText.gameObject.SetActive(visible);
        }

        public void ToggleStatsVisibility()
        {
            bool currentVisibility = damageText != null && damageText.gameObject.activeInHierarchy;
            SetStatsVisibility(!currentVisibility);
        }

        // Manual refresh method in case needed
        public void RefreshAllStats()
        {
            // This would require access to PlayerBaseStats, 
            // but the event system should handle updates automatically
            Debug.Log("[PlayerStatsUI] Manual refresh requested - stats should update via events");
        }
    }
} 