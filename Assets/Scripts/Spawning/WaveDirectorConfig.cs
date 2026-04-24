using System;
using System.Collections.Generic;
using Enemies.Data;
using UnityEngine;

namespace Spawning
{
    [CreateAssetMenu(fileName = "WaveDirectorConfig", menuName = "Game/Spawning/Wave Director Config")]
    public class WaveDirectorConfig : ScriptableObject
    {
        [Serializable]
        public struct RoleWeightModifier
        {
            public EnemySpawnRole role;
            public AnimationCurve weightByWave;

            public float Evaluate(int wave)
            {
                if (weightByWave == null || weightByWave.length == 0)
                {
                    return 1f;
                }

                return Mathf.Max(0f, weightByWave.Evaluate(wave));
            }
        }

        [Header("Roster")]
        [SerializeField] private List<EnemyDefinition> enemyRoster = new();

        [Header("Wave Flow")]
        [SerializeField, Min(0f)] private float initialDelay = 2f;
        [SerializeField, Min(0f)] private float intermissionDuration = 4f;
        [SerializeField, Min(1f)] private float startingWaveDuration = 25f;
        [SerializeField, Min(0f)] private float waveDurationPerWave = 1.5f;
        [SerializeField] private AnimationCurve waveDurationMultiplierByWave = AnimationCurve.Linear(1f, 1f, 50f, 1.5f);

        [Header("Budget")]
        [SerializeField, Min(1)] private int startingBudget = 6;
        [SerializeField, Min(0)] private int budgetPerWave = 2;
        [SerializeField] private AnimationCurve budgetMultiplierByWave = AnimationCurve.Linear(1f, 1f, 50f, 3.5f);

        [Header("Spawn Cadence")]
        [SerializeField, Min(0.05f)] private float startingSpawnInterval = 1.25f;
        [SerializeField, Min(0f)] private float spawnIntervalReductionPerWave = 0.02f;
        [SerializeField] private AnimationCurve spawnIntervalMultiplierByWave = AnimationCurve.Linear(1f, 1f, 50f, 0.45f);
        [SerializeField, Min(1)] private int startingMaxConcurrent = 6;
        [SerializeField, Min(0)] private int maxConcurrentPerWave = 1;
        [SerializeField] private AnimationCurve maxConcurrentMultiplierByWave = AnimationCurve.Linear(1f, 1f, 50f, 2f);

        [Header("Role Cadence")]
        [SerializeField, Min(1)] private int eliteUnlockWave = 5;
        [SerializeField, Min(1)] private int eliteWaveInterval = 5;
        [SerializeField, Min(1)] private int bossUnlockWave = 10;
        [SerializeField, Min(1)] private int bossWaveInterval = 10;
        [SerializeField] private List<RoleWeightModifier> roleWeightModifiers = new();

        public IReadOnlyList<EnemyDefinition> EnemyRoster => enemyRoster;
        public float InitialDelay => Mathf.Max(0f, initialDelay);
        public float IntermissionDuration => Mathf.Max(0f, intermissionDuration);

        public int GetBudgetForWave(int wave)
        {
            float linearBudget = startingBudget + Mathf.Max(0, wave - 1) * budgetPerWave;
            return Mathf.Max(1, Mathf.RoundToInt(linearBudget * EvaluateCurve(budgetMultiplierByWave, wave, 1f)));
        }

        public float GetWaveDuration(int wave)
        {
            float duration = startingWaveDuration + Mathf.Max(0, wave - 1) * waveDurationPerWave;
            return Mathf.Max(1f, duration * EvaluateCurve(waveDurationMultiplierByWave, wave, 1f));
        }

        public float GetSpawnInterval(int wave)
        {
            float reducedInterval = startingSpawnInterval - Mathf.Max(0, wave - 1) * spawnIntervalReductionPerWave;
            return Mathf.Max(0.05f, reducedInterval * EvaluateCurve(spawnIntervalMultiplierByWave, wave, 1f));
        }

        public int GetMaxConcurrent(int wave)
        {
            float concurrent = startingMaxConcurrent + Mathf.Max(0, wave - 1) * maxConcurrentPerWave;
            return Mathf.Max(1, Mathf.RoundToInt(concurrent * EvaluateCurve(maxConcurrentMultiplierByWave, wave, 1f)));
        }

        public bool IsRoleAllowed(EnemySpawnRole role, int wave)
        {
            switch (role)
            {
                case EnemySpawnRole.Elite:
                    return IsCadenceUnlocked(wave, eliteUnlockWave, eliteWaveInterval);
                case EnemySpawnRole.Boss:
                    return IsCadenceUnlocked(wave, bossUnlockWave, bossWaveInterval);
                default:
                    return true;
            }
        }

        public float GetRoleWeightMultiplier(EnemySpawnRole role, int wave)
        {
            for (int i = 0; i < roleWeightModifiers.Count; i++)
            {
                if (roleWeightModifiers[i].role == role)
                {
                    return roleWeightModifiers[i].Evaluate(wave);
                }
            }

            return 1f;
        }

        private static bool IsCadenceUnlocked(int wave, int unlockWave, int interval)
        {
            if (wave < unlockWave)
            {
                return false;
            }

            if (interval <= 1)
            {
                return true;
            }

            return (wave - unlockWave) % interval == 0;
        }

        private static float EvaluateCurve(AnimationCurve curve, int wave, float fallback)
        {
            if (curve == null || curve.length == 0)
            {
                return fallback;
            }

            return Mathf.Max(0f, curve.Evaluate(wave));
        }
    }
}
