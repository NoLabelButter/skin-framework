using System;
using HarmonyLib;
using UnityEngine;

namespace SkinFramework
{
    [HarmonyPatch(typeof(ModConfigWindow), "BuildEntry")]
    internal static class SkinPresetCreateButton
    {
        [HarmonyPrefix]
        private static bool Prefix(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            if (config.ModId != "skin_framework" || entry.Key != "selected_skin_v1")
                return true;

            string[] choices = SkinPresets.GetPresetNames();
            ModReflect.Call(entry, "set_Choices", (object)choices);
            int selectedIndex = System.Math.Max(0, System.Array.IndexOf(choices, entry.BoxedValue as string));
            string[] dropdownChoices = BuildDropdownChoices(choices);

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(entry.Label(), "label_" + entry.Key);
            b.AddFlexibleSpace();
            CustomUIElement dropdown = null;
            int activeIndex = selectedIndex;
            Action<bool> refreshDropdown = selectDefault =>
            {
                string selectedName = selectDefault
                    ? SkinPresets.GameDefault.DisplayName
                    : activeIndex >= 0 && activeIndex < choices.Length
                        ? choices[activeIndex]
                        : SkinPresets.GameDefault.DisplayName;
                choices = SkinPresets.GetPresetNames();
                ModReflect.Call(entry, "set_Choices", (object)choices);
                dropdownChoices = BuildDropdownChoices(choices);
                dropdown.SetOptions(dropdownChoices);
                activeIndex = System.Array.IndexOf(choices, selectedName);
                if (activeIndex < 0)
                    activeIndex = System.Array.IndexOf(choices, SkinPresets.GameDefault.DisplayName);
                dropdown.SetIndex(activeIndex, false);
            };
            b.AddDropdown(dropdownChoices, selectedIndex, index =>
            {
                if (index == choices.Length)
                {
                    dropdown.SetIndex(activeIndex, false);
                    SkinPresetCreateWindow.Open(() => refreshDropdown(false));
                    return;
                }
                if (index == choices.Length + 1)
                {
                    dropdown.SetIndex(activeIndex, false);
                    SkinTheme selectedTheme = activeIndex >= 0 && activeIndex < choices.Length
                        ? SkinPresets.FindByName(choices[activeIndex])
                        : null;
                    SkinPresetDeleteWindow.Open(selectedTheme, deleted =>
                    {
                        if (deleted)
                            refreshDropdown(true);
                    });
                    return;
                }
                activeIndex = index;
            }, entry.Key);
            dropdown = b.Last;
            b.Last.SetFixedWidth(242f);
            b.End();
            b.BeginRow(6f, TextAnchor.MiddleRight);
            b.AddButton(SkinLanguage.Text("导入 / 导出美化包", "Import / Export Skin Pack"), () => SkinPackageWindow.Open(
                SkinPresets.FindByName(choices[dropdown.GetIndex()]), () => refreshDropdown(false)));
            b.AddButton(SkinLanguage.Text("复制预设素材目录", "Copy Asset Folder"), () =>
            {
                SkinTheme theme = SkinPresets.FindByName(choices[dropdown.GetIndex()]);
                string directory = SkinPresets.IsUserPreset(theme.Id)
                    ? ModHelper.GetModDataFolder("skin_framework") + "/Textures/" + SkinPresets.GetAssetFolder(theme.Id)
                    : ModHelper.GetModFolder("skin_framework") + "/Resources/Textures/" + theme.Id;
                GUIUtility.systemCopyBuffer = directory.Replace('/', '\\');
            }).WithTooltip(SkinLanguage.Text("复制下拉框所选预设的目录，可粘贴到文件资源管理器。", "Copy the selected preset folder to paste into File Explorer."));
            b.End();
            return false;
        }

        private static string[] BuildDropdownChoices(string[] presetNames)
        {
            string[] options = new string[presetNames.Length + 2];
            for (int i = 0; i < presetNames.Length; i++)
                options[i] = presetNames[i] == SkinPresets.GameDefault.DisplayName
                    ? SkinLanguage.Text(presetNames[i], "Game Default") : presetNames[i];
            options[presetNames.Length] = SkinLanguage.Text("＋ 新建预设", "＋ New Preset");
            options[presetNames.Length + 1] = SkinLanguage.Text("－ 删除所选自建预设", "－ Delete Selected Custom Preset");
            return options;
        }
    }
}
