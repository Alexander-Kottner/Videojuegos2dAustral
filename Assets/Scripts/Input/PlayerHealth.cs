using System;
using UnityEngine;

namespace Input
{
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0)] private int currentHealth = 100;

        public event Action<int, int> HealthChanged;
        public event Action<PlayerHealth> Died;

        public int CurrentHealth => currentHealth;
        public int MaximumHealth => maximumHealth;
        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            ClampHealth();
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

        public void RestoreHealth(int amount)
        {
            if (amount <= 0 || currentHealth >= maximumHealth)
            {
                return;
            }

            SetCurrentHealth(currentHealth + amount);
        }

        public void SetMaximumHealth(int value)
        {
            int previousMaximumHealth = maximumHealth;
            maximumHealth = Mathf.Max(1, value);
            currentHealth = Mathf.Clamp(currentHealth, 0, maximumHealth);

            if (maximumHealth != previousMaximumHealth)
            {
                HealthChanged?.Invoke(currentHealth, maximumHealth);
            }
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
            }
        }

        private void ClampHealth()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maximumHealth);
        }
    }
}
