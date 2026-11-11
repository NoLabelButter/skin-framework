using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>可选地隐藏或替换主菜单 Logo 和游戏版本文本，并在停用时恢复原状。</summary>
    internal sealed class MainMenuBrandingAdapter
    {
        private Image logo;
        private bool originalLogoEnabled;
        private TMP_Text version;
        private bool originalVersionEnabled;
        private LocalizeStringEvent versionLocalization;
        private UnityAction<string> localizedVersionHandler;
        private bool hideLogo;
        private bool hideVersion;
        private string customVersionText = "";

        public void SetOptions(bool hideLogo, bool hideVersion, string customVersionText)
        {
            this.hideLogo = hideLogo;
            this.hideVersion = hideVersion;
            this.customVersionText = customVersionText ?? "";
            ApplyToActiveScene();
        }

        public void ApplyToActiveScene() => ApplyToScene(SceneManager.GetActiveScene());

        public void ApplyToScene(Scene scene)
        {
            Transform canvas = FindMenuCanvas(scene);
            if (canvas == null)
            {
                Restore();
                return;
            }

            Image nextLogo = canvas.Find("Logo")?.GetComponent<Image>();
            TMP_Text nextVersion = canvas.Find("Version")?.GetComponent<TMP_Text>();
            if (logo != nextLogo || version != nextVersion)
            {
                Restore();
                logo = nextLogo;
                version = nextVersion;
                if (logo != null)
                    originalLogoEnabled = logo.enabled;
                if (version != null)
                {
                    originalVersionEnabled = version.enabled;
                    BindVersionLocalization();
                }
            }

            if (logo != null)
                logo.enabled = originalLogoEnabled && !hideLogo;
            if (version != null)
            {
                version.enabled = originalVersionEnabled && !hideVersion;
                ApplyVersionText();
            }
        }

        public void Dispose() => Restore();

        private static Transform FindMenuCanvas(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            Transform canvas = null;
            bool hasMenuBackground = false;
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] descendants = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < descendants.Length; j++)
                {
                    if (descendants[j].name == "MenuBackgroundNew")
                        hasMenuBackground = true;
                    else if (canvas == null && descendants[j].name == "Canvas")
                        canvas = descendants[j];
                }
            }

            return hasMenuBackground ? canvas : null;
        }

        private void BindVersionLocalization()
        {
            versionLocalization = version.GetComponent<LocalizeStringEvent>();
            if (versionLocalization == null)
                return;

            localizedVersionHandler = OnLocalizedVersionChanged;
            versionLocalization.OnUpdateString.AddListener(localizedVersionHandler);
        }

        private void OnLocalizedVersionChanged(string localizedText)
        {
            if (!string.IsNullOrWhiteSpace(customVersionText))
                version.text = customVersionText;
        }

        private void ApplyVersionText()
        {
            if (!string.IsNullOrWhiteSpace(customVersionText))
                version.text = customVersionText;
            else
                RefreshOriginalVersion();
        }

        private void RefreshOriginalVersion()
        {
            // Let the game's own LocalizeStringEvent + TMPLocalizer rebuild the
            // complete version string (including its runtime postfix). A cached
            // TMP value can already be stale, and manually appending postfix
            // produced the stray "032" after hide/show in 0.6.7.
            if (versionLocalization != null)
                versionLocalization.RefreshString();
        }

        private void Restore()
        {
            if (versionLocalization != null && localizedVersionHandler != null)
                versionLocalization.OnUpdateString.RemoveListener(localizedVersionHandler);
            if (logo != null)
                logo.enabled = originalLogoEnabled;
            if (version != null)
            {
                version.enabled = originalVersionEnabled;
                RefreshOriginalVersion();
            }

            logo = null;
            version = null;
            versionLocalization = null;
            localizedVersionHandler = null;
        }
    }
}
