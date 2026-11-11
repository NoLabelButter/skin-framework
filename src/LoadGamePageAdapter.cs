using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    internal sealed class LoadGameTextureSlotSetting
    {
        internal readonly string ConfigKey;
        internal readonly string Slot;
        internal readonly string Label;

        internal LoadGameTextureSlotSetting(string configKey, string slot, string label)
        {
            ConfigKey = configKey;
            Slot = slot;
            Label = label;
        }
    }

    /// <summary>载入游戏页可替换的装饰贴图；动态存档预览图继续使用存档自身资源。</summary>
    internal sealed class LoadGamePageAdapter
    {
        internal static readonly LoadGameTextureSlotSetting[] TextureSlots =
        {
            S("load_game_window_key_v1", SkinTextureSlots.LoadGameWindow, "载入页外框贴图"),
            S("load_game_panel_key_v1", SkinTextureSlots.LoadGamePanel, "载入页内容底板贴图"),
            S("load_game_back_button_key_v1", SkinTextureSlots.LoadGameBackButton, "返回按钮贴图"),
            S("load_game_back_button_hover_key_v1", SkinTextureSlots.LoadGameBackButtonHover, "返回按钮悬停贴图"),
            S("load_game_discover_button_key_v1", SkinTextureSlots.LoadGameDiscoverButton, "扫描存档按钮贴图"),
            S("load_game_discover_button_hover_key_v1", SkinTextureSlots.LoadGameDiscoverButtonHover, "扫描存档按钮悬停贴图"),
            S("load_game_scroll_background_key_v1", SkinTextureSlots.LoadGameScrollBackground, "存档列表底板贴图"),
            S("load_game_scroll_track_key_v1", SkinTextureSlots.LoadGameScrollTrack, "滚动槽贴图"),
            S("load_game_scroll_handle_key_v1", SkinTextureSlots.LoadGameScrollHandle, "滚动块贴图"),
            S("load_game_scroll_handle_border_key_v1", SkinTextureSlots.LoadGameScrollHandleBorder, "滚动块边框贴图"),
            S("load_game_plate_border_key_v1", SkinTextureSlots.LoadGamePlateBorder, "存档卡片边框贴图"),
            S("load_game_plate_icon_plate_key_v1", SkinTextureSlots.LoadGamePlateIconPlate, "存档图标底板贴图"),
            S("load_game_plate_icon_ring_key_v1", SkinTextureSlots.LoadGamePlateIconRing, "存档图标环贴图"),
            S("load_game_plate_separator_key_v1", SkinTextureSlots.LoadGamePlateSeparator, "存档卡片分隔线贴图"),
            S("load_game_common_button_background_key_v1", SkinTextureSlots.LoadGameCommonButtonBackground, "载入按钮底图贴图"),
            S("load_game_common_button_outer_key_v1", SkinTextureSlots.LoadGameCommonButtonOuter, "载入按钮外框贴图"),
            S("load_game_common_button_inner_key_v1", SkinTextureSlots.LoadGameCommonButtonInner, "载入按钮内框贴图"),
            S("load_game_duplicate_button_key_v1", SkinTextureSlots.LoadGameDuplicateButton, "复制存档按钮贴图"),
            S("load_game_duplicate_button_hover_key_v1", SkinTextureSlots.LoadGameDuplicateButtonHover, "复制存档按钮悬停贴图"),
            S("load_game_delete_button_key_v1", SkinTextureSlots.LoadGameDeleteButton, "删除存档按钮贴图"),
            S("load_game_delete_button_hover_key_v1", SkinTextureSlots.LoadGameDeleteButtonHover, "删除存档按钮悬停贴图"),
            S("load_game_expanded_border_key_v1", SkinTextureSlots.LoadGameExpandedBorder, "历史记录外框贴图"),
            S("load_game_expanded_bottom_key_v1", SkinTextureSlots.LoadGameExpandedBottom, "历史记录底边贴图"),
            S("load_game_history_rule_key_v1", SkinTextureSlots.LoadGameHistoryRule, "历史记录列分隔线贴图"),
            S("load_game_history_row_separator_key_v1", SkinTextureSlots.LoadGameHistoryRowSeparator, "历史记录行分隔线贴图"),
            S("load_game_dialog_overlay_key_v1", SkinTextureSlots.LoadGameDialogOverlay, "确认对话框遮罩贴图"),
            S("load_game_dialog_body_key_v1", SkinTextureSlots.LoadGameDialogBody, "确认对话框底板贴图"),
            S("load_game_dialog_body_border_key_v1", SkinTextureSlots.LoadGameDialogBodyBorder, "确认对话框底板边框贴图"),
            S("load_game_dialog_header_bar_key_v1", SkinTextureSlots.LoadGameDialogHeaderBar, "确认对话框标题栏贴图"),
            S("load_game_dialog_header_cap_key_v1", SkinTextureSlots.LoadGameDialogHeaderCap, "确认对话框标题端帽贴图"),
            S("load_game_dialog_separator_key_v1", SkinTextureSlots.LoadGameDialogSeparator, "确认对话框分隔线贴图")
        };

        private readonly List<ImageState> imageStates = new List<ImageState>();
        private readonly List<ButtonState> buttonStates = new List<ButtonState>();
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly Dictionary<string, Sprite> spritesBySlot = new Dictionary<string, Sprite>();

        internal void ApplyToOpenPage()
        {
            if (!LoadGameUI.IsOpen)
            {
                Restore();
                return;
            }

            LoadGameFrame frame = PositionAdjustmentTargets.GetLoadGameFrame();
            ApplyImage(frame.GetComponent<Image>(), SkinTextureSlots.LoadGameWindow);
            ApplyImage(FindImage(frame.panel, "Background"), SkinTextureSlots.LoadGamePanel);
            ApplyButton(frame.backButton, SkinTextureSlots.LoadGameBackButton,
                SkinTextureSlots.LoadGameBackButtonHover);
            ApplyButton(frame.discoverButton, SkinTextureSlots.LoadGameDiscoverButton,
                SkinTextureSlots.LoadGameDiscoverButtonHover);
            ApplyImage(frame.scroll.GetComponent<Image>(), SkinTextureSlots.LoadGameScrollBackground);
            ApplyImage(FindImage(frame.scrollbar.transform, "Track"), SkinTextureSlots.LoadGameScrollTrack);
            ApplyImage(frame.scrollbar.handleRect.GetComponent<Image>(), SkinTextureSlots.LoadGameScrollHandle);
            ApplyImage(FindImage(frame.scrollbar.handleRect, "Border"), SkinTextureSlots.LoadGameScrollHandleBorder);
            ApplyCommonButton(frame.dialogConfirm);
            ApplyCommonButton(frame.dialogCancel);
            ApplyDialog(frame.dialogRoot.transform);
            ApplySaveList(frame.content);
        }

        internal void Refresh()
        {
            Restore();
            ApplyToOpenPage();
        }

        internal void Dispose() => Restore();

        internal void Restore()
        {
            for (int i = buttonStates.Count - 1; i >= 0; i--)
                buttonStates[i].Restore();
            buttonStates.Clear();

            for (int i = imageStates.Count - 1; i >= 0; i--)
                imageStates[i].Restore();
            imageStates.Clear();

            for (int i = 0; i < sprites.Count; i++)
                if (sprites[i] != null)
                    Object.Destroy(sprites[i]);
            sprites.Clear();
            spritesBySlot.Clear();
        }

        private void ApplyDialog(Transform root)
        {
            ApplyImage(root.GetComponent<Image>(), SkinTextureSlots.LoadGameDialogOverlay);
            ApplyImage(FindImage(root, "Box/Body"), SkinTextureSlots.LoadGameDialogBody);
            ApplyImage(FindImage(root, "Box/BodyBorder"), SkinTextureSlots.LoadGameDialogBodyBorder);
            ApplyImage(FindImage(root, "Box/HeaderBar"), SkinTextureSlots.LoadGameDialogHeaderBar);
            ApplyImage(FindImage(root, "Box/HeaderCap"), SkinTextureSlots.LoadGameDialogHeaderCap);
            ApplyImage(FindImage(root, "Box/Separator"), SkinTextureSlots.LoadGameDialogSeparator);
        }

        private void ApplySaveList(RectTransform content)
        {
            Transform[] elements = content.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < elements.Length; i++)
            {
                Transform element = elements[i];
                switch (element.name)
                {
                    case "Border" when element.parent.name == "Body":
                        ApplyImage(element.GetComponent<Image>(), SkinTextureSlots.LoadGamePlateBorder);
                        break;
                    case "IconPlate":
                        ApplyImage(element.GetComponent<Image>(), SkinTextureSlots.LoadGamePlateIconPlate);
                        break;
                    case "Ring":
                        ApplyImage(element.GetComponent<Image>(), SkinTextureSlots.LoadGamePlateIconRing);
                        break;
                    case "Separator" when element.parent.name == "Body":
                        ApplyImage(element.GetComponent<Image>(), SkinTextureSlots.LoadGamePlateSeparator);
                        break;
                    case "Border" when element.parent.name == "Expanded":
                        ApplyImage(element.GetComponent<Image>(), SkinTextureSlots.LoadGameExpandedBorder);
                        break;
                    case "BorderBottom":
                        ApplyImage(element.GetComponent<Image>(), SkinTextureSlots.LoadGameExpandedBottom);
                        break;
                    case "Rule":
                        ApplyImage(element.GetComponent<Image>(), SkinTextureSlots.LoadGameHistoryRule);
                        break;
                    case "RowSeparator":
                        ApplyImage(element.GetComponent<Image>(), SkinTextureSlots.LoadGameHistoryRowSeparator);
                        break;
                    case "LoadLatest":
                    case "Load":
                        ApplyCommonButton(element);
                        break;
                    case "Duplicate":
                        ApplyButton(element.GetComponent<Button>(), SkinTextureSlots.LoadGameDuplicateButton,
                            SkinTextureSlots.LoadGameDuplicateButtonHover);
                        break;
                    case "Delete":
                        ApplyButton(element.GetComponent<Button>(), SkinTextureSlots.LoadGameDeleteButton,
                            SkinTextureSlots.LoadGameDeleteButtonHover);
                        break;
                }
            }
        }

        private void ApplyCommonButton(NewGameCommonButton button)
        {
            ApplyImage(button.background, SkinTextureSlots.LoadGameCommonButtonBackground);
            ApplyImage(button.outer, SkinTextureSlots.LoadGameCommonButtonOuter);
            ApplyImage(button.inner, SkinTextureSlots.LoadGameCommonButtonInner);
        }

        private void ApplyCommonButton(Transform root)
        {
            ApplyImage(root.GetComponent<Image>(), SkinTextureSlots.LoadGameCommonButtonBackground);
            ApplyImage(FindImage(root, "Outer"), SkinTextureSlots.LoadGameCommonButtonOuter);
            ApplyImage(FindImage(root, "Inner"), SkinTextureSlots.LoadGameCommonButtonInner);
        }

        private void ApplyButton(Button button, string normalSlot, string hoverSlot)
        {
            if (button == null)
                return;

            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            bool normalHidden = SkinService.IsTextureDisabled(normalSlot);
            bool hoverHidden = SkinService.IsTextureDisabled(hoverSlot);
            Sprite normal = normalHidden ? null : LoadSprite(image, normalSlot);
            Sprite hover = hoverHidden ? null : LoadSprite(image, hoverSlot);
            if (!normalHidden && !hoverHidden && normal == null && hover == null)
                return;

            ButtonState state = FindButtonState(button);
            if (state == null)
            {
                state = new ButtonState(button);
                buttonStates.Add(state);
            }
            if (normalHidden)
                ApplyImage(image, normalSlot);
            else if (normal != null)
                ApplyImage(image, normalSlot);

            SpriteState spriteState = state.Original;
            spriteState.highlightedSprite = hoverHidden ? null : hover ?? state.Original.highlightedSprite;
            spriteState.pressedSprite = hoverHidden ? null : hover ?? state.Original.pressedSprite;
            spriteState.selectedSprite = normalHidden ? null : normal ?? state.Original.selectedSprite;
            spriteState.disabledSprite = normalHidden ? null : normal ?? state.Original.disabledSprite;
            button.spriteState = spriteState;
        }

        private void ApplyImage(Image image, string slot)
        {
            if (image == null)
                return;

            bool disabled = SkinService.IsTextureDisabled(slot);
            Sprite sprite = disabled ? null : LoadSprite(image, slot);
            if (!disabled && sprite == null)
                return;

            if (FindImageState(image) == null)
                imageStates.Add(new ImageState(image));
            if (disabled)
            {
                image.enabled = false;
                return;
            }

            image.overrideSprite = null;
            image.sprite = sprite;
            if (slot == SkinTextureSlots.LoadGamePanel)
            {
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
        }

        private Sprite LoadSprite(Image image, string slot)
        {
            if (!SkinService.TryGetTexture(slot, out Texture2D texture))
                return null;
            if (spritesBySlot.TryGetValue(slot, out Sprite cached))
                return cached;

            Vector4 border = image.sprite == null ? Vector4.zero : image.sprite.border;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = "SkinFramework_" + slot;
            sprites.Add(sprite);
            spritesBySlot.Add(slot, sprite);
            return sprite;
        }

        private ImageState FindImageState(Image image)
        {
            for (int i = 0; i < imageStates.Count; i++)
                if (imageStates[i].Image == image)
                    return imageStates[i];
            return null;
        }

        private ButtonState FindButtonState(Button button)
        {
            for (int i = 0; i < buttonStates.Count; i++)
                if (buttonStates[i].Button == button)
                    return buttonStates[i];
            return null;
        }

        private static Image FindImage(Transform root, string path)
        {
            Transform target = root == null ? null : root.Find(path);
            return target == null ? null : target.GetComponent<Image>();
        }

        private static LoadGameTextureSlotSetting S(string key, string slot, string label) =>
            new LoadGameTextureSlotSetting(key, slot, label);

        private sealed class ImageState
        {
            internal readonly Image Image;
            private readonly Sprite sprite;
            private readonly Sprite overrideSprite;
            private readonly Image.Type type;
            private readonly bool preserveAspect;
            private readonly bool enabled;

            internal ImageState(Image image)
            {
                Image = image;
                sprite = image.sprite;
                overrideSprite = image.overrideSprite;
                type = image.type;
                preserveAspect = image.preserveAspect;
                enabled = image.enabled;
            }

            internal void Restore()
            {
                if (Image == null)
                    return;
                Image.overrideSprite = overrideSprite;
                Image.sprite = sprite;
                Image.type = type;
                Image.preserveAspect = preserveAspect;
                Image.enabled = enabled;
            }
        }

        private sealed class ButtonState
        {
            internal readonly Button Button;
            internal readonly SpriteState Original;

            internal ButtonState(Button button)
            {
                Button = button;
                Original = button.spriteState;
            }

            internal void Restore()
            {
                if (Button != null)
                    Button.spriteState = Original;
            }
        }
    }
}
