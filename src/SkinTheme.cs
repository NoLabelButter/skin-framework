namespace SkinFramework
{
    /// <summary>一套供调用方映射到自有控件的 UI 配色令牌。</summary>
    public sealed class SkinTheme
    {
        public string Id { get; }
        public string DisplayName { get; }
        public bool UsesGameDefaults { get; }
        internal bool UsesSharedTextureFallbacks { get; }
        public SkinColor Background { get; }
        public SkinColor Surface { get; }
        public SkinColor Text { get; }
        public SkinColor MutedText { get; }
        public SkinColor Accent { get; }
        public SkinColor AccentText { get; }
        public SkinColor Border { get; }
        public SkinColor IconTint { get; }
        private readonly System.Collections.Generic.Dictionary<string, string> textureKeys;
        private readonly System.Collections.Generic.HashSet<string> disabledTextureSlots;

        /// <summary>返回该主题中某个逻辑贴图槽对应的 Textures 根目录键名。</summary>
        public string GetTextureKey(string slot) =>
            textureKeys != null && textureKeys.TryGetValue(slot, out string key) ? key : Id + "_" + slot;

        /// <summary>Returns whether the theme explicitly hides the image component for a texture slot.</summary>
        internal bool IsTextureDisabled(string slot) =>
            disabledTextureSlots != null && disabledTextureSlots.Contains(slot);

        internal SkinTheme(
            string id,
            string displayName,
            bool usesGameDefaults,
            SkinColor background,
            SkinColor surface,
            SkinColor text,
            SkinColor mutedText,
            SkinColor accent,
            SkinColor accentText,
            SkinColor border,
            SkinColor iconTint,
            System.Collections.Generic.Dictionary<string, string> textureKeys = null,
            System.Collections.Generic.HashSet<string> disabledTextureSlots = null,
            bool usesSharedTextureFallbacks = true)
        {
            Id = id;
            DisplayName = displayName;
            UsesGameDefaults = usesGameDefaults;
            Background = background;
            Surface = surface;
            Text = text;
            MutedText = mutedText;
            Accent = accent;
            AccentText = accentText;
            Border = border;
            IconTint = iconTint;
            this.textureKeys = textureKeys;
            this.disabledTextureSlots = disabledTextureSlots;
            UsesSharedTextureFallbacks = usesSharedTextureFallbacks;
        }
    }
}
