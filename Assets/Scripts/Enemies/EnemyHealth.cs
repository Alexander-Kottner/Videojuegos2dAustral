using System;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    [DisallowMultipleComponent]
    public class EnemyHealth : MonoBehaviour
    {
        private static readonly List<EnemyHealth> AliveList = new();

        public static IReadOnlyList<EnemyHealth> AliveEnemies => AliveList;

        [SerializeField, Min(1)] private int maximumHealth = 30;
        [SerializeField, Min(0)] private int currentHealth = 30;
        [SerializeField] private bool resetHealthOnEnable = true;

        private int _baseMaximumHealth;
        private bool _baseHealthCaptured;

        public event Action<int, int> HealthChanged;
        public event Action<EnemyHealth> Died;

        public int CurrentHealth => currentHealth;
        public int MaximumHealth => maximumHealth;
        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            CaptureBaseHealth();
            ClampHealth();
        }

        private void OnEnable()
        {
            if (resetHealthOnEnable)
            {
                currentHealth = maximumHealth;
                HealthChanged?.Invoke(currentHealth, maximumHealth);
            }

            if (!AliveList.Contains(this))
            {
                AliveList.Add(this);
            }
        }

        private void OnDisable()
        {
            AliveList.Remove(this);
        }

        public void ApplyHealthMultiplier(float multiplier)
        {
            CaptureBaseHealth();
            maximumHealth = Mathf.Max(1, Mathf.RoundToInt(_baseMaximumHealth * Mathf.Max(0.01f, multiplier)));
            currentHealth = maximumHealth;
            HealthChanged?.Invoke(currentHealth, maximumHealth);
        }

        private void CaptureBaseHealth()
        {
            if (_baseHealthCaptured)
            {
                return;
            }

            _baseMaximumHealth = Mathf.Max(1, maximumHealth);
            _baseHealthCaptured = true;
        }

        private void OnValidate()
        {
            ClampHealth();
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || currentHealth <= 0)
            {
                return;
            }

            SetCurrentHealth(currentHealth - amount);
        }

        private void SetCurrentHealth(int value)
        {
            int previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(value, 0, maximumHealth);

            if (currentHealth == previousHealth)
            {
                return;
            }

            HealthChanged?.Invoke(currentHealth, maximumHealth);

            if (currentHealth == 0)
            {
                AudioManager.PlayEnemyDie();
                Died?.Invoke(this);
                gameObject.SetActive(false);
            }
        }

        private void ClampHealth()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maximumHealth);
        }
    }
}
