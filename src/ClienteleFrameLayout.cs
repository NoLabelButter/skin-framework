using System;

namespace SkinFramework
{
    // Four equal cells; the optional suffix declares one transparent column on each side.
    internal readonly struct ClienteleFrameLayout
    {
        internal const string PaddedSuffix = "__pad1";
        internal readonly int CellWidth;
        internal readonly int Padding;
        internal readonly float ContentScale;
        internal readonly float PivotX;

        internal ClienteleFrameLayout(string textureKey, int textureWidth, float sourceWidth, float sourcePivotX)
        {
            CellWidth = textureWidth / 4;
            Padding = textureKey.EndsWith(PaddedSuffix, StringComparison.Ordinal) ? 1 : 0;
            ContentScale = (CellWidth - 2f * Padding) / sourceWidth;
            PivotX = (Padding + sourcePivotX * ContentScale) / CellWidth;
        }
    }
}
