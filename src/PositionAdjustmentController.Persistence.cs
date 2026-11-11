using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinFramework
{
    internal sealed partial class PositionAdjustmentController
    {
        private void OnMainConfigChanged(string json)
        {
            if (mainDocument.Serialize() == json)
                return;
            Debug.LogWarning("[Skin Framework] 主页面位置配置变更，将重载并重套全部主页面偏移。");
            mainDocument = PositionAdjustmentDocument.Read(json, MainConfigKey);
            ResetAppliedOffsets("main.");
            ApplySaved(PositionAdjustmentPage.MainMenu);
        }

        private void OnSpecializationConfigChanged(string json)
        {
            if (specializationDocument.Serialize() == json)
                return;
            Debug.LogWarning("[Skin Framework] 专精页面位置配置变更，将重载并重套全部专精页偏移。");
            specializationDocument = PositionAdjustmentDocument.Read(json, SpecializationConfigKey);
            ResetAppliedOffsets("specialization.");
            ApplySaved(PositionAdjustmentPage.Specialization);
        }

        private void OnLoadGameConfigChanged(string json)
        {
            if (loadGameDocument.Serialize() == json)
                return;
            Debug.LogWarning("[Skin Framework] 载入游戏位置配置变更，将重载并重套载入游戏页面偏移。");
            loadGameDocument = PositionAdjustmentDocument.Read(json, LoadGameConfigKey);
            ResetAppliedOffsets("load_game.");
            ApplySaved(PositionAdjustmentPage.LoadGame);
        }

        private void OnCreditsConfigChanged(string json)
        {
            if (creditsDocument.Serialize() == json)
                return;
            creditsDocument = PositionAdjustmentDocument.Read(json, CreditsConfigKey);
            ResetAppliedOffsets("credits.");
            ApplySaved(PositionAdjustmentPage.Credits);
        }

        private void OnGameOptionsConfigChanged(string json)
        {
            if (gameOptionsDocument.Serialize() == json)
                return;
            gameOptionsDocument = PositionAdjustmentDocument.Read(json, GameOptionsConfigKey);
            ResetAppliedOffsets("game_options.");
            ApplySaved(PositionAdjustmentPage.GameOptions);
        }

        private void ResetAppliedOffsets(string prefix)
        {
            List<Transform> remove = new List<Transform>();
            foreach (KeyValuePair<Transform, PositionAppliedOffset> pair in applied)
            {
                if (!pair.Value.Id.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (pair.Key != null)
                {
                    PositionAdjustmentTarget target = new PositionAdjustmentTarget(pair.Value.Id, string.Empty, pair.Key);
                    if (target.CanMove && target.GetPosition() == pair.Value.AppliedPosition)
                        target.SetPosition(target.GetPosition() - pair.Value.Delta);
                }
                remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                applied.Remove(remove[i]);
        }

        private void ForgetAppliedOffsets(string prefix)
        {
            List<Transform> remove = new List<Transform>();
            foreach (KeyValuePair<Transform, PositionAppliedOffset> pair in applied)
                if (pair.Value.Id.StartsWith(prefix, StringComparison.Ordinal))
                    remove.Add(pair.Key);
            for (int i = 0; i < remove.Count; i++)
                applied.Remove(remove[i]);
        }

        private void RestoreAppliedOffsets()
        {
            ResetAppliedOffsets("main.");
            ResetAppliedOffsets("specialization.");
            ResetAppliedOffsets("load_game.");
            ResetAppliedOffsets("credits.");
            ResetAppliedOffsets("game_options.");
        }
    }
}
