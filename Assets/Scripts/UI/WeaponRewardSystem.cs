using System;
using System.Collections.Generic;
using Spawning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weapons;
using Weapons.Data;
using PlayerHealthComponent = global::Input.PlayerHealth;

namespace UI
{
    /// <summary>
    /// Offers reward choices (new weapons, weapon boosts, healing) after each completed wave.
    /// The UI is built in code on demand and respects the heavy/light inventory rules.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponRewardSystem : MonoBehaviour
    {
        [SerializeField] private WeaponLibrary weaponLibrary;
        [SerializeField] private PlayerWeaponController playerWeapons;
        [SerializeField] private SpawnerController spawner;
        [SerializeField, Min(1)] private int wavesPerReward = 1;
        [SerializeField, Min(1)] private int healAmount = 30;
        [SerializeField, Min(1)] private int finalWave = 20;

        private readonly List<WeaponDefinition> _candidateBuffer = new();
        private PlayerHealthComponent _playerHealth;
        private GameObject _panelRoot;

        private struct RewardOffer
        {
            public string Title;
            public string Tag;
            public string Description;
            public Sprite Icon;
            public Color IconTint;
            public Action Apply;
        }

        private void Awake()
        {
            if (playerWeapons == null)
            {
                playerWeapons = FindFirstObjectByType<PlayerWeaponController>();
            }

            if (spawner == null)
            {
                spawner = FindFirstObjectByType<SpawnerController>();
            }

            if (playerWeapons != null)
            {
                _playerHealth = playerWeapons.GetComponentInParent<PlayerHealthComponent>();
            }
        }

        private void OnEnable()
        {
            if (spawner != null)
            {
                spawner.WaveCompleted += HandleWaveCompleted;
            }
        }

        private void OnDisable()
        {
            if (spawner != null)
            {
                spawner.WaveCompleted -= HandleWaveCompleted;
            }
        }

        private void HandleWaveCompleted(int wave)
        {
            if (playerWeapons == null || weaponLibrary == null || _panelRoot != null)
            {
                return;
            }

            if (wave % wavesPerReward != 0 || wave == finalWave)
            {
                return;
            }

            if (_playerHealth != null && !_playerHealth.IsAlive)
            {
                return;
            }

            List<RewardOffer> offers = BuildOffers();
            if (offers.Count == 0)
            {
                return;
            }

            ShowPanel(wave, offers);
        }

        private List<RewardOffer> BuildOffers()
        {
            List<RewardOffer> offers = new();

            _candidateBuffer.Clear();
            IReadOnlyList<WeaponDefinition> all = weaponLibrary.Weapons;
            for (int i = 0; i < all.Count; i++)
            {
                WeaponDefinition definition = all[i];
                if (definition != null && !playerWeapons.HasDefinitionEquipped(definition))
                {
                    _candidateBuffer.Add(definition);
                }
            }

            Shuffle(_candidateBuffer);

            for (int i = 0; i < _candidateBuffer.Count && offers.Count < 2; i++)
            {
                if (TryCreateWeaponOffer(_candidateBuffer[i], out RewardOffer offer))
                {
                    offers.Add(offer);
                }
            }

            List<RewardOffer> supportOffers = BuildSupportOffers();
            Shuffle(supportOffers);

            for (int i = 0; i < supportOffers.Count && offers.Count < 3; i++)
            {
                offers.Add(supportOffers[i]);
            }

            return offers;
        }

        private bool TryCreateWeaponOffer(WeaponDefinition definition, out RewardOffer offer)
        {
            offer = default;
            string tag = $"{definition.WeightClass} • {definition.RangeType}";

            if (playerWeapons.CanEquip(definition))
            {
                offer = new RewardOffer
                {
                    Title = definition.DisplayName,
                    Tag = tag,
                    Description = definition.Description,
                    Icon = definition.Icon,
                    IconTint = Color.white,
                    Apply = () => playerWeapons.TryEquip(definition)
                };
                return true;
            }

            if (definition.WeightClass == WeaponWeightClass.Heavy)
            {
                offer = new RewardOffer
                {
                    Title = definition.DisplayName,
                    Tag = tag,
                    Description = $"{definition.Description}\n<color=#FFB36B>Replaces ALL current weapons.</color>",
                    Icon = definition.Icon,
                    IconTint = Color.white,
                    Apply = () => playerWeapons.EquipReplacingAll(definition)
                };
                return true;
            }

            int replaceIndex = FindReplaceIndex();
            if (replaceIndex < 0)
            {
                return false;
            }

            WeaponController replaced = playerWeapons.EquippedWeapons[replaceIndex];
            string replacedName = replaced != null && replaced.Definition != null
                ? replaced.Definition.DisplayName
                : "current weapon";

            offer = new RewardOffer
            {
                Title = definition.DisplayName,
                Tag = tag,
                Description = $"{definition.Description}\n<color=#FFB36B>Replaces {replacedName}.</color>",
                Icon = definition.Icon,
                IconTint = Color.white,
                Apply = () => playerWeapons.ReplaceWeaponAt(replaceIndex, definition)
            };
            return true;
        }

        private int FindReplaceIndex()
        {
            int index = -1;
            int lowestStage = int.MaxValue;

            for (int i = 0; i < playerWeapons.EquippedWeapons.Count; i++)
            {
                WeaponController weapon = playerWeapons.EquippedWeapons[i];
                if (weapon == null)
                {
                    continue;
                }

                if (weapon.StageIndex < lowestStage)
                {
                    lowestStage = weapon.StageIndex;
                    index = i;
                }
            }

            return index;
        }

        private List<RewardOffer> BuildSupportOffers()
        {
            List<RewardOffer> offers = new();

            for (int i = 0; i < playerWeapons.EquippedWeapons.Count; i++)
            {
                WeaponController weapon = playerWeapons.EquippedWeapons[i];
                if (weapon == null || weapon.Definition == null)
                {
                    continue;
                }

                WeaponController captured = weapon;
                string weaponName = weapon.Definition.DisplayName;

                offers.Add(new RewardOffer
                {
                    Title = $"Sharpen {weaponName}",
                    Tag = "Upgrade",
                    Description = $"{weaponName} deals 25% more damage.",
                    Icon = weapon.Definition.Icon,
                    IconTint = new Color(1f, 0.85f, 0.6f),
                    Apply = () => captured.AddDamageBoost(1.25f)
                });

                offers.Add(new RewardOffer
                {
                    Title = $"Hone {weaponName}",
                    Tag = "Upgrade",
                    Description = $"{weaponName} attacks 15% faster.",
                    Icon = weapon.Definition.Icon,
                    IconTint = new Color(0.7f, 0.9f, 1f),
                    Apply = () => captured.AddCooldownBoost(0.85f)
                });
            }

            offers.Add(new RewardOffer
            {
                Title = "Field Rations",
                Tag = "Recovery",
                Description = $"Restore {healAmount} health.",
                Icon = null,
                IconTint = new Color(0.55f, 1f, 0.55f),
                Apply = () =>
                {
                    if (_playerHealth != null)
                    {
                        _playerHealth.RestoreHealth(healAmount);
                    }
                }
            });

            return offers;
        }

        private void ShowPanel(int wave, List<RewardOffer> offers)
        {
            Time.timeScale = 0f;

            _panelRoot = new GameObject("WeaponRewardPanel");
            Canvas canvas = _panelRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = _panelRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _panelRoot.AddComponent<GraphicRaycaster>();

            Image dim = CreateImage(_panelRoot.transform, "Dim", new Color(0f, 0f, 0f, 0.65f));
            Stretch(dim.rectTransform);

            TextMeshProUGUI title = CreateText(_panelRoot.transform, "Title",
                $"Wave {wave} cleared — choose a reward", 44, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1400f, 70f));

            float cardSpacing = 420f;
            float startX = -cardSpacing * (offers.Count - 1) * 0.5f;

            for (int i = 0; i < offers.Count; i++)
            {
                CreateOfferCard(offers[i], new Vector2(startX + i * cardSpacing, -40f));
            }
        }

        private void CreateOfferCard(RewardOffer offer, Vector2 position)
        {
            GameObject card = new("OfferCard");
            card.transform.SetParent(_panelRoot.transform, false);

            Image background = card.AddComponent<Image>();
            background.color = new Color(0.13f, 0.13f, 0.18f, 0.97f);
            SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(380f, 460f));

            Button button = card.AddComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.35f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            button.colors = colors;

            Image icon = CreateImage(card.transform, "Icon", offer.IconTint);
            icon.sprite = offer.Icon != null ? offer.Icon : Weapons.Effects.CombatEffects.SoftCircleSprite;
            icon.preserveAspect = true;
            SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(130f, 130f));

            TextMeshProUGUI titleText = CreateText(card.transform, "Name", offer.Title, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(350f, 60f));

            TextMeshProUGUI tagText = CreateText(card.transform, "Tag", offer.Tag, 22, FontStyles.Normal, TextAlignmentOptions.Center);
            tagText.color = new Color(1f, 0.85f, 0.5f);
            SetRect(tagText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -255f), new Vector2(350f, 35f));

            TextMeshProUGUI descriptionText = CreateText(card.transform, "Description", offer.Description, 22, FontStyles.Normal, TextAlignmentOptions.Top);
            SetRect(descriptionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(330f, 150f));

            Action apply = offer.Apply;
            button.onClick.AddListener(() => ChooseOffer(apply));
        }

        private void ChooseOffer(Action apply)
        {
            apply?.Invoke();
            ClosePanel();
        }

        private void ClosePanel()
        {
            if (_panelRoot != null)
            {
                Destroy(_panelRoot);
                _panelRoot = null;
            }

            Time.timeScale = 1f;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
            rect.pivot = new Vector2(0.5f, anchor.y);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
