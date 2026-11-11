using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    [HarmonyPatch(typeof(ModConfigWindow), "BuildEntry")]
    internal static class MainMenuFontColorConfigPatch
    {
        private const string ModId = "skin_framework";
        private const string SwatchId = "main_menu_font_color_swatch";

        [HarmonyPrefix]
        private static bool Prefix(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            if (config.ModId != ModId || entry.Key != MainMenuFontColor.ConfigKey)
                return true;

            string configuredColor = entry.BoxedValue as string;
            if (!MainMenuFontColor.TryParse(configuredColor, out UnityEngine.Color color))
            {
                SkinColor themeText = SkinService.CurrentTheme.Text;
                color = new UnityEngine.Color(themeText.R, themeText.G, themeText.B, themeText.A);
            }

            b.AddHeader("主界面", "main_menu_font_color_section");
            b.BeginRow(4f, TextAnchor.MiddleLeft);
            b.AddLabel("主界面字体颜色", "label_" + entry.Key);
            b.AddFlexibleSpace();
            b.AddInput("", entry.BoxedValue as string ?? "", null, entry.Key);
            CustomUIElement valueField = b.Last;
            valueField.root.SetActive(false);
            if (SkinService.CurrentTheme.UsesGameDefaults)
            {
                b.AddLabel("游戏默认主题：配色固定", "locked_" + entry.Key);
                b.End();
                return false;
            }

            b.AddButton("", null, SwatchId);
            b.Last.SetPreferredSize(40f, 30f).SetImageColor(color);
            b.Last.button.transition = Selectable.Transition.None;
            CustomUIWindow configWindow = b.Handle;
            CustomUIElement swatch = b.Last;
            swatch.OnClick(() => ColorPickerWindow.Open(color,
                selected => ApplySelectedColor(configWindow, entry.Key, swatch, selected)));
            b.End();
            return false;
        }

        private static void ApplySelectedColor(CustomUIWindow window, string key, CustomUIElement swatch, string selected)
        {
            if (!window.IsAlive || !swatch.IsAlive)
                return;

            CustomUIElement valueField = window.Get(key);
            if (valueField == null || !valueField.IsAlive)
                return;

            valueField.SetString(selected, false);
            MainMenuFontColor.TryParse(selected, out UnityEngine.Color color);
            swatch.SetImageColor(color);
        }

        internal static void RefreshSwatch(CustomUIWindow window)
        {
            if (window == null || !window.IsAlive)
                return;
            CustomUIElement swatch = window.Get(SwatchId);
            if (swatch == null || !swatch.IsAlive)
                return;
            string value = window.Get(MainMenuFontColor.ConfigKey)?.GetString();
            if (!MainMenuFontColor.TryParse(value, out UnityEngine.Color color))
            {
                SkinColor themeText = SkinService.CurrentTheme.Text;
                color = new UnityEngine.Color(themeText.R, themeText.G, themeText.B, themeText.A);
            }
            swatch.SetImageColor(color);
        }
    }

    [HarmonyPatch(typeof(ModConfigWindow), "ResetWidgets")]
    internal static class MainMenuFontColorResetPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CustomUIWindow window, ModConfig config)
        {
            if (config.ModId != "skin_framework" || !window.IsAlive)
                return;
            MainMenuFontColorConfigPatch.RefreshSwatch(window);
        }
    }
}
