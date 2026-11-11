using HarmonyLib;
using UnityEngine;

namespace SkinFramework
{
    [HarmonyPatch(typeof(EmporiumEntry), "Update")]
    internal static class FixedFixtureEditorInputPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.TickFixedFixtureEditor();
    }

    [HarmonyPatch(typeof(MainMenuUIController), "Update")]
    internal static class PositionAdjustmentInputPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.TickPositionAdjustment();
    }

    [HarmonyPatch(typeof(NewGameSpecializationUI), "BuildStrip")]
    internal static class PositionAdjustmentStripPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => ModEntry.OnSpecializationStripRebuilt();
    }

    [HarmonyPatch(typeof(ModConfigWindow), "BuildEntry")]
    internal static class PositionAdjustmentConfigPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            if (config.ModId == "skin_framework" &&
                (entry.Key == SkinAdjustmentStore.ConfigKey || entry.Key == SkinAdjustmentStore.MigrationKey ||
                 entry.Key == SkinAdjustmentStore.SettingsMigrationKey))
                return false;
            if (config.ModId != "skin_framework")
                return true;
            bool positionSpecialization = entry.Key == PositionAdjustmentController.SpecializationConfigKey;
            bool positionMain = entry.Key == PositionAdjustmentController.MainConfigKey;
            bool transparencySpecialization = entry.Key == TransparencyAdjustmentController.SpecializationConfigKey;
            bool transparencyMain = entry.Key == TransparencyAdjustmentController.MainConfigKey;
            bool positionLoadGame = entry.Key == PositionAdjustmentController.LoadGameConfigKey;
            bool transparencyLoadGame = entry.Key == TransparencyAdjustmentController.LoadGameConfigKey;
            bool positionCredits = entry.Key == PositionAdjustmentController.CreditsConfigKey;
            bool transparencyCredits = entry.Key == TransparencyAdjustmentController.CreditsConfigKey;
            bool positionGameOptions = entry.Key == PositionAdjustmentController.GameOptionsConfigKey;
            bool transparencyGameOptions = entry.Key == TransparencyAdjustmentController.GameOptionsConfigKey;
            bool transparency = transparencyMain || transparencySpecialization || transparencyCredits || transparencyGameOptions;
            if (!positionSpecialization && !positionMain && !positionLoadGame && !transparency &&
                !transparencyLoadGame && !positionCredits && !positionGameOptions)
                return true;

            bool specialization = positionSpecialization || transparencySpecialization;
            bool loadGame = positionLoadGame || transparencyLoadGame;
            bool credits = positionCredits || transparencyCredits;
            bool gameOptions = positionGameOptions || transparencyGameOptions;
            transparency |= transparencyLoadGame;
            string category = gameOptions ? SkinLanguage.Text("游戏选项界面", "Game options") : transparency
                ? credits ? SkinLanguage.Text("制作人员名单", "Credits") : loadGame ? SkinLanguage.Text("载入游戏", "Load Game") : specialization ? SkinLanguage.Text("新游戏专精", "New Game specialization") : SkinLanguage.Text("主界面", "Main menu")
                : credits ? SkinLanguage.Text("制作人员名单", "Credits") : loadGame ? SkinLanguage.Text("载入游戏页面", "Load Game screen") : specialization ? SkinLanguage.Text("新游戏专精页面", "New Game specialization") : SkinLanguage.Text("主页面", "Main menu");
            PositionAdjustmentPage page = loadGame
                ? PositionAdjustmentPage.LoadGame
                : gameOptions ? PositionAdjustmentPage.GameOptions
                : credits ? PositionAdjustmentPage.Credits
                : specialization ? PositionAdjustmentPage.Specialization : PositionAdjustmentPage.MainMenu;
            b.BeginRow(4f, TextAnchor.MiddleLeft);
            b.AddLabel(category, "position_adjust_label_" + entry.Key);
            b.AddFlexibleSpace();
            b.AddButton(transparency ? SkinLanguage.Text("编辑", "Edit") : SkinLanguage.Text("开始调整", "Adjust"), () =>
            {
                if (transparency)
                    ModEntry.BeginTransparencyAdjustment(page, b.Handle);
                else
                    ModEntry.BeginPositionAdjustment(page, b.Handle);
            }, "position_adjust_button_" + entry.Key);
            b.Last.button.interactable = !SkinService.CurrentTheme.UsesGameDefaults;
            b.Last.SetFixedWidth(220f);
            b.AddInput("", entry.BoxedValue as string ?? "", null, entry.Key);
            b.Last.root.SetActive(false);
            b.End();
            return false;
        }

        internal static void RefreshThemeLocks(CustomUIWindow window)
        {
            string[] keys =
            {
                PositionAdjustmentController.MainConfigKey,
                PositionAdjustmentController.SpecializationConfigKey,
                PositionAdjustmentController.LoadGameConfigKey,
                PositionAdjustmentController.CreditsConfigKey,
                TransparencyAdjustmentController.MainConfigKey,
                TransparencyAdjustmentController.SpecializationConfigKey,
                TransparencyAdjustmentController.LoadGameConfigKey,
                TransparencyAdjustmentController.CreditsConfigKey,
                PositionAdjustmentController.GameOptionsConfigKey,
                TransparencyAdjustmentController.GameOptionsConfigKey
            };
            bool enabled = !SkinService.CurrentTheme.UsesGameDefaults;
            foreach (string key in keys)
                window.Get("position_adjust_button_" + key).button.interactable = enabled;
            ClassicThemeBrandingLockPatch.Refresh(window, enabled);
        }
    }

    [HarmonyPatch(typeof(ModConfigWindow), "BuildEntry")]
    internal static class ClassicThemeBrandingLockPatch
    {
        private static readonly string[] BrandingKeys =
        {
            "hide_main_menu_logo_v1",
            "hide_main_menu_version_v1",
            "main_menu_version_display_text_v1"
        };

        [HarmonyPostfix]
        private static void Postfix(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            if (config.ModId != "skin_framework" || !IsBrandingKey(entry.Key))
                return;
            SetEntryInteractive(b.Handle, entry.Key, !SkinService.CurrentTheme.UsesGameDefaults);
        }

        internal static void Refresh(CustomUIWindow window, bool enabled)
        {
            foreach (string key in BrandingKeys)
                SetEntryInteractive(window, key, enabled);
        }

        private static void SetEntryInteractive(CustomUIWindow window, string key, bool enabled)
        {
            CustomUIElement element = window.Get(key);
            if (element.input != null) element.input.interactable = enabled;
            if (element.toggle != null) element.toggle.interactable = enabled;
            if (element.button != null) element.button.interactable = enabled;
        }

        private static bool IsBrandingKey(string key)
        {
            foreach (string candidate in BrandingKeys)
                if (candidate == key) return true;
            return false;
        }
    }
}
