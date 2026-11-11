using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinFramework
{
    internal sealed class FixedFixtureAdjustment
    {
        public float x { get; set; }
        public float y { get; set; }
        public float alpha { get; set; } = 1f;
        public float contrast { get; set; } = 1f;
        public float brightness { get; set; }
        public float saturation { get; set; } = 1f;
    }

    internal sealed class FixedFixtureAdjustmentDocument
    {
        public int schemaVersion { get; set; } = 1;
        public float windowAlpha { get; set; } = 0.62f;
        public Dictionary<string, FixedFixtureAdjustment> fixtures { get; set; } =
            new Dictionary<string, FixedFixtureAdjustment>(StringComparer.Ordinal);
        public Dictionary<string, string> textureReferences { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);

        internal FixedFixtureAdjustmentDocument Copy() => Read(Serialize());
        internal string Serialize() => JsonConvert.SerializeObject(this);

        internal static FixedFixtureAdjustmentDocument Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new FixedFixtureAdjustmentDocument();
            try
            {
                FixedFixtureAdjustmentDocument document = JsonConvert.DeserializeObject<FixedFixtureAdjustmentDocument>(json);
                if (document == null || document.schemaVersion != 1 || document.fixtures == null ||
                    document.textureReferences == null ||
                    float.IsNaN(document.windowAlpha) || float.IsInfinity(document.windowAlpha) ||
                    document.windowAlpha < 0f || document.windowAlpha > 1f)
                    throw new JsonSerializationException("schemaVersion 1, fixtures and windowAlpha 0–1 are required");
                foreach (KeyValuePair<string, FixedFixtureAdjustment> pair in document.fixtures)
                {
                    FixedFixtureAdjustment value = pair.Value;
                    if (string.IsNullOrWhiteSpace(pair.Key) || value == null ||
                        float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                        float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                        float.IsNaN(value.alpha) || float.IsInfinity(value.alpha) || value.alpha < 0f || value.alpha > 1f ||
                        float.IsNaN(value.contrast) || float.IsInfinity(value.contrast) || value.contrast < 0f || value.contrast > 2f ||
                        float.IsNaN(value.brightness) || float.IsInfinity(value.brightness) || value.brightness < -0.5f || value.brightness > 0.5f ||
                        float.IsNaN(value.saturation) || value.saturation < 0f || value.saturation > 2f)
                        throw new JsonSerializationException("fixture adjustment contains an invalid value");
                }
                return document;
            }
            catch (JsonException exception)
            {
                Debug.LogError("[Skin Framework] 固定设施配置无效，使用默认设施与窗口设置；原配置文本保留：" + exception.Message);
                return new FixedFixtureAdjustmentDocument();
            }
        }
    }

    /// <summary>载入游戏中的固定设施列表、位置/透明度预览及按主题保存。</summary>
    internal sealed class FixedFixtureEditor
    {
        private const string OwnerId = "skin_framework.fixed_fixtures";
        private const string SettingId = "adjustments_v1";
        private readonly List<FixedFixtureTarget> targets = new List<FixedFixtureTarget>();
        private readonly HashSet<string> colorAdjustmentDiagnosticsLogged = new HashSet<string>(StringComparer.Ordinal);
        private readonly PositionAdjustmentHighlight highlight = new PositionAdjustmentHighlight();
        private FixedFixtureWindow window;
        private OverlayHandler blockedOverlay;
        private FixedFixtureTarget selected;
        private FixedFixtureAdjustmentDocument document = new FixedFixtureAdjustmentDocument();
        private FixedFixtureAdjustmentDocument initialDocument;
        private string search = string.Empty;
        private int categoryIndex;
        private int sceneHandle = -1;
        private bool sessionActive;

        internal bool IsOpen => window != null && window.IsAlive;

        internal void OnSceneChanged(Scene scene)
        {
            Cancel();
            RestoreBasePositionsAndColors();
            targets.Clear();
            selected = null;
            sceneHandle = -1;
        }

        internal void EnsureSceneApplied()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (sceneHandle == scene.handle || EmporiumEntry.Instance == null)
                return;
            RestoreBasePositionsAndColors();
            targets.Clear();
            targets.AddRange(FixedFixtureCatalog.Collect(scene));
            sceneHandle = scene.handle;
            document = ReadCurrentTheme();
            ApplyAll();
            Debug.Log("[Skin Framework] 固定设施已识别：" + targets.Count + " 个场景目标。");
        }

        internal void ApplyLoadedScene()
        {
            EnsureSceneApplied();
            ApplyAll();
        }

        internal bool HasActiveRadioSkin()
        {
            if (!SkinService.TryGetTexture(SkinTextureSlots.GameplayFixtureRadio, out _))
                return false;
            foreach (FixedFixtureTarget target in targets)
                if (target.Id == "--Physical Store--/Radio" && target.Renderer != null)
                    return true;
            return false;
        }

        internal void RefreshTextures()
        {
            EnsureSceneApplied();
            for (int i = 0; i < targets.Count; i++)
                targets[i].Dispose();
            ApplyAll();
            RefreshSelectionDetails();
        }

        internal void OnThemeChanged()
        {
            if (sessionActive)
                RestorePreview(initialDocument);
            highlight.Clear();
            document = ReadCurrentTheme();
            selected = null;
            if (IsOpen)
            {
                initialDocument = document.Copy();
                ApplyAll();
                window.SetTheme(SkinService.CurrentTheme, !SkinService.CurrentTheme.UsesGameDefaults);
                window.SetWindowAlpha(document.windowAlpha);
                window.SetSelection(null, new FixedFixtureAdjustment());
                window.RefreshTargets(targets, categoryIndex, search, null);
                RefreshSelectionDetails();
            }
            else
            {
                ApplyAll();
            }
        }

        internal void Tick()
        {
            if (!Input.anyKeyDown)
                return;
            EnsureSceneApplied();
            TickHotkey();
            TickEditingKeys();
        }

        internal Sprite ApplyTrashcanSprite(GameItemElement item, Sprite source)
        {
            EmporiumEntry shop = EmporiumEntry.Instance;
            if (shop == null || shop.trashcan != item || source == null)
                return source;

            EnsureSceneApplied();
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id != "--FixtureImages/Trashcan")
                    continue;
                if (!target.IsAlive)
                    return source;

                Sprite replacement = target.UpdateImageBaseSpritesAndGetTexture(
                    source, GetTrashcanTextureSlot(source.name));
                return replacement;
            }
            return source;
        }

        internal Sprite ApplyDrainSprite(GameItemElement item, Sprite source)
        {
            EmporiumEntry shop = EmporiumEntry.Instance;
            if (shop == null || shop.drain != item || source == null)
                return source;

            EnsureSceneApplied();
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id == "--FixtureImages/Drain")
                    return target.UpdateImageBaseSpritesAndGetTexture(source, SkinTextureSlots.GameplayFixtureDrain);
            }
            return source;
        }

        internal Sprite ApplyFaucetSprite(GameItemElement item, Sprite source)
        {
            EmporiumEntry shop = EmporiumEntry.Instance;
            if (shop == null || shop.faucet != item || source == null)
                return source;

            EnsureSceneApplied();
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id != "--FixtureImages/Faucet")
                    continue;

                return target.UpdateImageBaseSpritesAndGetTexture(source, GetFaucetTextureSlot(source.name));
            }
            return source;
        }

        internal Sprite ApplyDossierSprite(GameItemElement item, Sprite source)
        {
            EmporiumEntry shop = EmporiumEntry.Instance;
            if (shop == null || shop.dossier != item || source == null)
                return source;

            EnsureSceneApplied();
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id != "--FixtureImages/Dossier")
                    continue;

                return target.UpdateImageBaseSpritesAndGetTexture(source, GetDossierTextureSlot(source.name));
            }
            return source;
        }

        internal Sprite ApplyCassettePlayerSprite(GameItemElement item, Sprite source)
        {
            if (source == null || source.name != "cassette_player")
                return source;

            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id != "--FixtureImages/CassettePlayer")
                    continue;

                return target.UpdateImageBaseSpritesAndGetTexture(source,
                    SkinTextureSlots.GameplayFixtureCassettePlayer);
            }
            return source;
        }

        internal void ApplyTrashcanLayout(GameItemElement item)
        {
            EmporiumEntry shop = EmporiumEntry.Instance;
            if (shop == null || shop.trashcan != item)
                return;
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id != "--FixtureImages/Trashcan")
                    continue;
                if (!target.IsAlive)
                    return;
                FixedFixtureAdjustment adjustment = SkinService.CurrentTheme.UsesGameDefaults
                    ? new FixedFixtureAdjustment()
                    : document.fixtures.TryGetValue(target.Id, out FixedFixtureAdjustment saved)
                        ? saved
                        : new FixedFixtureAdjustment();
                target.Apply(new Vector2(adjustment.x, adjustment.y), adjustment.alpha);
                return;
            }
        }

        internal void ApplyShowcaseSprite(Sprite source)
        {
            EnsureSceneApplied();
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id != "--Physical Store--/showcase") continue;
                target.UpdateRendererBaseSprite(source);
                target.ApplyTexture(GetShowcaseTextureSlot(source.name));
                return;
            }
        }

        internal void ApplyPosterSprite(Sprite source)
        {
            EnsureSceneApplied();
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id != "--Physical Store--/NutruifruitPoster")
                    continue;
                target.UpdateRendererBaseSprite(source);
                target.ApplyTexture(GetPosterTextureSlot(source.name));
                return;
            }
        }

        internal void ApplyWall2Sprite(Sprite source)
        {
            EnsureSceneApplied();
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                if (target.Id != "--Physical Store--/EmporiumWall")
                    continue;
                target.UpdateRendererBaseSprite(source);
                target.ApplyTexture(SkinTextureSlots.GameplayFixtureWall2);
                return;
            }
        }


        internal void Dispose()
        {
            CloseWindowWithoutCallback();
            highlight.Dispose();
            RestoreBasePositionsAndColors();
            targets.Clear();
            selected = null;
            sceneHandle = -1;
        }

        internal void SelectCategory(int index)
        {
            categoryIndex = index;
            window?.RefreshTargets(targets, categoryIndex, search, selected);
        }

        internal void FilterTargets(string value)
        {
            search = value ?? string.Empty;
            window?.RefreshTargets(targets, categoryIndex, search, selected);
        }

        internal void SelectTarget(FixedFixtureTarget target)
        {
            highlight.Clear();
            selected = target;
            if (!target.IsMenuTarget)
                highlight.Apply(new PositionAdjustmentTarget(target.Id, target.Name, target.Transform));
            window.SetSelection(target, GetAdjustment(target.Id));
            window.RefreshTargets(targets, categoryIndex, search, selected);
            RefreshSelectionDetails();
        }

        internal void RefreshSelectionDetails()
        {
            if (!IsOpen)
                return;
            string slot = GetSelectedTextureSlot(selected);
            if (selected == null || !selected.IsAlive || string.IsNullOrEmpty(slot))
            {
                window.SetTextureReference(null, string.Empty, string.Empty, null, false,
                    selected == null ? SkinLanguage.Text("选择设施后查看和替换贴图", "Select a fixture to view or replace its texture.") : SkinLanguage.Text("该设施没有独立贴图槽", "This fixture has no separate texture slot."));
                return;
            }

            string reference = document.textureReferences.TryGetValue(slot, out string saved)
                ? saved : null;
            Texture2D texture = null;
            string resolvedKey = null;
            bool isHidden = string.Equals(reference, "false", StringComparison.OrdinalIgnoreCase);
            bool isOriginal = string.Equals(reference, "null", StringComparison.OrdinalIgnoreCase);
            bool hiddenByTheme = reference == null && SkinService.IsTextureDisabled(slot);
            bool loaded = !string.IsNullOrEmpty(reference) && !isHidden && !isOriginal
                ? SkinService.TryGetTextureByKey(slot, reference, out texture, out resolvedKey)
                : !isHidden && !isOriginal && !hiddenByTheme && SkinService.TryGetTexture(slot, out texture, out resolvedKey);
            string category = SkinService.GetTextureCategory(slot);
            string path = isHidden || hiddenByTheme ? SkinLanguage.Text("已隐藏（False）", "Hidden (False)")
                : isOriginal ? SkinLanguage.Text("游戏原版贴图（null）", "Original game texture (null)")
                : loaded
                ? SkinLanguage.Text("当前素材：", "Current texture: ") + resolvedKey + SkinLanguage.Text(".png　·　分类目录：", ".png · Folder: ") + category
                : selected.IsMenuTarget && string.IsNullOrEmpty(reference)
                    ? SkinLanguage.Text("未关联素材　·　资源分类目录：", "No texture assigned · Folder: ") + category
                : SkinService.CurrentTheme.UsesGameDefaults
                    ? SkinLanguage.Text("游戏原版贴图（当前主题为游戏默认）", "Original game texture")
                    : string.IsNullOrEmpty(reference) ? SkinLanguage.Text("此槽未设置素材，当前使用游戏原图", "No texture assigned. Using the game original.") : SkinLanguage.Text("素材未找到：", "Texture not found: ") + reference + ".png";
            string input = reference ?? (hiddenByTheme ? "false" : loaded ? resolvedKey : string.Empty);
            Sprite preview = selected.Sprite;
            bool editable = !SkinService.CurrentTheme.UsesGameDefaults;
            string hint = selected.IsMenuTarget
                ? SkinLanguage.Text("此关联对应菜单主背景 Image；false 隐藏背景图，子级文字和按钮仍保留。", "This slot controls the menu background. False hides it and keeps text and buttons visible.")
                : editable
                ? SkinLanguage.Text("输入同主题设施目录中的文件名（不含 .png）；实时预览，留空恢复主题默认。", "Enter the filename from this folder without .png. Leave blank to reset.")
                : SkinLanguage.Text("游戏默认主题锁定贴图编辑；切换到可编辑主题后可修改。", "Switch to an editable preset to replace textures.");
            window.SetTextureReference(slot, input, path, preview, editable, hint);
        }

        internal void RestoreSelectionAdjustmentValues()
        {
            if (!IsOpen || selected == null || !selected.IsAlive)
                return;
            FixedFixtureAdjustment adjustment = GetAdjustment(selected.Id);
            window.SetAlphaValue(adjustment.alpha);
            window.SetColorAdjustmentValues(adjustment.contrast, adjustment.brightness, adjustment.saturation);
        }

        internal void SetTextureReference(string value)
        {
            string slot = GetSelectedTextureSlot(selected);
            if (selected == null || !selected.IsAlive || string.IsNullOrEmpty(slot) ||
                SkinService.CurrentTheme.UsesGameDefaults)
                return;

            string key = (value ?? string.Empty).Trim();
            if (key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - 4);
            if (key.IndexOfAny(new[] { '/', '\\' }) >= 0)
            {
                window.SetTextureReferenceError(SkinLanguage.Text("仅填写文件名，不含目录和 .png", "Enter the filename without a folder or .png."));
                return;
            }
            bool isHidden = string.Equals(key, "false", StringComparison.OrdinalIgnoreCase);
            bool isOriginal = string.Equals(key, "null", StringComparison.OrdinalIgnoreCase);
            if (key.Length > 0 && !isHidden && !isOriginal &&
                !SkinService.TryGetTextureByKey(slot, key, out _, out _))
            {
                window.SetTextureReferenceError(SkinLanguage.Text("未找到素材；请确认文件位于当前主题的设施目录", "Texture not found. Check the copied folder."));
                return;
            }

            if (key.Length == 0)
                document.textureReferences.Remove(slot);
            else if (isHidden || isOriginal)
                document.textureReferences[slot] = key.ToLowerInvariant();
            else
                document.textureReferences[slot] = key;
            Apply(selected);
            window.RefreshTargets(targets, categoryIndex, search, selected);
            RefreshSelectionDetails();
        }

        internal void CyclePreset(int delta)
        {
            int index = 0;
            for (int i = 0; i < SkinPresets.All.Count; i++)
                if (SkinPresets.All[i].Id == SkinService.CurrentTheme.Id)
                {
                    index = i;
                    break;
                }
            int next = (index + delta + SkinPresets.All.Count) % SkinPresets.All.Count;
            SkinService.TrySelectPreset(SkinPresets.All[next].Id);
        }

        internal void SetX(float value)
        {
            if (selected == null || !selected.IsAlive || selected.IsMenuTarget)
                return;
            GetAdjustment(selected.Id).x = value;
            Apply(selected);
            window.SetPositionValues(value, GetAdjustment(selected.Id).y);
            highlight.Clear();
            highlight.Apply(new PositionAdjustmentTarget(selected.Id, selected.Name, selected.Transform));
        }

        internal void SetY(float value)
        {
            if (selected == null || !selected.IsAlive || selected.IsMenuTarget)
                return;
            GetAdjustment(selected.Id).y = value;
            Apply(selected);
            window.SetPositionValues(GetAdjustment(selected.Id).x, value);
            highlight.Clear();
            highlight.Apply(new PositionAdjustmentTarget(selected.Id, selected.Name, selected.Transform));
        }

        internal void SetAlpha(float value)
        {
            if (selected == null || !selected.IsAlive)
                return;
            if (!selected.IsMenuTarget)
                highlight.Clear();
            GetAdjustment(selected.Id).alpha = value;
            Apply(selected);
            window.SetAlphaValue(value);
            if (!selected.IsMenuTarget)
                highlight.Apply(new PositionAdjustmentTarget(selected.Id, selected.Name, selected.Transform));
        }

        internal void SetContrast(float value)
        {
            if (selected == null || !selected.IsAlive)
                return;
            GetAdjustment(selected.Id).contrast = value;
            Apply(selected);
            LogColorAdjustmentApplied(selected, GetAdjustment(selected.Id));
            window.SetColorAdjustmentValues(value, GetAdjustment(selected.Id).brightness, GetAdjustment(selected.Id).saturation);
            RefreshColorAdjustmentPreview();
        }

        internal void SetBrightness(float value)
        {
            if (selected == null || !selected.IsAlive)
                return;
            GetAdjustment(selected.Id).brightness = value;
            Apply(selected);
            LogColorAdjustmentApplied(selected, GetAdjustment(selected.Id));
            window.SetColorAdjustmentValues(GetAdjustment(selected.Id).contrast, value, GetAdjustment(selected.Id).saturation);
            RefreshColorAdjustmentPreview();
        }

        internal void SetSaturation(float value)
        {
            if (selected == null || !selected.IsAlive)
                return;
            FixedFixtureAdjustment adjustment = GetAdjustment(selected.Id);
            adjustment.saturation = value;
            Apply(selected);
            window.SetColorAdjustmentValues(adjustment.contrast, adjustment.brightness, value);
            RefreshColorAdjustmentPreview();
        }

        private void RefreshColorAdjustmentPreview()
        {

            RefreshSelectionDetails();
        }

        private void LogColorAdjustmentApplied(FixedFixtureTarget target, FixedFixtureAdjustment adjustment)
        {
            if (target == null || adjustment == null ||
                !colorAdjustmentDiagnosticsLogged.Add(target.Id))
                return;
            Sprite sprite = target.Sprite;
            Debug.Log("[Skin Framework] 单项颜色调整已应用：id=" + target.Id +
                ", contrast=" + adjustment.contrast.ToString("0.00") +
                ", brightness=" + adjustment.brightness.ToString("0.00") +
                ", sprite=" + (sprite == null ? "<missing>" : sprite.name) + ".");
        }

        internal void SetWindowAlpha(float value)
        {
            document.windowAlpha = value;
            window.SetWindowAlpha(value);
        }

        internal void ResetCurrentTheme()
        {
            highlight.Clear();
            document = new FixedFixtureAdjustmentDocument();
            ApplyAll();
            window.SetWindowAlpha(document.windowAlpha);
            window.SetSelection(selected, selected == null ? new FixedFixtureAdjustment() : GetAdjustment(selected.Id));
            RefreshSelectionDetails();
            if (selected != null && selected.IsAlive && !selected.IsMenuTarget)
                highlight.Apply(new PositionAdjustmentTarget(selected.Id, selected.Name, selected.Transform));
        }

        internal void SaveAndClose()
        {
            if (!SkinService.SetThemeSetting(OwnerId, SettingId, document.Serialize()))
            {
                Debug.LogError("[Skin Framework] 固定设施配置未保存：主题档案写入 API 返回失败。");
                return;
            }
            initialDocument = document.Copy();
            sessionActive = false;
            CloseWindowWithoutCallback();
            highlight.Clear();
        }

        internal void Cancel()
        {
            if (sessionActive)
                RestorePreview(initialDocument);
            sessionActive = false;
            highlight.Clear();
            CloseWindowWithoutCallback();
        }

        private void TickHotkey()
        {
            if (!Input.GetKeyDown(KeyCode.Space) ||
                !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) ||
                !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                return;
            if (IsOpen)
                Cancel();
            else
                Open();
        }

        private void TickEditingKeys()
        {
            if (!IsOpen)
                return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
                return;
            }
            if (selected == null || !selected.IsAlive || selected.IsMenuTarget)
                return;
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            float pixels = control ? 6f : 1f;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveX(-pixels);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) MoveX(pixels);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveY(-pixels);
            else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveY(pixels);
        }

        private void MoveX(float screenPixels)
        {
            FixedFixtureAdjustment adjustment = GetAdjustment(selected.Id);
            float localStep = FixedFixturePixelMovement.LocalUnitsForScreenPixels(
                selected.Transform, selected.Sprite, Vector3.right, Mathf.Abs(screenPixels)) * Mathf.Sign(screenPixels);
            SetX(Mathf.Clamp(adjustment.x + localStep, -400f, 400f));
        }

        private void MoveY(float screenPixels)
        {
            FixedFixtureAdjustment adjustment = GetAdjustment(selected.Id);
            float localStep = FixedFixturePixelMovement.LocalUnitsForScreenPixels(
                selected.Transform, selected.Sprite, Vector3.up, Mathf.Abs(screenPixels)) * Mathf.Sign(screenPixels);
            SetY(Mathf.Clamp(adjustment.y + localStep, -400f, 400f));
        }

        private void Open()
        {
            EnsureSceneApplied();
            if (targets.Count == 0)
            {
                Debug.LogWarning("[Skin Framework] 固定设施面板未打开：当前场景没有命中已映射的固定设施。");
                return;
            }
            document = ReadCurrentTheme();
            initialDocument = document.Copy();
            ApplyAll();
            sessionActive = true;
            window = new FixedFixtureWindow(this, targets, search, categoryIndex, selected,
                selected == null ? new FixedFixtureAdjustment() : GetAdjustment(selected.Id), document.windowAlpha);
            blockedOverlay = OverlayHandler.current;
            blockedOverlay.BlockMouse();
        }

        private void CloseWindowWithoutCallback()
        {
            FixedFixtureWindow closing = window;
            window = null;
            if (closing != null)
                closing.CloseWithoutCallback();
            OverlayHandler overlay = blockedOverlay;
            blockedOverlay = null;
            if (overlay != null)
                overlay.UnblockMouse();
        }

        private FixedFixtureAdjustmentDocument ReadCurrentTheme() =>
            SkinService.TryGetThemeSetting(OwnerId, SettingId, out string json)
                ? FixedFixtureAdjustmentDocument.Read(json)
                : new FixedFixtureAdjustmentDocument();

        private FixedFixtureAdjustment GetAdjustment(string id)
        {
            if (!document.fixtures.TryGetValue(id, out FixedFixtureAdjustment value))
            {
                value = new FixedFixtureAdjustment();
                document.fixtures.Add(id, value);
            }
            return value;
        }

        private void ApplyAll()
        {
            for (int i = 0; i < targets.Count; i++)
                Apply(targets[i]);
        }

        private void Apply(FixedFixtureTarget target)
        {
            if (!target.IsAlive)
                return;
            string selectedSlot = GetSelectedTextureSlot(target);
            string textureReference = GetTextureReference(selectedSlot);
            bool useOriginal = string.Equals(textureReference, "null", StringComparison.OrdinalIgnoreCase);
            bool hideTexture = string.Equals(textureReference, "false", StringComparison.OrdinalIgnoreCase);
            if (useOriginal)
                target.ApplyOriginalTexture();
            else if (target.Id == "--Interactable/ShutterButton")
                target.ApplyButtonTexture(SkinTextureSlots.GameplayFixtureShutterButton,
                    SkinTextureSlots.GameplayFixtureShutterButtonPressed,
                    textureReference);
            else if (target.Id == "--Interactable/Bell")
                target.ApplyButtonTexture(SkinTextureSlots.GameplayFixtureBell,
                    SkinTextureSlots.GameplayFixtureBellPressed,
                    textureReference);
            else if (target.Id == "--FixtureImages/Trashcan")
                target.ApplyImageTextures(sprite => GetTrashcanTextureSlot(sprite == null ? null : sprite.name));
            else if (target.Id == "--FixtureImages/Drain")
                target.ApplyImageTextures(_ => SkinTextureSlots.GameplayFixtureDrain);
            else if (target.Id == "--FixtureImages/Faucet")
                target.ApplyImageTextures(sprite => GetFaucetTextureSlot(sprite == null ? null : sprite.name));
            else if (target.Id == "--FixtureImages/Dossier")
                target.ApplyImageTextures(sprite => GetDossierTextureSlot(sprite == null ? null : sprite.name));
            else if (target.Id == "--FixtureImages/CassettePlayer")
                target.ApplyImageTextures(_ => SkinTextureSlots.GameplayFixtureCassettePlayer);
            else if (target.Id == "--Physical Store--/showcase")
                target.ApplyTexture(GetShowcaseTextureSlot(target.RendererBaseSprite.name));
            else if (target.Id == "--Physical Store--/NutruifruitPoster")
                target.ApplyTexture(GetPosterTextureSlot(target.RendererBaseSprite.name));
            else if (target.Id == "--Physical Store--/EmporiumWall")
                target.ApplyTexture(GetWallTextureSlot(target.RendererBaseSprite.name));
            else
                target.ApplyTexture(GetTextureSlot(target.Id));
            if (!useOriginal && !string.IsNullOrEmpty(selectedSlot) && !string.IsNullOrEmpty(textureReference) &&
                !hideTexture && target.Id != "--Interactable/ShutterButton" && target.Id != "--Interactable/Bell")
                target.ApplyTexture(selectedSlot, textureReference);
            FixedFixtureAdjustment value = SkinService.CurrentTheme.UsesGameDefaults
                ? new FixedFixtureAdjustment()
                : document.fixtures.TryGetValue(target.Id, out FixedFixtureAdjustment saved)
                    ? saved
                    : new FixedFixtureAdjustment();
            float alpha = hideTexture ? 0f : target.Id == "--FixtureImages/CassettePlayer" && HasActiveRadioSkin()
                ? 0f : value.alpha;
            string textureSlot = selectedSlot;
            bool hasProfileTextureReference = !string.IsNullOrEmpty(textureSlot) &&
                document.textureReferences.ContainsKey(textureSlot);
            if (!hasProfileTextureReference && !string.IsNullOrEmpty(textureSlot) &&
                SkinService.IsTextureDisabled(textureSlot))
                alpha = 0f;
            target.Apply(new Vector2(value.x, value.y), alpha);
            target.ApplyColorAdjustment(value.contrast, value.brightness, value.saturation);
        }

        private void RestorePreview(FixedFixtureAdjustmentDocument snapshot)
        {
            document = snapshot == null ? new FixedFixtureAdjustmentDocument() : snapshot.Copy();
            ApplyAll();
        }

        private void RestoreBasePositionsAndColors()
        {
            for (int i = 0; i < targets.Count; i++)
            {
                FixedFixtureTarget target = targets[i];
                target.Dispose();
            }
        }

        private static string GetTextureSlot(string targetId)
        {
            if (targetId == "--Menu/Calendar")
                return SkinTextureSlots.GameplayCalendarMenu;
            if (targetId == "--Menu/NegotiationTablet")
                return SkinTextureSlots.GameplayNegotiationTabletMenu;
            if (targetId == "--Menu/Phone")
                return SkinTextureSlots.GameplayPhoneMenu;
            if (targetId == "--Physical Store--/wires")
                return SkinTextureSlots.GameplayFixtureWallWires;
            if (targetId == "--Physical Store--/shutterFrameTop")
                return SkinTextureSlots.GameplayFixtureWindowTop;
            if (targetId == "--Physical Store--/ShutterDoor")
                return SkinTextureSlots.GameplayFixtureShutterDoor;
            if (targetId == "--Physical Store--/shutterFrame")
                return SkinTextureSlots.GameplayFixtureWindowFrame;
            if (targetId == "--Physical Store--/Radio")
                return SkinTextureSlots.GameplayFixtureRadio;
            if (targetId == "--Interactable/Recorder")
                return SkinTextureSlots.GameplayFixtureRecorder;
            if (targetId == "--Interactable/ClienteleDisplay")
                return SkinTextureSlots.GameplayFixtureClientele;
            if (targetId == "--Interactable/NegociationTablet")
                return SkinTextureSlots.GameplayFixtureNegociationTablet;
            if (targetId == "--Physical Store--/NutruifruitPoster")
                return SkinTextureSlots.GameplayFixturePoster;
            if (targetId == "--Interactable/Door")
                return SkinTextureSlots.GameplayFixtureDoor;
            if (targetId == "--Physical Store--/Door Lock")
                return SkinTextureSlots.GameplayFixtureDoorLock;
            if (targetId == "--Interactable/feedback_box")
                return SkinTextureSlots.GameplayFixtureReputationBox;
            if (targetId == "--Physical Store--/Desk")
                return SkinTextureSlots.GameplayFixtureDesk;
            if (targetId == "--Physical Store--/Table Wires")
                return SkinTextureSlots.GameplayFixtureTableWires;
            if (targetId == "--Interactable/PC/connecting wire")
                return SkinTextureSlots.GameplayFixtureConnectingWire;
            if (targetId == "--Physical Store--/Calendar_Wire")
                return SkinTextureSlots.GameplayFixtureCalendarWire;
            if (targetId == "--Interactable/Calendar")
                return SkinTextureSlots.GameplayFixtureCalendar;
            if (targetId == "--Interactable/transferButton")
                return SkinTextureSlots.GameplayFixtureTransferButton;
            if (targetId == "--Interactable/dismissButton")
                return SkinTextureSlots.GameplayFixtureDismissButton;
            if (targetId == "--Interactable/SecButton")
                return SkinTextureSlots.GameplayFixtureSecurityButton;
            if (targetId == "--Interactable/CreditDisplay")
                return SkinTextureSlots.GameplayFixtureCreditDisplay;
            if (targetId == "--Interactable/ClientDisplay")
                return SkinTextureSlots.GameplayFixtureClientDisplay;
            if (targetId == "--Interactable/wall_phone")
                return SkinTextureSlots.GameplayFixturePhone;
            if (targetId == "--Interactable/SleepingBag")
                return SkinTextureSlots.GameplayFixtureSleepingBag;
            if (targetId == "--Interactable/_Arm")
                return SkinTextureSlots.GameplayFixtureOneHand;
            if (targetId == "--Interactable/_Shotgun")
                return SkinTextureSlots.GameplayFixtureShotgunShadow;
            if (targetId == "--Interactable/_Shotgun/GameObject")
                return SkinTextureSlots.GameplayFixtureShotgunShadowOnly;
            if (targetId == "--FixtureImages/ShutterBlocker")
                return SkinTextureSlots.GameplayFixtureShutterBlocker;
            return null;
        }

        private string GetTextureReference(string slot) =>
            !string.IsNullOrEmpty(slot) && document.textureReferences.TryGetValue(slot, out string value)
                ? value : null;

        private static string GetSelectedTextureSlot(FixedFixtureTarget target)
        {
            if (target == null)
                return null;
            if (target.Id == "--Interactable/ShutterButton")
                return SkinTextureSlots.GameplayFixtureShutterButton;
            if (target.Id == "--Interactable/Bell")
                return SkinTextureSlots.GameplayFixtureBell;
            Sprite source = target.BaseSprite;
            if (target.Id == "--Physical Store--/showcase")
                return GetShowcaseTextureSlot(source == null ? null : source.name);
            if (target.Id == "--Physical Store--/NutruifruitPoster")
                return GetPosterTextureSlot(source == null ? null : source.name);
            if (target.Id == "--Physical Store--/EmporiumWall")
                return GetWallTextureSlot(source == null ? null : source.name);
            if (target.Id == "--FixtureImages/Trashcan")
                return GetTrashcanTextureSlot(source == null ? null : source.name);
            if (target.Id == "--FixtureImages/Faucet")
                return GetFaucetTextureSlot(source == null ? null : source.name);
            if (target.Id == "--FixtureImages/Dossier")
                return GetDossierTextureSlot(source == null ? null : source.name);
            if (target.Id == "--FixtureImages/Drain")
                return SkinTextureSlots.GameplayFixtureDrain;
            if (target.Id == "--FixtureImages/CassettePlayer")
                return SkinTextureSlots.GameplayFixtureCassettePlayer;
            if (target.Id == "--Physical Store--/shutterFrame" && source != null && source.name == "window")
                return SkinTextureSlots.GameplayFixtureWindowFrame;
            return GetTextureSlot(target.Id);
        }

        private static string GetTrashcanTextureSlot(string spriteName)
        {
            switch (spriteName)
            {
                case "trashcan0": return SkinTextureSlots.GameplayFixtureTrashcan0;
                case "trashcan25": return SkinTextureSlots.GameplayFixtureTrashcan25;
                case "trashcan50": return SkinTextureSlots.GameplayFixtureTrashcan50;
                case "trashcan75": return SkinTextureSlots.GameplayFixtureTrashcan75;
                case "trashcan100": return SkinTextureSlots.GameplayFixtureTrashcan100;
                default: return null;
            }
        }

        private static string GetFaucetTextureSlot(string spriteName) =>
            spriteName == "faucet1" ? SkinTextureSlots.GameplayFixtureFaucet1 : null;

        private static string GetDossierTextureSlot(string spriteName)
        {
            switch (spriteName)
            {
                case "dossier_0": return SkinTextureSlots.GameplayFixtureDossier0;
                case "dossier_1": return SkinTextureSlots.GameplayFixtureDossier1;
                case "dossier_2": return SkinTextureSlots.GameplayFixtureDossier2;
                case "dossier_3": return SkinTextureSlots.GameplayFixtureDossier3;
                default: return null;
            }
        }

        private static string GetShowcaseTextureSlot(string spriteName)
        {
            switch (spriteName)
            {
                case "display_case": return SkinTextureSlots.GameplayFixtureShowcase;
                case "display_case_medium": return SkinTextureSlots.GameplayFixtureShowcaseMedium;
                case "display_case_large": return SkinTextureSlots.GameplayFixtureShowcaseLarge;
                default: return null;
            }
        }

        private static string GetPosterTextureSlot(string spriteName)
        {
            if (spriteName == "poster_down")
                return SkinTextureSlots.GameplayFixturePosterPulled;
            if (spriteName == "poster" || spriteName == "poster_no_shadow")
                return SkinTextureSlots.GameplayFixturePoster;
            return null;
        }

        private static string GetWallTextureSlot(string spriteName) =>
            spriteName == "wall2" ? SkinTextureSlots.GameplayFixtureWall2 : null;
    }
}
