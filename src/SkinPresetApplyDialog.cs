using System;
using UnityEngine;

namespace SkinFramework
{
    internal static class SkinPresetApplyDialog
    {
        private const string WindowId = "skin_framework.apply_preset";
        private const string StatusId = "save_as_status";
        private static bool confirmed;

        internal static bool Confirm(CustomUIWindow settings, ModConfig config)
        {
            if (confirmed)
                return true;
            var edits = ModEntry.GetPendingPresetEdits(settings);
            if (edits.Count == 0)
                return true;

            string name = SkinService.CurrentTheme.DisplayName + " 副本";
            CustomUIBuilder builder = CustomUIManager.Window(WindowId, SkinLanguage.Text("预设修改", "Preset Changes"), CustomUIManager.LAYER_MENU)
                .SetSize(500, 205).SetCloseOnEscape(true)
                .AddLabel(SkinLanguage.Text("当前预设：", "Current preset: ") + SkinService.CurrentTheme.DisplayName)
                .AddInput(SkinLanguage.Text("另存为名称", "Name for the copy"), name, value => name = value, onTyping: value => name = value)
                .AddLabel("", id: StatusId);
            builder.BeginRow(6f, TextAnchor.MiddleRight);
            builder.AddButton(SkinLanguage.Text("保存", "Save"), () =>
            {
                Close();
                confirmed = true;
                try { ModReflect.CallStatic(typeof(ModConfigWindow), "Apply", settings, config); }
                finally { confirmed = false; }
            });
            builder.AddButton(SkinLanguage.Text("不保存", "Discard Changes"), () =>
            {
                ModEntry.DiscardPresetEdits(settings);
                Close();
            });
            builder.AddButton(SkinLanguage.Text("另存为", "Save As"), () =>
            {
                try
                {
                    ModEntry.SavePresetCopy(name, edits);
                    Close();
                }
                catch (Exception error) when (error is System.IO.IOException || error is FormatException ||
                    error is ArgumentException || error is InvalidOperationException ||
                    error is UnauthorizedAccessException || error is Newtonsoft.Json.JsonException)
                {
                    CustomUIManager.Instance.GetWindow(WindowId).Get(StatusId).SetText(SkinLanguage.Text("另存失败：", "Save As failed: ") + error.Message);
                    Debug.LogError("[Skin Framework] " + error);
                }
            });
            builder.AddButton(SkinLanguage.Text("取消", "Cancel"), Close);
            builder.End();
            builder.Show();
            return false;
        }

        private static void Close() => CustomUIManager.Instance.CloseWindow(WindowId);
    }
}
