namespace SkinFramework
{
    internal static class SkinLanguage
    {
        internal static bool English { get; set; }
        internal static string Text(string chinese, string english) => English ? english : chinese;
        internal static string TextureLabel(string label, string key)
        {
            if (!English || !System.Text.RegularExpressions.Regex.IsMatch(label, @"[\p{IsCJKUnifiedIdeographs}]"))
                return label;
            string words = key.Replace("_key_v1", "").Replace('_', ' ');
            return char.ToUpperInvariant(words[0]) + words.Substring(1);
        }

    }
}
