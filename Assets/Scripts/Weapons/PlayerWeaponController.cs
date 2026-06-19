using System;
using System.Collections.Generic;
using UnityEngine;
using Weapons.Data;
using Weapons.Effects;

namespace Weapons
{
    [DisallowMultipleComponent]
    public class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private List<WeaponController> startingWeaponPrefabs = new();
        [SerializeField] private List<WeaponDefinition> startingWeaponDefinitions = new();
        [SerializeField] private WeaponLibrary weaponLibrary;
        [SerializeField] private Vector2 weaponSlotOffset = new(0.38f, -0.05f);
        [SerializeField, Min(0.05f)] private float weaponVisualScale = 0.85f;

        private readonly List<WeaponController> _equippedWeapons = new();
        private SpriteRenderer _ownerRenderer;

        public int EquippedWeaponCount => _equippedWeapons.Count;
        public IReadOnlyList<WeaponController> EquippedWeapons => _equippedWeapons;

        public event Action WeaponsChanged;

        private void Awake()
        {
            _ownerRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_ownerRenderer != null)
            {
                CombatEffects.ConfigureSorting(_ownerRenderer.sortingLayerID, _ownerRenderer.sortingOrder + 5);
            }

            _equippedWeapons.Clear();

            for (int i = 0; i < startingWeaponPrefabs.Count; i++)
            {
                TryEquip(startingWeaponPrefabs[i]);
            }

            for (int i = 0; i < startingWeaponDefinitions.Count; i++)
            {
                TryEquip(startingWeaponDefinitions[i]);
            }
        }

        private void Update()
        {
            for (int i = 0; i < _equippedWeapons.Count; i++)
            {
                WeaponController weapon = _equippedWeapons[i];
                if (weapon == null)
                {
                    continue;
                }

                weapon.TickAnimation(Time.time);

                WeaponDefinition definition = weapon.Definition;
                if (definition == null || definition.AttackBehaviour == null)
                {
                    continue;
                }

                definition.AttackBehaviour.Tick(BuildContext(i, weapon));
            }
        }

        public void RestoreFromSave(List<WeaponSaveState> savedWeapons)
        {
            if (savedWeapons == null || savedWeapons.Count == 0) return;

            WeaponLibrary lib = weaponLibrary;
            if (lib == null)
            {
                WeaponLibrary[] found = Resources.FindObjectsOfTypeAll<WeaponLibrary>();
                if (found.Length > 0) lib = found[0];
            }

            if (lib == null) { Debug.LogWarning("[Restore] No WeaponLibrary found"); return; }

            foreach (WeaponController w in _equippedWeapons)
                if (w != null) Destroy(w.gameObject);
            _equippedWeapons.Clear();

            foreach (WeaponSaveState saved in savedWeapons)
            {
                WeaponDefinition def = null;
                foreach (WeaponDefinition d in lib.Weapons)
                {
                    if (d != null && d.name == saved.definitionName) { def = d; break; }
                }
                if (def == null) continue;

                WeaponController weapon = CreateWeapon(def);
                _equippedWeapons.Add(weapon);
                weapon.RestoreState(saved.stageIndex, saved.lastHitCount);
            }

            NotifyWeaponsChanged();
        }

        public bool HasDefinitionEquipped(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            for (int i = 0; i < _equippedWeapons.Count; i++)
            {
                if (_equippedWeapons[i] != null && _equippedWeapons[i].Definition == definition)
                {
                    return true;
                }
            }

            return false;
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
            NotifyWeaponsChanged();
            return true;
        }

        public bool TryEquip(WeaponDefinition definition)
        {
            if (!CanEquip(definition))
            {
                return false;
            }

            _equippedWeapons.Add(CreateWeapon(definition));
            NotifyWeaponsChanged();
            return true;
        }

        public bool ReplaceWeaponAt(int index, WeaponDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.WeightClass == WeaponWeightClass.Heavy)
            {
                return EquipReplacingAll(definition);
            }

            if (index < 0 || index >= _equippedWeapons.Count)
            {
                return false;
            }

            WeaponController old = _equippedWeapons[index];
            _equippedWeapons.RemoveAt(index);
            if (old != null)
            {
                Destroy(old.gameObject);
            }

            _equippedWeapons.Insert(index, CreateWeapon(definition));
            NotifyWeaponsChanged();
            return true;
        }

        public bool EquipReplacingAll(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            for (int i = 0; i < _equippedWeapons.Count; i++)
            {
                if (_equippedWeapons[i] != null)
                {
                    Destroy(_equippedWeapons[i].gameObject);
                }
            }

            _equippedWeapons.Clear();
            _equippedWeapons.Add(CreateWeapon(definition));
            NotifyWeaponsChanged();
            return true;
        }

        private WeaponController CreateWeapon(WeaponDefinition definition)
        {
            GameObject weaponObject = new(definition.DisplayName);
            weaponObject.transform.SetParent(transform, false);
            weaponObject.transform.localScale = Vector3.one * weaponVisualScale;

            SpriteRenderer weaponRenderer = weaponObject.AddComponent<SpriteRenderer>();
            if (_ownerRenderer != null)
            {
                weaponRenderer.sharedMaterial = _ownerRenderer.sharedMaterial;
            }

            WeaponController weapon = weaponObject.AddComponent<WeaponController>();
            weapon.ApplyDefinition(definition);
            weapon.BindOwner(transform);
            return weapon;
        }

        private void NotifyWeaponsChanged()
        {
            LayoutWeapons();
            WeaponsChanged?.Invoke();
        }

        private void LayoutWeapons()
        {
            for (int i = 0; i < _equippedWeapons.Count; i++)
            {
                WeaponController weapon = _equippedWeapons[i];
                if (weapon == null)
                {
                    continue;
                }

                float side = i == 0 ? 1f : -1f;
                weapon.SetBaseLocalPosition(new Vector3(weaponSlotOffset.x * side, weaponSlotOffset.y, 0f));
            }
        }

        private bool HasHeavyWeaponEquipped()
        {
            for (int i = 0; i < _equippedWeapons.Count; i++)
            {
                if (_equippedWeapons[i] == null)
                {
                    continue;
                }

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
