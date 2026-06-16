using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons
{
    public static class CombatDamage
    {
        public static bool Apply(WeaponController source, EnemyHealth target, int damage, Color effectColor)
        {
            if (target == null || !target.IsAlive || damage <= 0)
            {
                return false;
            }

            target.TakeDamage(damage);
            CombatEffects.SpawnDamageNumber(target.transform.position, damage, effectColor);

            if (!target.IsAlive && source != null)
            {
                source.NotifyLastHit();
            }

            return true;
        }
    }
}
