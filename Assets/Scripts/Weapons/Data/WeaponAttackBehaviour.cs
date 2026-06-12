using UnityEngine;

namespace Weapons.Data
{
    public abstract class WeaponAttackBehaviour : ScriptableObject
    {
        public abstract void Tick(WeaponRuntimeContext context);
    }
}
