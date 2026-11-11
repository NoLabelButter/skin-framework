using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    internal sealed partial class ColorPickerWindow
    {        private static GameObject CreateCanvas()
        {
            GameObject canvasObject = New("SkinFrameworkColorPickerCanvas", null);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32020;
            canvas.overrideSorting = true;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            float fit = Mathf.Min(1f, (Screen.width - 24f) / PanelWidth, (Screen.height - 24f) / PanelHeight);
            scaler.scaleFactor = Mathf.Max(0.55f, fit);
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject;
        }

        private static Button AddButton(Transform parent, string id, string label, Color background,
            Color foreground, float x, float y, float width, float height, Action onClick)
        {
            GameObject buttonObject = New(id, parent);
            Place(buttonObject, x, y, width, height);
            Image image = buttonObject.AddComponent<Image>();
            image.color = background;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            if (!string.IsNullOrEmpty(label))
                AddText(buttonObject.transform, "Label", label, 16, foreground,
                    3, 0, width - 6, height, TextAlignmentOptions.Center);
            return button;
        }

        private static TextMeshProUGUI AddText(Transform parent, string id, string value, float size,
            Color color, float x, float y, float width, float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject textObject = New(id, parent);
            Place(textObject, x, y, width, height);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            TMP_FontAsset font = ResolveFont();
            if (font != null)
                text.font = font;
            return text;
        }

        private static TMP_FontAsset ResolveFont()
        {
            if (!fontResolved)
            {
                fontResolved = true;
                FontLoader loader = FontLoader.Instance;
                if (loader != null)
                    cachedFont = loader.simplifiedChineseFont;
                if (cachedFont == null)
                    UnityEngine.Debug.LogWarning("[Skin Framework] 没有找到游戏简体中文 TMP 字体，颜色面板使用 TMP 默认字体。");
            }
            return cachedFont;
        }

        private static Image Box(Transform parent, string id, Color color, bool raycast) =>
            Box(parent, id, color, raycast, 0, 0, 0, 0);

        private static Image Box(Transform parent, string id, Color color, bool raycast,
            float x, float y, float width, float height)
        {
            GameObject box = New(id, parent);
            Place(box, x, y, width, height);
            Image image = box.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static GameObject New(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.AddComponent<RectTransform>();
            if (parent != null)
                gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Place(GameObject gameObject, float x, float y, float width, float height)
        {
            RectTransform rect = gameObject.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Pin(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

    }
}
