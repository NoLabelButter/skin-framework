using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    internal sealed partial class FixedFixtureWindow
    {
        private GameObject CreateCanvas()
        {
            GameObject canvasObject = FixedFixtureUi.New("SkinFrameworkFixedFixtureCanvas", null);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32021;
            canvas.overrideSorting = true;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            float fit = Mathf.Min(1f, (Screen.width - 24f) / PanelWidth, (Screen.height - 24f) / PanelHeight);
            scaler.scaleFactor = Mathf.Max(0.55f, fit);
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject;
        }

        private TMP_InputField CreateSearchField(Transform parent, string initial)
        {
            Image background = FixedFixtureUi.Box(parent, "SearchField", FixedFixtureUi.Track,
                18, 150, PanelWidth - 36, 34, true);
            searchBackgroundImage = background;
            TMP_InputField input = background.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            GameObject viewport = FixedFixtureUi.New("Viewport", background.transform);
            FixedFixtureUi.Place(viewport, 8, 3, PanelWidth - 52, 28);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8, 3);
            viewportRect.offsetMax = new Vector2(-8, -3);
            TextMeshProUGUI text = FixedFixtureUi.Text(viewport.transform, "Text", "", 15,
                FixedFixtureUi.Ink, 0, 0, PanelWidth - 52, 28);
            TextMeshProUGUI placeholder = FixedFixtureUi.Text(viewport.transform, "Placeholder",
                SkinLanguage.Text("搜索设施名称或对象路径…", "Search fixtures or object paths…"), 15, FixedFixtureUi.MutedInk, 0, 0, PanelWidth - 52, 28);
            placeholder.fontStyle = FontStyles.Italic;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.textViewport = viewportRect;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 160;
            input.selectionColor = FixedFixtureUi.Accent;
            input.text = initial ?? string.Empty;
            return input;
        }

        private TMP_InputField CreateTextureReferenceField(Transform parent)
        {
            Image background = FixedFixtureUi.Box(parent, "TextureReferenceField", FixedFixtureUi.Track,
                18, 474, 670, 28, true);
            TMP_InputField input = background.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.readOnly = false;
            input.interactable = true;
            GameObject viewport = FixedFixtureUi.New("Viewport", background.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(7, 2);
            viewportRect.offsetMax = new Vector2(-7, -2);
            TextMeshProUGUI text = FixedFixtureUi.Text(viewport.transform, "Text", "", 13,
                FixedFixtureUi.Ink, 0, 0, 650, 24);
            TextMeshProUGUI placeholder = FixedFixtureUi.Text(viewport.transform, "Placeholder",
                SkinLanguage.Text("贴图文件名（不含 .png）", "Texture filename (without .png)"), 13, FixedFixtureUi.MutedInk, 0, 0, 650, 24);
            placeholder.fontStyle = FontStyles.Italic;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.textViewport = viewportRect;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 128;
            input.selectionColor = FixedFixtureUi.Accent;
            input.onSelect.AddListener(_ => input.ActivateInputField());
            return input;
        }

        private Slider CreateSlider(Transform parent, string name, float min, float max, float value,
            float x, float y, float width, float height, Action<float> changed)
        {
            GameObject sliderObject = FixedFixtureUi.New(name, parent);
            FixedFixtureUi.Place(sliderObject, x, y, width, height);
            Image track = sliderObject.AddComponent<Image>();
            track.color = FixedFixtureUi.Track;
            GameObject fillArea = FixedFixtureUi.New("FillArea", sliderObject.transform);
            FixedFixtureUi.Pin(fillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.offsetMin = new Vector2(4, 6);
            fillAreaRect.offsetMax = new Vector2(-4, -6);
            GameObject fillObject = FixedFixtureUi.New("Fill", fillArea.transform);
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = FixedFixtureUi.Accent;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            GameObject handleObject = FixedFixtureUi.New("Handle", sliderObject.transform);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = FixedFixtureUi.Edge;
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(16f, 0f);
            handleRect.anchoredPosition = Vector2.zero;
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(valueChanged => changed(valueChanged));
            return slider;
        }

        private TMP_InputField CreatePercentInput(Transform parent, string name, int initialValue,
            float x, float y, float width, float height, float minimum, float maximum, Action<float> changed)
        {
            Image background = FixedFixtureUi.Box(parent, name, FixedFixtureUi.Track,
                x, y, width, height, true);
            TMP_InputField input = background.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 8;
            input.selectionColor = FixedFixtureUi.Accent;

            GameObject viewport = FixedFixtureUi.New("Viewport", background.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(3f, 1f);
            viewportRect.offsetMax = new Vector2(-3f, -1f);
            TextMeshProUGUI text = FixedFixtureUi.Text(viewport.transform, "Text", "", 13,
                FixedFixtureUi.Ink, 0, 0, width - 6f, height - 2f, TextAlignmentOptions.Right);
            text.raycastTarget = false;
            TextMeshProUGUI placeholder = FixedFixtureUi.Text(viewport.transform, "Placeholder", "0", 13,
                FixedFixtureUi.MutedInk, 0, 0, width - 6f, height - 2f, TextAlignmentOptions.Right);
            placeholder.raycastTarget = false;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.textViewport = viewportRect;
            input.SetTextWithoutNotify(initialValue.ToString(CultureInfo.InvariantCulture));
            input.onEndEdit.AddListener(value =>
            {
                string normalized = (value ?? string.Empty).Trim().TrimEnd('%').Trim();
                if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float percent))
                {
                    editor.RestoreSelectionAdjustmentValues();
                    return;
                }
                changed(Mathf.Clamp(percent, minimum, maximum));
            });
            return input;
        }

        private void RebuildRows(List<FixedFixtureTarget> targets)
        {
            for (int i = 0; i < rows.Count; i++)
                UnityEngine.Object.Destroy(rows[i].Object);
            rows.Clear();
            rowTargets.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                GameObject rowObject = FixedFixtureUi.New("Target" + i, listContent);
                FixedFixtureUi.Place(rowObject, 0, 0, PanelWidth - 70, ListRowHeight - 2);
                Image image = rowObject.AddComponent<Image>();
                image.color = FixedFixtureUi.ButtonFace;
                Button button = rowObject.AddComponent<Button>();
                button.targetGraphic = image;
                ColorBlock colors = button.colors;
                colors.normalColor = FixedFixtureUi.ButtonFace;
                colors.highlightedColor = FixedFixtureUi.Selected;
                colors.pressedColor = FixedFixtureUi.Accent;
                button.colors = colors;
                FixedFixtureTarget captured = target;
                button.onClick.AddListener(() => editor.SelectTarget(captured));
                GameObject thumbnailObject = FixedFixtureUi.New("Thumbnail", rowObject.transform);
                FixedFixtureUi.Place(thumbnailObject, 3, 3, 36, 36);
                Image thumbnail = thumbnailObject.AddComponent<Image>();
                thumbnail.sprite = target.Sprite;
                thumbnail.preserveAspect = true;
                thumbnail.raycastTarget = false;
                TextMeshProUGUI label = FixedFixtureUi.Text(rowObject.transform, "Label", "", 15,
                    FixedFixtureUi.Ink, 48, 0, PanelWidth - 126, ListRowHeight - 2);
                rows.Add(new Row(rowObject, image, button, label, thumbnail));
                rowTargets.Add(target);
            }
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private bool SameTargets(List<FixedFixtureTarget> targets)
        {
            for (int i = 0; i < rowTargets.Count; i++)
                if (rowTargets[i] != targets[i])
                    return false;
            return true;
        }

        private static void SetSliderValue(Slider slider, float value) => slider.SetValueWithoutNotify(value);

        private sealed class Row
        {
            internal readonly GameObject Object;
            internal readonly Image Image;
            internal readonly Button Button;
            internal readonly TextMeshProUGUI Label;
            internal readonly Image Thumbnail;

            internal Row(GameObject gameObject, Image image, Button button, TextMeshProUGUI label, Image thumbnail)
            {
                Object = gameObject;
                Image = image;
                Button = button;
                Label = label;
                Thumbnail = thumbnail;
            }
        }
    }
}
