using System;
using UnityEngine;
using System.Collections.Generic;

namespace SkinFramework
{
    /// <summary>公开当前预设、主题选择/档案设置、贴图槽查询和主题切换事件。</summary>
    public static class SkinService
    {
        private static SkinTheme currentTheme = SkinPresets.GameDefault;
        private static bool assetsReady;
        private static readonly System.Collections.Generic.Dictionary<string, string> textureOverrides =
            new System.Collections.Generic.Dictionary<string, string>();
        private static readonly HashSet<string> disabledTextureOverrides = new HashSet<string>();
        private static readonly HashSet<string> originalTextureOverrides = new HashSet<string>();
        private static readonly HashSet<string> nestedTextureKeys = new HashSet<string>();
        private static readonly HashSet<string> invalidPlayerTexturePaths = new HashSet<string>();

        public static SkinTheme CurrentTheme => currentTheme;

        /// <summary>按稳定 ID 切换当前主题，并同步框架 ModConfig 中的预设选择项。</summary>
        public static bool TrySelectPreset(string presetId)
        {
            if (!SkinPresets.TryGet(presetId, out SkinTheme theme))
                return false;
            return ModEntry.TrySelectPreset(theme);
        }

        /// <summary>读取当前主题中由调用方命名空间隔离的字符串设置。</summary>
        public static bool TryGetThemeSetting(string ownerId, string settingId, out string value)
        {
            return ModEntry.TryGetExternalThemeSetting(ownerId, settingId, out value);
        }

        /// <summary>将字符串设置写入当前主题档案；调用方 ID 与设置 ID 均按序区分。</summary>
        public static bool SetThemeSetting(string ownerId, string settingId, string value)
        {
            return ModEntry.SetExternalThemeSetting(ownerId, settingId, value);
        }

        /// <summary>
        /// 先按当前主题读取贴图；主题没有该槽时回退到 shared_ 共用贴图。
        /// 贴图尚未加载或主题/共用资源均不存在时返回 false。
        /// </summary>
        public static bool TryGetTexture(string slot, out Texture2D texture)
        {
            return TryGetTexture(slot, out texture, out _);
        }

        internal static bool TryGetTexture(string slot, out Texture2D texture, out string textureKey)
        {
            textureKey = null;
            if (IsTextureDisabled(slot))
            {
                texture = null;
                return false;
            }

            if (IsTextureOriginal(slot))
            {
                texture = null;
                return false;
            }

            if (currentTheme.UsesGameDefaults)
            {
                texture = null;
                return false;
            }

            if (!assetsReady)
            {
                texture = null;
                return false;
            }

            LoadedMod skinMod = ModLoader.Mods.Find(mod => mod.Manifest.ID == "skin_framework");
            string category = GetTextureCategory(slot);
            if (textureOverrides.TryGetValue(slot, out string overrideKey) &&
                !string.IsNullOrWhiteSpace(overrideKey))
            {
                if (TryGetTexture(skinMod, currentTheme.Id, overrideKey, category, out texture, out textureKey) ||
                    TryGetTexture(skinMod, "Shared", overrideKey, category, out texture, out textureKey))
                    return true;
            }

            string presetTextureKey = currentTheme.GetTextureKey(slot);
            if (TryGetTexture(skinMod, currentTheme.Id, presetTextureKey, category,
                out texture, out textureKey))
                return true;

            if (currentTheme.UsesSharedTextureFallbacks)
            {
                string sharedTextureKey = "shared_" + slot;
                if (TryGetTexture(skinMod, "Shared", sharedTextureKey, category, out texture, out textureKey))
                    return true;
            }

            if (currentTheme.UsesSharedTextureFallbacks && IsMainMenuButtonSlot(slot))
            {
                string themedButtonKey = currentTheme.GetTextureKey("main_menu_button");
                if (TryGetTexture(skinMod, currentTheme.Id, themedButtonKey, category, out texture, out textureKey))
                    return true;

                if (TryGetTexture(skinMod, "Shared", "shared_main_menu_button", category, out texture, out textureKey))
                    return true;
            }

            texture = null;
            return false;
        }

        internal static bool TryGetTextureByKey(string slot, string key, out Texture2D texture,
            out string textureKey)
        {
            textureKey = null;
            texture = null;
            if (string.IsNullOrWhiteSpace(slot) || string.IsNullOrWhiteSpace(key) ||
                currentTheme.UsesGameDefaults || !assetsReady || key.IndexOfAny(new[] { '/', '\\' }) >= 0)
                return false;

            string normalized = key.Trim();
            if (normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(0, normalized.Length - 4);
            LoadedMod skinMod = ModLoader.Mods.Find(mod => mod.Manifest.ID == "skin_framework");
            string category = GetTextureCategory(slot);
            if (TryGetTexture(skinMod, currentTheme.Id, normalized, category, out texture, out textureKey) ||
                TryGetTexture(skinMod, "Shared", normalized, category, out texture, out textureKey))
                return true;
            return false;
        }

        internal static void MarkAssetsLoaded() => assetsReady = true;

        internal static void SetTextureOverride(string slot, string textureKey)
        {
            if (string.IsNullOrWhiteSpace(textureKey))
            {
                textureOverrides.Remove(slot);
                disabledTextureOverrides.Remove(slot);
                originalTextureOverrides.Remove(slot);
            }
            else if (string.Equals(textureKey.Trim(), "false", StringComparison.OrdinalIgnoreCase))
            {
                textureOverrides.Remove(slot);
                originalTextureOverrides.Remove(slot);
                disabledTextureOverrides.Add(slot);
            }
            else if (string.Equals(textureKey.Trim(), "null", StringComparison.OrdinalIgnoreCase))
            {
                textureOverrides.Remove(slot);
                disabledTextureOverrides.Remove(slot);
                originalTextureOverrides.Add(slot);
            }
            else
            {
                string normalizedKey = textureKey.Trim();
                if (normalizedKey.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    normalizedKey = normalizedKey.Substring(0, normalizedKey.Length - 4);
                disabledTextureOverrides.Remove(slot);
                originalTextureOverrides.Remove(slot);
                textureOverrides[slot] = normalizedKey;
            }
        }

        /// <summary>Returns whether the active theme or explicit path override hides this texture slot.</summary>
        public static bool IsTextureDisabled(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
                return false;
            if (originalTextureOverrides.Contains(slot))
                return false;
            return disabledTextureOverrides.Contains(slot) ||
                (!currentTheme.UsesGameDefaults && currentTheme.IsTextureDisabled(slot));
        }

        /// <summary>Returns whether the slot explicitly restores the game's original sprite.</summary>
        public static bool IsTextureOriginal(string slot) =>
            !string.IsNullOrWhiteSpace(slot) && originalTextureOverrides.Contains(slot);

        private static bool IsMainMenuButtonSlot(string slot) =>
            slot == SkinTextureSlots.MainMenuNewGameButton ||
            slot == SkinTextureSlots.MainMenuLoadGameButton ||
            slot == SkinTextureSlots.MainMenuOptionsButton ||
            slot == SkinTextureSlots.MainMenuCreditsButton ||
            slot == SkinTextureSlots.MainMenuModsButton ||
            slot == SkinTextureSlots.MainMenuQuitButton;

        /// <summary>在选择变化时同步触发。订阅方应在启用时订阅、禁用时取消订阅。</summary>
        public static event Action<SkinTheme> ThemeChanged;

        internal static void Select(SkinTheme theme)
        {
            if (ReferenceEquals(currentTheme, theme))
                return;

            currentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }

        internal static void Reset()
        {
            assetsReady = false;
            textureOverrides.Clear();
            disabledTextureOverrides.Clear();
            originalTextureOverrides.Clear();
            LoadedMod skinMod = ModLoader.Mods.Find(mod => mod.Manifest.ID == "skin_framework");
            foreach (string key in nestedTextureKeys)
            {
                if (skinMod != null && skinMod.Textures.TryGetValue(key, out Texture2D texture))
                {
                    skinMod.Textures.Remove(key);
                    UnityEngine.Object.Destroy(texture);
                }
            }
            nestedTextureKeys.Clear();
            invalidPlayerTexturePaths.Clear();
            // Event subscription ownership belongs to each caller; do not clear the public event here.
            Select(SkinPresets.GameDefault);
        }

        private static bool TryGetTexture(LoadedMod mod, string presetId, string key, string category,
            out Texture2D texture, out string resolvedKey)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(key) ||
                key.IndexOfAny(new[] { '/', '\\' }) >= 0)
            {
                if (mod.Textures.TryGetValue(key, out texture))
                {
                    resolvedKey = key;
                    return true;
                }
                texture = null;
                resolvedKey = null;
                return false;
            }

            string nestedKey = presetId + "/" + category + "/" + key;
            string playerKey = SkinPresets.GetAssetFolder(presetId) + "/" + category + "/" + key;
            string playerTextureKey = "skin_framework_user/" + playerKey;
            if (mod.Textures.TryGetValue(playerTextureKey, out texture))
            {
                resolvedKey = key;
                return true;
            }

            string playerTexturePath = "Textures/" + playerKey + ".png";
            if (ModHelper.ModDataFileExists(mod.Manifest.ID, playerTexturePath))
            {
                byte[] playerImageBytes = ModHelper.ReadModDataFileBytes(mod.Manifest.ID, playerTexturePath);
                Texture2D playerTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (playerImageBytes != null && playerTexture.LoadImage(playerImageBytes))
                {
                    ConfigureTexture(playerTexture, key, category);
                    mod.Textures[playerTextureKey] = playerTexture;
                    nestedTextureKeys.Add(playerTextureKey);
                    texture = playerTexture;
                    resolvedKey = key;
                    return true;
                }

                UnityEngine.Object.Destroy(playerTexture);
                if (invalidPlayerTexturePaths.Add(playerTexturePath))
                    Debug.LogError("[Skin Framework] 玩家自定义贴图不是有效 PNG，回退到主题贴图：" + playerTexturePath);
            }

            if (mod.Textures.TryGetValue(key, out texture))
            {
                resolvedKey = key;
                return true;
            }

            if (mod.Textures.TryGetValue(nestedKey, out texture))
            {
                resolvedKey = key;
                return true;
            }

            byte[] imageBytes = ModHelper.ReadModFileBytes(mod.Manifest.ID, "Resources/Textures/" + nestedKey + ".png");
            if (imageBytes == null)
            {
                texture = null;
                resolvedKey = null;
                return false;
            }

            Texture2D loadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!loadedTexture.LoadImage(imageBytes))
            {
                UnityEngine.Object.Destroy(loadedTexture);
                texture = null;
                resolvedKey = null;
                return false;
            }

            ConfigureTexture(loadedTexture, key, category);
            mod.Textures[nestedKey] = loadedTexture;
            nestedTextureKeys.Add(nestedKey);
            texture = loadedTexture;
            resolvedKey = key;
            return true;
        }

        private static void ConfigureTexture(Texture2D texture, string key, string category)
        {
            texture.name = key;
            texture.filterMode = category.StartsWith("Gameplay/Fixtures", StringComparison.Ordinal)
                ? FilterMode.Point
                : FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, true);
        }

        internal static string GetTextureCategory(string slot)
        {
            if (slot != null && slot.StartsWith("language_dropdown_", StringComparison.Ordinal))
                return "MainMenu/LanguageDropdown";
            if (slot != null && slot.StartsWith("new_game_specialization_", StringComparison.Ordinal))
                return "MainMenu/Specialization";
            if (slot != null && slot.StartsWith("load_game_", StringComparison.Ordinal))
                return "MainMenu/LoadGame";
            if (slot != null && slot.StartsWith("credits_", StringComparison.Ordinal))
                return "MainMenu/Credits";
            if (slot != null && slot.StartsWith("game_options_", StringComparison.Ordinal))
                return "MainMenu/GameOptions";
            if (slot == SkinTextureSlots.MainMenuBackground)
                return "MainMenu/Background";
            if (slot == "main_menu_button" || IsMainMenuButtonSlot(slot))
                return "MainMenu/Buttons";
            if (slot == SkinTextureSlots.GameplayBackground)
                return "Gameplay/Background";
            if (slot == SkinTextureSlots.GameplayHud)
                return "Gameplay/Icons";
            if (slot == SkinTextureSlots.GameplayFixtureShutterDoor ||
                slot == SkinTextureSlots.GameplayFixtureShutterBlocker)
                return "Gameplay/Fixtures/ShutterDoor";
            if (slot == SkinTextureSlots.GameplayFixtureWindowFrame ||
                slot == SkinTextureSlots.GameplayFixtureWindowTop)
                return "Gameplay/Fixtures/Window";
            if (slot == SkinTextureSlots.GameplayFixtureNegociationTablet)
                return "Gameplay/Fixtures/NegotiationTablet";
            if (slot == SkinTextureSlots.GameplayFixtureCassettePlayer)
                return "Gameplay/Fixtures/CassettePlayer";
            if (slot == SkinTextureSlots.GameplayFixtureRecorder)
                return "Gameplay/Fixtures/Recorder";
            if (slot == SkinTextureSlots.GameplayFixtureClientele)
                return "Gameplay/Fixtures/Clientele";
            if (slot == SkinTextureSlots.GameplayFixtureCalendar)
                return "Gameplay/Fixtures/Calendar";
            if (slot == SkinTextureSlots.GameplayCalendarMenu)
                return "Gameplay/Menus/Calendar";
            if (slot == SkinTextureSlots.GameplayNegotiationTabletMenu)
                return "Gameplay/Menus/NegotiationTablet";
            if (slot == SkinTextureSlots.GameplayPhoneMenu)
                return "Gameplay/Menus/Phone";
            if (slot == SkinTextureSlots.GameplayFixtureCreditDisplay)
                return "Gameplay/Fixtures/CreditDisplay";
            if (slot == SkinTextureSlots.GameplayFixtureClientDisplay)
                return "Gameplay/Fixtures/ClientDisplay";
            if (slot == SkinTextureSlots.GameplayFixtureRadio)
                return "Gameplay/Fixtures/Radio";
            if (slot == SkinTextureSlots.GameplayFixturePoster ||
                slot == SkinTextureSlots.GameplayFixturePosterPulled)
                return "Gameplay/Fixtures/Poster";
            if (slot == SkinTextureSlots.GameplayFixtureShutterButton || slot == SkinTextureSlots.GameplayFixtureShutterButtonPressed)
                return "Gameplay/Fixtures/ShutterButton";
            if (slot == SkinTextureSlots.GameplayFixtureBell || slot == SkinTextureSlots.GameplayFixtureBellPressed)
                return "Gameplay/Fixtures/Bell";
            if (slot == SkinTextureSlots.GameplayFixtureTransferButton || slot == SkinTextureSlots.GameplayFixtureDismissButton ||
                slot == SkinTextureSlots.GameplayFixtureSecurityButton)
                return "Gameplay/Fixtures/ControlButtons";
            if (slot == SkinTextureSlots.GameplayFixtureShowcase || slot == SkinTextureSlots.GameplayFixtureShowcaseMedium ||
                slot == SkinTextureSlots.GameplayFixtureShowcaseLarge)
                return "Gameplay/Fixtures/Showcase";
            if (slot == SkinTextureSlots.GameplayFixtureDrain)
                return "Gameplay/Fixtures/Drain";
            if (slot == SkinTextureSlots.GameplayFixtureFaucet1)
                return "Gameplay/Fixtures/Faucet";
            if (slot == SkinTextureSlots.GameplayFixtureSleepingBag)
                return "Gameplay/Fixtures/SleepingBag";
            if (slot == SkinTextureSlots.GameplayFixtureWall2)
                return "Gameplay/Fixtures/Wall";
            if (slot == SkinTextureSlots.GameplayFixtureDoor)
                return "Gameplay/Fixtures/Door";
            if (slot == SkinTextureSlots.GameplayFixtureDoorLock)
                return "Gameplay/Fixtures/DoorLock";
            if (slot == SkinTextureSlots.GameplayFixtureReputationBox)
                return "Gameplay/Fixtures/ReputationBox";
            if (slot == SkinTextureSlots.GameplayFixtureDesk)
                return "Gameplay/Fixtures/Desk";
            if (slot == SkinTextureSlots.GameplayStoreInventoryGrid)
                return "Gameplay/Fixtures/StoreGrid";
            if (slot == SkinTextureSlots.GameplayFixtureWallWires)
                return "Gameplay/Fixtures/WallWires";
            if (slot == SkinTextureSlots.GameplayFixturePhone)
                return "Gameplay/Fixtures/Phone";
            if (slot == SkinTextureSlots.GameplayFixtureOneHand)
                return "Gameplay/Fixtures/OneHand";
            if (slot == SkinTextureSlots.GameplayFixtureShotgunShadow ||
                slot == SkinTextureSlots.GameplayFixtureShotgunShadowOnly)
                return "Gameplay/Fixtures/Shotgun";
            if (slot == SkinTextureSlots.GameplayFixtureTrashcan0 ||
                slot == SkinTextureSlots.GameplayFixtureTrashcan25 ||
                slot == SkinTextureSlots.GameplayFixtureTrashcan50 ||
                slot == SkinTextureSlots.GameplayFixtureTrashcan75 ||
                slot == SkinTextureSlots.GameplayFixtureTrashcan100)
                return "Gameplay/Fixtures/Trashcan";
            if (slot == SkinTextureSlots.GameplayFixtureDossier0 ||
                slot == SkinTextureSlots.GameplayFixtureDossier1 ||
                slot == SkinTextureSlots.GameplayFixtureDossier2 ||
                slot == SkinTextureSlots.GameplayFixtureDossier3)
                return "Gameplay/Fixtures/Dossier";
            if (slot != null && slot.StartsWith(SkinTextureSlots.GameplayFixturePrefix, StringComparison.Ordinal))
                return "Gameplay/Fixtures";
            return null;
        }
    }
}
