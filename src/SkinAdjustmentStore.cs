using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace SkinFramework
{
    internal sealed class SkinAdjustmentProfile
    {
        public Dictionary<string, PositionAdjustmentDocument> positions { get; set; } =
            new Dictionary<string, PositionAdjustmentDocument>(StringComparer.Ordinal);
        public Dictionary<string, TransparencyAdjustmentDocument> transparency { get; set; } =
            new Dictionary<string, TransparencyAdjustmentDocument>(StringComparer.Ordinal);
        public Dictionary<string, string> settings { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    internal sealed class SkinAdjustmentStoreDocument
    {
        public int schemaVersion { get; set; } = 2;
        public Dictionary<string, SkinAdjustmentProfile> profiles { get; set; } =
            new Dictionary<string, SkinAdjustmentProfile>(StringComparer.Ordinal);
    }

    /// <summary>按稳定皮肤 ID 保存四个页面的位置与透明度文档。</summary>
    internal sealed class SkinAdjustmentStore
    {
        internal const string ConfigKey = "skin_adjustments_v1";
        internal const string MigrationKey = "skin_adjustments_migrated_v1";
        internal const string SettingsMigrationKey = "skin_settings_migrated_v1";
        private ConfigEntry<string> entry;
        private SkinAdjustmentStoreDocument document;

        internal void Initialize(ConfigEntry<string> storeEntry, ConfigEntry<bool> migrationEntry,
            string selectedSkin, ConfigEntry<string>[] legacyPositions,
            ConfigEntry<string>[] legacyTransparency)
        {
            entry = storeEntry;
            document = Read(entry.Value);
            if (migrationEntry.Value)
                return;

            SkinAdjustmentProfile profile = GetProfile(selectedSkin);
            string[] pages = { "main", "specialization", "load_game", "credits", "game_options" };
            for (int i = 0; i < pages.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(legacyPositions[i].Value) && !profile.positions.ContainsKey(pages[i]))
                    profile.positions[pages[i]] = PositionAdjustmentDocument.Read(legacyPositions[i].Value, legacyPositions[i].Key);
                if (!string.IsNullOrWhiteSpace(legacyTransparency[i].Value) && !profile.transparency.ContainsKey(pages[i]))
                    profile.transparency[pages[i]] = TransparencyAdjustmentDocument.Read(legacyTransparency[i].Value, legacyTransparency[i].Key);
            }
            entry.Set(JsonConvert.SerializeObject(document));
            migrationEntry.Set(true);
            Debug.Log($"[Skin Framework] 已将旧位置/透明度配置迁移到皮肤档案：skinId={selectedSkin}；旧配置键保留。新配置键={ConfigKey}。");
        }

        internal void MigrateSettings(ConfigEntry<bool> migrationEntry, string skinId,
            Dictionary<string, string> legacyValues)
        {
            if (migrationEntry.Value)
                return;
            SkinAdjustmentProfile profile = GetProfile(skinId);
            foreach (KeyValuePair<string, string> pair in legacyValues)
                if (!string.IsNullOrWhiteSpace(pair.Value) && !profile.settings.ContainsKey(pair.Key))
                    profile.settings[pair.Key] = pair.Value;
            Persist();
            migrationEntry.Set(true);
            Debug.Log($"[Skin Framework] 已将旧全局外观配置迁移到皮肤档案：skinId={skinId}；旧配置键保留。");
        }

        internal string GetSetting(string skinId, string key) =>
            GetProfile(skinId).settings.TryGetValue(key, out string value) ? value : null;

        internal bool HasSetting(string skinId, string key) => GetProfile(skinId).settings.ContainsKey(key);

        internal void SaveSetting(string skinId, string key, string value)
        {
            SkinAdjustmentProfile profile = GetProfile(skinId);
            profile.settings[key] = value ?? string.Empty;
            Persist();
        }

        internal PositionAdjustmentDocument GetPosition(string skinId, string page) =>
            GetProfile(skinId).positions.TryGetValue(page, out PositionAdjustmentDocument value)
                ? PositionAdjustmentDocument.Read(value.Serialize(), $"{ConfigKey}/{skinId}/{page}/position")
                : new PositionAdjustmentDocument();

        internal TransparencyAdjustmentDocument GetTransparency(string skinId, string page) =>
            GetProfile(skinId).transparency.TryGetValue(page, out TransparencyAdjustmentDocument value)
                ? TransparencyAdjustmentDocument.Read(value.Serialize(), $"{ConfigKey}/{skinId}/{page}/transparency")
                : new TransparencyAdjustmentDocument();

        internal void SavePosition(string skinId, string page, PositionAdjustmentDocument value)
        {
            GetProfile(skinId).positions[page] = PositionAdjustmentDocument.Read(value.Serialize(),
                $"{ConfigKey}/{skinId}/{page}/position");
            Persist();
        }

        internal void SaveTransparency(string skinId, string page, TransparencyAdjustmentDocument value)
        {
            GetProfile(skinId).transparency[page] = TransparencyAdjustmentDocument.Read(value.Serialize(),
                $"{ConfigKey}/{skinId}/{page}/transparency");
            Persist();
        }


        private SkinAdjustmentProfile GetProfile(string skinId)
        {
            if (!document.profiles.TryGetValue(skinId, out SkinAdjustmentProfile profile))
            {
                profile = new SkinAdjustmentProfile();
                document.profiles.Add(skinId, profile);
            }
            return profile;
        }

        internal SkinAdjustmentProfile ExportProfile(string id) => GetProfile(id);

        internal void ImportProfile(string id, SkinAdjustmentProfile profile)
        {
            document.profiles[id] = profile;
            Persist();
        }

        internal void RemoveProfile(string id)
        {
            if (document.profiles.Remove(id)) Persist();
        }

        private void Persist() => entry.Set(JsonConvert.SerializeObject(document));

        private static SkinAdjustmentStoreDocument Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new SkinAdjustmentStoreDocument();
            try
            {
                SkinAdjustmentStoreDocument loaded = JsonConvert.DeserializeObject<SkinAdjustmentStoreDocument>(json);
                if (loaded != null && (loaded.schemaVersion == 1 || loaded.schemaVersion == 2) && loaded.profiles != null)
                {
                    loaded.schemaVersion = 2;
                    foreach (KeyValuePair<string, SkinAdjustmentProfile> pair in loaded.profiles)
                    {
                        if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null ||
                            pair.Value.positions == null || pair.Value.transparency == null)
                            throw new JsonSerializationException("皮肤档案缺少 ID、位置表或透明度表");
                        if (pair.Value.settings == null)
                            pair.Value.settings = new Dictionary<string, string>(StringComparer.Ordinal);
                    }
                    return loaded;
                }
                Debug.LogError($"[Skin Framework] 配置 {ConfigKey} 的 schema 或结构无效；使用空档案，原配置文本保留。");
            }
            catch (JsonException exception)
            {
                Debug.LogError($"[Skin Framework] 配置 {ConfigKey} 无法解析；使用空档案，原配置文本保留：{exception.Message}");
            }
            return new SkinAdjustmentStoreDocument();
        }
    }
}
