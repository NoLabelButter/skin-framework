using UnityEngine.SceneManagement;

namespace SkinFramework
{
    /// <summary>管理高级选项中的主菜单 Logo 与版本文字设置。</summary>
    internal sealed class MainMenuBrandingController
    {
        private ConfigEntry<bool> hideLogo;
        private ConfigEntry<bool> hideVersion;
        private ConfigEntry<string> versionText;
        private readonly MainMenuBrandingAdapter adapter = new MainMenuBrandingAdapter();
        private bool eventsBound;
        internal System.Action<string, string> SettingChanged;

        public void DefineConfiguration(ModConfig config)
        {
            hideLogo = config.Define("hide_main_menu_logo_v1", false, "高级功能",
                () => "隐藏主菜单原版 Logo",
                tooltip: () => "隐藏主菜单左上角的游戏 Logo 图片，不修改背景贴图。");
            hideVersion = config.Define("hide_main_menu_version_v1", false, "高级功能",
                () => "隐藏主菜单版本号",
                tooltip: () => "只隐藏主菜单左下角的版本文字。");
            versionText = config.Define("main_menu_version_display_text_v1", "", "高级功能",
                () => "自定义显示版本号",
                tooltip: () => "仅修改主菜单显示文本；留空沿用游戏文本，不会修改游戏版本或存档。隐藏版本号开启时此文字不可见。");
        }

        public void Activate()
        {
            if (!eventsBound)
            {
                hideLogo.OnChanged += OnLogoChanged;
                hideVersion.OnChanged += OnVersionVisibilityChanged;
                versionText.OnChanged += OnVersionTextChanged;
                eventsBound = true;
            }

            ApplySettings();
        }

        public void ApplyToActiveScene() => adapter.ApplyToActiveScene();

        public void ApplyToScene(Scene scene) => adapter.ApplyToScene(scene);

        internal ConfigEntry<bool> HideLogoEntry => hideLogo;
        internal ConfigEntry<bool> HideVersionEntry => hideVersion;
        internal ConfigEntry<string> VersionTextEntry => versionText;
        internal void ApplyCurrentSettings() => ApplySettings();

        public void Dispose()
        {
            if (eventsBound)
            {
                hideLogo.OnChanged -= OnLogoChanged;
                hideVersion.OnChanged -= OnVersionVisibilityChanged;
                versionText.OnChanged -= OnVersionTextChanged;
                eventsBound = false;
            }

            adapter.Dispose();
        }

        private void OnLogoChanged(bool value)
        {
            ApplySettings();
            SettingChanged?.Invoke(hideLogo.Key, value.ToString());
        }

        private void OnVersionVisibilityChanged(bool value)
        {
            ApplySettings();
            SettingChanged?.Invoke(hideVersion.Key, value.ToString());
        }

        private void OnVersionTextChanged(string value)
        {
            ApplySettings();
            SettingChanged?.Invoke(versionText.Key, value);
        }

        private void ApplySettings() => adapter.SetOptions(hideLogo.Value, hideVersion.Value, versionText.Value);
    }
}
