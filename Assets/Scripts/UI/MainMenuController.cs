using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    [DisallowMultipleComponent]
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite enemySprite;
        [SerializeField] private Sprite weaponSprite;

        private void Awake()
        {
            EnsureEventSystem();
            BuildMenu();
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            GameObject es = new("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private void BuildMenu()
        {
            GameObject canvasObject = new("MainMenuCanvas");

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();

            // Dark dungeon background
            CreateImage(canvasObject.transform, "Background", new Color(0.05f, 0.04f, 0.08f));

            // Vignette overlay
            GameObject vignette = new("Vignette");
            vignette.transform.SetParent(canvasObject.transform, false);
            Image vigImg = vignette.AddComponent<Image>();
            vigImg.color = new Color(0f, 0f, 0f, 0.55f);
            vigImg.raycastTarget = false;
            Stretch(vigImg.rectTransform);

            // Left player sprite
            if (playerSprite != null)
            {
                GameObject playerGo = new("PlayerDecor");
                playerGo.transform.SetParent(canvasObject.transform, false);
                Image img = playerGo.AddComponent<Image>();
                img.sprite = playerSprite;
                img.preserveAspect = true;
                img.color = new Color(1f, 1f, 1f, 0.9f);
                img.raycastTarget = false;
                SetRect(img.rectTransform, new Vector2(0.18f, 0.5f), new Vector2(0f, -60f), new Vector2(220f, 220f));
            }

            // Right enemy sprite (flipped)
            if (enemySprite != null)
            {
                GameObject enemyGo = new("EnemyDecor");
                enemyGo.transform.SetParent(canvasObject.transform, false);
                Image img = enemyGo.AddComponent<Image>();
                img.sprite = enemySprite;
                img.preserveAspect = true;
                img.color = new Color(0.8f, 0.3f, 0.3f, 0.9f);
                img.raycastTarget = false;
                img.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                SetRect(img.rectTransform, new Vector2(0.82f, 0.5f), new Vector2(0f, -60f), new Vector2(220f, 220f));
            }

            // Divider line above title
            GameObject topLine = new("TopLine");
            topLine.transform.SetParent(canvasObject.transform, false);
            Image topLineImg = topLine.AddComponent<Image>();
            topLineImg.color = new Color(0.95f, 0.75f, 0.2f, 0.6f);
            topLineImg.raycastTarget = false;
            SetRect(topLineImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(860f, 3f));

            // Title
            TextMeshProUGUI title = CreateText(canvasObject.transform, "Title", "Austral Survivors", 100, FontStyles.Bold, TextAlignmentOptions.Center);
            title.color = new Color(0.95f, 0.82f, 0.3f);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(1100f, 130f));

            // Subtitle with weapon icon
            string subtitleText = weaponSprite != null ? "⚔  Survive the Horde  ⚔" : "Survive the Horde";
            TextMeshProUGUI subtitle = CreateText(canvasObject.transform, "Subtitle", subtitleText, 32, FontStyles.Italic, TextAlignmentOptions.Center);
            subtitle.color = new Color(0.75f, 0.65f, 0.5f, 0.85f);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(800f, 50f));

            // Divider line below subtitle
            GameObject botLine = new("BottomLine");
            botLine.transform.SetParent(canvasObject.transform, false);
            Image botLineImg = botLine.AddComponent<Image>();
            botLineImg.color = new Color(0.95f, 0.75f, 0.2f, 0.6f);
            botLineImg.raycastTarget = false;
            SetRect(botLineImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(860f, 3f));

            // Weapon icon next to button
            if (weaponSprite != null)
            {
                GameObject wepGo = new("WeaponIcon");
                wepGo.transform.SetParent(canvasObject.transform, false);
                Image img = wepGo.AddComponent<Image>();
                img.sprite = weaponSprite;
                img.preserveAspect = true;
                img.color = new Color(0.95f, 0.82f, 0.3f, 0.9f);
                img.raycastTarget = false;
                SetRect(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(56f, 56f));
            }

            // Start button
            CreateButton(canvasObject.transform, "START GAME", new Vector2(0f, -140f), () =>
            {
                SceneManager.LoadScene("SampleScene");
            });

            // Version hint
            TextMeshProUGUI hint = CreateText(canvasObject.transform, "Hint", "Press START GAME to begin", 22, FontStyles.Normal, TextAlignmentOptions.Center);
            hint.color = new Color(0.5f, 0.45f, 0.4f, 0.7f);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(600f, 40f));
        }

        private void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new(label);
            buttonObject.transform.SetParent(parent, false);

            // Outer border
            Image border = buttonObject.AddComponent<Image>();
            border.color = new Color(0.95f, 0.75f, 0.2f, 0.9f);
            SetRect(border.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(364f, 84f));

            // Inner fill
            GameObject inner = new("Inner");
            inner.transform.SetParent(buttonObject.transform, false);
            Image fill = inner.AddComponent<Image>();
            fill.color = new Color(0.1f, 0.08f, 0.15f, 0.97f);
            SetRect(fill.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(356f, 76f));

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = border;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.95f, 0.75f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(1f, 0.92f, 0.4f, 1f);
            colors.pressedColor = new Color(0.6f, 0.45f, 0.1f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            button.onClick.AddListener(onClick);

            TextMeshProUGUI text = CreateText(inner.transform, "Label", label, 34, FontStyles.Bold, TextAlignmentOptions.Center);
            text.color = new Color(0.95f, 0.82f, 0.3f);
            Stretch(text.rectTransform);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Stretch(img.rectTransform);
            return img;
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
