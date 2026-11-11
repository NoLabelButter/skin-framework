using System;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinFramework
{
    internal enum PositionAdjustmentPage { None, MainMenu, Specialization, LoadGame, Credits, GameOptions }

    internal sealed class PositionAdjustmentTarget
    {
        public readonly string Id;
        public readonly string Label;
        public readonly Transform Transform;
        public readonly RectTransform Rect;
        public readonly Graphic Graphic;
        public readonly SpriteRenderer SpriteRenderer;

        public PositionAdjustmentTarget(string id, string label, Transform transform)
        {
            Id = id;
            Label = label;
            Transform = transform;
            Rect = transform as RectTransform;
            Graphic = transform.GetComponent<Graphic>();
            SpriteRenderer = transform.GetComponent<SpriteRenderer>();
        }

        public bool CanMove => Rect != null || SpriteRenderer != null;

        public Vector2 GetPosition() => Rect != null
            ? Rect.anchoredPosition
            : (Vector2)Transform.localPosition;

        public void SetPosition(Vector2 position)
        {
            if (Rect != null)
                Rect.anchoredPosition = position;
            else
            {
                Vector3 localPosition = Transform.localPosition;
                Transform.localPosition = new Vector3(position.x, position.y, localPosition.z);
            }
        }
    }

    internal sealed class PositionAppliedOffset
    {
        public readonly string Id;
        public readonly Vector2 Delta;
        public readonly Vector2 AppliedPosition;
        public PositionAppliedOffset(string id, Vector2 delta, Vector2 appliedPosition)
        {
            Id = id;
            Delta = delta;
            AppliedPosition = appliedPosition;
        }
    }

    internal sealed class PositionAdjustmentDocument
    {
        public int schemaVersion { get; set; } = 1;
        public Dictionary<string, PositionAdjustmentOffset> offsets { get; set; } =
            new Dictionary<string, PositionAdjustmentOffset>(StringComparer.Ordinal);

        public string Serialize() => JsonConvert.SerializeObject(this);

        public static PositionAdjustmentDocument Read(string json, string key)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new PositionAdjustmentDocument();
            try
            {
                PositionAdjustmentDocument document = JsonConvert.DeserializeObject<PositionAdjustmentDocument>(json);
                if (document == null || document.schemaVersion != 1 || document.offsets == null)
                {
                    Debug.LogError($"[Skin Framework] 配置 {key} 的位置数据版本或结构无效，忽略该栏偏移；原配置文本保留。");
                    return new PositionAdjustmentDocument();
                }
                return document;
            }
            catch (JsonException exception)
            {
                Debug.LogError($"[Skin Framework] 配置 {key} 的位置 JSON 无法解析，忽略该栏偏移；原配置文本保留：{exception.Message}");
                return new PositionAdjustmentDocument();
            }
        }
    }

    internal sealed class PositionAdjustmentOffset
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    internal static class PositionAdjustmentTargets
    {
        private static readonly System.Reflection.FieldInfo LoadGameFrameField =
            AccessTools.Field(typeof(LoadGameUI), "frame");
        private static readonly System.Reflection.FieldInfo CreditsFrameField =
            AccessTools.Field(typeof(CreditsUI), "frame");

        internal static LoadGameFrame GetLoadGameFrame() => LoadGameUI.IsOpen
            ? (LoadGameFrame)LoadGameFrameField.GetValue(null)
            : null;

        internal static CreditsFrame GetCreditsFrame() => CreditsUI.IsOpen
            ? (CreditsFrame)CreditsFrameField.GetValue(null)
            : null;

        public static List<PositionAdjustmentTarget> Collect(PositionAdjustmentPage page)
        {
            List<PositionAdjustmentTarget> result = new List<PositionAdjustmentTarget>();
            if (page == PositionAdjustmentPage.MainMenu)
            {
                Transform canvas = FindNamed(SceneManager.GetActiveScene(), "Canvas");
                Transform layout = canvas == null ? null : canvas.Find("LayoutGroup");
                Transform background = FindNamed(SceneManager.GetActiveScene(), "MenuBackgroundNew");
                AddVisual(result, "main.background", "主页面 / 背景贴图", background);
                if (layout != null)
                {
                    AddControl(result, "main.new_game", "主页面 / 新游戏按钮", layout.Find("Button"));
                    AddControl(result, "main.load_game", "主页面 / 载入游戏按钮", layout.Find("Button (1)"));
                    AddControl(result, "main.options", "主页面 / 选项按钮", layout.Find("Button (2)"));
                    AddControl(result, "main.credits", "主页面 / 制作人员按钮", layout.Find("Button (3)"));
                    AddControl(result, "main.mods", "主页面 / 改装体按钮", layout.Find("ButtonMod"));
                    AddControl(result, "main.quit", "主页面 / 退出游戏按钮", layout.Find("Button (4)"));
                }
                AddControl(result, "main.language", "主页面 / 语言栏", canvas == null ? null : canvas.Find("LanguageSelectorDemo"));
                CollectMainMenuButtonImages(result, layout);
                CollectLanguageImages(result, canvas);
            }
            else if (page == PositionAdjustmentPage.Specialization)
            {
                GameObject root = GameObject.Find("custom_window_new_game_specialization");
                NewGameSpecializationFrame frame = root == null ? null : root.GetComponent<NewGameSpecializationFrame>();
                if (frame == null)
                    return result;

                AddControl(result, "specialization.header", "专精页面 / 专精标题", frame.headerTitle == null ? null : frame.headerTitle.transform);
                AddControl(result, "specialization.scenario.title", "专精页面 / 详情标题文本",
                    frame.title == null ? null : frame.title.transform);
                AddControl(result, "specialization.scenario.tag", "专精页面 / 详情标签文本",
                    frame.tag == null ? null : frame.tag.transform);
                AddControl(result, "specialization.scenario.description", "专精页面 / 详情描述文本",
                    frame.description == null ? null : frame.description.transform);
                AddControl(result, "specialization.starting.label", "专精页面 / 初始拥有标题",
                    frame.startWithLabel == null ? null : frame.startWithLabel.transform);
                AddControl(result, "specialization.starting.items", "专精页面 / 初始拥有条目列表",
                    frame.bulletsHost);
                if (frame.stripSlots != null)
                {
                    for (int i = 0; i < frame.stripSlots.childCount; i++)
                    {
                        Transform slot = frame.stripSlots.GetChild(i);
                        string scenarioId = GetScenarioId(slot);
                        AddControl(result, "specialization.slot." + scenarioId, "专精页面 / 专精项 " + scenarioId, slot);
                        AddVisual(result, "specialization.slot." + scenarioId + ".icon", "专精页面 / 专精图标 " + scenarioId,
                            slot.Find("icon"));
                    }
                }
                AddControl(result, "specialization.previous", "专精页面 / 左切换", frame.prevButton == null ? null : frame.prevButton.transform);
                AddControl(result, "specialization.next", "专精页面 / 右切换", frame.nextButton == null ? null : frame.nextButton.transform);
                AddControl(result, "specialization.back", "专精页面 / 返回按钮", frame.backButton == null ? null : frame.backButton.transform);
                AddControl(result, "specialization.continue", "专精页面 / 继续按钮", frame.continueButton == null ? null : frame.continueButton.transform);
                AddVisual(result, "specialization.window", "专精页面 / 外框贴图", root.transform);
                AddVisual(result, "specialization.panel", "专精页面 / 内容底板贴图",
                    frame.panel == null ? null : frame.panel.Find("Background"));
                AddVisual(result, "specialization.separator.top", "专精页面 / 上分隔线贴图", FindPanel(frame, "Separator1"));
                AddVisual(result, "specialization.separator.middle", "专精页面 / 中分隔线贴图", FindPanel(frame, "Separator2"));
                AddVisual(result, "specialization.separator.bottom", "专精页面 / 下分隔线贴图", FindPanel(frame, "SeparatorBottom"));
                AddVisual(result, "specialization.previous.image", "专精页面 / 左翻页贴图", frame.prevImage == null ? null : frame.prevImage.transform);
                AddVisual(result, "specialization.back.image", "专精页面 / 返回按钮贴图",
                    frame.backButton == null || frame.backButton.targetGraphic == null ? null : frame.backButton.targetGraphic.transform);
                AddVisual(result, "specialization.next.image", "专精页面 / 右翻页贴图", frame.nextImage == null ? null : frame.nextImage.transform);
                AddVisual(result, "specialization.continue.frame.outer", "专精页面 / 继续按钮外框贴图", frame.continueOuter == null ? null : frame.continueOuter.transform);
                AddVisual(result, "specialization.continue.frame.inner", "专精页面 / 继续按钮内框贴图", frame.continueInner == null ? null : frame.continueInner.transform);
            }
            else if (page == PositionAdjustmentPage.LoadGame)
            {
                LoadGameFrame frame = GetLoadGameFrame();
                if (frame == null)
                    return result;

                Transform[] descendants = frame.transform.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < descendants.Length; i++)
                {
                    Transform target = descendants[i];
                    if (target.GetComponent<Graphic>() == null && target.GetComponent<SpriteRenderer>() == null)
                        continue;
                    string path = LoadGamePath(frame.transform, target);
                    AddTarget(result, "load_game." + path, "载入游戏 / " + path.Replace('.', ' '), target);
                }
            }
            else if (page == PositionAdjustmentPage.Credits)
            {
                CreditsFrame frame = GetCreditsFrame();
                if (frame == null)
                    return result;

                if (frame.scrollbar != null)
                {
                    Transform scrollbar = frame.scrollbar.transform;
                    AddControl(result, "credits." + HierarchyPath(frame.transform, scrollbar),
                        "制作人员名单 / 滚动条轨道", scrollbar);
                    Transform handle = frame.scrollbar.handleRect;
                    if (handle != null)
                        AddControl(result, "credits." + HierarchyPath(frame.transform, handle),
                            "制作人员名单 / 滚动滑块", handle);
                }

                Transform[] descendants = frame.transform.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < descendants.Length; i++)
                {
                    Transform target = descendants[i];
                    if (target.GetComponent<Graphic>() == null && target.GetComponent<SpriteRenderer>() == null)
                        continue;
                    string path = HierarchyPath(frame.transform, target);
                    AddTarget(result, "credits." + path, CreditsLabel(frame.transform, target, path), target);
                }
                for (int i = 0; i < frame.content.childCount; i++)
                {
                    Transform section = frame.content.GetChild(i);
                    if (section.name != "TeamRows") continue;
                    for (int rowIndex = 0; rowIndex < section.childCount; rowIndex++)
                    {
                        Transform row = section.GetChild(rowIndex);
                        AddControl(result, "credits.team.row." + rowIndex,
                            "制作人员名单 / 开发团队行 " + (rowIndex + 1), row);
                    }
                }
            }
            else if (page == PositionAdjustmentPage.GameOptions)
            {
                CustomUIManager manager = CustomUIManager.Instance;
                if (manager == null)
                    return result;
                CustomUIWindow window = manager.GetWindow(GameOptionUIManager.WINDOW_ID);
                if (window == null || !window.IsAlive)
                    return result;

                AddControl(result, "game_options.window", "选项界面 / 整体窗口", window.root.transform);
                if (window.frame != null)
                {
                    AddControl(result, "game_options.title_bar", "选项界面 / 标题栏", window.frame.titleText == null
                        ? null
                        : window.frame.titleText.transform.parent);
                    AddVisual(result, "game_options.title_text", "选项界面 / 标题文字",
                        window.frame.titleText == null ? null : window.frame.titleText.transform);
                    AddControl(result, "game_options.close", "选项界面 / 关闭按钮",
                        window.frame.closeButton == null ? null : window.frame.closeButton.transform);
                }

                Transform scrollRoot = FindGameOptionsElement(window, "scroll");
                AddControl(result, "game_options.scroll", "选项界面 / 滚动内容区域",
                    scrollRoot);
                ScrollRect scrollRect = scrollRoot == null ? null : scrollRoot.GetComponentInChildren<ScrollRect>(true);
                if (scrollRect != null && scrollRect.verticalScrollbar != null)
                {
                    AddControl(result, "game_options.scrollbar.track", "选项界面 / 滚动条轨道",
                        scrollRect.verticalScrollbar.transform);
                    AddControl(result, "game_options.scrollbar.thumb", "选项界面 / 滚动条滑块",
                        scrollRect.verticalScrollbar.handleRect);
                }

                Transform[] allElements = window.root.GetComponentsInChildren<Transform>(true);
                List<Transform> separators = new List<Transform>();
                for (int i = 0; i < allElements.Length; i++)
                    if (allElements[i].name == "separator")
                        separators.Add(allElements[i]);
                int separatorIndex = separators.Count;
                int visibleSectionIndex = 0;
                foreach (GameOptionRegistry.Section section in GameOptionRegistry.Sections)
                {
                    if (section.entries.Count == 0)
                        continue;
                    if (visibleSectionIndex < separatorIndex)
                        AddControl(result, "game_options.separator.section." + section.id,
                            "选项界面 / " + GetSectionLabel(window, section) + "分隔线",
                            separators[visibleSectionIndex]);
                    visibleSectionIndex++;
                }
                for (int i = visibleSectionIndex; i < separatorIndex; i++)
                {
                    string id = i == visibleSectionIndex
                        ? "game_options.separator.apply"
                        : "game_options.separator.extra." + (i - visibleSectionIndex);
                    string label = i == visibleSectionIndex
                        ? "选项界面 / 应用区域分隔线"
                        : "选项界面 / 额外分隔线 " + (i - visibleSectionIndex + 1);
                    AddControl(result, id, label, separators[i]);
                }

                foreach (GameOptionRegistry.Section section in GameOptionRegistry.Sections)
                {
                    if (section.entries.Count == 0)
                        continue;
                    CustomUIElement heading = window.Get("section_" + section.id);
                    string sectionLabel = GetVisibleText(heading, section.id);
                    AddControl(result, "game_options.section." + section.id,
                        "选项界面 / 分组标题：" + sectionLabel,
                        heading == null ? null : heading.root.transform);
                    foreach (GameOptionRegistry.Entry entry in section.entries)
                    {
                        CustomUIElement label = window.Get("label_" + entry.id);
                        CustomUIElement control = window.Get(entry.id);
                        string rowLabel = GetVisibleText(label, entry.id);
                        AddControl(result, "game_options.option." + entry.id + ".label",
                            "选项界面 / " + rowLabel + " / 标签", label == null ? null : label.root.transform);
                        AddControl(result, "game_options.option." + entry.id + ".control",
                            "选项界面 / " + rowLabel + " / 控件", control == null ? null : control.root.transform);
                    }
                }

                CustomUIElement apply = window.Get("apply");
                AddControl(result, "game_options.apply", "选项界面 / 应用按钮",
                    apply == null ? null : apply.root.transform);
            }
            return result;
        }

        private static string LoadGamePath(Transform root, Transform target)
        {
            return HierarchyPath(root, target);
        }

        internal static Transform FindGameOptionsElement(CustomUIWindow window, string elementName)
        {
            Transform[] descendants = window.root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
                if (descendants[i].name == elementName)
                    return descendants[i];
            return null;
        }

        private static string GetVisibleText(CustomUIElement element, string fallback)
        {
            if (element == null || element.root == null)
                return fallback;
            TMP_Text text = element.root.GetComponentInChildren<TMP_Text>(true);
            return text == null || string.IsNullOrWhiteSpace(text.text) ? fallback : text.text;
        }

        private static string GetSectionLabel(CustomUIWindow window, GameOptionRegistry.Section section)
        {
            CustomUIElement heading = window.Get("section_" + section.id);
            return GetVisibleText(heading, section.id) + " / ";
        }

        private static string HierarchyPath(Transform root, Transform target)
        {
            Stack<string> segments = new Stack<string>();
            Transform current = target;
            while (current != root)
            {
                segments.Push(current.name.Replace('.', '_') + "_" + current.GetSiblingIndex());
                current = current.parent;
            }
            return segments.Count == 0 ? "Frame" : string.Join(".", segments.ToArray());
        }

        private static string CreditsLabel(Transform root, Transform target, string path)
        {
            switch (target.name)
            {
                case "Credits": return "制作人员名单 / 窗口外框";
                case "Background": return "制作人员名单 / 内容底板";
                case "BackButton": return "制作人员名单 / 关闭按钮";
                case "ColumnScroll": return "制作人员名单 / 滚动视口";
                case "Track": return "制作人员名单 / 滚动槽";
                case "Handle": return "制作人员名单 / 滚动滑块";
                case "Border": return "制作人员名单 / 滑块边框";
                case "HeaderTitle": return "制作人员名单 / 页头标题";
                case "SectionTitle": return "制作人员名单 / 分节标题 " + (target.GetSiblingIndex() + 1);
                case "Rule": return "制作人员名单 / 分隔线 " + (target.GetSiblingIndex() + 1);
                case "Role": return "制作人员名单 / 开发团队角色 " + (target.parent.GetSiblingIndex() + 1);
                case "Name": return "制作人员名单 / 开发团队姓名 " + (target.parent.GetSiblingIndex() + 1);
                case "Acknowledgement": return "制作人员名单 / 特别鸣谢";
                case "Community": return "制作人员名单 / 社区贡献者名单";
                case "CommunityNote": return "制作人员名单 / 社区说明";
                default: return "制作人员名单 / " + path.Replace('.', ' ');
            }
        }

        public static void Apply(PositionAdjustmentPage page, PositionAdjustmentDocument document,
            Dictionary<Transform, PositionAppliedOffset> applied)
        {
            List<PositionAdjustmentTarget> targets = Collect(page);
            for (int i = 0; i < targets.Count; i++)
            {
                PositionAdjustmentTarget target = targets[i];
                if (!target.CanMove || applied.ContainsKey(target.Transform))
                    continue;
                PositionAdjustmentOffset offset = document.offsets.TryGetValue(target.Id, out PositionAdjustmentOffset saved)
                    ? saved
                    : new PositionAdjustmentOffset();
                Vector2 delta = new Vector2(offset.x, offset.y);
                target.SetPosition(target.GetPosition() + delta);
                applied.Add(target.Transform, new PositionAppliedOffset(target.Id, delta, target.GetPosition()));
            }
        }

        public static string GetScenarioId(Transform slot) => slot.name.StartsWith("slot_", StringComparison.Ordinal)
            ? slot.name.Substring("slot_".Length)
            : slot.name;

        private static void AddControl(List<PositionAdjustmentTarget> targets, string id, string label, Transform transform)
        {
            if (transform is RectTransform)
                AddTarget(targets, id, label, transform);
        }

        private static void AddVisual(List<PositionAdjustmentTarget> targets, string id, string label, Transform transform)
        {
            if (transform == null)
                return;
            if (transform.GetComponent<Graphic>() == null && transform.GetComponent<SpriteRenderer>() == null)
                return;
            AddTarget(targets, id, label, transform);
        }

        private static void AddTarget(List<PositionAdjustmentTarget> targets, string id, string label, Transform transform)
        {
            for (int i = 0; i < targets.Count; i++)
                if (targets[i].Transform == transform)
                    return;
            targets.Add(new PositionAdjustmentTarget(id, label, transform));
        }

        private static void CollectMainMenuButtonImages(List<PositionAdjustmentTarget> targets, Transform layout)
        {
            if (layout == null)
                return;
            string[] ids = { "new_game", "load_game", "options", "credits", "mods", "quit" };
            string[] names = { "Button", "Button (1)", "Button (2)", "Button (3)", "ButtonMod", "Button (4)" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform buttonTransform = layout.Find(names[i]);
                Button button = buttonTransform == null ? null : buttonTransform.GetComponent<Button>();
                Graphic image = button == null ? null : button.targetGraphic;
                AddVisual(targets, "main." + ids[i] + ".image", "主页面 / " + ids[i] + "按钮贴图",
                    image == null ? null : image.transform);
            }
        }

        private static void CollectLanguageImages(List<PositionAdjustmentTarget> targets, Transform canvas)
        {
            if (canvas == null)
                return;
            Transform dropdown = canvas.Find("LanguageSelectorDemo");
            if (dropdown == null || dropdown.GetComponent<TMP_Dropdown>() == null)
                dropdown = canvas.Find("LanguageSelector");
            TMP_Dropdown component = dropdown == null ? null : dropdown.GetComponent<TMP_Dropdown>();
            if (component == null)
                return;
            string prefix = "main.language.";
            Graphic closed = component.targetGraphic;
            AddVisual(targets, prefix + "closed", "主页面 / 语言栏关闭背景", closed == null ? null : closed.transform);
            Transform template = component.template;
            AddVisual(targets, prefix + "panel", "主页面 / 语言下拉面板", template);
            Transform item = template == null ? null : template.Find("Viewport/Content/Item");
            Transform itemBackground = item == null ? null : item.Find("Item Background");
            AddVisual(targets, prefix + "row", "主页面 / 语言选项底图", itemBackground);
            AddVisual(targets, prefix + "selected", "主页面 / 当前语言高亮", itemBackground == null ? null : itemBackground.Find("SkinFrameworkSelected"));
            Transform scrollbar = template == null ? null : template.Find("Scrollbar");
            AddVisual(targets, prefix + "scroll.track", "主页面 / 语言滚动槽", scrollbar);
            Transform handle = scrollbar == null ? null : scrollbar.Find("Sliding Area/Handle");
            AddVisual(targets, prefix + "scroll.thumb", "主页面 / 语言滚动块", handle);
        }

        private static Transform FindPanel(NewGameSpecializationFrame frame, string name)
        {
            return frame.panel == null ? null : frame.panel.Find(name);
        }

        private static Transform FindNamed(Scene scene, string name)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] descendants = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < descendants.Length; j++)
                    if (descendants[j].name == name)
                        return descendants[j];
            }
            return null;
        }
    }
}
