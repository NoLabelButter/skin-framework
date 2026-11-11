using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>Reskins the original language TMP_Dropdown without changing its options or callbacks.</summary>
    internal sealed class LanguageDropdownAdapter
    {
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly List<ImageState> imageStates = new List<ImageState>();
        private readonly List<TextState> textStates = new List<TextState>();
        private Transform styledDropdown;
        private SkinTheme styledTheme;
        private Transform styledItem;
        private Transform styledItemBackground;
        private Image overlayImage;
        private Sprite originalOverlaySprite;
        private bool deferredApplyPending;
        private Scene deferredScene;

        public void ApplyToActiveScene() => ApplyToScene(SceneManager.GetActiveScene());

        public void ApplyToScene(Scene scene)
        {
            if (SkinService.CurrentTheme.UsesGameDefaults)
            {
                Restore();
                return;
            }

            deferredScene = scene;
            if (!deferredApplyPending)
            {
                deferredApplyPending = true;
                Canvas.willRenderCanvases += ApplyBeforeCanvasRender;
            }
        }

        private void ApplyBeforeCanvasRender()
        {
            Canvas.willRenderCanvases -= ApplyBeforeCanvasRender;
            deferredApplyPending = false;
            Scene scene = deferredScene;
            deferredScene = default;
            ApplyNow(scene);
        }

        private void ApplyNow(Scene scene)
        {
            if (SkinService.CurrentTheme.UsesGameDefaults)
            {
                Restore();
                return;
            }

            Transform canvas = FindMenuCanvas(scene);
            Transform dropdown = FindLanguageDropdown(canvas);
            if (dropdown == null || dropdown.GetComponent<TMP_Dropdown>() == null)
            {
                Restore();
                return;
            }

            if (styledDropdown != dropdown || !ReferenceEquals(styledTheme, SkinService.CurrentTheme))
            {
                Restore();
                styledDropdown = dropdown;
                styledTheme = SkinService.CurrentTheme;
            }

            Sprite closed = LoadSprite("closed");
            Sprite panel = LoadSprite("panel");
            Sprite row = LoadSprite("row");
            Sprite selected = LoadSprite("selected");
            Sprite track = LoadSprite("scroll_track");
            Sprite thumb = LoadSprite("scroll_thumb");
            bool closedHidden = SkinService.IsTextureDisabled("language_dropdown_closed");
            bool panelHidden = SkinService.IsTextureDisabled("language_dropdown_panel");
            bool rowHidden = SkinService.IsTextureDisabled("language_dropdown_row");
            bool selectedHidden = SkinService.IsTextureDisabled("language_dropdown_selected");
            bool trackHidden = SkinService.IsTextureDisabled("language_dropdown_scroll_track");
            bool thumbHidden = SkinService.IsTextureDisabled("language_dropdown_scroll_thumb");
            if ((closed == null && !closedHidden) || (panel == null && !panelHidden) ||
                (row == null && !rowHidden) || (selected == null && !selectedHidden) ||
                (track == null && !trackHidden) || (thumb == null && !thumbHidden))
                return;

            TMP_Dropdown dropdownComponent = dropdown.GetComponent<TMP_Dropdown>();
            FitLanguageText(dropdownComponent.captionText, true);
            FitLanguageText(dropdownComponent.itemText, false);
            Image dropdownBackground = dropdownComponent.targetGraphic as Image;
            if (dropdownBackground == null)
                dropdownBackground = dropdown.GetComponent<Image>();
            if (closedHidden)
                SetImageEnabled(dropdownBackground, false);
            else
                SetImage(dropdownBackground, closed);
            Transform arrow = dropdown.Find("Arrow");
            Image arrowImage = arrow == null ? null : arrow.GetComponent<Image>();
            if (arrowImage != null)
                SetImageEnabled(arrowImage, false);

            Transform template = dropdownComponent.template != null
                ? dropdownComponent.template
                : dropdown.Find("Template");
            if (template == null)
                return;
            Image panelImage = template.GetComponent<Image>();
            if (panelHidden)
                SetImageEnabled(panelImage, false);
            else
                SetImage(panelImage, panel);

            ScrollRect scrollRect = template.GetComponent<ScrollRect>();

            Toggle itemToggle = dropdownComponent.itemText == null
                ? null
                : dropdownComponent.itemText.GetComponentInParent<Toggle>();
            Transform item = itemToggle == null
                ? template.Find("Viewport/Content/Item")
                : itemToggle.transform;
            Transform itemBackground = item == null ? null : item.Find("Item Background");
            Image itemImage = itemBackground == null
                ? null
                : itemBackground.GetComponent<Image>();
            if (rowHidden)
                SetImageEnabled(itemImage, false);
            else
                SetImage(itemImage, row);
            if (itemBackground != null && selected != null)
            {
                styledItem = item;
                styledItemBackground = itemBackground;
                EnsureSelectionOverlay(itemBackground, item, selected);
            }

            Scrollbar scrollComponent = scrollRect == null
                ? null
                : scrollRect.verticalScrollbar;
            Transform scrollbar = scrollComponent == null
                ? template.Find("Scrollbar")
                : scrollComponent.transform;
            if (scrollbar != null)
            {
                Image trackImage = scrollbar.GetComponent<Image>();
                if (trackHidden)
                    SetImageEnabled(trackImage, false);
                else
                    SetImage(trackImage, track);
                Transform handle = scrollbar.Find("Sliding Area/Handle");
                if (handle != null)
                {
                    Image thumbImage = handle.GetComponent<Image>();
                    if (thumbHidden)
                        SetImageEnabled(thumbImage, false);
                    else
                        SetImage(thumbImage, thumb);
                }
            }
        }

        public void Dispose() => Restore();

        private static Transform FindMenuCanvas(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            Transform canvas = null;
            bool hasMenuBackground = false;
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] descendants = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < descendants.Length; j++)
                {
                    if (descendants[j].name == "MenuBackgroundNew")
                        hasMenuBackground = true;
                    else if (canvas == null && descendants[j].name == "Canvas")
                        canvas = descendants[j];
                }
            }

            return hasMenuBackground ? canvas : null;
        }

        private static Transform FindLanguageDropdown(Transform canvas)
        {
            if (canvas == null)
                return null;

            // 0.46D contains an inactive LanguageSelector prototype and the
            // active right-corner LanguageSelectorDemo instance. Prefer the
            // active control so the menu the player sees receives the skin.
            Transform demo = canvas.Find("LanguageSelectorDemo");
            Transform selector = canvas.Find("LanguageSelector");
            if (IsDropdown(demo) && demo.gameObject.activeInHierarchy)
                return demo;
            if (IsDropdown(selector) && selector.gameObject.activeInHierarchy)
                return selector;
            if (IsDropdown(demo))
                return demo;
            return IsDropdown(selector) ? selector : null;
        }

        private static bool IsDropdown(Transform candidate) =>
            candidate != null && candidate.GetComponent<TMP_Dropdown>() != null;

        private Sprite LoadSprite(string key)
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] != null && sprites[i].name == key)
                    return sprites[i];
            }

            if (!SkinService.TryGetTexture("language_dropdown_" + key, out Texture2D texture))
                return null;
            Rect spriteRect = new Rect(0, 0, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, Vector4.zero);
            sprite.name = key;
            sprites.Add(sprite);
            return sprite;
        }

        private void SetImage(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            TrackImage(image);
            image.overrideSprite = null;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.enabled = true;
            image.fillCenter = true;
        }

        private void SetImageEnabled(Image image, bool enabled)
        {
            if (image == null)
                return;
            TrackImage(image);
            image.enabled = enabled;
        }

        private void TrackImage(Image image)
        {
            for (int i = 0; i < imageStates.Count; i++)
            {
                if (imageStates[i].image == image)
                    return;
            }

            imageStates.Add(new ImageState(image));
        }

        private void EnsureSelectionOverlay(Transform itemBackground, Transform item, Sprite selected)
        {
            Transform overlay = itemBackground.Find("SkinFrameworkSelected");
            if (overlay == null)
            {
                GameObject child = new GameObject("SkinFrameworkSelected", typeof(RectTransform), typeof(Image));
                overlay = child.transform;
                overlay.SetParent(itemBackground, false);
                RectTransform rect = (RectTransform)overlay;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                overlay.SetAsLastSibling();
            }

            Image image = overlay.GetComponent<Image>();
            if (overlayImage == null)
            {
                overlayImage = image;
                originalOverlaySprite = image.sprite;
            }
            image.sprite = selected;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;
            image.fillCenter = true;
            image.enabled = false;

            if (item != null && item.GetComponent<LanguageDropdownRowState>() == null)
                item.gameObject.AddComponent<LanguageDropdownRowState>();
        }

        private void Restore()
        {
            if (deferredApplyPending)
            {
                Canvas.willRenderCanvases -= ApplyBeforeCanvasRender;
                deferredApplyPending = false;
                deferredScene = default;
            }

            for (int i = imageStates.Count - 1; i >= 0; i--)
                imageStates[i].Restore();
            imageStates.Clear();
            for (int i = textStates.Count - 1; i >= 0; i--)
                textStates[i].Restore();
            textStates.Clear();

            if (overlayImage != null)
                overlayImage.sprite = originalOverlaySprite;
            if (styledItemBackground != null)
            {
                Transform overlay = styledItemBackground.Find("SkinFrameworkSelected");
                if (overlay != null)
                    UnityEngine.Object.Destroy(overlay.gameObject);
                LanguageDropdownRowState rowState = styledItem == null
                    ? null
                    : styledItem.GetComponent<LanguageDropdownRowState>();
                if (rowState != null)
                    UnityEngine.Object.Destroy(rowState);
            }

            overlayImage = null;
            originalOverlaySprite = null;
            styledItem = null;
            styledItemBackground = null;
            styledDropdown = null;
            styledTheme = null;
            for (int i = 0; i < sprites.Count; i++)
                if (sprites[i] != null) UnityEngine.Object.Destroy(sprites[i]);
            sprites.Clear();
        }

        private void FitLanguageText(TMP_Text text, bool caption)
        {
            if (text == null)
                return;
            for (int i = 0; i < textStates.Count; i++)
                if (textStates[i].text == text) return;
            textStates.Add(new TextState(text));
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Min(10f, text.fontSizeMax);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Truncate;
            Vector4 margin = text.margin;
            margin.x = Mathf.Max(margin.x, 5f);
            margin.z = Mathf.Max(margin.z, caption ? 25f : 5f);
            text.margin = margin;
        }

        private sealed class TextState
        {
            public readonly TMP_Text text;
            private readonly bool autoSize, wordWrap;
            private readonly float fontSize, minSize, maxSize;
            private readonly Vector4 margin;
            private readonly TextOverflowModes overflow;
            public TextState(TMP_Text text)
            {
                this.text = text;
                autoSize = text.enableAutoSizing;
                wordWrap = text.enableWordWrapping;
                fontSize = text.fontSize;
                minSize = text.fontSizeMin;
                maxSize = text.fontSizeMax;
                margin = text.margin;
                overflow = text.overflowMode;
            }
            public void Restore()
            {
                if (text == null) return;
                text.enableAutoSizing = autoSize;
                text.enableWordWrapping = wordWrap;
                text.fontSize = fontSize;
                text.fontSizeMin = minSize;
                text.fontSizeMax = maxSize;
                text.margin = margin;
                text.overflowMode = overflow;
            }
        }

        private sealed class ImageState
        {
            public readonly Image image;
            private readonly Sprite sprite;
            private readonly Sprite overrideSprite;
            private readonly Image.Type type;
            private readonly Color color;
            private readonly bool enabled;
            private readonly bool fillCenter;

            public ImageState(Image image)
            {
                this.image = image;
                sprite = image.sprite;
                overrideSprite = image.overrideSprite == sprite ? null : image.overrideSprite;
                type = image.type;
                color = image.color;
                enabled = image.enabled;
                fillCenter = image.fillCenter;
            }

            public void Restore()
            {
                if (image == null)
                    return;
                image.sprite = sprite;
                image.overrideSprite = overrideSprite;
                image.type = type;
                image.color = color;
                image.enabled = enabled;
                image.fillCenter = fillCenter;
            }
        }
    }
}
