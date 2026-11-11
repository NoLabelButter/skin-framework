using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace SkinFramework
{
    internal static class StoreInventoryGridSkinAdapter
    {
        private static Image targetImage;
        private static Sprite originalSprite;
        private static Texture2D appliedTexture;
        private static Sprite replacementSprite;
        private static bool recomputing;
        private static bool suspended;

        internal static void Apply()
        {
            if (suspended)
                return;
            EmporiumEntry store = EmporiumEntry.Instance;
            GameGridInventory inventory = store == null ? null : store.invElement;
            if (inventory == null || inventory.handler == null)
                return;

            Transform handler = inventory.handler.transform;
            if (handler.childCount < 2)
                return;

            TreeNodeRender backgroundNode = handler.GetChild(1).GetComponent<TreeNodeRender>();
            Image image = backgroundNode == null ? null : backgroundNode.image;
            if (image == null)
                return;

            if (targetImage != image)
            {
                Restore();
                targetImage = image;
                originalSprite = image.sprite;
            }

            if (!SkinService.TryGetTexture(SkinTextureSlots.GameplayStoreInventoryGrid,
                out Texture2D texture) || texture == null)
            {
                RestoreTargetImage(inventory);
                return;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            if (appliedTexture != texture || replacementSprite == null)
            {
                if (replacementSprite != null)
                    Object.Destroy(replacementSprite);
                replacementSprite = Sprite.Create(texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), originalSprite == null ? 100f : originalSprite.pixelsPerUnit,
                    0, SpriteMeshType.FullRect);
                replacementSprite.name = "SkinFramework_StoreInventoryGrid";
                appliedTexture = texture;
            }

            if (image.sprite == replacementSprite)
                return;
            image.sprite = replacementSprite;
            if (inventory.renderDynamicBackground && !recomputing)
            {
                recomputing = true;
                inventory.ForceRecomputeDynamicBackground();
                recomputing = false;
            }
        }

        internal static void Restore()
        {
            suspended = true;
            EmporiumEntry store = EmporiumEntry.Instance;
            GameGridInventory inventory = store == null ? null : store.invElement;
            RestoreTargetImage(inventory);
            targetImage = null;
            originalSprite = null;
            if (replacementSprite != null)
                Object.Destroy(replacementSprite);
            replacementSprite = null;
            appliedTexture = null;
            suspended = false;
        }

        private static void RestoreTargetImage(GameGridInventory inventory)
        {
            if (targetImage == null)
                return;
            bool changed = originalSprite != null && targetImage.sprite != originalSprite;
            if (changed)
                targetImage.sprite = originalSprite;
            if (changed && inventory != null && inventory.renderDynamicBackground && !recomputing)
            {
                recomputing = true;
                inventory.ForceRecomputeDynamicBackground();
                recomputing = false;
            }
        }
    }

    [HarmonyPatch(typeof(GameGridInventory), "ValidateBackground")]
    internal static class StoreInventoryGridBackgroundPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameGridInventory __instance)
        {
            EmporiumEntry store = EmporiumEntry.Instance;
            if (!StoreInventoryGridSkinAdapterIsTarget(store, __instance))
                return;
            StoreInventoryGridSkinAdapter.Apply();
        }

        private static bool StoreInventoryGridSkinAdapterIsTarget(EmporiumEntry store,
            GameGridInventory inventory) => store != null && inventory != null &&
            inventory == store.invElement;
    }
}
