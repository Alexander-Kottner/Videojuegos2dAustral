using TMPro;
using UnityEngine;
using PlayerHealthComponent = global::Input.PlayerHealth;

namespace UI
{
    [DisallowMultipleComponent]
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealthComponent playerHealth;
        [SerializeField] private RectTransform health;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private string playerTag = "Player";

        private Vector3 _healthScale;
        private bool _isSubscribed;

        private void Awake()
        {
            ResolveReferences();
            CacheHealthScale();
        }

        private void OnEnable()
        {
            Bind(playerHealth != null ? playerHealth : FindPlayerHealth());
        }

        private void Start()
        {
            if (playerHealth == null)
            {
                Bind(FindPlayerHealth());
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null && _isSubscribed)
            {
                playerHealth.HealthChanged -= UpdateHealth;
                _isSubscribed = false;
            }
        }

        public void Bind(PlayerHealthComponent healthSource)
        {
            if (playerHealth == healthSource)
            {
                Subscribe();
                UpdateHealthBar();
                return;
            }

            if (playerHealth != null && _isSubscribed)
            {
                playerHealth.HealthChanged -= UpdateHealth;
                _isSubscribed = false;
            }

            playerHealth = healthSource;
            Subscribe();

            UpdateHealthBar();
        }

        private void Subscribe()
        {
            if (playerHealth == null || _isSubscribed)
            {
                return;
            }

            playerHealth.HealthChanged += UpdateHealth;
            _isSubscribed = true;
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                Transform healthTransform = transform.Find("Health");
                health = healthTransform as RectTransform;
            }

            if (healthText == null)
            {
                Transform textTransform = transform.Find("Text (TMP)");
                healthText = textTransform != null
                    ? textTransform.GetComponent<TextMeshProUGUI>()
                    : GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void CacheHealthScale()
        {
            _healthScale = health != null ? health.localScale : Vector3.one;
        }

        private PlayerHealthComponent FindPlayerHealth()
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return null;
            }

            try
            {
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                return player != null ? player.GetComponentInParent<PlayerHealthComponent>() : null;
            }
            catch (UnityException)
            {
                return null;
            }
        }

        private void UpdateHealth(int currentHealth, int maximumHealth)
        {
            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            int currentHealth = playerHealth != null ? playerHealth.CurrentHealth : 0;
            int maximumHealth = playerHealth != null ? playerHealth.MaximumHealth : 1;
            float percentage = maximumHealth > 0 ? (float)currentHealth / maximumHealth : 0f;

            if (health != null)
            {
                health.localScale = new Vector3(_healthScale.x * percentage, _healthScale.y, _healthScale.z);
            }

            if (healthText != null)
            {
                healthText.text = $"{currentHealth}/{maximumHealth}";
            }
        }
    }
}
