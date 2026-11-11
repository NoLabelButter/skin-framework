using System.Collections.Generic;
using UnityEngine;

namespace SkinFramework
{
    /// <summary>保存一次位置调整开始时的配置和屏幕位置，供取消操作回滚。</summary>
    internal sealed class PositionAdjustmentSession
    {
        private readonly Dictionary<Transform, Vector2> initialPositions =
            new Dictionary<Transform, Vector2>();
        private readonly Dictionary<Transform, Vector2> defaultPositions =
            new Dictionary<Transform, Vector2>();
        private readonly PositionAdjustmentDocument initialDocument;

        internal PositionAdjustmentPage Page { get; }

        internal PositionAdjustmentSession(PositionAdjustmentPage page,
            PositionAdjustmentDocument document, List<PositionAdjustmentTarget> targets)
        {
            Page = page;
            string key = GetConfigKey(page);
            initialDocument = PositionAdjustmentDocument.Read(document.Serialize(), key);
            for (int i = 0; i < targets.Count; i++)
            {
                PositionAdjustmentTarget target = targets[i];
                if (!target.CanMove)
                    continue;
                Vector2 position = target.GetPosition();
                initialPositions[target.Transform] = position;
                if (document.offsets.TryGetValue(target.Id, out PositionAdjustmentOffset offset))
                    position -= new Vector2(offset.x, offset.y);
                defaultPositions[target.Transform] = position;
            }
        }

        internal PositionAdjustmentDocument RestoreDocument()
        {
            string key = GetConfigKey(Page);
            return PositionAdjustmentDocument.Read(initialDocument.Serialize(), key);
        }

        private static string GetConfigKey(PositionAdjustmentPage page) => page switch
        {
            PositionAdjustmentPage.MainMenu => PositionAdjustmentController.MainConfigKey,
            PositionAdjustmentPage.Specialization => PositionAdjustmentController.SpecializationConfigKey,
            PositionAdjustmentPage.LoadGame => PositionAdjustmentController.LoadGameConfigKey,
            _ => PositionAdjustmentController.CreditsConfigKey
        };

        internal void RestorePositions()
        {
            foreach (KeyValuePair<Transform, Vector2> pair in initialPositions)
                if (pair.Key != null)
                {
                    PositionAdjustmentTarget target = new PositionAdjustmentTarget(string.Empty, string.Empty, pair.Key);
                    target.SetPosition(pair.Value);
                }
        }

        internal void ResetTargetsToDefaults(List<PositionAdjustmentTarget> targets)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                PositionAdjustmentTarget target = targets[i];
                if (target.CanMove && defaultPositions.TryGetValue(target.Transform, out Vector2 position))
                    target.SetPosition(position);
            }
        }
    }
}
