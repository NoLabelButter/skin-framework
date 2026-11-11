namespace SkinFramework
{
    /// <summary>无 Unity 依赖的 RGBA 颜色值，通道范围为 0 到 1。</summary>
    public readonly struct SkinColor
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public SkinColor(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}
