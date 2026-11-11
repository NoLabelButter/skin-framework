using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>按主题替换新游戏专精页的图像组件，并在窗口关闭时恢复原版引用。</summary>
    internal sealed class SpecializationPageAdapter
    {
        private const string WindowObjectName = "custom_window_new_game_specialization";
        private readonly ModLog log;
        private readonly List<ImageState> imageStates = new List<ImageState>();
        private readonly List<ButtonState> buttonStates = new List<ButtonState>();
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly Dictionary<string, Sprite> spritesBySlot = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private FrameState frameState;
        private bool loggedMissingWindow;

        public SpecializationPageAdapter(ModLog log) => this.log = log;

        public void ApplyToOpenPage()
        {
            if (!NewGameSpecializationUI.IsOpen ||
                (SkinService.CurrentTheme.UsesGameDefaults && !HasDisabledSpecializationSlot()))
            {
                Restore();
                return;
            }

            Restore();
            GameObject root = GameObject.Find(WindowObjectName);
            NewGameSpecializationFrame frame = root == null
                ? null
                : root.GetComponent<NewGameSpecializationFrame>();
            if (frame == null)
            {
                if (!loggedMissingWindow)
                {
                    log.Log("专精页已打开，但没有找到 custom_window_new_game_specialization / NewGameSpecializationFrame；本次保留原版外观。");
                    loggedMissingWindow = true;
                }
                return;
            }

            loggedMissingWindow = false;
            frameState = new FrameState(frame);
            ApplyImage(root.GetComponent<Image>(), SkinTextureSlots.SpecializationWindow, Image.Type.Simple);
            Transform background = frame.panel == null ? null : frame.panel.Find("Background");
            ApplyImage(background == null ? null : background.GetComponent<Image>(),
                SkinTextureSlots.SpecializationPanel, Image.Type.Simple);

            ApplyImage(FindPanelImage(frame, "Separator1"), SkinTextureSlots.SpecializationSeparator, Image.Type.Simple);
            ApplyImage(FindPanelImage(frame, "Separator2"), SkinTextureSlots.SpecializationSeparator, Image.Type.Simple);
            ApplyImage(FindPanelImage(frame, "SeparatorBottom"), SkinTextureSlots.SpecializationSeparator, Image.Type.Simple);
            ApplyArrows(frame);
            ApplyContinueFrame(frame);
            ApplySlotFrames(frame);
            ApplyScenarioIcons(frame);

            log.Log("新游戏专精页主题贴图已解析并应用；专精图标与底槽未配置时保留游戏原版。");
        }

        public void Dispose() => Restore();

        public void Restore()
        {
            for (int i = imageStates.Count - 1; i >= 0; i--)
                imageStates[i].Restore();
            imageStates.Clear();

            for (int i = buttonStates.Count - 1; i >= 0; i--)
                buttonStates[i].Restore();
            buttonStates.Clear();

            if (frameState != null && frameState.Frame != null)
                frameState.Restore();
            frameState = null;

            for (int i = 0; i < sprites.Count; i++)
                if (sprites[i] != null) UnityEngine.Object.Destroy(sprites[i]);
            sprites.Clear();
            spritesBySlot.Clear();
        }

        private static Image FindPanelImage(NewGameSpecializationFrame frame, string name)
        {
            Transform child = frame.panel == null ? null : frame.panel.Find(name);
            return child == null ? null : child.GetComponent<Image>();
        }

        private void ApplyArrows(NewGameSpecializationFrame frame)
        {
            bool prevHidden = SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationArrowPrev);
            bool prevHoverHidden = SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationArrowPrevHover);
            Sprite prevNormal = prevHidden ? null : LoadSprite(SkinTextureSlots.SpecializationArrowPrev, Vector4.zero);
            Sprite prevHover = prevHoverHidden ? null : LoadSprite(SkinTextureSlots.SpecializationArrowPrevHover, Vector4.zero);
            if (prevHidden)
            {
                HideImage(GetButtonImage(frame.prevButton));
                HideImage(frame.prevImage);
                HideImage(GetButtonImage(frame.backButton));
            }
            else if (prevNormal != null || prevHover != null || prevHoverHidden)
            {
                Sprite normal = prevNormal ?? frameState.ArrowPrevNormal;
                Sprite hover = prevHoverHidden ? null : prevHover ?? frameState.ArrowPrevHover;
                frame.arrowPrevNormal = normal;
                frame.arrowPrevHover = hover;
                ApplyButton(frame.prevButton, frame.prevImage, normal, hover);
                ApplyButton(frame.backButton, null, normal, hover);
            }

            bool nextHidden = SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationArrowNext);
            bool nextHoverHidden = SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationArrowNextHover);
            Sprite nextNormal = nextHidden ? null : LoadSprite(SkinTextureSlots.SpecializationArrowNext, Vector4.zero);
            Sprite nextHover = nextHoverHidden ? null : LoadSprite(SkinTextureSlots.SpecializationArrowNextHover, Vector4.zero);
            if (nextHidden)
            {
                HideImage(frame.nextImage);
            }
            else if (nextNormal != null || nextHover != null || nextHoverHidden)
            {
                Sprite normal = nextNormal ?? frameState.ArrowNextNormal;
                Sprite hover = nextHoverHidden ? null : nextHover ?? frameState.ArrowNextHover;
                frame.arrowNextNormal = normal;
                frame.arrowNextHover = hover;
                ApplyButton(frame.nextButton, frame.nextImage, normal, hover);
            }
        }

        private void ApplyContinueFrame(NewGameSpecializationFrame frame)
        {
            if (SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationContinueFrame))
            {
                HideImage(frame.continueOuter);
                HideImage(frame.continueInner);
                return;
            }

            Sprite sprite = LoadSprite(SkinTextureSlots.SpecializationContinueFrame, new Vector4(2f, 2f, 2f, 2f));
            if (sprite == null)
                return;

            frame.frame2Square = sprite;
            ApplyImage(frame.continueOuter, sprite, Image.Type.Sliced);
            ApplyImage(frame.continueInner, sprite, Image.Type.Sliced);
        }

        private void ApplySlotFrames(NewGameSpecializationFrame frame)
        {
            bool normalHidden = SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationSlotNormal);
            bool selectedHidden = SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationSlotSelected);
            Sprite normal = normalHidden ? null : LoadSprite(SkinTextureSlots.SpecializationSlotNormal, Vector4.zero);
            Sprite selected = selectedHidden ? null : LoadSprite(SkinTextureSlots.SpecializationSlotSelected, Vector4.zero);
            if (normal == null && selected == null && !normalHidden && !selectedHidden)
                return;

            frame.slotNormal = normal ?? frameState.SlotNormal;
            frame.slotSelected = selected ?? frameState.SlotSelected;
            if (frame.stripSlots == null)
                return;

            for (int i = 0; i < frame.stripSlots.childCount; i++)
            {
                Transform slot = frame.stripSlots.GetChild(i);
                Image image = slot.GetComponent<Image>();
                if (image == null)
                    continue;

                Sprite original = image.sprite;
                if (original == frameState.SlotSelected && selected != null)
                    ApplyImage(image, selected, Image.Type.Simple);
                else if (original == frameState.SlotSelected && selectedHidden)
                    HideImage(image);
                else if (original == frameState.SlotNormal && normal != null)
                    ApplyImage(image, normal, Image.Type.Simple);
                else if (original == frameState.SlotNormal && normalHidden)
                    HideImage(image);
            }
        }

        private void ApplyScenarioIcons(NewGameSpecializationFrame frame)
        {
            if (frame.stripSlots == null)
                return;

            for (int i = 0; i < frame.stripSlots.childCount; i++)
            {
                Transform slot = frame.stripSlots.GetChild(i);
                Transform icon = slot.Find("icon");
                Image image = icon == null ? null : icon.GetComponent<Image>();
                if (image == null || !slot.name.StartsWith("slot_", StringComparison.Ordinal))
                    continue;

                string scenarioId = slot.name.Substring("slot_".Length);
                string scenarioSlot = SkinTextureSlots.SpecializationIcon + "_" + scenarioId;
                if (SkinService.IsTextureDisabled(scenarioSlot))
                {
                    HideImage(image);
                    continue;
                }

                Sprite sprite = LoadSprite(scenarioSlot, Vector4.zero);
                if (sprite == null && SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationIcon))
                {
                    HideImage(image);
                    continue;
                }
                if (sprite == null)
                    sprite = LoadSprite(SkinTextureSlots.SpecializationIcon, Vector4.zero);
                if (sprite != null)
                    ApplyImage(image, sprite, Image.Type.Simple);
            }
        }

        private void ApplyButton(Button button, Image preferredImage, Sprite normal, Sprite hover)
        {
            if (button == null)
                return;

            TrackButton(button);
            Image image = preferredImage ?? button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (normal != null && normal.name.StartsWith("SkinFramework_", StringComparison.Ordinal))
                ApplyImage(image, normal, Image.Type.Simple);
            SpriteState state = button.spriteState;
            state.highlightedSprite = hover;
            state.pressedSprite = hover;
            state.selectedSprite = normal;
            state.disabledSprite = normal;
            button.spriteState = state;
            if (image != null && !button.interactable)
                image.color = new Color(1f, 1f, 1f, 0.3f);
        }

        private void ApplyImage(Image image, string slot, Image.Type type)
        {
            if (SkinService.IsTextureDisabled(slot))
            {
                HideImage(image);
                return;
            }

            Sprite sprite = LoadSprite(slot, Vector4.zero);
            if (sprite != null)
                ApplyImage(image, sprite, type);
        }

        private void ApplyImage(Image image, Sprite sprite, Image.Type type)
        {
            if (image == null || sprite == null)
                return;

            TrackImage(image);
            image.overrideSprite = null;
            image.sprite = sprite;
            image.type = type;
            image.color = Color.white;
            image.enabled = true;
        }

        private Sprite LoadSprite(string slot, Vector4 border)
        {
            if (!SkinService.TryGetTexture(slot, out Texture2D texture))
                return null;
            if (spritesBySlot.TryGetValue(slot, out Sprite cached))
                return cached;

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = "SkinFramework_" + slot;
            sprites.Add(sprite);
            spritesBySlot.Add(slot, sprite);
            return sprite;
        }

        private void TrackImage(Image image)
        {
            for (int i = 0; i < imageStates.Count; i++)
                if (imageStates[i].Image == image) return;
            imageStates.Add(new ImageState(image));
        }

        private void HideImage(Image image)
        {
            if (image == null)
                return;
            TrackImage(image);
            image.enabled = false;
        }

        private static Image GetButtonImage(Button button) => button == null
            ? null
            : button.targetGraphic as Image ?? button.GetComponent<Image>();

        private static bool HasDisabledSpecializationSlot()
        {
            return SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationWindow) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationPanel) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationSeparator) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationArrowPrev) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationArrowPrevHover) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationArrowNext) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationArrowNextHover) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationContinueFrame) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationSlotNormal) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationSlotSelected) ||
                SkinService.IsTextureDisabled(SkinTextureSlots.SpecializationIcon);
        }

        private void TrackButton(Button button)
        {
            for (int i = 0; i < buttonStates.Count; i++)
                if (buttonStates[i].Button == button) return;
            buttonStates.Add(new ButtonState(button));
        }

        private sealed class FrameState
        {
            public readonly NewGameSpecializationFrame Frame;
            public readonly Sprite ArrowPrevNormal, ArrowPrevHover, ArrowNextNormal, ArrowNextHover;
            public readonly Sprite ContinueFrame, SlotNormal, SlotSelected;

            public FrameState(NewGameSpecializationFrame frame)
            {
                Frame = frame;
                ArrowPrevNormal = frame.arrowPrevNormal;
                ArrowPrevHover = frame.arrowPrevHover;
                ArrowNextNormal = frame.arrowNextNormal;
                ArrowNextHover = frame.arrowNextHover;
                ContinueFrame = frame.frame2Square;
                SlotNormal = frame.slotNormal;
                SlotSelected = frame.slotSelected;
            }

            public void Restore()
            {
                Frame.arrowPrevNormal = ArrowPrevNormal;
                Frame.arrowPrevHover = ArrowPrevHover;
                Frame.arrowNextNormal = ArrowNextNormal;
                Frame.arrowNextHover = ArrowNextHover;
                Frame.frame2Square = ContinueFrame;
                Frame.slotNormal = SlotNormal;
                Frame.slotSelected = SlotSelected;
            }
        }

        private sealed class ImageState
        {
            public readonly Image Image;
            private readonly Sprite sprite, overrideSprite;
            private readonly Image.Type type;
            private readonly Color color;
            private readonly bool enabled;

            public ImageState(Image image)
            {
                Image = image;
                sprite = image.sprite;
                overrideSprite = image.overrideSprite;
                type = image.type;
                color = image.color;
                enabled = image.enabled;
            }

            public void Restore()
            {
                if (Image == null)
                    return;
                Image.overrideSprite = overrideSprite;
                Image.sprite = sprite;
                Image.type = type;
                Image.color = color;
                Image.enabled = enabled;
            }
        }

        private sealed class ButtonState
        {
            public readonly Button Button;
            private readonly SpriteState spriteState;

            public ButtonState(Button button)
            {
                Button = button;
                spriteState = button.spriteState;
            }

            public void Restore()
            {
                if (Button != null)
                    Button.spriteState = spriteState;
            }
        }
    }

    [HarmonyPatch(typeof(NewGameSpecializationUI), "BuildStrip")]
    internal static class SpecializationPageRebuildPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            ModEntry.BeforeSpecializationStripRebuilt();
            ModEntry.RestoreSpecializationPageSkin();
        }
    }

    [HarmonyPatch(typeof(NewGameSpecializationUI), nameof(NewGameSpecializationUI.Open))]
    internal static class SpecializationPageOpenPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.ApplySpecializationPageSkin();
    }

    [HarmonyPatch(typeof(NewGameSpecializationUI), "OnWindowClosed")]
    internal static class SpecializationPageClosedPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.RestoreSpecializationPageSkin();
    }
}
