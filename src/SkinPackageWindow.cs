using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SkinFramework
{
    internal static class SkinPackageWindow
    {
        private const string ModId = "skin_framework";
        private const string Folder = "SkinPackages/";
        private const string WindowId = ModId + ".packages";
        private const string Status = "package_status";

        private sealed class Package
        {
            public int version = 1;
            public SkinPresets.PresetRecord preset;
            public SkinAdjustmentProfile profile;
        }

        internal static void Open(SkinTheme theme, Action onImported)
        {
            string[] files = ModHelper.ListModDataFiles(ModId).Select(p => p.Replace('\\', '/'))
                .Where(p => p.StartsWith(Folder, StringComparison.Ordinal) &&
                    !p.Substring(Folder.Length).Contains("/") && p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.Ordinal).ToArray();
            int selected = 0;
            CustomUIBuilder builder = CustomUIManager.Window(WindowId, SkinLanguage.Text("美化包", "Skin Pack"), CustomUIManager.LAYER_MENU)
                .SetSize(520, 235).SetCloseOnEscape(true)
                .AddLabel(SkinLanguage.Text("导出所选预设：", "Export selected preset: ") + theme.DisplayName)
                .AddLabel(SkinLanguage.Text("把 ZIP 放进美化包目录，再重开此窗口导入。", "Put the ZIP in the pack folder, then reopen this window."));
            if (files.Length > 0)
                builder.AddDropdown(files.Select(p => p.Substring(Folder.Length)).ToArray(), 0, i => selected = i);
            builder.AddLabel(SkinLanguage.Text("导入会创建独立预设。", "Import creates a separate preset."), id: Status);
            builder.BeginRow(6f, TextAnchor.MiddleRight);
            builder.AddButton(SkinLanguage.Text("复制目录", "Copy Folder"), () => Run(CopyFolder));
            builder.AddButton(SkinLanguage.Text("导出", "Export"), () => Run(() => Export(theme)));
            if (files.Length > 0)
                builder.AddButton(SkinLanguage.Text("导入", "Import"), () => Run(() => { string name = Import(files[selected]); onImported(); return SkinLanguage.Text("已导入：", "Imported: ") + name; }));
            builder.End();
            builder.Show();
        }

        private static void Run(Func<string> action)
        {
            string message;
            try { message = action(); }
            catch (Exception error) when (error is IOException || error is JsonException ||
                error is FormatException || error is InvalidOperationException || error is ArgumentException ||
                error is UnauthorizedAccessException)
            {
                message = SkinLanguage.Text("操作失败：", "Operation failed: ") + error.Message;
                Debug.LogError("[Skin Framework] " + error);
            }
            CustomUIManager.Instance.GetWindow(WindowId).Get(Status).SetText(message);
        }

        private static string CopyFolder()
        {
            const string readme = Folder + "README.txt";
            if (!ModHelper.ModDataFileExists(ModId, readme))
                ModHelper.WriteModDataFileText(ModId, readme, "将 Skin Framework 美化包 ZIP 放在此目录，再打开游戏内的美化包窗口。");
            GUIUtility.systemCopyBuffer = (ModHelper.GetModDataFolder(ModId) + "/" + Folder).Replace('/', '\\');
            return SkinLanguage.Text("已复制美化包目录。", "Pack folder copied.");
        }

        private static string Export(SkinTheme theme)
        {
            string filename = theme.Id + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".zip";
            ModHelper.WriteModDataFileBytes(ModId, Folder + filename, BuildPackage(theme, ModEntry.ExportPackageProfile(theme.Id)));
            return SkinLanguage.Text("已导出：", "Exported: ") + filename;
        }

        internal static string SaveAs(SkinTheme theme, string name, SkinAdjustmentProfile profile) =>
            ImportBytes(BuildPackage(theme, profile), name);

        private static byte[] BuildPackage(SkinTheme theme, SkinAdjustmentProfile profile)
        {
            var package = new Package { preset = SkinPresets.GetPackageRecord(theme.Id), profile = profile };
            var images = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            string prefix = "Textures/" + SkinPresets.GetAssetFolder(theme.Id) + "/";
            foreach (string listed in ModHelper.ListModDataFiles(ModId))
            {
                string path = listed.Replace('\\', '/');
                if (path.StartsWith(prefix, StringComparison.Ordinal) &&
                    path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    images[path.Substring(prefix.Length)] = ModHelper.ReadModDataFileBytes(ModId, listed);
            }
            var slots = package.preset.textures ?? Array.Empty<SkinPresets.TextureRecord>();
            var keys = slots.Where(t => t.key.Type == JTokenType.String).Select(t => t.key.Value<string>())
                .Concat(package.profile.settings.Values).Select(key => key?.Trim()).Select(key =>
                    key != null && key.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? key.Substring(0, key.Length - 4) : key)
                .Where(IsTextureKey).Distinct(StringComparer.Ordinal);
            foreach (string category in slots.Concat(SkinPresets.GetPackageRecord(SkinPresets.Chiikawa.Id).textures ?? Array.Empty<SkinPresets.TextureRecord>())
                .Select(t => SkinService.GetTextureCategory(t.slot)).Distinct())
            {
                foreach (string key in keys)
                {
                    string path = category + "/" + key + ".png";
                    if (images.ContainsKey(path)) continue;
                    string source = "Resources/Textures/" + theme.Id + "/" + path;
                    if (ModHelper.ModFileExists(ModId, source))
                        images.Add(path, ModHelper.ReadModFileBytes(ModId, source));
                }
            }
            using var buffer = new MemoryStream();
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
            {
                using (var writer = new StreamWriter(zip.CreateEntry("preset.json").Open()))
                    writer.Write(JsonConvert.SerializeObject(package));
                foreach (var image in images)
                {
                    using var output = zip.CreateEntry("Textures/" + image.Key).Open();
                    output.Write(image.Value, 0, image.Value.Length);
                }
            }
            return buffer.ToArray();
        }

        private static bool IsTextureKey(string key) => !string.IsNullOrWhiteSpace(key) &&
            key.Length <= 150 && key.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');

        private static string Import(string file) => ImportBytes(ModHelper.ReadModDataFileBytes(ModId, file), null);

        private const int MaxImageBytes = 16 * 1024 * 1024;
        private const int MaxPackageBytes = 64 * 1024 * 1024;

        private static string ImportBytes(byte[] data, string name)
        {
            if (data == null || data.Length > MaxPackageBytes)
                throw new FormatException("美化包超过 64 MiB 或无法读取。");
            using var buffer = new MemoryStream(data);
            using var zip = new ZipArchive(buffer, ZipArchiveMode.Read);
            var metadata = zip.GetEntry("preset.json");
            if (metadata == null || metadata.Length > 1024 * 1024 || zip.Entries.Count > 2048 || zip.Entries.Sum(e => e.Length) > MaxPackageBytes)
                throw new FormatException("美化包缺少配置或超出大小限制。");
            Package package;
            using (var reader = new StreamReader(new MemoryStream(ReadEntry(metadata, 1024 * 1024))))
                package = JsonConvert.DeserializeObject<Package>(reader.ReadToEnd());
            if (package?.version != 1 || package.preset == null || package.profile?.settings == null ||
                package.profile.positions == null || package.profile.transparency == null)
                throw new FormatException("美化包配置无效。");
            var images = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            long pixels = 0;
            int remainingBytes = MaxPackageBytes;
            foreach (var entry in zip.Entries)
            {
                if (entry == metadata) continue;
                string path = entry.FullName;
                if (!path.StartsWith("Textures/", StringComparison.Ordinal) ||
                    !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    path.Substring(0, path.Length - 4).Split('/').Any(segment => !IsTextureKey(segment)) ||
                    images.ContainsKey(path))
                    throw new FormatException("美化包包含无效或重复的贴图路径。");
                byte[] png = ReadEntry(entry, Math.Min(remainingBytes, MaxImageBytes));
                remainingBytes -= png.Length;
                pixels += ValidatePng(png);
                if (pixels > 32L * 1024 * 1024)
                    throw new FormatException("美化包贴图总像素数过大。");
                images.Add(path, png);
            }
            if (name != null) package.preset.name = name;
            ModEntry.ImportPackage(package.preset, package.profile, folder =>
            {
                foreach (var image in images)
                {
                    ModHelper.WriteModDataFileBytes(ModId,
                        "Textures/" + folder + "/" + image.Key.Substring("Textures/".Length), image.Value);
                }
            });
            return package.preset.name;
        }
        private static byte[] ReadEntry(ZipArchiveEntry entry, int limit)
        {
            if (entry.Length > limit)
                throw new FormatException("美化包条目过大。");
            using var input = entry.Open();
            using var output = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = input.Read(chunk, 0, chunk.Length)) != 0)
            {
                if (output.Length + count > limit)
                    throw new FormatException("解压后的美化包条目过大。");
                output.Write(chunk, 0, count);
            }
            return output.ToArray();
        }

        private static long ValidatePng(byte[] png)
        {
            if (png.Length < 33 || ReadUInt(png, 0) != 0x89504e47 ||
                ReadUInt(png, 4) != 0x0d0a1a0a || ReadUInt(png, 8) != 13 ||
                ReadUInt(png, 12) != 0x49484452)
                throw new FormatException("美化包包含无效 PNG。");
            uint width = ReadUInt(png, 16);
            uint height = ReadUInt(png, 20);
            if (width == 0 || height == 0 || width > 4096 || height > 4096)
                throw new FormatException("PNG 尺寸必须在 1–4096 像素内。");
            return (long)width * height;
        }

        private static uint ReadUInt(byte[] bytes, int offset) =>
            ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) |
            ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];

    }
}
