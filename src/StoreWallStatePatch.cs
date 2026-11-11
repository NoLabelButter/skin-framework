using HarmonyLib;

namespace SkinFramework
{
    [HarmonyPatch(typeof(EmporiumEntry), nameof(EmporiumEntry.ApplyFaucetState))]
    internal static class StoreWallStatePatch
    {
        private static void Postfix()
        {
            StoreUIManager storeUi = StoreUIManager.Instance;
            if (storeUi == null || storeUi.wallRenderer == null)
                return;

            storeUi.wallRenderer.sprite = storeUi.wall2;
            ModEntry.ApplyWall2Sprite(storeUi.wall2);
        }
    }
}
