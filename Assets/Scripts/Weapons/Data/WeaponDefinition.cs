using System.Collections.Generic;
using UnityEngine;

namespace Weapons.Data
{
    public enum WeaponSoundCategory
    {
        MeleeLight,
        MeleeHeavy,
        Projectile,
        Magic
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game/Weapons/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private WeaponRangeType rangeType = WeaponRangeType.Melee;
        [SerializeField] private WeaponWeightClass weightClass = WeaponWeightClass.Light;
        [SerializeField] private WeaponSoundCategory soundCategory = WeaponSoundCategory.MeleeLight;
        [SerializeField] private WeaponAttackBehaviour attackBehaviour;
        [SerializeField] private List<WeaponStage> stages = new();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public WeaponRangeType RangeType => rangeType;
        public WeaponWeightClass WeightClass => weightClass;
        public WeaponSoundCategory SoundCategory => soundCategory;
        public WeaponAttackBehaviour AttackBehaviour => attackBehaviour;
        public int StageCount => stages.Count;

        public Sprite Icon
        {
            get
            {
                for (int i = 0; i < stages.Count; i++)
                {
                    if (stages[i] != null && stages[i].Sprite != null)
                    {
                        return stages[i].Sprite;
                    }
                }

                return null;
            }
        }

        public WeaponStage GetStage(int index)
        {
            if (stages.Count == 0)
            {
                return null;
            }

            return stages[Mathf.Clamp(index, 0, stages.Count - 1)];
        }

        public int GetStageIndexForLastHits(int lastHits)
        {
            int index = 0;

            for (int i = 1; i < stages.Count; i++)
            {
                if (stages[i] != null && lastHits >= stages[i].LastHitsToReach)
                {
                    index = i;
                }
            }

            return index;
        }

        public int GetLastHitsForNextStage(int currentStageIndex)
        {
            int nextIndex = currentStageIndex + 1;
            if (nextIndex >= stages.Count || stages[nextIndex] == null)
            {
                return -1;
            }

            return stages[nextIndex].LastHitsToReach;
        }
    }
}
