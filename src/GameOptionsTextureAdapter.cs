using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    internal sealed class GameOptionsTextureSlotSetting
    {
        internal readonly string ConfigKey;
        internal readonly string Slot;
        internal readonly string Label;

        internal GameOptionsTextureSlotSetting(string configKey, string slot, string label)
        {
            ConfigKey = configKey;
            Slot = slot;
            Label = label;
        }
    }

    /// <summary>只替换原生选项窗口的装饰图像；控件状态与交互仍由游戏管理。</summary>
    internal sealed class GameOptionsTextureAdapter
    {
        internal static readonly GameOptionsTextureSlotSetting[] TextureSlots =
        {
            S("game_options_window_key_v1", SkinTextureSlots.GameOptionsWindow, "选项窗口外框贴图"),
            S("game_options_title_bar_key_v1", SkinTextureSlots.GameOptionsTitleBar, "选项标题栏贴图"),
            S("game_options_content_panel_key_v1", SkinTextureSlots.GameOptionsContentPanel, "选项内容底板贴图"),
            S("game_options_close_button_key_v1", SkinTextureSlots.GameOptionsCloseButton, "关闭按钮贴图"),
            S("game_options_close_button_hover_key_v1", SkinTextureSlots.GameOptionsCloseButtonHover, "关闭按钮悬停贴图"),
            S("game_options_scroll_track_key_v1", SkinTextureSlots.GameOptionsScrollTrack, "滚动条轨道贴图"),
            S("game_options_scroll_thumb_key_v1", SkinTextureSlots.GameOptionsScrollThumb, "滚动条滑块贴图"),
            S("game_options_separator_key_v1", SkinTextureSlots.GameOptionsSeparator, "分组分隔线贴图"),
            S("game_options_apply_button_key_v1", SkinTextureSlots.GameOptionsApplyButton, "应用按钮贴图"),
            S("game_options_apply_button_hover_key_v1", SkinTextureSlots.GameOptionsApplyButtonHover, "应用按钮悬停贴图")
        };

        private readonly List<ImageState> images = new List<ImageState>();
        private readonly HashSet<Image> capturedImages = new HashSet<Image>();
        private readonly List<ButtonState> buttons = new List<ButtonState>();
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly Dictionary<string, Sprite> spritesBySlot = new Dictionary<string, Sprite>();

        internal void ApplyToOpenWindow()
        {
            Restore();
            CustomUIManager manager = CustomUIManager.Instance;
            CustomUIWindow window = manager == null ? null : manager.GetWindow(GameOptionUIManager.WINDOW_ID);
            if (window == null || !window.IsAlive || !window.IsVisible)
                return;

            ApplyImage(window.root.GetComponent<Image>(), SkinTextureSlots.GameOptionsWindow);
            if (window.frame != null)
            {
                Transform title = window.frame.titleText == null ? null : window.frame.titleText.transform.parent;
                ApplyImage(title == null ? null : title.GetComponent<Image>(), SkinTextureSlots.GameOptionsTitleBar);
                Transform panel = window.frame.contentRoot;
                ApplyImage(panel == null ? null : panel.GetComponent<Image>(), SkinTextureSlots.GameOptionsContentPanel);
                ApplyButton(window.frame.closeButton, SkinTextureSlots.GameOptionsCloseButton,
                    SkinTextureSlots.GameOptionsCloseButtonHover);
            }

            Transform scrollRoot = PositionAdjustmentTargets.FindGameOptionsElement(window, "scroll");
            ScrollRect scrollRect = scrollRoot == null ? null : scrollRoot.GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null && scrollRect.verticalScrollbar != null)
            {
                ApplyImage(scrollRect.verticalScrollbar.GetComponent<Image>(), SkinTextureSlots.GameOptionsScrollTrack);
                RectTransform handle = scrollRect.verticalScrollbar.handleRect;
                ApplyImage(handle == null ? null : handle.GetComponent<Image>(), SkinTextureSlots.GameOptionsScrollThumb);
            }

            Image[] descendants = window.root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < descendants.Length; i++)
                if (descendants[i].name.IndexOf("separator", StringComparison.OrdinalIgnoreCase) >= 0)
                    ApplyImage(descendants[i], SkinTextureSlots.GameOptionsSeparator);

            CustomUIElement apply = window.Get("apply");
            ApplyButton(apply == null ? null : apply.button,
                SkinTextureSlots.GameOptionsApplyButton, SkinTextureSlots.GameOptionsApplyButtonHover);
        }

        internal void Restore()
        {
            for (int i = buttons.Count - 1; i >= 0; i--) buttons[i].Restore();
            buttons.Clear();
            for (int i = images.Count - 1; i >= 0; i--) images[i].Restore();
            images.Clear();
            capturedImages.Clear();
            for (int i = 0; i < sprites.Count; i++)
                if (sprites[i] != null) UnityEngine.Object.Destroy(sprites[i]);
            sprites.Clear();
            spritesBySlot.Clear();
        }

        internal void Dispose() => Restore();

        private static GameOptionsTextureSlotSetting S(string key, string slot, string label) =>
            new GameOptionsTextureSlotSetting(key, slot, label);

        private void ApplyButton(Button button, string normalSlot, string hoverSlot)
        {
            if (button == null || button.targetGraphic == null)
                return;

            Image image = button.targetGraphic as Image;
            bool normalDisabled = SkinService.IsTextureDisabled(normalSlot);
            bool hoverDisabled = SkinService.IsTextureDisabled(hoverSlot);
            Sprite normal = normalDisabled || image == null ? null : LoadSprite(normalSlot, image);
            Sprite hover = hoverDisabled ? null : LoadSprite(hoverSlot, image);
            if (!normalDisabled && !hoverDisabled && normal == null && hover == null)
                return;

            ButtonState state = new ButtonState(button);
            buttons.Add(state);
            if (image != null && (normalDisabled || normal != null))
                ApplyImage(image, normalSlot);

            SpriteState spriteState = state.Original;
            if (hover != null)
            {
                spriteState.highlightedSprite = hover;
                spriteState.selectedSprite = hover;
                spriteState.pressedSprite = hover;
            }
            else if (hoverDisabled)
            {
                spriteState.highlightedSprite = null;
                spriteState.selectedSprite = null;
                spriteState.pressedSprite = null;
            }
            button.spriteState = spriteState;
        }

        private void ApplyImage(Image image, string slot)
        {
            if (image == null)
                return;

            bool disabled = SkinService.IsTextureDisabled(slot);
            Sprite sprite = disabled ? null : LoadSprite(slot, image);
            if (!disabled && sprite == null)
                return;

            if (capturedImages.Add(image))
                images.Add(new ImageState(image));
            if (disabled)
            {
                image.enabled = false;
                return;
            }

            image.sprite = sprite;
            image.overrideSprite = null;
            image.enabled = true;
        }

        private Sprite LoadSprite(string slot, Image image)
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

        private sealed class ImageState
        {
            private readonly Image image;
            private readonly Sprite sprite;
            private readonly Sprite overrideSprite;
            private readonly Image.Type type;
            private readonly bool preserveAspect;
            private readonly bool enabled;

            internal ImageState(Image image)
            {
                this.image = image;
                sprite = image.sprite;
                overrideSprite = image.overrideSprite;
                type = image.type;
                preserveAspect = image.preserveAspect;
                enabled = image.enabled;
            }

            internal void Restore()
            {
                if (image == null) return;
                image.sprite = sprite;
                image.overrideSprite = overrideSprite;
                image.type = type;
                image.preserveAspect = preserveAspect;
                image.enabled = enabled;
            }
        }

        private sealed class ButtonState
        {
            private readonly Button button;
            internal readonly SpriteState Original;

            internal ButtonState(Button button)
            {
                this.button = button;
                Original = button.spriteState;
            }

            internal void Restore()
            {
                if (button != null) button.spriteState = Original;
            }
        }
    }
}
