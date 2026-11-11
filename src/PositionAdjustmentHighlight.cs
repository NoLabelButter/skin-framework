using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SkinFramework
{
    /// <summary>给当前调整目标临时加描边，并在切换或退出时还原原有描边状态。</summary>
    internal sealed class PositionAdjustmentHighlight
    {
        private Outline active;
        private Graphic activeGraphic;
        private SpriteRenderer activeRenderer;
        private Color originalRendererColor;
        private Color originalGraphicColor;
        private Color originalColor;
        private Vector2 originalDistance;
        private bool originalEnabled;
        private bool created;
        private RectTransform markerCanvasRect;
        private RectTransform markerRect;
        private GameObject markerCanvasObject;
        private RawImage[] markerEdges;
        private RawImage markerLabelBackground;
        private TextMeshProUGUI markerLabel;

        private static readonly Color MarkerColor = new Color32(255, 190, 58, 245);

        public void Apply(Graphic graphic)
        {
            if (graphic == null)
                return;
            activeGraphic = graphic;
            originalGraphicColor = graphic.color;
            graphic.color = Color.Lerp(originalGraphicColor, new Color(1f, 0.78f, 0.28f, originalGraphicColor.a), 0.65f);
            active = graphic.GetComponent<Outline>();
            if (active == null)
            {
                active = graphic.gameObject.AddComponent<Outline>();
                created = true;
            }
            else
            {
                originalColor = active.effectColor;
                originalDistance = active.effectDistance;
                originalEnabled = active.enabled;
            }
            active.effectColor = new Color(1f, 0.78f, 0.28f, 1f);
            active.effectDistance = new Vector2(3f, 3f);
            active.enabled = true;
            graphic.SetVerticesDirty();
        }

        public void Apply(PositionAdjustmentTarget target)
        {
            ShowMarker(target);
            if (target.Graphic != null)
            {
                Apply(target.Graphic);
                return;
            }
            SpriteRenderer renderer = target.SpriteRenderer;
            if (renderer == null)
                return;
            activeRenderer = renderer;
            originalRendererColor = renderer.color;
            Color tint = new Color(1f, 0.78f, 0.28f, originalRendererColor.a);
            renderer.color = Color.Lerp(originalRendererColor, tint, 0.65f);
        }

        public void Clear()
        {
            if (activeGraphic != null)
            {
                activeGraphic.color = originalGraphicColor;
                activeGraphic.SetVerticesDirty();
            }
            activeGraphic = null;
            if (activeRenderer != null)
                activeRenderer.color = originalRendererColor;
            activeRenderer = null;
            if (active != null)
            {
                if (created)
                    Object.Destroy(active);
                else
                {
                    active.effectColor = originalColor;
                    active.effectDistance = originalDistance;
                    active.enabled = originalEnabled;
                }
            }
            active = null;
            created = false;
            if (markerRect != null)
                markerRect.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            Clear();
            if (markerCanvasObject != null)
                Object.Destroy(markerCanvasObject);
            markerCanvasRect = null;
            markerRect = null;
            markerCanvasObject = null;
            markerEdges = null;
            markerLabelBackground = null;
            markerLabel = null;
        }

        public void UpdateSpriteRendererAlpha(SpriteRenderer renderer)
        {
            if (activeRenderer == renderer && renderer != null)
                originalRendererColor.a = renderer.color.a;
        }

        private void ShowMarker(PositionAdjustmentTarget target)
        {
            EnsureMarker();
            if (!TryGetScreenBounds(target, out Vector2 min, out Vector2 max))
                return;
            markerRect.gameObject.SetActive(true);
            markerLabel.text = target.Label;

            float width = Mathf.Max(18f, max.x - min.x);
            float height = Mathf.Max(18f, max.y - min.y);
            Vector2 center = (min + max) * 0.5f;
            SetBox(markerEdges[0].rectTransform, new Vector2(center.x, max.y), new Vector2(width + 5f, 3f));
            SetBox(markerEdges[1].rectTransform, new Vector2(center.x, min.y), new Vector2(width + 5f, 3f));
            SetBox(markerEdges[2].rectTransform, new Vector2(min.x, center.y), new Vector2(3f, height + 5f));
            SetBox(markerEdges[3].rectTransform, new Vector2(max.x, center.y), new Vector2(3f, height + 5f));

            float labelWidth = Mathf.Clamp(target.Label.Length * 14f + 18f, 150f, 360f);
            float labelHeight = 25f;
            float labelX = Mathf.Clamp(center.x, -markerCanvasRect.rect.width * 0.5f + labelWidth * 0.5f,
                markerCanvasRect.rect.width * 0.5f - labelWidth * 0.5f);
            float labelY = max.y + labelHeight * 0.5f + 4f;
            if (labelY + labelHeight * 0.5f > markerCanvasRect.rect.height * 0.5f)
                labelY = min.y - labelHeight * 0.5f - 4f;
            SetBox(markerLabelBackground.rectTransform, new Vector2(labelX, labelY), new Vector2(labelWidth, labelHeight));
        }

        private void EnsureMarker()
        {
            if (markerRect != null)
                return;

            GameObject canvasObject = new GameObject("SkinFrameworkAdjustmentMarker", typeof(RectTransform), typeof(Canvas));
            markerCanvasObject = canvasObject;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32760;
            markerCanvasRect = canvasObject.GetComponent<RectTransform>();
            markerCanvasRect.anchorMin = Vector2.zero;
            markerCanvasRect.anchorMax = Vector2.one;
            markerCanvasRect.offsetMin = Vector2.zero;
            markerCanvasRect.offsetMax = Vector2.zero;

            GameObject markerObject = new GameObject("Marker", typeof(RectTransform));
            markerObject.transform.SetParent(markerCanvasRect, false);
            markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = Vector2.zero;
            markerRect.anchorMax = Vector2.one;
            markerRect.offsetMin = Vector2.zero;
            markerRect.offsetMax = Vector2.zero;
            markerEdges = new RawImage[4];
            for (int i = 0; i < markerEdges.Length; i++)
                markerEdges[i] = CreateSolidImage("Edge" + i, markerRect, MarkerColor);
            markerLabelBackground = CreateSolidImage("TargetLabelBackground", markerRect,
                new Color32(35, 31, 40, 245));
            GameObject textObject = new GameObject("TargetLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(markerLabelBackground.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(7f, 1f);
            textRect.offsetMax = new Vector2(-7f, -1f);
            markerLabel = textObject.GetComponent<TextMeshProUGUI>();
            markerLabel.fontSize = 13f;
            markerLabel.color = Color.white;
            markerLabel.alignment = TextAlignmentOptions.Center;
            markerLabel.enableWordWrapping = false;
            markerLabel.raycastTarget = false;
            FontLoader loader = FontLoader.Instance;
            if (loader != null && loader.simplifiedChineseFont != null)
                markerLabel.font = loader.simplifiedChineseFont;
            markerRect.gameObject.SetActive(false);
        }

        private static RawImage CreateSolidImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetBox(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private bool TryGetScreenBounds(PositionAdjustmentTarget target, out Vector2 min, out Vector2 max)
        {
            Vector3[] worldCorners = new Vector3[4];
            Camera sourceCamera = null;
            bool worldSpace = false;
            if (target.Rect != null)
            {
                target.Rect.GetWorldCorners(worldCorners);
                Canvas targetCanvas = target.Rect.GetComponentInParent<Canvas>();
                if (targetCanvas != null && targetCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    sourceCamera = targetCanvas.rootCanvas.worldCamera != null
                        ? targetCanvas.rootCanvas.worldCamera
                        : Camera.main;
            }
            else if (target.SpriteRenderer != null)
            {
                sourceCamera = Camera.main;
                if (sourceCamera == null)
                {
                    min = max = Vector2.zero;
                    return false;
                }
                worldSpace = true;
                Bounds bounds = target.SpriteRenderer.bounds;
                worldCorners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.center.z);
                worldCorners[1] = new Vector3(bounds.min.x, bounds.max.y, bounds.center.z);
                worldCorners[2] = new Vector3(bounds.max.x, bounds.max.y, bounds.center.z);
                worldCorners[3] = new Vector3(bounds.max.x, bounds.min.y, bounds.center.z);
            }
            else
            {
                min = max = Vector2.zero;
                return false;
            }

            Vector2 first = ToMarkerLocal(worldCorners[0], sourceCamera, worldSpace);
            min = max = first;
            for (int i = 1; i < worldCorners.Length; i++)
            {
                Vector2 point = ToMarkerLocal(worldCorners[i], sourceCamera, worldSpace);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return true;
        }

        private Vector2 ToMarkerLocal(Vector3 worldPoint, Camera sourceCamera, bool worldSpace)
        {
            Vector2 screenPoint = worldSpace
                ? (Vector2)sourceCamera.WorldToScreenPoint(worldPoint)
                : RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(markerCanvasRect, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }
    }
}
