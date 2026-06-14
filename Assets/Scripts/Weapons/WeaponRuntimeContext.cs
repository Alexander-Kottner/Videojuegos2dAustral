using Enemies;
using UnityEngine;
using Weapons.Data;

namespace Weapons
{
    public readonly struct WeaponRuntimeContext
    {
        public WeaponRuntimeContext(
            PlayerWeaponController controller,
            int weaponIndex,
            WeaponController weapon,
            WeaponDefinition definition,
            Transform ownerTransform,
            float deltaTime,
            float time)
        {
            Controller = controller;
            WeaponIndex = weaponIndex;
            Weapon = weapon;
            Definition = definition;
            OwnerTransform = ownerTransform;
            DeltaTime = deltaTime;
            Time = time;
        }

        public PlayerWeaponController Controller { get; }
        public int WeaponIndex { get; }
        public WeaponController Weapon { get; }
        public WeaponDefinition Definition { get; }
        public Transform OwnerTransform { get; }
        public float DeltaTime { get; }
        public float Time { get; }

        public bool HasOwner => OwnerTransform != null;
        public Vector2 Position => HasOwner ? (Vector2)OwnerTransform.position : Vector2.zero;
        public WeaponStage Stage => Weapon != null ? Weapon.CurrentStage : null;
        public int ExtraProjectiles => Stage?.ExtraProjectiles ?? 0;

        public int ScaleDamage(int baseDamage)
        {
            float stageMultiplier = Stage?.DamageMultiplier ?? 1f;
            float bonus = Weapon != null ? Weapon.BonusDamageMultiplier : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * stageMultiplier * bonus));
        }

        public float ScaleCooldown(float baseCooldown)
        {
            float stageMultiplier = Stage?.CooldownMultiplier ?? 1f;
            float bonus = Weapon != null ? Weapon.BonusCooldownMultiplier : 1f;
            return Mathf.Max(0.05f, baseCooldown * stageMultiplier * bonus);
        }

        public float ScaleRange(float baseRange)
        {
            return baseRange * (Stage?.RangeMultiplier ?? 1f);
        }

        public float ScaleArea(float baseArea)
        {
            return baseArea * (Stage?.AreaMultiplier ?? 1f);
        }

        public bool TryUseAttack(float cooldown)
        {
            return Weapon != null && Weapon.TryUseAttack(Time, cooldown);
        }

        public bool DealDamage(EnemyHealth target, int scaledDamage, Color effectColor)
        {
            return CombatDamage.Apply(Weapon, target, scaledDamage, effectColor);
        }

        public void PlayAttackAnimation()
        {
            Weapon?.PlayAttackAnimation(Time);
        }

        public void PlayAttackPunch(Vector2 worldDirection)
        {
            Weapon?.PlayAttackPunch(worldDirection, Time);
        }

        public void PlayHitIndicator(Transform target)
        {
            Weapon?.PlayHitIndicator(target, Time);
        }
    }
}
