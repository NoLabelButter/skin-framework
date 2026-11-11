using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>在布局组子项脱离自动排版时保留原槽位，供界面控件独立移动。</summary>
    internal sealed class PositionAdjustmentLayoutSlotLease
    {
        private readonly Dictionary<RectTransform, SlotState> states =
            new Dictionary<RectTransform, SlotState>();
        private readonly HashSet<RectTransform> parents = new HashSet<RectTransform>();

        internal void PreservePositionsOutsideLayout(List<PositionAdjustmentTarget> targets)
        {
            if (states.Count > 0)
                return;
            foreach (PositionAdjustmentTarget target in targets)
            {
                RectTransform rect = target.Rect;
                RectTransform parent = rect == null ? null : rect.parent as RectTransform;
                LayoutGroup group = parent == null ? null : parent.GetComponent<LayoutGroup>();
                if (!target.CanMove || group == null)
                    continue;

                LayoutElement element = rect.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout)
                    continue;

                bool createdElement = element == null;
                if (createdElement)
                    element = rect.gameObject.AddComponent<LayoutElement>();

                int siblingIndex = rect.GetSiblingIndex();
                GameObject placeholder = new GameObject("SkinFrameworkPositionSlot",
                    typeof(RectTransform), typeof(LayoutElement));
                placeholder.transform.SetParent(parent, false);
                CopyRectTransform(rect, placeholder.transform as RectTransform);
                placeholder.transform.SetSiblingIndex(siblingIndex);
                CopyLayoutMetrics(rect, placeholder.GetComponent<LayoutElement>());

                states.Add(rect, new SlotState(element, createdElement, element.ignoreLayout, placeholder));
                element.ignoreLayout = true;
                parents.Add(parent);
            }

            RebuildParents();
            foreach (KeyValuePair<RectTransform, SlotState> pair in states)
            {
                RectTransform target = pair.Key;
                RectTransform slot = pair.Value.Placeholder.transform as RectTransform;
                target.anchoredPosition = slot.anchoredPosition;
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, slot.rect.width);
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, slot.rect.height);
            }
        }

        internal void Restore()
        {
            foreach (KeyValuePair<RectTransform, SlotState> pair in states)
            {
                SlotState state = pair.Value;
                if (state.Element != null)
                {
                    if (state.CreatedElement)
                        Object.Destroy(state.Element);
                    else
                        state.Element.ignoreLayout = state.OriginalIgnoreLayout;
                }
                if (state.Placeholder != null)
                {
                    state.Placeholder.SetActive(false);
                    Object.Destroy(state.Placeholder);
                }
            }
            states.Clear();
            RebuildParents();
            parents.Clear();
        }

        private void RebuildParents()
        {
            List<RectTransform> ordered = new List<RectTransform>();
            foreach (RectTransform parent in parents)
                if (parent != null)
                    ordered.Add(parent);
            ordered.Sort((left, right) => GetDepth(left).CompareTo(GetDepth(right)));
            for (int i = 0; i < ordered.Count; i++)
                LayoutRebuilder.ForceRebuildLayoutImmediate(ordered[i]);
        }

        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            for (Transform current = transform; current != null; current = current.parent)
                depth++;
            return depth;
        }

        private static void CopyLayoutMetrics(RectTransform source, LayoutElement target)
        {
            target.minWidth = LayoutUtility.GetMinWidth(source);
            target.minHeight = LayoutUtility.GetMinHeight(source);
            target.preferredWidth = LayoutUtility.GetPreferredWidth(source);
            target.preferredHeight = LayoutUtility.GetPreferredHeight(source);
            target.flexibleWidth = LayoutUtility.GetFlexibleWidth(source);
            target.flexibleHeight = LayoutUtility.GetFlexibleHeight(source);
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
            target.anchoredPosition = source.anchoredPosition;
            target.localScale = source.localScale;
            target.localRotation = source.localRotation;
        }

        private sealed class SlotState
        {
            internal readonly LayoutElement Element;
            internal readonly bool CreatedElement;
            internal readonly bool OriginalIgnoreLayout;
            internal readonly GameObject Placeholder;

            internal SlotState(LayoutElement element, bool createdElement, bool originalIgnoreLayout,
                GameObject placeholder)
            {
                Element = element;
                CreatedElement = createdElement;
                OriginalIgnoreLayout = originalIgnoreLayout;
                Placeholder = placeholder;
            }
        }
    }
}
