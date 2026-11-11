using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    internal static class FixedFixtureUi
    {
        internal static readonly Color Backdrop = new Color(0.07f, 0.06f, 0.09f, 0.76f);
        internal static readonly Color Face = new Color32(43, 37, 50, 255);
        internal static readonly Color Edge = new Color32(125, 190, 186, 255);
        internal static readonly Color EdgeDim = new Color32(70, 91, 94, 255);
        internal static readonly Color Accent = new Color32(143, 199, 194, 255);
        internal static readonly Color Ink = new Color32(232, 226, 207, 255);
        internal static readonly Color MutedInk = new Color32(177, 178, 173, 255);
        internal static readonly Color Track = new Color32(29, 27, 36, 255);
        internal static readonly Color ButtonFace = new Color32(54, 49, 61, 255);
        internal static readonly Color Selected = new Color32(83, 119, 115, 255);

        private static TMP_FontAsset cachedFont;
        private static bool fontResolved;

        internal static GameObject New(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        internal static void Place(GameObject gameObject, float x, float y, float width, float height)
        {
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        internal static void Pin(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static Image Box(Transform parent, string name, Color color, float x, float y,
            float width, float height, bool raycast = true)
        {
            GameObject gameObject = New(name, parent);
            Place(gameObject, x, y, width, height);
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static TextMeshProUGUI Text(Transform parent, string name, string value, float size,
            Color color, float x, float y, float width, float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject gameObject = New(name, parent);
            Place(gameObject, x, y, width, height);
            TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            TMP_FontAsset font = ResolveFont();
            if (font != null)
                text.font = font;
            return text;
        }

        internal static Button Button(Transform parent, string name, string label, Color background,
            Color foreground, float x, float y, float width, float height, Action onClick,
            float fontSize = 15f)
        {
            GameObject gameObject = New(name, parent);
            Place(gameObject, x, y, width, height);
            Image image = gameObject.AddComponent<Image>();
            image.color = background;
            Button button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Edge, 0.35f);
            colors.pressedColor = Accent;
            colors.selectedColor = Color.Lerp(background, Edge, 0.25f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            if (!string.IsNullOrEmpty(label))
                Text(gameObject.transform, "Label", label, fontSize, foreground, 4, 0, width - 8, height,
                    TextAlignmentOptions.Center);
            return button;
        }

        internal static TMP_FontAsset ResolveFont()
        {
            if (!fontResolved)
            {
                fontResolved = true;
                FontLoader loader = FontLoader.Instance;
                if (loader != null)
                    cachedFont = loader.simplifiedChineseFont;
                if (cachedFont == null)
                    Debug.LogWarning("[Skin Framework] 没有找到游戏简体中文 TMP 字体，固定设施自绘面板使用 TMP 默认字体。");
            }
            return cachedFont;
        }
    }
}
