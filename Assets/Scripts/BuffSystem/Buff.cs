// Inside your Buff.cs file
using System;
using UnityEngine;
using playerStat;

namespace buffSystem
{
    public enum BuffTier
    {
        Silver,
        Gold,
        Diamond,
    }

    [System.Serializable]
    public class Buff
    {
        public Sprite IconSprite; 
        public string Name;
        public string Description;
        public BuffTier Tier;
        public Action<PlayerBaseStats> ApplyEffect;

    }
}