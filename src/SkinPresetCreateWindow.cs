using System;
using UnityEngine;

namespace SkinFramework
{
    internal static class SkinPresetCreateWindow
    {
        private const string WindowId = "skin_framework.create_preset";
        private const string NameId = "skin_framework.create_preset.name";
        private const string StatusId = "skin_framework.create_preset.status";
        private static string displayName = "";

        internal static void Open(Action onCreated)
        {
            if (CustomUIManager.Instance == null)
                return;

            displayName = "";
            CustomUIBuilder builder = CustomUIManager.Window(WindowId, SkinLanguage.Text("新建皮肤预设", "New Skin Preset"), CustomUIManager.LAYER_MENU)
                .SetSize(420f, 180f)
                .SetCloseOnEscape(true)
                .AddLabel(SkinLanguage.Text("创建原版外观主题，可稍后添加贴图。", "Create a preset with the original appearance. You can add textures later."))
                .AddInput(SkinLanguage.Text("预设名称", "Preset name"), "", value => displayName = value,
                    id: NameId, onTyping: value => displayName = value)
                .AddLabel(SkinLanguage.Text("点“创建”后立即保存并加入列表；取消不会创建。", "Create saves the preset and adds it to the list."), id: StatusId);
            builder.BeginRow(8f, TextAnchor.MiddleRight);
            builder.AddButton(SkinLanguage.Text("创建", "Create"), () => Create(onCreated));
            builder.AddButton(SkinLanguage.Text("取消", "Cancel"), () => CustomUIManager.Instance.CloseWindow(WindowId));
            builder.End();
            builder.Show();
        }

        private static void Create(Action onCreated)
        {
            bool created = SkinPresets.TryCreateUserPreset(displayName, out string presetId, out string message);
            bool refreshed = created && ModEntry.RefreshPresetCatalog();
            CustomUIWindow window = CustomUIManager.Instance?.GetWindow(WindowId);
            if (window != null && window.IsAlive)
                window.Get(StatusId)?.SetText(!created ? message : refreshed
                    ? SkinLanguage.Text("已创建并加入当前列表。", "Preset created and added to the list.")
                    : "已保存，但当前列表刷新失败；请重启游戏。" );
            if (!created)
            {
                Debug.LogError("[Skin Framework] " + message);
                return;
            }

            if (refreshed)
                onCreated?.Invoke();
            else
                Debug.LogError("[Skin Framework] 自建预设已保存，但当前列表刷新失败；请重启游戏后检查。 ");

        }
    }
}
