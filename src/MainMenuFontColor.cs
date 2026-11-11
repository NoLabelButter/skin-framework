using System.Globalization;
using UnityEngine;

namespace SkinFramework
{
    internal static class MainMenuFontColor
    {
        public const string ConfigKey = "main_menu_font_color_v1";

        public static bool TryParse(string value, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string hex = value.Trim();
            if (hex.StartsWith("#", System.StringComparison.Ordinal))
                hex = hex.Substring(1);
            if (hex.Length != 6)
                return false;

            if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) ||
                !byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) ||
                !byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                return false;

            color = new Color32(r, g, b, 255);
            return true;
        }

        public static string ToHex(Color color)
        {
            Color32 rgb = color;
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", rgb.r, rgb.g, rgb.b);
        }
    }
}
