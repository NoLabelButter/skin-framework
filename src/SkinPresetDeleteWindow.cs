using System;
using UnityEngine;

namespace SkinFramework
{
    internal static class SkinPresetDeleteWindow
    {
        private const string WindowId = "skin_framework.delete_preset";
        private const string StatusId = "skin_framework.delete_preset.status";

        internal static void Open(SkinTheme theme, Action<bool> onDeleted)
        {
            if (CustomUIManager.Instance == null)
                return;

            string displayName = theme?.DisplayName ?? "（未选择）";
            CustomUIBuilder builder = CustomUIManager.Window(WindowId, SkinLanguage.Text("删除皮肤预设", "Delete Skin Preset"), CustomUIManager.LAYER_MENU)
                .SetSize(420f, 190f)
                .SetCloseOnEscape(true)
                .AddLabel(SkinLanguage.Text("将从预设列表移除：", "Remove preset: ") + displayName)
                .AddLabel(SkinLanguage.Text("确认后删除预设及该目录里的素材文件。", "Confirm to delete the preset and its asset files."), id: StatusId);
            builder.BeginRow(8f, TextAnchor.MiddleRight);
            builder.AddButton(SkinLanguage.Text("删除", "Delete"), () =>
            {
                if (theme == null)
                {
                    SetStatus("请先在列表中选中一个自建预设。");
                    return;
                }

                bool deleted = SkinPresets.TryDeleteUserPreset(theme.Id, out string message);
                if (!deleted)
                {
                    SetStatus(message);
                    Debug.LogError("[Skin Framework] " + message);
                    return;
                }

                bool refreshed = ModEntry.RefreshPresetCatalog();
                if (refreshed)
                {
                    ModEntry.TrySelectPreset(SkinPresets.GameDefault);
                    message = SkinLanguage.Text("已从当前列表移除。", "Preset removed.");
                }
                else
                {
                    message = "记录已删除，但当前列表刷新失败；请重启游戏。";
                    Debug.LogError("[Skin Framework] " + message);
                }
                SetStatus(message);
                ModEntry.RemovePresetProfile(theme.Id);
                onDeleted?.Invoke(refreshed);
                CustomUIManager.Instance.CloseWindow(WindowId);
            });
            builder.AddButton(SkinLanguage.Text("取消", "Cancel"), () => CustomUIManager.Instance.CloseWindow(WindowId));
            builder.End();
            builder.Show();
        }

        private static void SetStatus(string message)
        {
            CustomUIWindow window = CustomUIManager.Instance?.GetWindow(WindowId);
            if (window != null && window.IsAlive)
                window.Get(StatusId)?.SetText(message ?? "");
        }
    }
}
