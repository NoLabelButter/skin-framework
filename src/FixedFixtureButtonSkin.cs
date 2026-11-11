using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinFramework
{
    internal sealed class FixedFixtureButtonSkin
    {
        private readonly SpriteRenderer renderer;
        private readonly Component stateOwner;
        private readonly Func<Sprite> getNormal, getHover, getClick;
        private readonly Action<Sprite, Sprite, Sprite> assign;
        private readonly Sprite originalNormal, originalHover, originalClick;
        private readonly Dictionary<Texture2D, Sprite> replacements = new Dictionary<Texture2D, Sprite>();

        private FixedFixtureButtonSkin(SpriteRenderer renderer, Component stateOwner, Func<Sprite> normal, Func<Sprite> hover,
            Func<Sprite> click, Action<Sprite, Sprite, Sprite> assign)
        {
            this.renderer = renderer; this.stateOwner = stateOwner;
            getNormal = normal; getHover = hover; getClick = click; this.assign = assign;
            originalNormal = normal(); originalHover = hover(); originalClick = click();
        }

        internal static FixedFixtureButtonSkin ForShutter(SpriteRenderer renderer)
        {
            StoreShutterButton button = renderer.GetComponent<StoreShutterButton>();
            return new FixedFixtureButtonSkin(renderer, button, () => button.spriteNormal, () => button.spriteHover,
                () => button.spriteClick, (normal, hover, click) =>
                { button.spriteNormal = normal; button.spriteHover = hover; button.spriteClick = click; });
        }

        internal static FixedFixtureButtonSkin ForBell(SpriteRenderer renderer)
        {
            StoreBell button = renderer.GetComponent<StoreBell>();
            return new FixedFixtureButtonSkin(renderer, button, () => button.spriteNormal, () => button.spriteHover,
                () => button.spriteClick, (normal, hover, click) =>
                { button.spriteNormal = normal; button.spriteHover = hover; button.spriteClick = click; });
        }

        internal void Apply(string normalSlot, string pressedSlot, string normalOverride = null)
        {
            Texture2D normalTexture;
            bool normalLoaded = string.IsNullOrEmpty(normalOverride)
                ? SkinService.TryGetTexture(normalSlot, out normalTexture)
                : SkinService.TryGetTextureByKey(normalSlot, normalOverride, out normalTexture, out _);
            Sprite normal = normalLoaded
                ? GetReplacement(originalNormal, normalTexture) : originalNormal;
            Sprite hover = normal == originalNormal ? originalHover : normal;
            Sprite click = SkinService.TryGetTexture(pressedSlot, out Texture2D pressedTexture)
                ? GetReplacement(originalClick, pressedTexture) : originalClick;
            SetStates(normal, hover, click);
        }

        internal void Restore() => SetStates(originalNormal, originalHover, originalClick);

        internal void ApplyColorAdjustment(Func<Sprite, Sprite> adjust)
        {
            SetStates(adjust(getNormal()), adjust(getHover()), adjust(getClick()));
        }

        internal void Release()
        {
            foreach (Sprite sprite in replacements.Values)
                UnityEngine.Object.Destroy(sprite);
            replacements.Clear();
        }

        private void SetStates(Sprite normal, Sprite hover, Sprite click)
        {
            if (renderer == null || stateOwner == null)
                return;
            Sprite current = renderer.sprite;
            Sprite oldNormal = getNormal(), oldHover = getHover(), oldClick = getClick();
            assign(normal, hover, click);
            if (current == oldClick) renderer.sprite = click;
            else if (current == oldHover) renderer.sprite = hover;
            else if (current == oldNormal) renderer.sprite = normal;
        }

        private Sprite GetReplacement(Sprite original, Texture2D texture)
        {
            if (replacements.TryGetValue(texture, out Sprite cached)) return cached;
            Rect rect = original.rect;
            Vector2 pivot = new Vector2(original.pivot.x / rect.width, original.pivot.y / rect.height);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot,
                original.pixelsPerUnit, 0, SpriteMeshType.FullRect, original.border);
            sprite.name = original.name + "_SkinFramework";
            replacements.Add(texture, sprite);
            return sprite;
        }
    }
}
