using HarmonyLib;

namespace SkinFramework
{
    [HarmonyPatch(typeof(StorePoster), "PullUp")]
    internal static class StorePosterPullUpTexturePatch
    {
        [HarmonyPostfix]
        private static void Postfix(StorePoster __instance) =>
            ModEntry.ApplyPosterSprite(__instance.spriteRenderer.sprite);
    }

    [HarmonyPatch(typeof(StorePoster), "PullDown")]
    internal static class StorePosterPullDownTexturePatch
    {
        [HarmonyPostfix]
        private static void Postfix(StorePoster __instance) =>
            ModEntry.ApplyPosterSprite(__instance.spriteRenderer.sprite);
    }
}
