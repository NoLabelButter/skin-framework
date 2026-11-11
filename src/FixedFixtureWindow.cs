using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>调色板同款的纯色 UGUI 固定设施面板，不使用官方 CustomUI 窗口和控件 prefab。</summary>
    internal sealed partial class FixedFixtureWindow
    {
        private const float PanelWidth = 940f;
        private const float PanelHeight = 812f;
        private const float ListHeight = 200f;
        private const float ListRowHeight = 44f;
        private static readonly string[] Categories = { "全部", "墙体与卷帘", "柜台与桌面", "墙面设备", "地面设施", "其他设施", "界面菜单" };

        private readonly FixedFixtureEditor editor;
        private readonly GameObject root;
        private readonly Image backdropImage;
        private readonly Image frameImage;
        private readonly Image faceImage;
        private readonly Image titleBarImage;
        private readonly Image titleDividerImage;
        private readonly Image listBorderImage;
        private readonly Image listFaceImage;
        private Image searchBackgroundImage;
        private readonly List<Row> rows = new List<Row>();
        private readonly List<FixedFixtureTarget> rowTargets = new List<FixedFixtureTarget>();
        private readonly TextMeshProUGUI themeName;
        private readonly TextMeshProUGUI selectedName;
        private readonly TextMeshProUGUI textureReferencePath;
        private readonly TextMeshProUGUI textureReferenceHint;
        private readonly TMP_InputField textureReferenceInput;
        private readonly Image textureReferencePreview;
        private readonly Button copyTextureFolderButton;
        private readonly Button applyTextureReferenceButton;
        private readonly Button resetTextureReferenceButton;
        private readonly TMP_InputField searchInput;
        private readonly RectTransform listContent;
        private readonly RectTransform listViewport;
        private readonly ScrollRect scrollRect;
        private readonly Scrollbar verticalScrollbar;
        private readonly Button[] categoryButtons = new Button[Categories.Length];
        private readonly Slider xSlider;
        private readonly Slider ySlider;
        private readonly Slider alphaSlider;
        private readonly Slider contrastSlider;
        private readonly Slider brightnessSlider;
        private readonly Slider saturationSlider;
        private readonly TMP_InputField saturationValueInput;
        private readonly Slider windowAlphaSlider;
        private readonly TextMeshProUGUI xValueText;
        private readonly TextMeshProUGUI yValueText;
        private readonly TMP_InputField alphaValueInput;
        private readonly TMP_InputField contrastValueInput;
        private readonly TMP_InputField brightnessValueInput;
        private readonly TextMeshProUGUI windowAlphaValueText;
        private readonly Button resetButton;
        private readonly Button saveButton;
        private FixedFixtureTarget selected;
        private string selectedTextureSlot;
        private int activeCategory;
        private bool themeEditable;
        private bool closed;

        internal bool IsAlive => root != null && !closed;

        internal FixedFixtureWindow(FixedFixtureEditor editor, List<FixedFixtureTarget> targets,
            string search, int categoryIndex, FixedFixtureTarget selected,
            FixedFixtureAdjustment adjustment, float windowAlpha)
        {
            this.editor = editor;
            root = CreateCanvas();

            backdropImage = FixedFixtureUi.Box(root.transform, "Backdrop", FixedFixtureUi.Backdrop,
                0, 0, Screen.width, Screen.height);
            RectTransform backdropRect = backdropImage.rectTransform;
            FixedFixtureUi.Pin(backdropRect, Vector2.zero, Vector2.one);
            Button dismiss = backdropImage.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = backdropImage;
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(editor.Cancel);

            GameObject panel = FixedFixtureUi.New("FixedFixturePanel", root.transform);
            float canvasScale = root.GetComponent<Canvas>().scaleFactor;
            float canvasWidth = Screen.width / canvasScale;
            float canvasHeight = Screen.height / canvasScale;
            FixedFixtureUi.Place(panel, (canvasWidth - PanelWidth) * 0.5f,
                (canvasHeight - PanelHeight) * 0.5f, PanelWidth, PanelHeight);
            frameImage = FixedFixtureUi.Box(panel.transform, "Frame", FixedFixtureUi.Edge, 0, 0, PanelWidth, PanelHeight, false);
            faceImage = FixedFixtureUi.Box(panel.transform, "Face", FixedFixtureUi.Face, 2, 2, PanelWidth - 4, PanelHeight - 4);
            titleBarImage = FixedFixtureUi.Box(panel.transform, "TitleBar", FixedFixtureUi.Accent, 2, 2, PanelWidth - 4, 48);
            FixedFixtureWindowDragHandle dragHandle = titleBarImage.gameObject.AddComponent<FixedFixtureWindowDragHandle>();
            dragHandle.Target = panel.GetComponent<RectTransform>();
            FixedFixtureUi.Text(panel.transform, "Title", SkinLanguage.Text("固定设施外观", "Fixture Appearance"), 21, FixedFixtureUi.Face,
                18, 10, 620, 30);
            FixedFixtureUi.Button(panel.transform, "Close", "×", FixedFixtureUi.Face, FixedFixtureUi.Ink,
                PanelWidth - 50, 8, 34, 34, editor.Cancel, 22);
            titleDividerImage = FixedFixtureUi.Box(panel.transform, "TitleDivider", FixedFixtureUi.Edge, 16, 54,
                PanelWidth - 32, 1, false);

            FixedFixtureUi.Text(panel.transform, "ThemeLabel", SkinLanguage.Text("当前主题", "Current preset"), 15, FixedFixtureUi.MutedInk,
                18, 65, 72, 30);
            FixedFixtureUi.Button(panel.transform, "PreviousTheme", "‹", FixedFixtureUi.ButtonFace,
                FixedFixtureUi.Ink, 92, 63, 34, 34, () => editor.CyclePreset(-1), 20);
            themeName = FixedFixtureUi.Text(panel.transform, "ThemeName", "", 15, FixedFixtureUi.Ink,
                132, 65, 165, 30, TextAlignmentOptions.Center);
            FixedFixtureUi.Button(panel.transform, "NextTheme", "›", FixedFixtureUi.ButtonFace,
                FixedFixtureUi.Ink, 301, 63, 34, 34, () => editor.CyclePreset(1), 20);
            FixedFixtureUi.Text(panel.transform, "ThemeHint", SkinLanguage.Text("设施与窗口外观按主题分别保存", "Fixture and window settings are saved per preset."), 13,
                FixedFixtureUi.MutedInk, 352, 65, 420, 30);

            for (int i = 0; i < Categories.Length; i++)
            {
                int category = i;
                float width = 128f;
                categoryButtons[i] = FixedFixtureUi.Button(panel.transform, "Category" + i,
                    CategoryLabel(Categories[i]), i == categoryIndex ? FixedFixtureUi.Selected : FixedFixtureUi.ButtonFace,
                    FixedFixtureUi.Ink, 18 + i * 132f, 106, width, 34,
                    () => editor.SelectCategory(category), 14);
            }

            searchInput = CreateSearchField(panel.transform, search);
            searchInput.onValueChanged.AddListener(editor.FilterTargets);
            FixedFixtureUi.Text(panel.transform, "TargetHelp",
                SkinLanguage.Text("拖动标题栏移动窗口；列表含设施原图预览。方向键每次移动1px，Ctrl加速为6px。", "Drag the title bar to move the window. Arrow keys move by 1 px, or 6 px with Ctrl."), 14,
                FixedFixtureUi.MutedInk, 18, 190, PanelWidth - 36, 22);

            listBorderImage = FixedFixtureUi.Box(panel.transform, "ListBorder", FixedFixtureUi.EdgeDim,
                16, 218, PanelWidth - 32, ListHeight + 2, true);
            listFaceImage = FixedFixtureUi.Box(listBorderImage.transform, "ListFace", FixedFixtureUi.Track,
                1, 1, PanelWidth - 34, ListHeight, true);
            GameObject viewportObject = FixedFixtureUi.New("ListViewport", listBorderImage.transform);
            FixedFixtureUi.Place(viewportObject, 4, 4, PanelWidth - 70, ListHeight - 6);
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = Color.white;
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            listViewport = viewportObject.GetComponent<RectTransform>();
            GameObject contentObject = FixedFixtureUi.New("ListContent", viewportObject.transform);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(PanelWidth - 84, ListHeight - 6);
            listContent = contentRect;
            GameObject scrollbarObject = FixedFixtureUi.New("ListScrollbar", listBorderImage.transform);
            FixedFixtureUi.Place(scrollbarObject, PanelWidth - 57, 4, 12, ListHeight - 6);
            Image scrollbarTrack = scrollbarObject.AddComponent<Image>();
            scrollbarTrack.color = FixedFixtureUi.Track;
            GameObject scrollbarHandle = FixedFixtureUi.New("Handle", scrollbarObject.transform);
            Image scrollbarHandleImage = scrollbarHandle.AddComponent<Image>();
            scrollbarHandleImage.color = FixedFixtureUi.Edge;
            RectTransform scrollbarHandleRect = scrollbarHandle.GetComponent<RectTransform>();
            scrollbarHandleRect.anchorMin = Vector2.zero;
            scrollbarHandleRect.anchorMax = Vector2.one;
            scrollbarHandleRect.offsetMin = scrollbarHandleRect.offsetMax = Vector2.zero;
            verticalScrollbar = scrollbarObject.AddComponent<Scrollbar>();
            verticalScrollbar.handleRect = scrollbarHandleRect;
            verticalScrollbar.targetGraphic = scrollbarHandleImage;
            verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollRect = viewportObject.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = listViewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            selectedName = FixedFixtureUi.Text(panel.transform, "SelectedName", SkinLanguage.Text("尚未选择设施", "No fixture selected"), 16,
                FixedFixtureUi.Ink, 18, 426, PanelWidth - 150, 22);
            textureReferencePath = FixedFixtureUi.Text(panel.transform, "TextureReferencePath",
                SkinLanguage.Text("选择设施后查看素材引用", "Select a fixture to view its texture."), 12, FixedFixtureUi.MutedInk, 18, 450, 650, 20);
            copyTextureFolderButton = FixedFixtureUi.Button(panel.transform, "CopyTextureFolder",
                SkinLanguage.Text("复制贴图目录", "Copy Folder"), FixedFixtureUi.ButtonFace, FixedFixtureUi.Ink,
                678, 448, 132, 24, CopyTextureFolder, 12);
            textureReferencePreview = FixedFixtureUi.Box(panel.transform, "TextureReferencePreview",
                FixedFixtureUi.ButtonFace, PanelWidth - 124, 426, 104, 68);
            textureReferencePreview.preserveAspect = true;
            textureReferencePreview.raycastTarget = false;
            textureReferenceInput = CreateTextureReferenceField(panel.transform);
            textureReferenceInput.onEndEdit.AddListener(editor.SetTextureReference);
            applyTextureReferenceButton = FixedFixtureUi.Button(panel.transform, "ApplyTextureReference",
                SkinLanguage.Text("实时应用", "Apply Preview"), FixedFixtureUi.ButtonFace, FixedFixtureUi.Ink,
                698, 474, 98, 28, () => editor.SetTextureReference(textureReferenceInput.text), 13);
            resetTextureReferenceButton = FixedFixtureUi.Button(panel.transform, "ResetTextureReference",
                SkinLanguage.Text("恢复主题默认", "Reset Texture"), FixedFixtureUi.ButtonFace, FixedFixtureUi.Ink,
                802, 474, 116, 28, () => editor.SetTextureReference(string.Empty), 12);
            textureReferenceHint = FixedFixtureUi.Text(panel.transform, "TextureReferenceHint",
                SkinLanguage.Text("输入素材文件名；false 隐藏，null 使用游戏原图，留空恢复主题默认。应用后即时预览，保存后按主题记录。", "Enter a filename. Use false to hide, null for the original, or leave blank to reset."),
                11, FixedFixtureUi.MutedInk, 18, 506, PanelWidth - 36, 14);
            FixedFixtureUi.Text(panel.transform, "PositionLabel", SkinLanguage.Text("位置偏移", "Position offset"), 14,
                FixedFixtureUi.MutedInk, 18, 526, 74, 26);
            FixedFixtureUi.Text(panel.transform, "XLabel", "X", 14, FixedFixtureUi.Ink, 98, 526, 20, 26);
            xSlider = CreateSlider(panel.transform, "XSlider", -400f, 400f, 0f,
                120, 530, 330, 20, editor.SetX);
            xValueText = FixedFixtureUi.Text(panel.transform, "XValue", "0", 13,
                FixedFixtureUi.MutedInk, 454, 526, 48, 26, TextAlignmentOptions.Right);
            FixedFixtureUi.Text(panel.transform, "YLabel", "Y", 14, FixedFixtureUi.Ink, 476, 526, 20, 26);
            ySlider = CreateSlider(panel.transform, "YSlider", -400f, 400f, 0f,
                508, 530, 300, 20, editor.SetY);
            yValueText = FixedFixtureUi.Text(panel.transform, "YValue", "0", 13,
                FixedFixtureUi.MutedInk, 814, 526, 48, 26, TextAlignmentOptions.Right);
            FixedFixtureUi.Text(panel.transform, "ContrastLabel", SkinLanguage.Text("对比度", "Contrast"), 14,
                FixedFixtureUi.MutedInk, 18, 558, 90, 26);
            contrastSlider = CreateSlider(panel.transform, "ContrastSlider", 0f, 2f, 1f,
                120, 562, 560, 20, editor.SetContrast);
            contrastValueInput = CreatePercentInput(panel.transform, "ContrastValue", 100,
                690, 558, 62, 26, 0f, 200f, value => editor.SetContrast(value / 100f));
            FixedFixtureUi.Text(panel.transform, "ContrastRange", "0–200%", 12,
                FixedFixtureUi.MutedInk, 762, 558, 100, 26, TextAlignmentOptions.Right);
            FixedFixtureUi.Text(panel.transform, "BrightnessLabel", SkinLanguage.Text("亮度", "Brightness"), 14,
                FixedFixtureUi.MutedInk, 18, 590, 90, 26);
            brightnessSlider = CreateSlider(panel.transform, "BrightnessSlider", -0.5f, 0.5f, 0f,
                120, 594, 560, 20, editor.SetBrightness);
            brightnessValueInput = CreatePercentInput(panel.transform, "BrightnessValue", 0,
                690, 590, 62, 26, -50f, 50f, value => editor.SetBrightness(value / 100f));
            FixedFixtureUi.Text(panel.transform, "BrightnessRange", "−50%–+50%", 12,
                FixedFixtureUi.MutedInk, 762, 590, 100, 26, TextAlignmentOptions.Right);
            FixedFixtureUi.Text(panel.transform, "SaturationLabel", SkinLanguage.Text("饱和度", "Saturation"), 14,
                FixedFixtureUi.MutedInk, 18, 622, 90, 26);
            saturationSlider = CreateSlider(panel.transform, "SaturationSlider", 0f, 2f, 1f,
                120, 626, 560, 20, editor.SetSaturation);
            saturationValueInput = CreatePercentInput(panel.transform, "SaturationValue", 100,
                690, 622, 62, 26, 0f, 200f, value => editor.SetSaturation(value / 100f));
            FixedFixtureUi.Text(panel.transform, "SaturationRange", "0–200%", 12,
                FixedFixtureUi.MutedInk, 762, 622, 100, 26, TextAlignmentOptions.Right);
            FixedFixtureUi.Text(panel.transform, "AlphaLabel", SkinLanguage.Text("物品透明度", "Fixture opacity"), 14, FixedFixtureUi.MutedInk, 18, 654, 90, 26);
            alphaSlider = CreateSlider(panel.transform, "AlphaSlider", 0f, 1f, 1f,
                120, 658, 560, 20, editor.SetAlpha);
            alphaValueInput = CreatePercentInput(panel.transform, "AlphaValue", 100,
                690, 654, 62, 26, 0f, 100f, value => editor.SetAlpha(value / 100f));
            FixedFixtureUi.Text(panel.transform, "AlphaRange", "0–100%", 12,
                FixedFixtureUi.MutedInk, 762, 654, 100, 26, TextAlignmentOptions.Right);

            FixedFixtureUi.Text(panel.transform, "WindowAlphaLabel", SkinLanguage.Text("窗口透明度", "Window opacity"), 14,
                FixedFixtureUi.MutedInk, 18, 686, 90, 26);
            windowAlphaSlider = CreateSlider(panel.transform, "WindowAlphaSlider", 0f, 1f, windowAlpha,
                120, 690, 520, 20, editor.SetWindowAlpha);
            windowAlphaValueText = FixedFixtureUi.Text(panel.transform, "WindowAlphaValue", "62%", 13,
                FixedFixtureUi.MutedInk, 650, 686, 62, 26, TextAlignmentOptions.Right);

            FixedFixtureUi.Text(panel.transform, "EditHint",
                SkinLanguage.Text("Ctrl+Shift+Space 打开/取消　·　Esc 取消　·　取消或关闭会撤销未保存调整", "Ctrl+Shift+Space opens or cancels. Esc discards unsaved edits."),
                13, FixedFixtureUi.MutedInk, 18, 722, 650, 20);
            resetButton = FixedFixtureUi.Button(panel.transform, "Reset", SkinLanguage.Text("恢复当前主题默认", "Reset Preset"),
                FixedFixtureUi.ButtonFace, FixedFixtureUi.Ink, PanelWidth - 206, 682, 180, 34,
                editor.ResetCurrentTheme, 14);
            FixedFixtureUi.Box(panel.transform, "FooterDivider", FixedFixtureUi.EdgeDim,
                16, 752, PanelWidth - 32, 1, false);
            FixedFixtureUi.Button(panel.transform, "Cancel", SkinLanguage.Text("取消", "Cancel"), FixedFixtureUi.ButtonFace,
                FixedFixtureUi.Ink, PanelWidth - 265, 762, 110, 30, editor.Cancel, 15);
            saveButton = FixedFixtureUi.Button(panel.transform, "Save", SkinLanguage.Text("保存并关闭", "Save & Close"),
                FixedFixtureUi.Accent, FixedFixtureUi.Face, PanelWidth - 140, 762, 122, 30,
                editor.SaveAndClose, 15);

            RefreshTargets(targets, categoryIndex, search, selected);
            SetSelection(selected, adjustment);
            editor.RefreshSelectionDetails();
            SetTheme(SkinService.CurrentTheme, !SkinService.CurrentTheme.UsesGameDefaults);
            SetWindowAlpha(windowAlpha);
        }

        internal void RefreshTargets(List<FixedFixtureTarget> targets, int categoryIndex,
            string search, FixedFixtureTarget selected)
        {
            activeCategory = categoryIndex;
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                Color tint = i == activeCategory ? FixedFixtureUi.Selected : FixedFixtureUi.ButtonFace;
                categoryButtons[i].GetComponent<Image>().color = tint;
                ColorBlock categoryColors = categoryButtons[i].colors;
                categoryColors.normalColor = tint;
                categoryButtons[i].colors = categoryColors;
            }
            if (rowTargets.Count != targets.Count || !SameTargets(targets))
                RebuildRows(targets);
            this.selected = selected;
            int visible = 0;
            string query = (search ?? string.Empty).Trim();
            for (int i = 0; i < rows.Count; i++)
            {
                FixedFixtureTarget target = rowTargets[i];
                bool match = target.IsAlive && (categoryIndex == 0 || target.Group == Categories[categoryIndex]) &&
                    (query.Length == 0 || target.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                     target.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
                rows[i].Object.SetActive(match);
                if (!match)
                    continue;
                rows[i].Thumbnail.sprite = target.Sprite;
                FixedFixtureUi.Place(rows[i].Object, 0, visible * ListRowHeight,
                    PanelWidth - 70, ListRowHeight - 2);
                bool isSelected = selected == target;
                rows[i].Image.color = isSelected ? FixedFixtureUi.Selected : FixedFixtureUi.ButtonFace;
                ColorBlock colors = rows[i].Button.colors;
                colors.normalColor = rows[i].Image.color;
                rows[i].Button.colors = colors;
                rows[i].Label.text = (isSelected ? "▶  " : "　　") + target.Name + "　·　" + CategoryLabel(target.Group);
                visible++;
            }
            listContent.sizeDelta = new Vector2(listContent.sizeDelta.x,
                Mathf.Max(ListHeight - 6, visible * ListRowHeight));
        }

        internal void SetSelection(FixedFixtureTarget target, FixedFixtureAdjustment adjustment)
        {
            selected = target;
            if (target == null || !target.IsAlive)
            {
                selectedName.text = SkinLanguage.Text("尚未选择设施", "No fixture selected");
                SetSliderValue(xSlider, 0f);
                SetSliderValue(ySlider, 0f);
                SetSliderValue(alphaSlider, 1f);
                xValueText.text = yValueText.text = "0";
                alphaValueInput.SetTextWithoutNotify("100");
                SetColorAdjustmentValues(1f, 0f, 1f);
            }
            else
            {
                selectedName.text = CategoryLabel(target.Group) + " / " + target.Name +
                    (target.Sprite == null ? "" : "　(" + target.Sprite.name + ")");
                SetPositionValues(adjustment.x, adjustment.y);
                SetAlphaValue(adjustment.alpha);
                SetColorAdjustmentValues(adjustment.contrast, adjustment.brightness, adjustment.saturation);
            }
            bool canAdjust = themeEditable && target != null && target.IsAlive;
            SetPositionControlsEnabled(canAdjust && (target == null || !target.IsMenuTarget));
            SetVisualAdjustmentControlsEnabled(canAdjust);
        }

        internal void SetTextureReference(string slot, string input, string path, Sprite preview,
            bool editable, string hint)
        {
            selectedTextureSlot = slot;
            textureReferenceInput.SetTextWithoutNotify(input ?? string.Empty);
            textureReferenceInput.interactable = editable && !string.IsNullOrEmpty(slot);
            applyTextureReferenceButton.interactable = editable && !string.IsNullOrEmpty(slot);
            resetTextureReferenceButton.interactable = editable && !string.IsNullOrEmpty(slot);
            copyTextureFolderButton.interactable = editable && !string.IsNullOrEmpty(slot);
            textureReferencePath.text = editable && !string.IsNullOrEmpty(slot)
                ? SkinLanguage.Text("放置 PNG：", "Put PNG files in: ") + SkinService.GetTextureCategory(slot) + "  ·  " + path
                : path ?? string.Empty;
            textureReferencePreview.sprite = preview;
            textureReferencePreview.enabled = preview != null;
            textureReferencePath.color = FixedFixtureUi.MutedInk;
            textureReferenceHint.text = hint ?? string.Empty;
        }

        private void CopyTextureFolder()
        {
            string folder = ModHelper.GetModDataFolder("skin_framework") + "/Textures/" +
                SkinPresets.GetAssetFolder(SkinService.CurrentTheme.Id) + "/" +
                SkinService.GetTextureCategory(selectedTextureSlot);
            GUIUtility.systemCopyBuffer = folder.Replace('/', '\\');
            textureReferencePath.text = SkinLanguage.Text("路径已复制；目录不存在时请先创建。", "Path copied. Create the folder if it does not exist.");
        }

        private static string CategoryLabel(string category)
        {
            if (!SkinLanguage.English) return category;
            switch (category)
            {
                case "全部": return "All";
                case "墙体与卷帘": return "Walls & shutters";
                case "柜台与桌面": return "Counter & desk";
                case "墙面设备": return "Wall fixtures";
                case "地面设施": return "Floor fixtures";
                case "其他设施": return "Other fixtures";
                case "界面菜单": return "Menus";
                default: return category;
            }
        }

        internal void SetTextureReferenceError(string message)
        {
            textureReferencePath.text = message ?? string.Empty;
            textureReferencePath.color = new Color(1f, 0.62f, 0.56f, 1f);
        }

        internal void SetPositionValues(float x, float y)
        {
            SetSliderValue(xSlider, x);
            SetSliderValue(ySlider, y);
            xValueText.text = x.ToString("0.###");
            yValueText.text = y.ToString("0.###");
        }

        internal void SetAlphaValue(float alpha) =>
            alphaValueInput.SetTextWithoutNotify(Mathf.RoundToInt(alpha * 100f).ToString());

        internal void SetColorAdjustmentValues(float contrast, float brightness, float saturation)
        {
            contrastSlider.SetValueWithoutNotify(contrast);
            brightnessSlider.SetValueWithoutNotify(brightness);
            saturationSlider.SetValueWithoutNotify(saturation);
            saturationValueInput.SetTextWithoutNotify(Mathf.RoundToInt(saturation * 100f).ToString());
            contrastValueInput.SetTextWithoutNotify(Mathf.RoundToInt(contrast * 100f).ToString());
            brightnessValueInput.SetTextWithoutNotify(Mathf.RoundToInt(brightness * 100f).ToString());
        }

        internal void SetTheme(SkinTheme theme, bool editable)
        {
            themeEditable = editable;
            themeName.text = theme.UsesGameDefaults ? SkinLanguage.Text(theme.DisplayName, "Game Default") : theme.DisplayName;
            bool canAdjust = themeEditable && selected != null && selected.IsAlive;
            SetPositionControlsEnabled(canAdjust && !selected.IsMenuTarget);
            SetVisualAdjustmentControlsEnabled(canAdjust);
            textureReferenceInput.interactable = editable && !string.IsNullOrEmpty(selectedTextureSlot);
            applyTextureReferenceButton.interactable = editable && !string.IsNullOrEmpty(selectedTextureSlot);
            resetTextureReferenceButton.interactable = editable && !string.IsNullOrEmpty(selectedTextureSlot);
            resetButton.interactable = editable;
            saveButton.interactable = true;
        }

        private void SetPositionControlsEnabled(bool enabled)
        {
            xSlider.interactable = enabled;
            ySlider.interactable = enabled;
        }

        private void SetVisualAdjustmentControlsEnabled(bool enabled)
        {
            alphaSlider.interactable = enabled;
            contrastSlider.interactable = enabled;
            brightnessSlider.interactable = enabled;
            saturationSlider.interactable = enabled;
            saturationValueInput.interactable = enabled;
            alphaValueInput.interactable = enabled;
            contrastValueInput.interactable = enabled;
            brightnessValueInput.interactable = enabled;
        }

        internal void CloseWithoutCallback()
        {
            if (closed)
                return;
            closed = true;
            if (root != null)
                UnityEngine.Object.Destroy(root);
        }

    }
}
