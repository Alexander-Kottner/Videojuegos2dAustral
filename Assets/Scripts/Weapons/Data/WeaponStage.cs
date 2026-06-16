using System;
using UnityEngine;

namespace Weapons.Data
{
    [Serializable]
    public class WeaponStage
    {
        [SerializeField] private string stageName = "Base";
        [SerializeField] private Sprite sprite;
        [SerializeField, Min(0)] private int lastHitsToReach;
        [SerializeField, Min(0.01f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float cooldownMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float rangeMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float areaMultiplier = 1f;
        [SerializeField, Min(0)] private int extraProjectiles;

        public string StageName => stageName;
        public Sprite Sprite => sprite;
        public int LastHitsToReach => lastHitsToReach;
        public float DamageMultiplier => damageMultiplier;
        public float CooldownMultiplier => cooldownMultiplier;
        public float RangeMultiplier => rangeMultiplier;
        public float AreaMultiplier => areaMultiplier;
        public int ExtraProjectiles => extraProjectiles;
    }
}
