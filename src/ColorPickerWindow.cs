using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>独立 UGUI 颜色面板，只用纯色 UI Graphic，不创建贴图资源。</summary>
    internal sealed partial class ColorPickerWindow
    {
        private const float PanelWidth = 520f;
        private const float PanelHeight = 450f;
        private static readonly Color BackdropColor = new Color(0.08f, 0.07f, 0.10f, 0.72f);
        private static readonly Color FaceColor = new Color32(43, 37, 50, 255);
        private static readonly Color EdgeColor = new Color32(125, 190, 186, 255);
        private static readonly Color EdgeDimColor = new Color32(70, 91, 94, 255);
        private static readonly Color AccentColor = new Color32(143, 199, 194, 255);
        private static readonly Color InkColor = new Color32(232, 226, 207, 255);
        private static readonly Color MutedInkColor = new Color32(177, 178, 173, 255);
        private static readonly Color TrackColor = new Color32(29, 27, 36, 255);
        private static readonly Color ButtonColor = new Color32(54, 49, 61, 255);
        private static readonly Color32[] Palette =
        {
            new Color32(242, 239, 225, 255), new Color32(35, 32, 41, 255),
            new Color32(220, 65, 65, 255), new Color32(240, 150, 55, 255),
            new Color32(235, 210, 75, 255), new Color32(85, 175, 100, 255),
            new Color32(70, 165, 205, 255), new Color32(85, 105, 205, 255),
            new Color32(160, 90, 190, 255), new Color32(215, 105, 145, 255),
            new Color32(210, 175, 145, 255), new Color32(140, 150, 160, 255)
        };

        private static TMP_FontAsset cachedFont;
        private static bool fontResolved;

        private readonly Action<string> onSelected;
        private readonly GameObject root;
        private readonly PickerState state;
        private readonly TextMeshProUGUI hexText;
        private readonly Image preview;
        private readonly TextMeshProUGUI[] channelValues = new TextMeshProUGUI[3];
        private readonly List<PaletteSwatch> swatches = new List<PaletteSwatch>();
        private bool isClosed;

        private ColorPickerWindow(Color initial, Action<string> onSelected)
        {
            this.onSelected = onSelected;
            state = new PickerState(initial);
            root = CreateCanvas();

            Image backdrop = Box(root.transform, "Backdrop", BackdropColor, true);
            Pin(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button dismiss = backdrop.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = backdrop;
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(Close);

            GameObject panel = New("ColorPanel", root.transform);
            Place(panel, (Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
            Box(panel.transform, "Frame", EdgeColor, false, 0, 0, PanelWidth, PanelHeight);
            Box(panel.transform, "Face", FaceColor, true, 2, 2, PanelWidth - 4, PanelHeight - 4);
            Box(panel.transform, "TitleBar", AccentColor, true, 2, 2, PanelWidth - 4, 48);
            AddText(panel.transform, "Title", "主界面字体颜色", 21, FaceColor, 18, 10, 420, 30);
            AddButton(panel.transform, "Close", "×", FaceColor, InkColor,
                PanelWidth - 48, 8, 34, 34, Close);
            Box(panel.transform, "TitleDivider", EdgeColor, false, 16, 54, PanelWidth - 32, 1);

            AddText(panel.transform, "CurrentLabel", "当前颜色", 15, MutedInkColor, 18, 66, 160, 22);
            hexText = AddText(panel.transform, "HexValue", "", 15, AccentColor,
                PanelWidth - 178, 66, 160, 22, TextAlignmentOptions.Right);
            Box(panel.transform, "PreviewEdge", EdgeColor, false, 18, 92, PanelWidth - 36, 28);
            preview = Box(panel.transform, "Preview", initial, false, 20, 94, PanelWidth - 40, 24);

            AddChannel(panel.transform, 0, "红", state.Red, 135);
            AddChannel(panel.transform, 1, "绿", state.Green, 174);
            AddChannel(panel.transform, 2, "蓝", state.Blue, 213);

            AddText(panel.transform, "PaletteTitle", "常用颜色", 18, InkColor, 18, 252, 280, 26);
            for (int i = 0; i < Palette.Length; i++)
                AddPaletteSwatch(panel.transform, i, Palette[i]);

            AddText(panel.transform, "PresetSaveHint", "修改只作字体覆盖；可在皮肤设置中点击“＋ 新建预设”创建主题。",
                12, MutedInkColor, 18, 352, PanelWidth - 36, 18);
            Box(panel.transform, "FooterDivider", EdgeDimColor, false, 16, 376, PanelWidth - 32, 1);
            AddButton(panel.transform, "Cancel", "取消", ButtonColor, InkColor,
                PanelWidth - 246, 389, 104, 42, Close);
            AddButton(panel.transform, "Apply", "应用", AccentColor, FaceColor,
                PanelWidth - 128, 389, 110, 42, Apply);
            RefreshColor();
        }

        public static void Open(Color initial, Action<string> onSelected) => new ColorPickerWindow(initial, onSelected);

        private void AddChannel(Transform panel, int index, string label, int initial, float y)
        {
            AddText(panel, "ChannelLabel" + index, label, 15, InkColor, 18, y, 30, 24);
            GameObject sliderObject = New("ChannelSlider" + index, panel);
            Place(sliderObject, 62, y + 2, 376, 20);
            Image track = sliderObject.AddComponent<Image>();
            track.color = TrackColor;

            GameObject fillArea = New("FillArea", sliderObject.transform);
            Pin(fillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(4, 6), new Vector2(-4, -6));
            GameObject fillObject = New("Fill", fillArea.transform);
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = AccentColor;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            GameObject handleObject = New("Handle", sliderObject.transform);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = AccentColor;
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0);
            handleRect.anchorMax = new Vector2(0, 1);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(14, 0);
            handleRect.anchoredPosition = Vector2.zero;

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 255;
            slider.wholeNumbers = true;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = track;
            slider.value = initial;
            channelValues[index] = AddText(panel, "ChannelValue" + index, initial.ToString(),
                14, MutedInkColor, 450, y, 52, 24, TextAlignmentOptions.Right);
            slider.onValueChanged.AddListener(value =>
            {
                state.SetChannel(index, Mathf.RoundToInt(value));
                channelValues[index].text = Mathf.RoundToInt(value).ToString();
                RefreshColor();
            });
        }

        private void AddPaletteSwatch(Transform panel, int index, Color32 color)
        {
            int column = index % 6;
            int row = index / 6;
            float x = 18 + column * 80;
            float y = 286 + row * 36;
            Image border = Box(panel, "PaletteBorder" + index, EdgeDimColor, false, x, y, 70, 28);
            GameObject buttonObject = New("PaletteButton" + index, border.transform);
            Place(buttonObject, 1, 1, 68, 26);
            Image fill = buttonObject.AddComponent<Image>();
            fill.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                state.SetColor(color);
                RefreshColor();
            });
            swatches.Add(new PaletteSwatch(color, border));
        }

        private void RefreshColor()
        {
            Color color = state.ToColor();
            preview.color = color;
            hexText.text = MainMenuFontColor.ToHex(color);
            Color32 selected = color;
            for (int i = 0; i < swatches.Count; i++)
                swatches[i].Border.color = swatches[i].Color.Equals(selected) ? AccentColor : EdgeDimColor;
        }

        private void Apply()
        {
            onSelected(MainMenuFontColor.ToHex(state.ToColor()));
            Close();
        }

        private void Close()
        {
            if (isClosed)
                return;
            isClosed = true;
            if (root != null)
                UnityEngine.Object.Destroy(root);
        }

        private sealed class PickerState
        {
            public int Red { get; private set; }
            public int Green { get; private set; }
            public int Blue { get; private set; }

            public PickerState(Color color)
            {
                Color32 value = color;
                Red = value.r;
                Green = value.g;
                Blue = value.b;
            }

            public void SetChannel(int index, int value)
            {
                if (index == 0) Red = value;
                else if (index == 1) Green = value;
                else Blue = value;
            }

            public void SetColor(Color32 color)
            {
                Red = color.r;
                Green = color.g;
                Blue = color.b;
            }

            public Color ToColor() => new Color32((byte)Red, (byte)Green, (byte)Blue, 255);
        }

        private sealed class PaletteSwatch
        {
            public Color32 Color { get; }
            public Image Border { get; }

            public PaletteSwatch(Color32 color, Image border)
            {
                Color = color;
                Border = border;
            }
        }
    }
}
