using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinFramework
{
    /// <summary>把当前主题的共用/专属背景贴图应用到原版主菜单背景对象。</summary>
    internal sealed class MainMenuBackgroundAdapter
    {
        private const string TextureSlot = SkinTextureSlots.MainMenuBackground;
        private const string TargetObjectName = "MenuBackgroundNew";
        private const float PixelsPerUnit = 200f;

        private SpriteRenderer targetRenderer;
        private Sprite originalSprite;
        private bool originalEnabled;
        private Sprite replacementSprite;
        private Texture2D replacementTexture;

        public void ApplyToActiveScene()
        {
            ApplyToScene(SceneManager.GetActiveScene());
        }

        public void ApplyToScene(Scene scene)
        {
            if (SkinService.IsTextureDisabled(TextureSlot))
            {
                SpriteRenderer hiddenRenderer = FindTarget(scene);
                if (hiddenRenderer != null)
                {
                    if (targetRenderer != hiddenRenderer)
                    {
                        RestoreOriginal();
                        targetRenderer = hiddenRenderer;
                        originalSprite = hiddenRenderer.sprite;
                        originalEnabled = hiddenRenderer.enabled;
                    }
                    hiddenRenderer.enabled = false;
                }
                return;
            }

            if (!SkinService.TryGetTexture(TextureSlot, out Texture2D texture))
            {
                RestoreOriginal();
                return;
            }

            SpriteRenderer renderer = FindTarget(scene);
            if (renderer == null)
                return;

            if (targetRenderer != renderer)
            {
                RestoreOriginal();
                targetRenderer = renderer;
                originalSprite = renderer.sprite;
                originalEnabled = renderer.enabled;
            }

            if (replacementTexture != texture)
                CreateReplacementSprite(texture);

            renderer.sprite = replacementSprite;
            renderer.enabled = originalEnabled;
        }

        public void Dispose()
        {
            RestoreOriginal();
            if (replacementSprite != null)
                Object.Destroy(replacementSprite);

            replacementSprite = null;
            replacementTexture = null;
        }

        private static SpriteRenderer FindTarget(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                SpriteRenderer[] renderers = roots[i].GetComponentsInChildren<SpriteRenderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    if (renderers[j].gameObject.name == TargetObjectName)
                        return renderers[j];
                }
            }

            return null;
        }

        private void CreateReplacementSprite(Texture2D texture)
        {
            if (replacementSprite != null)
                Object.Destroy(replacementSprite);

            replacementTexture = texture;
            replacementSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            replacementSprite.name = "SkinFrameworkMainMenuBackground";
        }

        private void RestoreOriginal()
        {
            if (targetRenderer != null && targetRenderer.sprite == replacementSprite)
                targetRenderer.sprite = originalSprite;
            if (targetRenderer != null)
                targetRenderer.enabled = originalEnabled;

            targetRenderer = null;
            originalSprite = null;
            originalEnabled = false;
        }
    }
}
