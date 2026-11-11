using HarmonyLib;

namespace SkinFramework
{
    [HarmonyPatch(typeof(GameOptionUIManager), nameof(GameOptionUIManager.OpenUI))]
    internal static class GameOptionsOpenedAdjustmentPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.OnGameOptionsOpened();
    }

    [HarmonyPatch(typeof(GameOptionUIManager), "OnWindowClosed")]
    internal static class GameOptionsClosedAdjustmentPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.OnGameOptionsClosed();
    }
}
