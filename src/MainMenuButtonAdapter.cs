using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace SkinFramework
{
    /// <summary>按原版主菜单层级分别替换六个按钮的图片，保留按钮与文字组件。</summary>
    internal sealed class MainMenuButtonAdapter
    {
        private static readonly ButtonTarget[] Targets =
        {
            new ButtonTarget("Button", SkinTextureSlots.MainMenuNewGameButton),
            new ButtonTarget("Button (1)", SkinTextureSlots.MainMenuLoadGameButton),
            new ButtonTarget("Button (2)", SkinTextureSlots.MainMenuOptionsButton),
            new ButtonTarget("Button (3)", SkinTextureSlots.MainMenuCreditsButton),
            new ButtonTarget("ButtonMod", SkinTextureSlots.MainMenuModsButton),
            new ButtonTarget("Button (4)", SkinTextureSlots.MainMenuQuitButton)
        };

        private readonly ButtonState[] states = new ButtonState[Targets.Length];
        private readonly Dictionary<TextMeshProUGUI, TextColorState> textStates =
            new Dictionary<TextMeshProUGUI, TextColorState>();
        private readonly Dictionary<ButtonTextColor, bool> originalTextColorHandlers =
            new Dictionary<ButtonTextColor, bool>();
        private Color textColor;
        private bool hasTextColor;

        public MainMenuButtonAdapter()
        {
            for (int i = 0; i < states.Length; i++)
                states[i] = new ButtonState();
        }

        public void ApplyToActiveScene() => ApplyToScene(SceneManager.GetActiveScene());

        public void SetTextColor(Color color)
        {
            textColor = color;
            hasTextColor = true;
            ApplyToActiveScene();
        }

        public void ClearTextColor()
        {
            hasTextColor = false;
            RestoreTextColors();
        }

        public void ApplyToScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                RestoreAll();
                return;
            }

            Transform layout = FindLayout(scene);
            if (layout == null)
            {
                RestoreAll();

                return;
            }

            for (int i = 0; i < Targets.Length; i++)
                ApplyButton(layout, i);
            ApplyTextColor(layout);

        }

        public void Dispose() => RestoreAll();

        private static Transform FindLayout(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            Transform layout = null;
            bool hasMenuBackground = false;
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] descendants = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < descendants.Length; j++)
                {
                    if (descendants[j].name == "MenuBackgroundNew")
                        hasMenuBackground = true;
                    else if (layout == null && descendants[j].name == "Canvas")
                        layout = descendants[j].Find("LayoutGroup");
                }
            }

            return hasMenuBackground ? layout : null;
        }

        private bool ApplyButton(Transform layout, int index)
        {
            ButtonState state = states[index];
            Transform target = layout == null ? null : layout.Find(Targets[index].ObjectName);
            Button button = target == null ? null : target.GetComponent<Button>();
            Image image = button == null ? null : button.targetGraphic as Image;
            if (image == null && button != null)
                image = button.GetComponent<Image>();
            if (image == null)
            {
                Restore(index);
                return false;
            }

            if (state.Image != image)
            {
                Restore(index);
                state.Image = image;
                state.Button = button;
                state.OriginalSprite = image.sprite;
                state.OriginalEnabled = image.enabled;
                state.OriginalSpriteState = button.spriteState;
            }

            if (!SkinService.TryGetTexture(Targets[index].TextureSlot, out Texture2D texture))
            {
                RestoreSprite(index);
                if (state.Button != null)
                    state.Button.spriteState = state.OriginalSpriteState;
                image.enabled = SkinService.IsTextureDisabled(Targets[index].TextureSlot)
                    ? false
                    : state.OriginalEnabled;
                return false;
            }

            image.enabled = state.OriginalEnabled;

            if (state.Texture == texture)
            {
                if (image.sprite != state.ReplacementSprite)
                    image.sprite = state.ReplacementSprite;
                return true;
            }

            DestroyReplacement(state);
            state.Texture = texture;
            state.ReplacementSprite = Sprite.Create(
                texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            state.ReplacementSprite.name = "SkinFramework_" + Targets[index].TextureSlot;
            image.sprite = state.ReplacementSprite;

            if (button.transition == Selectable.Transition.SpriteSwap)
            {
                SpriteState spriteState = button.spriteState;
                spriteState.highlightedSprite = state.ReplacementSprite;
                spriteState.pressedSprite = state.ReplacementSprite;
                spriteState.selectedSprite = state.ReplacementSprite;
                spriteState.disabledSprite = state.ReplacementSprite;
                button.spriteState = spriteState;
            }

            return true;
        }

        private void RestoreAll()
        {
            for (int i = 0; i < states.Length; i++)
                Restore(i);
            RestoreTextColors();
        }

        private void ApplyTextColor(Transform layout)
        {
            if (!hasTextColor || layout == null)
                return;

            for (int i = 0; i < Targets.Length; i++)
            {
                Transform button = layout.Find(Targets[i].ObjectName);
                if (button == null)
                    continue;

                ButtonTextColor[] colorHandlers = button.GetComponents<ButtonTextColor>();
                for (int j = 0; j < colorHandlers.Length; j++)
                {
                    ButtonTextColor handler = colorHandlers[j];
                    if (originalTextColorHandlers.ContainsKey(handler))
                        continue;

                    bool wasEnabled = handler.enabled;
                    originalTextColorHandlers.Add(handler, wasEnabled);
                    if (wasEnabled)
                        handler.enabled = false;
                }

                TextMeshProUGUI[] texts = button.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int j = 0; j < texts.Length; j++)
                {
                    TextMeshProUGUI text = texts[j];
                    if (!textStates.TryGetValue(text, out TextColorState state))
                    {
                        state = new TextColorState(text.color);
                        textStates.Add(text, state);
                    }

                    text.color = textColor;
                    state.Applied = textColor;
                }
            }
        }

        private void RestoreTextColors()
        {
            foreach (KeyValuePair<TextMeshProUGUI, TextColorState> pair in textStates)
            {
                if (pair.Key != null && pair.Key.color == pair.Value.Applied)
                    pair.Key.color = pair.Value.Original;
            }
            textStates.Clear();

            foreach (KeyValuePair<ButtonTextColor, bool> pair in originalTextColorHandlers)
            {
                if (pair.Key != null)
                    pair.Key.enabled = pair.Value;
            }
            originalTextColorHandlers.Clear();
        }

        private void Restore(int index)
        {
            ButtonState state = states[index];
            RestoreSprite(index);
            if (state.Button != null)
                state.Button.spriteState = state.OriginalSpriteState;
            state.Image = null;
            state.Button = null;
            state.OriginalSprite = null;
            state.OriginalEnabled = false;
            state.Texture = null;
        }

        private void RestoreSprite(int index)
        {
            ButtonState state = states[index];
            if (state.Image != null && state.Image.sprite == state.ReplacementSprite)
                state.Image.sprite = state.OriginalSprite;
            if (state.Image != null)
                state.Image.enabled = state.OriginalEnabled;
            DestroyReplacement(state);
        }

        private static void DestroyReplacement(ButtonState state)
        {
            if (state.ReplacementSprite != null)
                Object.Destroy(state.ReplacementSprite);
            state.ReplacementSprite = null;
            state.Texture = null;
        }

        private sealed class ButtonState
        {
            public Image Image;
            public Button Button;
            public Sprite OriginalSprite;
            public bool OriginalEnabled;
            public SpriteState OriginalSpriteState;
            public Texture2D Texture;
            public Sprite ReplacementSprite;
        }

        private sealed class TextColorState
        {
            public Color Original { get; }
            public Color Applied;

            public TextColorState(Color original)
            {
                Original = original;
                Applied = original;
            }
        }

        private sealed class ButtonTarget
        {
            public string ObjectName { get; }
            public string TextureSlot { get; }

            public ButtonTarget(string objectName, string textureSlot)
            {
                ObjectName = objectName;
                TextureSlot = textureSlot;
            }
        }
    }
}
