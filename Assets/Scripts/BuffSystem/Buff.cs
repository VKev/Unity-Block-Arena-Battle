using System;
using UnityEngine;
using playerStat; // Make sure this namespace is correctly referenced

namespace buffSystem // This is the namespace for your buff system classes
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