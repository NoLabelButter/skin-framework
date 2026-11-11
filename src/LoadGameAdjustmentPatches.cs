using HarmonyLib;

namespace SkinFramework
{
    [HarmonyPatch(typeof(LoadGameUI), "Open")]
    internal static class LoadGameOpenedAdjustmentPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.OnLoadGameOpened();
    }

    [HarmonyPatch(typeof(LoadGameUI), "RefreshVisible")]
    internal static class LoadGameRefreshAdjustmentPatch
    {
        [HarmonyPrefix]
        private static void Prefix() => ModEntry.BeforeLoadGameRefresh();

        [HarmonyPostfix]
        private static void Postfix() => ModEntry.OnLoadGameRefreshed();
    }

    [HarmonyPatch(typeof(LoadGameUI), "OnWindowClosed")]
    internal static class LoadGameClosedAdjustmentPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.OnLoadGameClosed();
    }
}
