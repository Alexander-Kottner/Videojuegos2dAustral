using System;
using UnityEngine;

namespace Enemies
{
    [DisallowMultipleComponent]
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 30;
        [SerializeField, Min(0)] private int currentHealth = 30;
        [SerializeField] private bool resetHealthOnEnable = true;

        public event Action<int, int> HealthChanged;
        public event Action<EnemyHealth> Died;

        public int CurrentHealth => currentHealth;
        public int MaximumHealth => maximumHealth;
        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            ClampHealth();
        }

        private void OnEnable()
        {
            if (resetHealthOnEnable)
            {
                currentHealth = maximumHealth;
                HealthChanged?.Invoke(currentHealth, maximumHealth);
            }
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
