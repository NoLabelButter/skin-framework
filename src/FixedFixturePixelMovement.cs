using UnityEngine;

namespace SkinFramework
{
    internal static class FixedFixturePixelMovement
    {
        internal static float LocalUnitsForScreenPixels(Transform target, Sprite sprite, Vector3 localAxis, float pixels)
        {
            Transform parent = target.parent;
            Vector3 localStart = target.localPosition;
            Vector3 localEnd = localStart + localAxis;
            Vector3 worldStart = parent == null ? localStart : parent.TransformPoint(localStart);
            Vector3 worldEnd = parent == null ? localEnd : parent.TransformPoint(localEnd);

            Canvas canvas = target.GetComponentInParent<Canvas>();
            Canvas rootCanvas = canvas == null ? null : canvas.rootCanvas;
            if (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return pixels / rootCanvas.scaleFactor;

            Camera camera = rootCanvas != null && rootCanvas.worldCamera != null
                ? rootCanvas.worldCamera
                : Camera.main;
            if (camera != null)
            {
                Vector2 screenStart = camera.WorldToScreenPoint(worldStart);
                Vector2 screenEnd = camera.WorldToScreenPoint(worldEnd);
                return pixels / Vector2.Distance(screenStart, screenEnd);
            }

            float worldScale = parent == null ? 1f : parent.TransformVector(localAxis).magnitude;
            return pixels / (sprite.pixelsPerUnit * worldScale);
        }
    }
}
