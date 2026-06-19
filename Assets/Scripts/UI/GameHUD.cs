using System.Text;
using Spawning;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Weapons;
using PlayerHealthComponent = global::Input.PlayerHealth;

namespace UI
{
    /// <summary>
    /// Wave announcements, per-weapon progression readout, game over and victory screens.
    /// All UI elements are built in code at startup.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private SpawnerController spawner;
        [SerializeField] private PlayerWeaponController playerWeapons;
        [SerializeField, Min(1)] private int finalWave = 20;

        private const float WeaponStatusRefreshInterval = 0.25f;

        private PlayerHealthComponent _playerHealth;
        private Canvas _canvas;
        private TextMeshProUGUI _waveText;
        private TextMeshProUGUI _weaponStatusText;
        private GameObject _endPanel;
        private GameObject _pausePanel;
        private float _nextWeaponStatusTime;
        private bool _gameEnded;
        private bool _isPaused;

        private readonly StringBuilder _statusBuilder = new();

        private void Awake()
        {
            if (spawner == null)
            {
                spawner = FindFirstObjectByType<SpawnerController>();
            }

            if (playerWeapons == null)
            {
                playerWeapons = FindFirstObjectByType<PlayerWeaponController>();
            }

            if (playerWeapons != null)
            {
                _playerHealth = playerWeapons.GetComponentInParent<PlayerHealthComponent>();
            }

            BuildHud();
        }

        private void OnEnable()
        {
            if (spawner != null)
            {
                spawner.WaveStarted += HandleWaveStarted;
                spawner.WaveCompleted += HandleWaveCompleted;
            }

            if (_playerHealth != null)
            {
                _playerHealth.Died += HandlePlayerDied;
            }
        }

        private void OnDisable()
        {
            if (spawner != null)
            {
                spawner.WaveStarted -= HandleWaveStarted;
                spawner.WaveCompleted -= HandleWaveCompleted;
            }

            if (_playerHealth != null)
            {
                _playerHealth.Died -= HandlePlayerDied;
            }
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && !_gameEnded)
                TogglePause();

            if (_weaponStatusText == null || playerWeapons == null || Time.unscaledTime < _nextWeaponStatusTime)
                return;

            _nextWeaponStatusTime = Time.unscaledTime + WeaponStatusRefreshInterval;
            RefreshWeaponStatus();
        }

        public void TogglePause()
        {
            if (_isPaused) Resume(); else Pause();
        }

        private void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            if (_pausePanel != null) _pausePanel.SetActive(true);
        }

        private void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            if (_pausePanel != null) _pausePanel.SetActive(false);
        }

        private void HandleWaveStarted(int wave)
        {
            if (_waveText != null)
            {
                _waveText.text = $"Wave {wave}";
            }
        }

        private void HandleWaveCompleted(int wave)
        {
            if (_waveText != null)
                _waveText.text = $"Wave {wave} cleared";

            GameStateManager.SaveWaveCompleted(wave);

            if (wave == finalWave && !_gameEnded)
                ShowVictory(wave);
        }

        private void HandlePlayerDied(PlayerHealthComponent health)
        {
            if (_gameEnded)
            {
                return;
            }

            int wave = spawner != null ? spawner.CurrentWave : 0;
            ShowEndPanel($"You fell on wave {wave}", showContinue: false);
        }

        private void ShowVictory(int wave)
        {
            ShowEndPanel($"Victory! You survived all {wave} waves", showContinue: true);
        }

        private void ShowEndPanel(string message, bool showContinue)
        {
            _gameEnded = !showContinue;
            Time.timeScale = 0f;

            _endPanel = new GameObject("EndPanel");
            _endPanel.transform.SetParent(_canvas.transform, false);

            Image dim = CreateImage(_endPanel.transform, "Dim", new Color(0f, 0f, 0f, 0.75f));
            Stretch(dim.rectTransform);

            TextMeshProUGUI title = CreateText(_endPanel.transform, "Message", message, 52, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 90f));

            if (showContinue)
            {
                CreateButton(_endPanel.transform, "Keep fighting", new Vector2(0f, -20f), () =>
                {
                    Destroy(_endPanel);
                    _endPanel = null;
                    Time.timeScale = 1f;
                });
            }

            CreateButton(_endPanel.transform, "Restart", new Vector2(0f, showContinue ? -120f : -20f), () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }

        private void RefreshWeaponStatus()
        {
            _statusBuilder.Clear();

            for (int i = 0; i < playerWeapons.EquippedWeapons.Count; i++)
            {
                WeaponController weapon = playerWeapons.EquippedWeapons[i];
                if (weapon == null || weapon.Definition == null)
                {
                    continue;
                }

                if (_statusBuilder.Length > 0)
                {
                    _statusBuilder.AppendLine();
                }

                _statusBuilder.Append(weapon.Definition.DisplayName);
                _statusBuilder.Append("  Lv").Append(weapon.StageIndex + 1);

                int nextThreshold = weapon.Definition.GetLastHitsForNextStage(weapon.StageIndex);
                if (nextThreshold > 0)
                {
                    _statusBuilder.Append("  ").Append(weapon.LastHitCount).Append('/').Append(nextThreshold).Append(" kills");
                }
                else
                {
                    _statusBuilder.Append("  MAX  ").Append(weapon.LastHitCount).Append(" kills");
                }
            }

            _weaponStatusText.text = _statusBuilder.ToString();
        }

        private void BuildHud()
        {
            GameObject canvasObject = new("GameHUDCanvas");
            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();

            _waveText = CreateText(canvasObject.transform, "WaveText", "Get ready…", 40, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(_waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 60f));

            _weaponStatusText = CreateText(canvasObject.transform, "WeaponStatus", string.Empty, 24, FontStyles.Normal, TextAlignmentOptions.BottomLeft);
            _weaponStatusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _weaponStatusText.rectTransform.anchorMax = new Vector2(0f, 0f);
            _weaponStatusText.rectTransform.pivot = new Vector2(0f, 0f);
            _weaponStatusText.rectTransform.anchoredPosition = new Vector2(25f, 20f);
            _weaponStatusText.rectTransform.sizeDelta = new Vector2(700f, 130f);

            // Pause button (top-right)
            GameObject pauseBtn = new("PauseButton");
            pauseBtn.transform.SetParent(canvasObject.transform, false);
            Image pauseBtnBg = pauseBtn.AddComponent<Image>();
            pauseBtnBg.color = new Color(0.15f, 0.15f, 0.22f, 0.85f);
            SetRect(pauseBtnBg.rectTransform, new Vector2(1f, 1f), new Vector2(-50f, -40f), new Vector2(72f, 48f));
            Button pauseButtonComp = pauseBtn.AddComponent<Button>();
            pauseButtonComp.targetGraphic = pauseBtnBg;
            pauseButtonComp.onClick.AddListener(TogglePause);
            TextMeshProUGUI pauseLabel = CreateText(pauseBtn.transform, "Label", "II", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(pauseLabel.rectTransform);

            // Pause panel (hidden by default)
            _pausePanel = new GameObject("PausePanel");
            _pausePanel.transform.SetParent(canvasObject.transform, false);

            Image dim = CreateImage(_pausePanel.transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
            Stretch(dim.rectTransform);

            TextMeshProUGUI pauseTitle = CreateText(_pausePanel.transform, "PauseTitle", "PAUSED", 60, FontStyles.Bold, TextAlignmentOptions.Center);
            pauseTitle.color = new Color(0.95f, 0.82f, 0.3f);
            SetRect(pauseTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(600f, 90f));

            CreateButton(_pausePanel.transform, "Resume", new Vector2(0f, 0f), () => Resume());

            CreateButton(_pausePanel.transform, "Save & Quit to Menu", new Vector2(0f, -100f), () =>
            {
                GameStateManager.SaveCurrentState();
                Time.timeScale = 1f;
                _isPaused = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            });

            _pausePanel.SetActive(false);
        }

        private void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new(label);
            buttonObject.transform.SetParent(parent, false);

            Image background = buttonObject.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.28f, 0.95f);
            SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(340f, 80f));

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(onClick);

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
