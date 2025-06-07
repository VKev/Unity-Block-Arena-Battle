using buffSystem;
using playerStat; // To access PlayerBaseStats
using System.Collections.Generic;
using System.Linq;
using UnityEngine; // For Debug.Log (optional)


namespace buffSystem
{
    public class BuffLibrary
    {
        public static List<Buff> GetAllBuffs()
        {
            List<Buff> allBuffs = new List<Buff>();

            // --- Silver Buffs (Tier 1) ---
            allBuffs.Add(new Buff
            {
                Name = "Minor Damage Boost",
                Description = "Increases damage by 15%.",
                Tier = BuffTier.Silver,
                ApplyEffect = (PlayerBaseStats player) => {
                    player.DamageMultiplier *= 1.15f;
                    Debug.Log($"{player.name} received Minor Damage Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Minor Speed Boost",
                Description = "Increases movement speed by 10%.",
                Tier = BuffTier.Silver,
                ApplyEffect = (player) => {
                    player.SpeedMultiplier *= 1.10f;
                    Debug.Log($"{player.name} received Minor Speed Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Minor Gold Boost",
                Description = "Gain 2 bonus gold per round.",
                Tier = BuffTier.Silver,
                ApplyEffect = (player) => {
                    player.BonusGoldPerRound += 2;
                    Debug.Log($"{player.name} received Minor Gold Boost.");
                }
            });

            // --- Gold Buffs (Tier 2) ---
            allBuffs.Add(new Buff
            {
                Name = "Moderate Damage Boost",
                Description = "Increases damage by 30%.",
                Tier = BuffTier.Gold,
                ApplyEffect = (player) => {
                    player.DamageMultiplier *= 1.30f;
                    Debug.Log($"{player.name} received Moderate Damage Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Moderate Speed Boost",
                Description = "Increases movement speed by 20%.",
                Tier = BuffTier.Gold,
                ApplyEffect = (player) => {
                    player.SpeedMultiplier *= 1.20f;
                    Debug.Log($"{player.name} received Moderate Speed Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Moderate Gold Boost",
                Description = "Gain 5 bonus gold per round.",
                Tier = BuffTier.Gold,
                ApplyEffect = (player) => {
                    player.BonusGoldPerRound += 5;
                    Debug.Log($"{player.name} received Moderate Gold Boost.");
                }
            });

            // --- Diamond Buffs (Tier 3) ---
            allBuffs.Add(new Buff
            {
                Name = "Major Damage Boost",
                Description = "Increases damage by 50%.",
                Tier = BuffTier.Diamond,
                ApplyEffect = (player) => {
                    player.DamageMultiplier *= 1.50f;
                    Debug.Log($"{player.name} received Major Damage Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Major Speed Boost",
                Description = "Increases movement speed by 30%.",
                Tier = BuffTier.Diamond,
                ApplyEffect = (player) => {
                    player.SpeedMultiplier *= 1.30f;
                    Debug.Log($"{player.name} received Major Speed Boost.");
                }
            });

            allBuffs.Add(new Buff
            {
                Name = "Major Gold Boost",
                Description = "Gain 10 bonus gold per round.",
                Tier = BuffTier.Diamond,
                ApplyEffect = (player) => {
                    player.BonusGoldPerRound += 10;
                    Debug.Log($"{player.name} received Major Gold Boost.");
                }
            });

            return allBuffs;
        }

        // This helper method filters the list of all buffs to return only those of a specific tier.
        public static List<Buff> GetBuffsByTier(BuffTier tier)
        {
            // .FindAll() returns a new list containing all elements that match the given predicate (condition)
            return GetAllBuffs().FindAll(buff => buff.Tier == tier);
        }

    }
}

