using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>配置面板隐藏后，通过方向键调整菜单 RectTransform，并把偏移保存到 ModConfig。</summary>
    internal sealed partial class PositionAdjustmentController
    {
        internal const string MainConfigKey = "position_adjust_main_v1";
        internal const string SpecializationConfigKey = "position_adjust_specialization_v1";
        internal const string LoadGameConfigKey = "position_adjust_load_game_v1";
        internal const string CreditsConfigKey = "position_adjust_credits_v1";
        internal const string GameOptionsConfigKey = "position_adjust_game_options_v1";

        private readonly Dictionary<Transform, PositionAppliedOffset> applied =
            new Dictionary<Transform, PositionAppliedOffset>();
        private readonly List<PositionAdjustmentTarget> targets = new List<PositionAdjustmentTarget>();
        private ConfigEntry<string> mainConfig;
        private ConfigEntry<string> specializationConfig;
        private ConfigEntry<string> loadGameConfig;
        private ConfigEntry<string> creditsConfig;
        private ConfigEntry<string> gameOptionsConfig;
        private SkinAdjustmentStore adjustmentStore;
        private string skinId;
        private PositionAdjustmentDocument mainDocument;
        private PositionAdjustmentDocument specializationDocument;
        private PositionAdjustmentDocument loadGameDocument;
        private PositionAdjustmentDocument creditsDocument;
        private PositionAdjustmentDocument gameOptionsDocument;
        private PositionAdjustmentPage page;
        private int selectedIndex;
        private CustomUIWindow configWindow;
        private readonly PositionAdjustmentHighlight highlight = new PositionAdjustmentHighlight();
        private readonly PositionAdjustmentLayoutLease menuLayoutLease = new PositionAdjustmentLayoutLease();
        private readonly PositionAdjustmentLayoutSlotLease creditsLayoutLease = new PositionAdjustmentLayoutSlotLease();
        private readonly PositionAdjustmentLayoutSlotLease gameOptionsLayoutLease = new PositionAdjustmentLayoutSlotLease();
        private readonly PositionAdjustmentActionBar actionBar = new PositionAdjustmentActionBar();
        private PositionAdjustmentSession session;

        internal bool IsActive => page != PositionAdjustmentPage.None;

        internal void Configure(ConfigEntry<string> main, ConfigEntry<string> specialization,
            ConfigEntry<string> loadGame, ConfigEntry<string> credits, ConfigEntry<string> gameOptions,
            SkinAdjustmentStore store, string activeSkinId)
        {
            mainConfig = main;
            specializationConfig = specialization;
            loadGameConfig = loadGame;
            creditsConfig = credits;
            gameOptionsConfig = gameOptions;
            adjustmentStore = store;
            skinId = activeSkinId;
            LoadSkinDocuments(skinId);
            PrepareMainMenuLayout();
            ApplySaved(PositionAdjustmentPage.MainMenu);
            ApplySaved(PositionAdjustmentPage.Specialization);
            ApplyGameOptionsPositions();
        }

        internal void SelectSkin(string nextSkinId)
        {
            skinId = nextSkinId;
            LoadSkinDocuments(skinId);
            PrepareMainMenuLayout();
            ApplySaved(PositionAdjustmentPage.MainMenu);
            ApplySaved(PositionAdjustmentPage.Specialization);
            ApplySaved(PositionAdjustmentPage.LoadGame);
            ApplySaved(PositionAdjustmentPage.Credits);
            ApplyGameOptionsPositions();
        }

        internal void PrepareSkinChange()
        {
            if (IsActive)
                CancelSession(false);
            RestoreAppliedOffsets();
        }

        private void LoadSkinDocuments(string id)
        {
            mainDocument = adjustmentStore.GetPosition(id, "main");
            specializationDocument = adjustmentStore.GetPosition(id, "specialization");
            loadGameDocument = adjustmentStore.GetPosition(id, "load_game");
            creditsDocument = adjustmentStore.GetPosition(id, "credits");
            gameOptionsDocument = adjustmentStore.GetPosition(id, "game_options");
        }

        internal void Begin(PositionAdjustmentPage targetPage, CustomUIWindow window)
        {
            configWindow = window;
            configWindow.Hide();
            if (ModUIManager.Instance != null)
                ModUIManager.Instance.ClosePanel();

            if (targetPage == PositionAdjustmentPage.Specialization)
                NewGameSpecializationUI.Open();
            else if (targetPage == PositionAdjustmentPage.LoadGame)
                LoadGameUI.Open();
            else if (targetPage == PositionAdjustmentPage.Credits)
                CreditsUI.Open();
            else if (targetPage == PositionAdjustmentPage.GameOptions)
                GameOptionUIManager.Instance.OpenUI();

            page = targetPage;

            if (page == PositionAdjustmentPage.Credits)
                Canvas.ForceUpdateCanvases();

            if (page == PositionAdjustmentPage.GameOptions)
            {
                // OpenUI builds the native option hierarchy synchronously, but its
                // Canvas layout is normally calculated later in the frame. Settle it
                // before the slot lease snapshots the scroll container's geometry.
                Canvas.ForceUpdateCanvases();
                ConfigureGameOptionsPositionLayout(true);
            }

            ResetAppliedOffsets(PagePrefix(page));
            if (page == PositionAdjustmentPage.MainMenu)
                menuLayoutLease.PreservePositionsOutsideLayout(PositionAdjustmentTargets.Collect(page));
            if (page == PositionAdjustmentPage.Credits)
                creditsLayoutLease.PreservePositionsOutsideLayout(PositionAdjustmentTargets.Collect(page));
            ApplySaved(page);
            RefreshTargets();
            if (targets.Count == 0)
            {
                Debug.LogError("[Skin Framework] 位置调整未启动：当前页面没有找到可调整控件。");
                End(true);
                return;
            }
            session = new PositionAdjustmentSession(page, CurrentDocument(), targets);
            if (!actionBar.Open(SaveSession, CancelSession, RestoreDefaults))
            {
                Debug.LogError("[Skin Framework] 位置调整操作条无法创建，已取消调整。");
                End(true);
            }
        }

        internal void Tick()
        {
            if (!IsActive)
                return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSession();
                return;
            }

            if (targets.Count == 0)
                return;

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (shift && control && page == PositionAdjustmentPage.Specialization)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) TurnSpecializationPage(false);
                else if (Input.GetKeyDown(KeyCode.RightArrow)) TurnSpecializationPage(true);
                return;
            }
            if (shift)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) Select(selectedIndex - 1);
                else if (Input.GetKeyDown(KeyCode.RightArrow)) Select(selectedIndex + 1);
                return;
            }

            Vector2 delta = Vector2.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) delta.x = -1f;
            else if (Input.GetKeyDown(KeyCode.RightArrow)) delta.x = 1f;
            else if (Input.GetKeyDown(KeyCode.UpArrow)) delta.y = 1f;
            else if (Input.GetKeyDown(KeyCode.DownArrow)) delta.y = -1f;
            if (delta == Vector2.zero)
                return;

            Move(delta * (control ? 10f : 1f));
        }

        internal static void TurnSpecializationPage(bool next)
        {
            GameObject root = GameObject.Find("custom_window_new_game_specialization");
            NewGameSpecializationFrame frame = root.GetComponent<NewGameSpecializationFrame>();
            Button button = next ? frame.nextButton : frame.prevButton;
            string direction = next ? "next" : "previous";
            string firstSlotBefore = frame.stripSlots.GetChild(0).name;
            Debug.Log($"[Skin Framework] 专精分页快捷键已收到：direction={direction}，buttonInteractable={button.interactable}，firstSlot={firstSlotBefore}。");
            if (button.interactable)
            {
                button.onClick.Invoke();
                Debug.Log($"[Skin Framework] 专精分页按钮已触发：direction={direction}；等待槽位重建完成后刷新目标。");
            }
        }

        internal void ApplyMainMenuPositions() => ApplySaved(PositionAdjustmentPage.MainMenu);

        internal void ApplySpecializationPositions()
        {
            ApplySaved(PositionAdjustmentPage.Specialization);
            if (page == PositionAdjustmentPage.Specialization)
                RefreshTargets();
        }

        internal void ApplyLoadGamePositions()
        {
            ApplySaved(PositionAdjustmentPage.LoadGame);
            if (page == PositionAdjustmentPage.LoadGame)
                RefreshTargets();
        }

        internal void ApplyCreditsPositions()
        {
            bool required = page == PositionAdjustmentPage.Credits ||
                creditsDocument.offsets.Count > 0;
            if (required)
            {
                Canvas.ForceUpdateCanvases();
                creditsLayoutLease.PreservePositionsOutsideLayout(
                    PositionAdjustmentTargets.Collect(PositionAdjustmentPage.Credits));
            }
            ApplySaved(PositionAdjustmentPage.Credits);
            if (page == PositionAdjustmentPage.Credits)
                RefreshTargets();
        }

        internal void ApplyGameOptionsPositions()
        {
            bool required = page == PositionAdjustmentPage.GameOptions ||
                gameOptionsDocument.offsets.Count > 0;
            if (required)
                Canvas.ForceUpdateCanvases();
            ConfigureGameOptionsPositionLayout(required);
            ApplySaved(PositionAdjustmentPage.GameOptions);
            if (page == PositionAdjustmentPage.GameOptions)
                RefreshTargets();
        }

        private void ConfigureGameOptionsPositionLayout(bool required)
        {
            if (required)
                gameOptionsLayoutLease.PreservePositionsOutsideLayout(
                    PositionAdjustmentTargets.Collect(PositionAdjustmentPage.GameOptions));
            else
                gameOptionsLayoutLease.Restore();
        }

        internal void OnGameOptionsClosed()
        {
            if (page == PositionAdjustmentPage.GameOptions)
                CancelSession();
            else
            {
                ResetAppliedOffsets("game_options.");
                gameOptionsLayoutLease.Restore();
            }
        }

        internal void BeforeLoadGameRefresh() => ResetAppliedOffsets("load_game.");

        internal void OnLoadGameClosed()
        {
            if (page == PositionAdjustmentPage.LoadGame)
                CancelSession();
            else
                ResetAppliedOffsets("load_game.");
        }

        internal void OnCreditsClosed()
        {
            if (page == PositionAdjustmentPage.Credits)
                CancelSession();
            else
                ResetAppliedOffsets("credits.");
            creditsLayoutLease.Restore();
        }

        internal void OnSpecializationStripRebuilding()
        {
            if (page != PositionAdjustmentPage.Specialization)
                return;
            ClearOutline();
            targets.Clear();
            selectedIndex = 0;
        }

        internal void OnSceneLoaded(Scene scene)
        {
            if (IsActive)
                End(false);
            RestoreAppliedOffsets();
            gameOptionsLayoutLease.Restore();
            menuLayoutLease.Restore();
            creditsLayoutLease.Restore();
            applied.Clear();
            PrepareMainMenuLayout();
            ApplySaved(PositionAdjustmentPage.MainMenu);
            ApplySaved(PositionAdjustmentPage.Specialization);
        }

        internal void Dispose()
        {
            if (IsActive)
                End(false);
            highlight.Dispose();
            RestoreAppliedOffsets();
            gameOptionsLayoutLease.Restore();
            menuLayoutLease.Restore();
            creditsLayoutLease.Restore();
            mainConfig = null;
            specializationConfig = null;
            loadGameConfig = null;
            creditsConfig = null;
            gameOptionsConfig = null;
            adjustmentStore = null;
        }

        private void End(bool reopenConfiguration)
        {
            if (session != null)
                RollbackSession(reopenConfiguration);
            actionBar.Close();
            ClearOutline();
            PositionAdjustmentPage endedPage = page;
            CustomUIWindow restoreWindow = configWindow;
            page = PositionAdjustmentPage.None;
            targets.Clear();
            selectedIndex = 0;
            configWindow = null;
            creditsLayoutLease.Restore();

            if (reopenConfiguration && endedPage == PositionAdjustmentPage.GameOptions &&
                GameOptionUIManager.Instance != null)
                GameOptionUIManager.Instance.CloseUI();
            if (reopenConfiguration && endedPage == PositionAdjustmentPage.GameOptions)
                gameOptionsLayoutLease.Restore();

            if (!reopenConfiguration || MainMenuUIController.Instance == null)
                return;
            if (endedPage == PositionAdjustmentPage.Specialization)
                NewGameSpecializationUI.Close();
            if (endedPage == PositionAdjustmentPage.LoadGame && LoadGameUI.IsOpen)
                LoadGameUI.Close();
            if (endedPage == PositionAdjustmentPage.Credits && CreditsUI.IsOpen)
                CreditsUI.Close();
            if (ModUIManager.Instance != null)
                ModUIManager.Instance.OpenPanel();
            if (restoreWindow != null && restoreWindow.IsAlive)
            {
                restoreWindow.Get(MainConfigKey)?.SetString(mainConfig.Value, false);
                restoreWindow.Get(SpecializationConfigKey)?.SetString(specializationConfig.Value, false);
                restoreWindow.Get(LoadGameConfigKey)?.SetString(loadGameConfig.Value, false);
                restoreWindow.Get(CreditsConfigKey)?.SetString(creditsConfig.Value, false);
                restoreWindow.Get(GameOptionsConfigKey)?.SetString(gameOptionsConfig.Value, false);
                restoreWindow.Show();
            }
        }

        private void RefreshTargets()
        {
            List<PositionAdjustmentTarget> refreshed = PositionAdjustmentTargets.Collect(page);
            if (SameTargets(refreshed))
                return;

            string previousId = targets.Count > 0 && selectedIndex < targets.Count
                ? targets[selectedIndex].Id
                : null;
            ClearOutline();
            targets.Clear();
            targets.AddRange(refreshed);
            selectedIndex = 0;
            if (previousId != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].Id == previousId)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            if (IsActive && targets.Count > 0)
                Select(selectedIndex);
        }

        private bool SameTargets(List<PositionAdjustmentTarget> refreshed)
        {
            if (targets.Count != refreshed.Count)
                return false;
            for (int i = 0; i < targets.Count; i++)
                if (targets[i].Id != refreshed[i].Id || targets[i].Transform != refreshed[i].Transform)
                    return false;
            return true;
        }

        private void ApplySaved(PositionAdjustmentPage targetPage)
        {
            PositionAdjustmentDocument document = targetPage switch
            {
                PositionAdjustmentPage.MainMenu => mainDocument,
                PositionAdjustmentPage.Specialization => specializationDocument,
                PositionAdjustmentPage.LoadGame => loadGameDocument,
                PositionAdjustmentPage.Credits => creditsDocument,
                _ => gameOptionsDocument
            };
            PositionAdjustmentTargets.Apply(targetPage, document, applied);
        }

        private void PrepareMainMenuLayout()
        {
            if (MainMenuUIController.Instance == null)
                return;
            menuLayoutLease.PreservePositionsOutsideLayout(PositionAdjustmentTargets.Collect(PositionAdjustmentPage.MainMenu));
        }

        private void Select(int index)
        {
            if (targets.Count == 0)
                return;
            ClearOutline();
            selectedIndex = (index % targets.Count + targets.Count) % targets.Count;
            highlight.Apply(targets[selectedIndex]);
            string pageNavigation = page == PositionAdjustmentPage.Specialization
                ? "；Ctrl+Shift+左右切换职业页"
                : string.Empty;
            Debug.Log($"[Skin Framework] 位置调整选中：{targets[selectedIndex].Label}；Shift+左右切换，方向键移动，Ctrl+方向键每次移动 10 单位{pageNavigation}；左上角可保存、取消或恢复默认，Esc 取消。");
        }

        private void Move(Vector2 delta)
        {
            PositionAdjustmentTarget target = targets[selectedIndex];
            if (!target.CanMove)
                return;
            target.SetPosition(target.GetPosition() + delta);
            PositionAdjustmentDocument document = CurrentDocument();
            if (!document.offsets.TryGetValue(target.Id, out PositionAdjustmentOffset offset))
            {
                offset = new PositionAdjustmentOffset();
                document.offsets.Add(target.Id, offset);
            }
            offset.x += delta.x;
            offset.y += delta.y;
            if (applied.TryGetValue(target.Transform, out PositionAppliedOffset appliedOffset))
                applied[target.Transform] = new PositionAppliedOffset(target.Id, appliedOffset.Delta + delta, target.GetPosition());
        }

        private void ClearOutline() => highlight.Clear();

    }
}
