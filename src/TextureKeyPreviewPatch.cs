using System;
using HarmonyLib;
using TMPro;

namespace SkinFramework
{
    [HarmonyPatch(typeof(ModConfigWindow), "BuildEntry")]
    internal static class TextureKeyPreviewPatch
    {
        private const string ModId = "skin_framework";

        [HarmonyPostfix]
        private static void Postfix(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            if (config.ModId != ModId || !TryGetSlot(entry.Key, out string slot))
                return;

            UpdateField(b.Handle, config, entry, slot);
            EnableResolvedTextureEditing(b.Handle, entry, slot);
        }

        private static void UpdateField(CustomUIWindow window, ModConfig config, ConfigEntry entry, string slot)
        {
            CustomUIElement field = window.Get(entry.Key);
            if (field?.input != null)
                field.input.interactable = !SkinService.CurrentTheme.UsesGameDefaults;
            if (field?.input?.placeholder is TMP_Text placeholder)
            {
                field.SetFixedWidth(380f);
                string preview = GetPreview(slot);
                placeholder.text = preview;
                field.input.SetTextWithoutNotify("");

                string tooltip = entry.Tooltip?.Invoke() ?? "";
                string linkId = "modcfg:" + config.ModId + ":" + entry.Key;
                string tooltipText = tooltip + "\n当前实际使用：" + preview;
                TMPIconHover.CustomLinks[linkId] = () => tooltipText;
            }
        }

        internal static void RefreshField(CustomUIWindow window, ModConfig config, ConfigEntry entry, string slot) =>
            UpdateField(window, config, entry, slot);

        private static void EnableResolvedTextureEditing(CustomUIWindow window, ConfigEntry entry, string slot)
        {
            CustomUIElement field = window.Get(entry.Key);
            if (field?.input?.placeholder is not TMP_Text placeholder)
                return;

            TMP_InputField input = field.input;
            input.onSelect.AddListener(_ =>
            {
                if (string.IsNullOrEmpty(placeholder.text))
                    return;

                placeholder.text = "";
                string value = entry.BoxedValue?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    UnityEngine.Texture2D texture;
                    string textureKey;
                    if (!SkinService.TryGetTexture(slot, out texture, out textureKey))
                        return;
                    value = textureKey + ".png";
                }
                else if (!string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) &&
                    !value.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    value += ".png";
                }

                input.SetTextWithoutNotify(value);
                input.caretPosition = value.Length;
            });
        }

        private static string GetPreview(string slot)
        {
            if (SkinService.IsTextureOriginal(slot))
                return SkinLanguage.Text("已恢复游戏原版贴图（null）", "Original game texture (null)");
            if (SkinService.IsTextureDisabled(slot))
                return SkinLanguage.Text("已隐藏（False）", "Hidden (False)");
            if (SkinService.CurrentTheme.UsesGameDefaults)
                return SkinLanguage.Text("游戏原版贴图", "Original game texture");

            if (SkinService.TryGetTexture(slot, out _, out string textureKey))
                return textureKey + ".png";
            return SkinLanguage.Text("缺图 · ", "Missing texture · ") + SkinService.GetTextureCategory(slot) + "/";
        }

        internal static bool TryGetSlot(string key, out string slot)
        {
            if (key == "main_menu_background_key_v1") slot = SkinTextureSlots.MainMenuBackground;
            else if (key == "main_menu_new_game_button_key_v1") slot = SkinTextureSlots.MainMenuNewGameButton;
            else if (key == "main_menu_load_game_button_key_v1") slot = SkinTextureSlots.MainMenuLoadGameButton;
            else if (key == "main_menu_options_button_key_v1") slot = SkinTextureSlots.MainMenuOptionsButton;
            else if (key == "main_menu_credits_button_key_v1") slot = SkinTextureSlots.MainMenuCreditsButton;
            else if (key == "main_menu_mods_button_key_v1") slot = SkinTextureSlots.MainMenuModsButton;
            else if (key == "main_menu_quit_button_key_v1") slot = SkinTextureSlots.MainMenuQuitButton;
            else if (key == "gameplay_fixture_trashcan_0_key_v1") slot = SkinTextureSlots.GameplayFixtureTrashcan0;
            else if (key == "gameplay_fixture_trashcan_25_key_v1") slot = SkinTextureSlots.GameplayFixtureTrashcan25;
            else if (key == "gameplay_fixture_trashcan_50_key_v1") slot = SkinTextureSlots.GameplayFixtureTrashcan50;
            else if (key == "gameplay_fixture_trashcan_75_key_v1") slot = SkinTextureSlots.GameplayFixtureTrashcan75;
            else if (key == "gameplay_fixture_trashcan_100_key_v1") slot = SkinTextureSlots.GameplayFixtureTrashcan100;
            else if (key == "gameplay_fixture_dossier_0_key_v1") slot = SkinTextureSlots.GameplayFixtureDossier0;
            else if (key == "gameplay_fixture_dossier_1_key_v1") slot = SkinTextureSlots.GameplayFixtureDossier1;
            else if (key == "gameplay_fixture_dossier_2_key_v1") slot = SkinTextureSlots.GameplayFixtureDossier2;
            else if (key == "gameplay_fixture_dossier_3_key_v1") slot = SkinTextureSlots.GameplayFixtureDossier3;
            else if (key == "gameplay_fixture_cassette_player_key_v1") slot = SkinTextureSlots.GameplayFixtureCassettePlayer;
            else if (key == "gameplay_fixture_recorder_key_v1") slot = SkinTextureSlots.GameplayFixtureRecorder;
            else if (key == "gameplay_fixture_clientele_key_v1") slot = SkinTextureSlots.GameplayFixtureClientele;
            else if (key == "gameplay_fixture_calendar_key_v1") slot = SkinTextureSlots.GameplayFixtureCalendar;
            else if (key == "gameplay_fixture_credit_display_key_v1") slot = SkinTextureSlots.GameplayFixtureCreditDisplay;
            else if (key == "gameplay_fixture_client_display_key_v1") slot = SkinTextureSlots.GameplayFixtureClientDisplay;
            else if (key == "gameplay_fixture_radio_key_v1") slot = SkinTextureSlots.GameplayFixtureRadio;
            else if (key == "gameplay_fixture_shutter_door_key_v1") slot = SkinTextureSlots.GameplayFixtureShutterDoor;
            else if (key == "gameplay_fixture_shutter_blocker_key_v1") slot = SkinTextureSlots.GameplayFixtureShutterBlocker;
            else if (key == "gameplay_fixture_window_frame_key_v1") slot = SkinTextureSlots.GameplayFixtureWindowFrame;
            else if (key == "gameplay_fixture_window_top_key_v1") slot = SkinTextureSlots.GameplayFixtureWindowTop;
            else if (key == "gameplay_fixture_negociation_tablet_key_v1") slot = SkinTextureSlots.GameplayFixtureNegociationTablet;
            else if (key == "gameplay_fixture_poster_key_v1") slot = SkinTextureSlots.GameplayFixturePoster;
            else if (key == "gameplay_fixture_poster_pulled_key_v1") slot = SkinTextureSlots.GameplayFixturePosterPulled;
            else if (key == "gameplay_fixture_shutter_button_key_v1") slot = SkinTextureSlots.GameplayFixtureShutterButton;
            else if (key == "gameplay_fixture_shutter_button_pressed_key_v1") slot = SkinTextureSlots.GameplayFixtureShutterButtonPressed;
            else if (key == "gameplay_fixture_bell_key_v1") slot = SkinTextureSlots.GameplayFixtureBell;
            else if (key == "gameplay_fixture_bell_pressed_key_v1") slot = SkinTextureSlots.GameplayFixtureBellPressed;
            else if (key == "gameplay_fixture_transfer_button_key_v1") slot = SkinTextureSlots.GameplayFixtureTransferButton;
            else if (key == "gameplay_fixture_dismiss_button_key_v1") slot = SkinTextureSlots.GameplayFixtureDismissButton;
            else if (key == "gameplay_fixture_security_button_key_v1") slot = SkinTextureSlots.GameplayFixtureSecurityButton;
            else if (key == "gameplay_fixture_showcase_key_v1") slot = SkinTextureSlots.GameplayFixtureShowcase;
            else if (key == "gameplay_fixture_showcase_medium_key_v1") slot = SkinTextureSlots.GameplayFixtureShowcaseMedium;
            else if (key == "gameplay_fixture_showcase_large_key_v1") slot = SkinTextureSlots.GameplayFixtureShowcaseLarge;
            else if (key == "gameplay_fixture_drain_key_v1") slot = SkinTextureSlots.GameplayFixtureDrain;
            else if (key == "gameplay_fixture_faucet1_key_v1") slot = SkinTextureSlots.GameplayFixtureFaucet1;
            else if (key == "gameplay_fixture_sleeping_bag_key_v1") slot = SkinTextureSlots.GameplayFixtureSleepingBag;
            else if (key == "gameplay_fixture_wall2_key_v1") slot = SkinTextureSlots.GameplayFixtureWall2;
            else if (key == "gameplay_fixture_door_key_v1") slot = SkinTextureSlots.GameplayFixtureDoor;
            else if (key == "gameplay_fixture_door_lock_key_v1") slot = SkinTextureSlots.GameplayFixtureDoorLock;
            else if (key == "gameplay_fixture_reputation_box_key_v1") slot = SkinTextureSlots.GameplayFixtureReputationBox;
            else if (key == "gameplay_fixture_desk_key_v1") slot = SkinTextureSlots.GameplayFixtureDesk;
            else if (key == "gameplay_fixture_wall_wires_key_v1") slot = SkinTextureSlots.GameplayFixtureWallWires;
            else if (key == "gameplay_fixture_phone_key_v1") slot = SkinTextureSlots.GameplayFixturePhone;
            else if (key == "gameplay_fixture_one_hand_key_v1") slot = SkinTextureSlots.GameplayFixtureOneHand;
            else if (key == "gameplay_fixture_shotgun_shadow_key_v1") slot = SkinTextureSlots.GameplayFixtureShotgunShadow;
            else if (key == "gameplay_fixture_shotgun_shadow_only_key_v1") slot = SkinTextureSlots.GameplayFixtureShotgunShadowOnly;
            else if (key == "new_game_specialization_window_key_v1") slot = SkinTextureSlots.SpecializationWindow;
            else if (key == "new_game_specialization_panel_key_v1") slot = SkinTextureSlots.SpecializationPanel;
            else if (key == "new_game_specialization_separator_key_v1") slot = SkinTextureSlots.SpecializationSeparator;
            else if (key == "new_game_specialization_arrow_prev_key_v1") slot = SkinTextureSlots.SpecializationArrowPrev;
            else if (key == "new_game_specialization_arrow_prev_hover_key_v1") slot = SkinTextureSlots.SpecializationArrowPrevHover;
            else if (key == "new_game_specialization_arrow_next_key_v1") slot = SkinTextureSlots.SpecializationArrowNext;
            else if (key == "new_game_specialization_arrow_next_hover_key_v1") slot = SkinTextureSlots.SpecializationArrowNextHover;
            else if (key == "new_game_specialization_continue_frame_key_v1") slot = SkinTextureSlots.SpecializationContinueFrame;
            else if (key == "new_game_specialization_slot_normal_key_v1") slot = SkinTextureSlots.SpecializationSlotNormal;
            else if (key == "new_game_specialization_slot_selected_key_v1") slot = SkinTextureSlots.SpecializationSlotSelected;
            else if (key == "new_game_specialization_icon_key_v1") slot = SkinTextureSlots.SpecializationIcon;
            else if (key == "game_options_window_key_v1") slot = SkinTextureSlots.GameOptionsWindow;
            else if (key == "game_options_title_bar_key_v1") slot = SkinTextureSlots.GameOptionsTitleBar;
            else if (key == "game_options_content_panel_key_v1") slot = SkinTextureSlots.GameOptionsContentPanel;
            else if (key == "game_options_close_button_key_v1") slot = SkinTextureSlots.GameOptionsCloseButton;
            else if (key == "game_options_close_button_hover_key_v1") slot = SkinTextureSlots.GameOptionsCloseButtonHover;
            else if (key == "game_options_scroll_track_key_v1") slot = SkinTextureSlots.GameOptionsScrollTrack;
            else if (key == "game_options_scroll_thumb_key_v1") slot = SkinTextureSlots.GameOptionsScrollThumb;
            else if (key == "game_options_separator_key_v1") slot = SkinTextureSlots.GameOptionsSeparator;
            else if (key == "game_options_apply_button_key_v1") slot = SkinTextureSlots.GameOptionsApplyButton;
            else if (key == "game_options_apply_button_hover_key_v1") slot = SkinTextureSlots.GameOptionsApplyButtonHover;
            else
            {
                foreach (LoadGameTextureSlotSetting setting in LoadGamePageAdapter.TextureSlots)
                {
                    if (setting.ConfigKey == key)
                    {
                        slot = setting.Slot;
                        return true;
                    }
                }
                foreach (CreditsTextureSlotSetting setting in CreditsPageAdapter.TextureSlots)
                {
                    if (setting.ConfigKey == key)
                    {
                        slot = setting.Slot;
                        return true;
                    }
                }
                slot = null;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ModConfigWindow), "Apply")]
    internal static class TextureKeyPreviewRefreshPatch
    {
        private const string ModId = "skin_framework";

        [HarmonyPrefix]
        private static bool Prefix(CustomUIWindow window, ModConfig config, out bool __state)
        {
            __state = false;
            if (config.ModId != ModId || window == null || !window.IsAlive)
                return true;
            if (!SkinPresetApplyDialog.Confirm(window, config))
                return false;

            __state = true;
            ModEntry.BeginConfigApply();

            foreach (ConfigEntry entry in config.Entries)
            {
                if (!TextureKeyPreviewPatch.TryGetSlot(entry.Key, out _))
                    continue;

                CustomUIElement field = window.Get(entry.Key);
                if (field?.input?.placeholder is TMP_Text placeholder &&
                    !string.IsNullOrEmpty(placeholder.text) &&
                    string.IsNullOrEmpty(field.input.text) &&
                    !string.IsNullOrEmpty(entry.BoxedValue?.ToString()))
                {
                    // An untouched preview field is blank by design; retain its saved override on Apply.
                    field.input.SetTextWithoutNotify(entry.BoxedValue.ToString());
                }
            }
            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(CustomUIWindow window, ModConfig config, bool __state)
        {
            if (!__state)
                return;

            ModEntry.EndConfigApply(window);

            foreach (ConfigEntry entry in config.Entries)
            {
                if (TextureKeyPreviewPatch.TryGetSlot(entry.Key, out string slot))
                    TextureKeyPreviewPatch.RefreshField(window, config, entry, slot);
            }
        }
        [HarmonyFinalizer]
        private static void Finalizer(Exception __exception, bool __state)
        {
            if (__exception != null && __state)
                ModEntry.AbortConfigApply();
        }

    }

    [HarmonyPatch(typeof(ModConfigWindow), "ResetWidgets")]
    internal static class TextureKeyPreviewResetPatch
    {
        private const string ModId = "skin_framework";

        [HarmonyPostfix]
        private static void Postfix(CustomUIWindow window, ModConfig config)
        {
            if (config.ModId != ModId)
                return;

            foreach (ConfigEntry entry in config.Entries)
            {
                if (!TextureKeyPreviewPatch.TryGetSlot(entry.Key, out _))
                    continue;

                CustomUIElement field = window.Get(entry.Key);
                if (field?.input?.placeholder is TMP_Text placeholder)
                    // ResetWidgets makes the blank input intentional, so Apply must not restore its old value.
                    placeholder.text = "";
            }
        }
    }
}
