using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    internal sealed partial class FixedFixtureWindow
    {
        internal void SetWindowAlpha(float alpha)
        {
            SetSliderValue(windowAlphaSlider, alpha);
            windowAlphaValueText.text = Mathf.RoundToInt(alpha * 100f) + "%";
            SetImageAlpha(backdropImage, 0.32f * alpha);
            SetImageAlpha(frameImage, alpha);
            SetImageAlpha(faceImage, alpha);
            SetImageAlpha(titleBarImage, alpha);
            SetImageAlpha(titleDividerImage, alpha);
            SetImageAlpha(listBorderImage, alpha);
            SetImageAlpha(listFaceImage, alpha);
            SetImageAlpha(searchBackgroundImage, alpha);
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
