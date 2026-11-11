using UnityEngine;
using UnityEngine.EventSystems;

namespace SkinFramework
{
    internal sealed class FixedFixtureWindowDragHandle : MonoBehaviour, IDragHandler
    {
        internal RectTransform Target { private get; set; }

        public void OnDrag(PointerEventData eventData)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas.scaleFactor;
            Vector2 position = Target.anchoredPosition + eventData.delta / scale;
            float canvasWidth = Screen.width / scale;
            float canvasHeight = Screen.height / scale;
            position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, canvasWidth - Target.rect.width));
            position.y = Mathf.Clamp(position.y, Mathf.Min(0f, Target.rect.height - canvasHeight), 0f);
            Target.anchoredPosition = position;
        }
    }
}
