using System.Collections.Generic;
using UnityEngine;
using Weapons.Data;

namespace Weapons
{
    [DisallowMultipleComponent]
    public class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private List<WeaponController> startingWeaponPrefabs = new();

        private readonly List<WeaponController> _equippedWeapons = new();

        public int EquippedWeaponCount => _equippedWeapons.Count;

        private void Awake()
        {
            _equippedWeapons.Clear();

            for (int i = 0; i < startingWeaponPrefabs.Count; i++)
            {
                TryEquip(startingWeaponPrefabs[i]);
            }
        }

        private void Update()
        {
            for (int i = 0; i < _equippedWeapons.Count; i++)
            {
                WeaponController weapon = _equippedWeapons[i];
                weapon.TickAnimation(Time.time);

                WeaponDefinition definition = weapon.Definition;
                if (definition == null || definition.AttackBehaviour == null)
                {
                    continue;
                }

                definition.AttackBehaviour.Tick(BuildContext(i, weapon));
            }
        }

        public bool CanEquip(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.WeightClass == WeaponWeightClass.Heavy)
            {
                return _equippedWeapons.Count == 0;
            }

            if (HasHeavyWeaponEquipped())
            {
                return false;
            }

            return _equippedWeapons.Count < 2;
        }

        public bool TryEquip(WeaponController weaponPrefab)
        {
            if (weaponPrefab == null || !CanEquip(weaponPrefab.Definition))
            {
                return false;
            }

            WeaponController weapon = Instantiate(weaponPrefab, transform);
            weapon.BindOwner(transform);
            _equippedWeapons.Add(weapon);
            return true;
        }

        private bool HasHeavyWeaponEquipped()
        {
            for (int i = 0; i < _equippedWeapons.Count; i++)
            {
                WeaponDefinition definition = _equippedWeapons[i].Definition;
                if (definition != null && definition.WeightClass == WeaponWeightClass.Heavy)
                {
                    return true;
                }
            }

            return false;
        }

        private WeaponRuntimeContext BuildContext(int weaponIndex, WeaponController weapon)
        {
            return new WeaponRuntimeContext(
                this,
                weaponIndex,
                weapon,
                weapon.Definition,
                transform,
                Time.deltaTime,
                Time.time);
        }
    }
}
