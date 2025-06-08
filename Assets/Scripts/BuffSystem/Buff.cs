using System;
using UnityEngine;
using playerStat;
namespace buffSystem 
{
    // Enum to define the different tiers of buffs
    public enum BuffTier
    {
        Silver,
        Gold,
        Diamond,
    }
    [System.Serializable]
    public class Buff
    {
        public string Name;
        public string Description;
        public BuffTier Tier;
        public Action<PlayerBaseStats> ApplyEffect;

        
    }
}