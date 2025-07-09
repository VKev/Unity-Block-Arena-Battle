using Player;
using UnityEngine;
using UnityEngine.UI;

namespace Skill
{
    public class SkillSlotUI : MonoBehaviour
    {
        public GameObject skillPanel;
        public SkillSlot[] skillSlots;
        public Sprite emptySlotSprite;
        public Sprite speedBoostSprite;
        public Sprite damageSprite;
        public Sprite pushBackSprite;
        public Sprite trapSprite;
        private PlayerSkillE playerSkill;

        void Start()
        {
            skillPanel.SetActive(false);
            
            PlayerSkillE.OnPlayerSkillSpawned += RegisterPlayer;
            
            // Subscribe to phase changes
            NetworkCountdownManager.OnPhaseChanged += OnPhaseChanged;
            
            // Set initial visibility based on current phase
            UpdateSkillPanelVisibility();
        }


        private void OnDestroy()
        {
            if (playerSkill != null)
            {
                playerSkill.networkOrbSlots.OnListChanged -= OnOrbSlotsChanged;
            }
            PlayerSkillE.OnPlayerSkillSpawned -= RegisterPlayer;
            NetworkCountdownManager.OnPhaseChanged -= OnPhaseChanged;
        }

        void OnOrbSlotsChanged(Unity.Netcode.NetworkListEvent<int> changeEvent)
        {
            UpdateUI();
        }

        void UpdateUI()
        {
            if (playerSkill == null)
            {
                Debug.LogWarning("PlayerSkillE not found!");
                return;
            }

            Debug.Log("NetworkOrbSlots Count: " + playerSkill.networkOrbSlots.Count);
            for (int i = 0; i < playerSkill.networkOrbSlots.Count; i++)
            {
                Debug.Log($"Slot {i}: {(SkillType)playerSkill.networkOrbSlots[i]}");
            }

            for (int i = 0; i < skillSlots.Length; i++)
            {
                skillSlots[i].iconImage.sprite = emptySlotSprite;
                skillSlots[i].iconImage.enabled = false;
            }

            for (int i = 0; i < playerSkill.networkOrbSlots.Count; i++)
            {
                if (i >= skillSlots.Length) break;

                SkillType skillType = (SkillType)playerSkill.networkOrbSlots[i];
                skillSlots[i].iconImage.sprite = GetSkillSprite(skillType);
                skillSlots[i].iconImage.enabled = true;
            }
        }


        Sprite GetSkillSprite(SkillType type)
        {
            switch (type)
            {
                case SkillType.SpeedBoost: return speedBoostSprite;
                case SkillType.DamageProjectile: return damageSprite;
                case SkillType.PushBack: return pushBackSprite;
                case SkillType.Trap: return trapSprite;
                default: return emptySlotSprite;
            }
        }


        public void RegisterPlayer(PlayerSkillE player)
        {
            if (!player.IsLocalPlayer()) return;

            playerSkill = player;
            playerSkill.OnOrbSlotsChangedEvent += UpdateUI;
            UpdateUI();
        }


        public void ShowSkillPanel()
        {
            skillPanel.SetActive(true);
        }
        
        private void OnPhaseChanged(GamePhase newPhase)
        {
            UpdateSkillPanelVisibility();
        }
        
        private void UpdateSkillPanelVisibility()
        {
            if (NetworkCountdownManager.Instance != null)
            {
                GamePhase currentPhase = NetworkCountdownManager.Instance.GetCurrentPhase();
                bool shouldShowSkillPanel = currentPhase == GamePhase.FightPhase;
                
                if (skillPanel != null)
                {
                    skillPanel.SetActive(shouldShowSkillPanel);
                    Debug.Log($"[SkillSlotUI] Skill panel visibility set to {shouldShowSkillPanel} for phase {currentPhase}");
                }
            }
            else
            {
                // Fallback - hide the panel if NetworkCountdownManager is not available
                if (skillPanel != null)
                {
                    skillPanel.SetActive(false);
                    Debug.LogWarning("[SkillSlotUI] NetworkCountdownManager.Instance is null, hiding skill panel");
                }
            }
        }
    }
}