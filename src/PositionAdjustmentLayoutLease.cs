using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinFramework
{
    /// <summary>位置调整期间让目标退出父布局的坐标管理，并在结束时恢复原组件状态。</summary>
    internal sealed class PositionAdjustmentLayoutLease
    {
        private readonly Dictionary<RectTransform, LayoutElementState> states =
            new Dictionary<RectTransform, LayoutElementState>();

        internal void PreservePositionsOutsideLayout(List<PositionAdjustmentTarget> targets)
        {
            if (targets.Count == 0)
                return;

            HashSet<RectTransform> layoutParents = new HashSet<RectTransform>();
            for (int i = 0; i < targets.Count; i++)
            {
                RectTransform rect = targets[i].Rect;
                RectTransform parent = rect == null ? null : rect.parent as RectTransform;
                if (parent != null && parent.GetComponent<LayoutGroup>() != null)
                    layoutParents.Add(parent);
            }
            if (layoutParents.Count == 0)
                return;

            RestoreOriginalValues();
            foreach (RectTransform parent in layoutParents)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);

            for (int i = 0; i < targets.Count; i++)
            {
                RectTransform rect = targets[i].Rect;
                RectTransform parent = rect == null ? null : rect.parent as RectTransform;
                if (parent == null || !layoutParents.Contains(parent))
                    continue;

                if (!states.TryGetValue(rect, out LayoutElementState state))
                {
                    LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
                    bool created = layoutElement == null;
                    if (created)
                        layoutElement = rect.gameObject.AddComponent<LayoutElement>();
                    state = new LayoutElementState(layoutElement, created, layoutElement.ignoreLayout);
                    states.Add(rect, state);
                }
                LayoutElement element = state.Element;
                element.ignoreLayout = true;
            }
        }

        internal void Restore()
        {
            foreach (KeyValuePair<RectTransform, LayoutElementState> pair in states)
            {
                LayoutElementState state = pair.Value;
                if (state.Element == null)
                    continue;
                if (state.Created)
                    Object.Destroy(state.Element);
                else
                    state.Element.ignoreLayout = state.OriginalIgnoreLayout;
            }
            states.Clear();
        }

        private void RestoreOriginalValues()
        {
            foreach (LayoutElementState state in states.Values)
                if (state.Element != null)
                    state.Element.ignoreLayout = state.Created ? false : state.OriginalIgnoreLayout;
        }

        private sealed class LayoutElementState
        {
            internal readonly LayoutElement Element;
            internal readonly bool Created;
            internal readonly bool OriginalIgnoreLayout;

            internal LayoutElementState(LayoutElement element, bool created, bool originalIgnoreLayout)
            {
                Element = element;
                Created = created;
                OriginalIgnoreLayout = originalIgnoreLayout;
            }
        }
    }
}
