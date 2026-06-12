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

        public bool TryUseAttack(float cooldown)
        {
            return Weapon != null && Weapon.TryUseAttack(Time, cooldown);
        }

        public void PlayAttackAnimation()
        {
            Weapon?.PlayAttackAnimation(Time);
        }

        public void PlayHitIndicator(Transform target)
        {
            Weapon?.PlayHitIndicator(target, Time);
        }
    }
}
