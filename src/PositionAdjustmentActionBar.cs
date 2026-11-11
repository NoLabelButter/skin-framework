using System;
using UnityEngine;

namespace SkinFramework
{
    /// <summary>位置调整期间固定显示于菜单左上角的保存、取消和恢复默认操作条。</summary>
    internal sealed class PositionAdjustmentActionBar
    {
        private const string WindowId = "skin_framework.position_adjustment_actions";
        private CustomUIWindow window;
        private Action onSave;
        private Action onCancel;
        private Action onRestoreDefaults;

        internal bool Open(Action save, Action cancel, Action restoreDefaults)
        {
            return Open(SkinLanguage.Text("位置调整", "Position adjustment"), "保存本次位置调整并返回配置", "撤销本次调整，恢复进入时的位置",
                "清空当前页面的所有位置偏移；可继续调整后保存", save, cancel, restoreDefaults,
                showPositionSaveReminder: true);
        }

        internal bool Open(string title, string saveTooltip, string cancelTooltip, string defaultsTooltip,
            Action save, Action cancel, Action restoreDefaults, bool showPositionSaveReminder = false)
        {
            if (CustomUIManager.Instance == null)
                return false;

            onSave = save;
            onCancel = cancel;
            onRestoreDefaults = restoreDefaults;
            CustomUIBuilder builder = CustomUIManager.Window(WindowId, title, CustomUIManager.LAYER_MENU)
                .SetSize(showPositionSaveReminder ? 560f : 410f, showPositionSaveReminder ? 150f : 92f)
                .SetDraggable(false)
                .SetCloseOnEscape(false)
                .OnClose(OnWindowClosed);
            if (showPositionSaveReminder)
            {
                builder.BeginRow(4f, TextAnchor.MiddleLeft);
                builder.AddLabel("快捷键：Shift+←/→选控件；方向键移动，Ctrl加速；专精页 Ctrl+Shift+←/→翻页；Esc取消。",
                    "position_adjust_save_reminder");
                builder.Last.SetFixedWidth(530f);
                builder.End();
            }
            builder.BeginRow(8f, TextAnchor.MiddleCenter);
            builder.AddButton(SkinLanguage.Text("保存", "Save"), () => onSave?.Invoke(), "position_adjust_save").WithTooltip(saveTooltip);
            builder.Last.SetFixedWidth(112f);
            builder.AddButton(SkinLanguage.Text("取消", "Cancel"), () => onCancel?.Invoke(), "position_adjust_cancel").WithTooltip(cancelTooltip);
            builder.Last.SetFixedWidth(112f);
            builder.AddButton(SkinLanguage.Text("恢复默认", "Reset to Default"), () => onRestoreDefaults?.Invoke(), "position_adjust_defaults")
                .WithTooltip(defaultsTooltip);
            builder.Last.SetFixedWidth(132f);
            builder.End();
            window = builder.Show();

            RectTransform rect = window.Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -18f);
            return true;
        }

        internal void Close()
        {
            CustomUIWindow closing = window;
            window = null;
            ClearCallbacks();
            if (closing != null && closing.IsAlive)
            {
                closing.onClose = null;
                closing.Close();
            }
        }

        private void OnWindowClosed()
        {
            window = null;
            Action cancel = onCancel;
            ClearCallbacks();
            cancel?.Invoke();
        }

        private void ClearCallbacks()
        {
            onSave = null;
            onCancel = null;
            onRestoreDefaults = null;
        }
    }
}
