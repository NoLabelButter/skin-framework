using HarmonyLib;
using UnityEngine;

namespace SkinFramework
{
    [HarmonyPatch(typeof(GameItemElement), "ResolveItemSprite")]
    internal static class GameItemSpritePatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameItemElement __instance, ref Sprite __result)
        {
            __result = ModEntry.ApplyTrashcanSprite(__instance, __result);
            __result = ModEntry.ApplyFaucetSprite(__instance, __result);
            __result = ModEntry.ApplyDrainSprite(__instance, __result);
            __result = ModEntry.ApplyDossierSprite(__instance, __result);
            __result = ModEntry.ApplyCassettePlayerSprite(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(GameItemElement), "ValidateSelectLocked")]
    internal static class TrashcanSpriteLayoutPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameItemElement __instance)
        {
            ModEntry.ApplyTrashcanLayout(__instance);
        }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground")]
    internal static class CassettePlayerBackgroundPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameItemElement __instance, bool enabled)
        {
            EmporiumEntry shop = EmporiumEntry.Instance;
            if (!enabled || shop == null || shop.cassettePlayer != __instance || !ModEntry.HasActiveRadioSkin())
                return;
            Color color = __instance.background.image.color;
            color.a = 0f;
            __instance.background.image.color = color;
        }
    }
}
