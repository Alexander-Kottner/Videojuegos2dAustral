using UnityEngine;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game/Weapons/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private WeaponRangeType rangeType = WeaponRangeType.Melee;
        [SerializeField] private WeaponWeightClass weightClass = WeaponWeightClass.Light;
        [SerializeField] private WeaponAttackBehaviour attackBehaviour;

        public WeaponRangeType RangeType => rangeType;
        public WeaponWeightClass WeightClass => weightClass;
        public WeaponAttackBehaviour AttackBehaviour => attackBehaviour;
    }
}
