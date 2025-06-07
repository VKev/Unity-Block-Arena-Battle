using UnityEngine;
using System.Collections.Generic;
using buffSystem;
using System; // <--- ADD THIS LINE HERE

namespace playerStat
{
    public class PlayerBaseStats : MonoBehaviour
    {
        // === Base stats ===
        [SerializeField] private int _maxHP = 100; // Backing field
        public int MaxHP => _maxHP; // Read-only public property

        [SerializeField] private int _currentHP; // Backing field
        public int CurrentHP => _currentHP; // Read-only public property

        [SerializeField] private int _baseDamage = 20; // Backing field
        public int BaseDamage => _baseDamage; // Read-only public property

        [SerializeField] private float _baseMoveSpeed = 5f; // Backing field
        public float BaseMoveSpeed => _baseMoveSpeed; // Read-only public property

        [SerializeField] private int _gold = 0; // Backing field
        public int Gold => _gold; // Read-only public property

        // === Buff-influenced multipliers ===
        [SerializeField] public float DamageMultiplier { get; set; } = 1f;
        [SerializeField] public float SpeedMultiplier { get; set; } = 1f;
        [SerializeField] public int BonusGoldPerRound { get; set; } = 0;

        // === Runtime calculated stats ===
        // These cannot be serialized directly because they are computed properties.
        // You can display them in the Inspector using a custom editor or just calculate them at runtime.
        [SerializeField] public int Damage => Mathf.RoundToInt(_baseDamage * DamageMultiplier);
        [SerializeField] public float MoveSpeed => _baseMoveSpeed * SpeedMultiplier;

        // === Active buffs ===
        [SerializeField] private List<Buff> _activeBuffs = new List<Buff>(); // Backing field
        public List<Buff> ActiveBuffs => _activeBuffs; // Read-only public property

        private void Awake()
        {
            _currentHP = _maxHP;
        }

        // --- THIS IS THE KEY CHANGE for Pub/Sub: Subscribing ---
        private void OnEnable()
        {
            // When this PlayerBaseStats object becomes active,
            // it 'tunes in' to the OnBuffChosen event.
            GameEvents.OnBuffChosen += HandleBuffChosen;
            Debug.Log("PlayerBaseStats subscribed to OnBuffChosen event.");
            GameEvents.OnChestCollected +=AddGold;
        }

        private void OnDisable()
        {
            // When this PlayerBaseStats object is disabled or destroyed,
            // it 'tunes out' from the OnBuffChosen event. This prevents errors.
            GameEvents.OnBuffChosen -= HandleBuffChosen;
            Debug.Log("PlayerBaseStats unsubscribed from OnBuffChosen event.");
            GameEvents.OnChestCollected -=  AddGold;
            Debug.Log("PlayerBaseStats unsubscribed from OnChestCollected event.");
        }

        // This is the method that will be called automatically when OnBuffChosen is triggered.
        private void HandleBuffChosen(object sender, BuffChosenEventArgs e)
        {
            Buff chosenBuff = e.ChosenBuff;
            ApplyBuff(chosenBuff); // Apply the buff to THIS PlayerBaseStats instance
            Debug.Log($"[PlayerBaseStats] Received and applied buff via event: {chosenBuff.Name}");
        }


        // === Gold methods ===
        public void AddGold(int amount)
        {
            _gold += amount;
            Debug.Log($"[Gold] +{amount} -> Total: {_gold}");
        }

        public void SpendGold(int amount)
        {
            _gold = Mathf.Max(0, _gold - amount);
            Debug.Log($"[Gold] Spent {amount} -> Total: {_gold}");
        }

        public void AddEndRoundGold()
        {
            AddGold(5 + BonusGoldPerRound); // Base 5 + bonus
        }

        // === Health methods ===
        public void TakeDamage(int amount)
        {
            _currentHP = Mathf.Max(0, _currentHP - amount);
            Debug.Log($"[HP] Took {amount} damage -> CurrentHP: {_currentHP}");
        }

        public void Heal(int amount)
        {
            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
            Debug.Log($"[HP] Healed {amount} -> CurrentHP: {_currentHP}");
        }

        // === Buff application ===
        public void ApplyBuff(Buff buff)
        {
            if (!_activeBuffs.Contains(buff))
            {
                _activeBuffs.Add(buff);
                buff.ApplyEffect?.Invoke(this);
                Debug.Log($"[Buff] Applied: {buff.Name} to {gameObject.name}");
            }
            else
            {
                Debug.Log($"[Buff] {buff.Name} is already active on {gameObject.name}.");
            }
        }

        public void ResetMultipliers()
        {
            DamageMultiplier = 1f;
            SpeedMultiplier = 1f;
            BonusGoldPerRound = 0;
            Debug.Log($"[Stats] Multipliers reset for {gameObject.name}.");
        }
    }
}