using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    internal sealed class FixedFixtureTarget
    {
        internal readonly string Id;
        internal readonly string Name;
        internal readonly string Group;
        internal readonly SpriteRenderer Renderer;
        internal readonly Image[] Images;
        internal readonly bool IsMenuTarget;
        private FixedFixtureButtonSkin buttonSkin;
        private readonly SimpleAnimator recorderAnimator;
        private readonly List<Sprite> recorderBaseFrames;
        private readonly SimpleAnimator clienteleAnimator;
        private readonly List<Sprite> clienteleBaseFrames;
        private readonly Dictionary<(Texture2D, int), List<Sprite>> clienteleReplacementSprites =
            new Dictionary<(Texture2D, int), List<Sprite>>();
        private readonly Dictionary<Texture2D, Sprite> recorderReplacementSprites =
            new Dictionary<Texture2D, Sprite>();
        private readonly Vector3[] imageBasePositions;
        private readonly Color[] imageBaseColors;
        private Sprite rendererBaseSprite;
        private readonly Sprite[] imageBaseSprites;
        private readonly Dictionary<(Sprite, Texture2D), Sprite> replacementSprites =
            new Dictionary<(Sprite, Texture2D), Sprite>();
        private readonly Dictionary<Sprite, ColorAdjustedSprite> colorAdjustedSprites =
            new Dictionary<Sprite, ColorAdjustedSprite>();
        private readonly Dictionary<Sprite, Sprite> colorAdjustmentSources =
            new Dictionary<Sprite, Sprite>();
        private readonly Vector3 rendererBasePosition;
        private readonly Color rendererBaseColor;
        internal Transform Transform
        {
            get
            {
                if (Renderer != null) return Renderer.transform;
                for (int i = 0; i < Images.Length; i++)
                    if (Images[i] != null) return Images[i].transform;
                return null;
            }
        }
        internal Sprite Sprite
        {
            get
            {
                if (Renderer != null)
                    return Renderer.sprite;
                for (int i = 0; i < Images.Length; i++)
                    if (Images[i] != null && Images[i].sprite != null)
                        return Images[i].sprite;
                return null;
            }
        }
        internal Sprite RendererBaseSprite => rendererBaseSprite;
        internal Sprite BaseSprite => Renderer != null
            ? rendererBaseSprite
            : imageBaseSprites != null && imageBaseSprites.Length > 0 ? imageBaseSprites[0] : null;

        internal FixedFixtureTarget(string id, string name, string group, SpriteRenderer renderer)
        {
            Id = id;
            Name = name;
            Group = group;
            Renderer = renderer;
            Images = new Image[0];
            IsMenuTarget = false;
            rendererBaseSprite = renderer.sprite;
            if (id == "--Interactable/ShutterButton")
                buttonSkin = FixedFixtureButtonSkin.ForShutter(renderer);
            else if (id == "--Interactable/Bell")
                buttonSkin = FixedFixtureButtonSkin.ForBell(renderer);
            if (id == "--Interactable/Recorder")
            {
                recorderAnimator = renderer.GetComponent<SimpleAnimator>();
                recorderBaseFrames = new List<Sprite>(recorderAnimator.sprites);
            }
            if (id == "--Interactable/ClienteleDisplay")
            {
                clienteleAnimator = renderer.GetComponent<SimpleAnimator>();
                clienteleBaseFrames = new List<Sprite>(clienteleAnimator.sprites);
            }
            rendererBasePosition = renderer.transform.localPosition;
            rendererBaseColor = renderer.color;
        }

        internal FixedFixtureTarget(string id, string name, string group, Image[] images, bool isMenuTarget = false)
        {
            Id = id;
            Name = name;
            Group = group;
            Renderer = null;
            Images = images;
            IsMenuTarget = isMenuTarget;
            imageBasePositions = new Vector3[images.Length];
            imageBaseColors = new Color[images.Length];
            imageBaseSprites = new Sprite[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                imageBasePositions[i] = images[i].transform.localPosition;
                imageBaseColors[i] = images[i].color;
                imageBaseSprites[i] = images[i].sprite;
            }
        }

        internal bool IsAlive => Renderer != null
            ? Renderer.sprite != null
            : Transform != null && Sprite != null;

        internal void Apply(Vector2 offset, float alpha)
        {
            if (Renderer != null)
            {
                Vector3 position = rendererBasePosition;
                Renderer.transform.localPosition = position + new Vector3(offset.x, offset.y, 0f);
                Color color = rendererBaseColor;
                color.a *= alpha;
                Renderer.color = color;
                return;
            }
            for (int i = 0; i < Images.Length; i++)
            {
                Image image = Images[i];
                if (image == null)
                    continue;
                image.transform.localPosition = imageBasePositions[i] + new Vector3(offset.x, offset.y, 0f);
                Color color = imageBaseColors[i];
                color.a *= alpha;
                image.color = color;
            }
        }

        internal void ApplyButtonTexture(string normalSlot, string pressedSlot, string normalOverride = null) =>
            buttonSkin.Apply(normalSlot, pressedSlot, normalOverride);

        internal void ApplyOriginalTexture()
        {
            if (buttonSkin != null)
                buttonSkin.Restore();
            else
                RestoreSprites();
        }

        internal void ApplyTexture(string slot) => ApplyTexture(slot, null);

        internal bool ApplyTexture(string slot, string textureKeyOverride)
        {
            if (string.IsNullOrEmpty(slot))
                return false;
            Texture2D texture;
            string textureKey;
            bool loaded = string.IsNullOrEmpty(textureKeyOverride)
                ? SkinService.TryGetTexture(slot, out texture, out textureKey)
                : SkinService.TryGetTextureByKey(slot, textureKeyOverride, out texture, out textureKey);
            if (!loaded)
            {
                RestoreSprites();
                return false;
            }

            if (Renderer != null)
            {
                if (Id == "--Interactable/ClienteleDisplay")
                {
                    ApplyClienteleTexture(texture, textureKey);
                    return true;
                }
                if (Id == "--Interactable/Recorder")
                {
                    if (!recorderReplacementSprites.TryGetValue(texture, out Sprite animationSprite))
                    {
                        Sprite source = recorderBaseFrames.Count > 0 ? recorderBaseFrames[0] : rendererBaseSprite;
                        Rect sourceRect = source.rect;
                        Vector2 pivot = new Vector2(source.pivot.x / sourceRect.width,
                            source.pivot.y / sourceRect.height);
                        animationSprite = Sprite.Create(texture,
                            new Rect(0f, 0f, texture.width, texture.height), pivot,
                            source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
                        animationSprite.name = "chiikawa_recorder_SkinFramework";
                        recorderReplacementSprites.Add(texture, animationSprite);
                    }

                    recorderAnimator.sprites.Clear();
                    for (int i = 0; i < recorderBaseFrames.Count; i++)
                        recorderAnimator.sprites.Add(animationSprite);
                    Renderer.sprite = animationSprite;
                    return true;
                }
                Renderer.sprite = GetReplacementSprite(rendererBaseSprite, texture);
                return true;
            }

            for (int i = 0; i < Images.Length; i++)
                if (Images[i] != null)
                    Images[i].sprite = GetReplacementSprite(imageBaseSprites[i], texture);
            return true;
        }

        internal void UpdateRendererBaseSprite(Sprite source) => rendererBaseSprite = source;

        internal void ApplyImageTextures(System.Func<Sprite, string> getSlot)
        {
            for (int i = 0; i < Images.Length; i++)
            {
                Image image = Images[i];
                Sprite source = imageBaseSprites[i];
                string slot = getSlot(source);
                if (image == null || string.IsNullOrEmpty(slot) ||
                    !SkinService.TryGetTexture(slot, out Texture2D texture))
                {
                    if (image != null)
                        image.sprite = source;
                    continue;
                }
                image.sprite = GetReplacementSprite(source, texture);
            }
        }

        internal void ApplyColorAdjustment(float contrast, float brightness, float saturation)
        {
            if (contrast == 1f && brightness == 0f && saturation == 1f)
            {
                RestoreColorAdjustedSprites();
                return;
            }

            System.Func<Sprite, Sprite> adjust = sprite => GetColorAdjustedSprite(sprite, contrast, brightness, saturation);
            if (buttonSkin != null)
            {
                buttonSkin.ApplyColorAdjustment(adjust);
                return;
            }
            if (Renderer != null)
            {
                if (recorderAnimator != null)
                {
                    for (int i = 0; i < recorderAnimator.sprites.Count; i++)
                        recorderAnimator.sprites[i] = adjust(recorderAnimator.sprites[i]);
                    if (recorderAnimator.sprites.Count > 0)
                        Renderer.sprite = recorderAnimator.sprites[0];
                    return;
                }
                if (clienteleAnimator != null)
                {
                    for (int i = 0; i < clienteleAnimator.sprites.Count; i++)
                        clienteleAnimator.sprites[i] = adjust(clienteleAnimator.sprites[i]);
                    if (clienteleAnimator.sprites.Count > 0)
                        Renderer.sprite = clienteleAnimator.sprites[0];
                    return;
                }
                Renderer.sprite = adjust(Renderer.sprite);
                return;
            }
            for (int i = 0; i < Images.Length; i++)
                if (Images[i] != null)
                    Images[i].sprite = adjust(Images[i].sprite);
        }

        internal Sprite UpdateImageBaseSpritesAndGetTexture(Sprite source, string slot)
        {
            for (int i = 0; i < imageBaseSprites.Length; i++)
                imageBaseSprites[i] = source;
            return !string.IsNullOrEmpty(slot) && SkinService.TryGetTexture(slot, out Texture2D texture)
                ? GetReplacementSprite(source, texture)
                : source;
        }

        internal void Restore()
        {
            RestoreSprites();
            if (Renderer != null)
            {
                Renderer.transform.localPosition = rendererBasePosition;
                Renderer.color = rendererBaseColor;
                return;
            }
            for (int i = 0; i < Images.Length; i++)
            {
                if (Images[i] == null)
                    continue;
                Images[i].transform.localPosition = imageBasePositions[i];
                Images[i].color = imageBaseColors[i];
            }
        }

        internal void Dispose()
        {
            buttonSkin?.Restore();
            Restore();
            buttonSkin?.Release();
            foreach (Sprite sprite in replacementSprites.Values)
                UnityEngine.Object.Destroy(sprite);
            replacementSprites.Clear();
            foreach (ColorAdjustedSprite adjusted in colorAdjustedSprites.Values)
            {
                UnityEngine.Object.Destroy(adjusted.Sprite);
                UnityEngine.Object.Destroy(adjusted.Texture);
            }
            colorAdjustedSprites.Clear();
            colorAdjustmentSources.Clear();
            foreach (Sprite sprite in recorderReplacementSprites.Values)
                UnityEngine.Object.Destroy(sprite);
            recorderReplacementSprites.Clear();
            foreach (List<Sprite> frames in clienteleReplacementSprites.Values)
                foreach (Sprite sprite in frames)
                    UnityEngine.Object.Destroy(sprite);
            clienteleReplacementSprites.Clear();
        }

        private void ApplyClienteleTexture(Texture2D texture, string textureKey)
        {
            Sprite anchor = clienteleBaseFrames[0];
            Rect anchorRect = anchor.rect;
            var layout = new ClienteleFrameLayout(textureKey, texture.width, anchorRect.width, anchor.pivot.x);
            var cacheKey = (texture, layout.Padding);
            if (!clienteleReplacementSprites.TryGetValue(cacheKey, out List<Sprite> frames))
            {
                frames = new List<Sprite>();
                Vector2 pivot = new Vector2(layout.PivotX, anchor.pivot.y / anchorRect.height);
                float pixelsPerUnit = anchor.pixelsPerUnit * layout.ContentScale;
                foreach (Sprite source in clienteleBaseFrames)
                {
                    int index;
                    switch (source.name)
                    {
                        case "clientele-Sheet_0": index = 0; break;
                        case "clientele-Sheet_1": index = 1; break;
                        case "clientele-Sheet_2": index = 2; break;
                        case "clientele-Sheet_3": index = 3; break;
                        default: throw new System.InvalidOperationException("Unknown clientele frame: " + source.name);
                    }
                    int left = layout.CellWidth * index;
                    Sprite replacement = Sprite.Create(texture, new Rect(left, 0, layout.CellWidth, texture.height),
                        pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    replacement.name = source.name + "_SkinFramework";
                    frames.Add(replacement);
                }
                clienteleReplacementSprites.Add(cacheKey, frames);
            }
            clienteleAnimator.sprites.Clear();
            clienteleAnimator.sprites.AddRange(frames);
            Renderer.sprite = frames[0];
        }

        private Sprite GetReplacementSprite(Sprite source, Texture2D texture)
        {
            if (source == null)
                return null;
            var cacheKey = (source, texture);
            if (replacementSprites.TryGetValue(cacheKey, out Sprite replacement))
                return replacement;

            Rect sourceRect = source.rect;
            Vector2 pivot = new Vector2(source.pivot.x / sourceRect.width, source.pivot.y / sourceRect.height);
            replacement = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot,
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
            replacement.name = source.name + "_SkinFramework";
            replacementSprites.Add(cacheKey, replacement);
            return replacement;
        }

        private Sprite GetColorAdjustedSprite(Sprite source, float contrast, float brightness, float saturation)
        {
            if (source == null)
                return null;
            if (colorAdjustmentSources.TryGetValue(source, out Sprite originalSource))
                source = originalSource;
            if (!colorAdjustedSprites.TryGetValue(source, out ColorAdjustedSprite adjusted))
            {
                Texture2D sourceTexture = source.texture;
                Color32[] sourcePixels = ReadPixels(sourceTexture);
                Texture2D output = new Texture2D(sourceTexture.width, sourceTexture.height,
                    TextureFormat.RGBA32, false);
                output.name = sourceTexture.name + "_SkinFrameworkColorAdjusted";
                output.filterMode = sourceTexture.filterMode;
                output.wrapMode = sourceTexture.wrapMode;
                Sprite sprite = Sprite.Create(output, source.rect, source.pivot,
                    source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
                sprite.name = source.name + "_SkinFrameworkColorAdjusted";
                adjusted = new ColorAdjustedSprite(sourcePixels, output, sprite);
                colorAdjustedSprites.Add(source, adjusted);
                colorAdjustmentSources.Add(sprite, source);
            }
            adjusted.Apply(contrast, brightness, saturation);
            return adjusted.Sprite;
        }

        private void RestoreColorAdjustedSprites()
        {
            System.Func<Sprite, Sprite> restore = sprite =>
                sprite != null && colorAdjustmentSources.TryGetValue(sprite, out Sprite source) ? source : sprite;
            if (buttonSkin != null)
            {
                buttonSkin.ApplyColorAdjustment(restore);
                return;
            }
            if (Renderer != null)
            {
                if (recorderAnimator != null)
                {
                    for (int i = 0; i < recorderAnimator.sprites.Count; i++)
                        recorderAnimator.sprites[i] = restore(recorderAnimator.sprites[i]);
                    if (recorderAnimator.sprites.Count > 0)
                        Renderer.sprite = recorderAnimator.sprites[0];
                    return;
                }
                if (clienteleAnimator != null)
                {
                    for (int i = 0; i < clienteleAnimator.sprites.Count; i++)
                        clienteleAnimator.sprites[i] = restore(clienteleAnimator.sprites[i]);
                    if (clienteleAnimator.sprites.Count > 0)
                        Renderer.sprite = clienteleAnimator.sprites[0];
                    return;
                }
                Renderer.sprite = restore(Renderer.sprite);
                return;
            }
            for (int i = 0; i < Images.Length; i++)
                if (Images[i] != null)
                    Images[i].sprite = restore(Images[i].sprite);
        }

        private static Color32[] ReadPixels(Texture2D source)
        {
            if (!source.isReadable)
                return ReadPixelsThroughRenderTexture(source);
            try
            {
                return source.GetPixels32();
            }
            catch (System.ArgumentException)
            {
                return ReadPixelsThroughRenderTexture(source);
            }
            catch (UnityException)
            {
                return ReadPixelsThroughRenderTexture(source);
            }
        }

        private static Color32[] ReadPixelsThroughRenderTexture(Texture2D source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            Texture2D readable = null;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                readable.Apply(false, false);
                return readable.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (readable != null)
                    UnityEngine.Object.Destroy(readable);
            }
        }

        private sealed class ColorAdjustedSprite
        {
            internal readonly Color32[] SourcePixels;
            private readonly Color32[] outputPixels;
            internal readonly Texture2D Texture;
            internal readonly Sprite Sprite;
            private float appliedContrast = float.NaN;
            private float appliedSaturation = float.NaN;
            private float appliedBrightness = float.NaN;

            internal ColorAdjustedSprite(Color32[] sourcePixels, Texture2D texture, Sprite sprite)
            {
                SourcePixels = sourcePixels;
                outputPixels = new Color32[sourcePixels.Length];
                Texture = texture;
                Sprite = sprite;
            }

            internal void Apply(float contrast, float brightness, float saturation)
            {
                if (Mathf.Approximately(appliedContrast, contrast) &&
                    Mathf.Approximately(appliedBrightness, brightness) &&
                    Mathf.Approximately(appliedSaturation, saturation))
                    return;
                for (int i = 0; i < SourcePixels.Length; i++)
                {
                    Color32 pixel = SourcePixels[i];
                    float gray = pixel.r * 0.2126f + pixel.g * 0.7152f + pixel.b * 0.0722f;
                    pixel.r = AdjustChannel(Mathf.LerpUnclamped(gray, pixel.r, saturation), contrast, brightness);
                    pixel.g = AdjustChannel(Mathf.LerpUnclamped(gray, pixel.g, saturation), contrast, brightness);
                    pixel.b = AdjustChannel(Mathf.LerpUnclamped(gray, pixel.b, saturation), contrast, brightness);
                    outputPixels[i] = pixel;
                }
                Texture.SetPixels32(outputPixels);
                Texture.Apply(false, false);
                appliedContrast = contrast;
                appliedBrightness = brightness;
                appliedSaturation = saturation;
            }

            private static byte AdjustChannel(float value, float contrast, float brightness)
            {
                float channel = value / 255f;
                float adjusted = (channel - 0.5f) * contrast + 0.5f + brightness;
                return (byte)Mathf.Clamp(Mathf.RoundToInt(adjusted * 255f), 0, 255);
            }
        }

        private void RestoreSprites()
        {
            if (Renderer != null)
            {
                if (Id == "--Interactable/ClienteleDisplay")
                {
                    clienteleAnimator.sprites.Clear();
                    clienteleAnimator.sprites.AddRange(clienteleBaseFrames);
                }
                if (Id == "--Interactable/Recorder")
                {
                    recorderAnimator.sprites.Clear();
                    recorderAnimator.sprites.AddRange(recorderBaseFrames);
                }
                Renderer.sprite = rendererBaseSprite;
                return;
            }
            for (int i = 0; i < Images.Length; i++)
                if (Images[i] != null)
                    Images[i].sprite = imageBaseSprites[i];
        }
    }
}
