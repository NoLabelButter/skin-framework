using HarmonyLib;

namespace SkinFramework
{
    [HarmonyPatch(typeof(StoreUIManager), "OnDisplayCaseUpgradeI")]
    internal static class ShowcaseUpgradeIPatch
    {
        [HarmonyPostfix]
        private static void Postfix(StoreUIManager __instance) => ModEntry.ApplyShowcaseSprite(__instance.showcaseRenderer.sprite);
    }

    [HarmonyPatch(typeof(StoreUIManager), "OnDisplayCaseUpgradeII")]
    internal static class ShowcaseUpgradeIIPatch
    {
        [HarmonyPostfix]
        private static void Postfix(StoreUIManager __instance) => ModEntry.ApplyShowcaseSprite(__instance.showcaseRenderer.sprite);
    }
}
