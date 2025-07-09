using playerStat;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace buffSystem
{
    public class BuffLibrary
    {
        private static Sprite LoadIcon(string buffName)
        {
            string key = "";

            if (buffName.Contains("Armor"))
                key = "armor";
            else if (buffName.Contains("Attack Speed"))
                key = "slash";
            else if (buffName.Contains("Gold"))
                key = "money";
            else if (buffName.Contains("Damage"))
                key = "attack";
            else if (buffName.Contains("Speed"))
                key = "speed";

            string loadPath = $"BuffIcons/{key}";
            Sprite sprite = Resources.Load<Sprite>(loadPath);

            Debug.Log($" Loading icon for: {buffName}  Key: {key}  Path: {loadPath}");

            if (sprite == null)
                Debug.LogError($" Sprite not found at path: Resources/{loadPath}");
            else
                Debug.Log($" Sprite loaded successfully: {sprite.name}");

            return sprite;
        }

        public static List<Buff> GetAllBuffs()
        {
            List<Buff> allBuffs = new List<Buff>();

            allBuffs.Add(new Buff
            {
                Name = "Minor Damage Boost",
                Description = "Increases damage by 15%.",
                Tier = BuffTier.Silver,
                IconSprite = LoadIcon("Minor Damage Boost"),
                ApplyEffect = player => {
                    player.DamageMultiplier *= 1.15f;
                    Debug.Log($"{player.name} received Minor Damage Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Minor Speed Boost",
                Description = "Increases movement speed by 50%.",
                Tier = BuffTier.Silver,
                IconSprite = LoadIcon("Minor Speed Boost"),
                ApplyEffect = player => {
                    player.SpeedMultiplier *= 1.5f;
                    Debug.Log($"{player.name} received Minor Speed Boost. Multiplier: {player.SpeedMultiplier}");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Minor Gold Boost",
                Description = "Gain 2 bonus gold per round.",
                Tier = BuffTier.Silver,
                IconSprite = LoadIcon("Minor Gold Boost"),
                ApplyEffect = player => {
                    player.BonusGoldPerRound += 2;
                    GameEvents.TriggerGoldChanged(player.BonusGoldPerRound);
                    Debug.Log($"{player.name} received Minor Gold Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Minor Attack Speed Boost",
                Description = "Increases attack speed by 20%.",
                Tier = BuffTier.Silver,
                IconSprite = LoadIcon("Minor Attack Speed Boost"),
                ApplyEffect = player => {
                    player.AttackSpeedMultiplier *= 1.2f;
                    Debug.Log($"{player.name} received Minor Attack Speed Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Minor Armor Boost",
                Description = "Gain 10% damage reduction.",
                Tier = BuffTier.Silver,
                IconSprite = LoadIcon("Minor Armor Boost"),
                ApplyEffect = player => {
                    player.ArmorBonus += 10f;
                    Debug.Log($"{player.name} received Minor Armor Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Moderate Damage Boost",
                Description = "Increases damage by 30%.",
                Tier = BuffTier.Gold,
                IconSprite = LoadIcon("Moderate Damage Boost"),
                ApplyEffect = player => {
                    player.DamageMultiplier *= 1.30f;
                    Debug.Log($"{player.name} received Moderate Damage Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Moderate Speed Boost",
                Description = "Increases movement speed by 100%.",
                Tier = BuffTier.Gold,
                IconSprite = LoadIcon("Moderate Speed Boost"),
                ApplyEffect = player => {
                    player.SpeedMultiplier *= 2f;
                    Debug.Log($"{player.name} received Moderate Speed Boost. Multiplier: {player.SpeedMultiplier}");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Moderate Gold Boost",
                Description = "Gain 5 bonus gold per round.",
                Tier = BuffTier.Gold,
                IconSprite = LoadIcon("Moderate Gold Boost"),
                ApplyEffect = player => {
                    player.BonusGoldPerRound += 5;
                    GameEvents.TriggerGoldChanged(player.BonusGoldPerRound);
                    Debug.Log($"{player.name} received Moderate Gold Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Moderate Attack Speed Boost",
                Description = "Increases attack speed by 40%.",
                Tier = BuffTier.Gold,
                IconSprite = LoadIcon("Moderate Attack Speed Boost"),
                ApplyEffect = player => {
                    player.AttackSpeedMultiplier *= 1.4f;
                    Debug.Log($"{player.name} received Moderate Attack Speed Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Moderate Armor Boost",
                Description = "Gain 20% damage reduction.",
                Tier = BuffTier.Gold,
                IconSprite = LoadIcon("Moderate Armor Boost"),
                ApplyEffect = player => {
                    player.ArmorBonus += 20f;
                    Debug.Log($"{player.name} received Moderate Armor Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Major Damage Boost",
                Description = "Increases damage by 50%.",
                Tier = BuffTier.Diamond,
                IconSprite = LoadIcon("Major Damage Boost"),
                ApplyEffect = player => {
                    player.DamageMultiplier *= 2.50f;
                    Debug.Log($"{player.name} received Major Damage Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Major Speed Boost",
                Description = "Increases movement speed by 150%.",
                Tier = BuffTier.Diamond,
                IconSprite = LoadIcon("Major Speed Boost"),
                ApplyEffect = player => {
                    player.SpeedMultiplier *= 2.5f;
                    Debug.Log($"{player.name} received Major Speed Boost. Multiplier: {player.SpeedMultiplier}");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Major Gold Boost",
                Description = "Gain 10 bonus gold per round.",
                Tier = BuffTier.Diamond,
                IconSprite = LoadIcon("Major Gold Boost"),
                ApplyEffect = player => {
                    player.BonusGoldPerRound += 10;
                    GameEvents.TriggerGoldChanged(player.BonusGoldPerRound);
                    Debug.Log($"{player.name} received Major Gold Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Major Attack Speed Boost",
                Description = "Increases attack speed by 60%.",
                Tier = BuffTier.Diamond,
                IconSprite = LoadIcon("Major Attack Speed Boost"),
                ApplyEffect = player => {
                    player.AttackSpeedMultiplier *= 1.6f;
                    Debug.Log($"{player.name} received Major Attack Speed Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Major Armor Boost",
                Description = "Gain 30% damage reduction.",
                Tier = BuffTier.Diamond,
                IconSprite = LoadIcon("Major Armor Boost"),
                ApplyEffect = player => {
                    player.ArmorBonus += 30f;
                    Debug.Log($"{player.name} received Major Armor Boost.");
                }
            });

            return allBuffs;
        }

        public static List<Buff> GetBuffsByTier(BuffTier tier)
        {
            return GetAllBuffs().FindAll(buff => buff.Tier == tier);
        }
    }
}
