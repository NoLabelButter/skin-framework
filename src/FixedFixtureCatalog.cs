using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SkinFramework
{
    internal static class FixedFixtureCatalog
    {
        private sealed class FixtureDefinition
        {
            internal readonly string Path;
            internal readonly string Name;
            internal readonly string Group;
            internal readonly string[] Sprites;

            internal FixtureDefinition(string path, string name, string group, params string[] sprites)
            {
                Path = path;
                Name = name;
                Group = group;
                Sprites = sprites;
            }
        }

        private static readonly FixtureDefinition[] Definitions =
        {
            F("--Physical Store--/EmporiumWall", SkinLanguage.Text("墙面", "Wall"), "墙体与卷帘", "wall", "wall2"),
            F("--Physical Store--/Desk", SkinLanguage.Text("柜台", "Counter"), "柜台与桌面", "desk"),
            F("--Physical Store--/ShutterDoor", SkinLanguage.Text("卷帘门", "Roller shutter"), "墙体与卷帘", "shutter_door"),
            F("--Physical Store--/shutterFrameTop", SkinLanguage.Text("卷帘上框", "Shutter top frame"), "墙体与卷帘", "window_top"),
            F("--Physical Store--/shutterFrame", SkinLanguage.Text("卷帘边框", "Shutter frame"), "墙体与卷帘", "window"),
            F("--Physical Store--/ShutterGlassDoor", SkinLanguage.Text("玻璃门", "Glass door"), "墙体与卷帘", "glass_door"),
            F("--Physical Store--/Radio", SkinLanguage.Text("收音机", "Radio"), "墙面设备", "radio"),
            F("--Physical Store--/NutruifruitPoster", SkinLanguage.Text("墙上海报", "Wall poster"), "墙面设备", "poster", "poster_no_shadow", "poster_down"),
            F("--Physical Store--/Door Lock", SkinLanguage.Text("门锁", "Door lock"), "墙面设备", "door lock"),
            F("--Physical Store--/showcase", SkinLanguage.Text("展示柜", "Display case"), "柜台与桌面", "display_case", "display_case_medium", "display_case_large"),
            F("--Physical Store--/wires", SkinLanguage.Text("墙面电线", "Wall wires"), "墙面设备", "wires"),
            F("--Physical Store--/Table Wires", SkinLanguage.Text("桌面电线", "Desk wires"), "柜台与桌面", "wires_table"),
            F("--Physical Store--/Calendar_Wire", SkinLanguage.Text("日历电线", "Calendar wire"), "墙面设备", "calendar wire"),
            F("--Interactable/Door", SkinLanguage.Text("店门", "Shop door"), "墙体与卷帘", "door"),
            F("--Interactable/PC", SkinLanguage.Text("电脑", "Computer"), "墙面设备", "marketplace_pc"),
            F("--Interactable/PC/pc_notif", SkinLanguage.Text("电脑提示灯", "Computer indicator"), "墙面设备", "marketplace_pc_notif"),
            F("--Interactable/PC/connecting wire", SkinLanguage.Text("电脑连接线", "Computer cable"), "墙面设备", "connectimg wire"),
            F("--Interactable/Calendar", SkinLanguage.Text("日历", "Calendar"), "墙面设备", "calendar"),
            F("--Interactable/CreditDisplay", SkinLanguage.Text("信用点显示器", "Credit display"), "柜台与桌面", "credit_display"),
            F("--Interactable/wall_phone", SkinLanguage.Text("壁挂电话", "Wall phone"), "墙面设备", "phone"),
            F("--Interactable/NegociationTablet", SkinLanguage.Text("议价平板", "Negotiation tablet"), "柜台与桌面", "negociation_tablet"),
            F("--Interactable/ClientDisplay", SkinLanguage.Text("顾客显示屏", "Customer display"), "柜台与桌面", "client_display"),
            F("--Interactable/SleepingBag", SkinLanguage.Text("睡袋", "Sleeping bag"), "地面设施", "sleeping_bag"),
            F("--Interactable/feedback_box", SkinLanguage.Text("反馈箱", "Feedback box"), "墙面设备", "reputation_box"),
            F("--Interactable/transferButton", SkinLanguage.Text("转账按钮", "Transfer button"), "柜台与桌面", "button_transfer"),
            F("--Interactable/dismissButton", SkinLanguage.Text("遣散按钮", "Dismiss button"), "柜台与桌面", "button_dismiss"),
            F("--Interactable/SecButton", SkinLanguage.Text("安保按钮", "Security button"), "墙面设备", "button_sec"),
            F("--Interactable/ShutterButton", SkinLanguage.Text("卷帘门按钮", "Shutter button"), "墙面设备",
                "shutter_button_sheet_0", "shutter_button_sheet_1", "shutter_button_sheet_2"),
            F("--Interactable/Bell", SkinLanguage.Text("呼叫铃按钮", "Call bell"), "柜台与桌面",
                "bell_button_sheet_0", "bell_button_sheet_1", "bell_button_sheet_2"),
            F("--Interactable/SecButton/Light", SkinLanguage.Text("安保指示灯", "Security indicator"), "墙面设备", "sec lamps red", "sec lamps green"),
            F("--Interactable/_Shotgun", SkinLanguage.Text("霰弹枪阴影", "Shotgun shadow"), "其他设施", "shotgun_shadow"),
            F("--Interactable/_Arm", SkinLanguage.Text("手臂阴影", "Arm shadow"), "其他设施", "one_hand"),
            F("--Interactable/_Shotgun/GameObject", SkinLanguage.Text("武器底层阴影", "Weapon base shadow"), "其他设施", "shotgun_shadow_only"),
            F("--Interactable/_MagnifyingWindow", SkinLanguage.Text("放大镜窗口", "Magnifier window"), "柜台与桌面", "magnifying_screen"),
            F("--Interactable/_MagnifyingWindowCollapsed", SkinLanguage.Text("放大镜收起窗口", "Collapsed magnifier"), "柜台与桌面", "magnifying_screen"),
            F("--Interactable/ClienteleDisplay", SkinLanguage.Text("顾客信息屏动画", "Customer information display"), "柜台与桌面",
                "clientele-Sheet_0", "clientele-Sheet_1", "clientele-Sheet_2", "clientele-Sheet_3"),
            F("--Interactable/ClientDisplayTop", SkinLanguage.Text("柜台显示屏动画", "Counter display"), "柜台与桌面",
                "client_counter-Sheet_0", "client_counter-Sheet_1", "client_counter-Sheet_2", "client_counter-Sheet_3"),
            F("--Interactable/CalendarTop", SkinLanguage.Text("日历上屏动画", "Calendar display"), "墙面设备",
                "calendar_top-Sheet_0", "calendar_top-Sheet_1", "calendar_top-Sheet_2", "calendar_top-Sheet_3"),
            F("--Interactable/Recorder", SkinLanguage.Text("录音机动画", "Recorder animation"), "墙面设备",
                "tape anim-Sheet_0", "tape anim-Sheet_1", "tape anim-Sheet_2", "tape anim-Sheet_3",
                "tape anim-Sheet_4", "tape anim-Sheet_5", "tape anim-Sheet_6", "tape anim-Sheet_7",
                "tape anim-Sheet_8", "tape anim-Sheet_9")
        };

        internal static List<FixedFixtureTarget> Collect(Scene scene)
        {
            Dictionary<string, FixtureDefinition> byPath = new Dictionary<string, FixtureDefinition>(StringComparer.Ordinal);
            foreach (FixtureDefinition definition in Definitions)
                byPath.Add(definition.Path, definition);

            List<FixedFixtureTarget> result = new List<FixedFixtureTarget>();
            foreach (SpriteRenderer renderer in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                if (renderer == null || renderer.sprite == null || renderer.gameObject.scene != scene || !scene.isLoaded)
                    continue;
                string path = GetPath(renderer.transform);
                string spriteName = renderer.sprite.name;
                if (!byPath.TryGetValue(path, out FixtureDefinition definition) ||
                    Array.IndexOf(definition.Sprites, spriteName) < 0)
                {
                    continue;
                }
                result.Add(new FixedFixtureTarget(path, definition.Name, definition.Group, renderer));
            }
            AddMenuBackgroundTargets(result, scene);
            AddInteractiveImages(result, scene);
            result.Sort((left, right) => string.Compare(left.Group + left.Name, right.Group + right.Name,
                StringComparison.CurrentCulture));
            return result;
        }

        private static void AddMenuBackgroundTargets(List<FixedFixtureTarget> result, Scene scene)
        {
            foreach (Image image in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (image == null || image.gameObject.scene != scene || !scene.isLoaded)
                    continue;

                string path = GetPath(image.transform);
                string id = null;
                string name = null;
                if (path == "CanvasPaperUI/_AdvCalendar")
                {
                    id = "--Menu/Calendar";
                    name = SkinLanguage.Text("日历打开后的大菜单", "Calendar menu");
                }
                else if (path == "CanvasPaperUI/_NegociationPanel")
                {
                    id = "--Menu/NegotiationTablet";
                    name = SkinLanguage.Text("平板电脑打开后的大菜单", "Tablet menu");
                }
                else if (path == "CanvasPaperUI/_Phone")
                {
                    id = "--Menu/Phone";
                    name = SkinLanguage.Text("电话打开后的大菜单", "Phone menu");
                }

                if (id == null)
                    continue;
                if (image.sprite == null)
                {
                    Debug.LogWarning("[Skin Framework] 菜单贴图目标尚无 Sprite，暂不添加：" + path + ".");
                    continue;
                }
                result.Add(new FixedFixtureTarget(id, name, "界面菜单", new[] { image }, true));
                Debug.Log("[Skin Framework] 菜单贴图目标已绑定：id=" + id + ", path=" + path +
                    ", sprite=" + image.sprite.name + ", active=" + image.gameObject.activeInHierarchy + ".");
            }
        }

        private static void AddInteractiveImages(List<FixedFixtureTarget> result, Scene scene)
        {
            EmporiumEntry shop = EmporiumEntry.Instance;
            if (shop == null)
                return;
            AddItemImages(result, scene, shop.faucet, "--FixtureImages/Faucet", SkinLanguage.Text("水龙头", "Faucet"), "地面设施", "faucet", "faucet1");
            AddItemImages(result, scene, shop.drain, "--FixtureImages/Drain", SkinLanguage.Text("排水口", "Drain"), "地面设施", "drain");
            AddItemImages(result, scene, shop.trashcan, "--FixtureImages/Trashcan", SkinLanguage.Text("废纸篓", "Trash can"), "地面设施",
                "trashcan", "trashcan0", "trashcan25", "trashcan50", "trashcan75", "trashcan100");
            AddItemImages(result, scene, shop.cassettePlayer, "--FixtureImages/CassettePlayer", SkinLanguage.Text("磁带播放器", "Cassette player"), "柜台与桌面", "cassette_player");
            if (shop.docInvElement != null && shop.docInvElement.childItem == shop.dossier)
                AddItemImages(result, scene, shop.dossier, "--FixtureImages/Dossier", SkinLanguage.Text("档案夹", "File box"), "柜台与桌面",
                    "dossier_0", "dossier_1", "dossier_2", "dossier_3");
            StoreShutterButton shutter = StoreShutterButton.Instance;
            if (shutter != null && shutter.blockerGameObject != null)
            {
                Image image = shutter.blockerGameObject.GetComponent<Image>();
                AddImages(result, scene, "--FixtureImages/ShutterBlocker", SkinLanguage.Text("卷帘遮挡图", "Shutter occlusion layer"), "墙体与卷帘",
                    new[] { image }, "shutter_door_cropped_2x");
            }
        }

        private static void AddItemImages(List<FixedFixtureTarget> result, Scene scene, GameItem item,
            string id, string name, string group, params string[] sprites)
        {
            GameItemElement element = item as GameItemElement;
            if (element == null)
                return;
            Image background = element.background == null ? null : element.background.image;
            Image fakeBackground = element.fakeBackground == null ? null : element.fakeBackground.image;
            AddImages(result, scene, id, name, group, new[] { background, fakeBackground }, sprites);
        }

        private static void AddImages(List<FixedFixtureTarget> result, Scene scene, string id,
            string name, string group, Image[] candidates, params string[] sprites)
        {
            List<Image> matched = new List<Image>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                Image image = candidates[i];
                if (image != null && image.sprite != null && image.gameObject.scene == scene &&
                    Array.IndexOf(sprites, image.sprite.name) >= 0)
                    matched.Add(image);
            }
            if (matched.Count > 0)
                result.Add(new FixedFixtureTarget(id, name, group, matched.ToArray()));
        }

        private static FixtureDefinition F(string path, string name, string group, params string[] sprites) =>
            new FixtureDefinition(path, name, group, sprites);

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
