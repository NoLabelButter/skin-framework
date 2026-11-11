using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinFramework
{
    internal sealed class TransparencyAdjustmentDocument
    {
        public int schemaVersion { get; set; } = 1;
        public Dictionary<string, float> values { get; set; } =
            new Dictionary<string, float>(StringComparer.Ordinal);

        public string Serialize() => JsonConvert.SerializeObject(this);

        public static TransparencyAdjustmentDocument Read(string json, string key)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new TransparencyAdjustmentDocument();
            try
            {
                TransparencyAdjustmentDocument document = JsonConvert.DeserializeObject<TransparencyAdjustmentDocument>(json);
                if (document == null || document.schemaVersion != 1 || document.values == null)
                {
                    Debug.LogError($"[Skin Framework] 配置 {key} 的透明度数据版本或结构无效，使用默认透明度；原配置文本保留。");
                    return new TransparencyAdjustmentDocument();
                }
                foreach (KeyValuePair<string, float> pair in document.values)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || float.IsNaN(pair.Value) ||
                        float.IsInfinity(pair.Value) || pair.Value < 0f || pair.Value > 1f)
                    {
                        Debug.LogError($"[Skin Framework] 配置 {key} 含无效透明度，使用默认透明度；原配置文本保留。");
                        return new TransparencyAdjustmentDocument();
                    }
                }
                return document;
            }
            catch (JsonException exception)
            {
                Debug.LogError($"[Skin Framework] 配置 {key} 的透明度 JSON 无法解析，使用默认透明度；原配置文本保留：{exception.Message}");
                return new TransparencyAdjustmentDocument();
            }
        }
    }

    internal sealed class TransparencyAdjustmentController
    {
        internal const string MainConfigKey = "transparency_adjust_main_v1";
        internal const string SpecializationConfigKey = "transparency_adjust_specialization_v1";
        internal const string LoadGameConfigKey = "transparency_adjust_load_game_v1";
        internal const string CreditsConfigKey = "transparency_adjust_credits_v1";
        internal const string GameOptionsConfigKey = "transparency_adjust_game_options_v1";
        private const float Step = 0.05f;
        private const float LargeStep = 0.10f;

        private readonly Dictionary<Transform, CanvasGroupLease> applied =
            new Dictionary<Transform, CanvasGroupLease>();
        private readonly List<PositionAdjustmentTarget> targets = new List<PositionAdjustmentTarget>();
        private readonly PositionAdjustmentHighlight highlight = new PositionAdjustmentHighlight();
        private readonly PositionAdjustmentActionBar actionBar = new PositionAdjustmentActionBar();
        private ConfigEntry<string> mainConfig;
        private ConfigEntry<string> specializationConfig;
        private ConfigEntry<string> loadGameConfig;
        private ConfigEntry<string> creditsConfig;
        private ConfigEntry<string> gameOptionsConfig;
        private SkinAdjustmentStore adjustmentStore;
        private string skinId;
        private TransparencyAdjustmentDocument mainDocument;
        private TransparencyAdjustmentDocument specializationDocument;
        private TransparencyAdjustmentDocument loadGameDocument;
        private TransparencyAdjustmentDocument creditsDocument;
        private TransparencyAdjustmentDocument gameOptionsDocument;
        private TransparencyAdjustmentDocument initialDocument;
        private PositionAdjustmentPage page;
        private CustomUIWindow configWindow;
        private int selectedIndex;

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
            ApplyPage(PositionAdjustmentPage.MainMenu);
            ApplyPage(PositionAdjustmentPage.Specialization);
            ApplyPage(PositionAdjustmentPage.GameOptions);
        }

        internal void SelectSkin(string nextSkinId)
        {
            skinId = nextSkinId;
            LoadSkinDocuments(skinId);
            ApplyPage(PositionAdjustmentPage.MainMenu);
            ApplyPage(PositionAdjustmentPage.Specialization);
            ApplyPage(PositionAdjustmentPage.LoadGame);
            ApplyPage(PositionAdjustmentPage.Credits);
            ApplyPage(PositionAdjustmentPage.GameOptions);
        }

        internal void PrepareSkinChange()
        {
            if (IsActive)
                CancelSession(false);
            ResetAppliedPage("");
        }

        private void LoadSkinDocuments(string id)
        {
            mainDocument = adjustmentStore.GetTransparency(id, "main");
            specializationDocument = adjustmentStore.GetTransparency(id, "specialization");
            loadGameDocument = adjustmentStore.GetTransparency(id, "load_game");
            creditsDocument = adjustmentStore.GetTransparency(id, "credits");
            gameOptionsDocument = adjustmentStore.GetTransparency(id, "game_options");
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
            Debug.Log($"[Skin Framework] 透明度调整入口：page={PageName()}，scene={SceneManager.GetActiveScene().name}。");
            ApplyPage(page);
            RefreshTargets();
            if (targets.Count == 0)
            {
                if (page == PositionAdjustmentPage.Specialization)
                {
                    GameObject root = GameObject.Find("custom_window_new_game_specialization");
                    NewGameSpecializationFrame frame = root == null
                        ? null
                        : root.GetComponent<NewGameSpecializationFrame>();
                    Debug.LogError($"[Skin Framework] 透明度调整未启动：专精页目标为空；isOpen={NewGameSpecializationUI.IsOpen}，rootFound={root != null}，frameFound={frame != null}，slots={frame?.stripSlots?.childCount ?? -1}，header={frame?.headerTitle != null}，prev={frame?.prevButton != null}，next={frame?.nextButton != null}，back={frame?.backButton != null}，continue={frame?.continueButton != null}。");
                }
                else
                {
                    Debug.LogError("[Skin Framework] 透明度调整未启动：当前页面没有找到可调整控件。");
                }
                End(true);
                return;
            }
            Debug.Log($"[Skin Framework] 透明度调整已启动：page={PageName()}，targets={targets.Count}。");
            initialDocument = TransparencyAdjustmentDocument.Read(CurrentDocument().Serialize(), CurrentConfigKey());
            if (!actionBar.Open("透明度调整", "保存本次透明度调整并返回配置", "撤销本次调整，恢复进入时的透明度",
                "恢复当前页面全部控件的原始透明度；可继续调整后保存", SaveSession, CancelSession,
                RestoreDefaults))
            {
                Debug.LogError("[Skin Framework] 透明度调整操作条无法创建，已取消调整。");
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
                if (Input.GetKeyDown(KeyCode.LeftArrow)) PositionAdjustmentController.TurnSpecializationPage(false);
                else if (Input.GetKeyDown(KeyCode.RightArrow)) PositionAdjustmentController.TurnSpecializationPage(true);
                return;
            }
            if (shift)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) Select(selectedIndex - 1);
                else if (Input.GetKeyDown(KeyCode.RightArrow)) Select(selectedIndex + 1);
                return;
            }

            float delta = 0f;
            if (Input.GetKeyDown(KeyCode.UpArrow)) delta = 1f;
            else if (Input.GetKeyDown(KeyCode.DownArrow)) delta = -1f;
            if (delta == 0f)
                return;

            ChangeSelectedOpacity(delta * (control ? LargeStep : Step));
        }

        internal void OnSceneLoaded(Scene scene)
        {
            if (IsActive)
                CancelSession(false);
            ResetAppliedPage("");
            ApplyPage(PositionAdjustmentPage.MainMenu);
        }

        internal void ApplyMainMenu() => ApplyPage(PositionAdjustmentPage.MainMenu);

        internal void ApplySpecialization()
        {
            ApplyPage(PositionAdjustmentPage.Specialization);
            if (page == PositionAdjustmentPage.Specialization)
                RefreshTargets();
        }

        internal void ApplyLoadGame()
        {
            ApplyPage(PositionAdjustmentPage.LoadGame);
            if (page == PositionAdjustmentPage.LoadGame)
                RefreshTargets();
        }

        internal void ApplyCredits()
        {
            ApplyPage(PositionAdjustmentPage.Credits);
            if (page == PositionAdjustmentPage.Credits)
                RefreshTargets();
        }

        internal void ApplyGameOptions()
        {
            ApplyPage(PositionAdjustmentPage.GameOptions);
            if (page == PositionAdjustmentPage.GameOptions)
                RefreshTargets();
        }

        internal void OnGameOptionsClosed()
        {
            if (page == PositionAdjustmentPage.GameOptions)
                CancelSession();
            else
                ResetAppliedPage("game_options.");
        }

        internal void RestoreCredits()
        {
            if (page == PositionAdjustmentPage.Credits && !CreditsUI.IsOpen)
            {
                CancelSession(false);
                return;
            }
            ResetAppliedPage("credits.");
        }

        internal void BeforeLoadGameRefresh() => ResetAppliedPage("load_game.");

        internal void OnLoadGameRefreshed() => ApplyLoadGame();

        internal void OnLoadGameClosed()
        {
            if (page == PositionAdjustmentPage.LoadGame)
                CancelSession();
            else
                ResetAppliedPage("load_game.");
        }

        internal void OnCreditsClosed()
        {
            if (page == PositionAdjustmentPage.Credits)
                CancelSession();
            else
                ResetAppliedPage("credits.");
        }

        internal void OnSpecializationStripRebuilding()
        {
            if (page != PositionAdjustmentPage.Specialization)
                return;
            highlight.Clear();
            targets.Clear();
            selectedIndex = 0;
        }

        internal void RestoreSpecialization()
        {
            if (page == PositionAdjustmentPage.Specialization && !NewGameSpecializationUI.IsOpen)
            {
                CancelSession(false);
                return;
            }
            ResetAppliedPage("specialization.");
        }

        internal void Dispose()
        {
            if (IsActive)
                CancelSession(false);
            highlight.Dispose();
            RestoreAllApplied();
            mainConfig = null;
            specializationConfig = null;
            loadGameConfig = null;
            creditsConfig = null;
            gameOptionsConfig = null;
            adjustmentStore = null;
        }

        private void SaveSession()
        {
            if (initialDocument == null)
                return;
            adjustmentStore.SaveTransparency(skinId, StorePageKey(page), CurrentDocument());
            Debug.Log($"[Skin Framework] 透明度调整：已保存{PageName()}控件透明度。");
            initialDocument = null;
            End(true);
        }

        private void CancelSession() => CancelSession(true);

        private void CancelSession(bool reopenConfiguration)
        {
            if (initialDocument != null)
            {
                SetCurrentDocument(initialDocument);
                initialDocument = null;
                ApplyPage(page);
                Debug.Log($"[Skin Framework] 透明度调整：已取消{PageName()}调整。");
            }
            End(reopenConfiguration);
        }

        private void RestoreDefaults()
        {
            if (initialDocument == null)
                return;
            CurrentDocument().values.Clear();
            ResetAppliedPage(PagePrefix(page));
            Debug.Log($"[Skin Framework] 透明度调整：已恢复{PageName()}默认透明度；点击保存后生效，取消可撤销。");
        }

        private void ChangeSelectedOpacity(float delta)
        {
            PositionAdjustmentTarget target = targets[selectedIndex];
            TransparencyAdjustmentDocument document = CurrentDocument();
            float current = document.values.TryGetValue(target.Id, out float saved) ? saved : 1f;
            float next = Mathf.Clamp01(current + delta);
            if (next >= 0.9999f)
                document.values.Remove(target.Id);
            else
                document.values[target.Id] = next;
            ApplyTarget(target, next);
            Debug.Log($"[Skin Framework] 透明度调整：{target.Label} = {Mathf.RoundToInt(next * 100f)}%（相对原始透明度）。");
        }

        private void Select(int index)
        {
            if (targets.Count == 0)
                return;
            highlight.Clear();
            selectedIndex = (index % targets.Count + targets.Count) % targets.Count;
            PositionAdjustmentTarget target = targets[selectedIndex];
            highlight.Apply(target);
            float opacity = CurrentDocument().values.TryGetValue(target.Id, out float saved) ? saved : 1f;
            Debug.Log($"[Skin Framework] 透明度调整选中：{target.Label}；当前 {Mathf.RoundToInt(opacity * 100f)}%；Shift+左右切换，上下调整，Ctrl+上下每次调整 10%。");
        }

        private void RefreshTargets()
        {
            List<PositionAdjustmentTarget> refreshed = PositionAdjustmentTargets.Collect(page);
            if (SameTargets(refreshed))
                return;

            string previousId = targets.Count > 0 && selectedIndex < targets.Count
                ? targets[selectedIndex].Id
                : null;
            highlight.Clear();
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

        private void ApplyPage(PositionAdjustmentPage targetPage)
        {
            string prefix = PagePrefix(targetPage);
            ResetAppliedPage(prefix);
            TransparencyAdjustmentDocument document = GetDocument(targetPage);
            List<PositionAdjustmentTarget> currentTargets = PositionAdjustmentTargets.Collect(targetPage);
            for (int i = 0; i < currentTargets.Count; i++)
            {
                PositionAdjustmentTarget target = currentTargets[i];
                float opacity = document.values.TryGetValue(target.Id, out float saved) ? saved : 1f;
                if (opacity < 0.9999f)
                    ApplyTarget(target, opacity);
            }
        }

        private void ApplyTarget(PositionAdjustmentTarget target, float opacity)
        {
            if (opacity >= 0.9999f)
            {
                RestoreTarget(target.Transform);
                highlight.UpdateSpriteRendererAlpha(target.SpriteRenderer);
                return;
            }

            if (!applied.TryGetValue(target.Transform, out CanvasGroupLease lease))
            {
                if ((target.Id.StartsWith("credits.", StringComparison.Ordinal) ||
                    target.Id.StartsWith("game_options.", StringComparison.Ordinal)) && target.Graphic != null)
                {
                    lease = new CanvasGroupLease(target.Id, target.Graphic, target.Graphic.color);
                }
                else if (target.SpriteRenderer != null)
                {
                    lease = new CanvasGroupLease(target.Id, target.SpriteRenderer, target.SpriteRenderer.color);
                }
                else
                {
                    CanvasGroup group = target.Transform.GetComponent<CanvasGroup>();
                    bool created = group == null;
                    if (created)
                        group = target.Transform.gameObject.AddComponent<CanvasGroup>();
                    lease = new CanvasGroupLease(target.Id, group, group.alpha, created);
                }
                applied.Add(target.Transform, lease);
            }
            lease.Apply(opacity);
            highlight.UpdateSpriteRendererAlpha(target.SpriteRenderer);
        }

        private void RestoreTarget(Transform transform)
        {
            if (!applied.TryGetValue(transform, out CanvasGroupLease lease))
                return;
            lease.ResetValue();
        }

        private void ResetAppliedPage(string prefix)
        {
            List<Transform> remove = new List<Transform>();
            foreach (KeyValuePair<Transform, CanvasGroupLease> pair in applied)
            {
                if (!pair.Value.Id.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (pair.Key == null || pair.Value.IsDestroyed)
                    remove.Add(pair.Key);
                else
                    pair.Value.ResetValue();
            }
            for (int i = 0; i < remove.Count; i++)
                applied.Remove(remove[i]);
        }

        private void RestoreAllApplied()
        {
            foreach (CanvasGroupLease lease in applied.Values)
                lease.Release();
            applied.Clear();
        }

        private void OnMainConfigChanged(string json) => OnConfigChanged(PositionAdjustmentPage.MainMenu, json);

        private void OnSpecializationConfigChanged(string json) => OnConfigChanged(PositionAdjustmentPage.Specialization, json);
        private void OnLoadGameConfigChanged(string json) => OnConfigChanged(PositionAdjustmentPage.LoadGame, json);
        private void OnCreditsConfigChanged(string json) => OnConfigChanged(PositionAdjustmentPage.Credits, json);

        private void OnConfigChanged(PositionAdjustmentPage changedPage, string json)
        {
            TransparencyAdjustmentDocument current = GetDocument(changedPage);
            if (current.Serialize() == json)
                return;
            SetDocument(changedPage, TransparencyAdjustmentDocument.Read(json, ConfigKey(changedPage)));
            ApplyPage(changedPage);
        }

        private void End(bool reopenConfiguration)
        {
            actionBar.Close();
            highlight.Clear();
            PositionAdjustmentPage endedPage = page;
            CustomUIWindow restoreWindow = configWindow;
            page = PositionAdjustmentPage.None;
            targets.Clear();
            selectedIndex = 0;
            configWindow = null;
            if (reopenConfiguration && endedPage == PositionAdjustmentPage.GameOptions &&
                GameOptionUIManager.Instance != null)
                GameOptionUIManager.Instance.CloseUI();
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

        private TransparencyAdjustmentDocument CurrentDocument() => GetDocument(page);

        private string CurrentConfigKey() => ConfigKey(page);

        private void SetCurrentDocument(TransparencyAdjustmentDocument document) => SetDocument(page, document);

        private TransparencyAdjustmentDocument GetDocument(PositionAdjustmentPage targetPage) => targetPage switch
        {
            PositionAdjustmentPage.MainMenu => mainDocument,
            PositionAdjustmentPage.Specialization => specializationDocument,
            PositionAdjustmentPage.LoadGame => loadGameDocument,
            PositionAdjustmentPage.Credits => creditsDocument,
            _ => gameOptionsDocument
        };

        private void SetDocument(PositionAdjustmentPage targetPage, TransparencyAdjustmentDocument document)
        {
            if (targetPage == PositionAdjustmentPage.MainMenu)
                mainDocument = document;
            else if (targetPage == PositionAdjustmentPage.Specialization)
                specializationDocument = document;
            else if (targetPage == PositionAdjustmentPage.LoadGame)
                loadGameDocument = document;
            else if (targetPage == PositionAdjustmentPage.Credits)
                creditsDocument = document;
            else
                gameOptionsDocument = document;
        }

        private static string PagePrefix(PositionAdjustmentPage targetPage) => targetPage switch
        {
            PositionAdjustmentPage.MainMenu => "main.",
            PositionAdjustmentPage.Specialization => "specialization.",
            PositionAdjustmentPage.LoadGame => "load_game.",
            PositionAdjustmentPage.Credits => "credits.",
            _ => "game_options."
        };

        private static string ConfigKey(PositionAdjustmentPage targetPage) => targetPage switch
        {
            PositionAdjustmentPage.MainMenu => MainConfigKey,
            PositionAdjustmentPage.Specialization => SpecializationConfigKey,
            PositionAdjustmentPage.LoadGame => LoadGameConfigKey,
            PositionAdjustmentPage.Credits => CreditsConfigKey,
            _ => GameOptionsConfigKey
        };

        private static string StorePageKey(PositionAdjustmentPage targetPage) => targetPage switch
        {
            PositionAdjustmentPage.MainMenu => "main",
            PositionAdjustmentPage.Specialization => "specialization",
            PositionAdjustmentPage.LoadGame => "load_game",
            PositionAdjustmentPage.Credits => "credits",
            _ => "game_options"
        };

        private string PageName() => page switch
        {
            PositionAdjustmentPage.MainMenu => "主界面",
            PositionAdjustmentPage.Specialization => "新游戏专精页面",
            PositionAdjustmentPage.LoadGame => "载入游戏页面",
            PositionAdjustmentPage.Credits => "制作人员名单",
            _ => "游戏选项界面"
        };

        private sealed class CanvasGroupLease
        {
            internal readonly string Id;
            internal readonly CanvasGroup Group;
            internal readonly Graphic Graphic;
            internal readonly SpriteRenderer Renderer;
            internal readonly float OriginalAlpha;
            internal readonly Color OriginalColor;
            internal readonly bool Created;

            internal CanvasGroupLease(string id, CanvasGroup group, float originalAlpha, bool created)
            {
                Id = id;
                Group = group;
                OriginalAlpha = originalAlpha;
                Created = created;
            }

            internal CanvasGroupLease(string id, SpriteRenderer renderer, Color originalColor)
            {
                Id = id;
                Renderer = renderer;
                OriginalColor = originalColor;
                OriginalAlpha = originalColor.a;
            }

            internal CanvasGroupLease(string id, Graphic graphic, Color originalColor)
            {
                Id = id;
                Graphic = graphic;
                OriginalColor = originalColor;
                OriginalAlpha = originalColor.a;
            }

            internal bool IsDestroyed => Group == null && Renderer == null && Graphic == null;

            internal void Apply(float opacity)
            {
                if (Group != null)
                    Group.alpha = OriginalAlpha * opacity;
                else if (Graphic != null)
                {
                    Color current = Graphic.color;
                    Graphic.color = new Color(current.r, current.g, current.b, OriginalAlpha * opacity);
                }
                else if (Renderer != null)
                {
                    Color current = Renderer.color;
                    Renderer.color = new Color(current.r, current.g, current.b, OriginalAlpha * opacity);
                }
            }

            internal void ResetValue()
            {
                if (Group != null)
                    Group.alpha = OriginalAlpha;
                if (Graphic != null)
                    Graphic.color = OriginalColor;
                if (Renderer != null)
                    Renderer.color = OriginalColor;
            }

            internal void Release()
            {
                if (Group != null)
                {
                    if (Created)
                        UnityEngine.Object.Destroy(Group);
                    else
                        Group.alpha = OriginalAlpha;
                }
                if (Graphic != null)
                    Graphic.color = OriginalColor;
                if (Renderer != null)
                    Renderer.color = OriginalColor;
            }
        }
    }
}
