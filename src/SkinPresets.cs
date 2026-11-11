using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SkinFramework
{
    /// <summary>合并随包主题与玩家在 ModData 中维护的主题预设。</summary>
    public static class SkinPresets
    {
        internal const string UserPresetFilePath = "Presets/skin_presets.json";
        private const string ThemeFolderReadmeName = "README.txt";
        private static readonly SkinTheme fallback = new SkinTheme(
            "classic", "游戏默认", true,
            C(0.12f, 0.12f, 0.12f), C(0.20f, 0.20f, 0.20f),
            C(0.93f, 0.91f, 0.84f), C(0.62f, 0.62f, 0.60f), C(0.36f, 0.62f, 0.72f),
            C(0.06f, 0.09f, 0.11f), C(0.38f, 0.38f, 0.36f), C(0.93f, 0.91f, 0.84f));
        private static ReadOnlyCollection<SkinTheme> all = new ReadOnlyCollection<SkinTheme>(new[] { fallback });

        // Retained as public fields for callers compiled against the original API.
        public static SkinTheme Classic = fallback;
        public static SkinTheme NebulaBlue = fallback;
        public static SkinTheme AmberMarket = fallback;
        public static SkinTheme GameDefault { get; private set; } = fallback;
        public static SkinTheme Chiikawa { get; private set; } = fallback;
        public static ReadOnlyCollection<SkinTheme> All => all;
        private static PresetRecord[] bundledRecords = Array.Empty<PresetRecord>();
        private static List<PresetRecord> userRecords = new List<PresetRecord>();
        private static bool userStoreWritable = true;

        internal static void Load(string json, ModLog log)
        {
            string userJson = null;
            bool hasUserFile;
            bool userFileAccess = true;
            try
            {
                hasUserFile = ModHelper.ModDataFileExists("skin_framework", UserPresetFilePath);
                if (hasUserFile)
                    userJson = ModHelper.ReadModDataFileText("skin_framework", UserPresetFilePath);
            }
            catch (Exception error) when (IsFileError(error))
            {
                userFileAccess = false;
                log.Log("无法读取用户预设文件，已保留随包预设；新建预设不可用：" + error.Message);
                hasUserFile = false;
            }

            PresetFile packaged;
            try
            {
                packaged = ParsePresetFile(json, "packaged skin_presets.json");
            }
            catch (Exception error) when (error is FormatException || error is JsonException)
            {
                LoadCore(null, log);
                UseFallback(log, error.Message);
                userStoreWritable = false;
                return;
            }

            bundledRecords = packaged.presets;
            userRecords = new List<PresetRecord>();
            userStoreWritable = userFileAccess;
            if (hasUserFile && userFileAccess)
            {
                try
                {
                    PresetFile custom = ParsePresetFile(userJson, "user skin_presets.json", allowEmpty: true);
                    userRecords.AddRange(custom.presets);
                }
                catch (Exception error) when (error is FormatException || error is JsonException)
                {
                    userStoreWritable = false;
                    log.Log("用户预设 JSON 无效，已保留随包预设；修复 " + UserPresetFilePath + " 后可继续新建：" + error.Message);
                }
            }

            if (userStoreWritable && userRecords.Count > 0)
                packaged.presets = packaged.presets.Concat(userRecords).ToArray();

            string merged = JsonConvert.SerializeObject(packaged);
            if (!LoadCore(merged, log))
            {
                log.Log("合并用户预设失败，已回退到随包预设。");
                userRecords.Clear();
                userStoreWritable = false;
                LoadCore(JsonConvert.SerializeObject(new PresetFile { schemaVersion = 1, presets = bundledRecords }), log);
            }
        }

        private static PresetFile ParsePresetFile(string json, string source, bool allowEmpty = false)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException(source + " is missing or empty");
            PresetFile file = JsonConvert.DeserializeObject<PresetFile>(json);
            if (file == null || file.schemaVersion != 1 || file.presets == null || (!allowEmpty && file.presets.Length == 0))
                throw new FormatException(source + " requires schemaVersion 1 and a presets array");
            return file;
        }

        private static bool LoadCore(string json, ModLog log, bool validateOnly = false)
        {
            try
            {
                PresetFile file = ParsePresetFile(json, "skin_presets.json");

                List<SkinTheme> loaded = new List<SkinTheme>();
                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
                foreach (PresetRecord record in file.presets)
                {
                    if (record == null || string.IsNullOrWhiteSpace(record.id) || string.IsNullOrWhiteSpace(record.name))
                        throw new FormatException("each preset requires a non-empty id and name");
                    record.id = record.id.Trim();
                    record.name = record.name.Trim();
                    if (!IsValidId(record.id))
                        throw new FormatException("preset ids may contain only English letters, digits, '_' and '-'");
                    if (!ids.Add(record.id) || !names.Add(record.name))
                        throw new FormatException("preset ids and names must be unique");

                    Dictionary<string, string> textures = new Dictionary<string, string>(StringComparer.Ordinal);
                    HashSet<string> disabledTextures = new HashSet<string>(StringComparer.Ordinal);
                    if (record.textures != null)
                    {
                        foreach (TextureRecord texture in record.textures)
                        {
                            if (texture == null || string.IsNullOrWhiteSpace(texture.slot) || texture.key == null)
                                throw new FormatException("texture entries require a slot and a file key or false");
                            texture.slot = texture.slot.Trim();

                            string key = null;
                            bool disabled = false;
                            if (texture.key.Type == JTokenType.Boolean)
                            {
                                if (!texture.key.Value<bool>())
                                    disabled = true;
                                else
                                    throw new FormatException("texture key only accepts false as a boolean value");
                            }
                            else if (texture.key.Type == JTokenType.String)
                            {
                                key = texture.key.Value<string>()?.Trim();
                                disabled = string.Equals(key, "false", StringComparison.OrdinalIgnoreCase);
                            }
                            else
                            {
                                throw new FormatException("texture key must be a file key or false");
                            }

                            if (disabledTextures.Contains(texture.slot) || textures.ContainsKey(texture.slot))
                                throw new FormatException("texture slots must be unique within each preset");
                            if (disabled)
                            {
                                disabledTextures.Add(texture.slot);
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(new[] { '/', '\\' }) >= 0)
                                    throw new FormatException("texture file keys must be non-empty and cannot contain a path");
                                textures.Add(texture.slot, key);
                            }
                        }
                    }

                    loaded.Add(new SkinTheme(record.id.Trim(), record.name.Trim(), record.useGameDefaults,
                        ParseColor(record.colors?.background, "background"), ParseColor(record.colors?.surface, "surface"),
                        ParseColor(record.colors?.text, "text"), ParseColor(record.colors?.mutedText, "mutedText"),
                        ParseColor(record.colors?.accent, "accent"), ParseColor(record.colors?.accentText, "accentText"),
                        ParseColor(record.colors?.border, "border"), ParseColor(record.colors?.iconTint, "iconTint"),
                        textures, disabledTextures, record.useSharedTextureFallbacks));
                }

                SkinTheme gameDefault = loaded.FirstOrDefault(theme => theme.Id == "classic")
                    ?? throw new FormatException("preset id 'classic' is required for the game default");
                if (!gameDefault.UsesGameDefaults)
                    throw new FormatException("preset id 'classic' must set useGameDefaults to true");
                if (validateOnly) return true;
                all = new ReadOnlyCollection<SkinTheme>(loaded);
                GameDefault = gameDefault;
                Chiikawa = FindById("chiikawa") ?? GameDefault;
                Classic = GameDefault;
                NebulaBlue = FindById("nebula_blue") ?? Chiikawa;
                AmberMarket = FindById("amber_market") ?? Chiikawa;
                return true;
            }
            catch (FormatException error)
            {
                log.Log("skin_presets.json 无效：" + error.Message);
                return false;
            }
            catch (JsonException error)
            {
                log.Log("skin_presets.json 无效：" + error.Message);
                return false;
            }
        }

        internal static bool TryCreateUserPreset(string displayName, out string presetId, out string message)
        {
            presetId = null;
            message = null;
            if (!userStoreWritable)
            {
                message = "用户预设文件不可写或 JSON 有误；请先检查 ModData/skin_framework/" + UserPresetFilePath;
                return false;
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                message = "请填写预设名称。";
                return false;
            }

            string normalizedName = displayName.Trim();
            foreach (SkinTheme theme in all)
            {
                if (string.Equals(theme.DisplayName, normalizedName, StringComparison.Ordinal))
                {
                    message = "预设名称已存在，请换一个名称。";
                    return false;
                }
            }

            presetId = "custom_skin_" + Guid.NewGuid().ToString("N");
            string safeName = new string(normalizedName.Select(c =>
                char.IsControl(c) || "<>:\"/\\|?*".IndexOf(c) >= 0 ? '_' : c).Take(60).ToArray()).Trim().TrimEnd('.');
            string assetFolder = safeName + "_" + presetId;

            SkinTheme source = GameDefault;
            PresetRecord chiikawa = bundledRecords.FirstOrDefault(record => record.id == "chiikawa");
            List<TextureRecord> textureTemplate = new List<TextureRecord>();
            if (chiikawa?.textures != null)
            {
                foreach (TextureRecord texture in chiikawa.textures)
                {
                    if (texture == null || string.IsNullOrWhiteSpace(texture.slot)) continue;
                    string key = presetId + "_" + texture.slot;
                    if (texture.key != null && texture.key.Type == JTokenType.String)
                    {
                        string sourceKey = texture.key.Value<string>();
                        if (!string.IsNullOrWhiteSpace(sourceKey))
                            key = sourceKey.StartsWith("chiikawa_", StringComparison.Ordinal)
                                ? presetId + sourceKey.Substring("chiikawa".Length)
                                : presetId + "_" + sourceKey;
                    }
                    textureTemplate.Add(new TextureRecord { slot = texture.slot, key = JToken.FromObject(key) });
                }
            }

            PresetRecord created = new PresetRecord
            {
                id = presetId,
                name = normalizedName,
                assetFolder = assetFolder,
                useGameDefaults = false,
                useSharedTextureFallbacks = false,
                colors = ColorsFrom(source),
                textures = textureTemplate.ToArray()
            };
            List<PresetRecord> updated = new List<PresetRecord>(userRecords) { created };
            string json = JsonConvert.SerializeObject(new PresetFile { schemaVersion = 1, presets = updated.ToArray() }, Formatting.Indented);
            string folderReadmePath = "Textures/" + assetFolder + "/" + ThemeFolderReadmeName;
            string folderReadme = "Skin Framework 自建预设素材目录\n\n" +
                "预设 ID: " + presetId + "\n" +
                "素材按 ModData/skin_framework/Textures/<预设素材目录>/<类别>/<文件名>.png 的结构放置。\n" +
                "预设 JSON 位于 ModData/skin_framework/" + UserPresetFilePath + "。\n" +
                "每个类别目录中的 ASSET_LAYOUT.txt 列出该目录对应的贴图槽和文件键。\n" +
                "修改 JSON 后重启游戏生效。\n";
            try
            {
                ModHelper.WriteModDataFileText("skin_framework", folderReadmePath, folderReadme);
                foreach (IGrouping<string, TextureRecord> group in textureTemplate
                    .GroupBy(texture => SkinService.GetTextureCategory(texture.slot), StringComparer.Ordinal))
                {
                    StringBuilder layout = new StringBuilder();
                    layout.AppendLine("贴图类别: ").AppendLine(group.Key).AppendLine();
                    foreach (TextureRecord texture in group)
                    {
                        string key = texture.key.Value<string>();
                        layout.Append(texture.slot).Append(" = ").Append(key).AppendLine(".png");
                    }
                    string categoryPath = "Textures/" + assetFolder + "/" + group.Key + "/ASSET_LAYOUT.txt";
                    ModHelper.WriteModDataFileText("skin_framework", categoryPath, layout.ToString());
                }
                ModHelper.WriteModDataFileText("skin_framework", UserPresetFilePath, json);
            }
            catch (Exception error) when (IsFileError(error))
            {
                message = "创建预设文件失败：" + error.Message;
                return false;
            }

            userRecords = updated;
            message = "已创建。";
            return true;
        }

        internal static bool TryDeleteUserPreset(string presetId, out string message)
        {
            message = null;
            if (!userStoreWritable)
            {
                message = "用户预设文件不可写或 JSON 有误；无法删除。";
                return false;
            }

            PresetRecord target = userRecords.FirstOrDefault(record =>
                record != null && string.Equals(record.id, presetId, StringComparison.Ordinal));
            if (target == null)
            {
                message = "只能删除自建预设；随包预设不能删除。";
                return false;
            }

            string folderPrefix = "Textures/" + GetAssetFolder(presetId) + "/";
            List<PresetRecord> updated = userRecords
                .Where(record => record != null && !string.Equals(record.id, presetId, StringComparison.Ordinal))
                .ToList();
            string json = JsonConvert.SerializeObject(
                new PresetFile { schemaVersion = 1, presets = updated.ToArray() }, Formatting.Indented);
            try
            {
                ModHelper.WriteModDataFileText("skin_framework", UserPresetFilePath, json);
            }
            catch (Exception error) when (IsFileError(error))
            {
                message = "删除预设记录失败：" + error.Message;
                return false;
            }

            userRecords = updated;
            CleanupPresetFiles(folderPrefix);
            message = "预设记录已移除。";
            return true;
        }

        private static bool IsFileError(Exception error) =>
            error is System.IO.IOException || error is UnauthorizedAccessException || error is ArgumentException;

        private static void CleanupPresetFiles(string folderPrefix)
        {
            try
            {
                foreach (string file in ModHelper.ListModDataFiles("skin_framework"))
                    if (file.Replace('\\', '/').StartsWith(folderPrefix, StringComparison.Ordinal) &&
                        !ModHelper.DeleteModDataFile("skin_framework", file))
                        Debug.LogWarning("[Skin Framework] 未能删除素材：" + file);
            }
            catch (Exception error) when (IsFileError(error))
            {
                Debug.LogError("[Skin Framework] 清理预设素材失败：" + error.Message);
            }
        }

        internal static PresetRecord GetPackageRecord(string id) =>
            bundledRecords.Concat(userRecords).First(record => record.id == id);

        internal static void ImportPackageRecord(PresetRecord record, ModLog log, Action<string> writeAssets)
        {
            if (!userStoreWritable)
                throw new InvalidOperationException("用户预设文件不可写。");
            string name = record.name?.Trim();
            if (string.IsNullOrEmpty(name))
                throw new FormatException("美化包缺少预设名称。");
            record.name = name;
            for (int suffix = 2; FindByName(record.name) != null; suffix++)
                record.name = name + " (" + suffix + ")";
            record.id = "custom_skin_" + Guid.NewGuid().ToString("N");
            string safeName = new string(record.name.Select(c =>
                char.IsControl(c) || "<>:\"/\\|?*".IndexOf(c) >= 0 ? '_' : c).Take(60).ToArray()).Trim();
            record.assetFolder = safeName + "_" + record.id;
            var updated = userRecords.Concat(new[] { record }).ToArray();
            var merged = new PresetFile { schemaVersion = 1, presets = bundledRecords.Concat(updated).ToArray() };
            if (!LoadCore(JsonConvert.SerializeObject(merged), log, validateOnly: true))
                throw new FormatException("美化包的预设配置无效。");
            string json = JsonConvert.SerializeObject(
                new PresetFile { schemaVersion = 1, presets = updated }, Formatting.Indented);
            bool committed = false;
            try
            {
                writeAssets(record.assetFolder);
                ModHelper.WriteModDataFileText("skin_framework", UserPresetFilePath, json);
                committed = true;
            }
            finally
            {
                if (!committed)
                    CleanupPresetFiles("Textures/" + record.assetFolder + "/");
            }
            userRecords = updated.ToList();
            RefreshRuntime(log);
        }

        internal static bool IsUserPreset(string id) => userRecords.Any(record => record.id == id);

        internal static string GetAssetFolder(string presetId)
        {
            string folder = userRecords.FirstOrDefault(record => record.id == presetId)?.assetFolder;
            return !string.IsNullOrEmpty(folder) &&
                folder.EndsWith("_" + presetId, StringComparison.Ordinal) &&
                !folder.Any(c => char.IsControl(c) || "<>:\"/\\|?*".IndexOf(c) >= 0)
                ? folder : presetId;
        }

        internal static bool RefreshRuntime(ModLog log)
        {
            PresetRecord[] records = bundledRecords.Concat(userRecords).ToArray();
            string json = JsonConvert.SerializeObject(
                new PresetFile { schemaVersion = 1, presets = records }, Formatting.None);
            return LoadCore(json, log);
        }

        internal static string[] GetPresetNames()
        {
            string[] names = new string[all.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = all[i].DisplayName;
            return names;
        }

        private static ColorRecord ColorsFrom(SkinTheme theme)
        {
            return new ColorRecord
            {
                background = Hex(theme.Background), surface = Hex(theme.Surface), text = Hex(theme.Text),
                mutedText = Hex(theme.MutedText), accent = Hex(theme.Accent), accentText = Hex(theme.AccentText),
                border = Hex(theme.Border), iconTint = Hex(theme.IconTint)
            };
        }

        private static string Hex(SkinColor color) => "#" + ColorUtility.ToHtmlStringRGB(new Color(color.R, color.G, color.B, color.A));

        private static void UseFallback(ModLog log, string reason)
        {
            log.Log("skin_presets.json 无效，当前会话仅提供游戏默认主题：" + reason);
            all = new ReadOnlyCollection<SkinTheme>(new[] { fallback });
            GameDefault = fallback;
            Chiikawa = fallback;
            Classic = fallback;
            NebulaBlue = fallback;
            AmberMarket = fallback;
        }

        public static bool TryGet(string id, out SkinTheme theme)
        {
            theme = id == "game_default" ? GameDefault : FindById(id);
            return theme != null;
        }

        internal static SkinTheme FindByName(string name)
        {
            for (int i = 0; i < all.Count; i++)
                if (string.Equals(all[i].DisplayName, name, StringComparison.Ordinal))
                    return all[i];
            return null;
        }

        private static SkinTheme FindById(string id)
        {
            for (int i = 0; i < all.Count; i++)
                if (string.Equals(all[i].Id, id, StringComparison.Ordinal))
                    return all[i];
            return null;
        }

        private static SkinColor ParseColor(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !ColorUtility.TryParseHtmlString(value, out Color color))
                throw new FormatException("colors." + field + " must be a #RRGGBB color");
            return new SkinColor(color.r, color.g, color.b, color.a);
        }

        private static bool IsValidId(string id)
        {
            foreach (char character in id)
            {
                if (!(character >= 'a' && character <= 'z') && !(character >= 'A' && character <= 'Z') &&
                    !(character >= '0' && character <= '9') && character != '_' && character != '-')
                    return false;
            }
            return true;
        }

        private static SkinColor C(float r, float g, float b) => new SkinColor(r, g, b);

        public sealed class PresetFile { public int schemaVersion; public PresetRecord[] presets; }
        public sealed class PresetRecord
        {
            public string id;
            public string name;
            public string assetFolder;
            public bool useGameDefaults;
            public ColorRecord colors;
            public TextureRecord[] textures;
            public bool useSharedTextureFallbacks = true;
        }
        public sealed class ColorRecord
        {
            public string background;
            public string surface;
            public string text;
            public string mutedText;
            public string accent;
            public string accentText;
            public string border;
            public string iconTint;
        }
        public sealed class TextureRecord { public string slot; public JToken key; }
    }
}
