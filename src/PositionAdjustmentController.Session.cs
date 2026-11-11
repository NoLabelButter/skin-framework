using System.Collections.Generic;
using UnityEngine;

namespace SkinFramework
{
    internal sealed partial class PositionAdjustmentController
    {
        private PositionAdjustmentDocument CurrentDocument() => page switch
        {
            PositionAdjustmentPage.MainMenu => mainDocument,
            PositionAdjustmentPage.Specialization => specializationDocument,
            PositionAdjustmentPage.LoadGame => loadGameDocument,
            PositionAdjustmentPage.Credits => creditsDocument,
            _ => gameOptionsDocument
        };

        private static string PagePrefix(PositionAdjustmentPage targetPage) =>
            targetPage switch
            {
                PositionAdjustmentPage.MainMenu => "main.",
                PositionAdjustmentPage.Specialization => "specialization.",
                PositionAdjustmentPage.LoadGame => "load_game.",
                PositionAdjustmentPage.Credits => "credits.",
                _ => "game_options."
            };

        private string PageName() => page switch
        {
            PositionAdjustmentPage.MainMenu => "主页面",
            PositionAdjustmentPage.Specialization => "新游戏专精页面",
            PositionAdjustmentPage.LoadGame => "载入游戏页面",
            PositionAdjustmentPage.Credits => "制作人员名单",
            _ => "游戏选项界面"
        };

        private void SaveSession()
        {
            if (session == null)
                return;
            adjustmentStore.SavePosition(skinId, StorePageKey(page), CurrentDocument());
            Debug.Log($"[Skin Framework] 位置调整：已保存{PageName()}偏移。");
            session = null;
            End(true);
        }

        private void CancelSession() => CancelSession(true);

        private void CancelSession(bool reopenConfiguration)
        {
            RollbackSession(reopenConfiguration);
            End(reopenConfiguration);
        }

        private void RollbackSession(bool restorePositions)
        {
            if (session == null)
                return;

            PositionAdjustmentPage rollbackPage = session.Page;
            PositionAdjustmentDocument restored = session.RestoreDocument();
            if (restorePositions)
            {
                ResetAppliedOffsets(PagePrefix(rollbackPage));
                SetDocument(rollbackPage, restored);
                ApplySaved(rollbackPage);
                session.RestorePositions();
                TrackAppliedOffsets(rollbackPage, restored);
                Debug.Log($"[Skin Framework] 位置调整：已取消{PageNameFor(rollbackPage)}调整。");
            }
            else
                SetDocument(rollbackPage, restored);
            session = null;
        }

        private void RestoreDefaults()
        {
            if (session == null)
                return;

            session.ResetTargetsToDefaults(targets);
            ForgetAppliedOffsets(PagePrefix(page));
            PositionAdjustmentDocument defaults = new PositionAdjustmentDocument();
            SetDocument(page, defaults);

            PositionAdjustmentTargets.Apply(page, defaults, applied);
            Debug.Log($"[Skin Framework] 位置调整：已恢复{PageName()}默认位置；点击保存后生效，取消可撤销。");
        }

        private void SetDocument(PositionAdjustmentPage targetPage, PositionAdjustmentDocument document)
        {
            if (targetPage == PositionAdjustmentPage.MainMenu)
                mainDocument = document;
            else if (targetPage == PositionAdjustmentPage.Specialization)
                specializationDocument = document;
            else if (targetPage == PositionAdjustmentPage.LoadGame)
                loadGameDocument = document;
            else if (targetPage == PositionAdjustmentPage.Credits)
                creditsDocument = document;
            else
                gameOptionsDocument = document;
        }

        private static string PageNameFor(PositionAdjustmentPage targetPage) => targetPage switch
        {
            PositionAdjustmentPage.MainMenu => "主页面",
            PositionAdjustmentPage.Specialization => "新游戏专精页面",
        PositionAdjustmentPage.LoadGame => "载入游戏页面",
        PositionAdjustmentPage.Credits => "制作人员名单",
        _ => "游戏选项界面"
        };

        private static string StorePageKey(PositionAdjustmentPage targetPage) => targetPage switch
        {
            PositionAdjustmentPage.MainMenu => "main",
            PositionAdjustmentPage.Specialization => "specialization",
            PositionAdjustmentPage.LoadGame => "load_game",
            PositionAdjustmentPage.Credits => "credits",
            _ => "game_options"
        };

        private void TrackAppliedOffsets(PositionAdjustmentPage targetPage, PositionAdjustmentDocument document)
        {
            List<PositionAdjustmentTarget> currentTargets = PositionAdjustmentTargets.Collect(targetPage);
            for (int i = 0; i < currentTargets.Count; i++)
            {
                PositionAdjustmentTarget target = currentTargets[i];
                if (!target.CanMove)
                    continue;
                PositionAdjustmentOffset offset = document.offsets.TryGetValue(target.Id, out PositionAdjustmentOffset saved)
                    ? saved
                    : new PositionAdjustmentOffset();
                applied[target.Transform] = new PositionAppliedOffset(target.Id,
                    new Vector2(offset.x, offset.y), target.GetPosition());
            }
        }
    }
}
