using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SkinFramework
{
    public sealed class ModEntry : IMod
    {
        private static ModEntry active;
        private const string ConfigKey = "selected_skin_v1";
        private const string MainMenuFontColorKey = "main_menu_font_color_v1";
        private ModLog log;
        private ModConfig config;
        private ConfigEntry<string> selectedSkin;
        private ConfigEntry<string> mainMenuBackgroundKey;
        private ConfigEntry<string> mainMenuNewGameButton;
        private ConfigEntry<string> mainMenuLoadGameButton;
        private ConfigEntry<string> mainMenuOptionsButton;
        private ConfigEntry<string> mainMenuCreditsButton;
        private ConfigEntry<string> mainMenuModsButton;
        private ConfigEntry<string> mainMenuQuitButton;
        private readonly List<TextureSlotBinding> fixedFixtureTextureSlots = new List<TextureSlotBinding>();
        private ConfigEntry<string> mainMenuFontColor;
        private ConfigEntry<string> mainMenuPositionOffsets;
        private ConfigEntry<string> specializationPositionOffsets;
        private ConfigEntry<string> loadGamePositionOffsets;
        private ConfigEntry<string> creditsPositionOffsets;
        private ConfigEntry<string> gameOptionsPositionOffsets;
        private ConfigEntry<string> mainMenuTransparency;
        private ConfigEntry<string> specializationTransparency;
        private ConfigEntry<string> loadGameTransparency;
        private ConfigEntry<string> creditsTransparency;
        private ConfigEntry<string> gameOptionsTransparency;
        private ConfigEntry<string> skinAdjustments;
        private ConfigEntry<bool> skinAdjustmentsMigrated;
        private ConfigEntry<bool> skinSettingsMigrated;
        private readonly SkinAdjustmentStore adjustmentStore = new SkinAdjustmentStore();
        private MainMenuBackgroundAdapter mainMenuBackground;
        private MainMenuButtonAdapter mainMenuButtons;
        private MainMenuBrandingController mainMenuBranding;
        private readonly LanguageDropdownAdapter languageDropdown = new LanguageDropdownAdapter();
        private readonly List<TextureSlotBinding> specializationTextureSlots = new List<TextureSlotBinding>();
        private readonly List<TextureSlotBinding> loadGameTextureSlots = new List<TextureSlotBinding>();
        private readonly List<TextureSlotBinding> creditsTextureSlots = new List<TextureSlotBinding>();
        private readonly List<TextureSlotBinding> gameOptionsTextureSlots = new List<TextureSlotBinding>();
        private SpecializationPageAdapter specializationPage;
        private LoadGamePageAdapter loadGamePage;
        private CreditsPageAdapter creditsPage;
        private GameOptionsTextureAdapter gameOptionsTextures;
        private PositionAdjustmentController positionAdjustment;
        private TransparencyAdjustmentController transparencyAdjustment;
        private FixedFixtureEditor fixedFixtureEditor;
        private bool configReady;
        private bool configEventsBound;
        private bool specializationStripRefreshPending;
        private bool configApplyInProgress;
        private string configApplyOwnerSkinId;
        private bool themeChangePending;

        public void Init(ModManifest manifest)
        {
            log = new ModLog(manifest);
            mainMenuBackground = new MainMenuBackgroundAdapter();
            mainMenuButtons = new MainMenuButtonAdapter();
            mainMenuBranding = new MainMenuBrandingController();
            specializationPage = new SpecializationPageAdapter(log);
            loadGamePage = new LoadGamePageAdapter();
            creditsPage = new CreditsPageAdapter();
            gameOptionsTextures = new GameOptionsTextureAdapter();
            positionAdjustment = new PositionAdjustmentController();
            transparencyAdjustment = new TransparencyAdjustmentController();
            fixedFixtureEditor = new FixedFixtureEditor();
            config = ModHelper.GetConfig(manifest.ID);
        }

        public void OnEnable()
        {
            active = this;
            ModHook.OnModAssetsLoaded += OnAssetsLoaded;
            ModHook.OnGameLoadedLate += ApplyFixturesAfterGameLoaded;
            ModHook.OnShutterOpenedLate += ApplyFixturesAfterStoreOpened;
            ModHook.OnReturningStoreLate += ApplyFixturesAfterReturningStore;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SkinService.ThemeChanged += OnThemeChanged;
            bool registered = ModLoader.Mods.Find(mod => mod.Manifest.ID == "skin_framework") != null;
            if (!configReady && registered)
                InitializeConfiguration();
            if (configReady)
            {
                if (registered)
                    SkinService.MarkAssetsLoaded();
                ActivateConfiguration();
            }
            mainMenuBackground.ApplyToActiveScene();
            mainMenuButtons.ApplyToActiveScene();
            languageDropdown.ApplyToActiveScene();
            if (configReady)
                loadGamePage.ApplyToOpenPage();
            if (configReady)
                creditsPage.ApplyToOpenPage();
            transparencyAdjustment.ApplyMainMenu();
            if (configReady)
                StoreInventoryGridSkinAdapter.Apply();
        }

        public void OnDisable()
        {
            specializationStripRefreshPending = false;
            ModHook.OnModAssetsLoaded -= OnAssetsLoaded;
            ModHook.OnGameLoadedLate -= ApplyFixturesAfterGameLoaded;
            ModHook.OnShutterOpenedLate -= ApplyFixturesAfterStoreOpened;
            ModHook.OnReturningStoreLate -= ApplyFixturesAfterReturningStore;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SkinService.ThemeChanged -= OnThemeChanged;
            UnbindConfigurationEvents();
            mainMenuBackground.Dispose();
            mainMenuButtons.Dispose();
            mainMenuBranding.Dispose();
            languageDropdown.Dispose();
            specializationPage.Dispose();
            loadGamePage.Dispose();
            creditsPage.Dispose();
            gameOptionsTextures.Dispose();
            positionAdjustment.Dispose();
            transparencyAdjustment.Dispose();
            fixedFixtureEditor.Dispose();
            StoreInventoryGridSkinAdapter.Restore();
            SkinService.Reset();
            if (active == this)
                active = null;
        }

        private void OnAssetsLoaded(LoadedMod mod)
        {
            if (mod.Manifest.ID == "skin_framework")
            {
                InitializeConfiguration();
                SkinService.MarkAssetsLoaded();
                ActivateConfiguration();
                StoreInventoryGridSkinAdapter.Apply();
                mainMenuBackground.ApplyToActiveScene();
                mainMenuButtons.ApplyToActiveScene();
                languageDropdown.ApplyToActiveScene();
                specializationPage.ApplyToOpenPage();
                loadGamePage.ApplyToOpenPage();
                creditsPage.ApplyToOpenPage();
                transparencyAdjustment.ApplyMainMenu();
                transparencyAdjustment.ApplySpecialization();
                transparencyAdjustment.ApplyCredits();
            }
        }

        private void InitializeConfiguration()
        {
            if (configReady)
                return;

            // ModHelper resolves folders through ModLoader.Mods; this callback runs after registration.
            SkinPresets.Load(ModHelper.ReadModFileText("skin_framework",
                "Resources/Configs/skin_presets.json"), log);

            var language = config.Define("ui_language_v1", "简体中文", "Language / 语言",
                () => "Language / 语言", choices: new[] { "简体中文", "English" },
                tooltip: () => "Restart the game to switch the mod language. / 重启游戏以切换模组语言。",
                requiresRestart: true);
            SkinLanguage.English = language.Value == "English";

            selectedSkin = config.Define(ConfigKey, SkinPresets.GameDefault.DisplayName, SkinLanguage.Text("皮肤", "Skins"),
                () => SkinLanguage.Text("预设主题", "Skin preset"), choices: GetPresetNames(),
                tooltip: () => SkinLanguage.Text("新建和删除自建预设会立即更新列表；手动编辑预设 JSON 或贴图后需重启游戏。", "Creating or deleting presets updates the list immediately. Restart after editing JSON or texture files manually."));

            mainMenuBackgroundKey = DefineTextureSlot(config, "main_menu_background_key_v1", SkinLanguage.Text("主界面", "Main menu"), SkinLanguage.Text("背景贴图", "Background texture"),
                "shared_main_menu_background");
            mainMenuNewGameButton = DefineTextureSlot(config, "main_menu_new_game_button_key_v1", SkinLanguage.Text("主界面", "Main menu"),
                SkinLanguage.Text("新游戏按钮贴图", "New Game button"), "");
            mainMenuLoadGameButton = DefineTextureSlot(config, "main_menu_load_game_button_key_v1", SkinLanguage.Text("主界面", "Main menu"),
                SkinLanguage.Text("载入游戏按钮贴图", "Load Game button"), "");
            mainMenuOptionsButton = DefineTextureSlot(config, "main_menu_options_button_key_v1", SkinLanguage.Text("主界面", "Main menu"),
                SkinLanguage.Text("选项按钮贴图", "Options button"), "");
            mainMenuCreditsButton = DefineTextureSlot(config, "main_menu_credits_button_key_v1", SkinLanguage.Text("主界面", "Main menu"),
                SkinLanguage.Text("制作人名单按钮贴图", "Credits button"), "");
            mainMenuModsButton = DefineTextureSlot(config, "main_menu_mods_button_key_v1", SkinLanguage.Text("主界面", "Main menu"),
                SkinLanguage.Text("改装体按钮贴图", "Mods button"), "");
            mainMenuQuitButton = DefineTextureSlot(config, "main_menu_quit_button_key_v1", SkinLanguage.Text("主界面", "Main menu"),
                SkinLanguage.Text("退出游戏按钮贴图", "Quit button"), "");
            AddFixedFixtureTextureSlot("gameplay_fixture_trashcan_0_key_v1",
                SkinTextureSlots.GameplayFixtureTrashcan0, SkinLanguage.Text("废纸篓空桶贴图", "Empty trash can"));
            AddFixedFixtureTextureSlot("gameplay_fixture_trashcan_25_key_v1",
                SkinTextureSlots.GameplayFixtureTrashcan25, SkinLanguage.Text("废纸篓 25% 贴图", "Trash can (25%)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_trashcan_50_key_v1",
                SkinTextureSlots.GameplayFixtureTrashcan50, SkinLanguage.Text("废纸篓 50% 贴图", "Trash can (50%)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_trashcan_75_key_v1",
                SkinTextureSlots.GameplayFixtureTrashcan75, SkinLanguage.Text("废纸篓 75% 贴图", "Trash can (75%)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_trashcan_100_key_v1",
                SkinTextureSlots.GameplayFixtureTrashcan100, SkinLanguage.Text("废纸篓满桶贴图", "Full trash can"));
            AddFixedFixtureTextureSlot("gameplay_fixture_dossier_0_key_v1",
                SkinTextureSlots.GameplayFixtureDossier0, SkinLanguage.Text("档案箱空档贴图", "Empty file box"));
            AddFixedFixtureTextureSlot("gameplay_fixture_dossier_1_key_v1",
                SkinTextureSlots.GameplayFixtureDossier1, SkinLanguage.Text("档案箱一档贴图", "File box (level 1)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_dossier_2_key_v1",
                SkinTextureSlots.GameplayFixtureDossier2, SkinLanguage.Text("档案箱二档贴图", "File box (level 2)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_dossier_3_key_v1",
                SkinTextureSlots.GameplayFixtureDossier3, SkinLanguage.Text("档案箱满档贴图", "Full file box"));
            AddFixedFixtureTextureSlot("gameplay_fixture_cassette_player_key_v1",
                SkinTextureSlots.GameplayFixtureCassettePlayer, SkinLanguage.Text("磁带播放器贴图", "Cassette player"));
            AddFixedFixtureTextureSlot("gameplay_fixture_recorder_key_v1",
                SkinTextureSlots.GameplayFixtureRecorder, SkinLanguage.Text("录音机贴图", "Recorder"));
            AddFixedFixtureTextureSlot("gameplay_fixture_clientele_key_v1",
                SkinTextureSlots.GameplayFixtureClientele, SkinLanguage.Text("顾客信息屏动画贴图", "Customer information display"));
            AddFixedFixtureTextureSlot("gameplay_fixture_radio_key_v1",
                SkinTextureSlots.GameplayFixtureRadio, SkinLanguage.Text("收音机贴图", "Radio"));
            AddFixedFixtureTextureSlot("gameplay_fixture_shutter_door_key_v1",
                SkinTextureSlots.GameplayFixtureShutterDoor, SkinLanguage.Text("卷帘门主体贴图", "Roller shutter"));
            AddFixedFixtureTextureSlot("gameplay_fixture_shutter_blocker_key_v1",
                SkinTextureSlots.GameplayFixtureShutterBlocker, SkinLanguage.Text("卷帘门遮挡层贴图", "Shutter occlusion layer"));
            AddFixedFixtureTextureSlot("gameplay_fixture_window_frame_key_v1",
                SkinTextureSlots.GameplayFixtureWindowFrame, SkinLanguage.Text("卷帘边框贴图", "Shutter frame"));
            AddFixedFixtureTextureSlot("gameplay_fixture_window_top_key_v1",
                SkinTextureSlots.GameplayFixtureWindowTop, SkinLanguage.Text("卷帘上框贴图", "Shutter top frame"));
            AddFixedFixtureTextureSlot("gameplay_fixture_negociation_tablet_key_v1",
                SkinTextureSlots.GameplayFixtureNegociationTablet, SkinLanguage.Text("议价平板贴图", "Negotiation tablet"));
            AddFixedFixtureTextureSlot("gameplay_fixture_poster_key_v1",
                SkinTextureSlots.GameplayFixturePoster, SkinLanguage.Text("墙上海报贴图", "Wall poster"));
            AddFixedFixtureTextureSlot("gameplay_fixture_poster_pulled_key_v1",
                SkinTextureSlots.GameplayFixturePosterPulled, SkinLanguage.Text("撕下状态海报贴图", "Removed poster"));
            AddFixedFixtureTextureSlot("gameplay_fixture_shutter_button_key_v1", SkinTextureSlots.GameplayFixtureShutterButton, SkinLanguage.Text("卷帘门按钮贴图", "Shutter button"));
            AddFixedFixtureTextureSlot("gameplay_fixture_shutter_button_pressed_key_v1", SkinTextureSlots.GameplayFixtureShutterButtonPressed, SkinLanguage.Text("卷帘门按钮按下贴图", "Shutter button (pressed)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_bell_key_v1", SkinTextureSlots.GameplayFixtureBell, SkinLanguage.Text("呼叫铃按钮贴图", "Call bell"));
            AddFixedFixtureTextureSlot("gameplay_fixture_bell_pressed_key_v1", SkinTextureSlots.GameplayFixtureBellPressed, SkinLanguage.Text("呼叫铃按钮按下贴图", "Call bell (pressed)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_transfer_button_key_v1", SkinTextureSlots.GameplayFixtureTransferButton, SkinLanguage.Text("转账按钮贴图", "Transfer button"));
            AddFixedFixtureTextureSlot("gameplay_fixture_dismiss_button_key_v1", SkinTextureSlots.GameplayFixtureDismissButton, SkinLanguage.Text("遣散按钮贴图", "Dismiss button"));
            AddFixedFixtureTextureSlot("gameplay_fixture_security_button_key_v1", SkinTextureSlots.GameplayFixtureSecurityButton, SkinLanguage.Text("安保按钮贴图", "Security button"));
            AddFixedFixtureTextureSlot("gameplay_fixture_showcase_key_v1", SkinTextureSlots.GameplayFixtureShowcase, SkinLanguage.Text("展示柜基础贴图", "Display case (base)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_showcase_medium_key_v1", SkinTextureSlots.GameplayFixtureShowcaseMedium, SkinLanguage.Text("展示柜一级贴图", "Display case (level 1)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_showcase_large_key_v1", SkinTextureSlots.GameplayFixtureShowcaseLarge, SkinLanguage.Text("展示柜二级贴图", "Display case (level 2)"));
            AddFixedFixtureTextureSlot("gameplay_fixture_drain_key_v1",
                SkinTextureSlots.GameplayFixtureDrain, SkinLanguage.Text("排水口贴图", "Drain"));
            AddFixedFixtureTextureSlot("gameplay_fixture_faucet1_key_v1",
                SkinTextureSlots.GameplayFixtureFaucet1, SkinLanguage.Text("水龙头出水状态贴图", "Running faucet"));
            AddFixedFixtureTextureSlot("gameplay_fixture_sleeping_bag_key_v1",
                SkinTextureSlots.GameplayFixtureSleepingBag, SkinLanguage.Text("睡袋贴图", "Sleeping bag"));
            AddFixedFixtureTextureSlot("gameplay_fixture_wall2_key_v1",
                SkinTextureSlots.GameplayFixtureWall2, SkinLanguage.Text("店铺墙面贴图", "Shop wall"));
            AddFixedFixtureTextureSlot("gameplay_fixture_door_key_v1",
                SkinTextureSlots.GameplayFixtureDoor, SkinLanguage.Text("店门贴图", "Shop door"));
            AddFixedFixtureTextureSlot("gameplay_fixture_door_lock_key_v1",
                SkinTextureSlots.GameplayFixtureDoorLock, SkinLanguage.Text("门锁贴图", "Door lock"));
            AddFixedFixtureTextureSlot("gameplay_fixture_reputation_box_key_v1",
                SkinTextureSlots.GameplayFixtureReputationBox, SkinLanguage.Text("反馈箱贴图", "Feedback box"));
            AddFixedFixtureTextureSlot("gameplay_fixture_desk_key_v1",
                SkinTextureSlots.GameplayFixtureDesk, SkinLanguage.Text("柜台贴图", "Counter"));
            AddFixedFixtureTextureSlot("gameplay_store_inventory_grid_key_v1",
                SkinTextureSlots.GameplayStoreInventoryGrid, SkinLanguage.Text("店铺库存格背景贴图", "Shop inventory grid background"));
            AddFixedFixtureTextureSlot("gameplay_fixture_wall_wires_key_v1",
                SkinTextureSlots.GameplayFixtureWallWires, SkinLanguage.Text("墙面电线贴图", "Wall wires"));
            AddFixedFixtureTextureSlot("gameplay_fixture_table_wires_key_v1",
                SkinTextureSlots.GameplayFixtureTableWires, SkinLanguage.Text("桌面电线贴图", "Desk wires"));
            AddFixedFixtureTextureSlot("gameplay_fixture_connecting_wire_key_v1",
                SkinTextureSlots.GameplayFixtureConnectingWire, SkinLanguage.Text("电脑连接线贴图", "Computer cable"));
            AddFixedFixtureTextureSlot("gameplay_fixture_calendar_wire_key_v1",
                SkinTextureSlots.GameplayFixtureCalendarWire, SkinLanguage.Text("日历电线贴图", "Calendar wire"));
            AddFixedFixtureTextureSlot("gameplay_fixture_calendar_key_v1",
                SkinTextureSlots.GameplayFixtureCalendar, SkinLanguage.Text("日历贴图", "Calendar"));
            AddFixedFixtureTextureSlot("gameplay_fixture_credit_display_key_v1",
                SkinTextureSlots.GameplayFixtureCreditDisplay, SkinLanguage.Text("信用点显示器贴图", "Credit display"));
            AddFixedFixtureTextureSlot("gameplay_fixture_client_display_key_v1",
                SkinTextureSlots.GameplayFixtureClientDisplay, SkinLanguage.Text("顾客显示屏贴图", "Customer display"));
            AddFixedFixtureTextureSlot("gameplay_fixture_phone_key_v1",
                SkinTextureSlots.GameplayFixturePhone, SkinLanguage.Text("壁挂电话贴图", "Wall phone"));
            AddFixedFixtureTextureSlot("gameplay_fixture_one_hand_key_v1",
                SkinTextureSlots.GameplayFixtureOneHand, SkinLanguage.Text("手持棍子贴图", "Held stick"));
            AddFixedFixtureTextureSlot("gameplay_fixture_shotgun_shadow_key_v1",
                SkinTextureSlots.GameplayFixtureShotgunShadow, SkinLanguage.Text("棍子阴影贴图", "Stick shadow"));
            AddFixedFixtureTextureSlot("gameplay_fixture_shotgun_shadow_only_key_v1",
                SkinTextureSlots.GameplayFixtureShotgunShadowOnly, SkinLanguage.Text("棍子主体贴图", "Stick body"));
            AddSpecializationTextureSlot("new_game_specialization_window_key_v1",
                SkinTextureSlots.SpecializationWindow, SkinLanguage.Text("专精页外框贴图", "Specialization window frame"));
            AddSpecializationTextureSlot("new_game_specialization_panel_key_v1",
                SkinTextureSlots.SpecializationPanel, SkinLanguage.Text("专精页内容底板贴图", "Specialization panel"));
            AddSpecializationTextureSlot("new_game_specialization_separator_key_v1",
                SkinTextureSlots.SpecializationSeparator, SkinLanguage.Text("专精页分隔线贴图", "Specialization divider"));
            AddSpecializationTextureSlot("new_game_specialization_arrow_prev_key_v1",
                SkinTextureSlots.SpecializationArrowPrev, SkinLanguage.Text("返回 / 上一页箭头贴图", "Previous arrow"));
            AddSpecializationTextureSlot("new_game_specialization_arrow_prev_hover_key_v1",
                SkinTextureSlots.SpecializationArrowPrevHover, SkinLanguage.Text("返回 / 上一页箭头悬停贴图", "Previous arrow (hover)"));
            AddSpecializationTextureSlot("new_game_specialization_arrow_next_key_v1",
                SkinTextureSlots.SpecializationArrowNext, SkinLanguage.Text("下一页箭头贴图", "Next arrow"));
            AddSpecializationTextureSlot("new_game_specialization_arrow_next_hover_key_v1",
                SkinTextureSlots.SpecializationArrowNextHover, SkinLanguage.Text("下一页箭头悬停贴图", "Next arrow (hover)"));
            AddSpecializationTextureSlot("new_game_specialization_continue_frame_key_v1",
                SkinTextureSlots.SpecializationContinueFrame, SkinLanguage.Text("继续按钮边框贴图", "Continue button frame"));
            AddSpecializationTextureSlot("new_game_specialization_slot_normal_key_v1",
                SkinTextureSlots.SpecializationSlotNormal, SkinLanguage.Text("专精图标底槽（普通，可选）", "Specialization slot (normal, optional)"));
            AddSpecializationTextureSlot("new_game_specialization_slot_selected_key_v1",
                SkinTextureSlots.SpecializationSlotSelected, SkinLanguage.Text("专精图标底槽（选中，可选）", "Specialization slot (selected, optional)"));
            AddSpecializationTextureSlot("new_game_specialization_icon_key_v1",
                SkinTextureSlots.SpecializationIcon, SkinLanguage.Text("专精图标通用覆盖（可选）", "Specialization icon override (optional)"));
            foreach (LoadGameTextureSlotSetting setting in LoadGamePageAdapter.TextureSlots)
                AddLoadGameTextureSlot(setting);
            foreach (CreditsTextureSlotSetting setting in CreditsPageAdapter.TextureSlots)
                creditsTextureSlots.Add(new TextureSlotBinding(setting.Slot,
                    DefineTextureSlot(config, setting.ConfigKey, SkinLanguage.Text("制作人员名单", "Credits"), setting.Label, "")));
            foreach (GameOptionsTextureSlotSetting setting in GameOptionsTextureAdapter.TextureSlots)
                gameOptionsTextureSlots.Add(new TextureSlotBinding(setting.Slot,
                    DefineTextureSlot(config, setting.ConfigKey, SkinLanguage.Text("游戏选项界面贴图", "Game options textures"), setting.Label, "")));
            mainMenuFontColor = config.Define(MainMenuFontColorKey, "", SkinLanguage.Text("高级功能", "Advanced"),
                () => SkinLanguage.Text("主界面字体颜色", "Main menu text color"),
                tooltip: () => SkinLanguage.Text("游戏默认主题的字体色固定。此项只覆盖当前主题；留空时采用该主题 skin_presets.json 的 colors.text。", "Overrides the current preset text color. Leave blank to use the preset color."));
            mainMenuPositionOffsets = config.Define(PositionAdjustmentController.MainConfigKey, "", SkinLanguage.Text("位置调整", "Position adjustment"),
                () => SkinLanguage.Text("主页面", "Main menu"), tooltip: () => SkinLanguage.Text("开始调整后左上角显示保存、取消和恢复默认；Shift+左右选择控件，方向键移动，Ctrl+方向键每次移动 10 个 UI 单位，Esc 取消。", "Adjust the position of controls on this screen. Use Shift+Left/Right to select a control and the arrow keys to move it."));
            specializationPositionOffsets = config.Define(PositionAdjustmentController.SpecializationConfigKey, "", SkinLanguage.Text("位置调整", "Position adjustment"),
                () => SkinLanguage.Text("新游戏专精页面", "New Game specialization"), tooltip: () => SkinLanguage.Text("左上角可保存、取消或恢复当前专精页默认位置；可分别调整页面标题、专精详情标题/标签/描述、初始拥有标题与条目列表、每个专精项、返回按钮、左右切换按钮和继续按钮；移动偏移按专精 ID 保存。", "Adjust the position of controls on this screen. Use Shift+Left/Right to select a control and the arrow keys to move it."));
            loadGamePositionOffsets = config.Define(PositionAdjustmentController.LoadGameConfigKey, "", SkinLanguage.Text("位置调整", "Position adjustment"),
                () => SkinLanguage.Text("载入游戏页面", "Load Game screen"), tooltip: () => SkinLanguage.Text("进入载入游戏页面后调整页头、存档卡片、历史记录、滚动条和对话框中的图像与文字；左上角可保存、取消或恢复默认。", "Adjust the position of controls on this screen. Use Shift+Left/Right to select a control and the arrow keys to move it."));
            creditsPositionOffsets = config.Define(PositionAdjustmentController.CreditsConfigKey, "", SkinLanguage.Text("位置调整", "Position adjustment"),
                () => SkinLanguage.Text("位置编辑", "Edit positions"), tooltip: () => SkinLanguage.Text("调整名单窗口、滚动控件、页头、分隔线、分节标题、开发团队角色/姓名与致谢文字；Shift+左右选择，方向键移动。", "Adjust the position of controls on this screen. Use Shift+Left/Right to select a control and the arrow keys to move it."));
            gameOptionsPositionOffsets = config.Define(PositionAdjustmentController.GameOptionsConfigKey, "", SkinLanguage.Text("位置调整", "Position adjustment"),
                () => SkinLanguage.Text("游戏选项界面", "Game options"), tooltip: () => SkinLanguage.Text("调整原生选项窗口、标题栏、关闭按钮、滚动区域/滚动条、分组标题、每项标签与控件、底部应用按钮的位置。", "Adjust the position of controls on this screen. Use Shift+Left/Right to select a control and the arrow keys to move it."));
            mainMenuTransparency = config.Define(TransparencyAdjustmentController.MainConfigKey, "", SkinLanguage.Text("透明度调整", "Opacity adjustment"),
                () => SkinLanguage.Text("主界面", "Main menu"), tooltip: () => SkinLanguage.Text("编辑主界面各控件的透明度；Shift+左右选择控件，上下调整，Ctrl+上下每次调整 10%。", "Adjust the opacity of controls on this screen. Use Shift+Left/Right to select a control and Up/Down to adjust it."));
            specializationTransparency = config.Define(TransparencyAdjustmentController.SpecializationConfigKey, "", SkinLanguage.Text("透明度调整", "Opacity adjustment"),
                () => SkinLanguage.Text("新游戏专精", "New Game specialization"), tooltip: () => SkinLanguage.Text("编辑新游戏专精页各控件的透明度；Shift+左右选择控件，上下调整，Ctrl+上下每次调整 10%。", "Adjust the opacity of controls on this screen. Use Shift+Left/Right to select a control and Up/Down to adjust it."));
            loadGameTransparency = config.Define(TransparencyAdjustmentController.LoadGameConfigKey, "", SkinLanguage.Text("透明度调整", "Opacity adjustment"),
                () => SkinLanguage.Text("载入游戏", "Load Game"), tooltip: () => SkinLanguage.Text("编辑载入游戏页中的页头、存档卡片、历史记录、滚动条和对话框图像及文字透明度；Shift+左右选择控件，上下调整。", "Adjust the opacity of controls on this screen. Use Shift+Left/Right to select a control and Up/Down to adjust it."));
            creditsTransparency = config.Define(TransparencyAdjustmentController.CreditsConfigKey, "", SkinLanguage.Text("透明度调整", "Opacity adjustment"),
                () => SkinLanguage.Text("透明度编辑", "Edit opacity"), tooltip: () => SkinLanguage.Text("调整制作人员名单页各图像、页头、团队条目与文字的透明度；Shift+左右选择，上下调整。", "Adjust the opacity of controls on this screen. Use Shift+Left/Right to select a control and Up/Down to adjust it."));
            gameOptionsTransparency = config.Define(TransparencyAdjustmentController.GameOptionsConfigKey, "", SkinLanguage.Text("透明度调整", "Opacity adjustment"),
                () => SkinLanguage.Text("游戏选项界面", "Game options"), tooltip: () => SkinLanguage.Text("调整原生选项窗口、标题栏、关闭按钮、滚动区域/滚动条、分组标题、每项标签与控件、底部应用按钮的透明度。", "Adjust the opacity of controls on this screen. Use Shift+Left/Right to select a control and Up/Down to adjust it."));
            skinAdjustments = config.Define(SkinAdjustmentStore.ConfigKey, "", "内部",
                () => "皮肤位置与透明度档案");
            skinAdjustmentsMigrated = config.Define(SkinAdjustmentStore.MigrationKey, false, "内部",
                () => "皮肤档案迁移状态");
            adjustmentStore.Initialize(skinAdjustments, skinAdjustmentsMigrated, SkinPresets.Chiikawa.Id,
                new[] { mainMenuPositionOffsets, specializationPositionOffsets, loadGamePositionOffsets, creditsPositionOffsets, gameOptionsPositionOffsets },
                new[] { mainMenuTransparency, specializationTransparency, loadGameTransparency, creditsTransparency, gameOptionsTransparency });
            mainMenuBranding.DefineConfiguration(config);
            skinSettingsMigrated = config.Define(SkinAdjustmentStore.SettingsMigrationKey, false, "内部",
                () => "主题外观配置迁移状态");
            adjustmentStore.MigrateSettings(skinSettingsMigrated, SkinPresets.Chiikawa.Id, GetThemeSettings());
            configReady = true;
        }

        private void ActivateConfiguration()
        {
            BindConfigurationEvents();
            positionAdjustment.Configure(mainMenuPositionOffsets, specializationPositionOffsets, loadGamePositionOffsets,
                creditsPositionOffsets, gameOptionsPositionOffsets, adjustmentStore, SkinService.CurrentTheme.Id);
            transparencyAdjustment.Configure(mainMenuTransparency, specializationTransparency, loadGameTransparency,
                creditsTransparency, gameOptionsTransparency, adjustmentStore, SkinService.CurrentTheme.Id);
            ApplyConfiguredSkin(selectedSkin.Value);
            mainMenuBranding.SettingChanged -= SaveThemeSetting;
            mainMenuBranding.SettingChanged += SaveThemeSetting;
            mainMenuBranding.Activate();
            LoadThemeSettings(SkinService.CurrentTheme.Id);
            ApplyTextureSettings();
        }

        private void BindConfigurationEvents()
        {
            if (configEventsBound)
                return;
            selectedSkin.OnChanged += ApplyConfiguredSkin;
            mainMenuFontColor.OnChanged += OnMainMenuFontColorChanged;
            BindTextureSlot(mainMenuBackgroundKey, SkinTextureSlots.MainMenuBackground);
            mainMenuNewGameButton.OnChanged += OnMainMenuNewGameButtonChanged;
            mainMenuLoadGameButton.OnChanged += OnMainMenuLoadGameButtonChanged;
            mainMenuOptionsButton.OnChanged += OnMainMenuOptionsButtonChanged;
            mainMenuCreditsButton.OnChanged += OnMainMenuCreditsButtonChanged;
            mainMenuModsButton.OnChanged += OnMainMenuModsButtonChanged;
            mainMenuQuitButton.OnChanged += OnMainMenuQuitButtonChanged;
            BindTextureSlots(specializationTextureSlots, specializationPage.ApplyToOpenPage);
            BindTextureSlots(fixedFixtureTextureSlots, RefreshFixedFixtureTextures);
            BindTextureSlots(loadGameTextureSlots, loadGamePage.Refresh);
            BindTextureSlots(creditsTextureSlots, creditsPage.ApplyToOpenPage);
            BindTextureSlots(gameOptionsTextureSlots, gameOptionsTextures.ApplyToOpenWindow);
            configEventsBound = true;
        }

        private void BindTextureSlots(List<TextureSlotBinding> bindings, Action refresh)
        {
            foreach (TextureSlotBinding binding in bindings)
            {
                TextureSlotBinding current = binding;
                current.Handler = value =>
                {
                    SaveThemeSetting(current.Entry.Key, value);
                    SkinService.SetTextureOverride(current.Slot, value);
                    refresh?.Invoke();
                };
                current.Entry.OnChanged += current.Handler;
            }
        }

        private void RefreshFixedFixtureTextures()
        {
            fixedFixtureEditor.RefreshTextures();
            StoreInventoryGridSkinAdapter.Apply();
        }

        private void UnbindConfigurationEvents()
        {
            if (!configEventsBound)
                return;
            selectedSkin.OnChanged -= ApplyConfiguredSkin;
            mainMenuFontColor.OnChanged -= OnMainMenuFontColorChanged;
            mainMenuBackgroundKey.OnChanged -= OnMainMenuBackgroundChanged;
            mainMenuNewGameButton.OnChanged -= OnMainMenuNewGameButtonChanged;
            mainMenuLoadGameButton.OnChanged -= OnMainMenuLoadGameButtonChanged;
            mainMenuOptionsButton.OnChanged -= OnMainMenuOptionsButtonChanged;
            mainMenuCreditsButton.OnChanged -= OnMainMenuCreditsButtonChanged;
            mainMenuModsButton.OnChanged -= OnMainMenuModsButtonChanged;
            mainMenuQuitButton.OnChanged -= OnMainMenuQuitButtonChanged;
            UnbindTextureSlots(specializationTextureSlots);
            UnbindTextureSlots(fixedFixtureTextureSlots);
            UnbindTextureSlots(loadGameTextureSlots);
            UnbindTextureSlots(creditsTextureSlots);
            UnbindTextureSlots(gameOptionsTextureSlots);
            configEventsBound = false;
        }

        private static void UnbindTextureSlots(List<TextureSlotBinding> bindings)
        {
            foreach (TextureSlotBinding binding in bindings)
            {
                if (binding.Handler == null)
                    continue;
                binding.Entry.OnChanged -= binding.Handler;
                binding.Handler = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            specializationStripRefreshPending = false;
            mainMenuBackground.ApplyToScene(scene);
            mainMenuButtons.ApplyToScene(scene);
            mainMenuBranding.ApplyToScene(scene);
            languageDropdown.ApplyToScene(scene);
            positionAdjustment.OnSceneLoaded(scene);
            transparencyAdjustment.OnSceneLoaded(scene);
            fixedFixtureEditor.OnSceneChanged(scene);
            StoreInventoryGridSkinAdapter.Apply();
        }

        private void ApplyFixturesAfterGameLoaded()
        {
            fixedFixtureEditor.ApplyLoadedScene();
            StoreInventoryGridSkinAdapter.Apply();
            ScheduleFixtureReapplyAfterFrame();
        }

        private void ApplyFixturesAfterStoreOpened()
        {
            fixedFixtureEditor.ApplyLoadedScene();
            StoreInventoryGridSkinAdapter.Apply();
            ScheduleFixtureReapplyAfterFrame();
        }

        private void ApplyFixturesAfterReturningStore()
        {
            fixedFixtureEditor.ApplyLoadedScene();
            StoreInventoryGridSkinAdapter.Apply();
            ScheduleFixtureReapplyAfterFrame();
        }

        private void ScheduleFixtureReapplyAfterFrame() =>
            ModHelper.StartCoroutine("skin_framework", ReapplyFixturesAfterFrame());

        private IEnumerator ReapplyFixturesAfterFrame()
        {
            yield return null;
            fixedFixtureEditor.ApplyLoadedScene();
        }

        private void OnThemeChanged(SkinTheme theme)
        {
            positionAdjustment.PrepareSkinChange();
            transparencyAdjustment.PrepareSkinChange();
            if (configApplyInProgress)
            {
                themeChangePending = true;
                return;
            }
            ApplyThemeRuntime(theme);
        }

        private void ApplyThemeRuntime(SkinTheme theme)
        {
            LoadThemeSettings(theme.Id);
            mainMenuBackground.ApplyToActiveScene();
            mainMenuButtons.ApplyToActiveScene();
            languageDropdown.ApplyToActiveScene();
            specializationPage.ApplyToOpenPage();
            loadGamePage.Refresh();
            creditsPage.ApplyToOpenPage();
            gameOptionsTextures.ApplyToOpenWindow();
            ApplyMainMenuFontColor(mainMenuFontColor.Value);
            positionAdjustment.SelectSkin(theme.Id);
            transparencyAdjustment.SelectSkin(theme.Id);
            fixedFixtureEditor.OnThemeChanged();
            StoreInventoryGridSkinAdapter.Apply();
        }

        internal static void ApplySpecializationPageSkin()
        {
            active?.specializationPage.ApplyToOpenPage();
            active?.positionAdjustment.ApplySpecializationPositions();
            active?.transparencyAdjustment.ApplySpecialization();
        }

        internal static void RestoreSpecializationPageSkin()
        {
            active?.transparencyAdjustment.RestoreSpecialization();
            active?.specializationPage.Restore();
        }

        internal static void BeginPositionAdjustment(PositionAdjustmentPage page, CustomUIWindow window) =>
            active?.positionAdjustment.Begin(page, window);

        internal static void TickPositionAdjustment()
        {
            active?.positionAdjustment.Tick();
            active?.transparencyAdjustment.Tick();
        }

        internal static void TickFixedFixtureEditor() => active?.fixedFixtureEditor.Tick();

        internal static UnityEngine.Sprite ApplyTrashcanSprite(GameItemElement item, UnityEngine.Sprite source) =>
            active == null ? source : active.fixedFixtureEditor.ApplyTrashcanSprite(item, source);

        internal static UnityEngine.Sprite ApplyDrainSprite(GameItemElement item, UnityEngine.Sprite source) =>
            active == null ? source : active.fixedFixtureEditor.ApplyDrainSprite(item, source);

        internal static UnityEngine.Sprite ApplyFaucetSprite(GameItemElement item, UnityEngine.Sprite source) =>
            active == null ? source : active.fixedFixtureEditor.ApplyFaucetSprite(item, source);

        internal static UnityEngine.Sprite ApplyDossierSprite(GameItemElement item, UnityEngine.Sprite source) =>
            active == null ? source : active.fixedFixtureEditor.ApplyDossierSprite(item, source);

        internal static UnityEngine.Sprite ApplyCassettePlayerSprite(GameItemElement item, UnityEngine.Sprite source) =>
            active == null ? source : active.fixedFixtureEditor.ApplyCassettePlayerSprite(item, source);

        internal static bool HasActiveRadioSkin() => active != null && active.fixedFixtureEditor.HasActiveRadioSkin();

        internal static void ApplyTrashcanLayout(GameItemElement item) =>
            active?.fixedFixtureEditor.ApplyTrashcanLayout(item);

        internal static void ApplyShowcaseSprite(UnityEngine.Sprite source) =>
            active?.fixedFixtureEditor.ApplyShowcaseSprite(source);

        internal static void ApplyPosterSprite(UnityEngine.Sprite source) =>
            active?.fixedFixtureEditor.ApplyPosterSprite(source);

        internal static void ApplyWall2Sprite(UnityEngine.Sprite source) =>
            active?.fixedFixtureEditor.ApplyWall2Sprite(source);


        internal static bool TrySelectPreset(SkinTheme theme)
        {
            if (active == null || !active.configReady)
                return false;
            active.selectedSkin.Set(theme.DisplayName);
            if (!ReferenceEquals(SkinService.CurrentTheme, theme))
                SkinService.Select(theme);
            return true;
        }

        internal static SkinAdjustmentProfile ExportPackageProfile(string id)
        {
            SkinAdjustmentProfile source = active.adjustmentStore.ExportProfile(id);
            var settings = new Dictionary<string, string>(StringComparer.Ordinal);
            var ownKeys = active.GetThemeSettings();
            foreach (var pair in source.settings)
                if (ownKeys.ContainsKey(pair.Key) || pair.Key == "external:skin_framework.fixed_fixtures:adjustments_v1")
                    settings.Add(pair.Key, pair.Value);
            return new SkinAdjustmentProfile
            {
                positions = source.positions,
                transparency = source.transparency,
                settings = settings
            };
        }

        internal static void ImportPackage(SkinPresets.PresetRecord record, SkinAdjustmentProfile profile,
            Action<string> writeAssets)
        {
            SkinPresets.ImportPackageRecord(record, active.log, writeAssets);
            active.adjustmentStore.ImportProfile(record.id, profile);
        }

        internal static bool RefreshPresetCatalog() =>
            active != null && active.configReady && SkinPresets.RefreshRuntime(active.log);

        internal static bool TryGetExternalThemeSetting(string ownerId, string settingId, out string value)
        {
            value = null;
            if (active == null || !active.configReady || string.IsNullOrWhiteSpace(ownerId) ||
                string.IsNullOrWhiteSpace(settingId))
                return false;
            string key = ExternalThemeSettingKey(ownerId, settingId);
            string skinId = SkinService.CurrentTheme.Id;
            if (!active.adjustmentStore.HasSetting(skinId, key))
                return false;
            value = active.adjustmentStore.GetSetting(skinId, key);
            return true;
        }

        internal static bool SetExternalThemeSetting(string ownerId, string settingId, string value)
        {
            if (active == null || !active.configReady || string.IsNullOrWhiteSpace(ownerId) ||
                string.IsNullOrWhiteSpace(settingId))
                return false;
            active.adjustmentStore.SaveSetting(SkinService.CurrentTheme.Id,
                ExternalThemeSettingKey(ownerId, settingId), value);
            return true;
        }

        private static string ExternalThemeSettingKey(string ownerId, string settingId) =>
            "external:" + ownerId.Trim() + ":" + settingId.Trim();

        internal static void BeginTransparencyAdjustment(PositionAdjustmentPage page, CustomUIWindow window) =>
            active?.transparencyAdjustment.Begin(page, window);

        internal static void OnGameOptionsOpened()
        {
            if (active == null || !active.configReady)
                return;
            active.gameOptionsTextures.ApplyToOpenWindow();
            active.positionAdjustment.ApplyGameOptionsPositions();
            active.transparencyAdjustment.ApplyGameOptions();
        }

        internal static void OnGameOptionsClosed()
        {
            if (active == null || !active.configReady)
                return;
            active.positionAdjustment.OnGameOptionsClosed();
            active.transparencyAdjustment.OnGameOptionsClosed();
            active.gameOptionsTextures.Restore();
        }

        internal static void OnLoadGameOpened()
        {
            if (active == null || !active.configReady)
                return;
            active.loadGamePage.ApplyToOpenPage();
            active.positionAdjustment.ApplyLoadGamePositions();
            active.transparencyAdjustment.ApplyLoadGame();
        }

        internal static void BeforeLoadGameRefresh()
        {
            if (active == null || !active.configReady)
                return;
            active.positionAdjustment.BeforeLoadGameRefresh();
            active.transparencyAdjustment.BeforeLoadGameRefresh();
        }

        internal static void OnLoadGameRefreshed()
        {
            if (active == null || !active.configReady)
                return;
            active.loadGamePage.ApplyToOpenPage();
            active.positionAdjustment.ApplyLoadGamePositions();
            active.transparencyAdjustment.OnLoadGameRefreshed();
        }

        internal static void OnLoadGameClosed()
        {
            if (active == null || !active.configReady)
                return;
            active.loadGamePage.Restore();
            active.positionAdjustment.OnLoadGameClosed();
            active.transparencyAdjustment.OnLoadGameClosed();
        }

        internal static void ApplyCreditsPageSkin()
        {
            if (active == null || !active.configReady) return;
            active.creditsPage.ApplyToOpenPage();
            active.positionAdjustment.ApplyCreditsPositions();
            active.transparencyAdjustment.ApplyCredits();
        }

        internal static void RestoreCreditsPageSkin()
        {
            if (active == null || !active.configReady) return;
            active.transparencyAdjustment.RestoreCredits();
            active.positionAdjustment.OnCreditsClosed();
            active.creditsPage.Restore();
        }

        internal static void ApplySpecializationPositions() => active?.positionAdjustment.ApplySpecializationPositions();

        internal static void BeforeSpecializationStripRebuilt()
        {
            active?.positionAdjustment.OnSpecializationStripRebuilding();
            active?.transparencyAdjustment.OnSpecializationStripRebuilding();
        }

        internal static void OnSpecializationStripRebuilt() => active?.ScheduleSpecializationStripRefresh();

        private void ScheduleSpecializationStripRefresh()
        {
            if (specializationStripRefreshPending)
                return;
            specializationStripRefreshPending = true;
            ModHelper.StartCoroutine("skin_framework", RefreshSpecializationStripAfterDestroy());
        }

        private IEnumerator RefreshSpecializationStripAfterDestroy()
        {
            yield return null;
            specializationStripRefreshPending = false;
            if (NewGameSpecializationUI.IsOpen)
            {
                specializationPage.ApplyToOpenPage();
                positionAdjustment.ApplySpecializationPositions();
                transparencyAdjustment.ApplySpecialization();
            }
        }

        private void OnMainMenuFontColorChanged(string value)
        {
            SaveThemeSetting(MainMenuFontColorKey, value);
            ApplyMainMenuFontColor(value);
        }

        private Dictionary<string, string> GetThemeSettings()
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ConfigEntry<string> entry in GetThemeStringEntries())
                values[entry.Key] = entry.Value ?? "";
            if (values[mainMenuBackgroundKey.Key] == "shared_main_menu_background")
                values[mainMenuBackgroundKey.Key] = "";
            values[mainMenuBranding.HideLogoEntry.Key] = mainMenuBranding.HideLogoEntry.Value.ToString();
            values[mainMenuBranding.HideVersionEntry.Key] = mainMenuBranding.HideVersionEntry.Value.ToString();
            values[mainMenuBranding.VersionTextEntry.Key] = mainMenuBranding.VersionTextEntry.Value ?? "";
            return values;
        }

        private IEnumerable<ConfigEntry<string>> GetThemeStringEntries()
        {
            yield return mainMenuBackgroundKey;
            yield return mainMenuNewGameButton;
            yield return mainMenuLoadGameButton;
            yield return mainMenuOptionsButton;
            yield return mainMenuCreditsButton;
            yield return mainMenuModsButton;
            yield return mainMenuQuitButton;
            yield return mainMenuFontColor;
            foreach (TextureSlotBinding binding in fixedFixtureTextureSlots) yield return binding.Entry;
            foreach (TextureSlotBinding binding in specializationTextureSlots) yield return binding.Entry;
            foreach (TextureSlotBinding binding in loadGameTextureSlots) yield return binding.Entry;
            foreach (TextureSlotBinding binding in creditsTextureSlots) yield return binding.Entry;
            foreach (TextureSlotBinding binding in gameOptionsTextureSlots) yield return binding.Entry;
        }

        private void LoadThemeSettings(string skinId)
        {
            foreach (ConfigEntry<string> entry in GetThemeStringEntries())
                entry.Set(adjustmentStore.HasSetting(skinId, entry.Key)
                    ? adjustmentStore.GetSetting(skinId, entry.Key)
                    : "");
            mainMenuBranding.HideLogoEntry.Set(ParseBoolSetting(skinId, mainMenuBranding.HideLogoEntry));
            mainMenuBranding.HideVersionEntry.Set(ParseBoolSetting(skinId, mainMenuBranding.HideVersionEntry));
            mainMenuBranding.VersionTextEntry.Set(adjustmentStore.GetSetting(skinId, mainMenuBranding.VersionTextEntry.Key) ?? "");
            mainMenuBranding.ApplyCurrentSettings();
            ApplyTextureSettings();
            ApplyMainMenuFontColor(mainMenuFontColor.Value);
        }

        private bool ParseBoolSetting(string skinId, ConfigEntry<bool> entry) =>
            bool.TryParse(adjustmentStore.GetSetting(skinId, entry.Key), out bool value) && value;

        private void SaveThemeSetting(string key, string value) =>
            adjustmentStore.SaveSetting(configApplyInProgress ? configApplyOwnerSkinId : SkinService.CurrentTheme.Id, key, value);

        internal static Dictionary<string, string> GetPendingPresetEdits(CustomUIWindow window)
        {
            var edits = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string key in active.GetThemeSettings().Keys)
            {
                ConfigEntry entry = active.config.Get(key);
                CustomUIElement field = window.Get(key);
                if (field == null) continue;
                string value = entry.BoxedValue is bool ? field.GetBool().ToString() : field.GetString();
                if (TextureKeyPreviewPatch.TryGetSlot(key, out _) && string.IsNullOrEmpty(value) &&
                    field.input?.placeholder is TMPro.TMP_Text placeholder && !string.IsNullOrEmpty(placeholder.text))
                    continue;
                if (!string.Equals(value, entry.BoxedValue?.ToString() ?? "", StringComparison.Ordinal))
                    edits.Add(key, value);
            }
            return edits;
        }

        internal static void DiscardPresetEdits(CustomUIWindow window)
        {
            active.RefreshThemeSettingsWidgets(window);
            var versionText = active.mainMenuBranding.VersionTextEntry;
            window.Get(versionText.Key)?.SetString(versionText.Value ?? "", false);
            foreach (ConfigEntry entry in active.config.Entries)
                if (TextureKeyPreviewPatch.TryGetSlot(entry.Key, out string slot))
                    TextureKeyPreviewPatch.RefreshField(window, active.config, entry, slot);
            var skin = window.Get(ConfigKey);
            skin.SetIndex(Array.IndexOf(active.selectedSkin.Choices, active.selectedSkin.Value), true);
        }

        internal static void SavePresetCopy(string name, Dictionary<string, string> edits)
        {
            SkinTheme source = SkinService.CurrentTheme;
            var profile = Newtonsoft.Json.JsonConvert.DeserializeObject<SkinAdjustmentProfile>(
                Newtonsoft.Json.JsonConvert.SerializeObject(ExportPackageProfile(source.Id)));
            foreach (var edit in edits)
                profile.settings[edit.Key] = edit.Value;
            string createdName = SkinPackageWindow.SaveAs(source, name, profile);
            ModReflect.Call(active.selectedSkin, "set_Choices", (object)SkinPresets.GetPresetNames());
            TrySelectPreset(SkinPresets.FindByName(createdName));
            ModConfigWindow.Open(ModLoader.Mods.Find(mod => mod.Manifest.ID == "skin_framework"));
        }

        internal static void BeginConfigApply()
        {
            if (active == null || !active.configReady)
                return;
            active.configApplyInProgress = true;
            active.configApplyOwnerSkinId = SkinService.CurrentTheme.Id;
        }

        internal static void RemovePresetProfile(string id) => active.adjustmentStore.RemoveProfile(id);

        internal static void AbortConfigApply()
        {
            active.configApplyInProgress = false;
            active.themeChangePending = false;
            active.configApplyOwnerSkinId = null;
        }

        internal static void EndConfigApply(CustomUIWindow window)
        {
            if (active == null || !active.configReady || !active.configApplyInProgress)
                return;
            active.configApplyInProgress = false;
            if (active.themeChangePending)
            {
                active.themeChangePending = false;
                active.ApplyThemeRuntime(SkinService.CurrentTheme);
                active.RefreshThemeSettingsWidgets(window);
            var versionText = active.mainMenuBranding.VersionTextEntry;
            window.Get(versionText.Key)?.SetString(versionText.Value ?? "", false);
            }
        }

        private void RefreshThemeSettingsWidgets(CustomUIWindow window)
        {
            PositionAdjustmentConfigPatch.RefreshThemeLocks(window);
            foreach (ConfigEntry<string> entry in GetThemeStringEntries())
                window.Get(entry.Key)?.SetString(entry.Value ?? "", false);
            window.Get(mainMenuBranding.HideLogoEntry.Key)?.toggle?.SetIsOnWithoutNotify(mainMenuBranding.HideLogoEntry.Value);
            window.Get(mainMenuBranding.HideVersionEntry.Key)?.toggle?.SetIsOnWithoutNotify(mainMenuBranding.HideVersionEntry.Value);
            MainMenuFontColorConfigPatch.RefreshSwatch(window);
        }

        private void ApplyMainMenuFontColor(string value)
        {
            if (SkinService.CurrentTheme.UsesGameDefaults)
            {
                mainMenuButtons.ClearTextColor();
                return;
            }

            if (MainMenuFontColor.TryParse(value, out UnityEngine.Color color))
            {
                mainMenuButtons.SetTextColor(color);
                return;
            }

            SkinColor themeText = SkinService.CurrentTheme.Text;
            mainMenuButtons.SetTextColor(new UnityEngine.Color(themeText.R, themeText.G, themeText.B, themeText.A));
            if (!string.IsNullOrWhiteSpace(value))
                log.Log($"主界面字体颜色值 '{value}' 无效，已使用当前主题颜色。");
        }

        private static ConfigEntry<string> DefineTextureSlot(ModConfig config, string key, string category,
            string label, string defaultValue)
        {
            string layoutHint = key == "gameplay_fixture_clientele_key_v1"
                ? SkinLanguage.Text(" 顾客信息屏四帧横条若每格左右各留 1 像素透明间隔，请让文件名（不含 .png）以 __pad1 结尾；其他名称按无间隔格式读取。", " Add __pad1 to the filename for a four-frame strip with one transparent pixel on each side of each frame.")
                : "";
            return config.Define(key, defaultValue, category, () => SkinLanguage.TextureLabel(label, key),
                tooltip: () => SkinLanguage.Text("单击输入框可编辑贴图名（可带 .png）；按“应用”保存，之后输入框恢复预览。自定义 PNG 放在启动日志给出的玩家自定义贴图目录对应子目录；玩家文件优先于包内主题图。留空跟随当前主题；输入 null 恢复游戏原版贴图；输入 False 隐藏对应图像组件。", "Enter a texture filename, with or without .png. Leave blank to follow the preset, use null for the original texture, or False to hide it. Click Apply to save.") + layoutHint);
        }

        private void BindTextureSlot(ConfigEntry<string> entry, string slot)
        {
            if (slot == SkinTextureSlots.MainMenuBackground) entry.OnChanged += OnMainMenuBackgroundChanged;
        }

        private void ApplyTextureSettings()
        {
            SkinService.SetTextureOverride(SkinTextureSlots.MainMenuBackground, mainMenuBackgroundKey.Value);
            SkinService.SetTextureOverride(SkinTextureSlots.MainMenuNewGameButton, mainMenuNewGameButton.Value);
            SkinService.SetTextureOverride(SkinTextureSlots.MainMenuLoadGameButton, mainMenuLoadGameButton.Value);
            SkinService.SetTextureOverride(SkinTextureSlots.MainMenuOptionsButton, mainMenuOptionsButton.Value);
            SkinService.SetTextureOverride(SkinTextureSlots.MainMenuCreditsButton, mainMenuCreditsButton.Value);
            SkinService.SetTextureOverride(SkinTextureSlots.MainMenuModsButton, mainMenuModsButton.Value);
            SkinService.SetTextureOverride(SkinTextureSlots.MainMenuQuitButton, mainMenuQuitButton.Value);
            foreach (TextureSlotBinding binding in specializationTextureSlots)
                SkinService.SetTextureOverride(binding.Slot, binding.Entry.Value);
            foreach (TextureSlotBinding binding in fixedFixtureTextureSlots)
                SkinService.SetTextureOverride(binding.Slot, binding.Entry.Value);
            foreach (TextureSlotBinding binding in loadGameTextureSlots)
                SkinService.SetTextureOverride(binding.Slot, binding.Entry.Value);
            foreach (TextureSlotBinding binding in creditsTextureSlots)
                SkinService.SetTextureOverride(binding.Slot, binding.Entry.Value);
            foreach (TextureSlotBinding binding in gameOptionsTextureSlots)
                SkinService.SetTextureOverride(binding.Slot, binding.Entry.Value);
        }

        private void OnMainMenuBackgroundChanged(string value)
        {
            SaveThemeSetting(mainMenuBackgroundKey.Key, value);
            SkinService.SetTextureOverride(SkinTextureSlots.MainMenuBackground, value);
            mainMenuBackground.ApplyToActiveScene();
        }

        private void OnMainMenuNewGameButtonChanged(string value) =>
            SetMainMenuButtonOverride(SkinTextureSlots.MainMenuNewGameButton, mainMenuNewGameButton.Key, value);

        private void OnMainMenuLoadGameButtonChanged(string value) =>
            SetMainMenuButtonOverride(SkinTextureSlots.MainMenuLoadGameButton, mainMenuLoadGameButton.Key, value);

        private void OnMainMenuOptionsButtonChanged(string value) =>
            SetMainMenuButtonOverride(SkinTextureSlots.MainMenuOptionsButton, mainMenuOptionsButton.Key, value);

        private void OnMainMenuCreditsButtonChanged(string value) =>
            SetMainMenuButtonOverride(SkinTextureSlots.MainMenuCreditsButton, mainMenuCreditsButton.Key, value);

        private void OnMainMenuModsButtonChanged(string value) =>
            SetMainMenuButtonOverride(SkinTextureSlots.MainMenuModsButton, mainMenuModsButton.Key, value);

        private void OnMainMenuQuitButtonChanged(string value) =>
            SetMainMenuButtonOverride(SkinTextureSlots.MainMenuQuitButton, mainMenuQuitButton.Key, value);

        private void SetMainMenuButtonOverride(string slot, string key, string value)
        {
            SaveThemeSetting(key, value);
            SkinService.SetTextureOverride(slot, value);
            mainMenuButtons.ApplyToActiveScene();
            transparencyAdjustment.ApplyMainMenu();
        }

        private void AddSpecializationTextureSlot(string key, string slot, string label)
        {
            ConfigEntry<string> entry = config.Define(key, "", SkinLanguage.Text("新游戏专精页", "New Game specialization"), () => SkinLanguage.TextureLabel(label, key),
                tooltip: () => SkinLanguage.Text("单击输入框可编辑贴图名（可带 .png）；按“应用”保存，之后输入框恢复预览。留空跟随主题 JSON；输入 null 恢复游戏原版贴图；输入 False 隐藏对应 Image 组件。", "Enter a texture filename, with or without .png. Leave blank to follow the preset, use null for the original texture, or False to hide it. Click Apply to save."));
            specializationTextureSlots.Add(new TextureSlotBinding(slot, entry));
        }

        private void AddFixedFixtureTextureSlot(string key, string slot, string label)
        {
            ConfigEntry<string> entry = DefineTextureSlot(config, key, SkinLanguage.Text("固定设施贴图", "Fixture textures"), label, "");
            fixedFixtureTextureSlots.Add(new TextureSlotBinding(slot, entry));
        }

        private void AddLoadGameTextureSlot(LoadGameTextureSlotSetting setting)
        {
            ConfigEntry<string> entry = DefineTextureSlot(config, setting.ConfigKey, SkinLanguage.Text("载入游戏页贴图", "Load Game textures"),
                setting.Label, "");
            loadGameTextureSlots.Add(new TextureSlotBinding(setting.Slot, entry));
        }

        private void ApplyConfiguredSkin(string displayName)
        {
            SkinTheme theme = SkinPresets.FindByName(displayName) ?? SkinPresets.GameDefault;
            SkinService.Select(theme);
            log.Log($"Active skin preset: {theme.Id}");
        }

        private static string[] GetPresetNames() => SkinPresets.GetPresetNames();

        private sealed class TextureSlotBinding
        {
            public readonly string Slot;
            public readonly ConfigEntry<string> Entry;
            public Action<string> Handler;

            public TextureSlotBinding(string slot, ConfigEntry<string> entry)
            {
                Slot = slot;
                Entry = entry;
            }
        }
    }
}
