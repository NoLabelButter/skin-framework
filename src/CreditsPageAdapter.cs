using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    internal sealed class CreditsTextureSlotSetting
    {
        internal readonly string ConfigKey;
        internal readonly string Slot;
        internal readonly string Label;

        internal CreditsTextureSlotSetting(string configKey, string slot, string label)
        {
            ConfigKey = configKey;
            Slot = slot;
            Label = label;
        }
    }

    /// <summary>制作人员名单页的静态图像替换；名单文字由游戏运行时生成，保留原文与字体。</summary>
    internal sealed class CreditsPageAdapter
    {
        internal static readonly CreditsTextureSlotSetting[] TextureSlots =
        {
            S("credits_window_key_v1", SkinTextureSlots.CreditsWindow, "名单窗口外框与页头色带贴图"),
            S("credits_panel_key_v1", SkinTextureSlots.CreditsPanel, "名单内容底板贴图"),
            S("credits_close_button_key_v1", SkinTextureSlots.CreditsCloseButton, "关闭按钮贴图"),
            S("credits_close_button_hover_key_v1", SkinTextureSlots.CreditsCloseButtonHover, "关闭按钮悬停贴图"),
            S("credits_scroll_background_key_v1", SkinTextureSlots.CreditsScrollBackground, "滚动视口底图贴图"),
            S("credits_scroll_track_key_v1", SkinTextureSlots.CreditsScrollTrack, "滚动槽贴图"),
            S("credits_scroll_handle_key_v1", SkinTextureSlots.CreditsScrollHandle, "滚动滑块贴图"),
            S("credits_scroll_handle_border_key_v1", SkinTextureSlots.CreditsScrollHandleBorder, "滚动滑块边框贴图"),
            S("credits_separator_key_v1", SkinTextureSlots.CreditsSeparator, "名单分隔线贴图")
        };

        private readonly List<ImageState> images = new List<ImageState>();
        private readonly List<ButtonState> buttons = new List<ButtonState>();
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly Dictionary<string, Sprite> spritesBySlot = new Dictionary<string, Sprite>();

        internal void ApplyToOpenPage()
        {
            Restore();
            CreditsFrame frame = PositionAdjustmentTargets.GetCreditsFrame();
            if (frame == null)
            {
                Restore();
                return;
            }

            ApplyImage(frame.GetComponent<Image>(), SkinTextureSlots.CreditsWindow);
            ApplyImage(frame.panel == null ? null : frame.panel.Find("Background")?.GetComponent<Image>(),
                SkinTextureSlots.CreditsPanel);
            ApplyButton(frame.backButton);
            ApplyImage(frame.scroll == null ? null : frame.scroll.GetComponent<Image>(),
                SkinTextureSlots.CreditsScrollBackground);
            ApplyImage(FindImage(frame.scrollbar == null ? null : frame.scrollbar.transform, "Track"),
                SkinTextureSlots.CreditsScrollTrack);
            ApplyImage(frame.scrollbar == null || frame.scrollbar.handleRect == null
                    ? null : frame.scrollbar.handleRect.GetComponent<Image>(),
                SkinTextureSlots.CreditsScrollHandle);
            ApplyImage(FindImage(frame.scrollbar == null || frame.scrollbar.handleRect == null
                ? null : frame.scrollbar.handleRect, "Border"), SkinTextureSlots.CreditsScrollHandleBorder);

            Transform[] content = frame.content.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < content.Length; i++)
                if (content[i].name == "Rule")
                    ApplyImage(content[i].GetComponent<Image>(), SkinTextureSlots.CreditsSeparator);
        }

        internal void Restore()
        {
            for (int i = buttons.Count - 1; i >= 0; i--) buttons[i].Restore();
            buttons.Clear();
            for (int i = images.Count - 1; i >= 0; i--) images[i].Restore();
            images.Clear();
            for (int i = 0; i < sprites.Count; i++)
                if (sprites[i] != null) Object.Destroy(sprites[i]);
            sprites.Clear();
            spritesBySlot.Clear();
        }

        internal void Dispose() => Restore();

        private void ApplyButton(Button button)
        {
            if (button == null || button.targetGraphic == null) return;
            bool normalHidden = SkinService.IsTextureDisabled(SkinTextureSlots.CreditsCloseButton);
            bool hoverHidden = SkinService.IsTextureDisabled(SkinTextureSlots.CreditsCloseButtonHover);
            Image image = button.targetGraphic as Image;
            Sprite normal = normalHidden ? null : LoadSprite(SkinTextureSlots.CreditsCloseButton, image);
            Sprite hover = hoverHidden ? null : LoadSprite(SkinTextureSlots.CreditsCloseButtonHover, image);
            if (!normalHidden && !hoverHidden && normal == null && hover == null) return;
            ButtonState buttonState = new ButtonState(button);
            buttons.Add(buttonState);
            if (normalHidden || normal != null)
                ApplyImage(image, SkinTextureSlots.CreditsCloseButton);
            if (hover != null)
            {
                SpriteState state = buttonState.Original;
                state.highlightedSprite = hover;
                state.selectedSprite = hover;
                state.pressedSprite = hover;
                button.spriteState = state;
            }
            else if (hoverHidden)
            {
                SpriteState state = buttonState.Original;
                state.highlightedSprite = null;
                state.selectedSprite = null;
                state.pressedSprite = null;
                button.spriteState = state;
            }
        }

        private void ApplyImage(Image image, string slot)
        {
            if (image == null) return;
            bool disabled = SkinService.IsTextureDisabled(slot);
            Sprite sprite = disabled ? null : LoadSprite(slot, image);
            if (!disabled && sprite == null) return;
            if (disabled)
            {
                images.Add(new ImageState(image));
                image.enabled = false;
                return;
            }
            images.Add(new ImageState(image));
            image.sprite = sprite;
            image.overrideSprite = null;
            if (slot == SkinTextureSlots.CreditsPanel)
            {
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
            image.enabled = true;
        }

        private Sprite LoadSprite(string slot, Image image)
        {
            if (!SkinService.TryGetTexture(slot, out Texture2D texture)) return null;
            if (spritesBySlot.TryGetValue(slot, out Sprite cached)) return cached;
            Vector4 border = image.sprite == null ? Vector4.zero : image.sprite.border;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = "SkinFramework_" + slot;
            sprites.Add(sprite);
            spritesBySlot.Add(slot, sprite);
            return sprite;
        }

        private static Image FindImage(Transform root, string childName)
        {
            Transform child = root == null ? null : root.Find(childName);
            return child == null ? null : child.GetComponent<Image>();
        }

        private static CreditsTextureSlotSetting S(string key, string slot, string label) =>
            new CreditsTextureSlotSetting(key, slot, label);

        [HarmonyPatch(typeof(CreditsUI), nameof(CreditsUI.Open))]
        private static class CreditsOpenedPatch
        {
            private static void Postfix() => ModEntry.ApplyCreditsPageSkin();
        }

        [HarmonyPatch(typeof(CreditsUI), "OnWindowClosed")]
        private static class CreditsClosedPatch
        {
            private static void Postfix() => ModEntry.RestoreCreditsPageSkin();
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
                this.image = image; sprite = image.sprite; overrideSprite = image.overrideSprite;
                type = image.type; preserveAspect = image.preserveAspect; enabled = image.enabled;
            }

            internal void Restore()
            {
                if (image == null) return;
                image.sprite = sprite; image.overrideSprite = overrideSprite;
                image.type = type; image.preserveAspect = preserveAspect; image.enabled = enabled;
            }
        }

        private sealed class ButtonState
        {
            private readonly Button button;
            private readonly Sprite sprite;
            private readonly SpriteState state;
            internal SpriteState Original => state;

            internal ButtonState(Button button)
            {
                this.button = button; sprite = button.image.sprite; state = button.spriteState;
            }

            internal void Restore()
            {
                if (button == null) return;
                button.image.sprite = sprite; button.spriteState = state;
            }
        }
    }
}
