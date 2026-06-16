using System.Collections.Generic;
using UnityEngine;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "WeaponLibrary", menuName = "Game/Weapons/Weapon Library")]
    public class WeaponLibrary : ScriptableObject
    {
        [SerializeField] private List<WeaponDefinition> weapons = new();

        public IReadOnlyList<WeaponDefinition> Weapons => weapons;
    }
}
