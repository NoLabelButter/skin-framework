# 自定义皮肤 皮肤框架 [Skin Framework]

**ButterLab · Probably Stolen / 深空当铺 · 目标游戏版本 0.46D**

[English guide](skin-guide.en.md)

Skin Framework 把界面和店铺设施的外观组织成预设。你可以从原版外观开始做自己的皮肤，也可以用随附的 Chiikawa 查看一套完整主题的结构。预设可以在游戏里切换，也可以用 ZIP 分享。

## 安装和打开设置

1. 把发行包里的 `skin_framework` 文件夹放进游戏的 `Mods` 目录，使 `Mods/skin_framework/manifest.xml` 和 `SkinFramework.dll` 位于同一目录。
2. 启动游戏，在改装体列表启用模组，按游戏提示重启，然后点齿轮打开设置。
3. 需要英文时，在 **Language / 语言** 选择 **English**，点「应用」并重启。

## 五分钟做出第一张贴图

1. 打开「预设主题」下拉框，选「＋ 新建预设」，填名称并点「创建」。新预设会立即加入列表。选中它并点「应用」。
2. 点「复制预设素材目录」，把路径粘贴到文件资源管理器。找到目标槽的分类目录。比如主界面背景用 `MainMenu/Background/`，主菜单按钮用 `MainMenu/Buttons/`。缺图提示也会显示该槽的分类。
3. 把一张 PNG 放进对应目录。例如，把 `my_background.png` 放进 `MainMenu/Background/`。
4. 在「背景贴图」输入 `my_background.png`，点「应用 → 保存」，回到主菜单查看。若游戏尚未读到刚放入或替换的 PNG，重启后再看。

可从同类原图或 Chiikawa 示例的画布比例起步。透明 PNG 的边距和透明度会影响显示，有状态切换的按钮或设施建议分别检查普通、悬停、按下及其他状态。新建预设目录中的 `ASSET_LAYOUT.txt` 会列出该分类的槽位和默认文件键。

| 填写值 | 效果 |
| --- | --- |
| `my_background.png` 或 `my_background` | 使用该分类目录中的同名贴图 |
| 留空 | 跟随当前预设的默认映射 |
| `null` | 使用游戏原版贴图 |
| `False` | 隐藏该槽对应的图像 |

修改预设后，「应用」提供**保存、不保存、另存为、取消**。保存写入当前预设，不保存丢弃本次修改，另存为生成独立副本，取消返回继续编辑。只切换预设时可直接应用。游戏默认预设的外观槽是锁定的，制作时请用自建预设。

## 编辑店铺设施和界面

- 载入店铺后按 **Ctrl+Shift+Space** 打开设施编辑器。选中设施后可查看预览、当前贴图和分类目录。点「复制贴图目录」，把 PNG 放进去。如果目录还没有建立，先按复制的路径建目录。
- 在设施编辑器填文件名并点「实时应用」。用滑块调整位置、对比度、亮度、饱和度和透明度。饱和度 100% 保持原色，0% 为灰度。「保存并关闭」将结果记入当前预设，Esc 取消本次编辑。
- 设置页的「位置调整」和「透明度调整」还能编辑主菜单、新游戏专精页、载入游戏页、制作人员名单和游戏选项界面。进入相应编辑器后按屏幕提示选择控件、调整并保存。

## 分享和导入美化包

1. 在设置里选中要分享的预设。先保存修改，再点「导入 / 导出美化包 → 导出」。
2. 点「复制目录」定位 `SkinPackages` 文件夹。分享生成的 ZIP。它包含预设配置、打包时找到的贴图和框架保存的外观调整。只分享你有权分发的素材。
3. 收到 ZIP 后，把它原样放进自己的 `SkinPackages` 文件夹，重新打开美化包窗口，选择 ZIP 并点「导入」。
4. 在预设列表中选中导入的副本并点「应用」。同名预设会自动加编号。

导入限制包括 64 MiB 整包与解压预算、单张 PNG 16 MiB、每边最多 4096 像素，以及全包最多 32 Mi 像素。文件和目录段建议用英文字母、数字、下划线或连字符。

## 路径与进阶制作

玩家预设和素材随模组共用，路径以游戏的用户数据目录为根。在 Windows 上通常位于 `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\`。

| 路径 | 内容 |
| --- | --- |
| `ModData/skin_framework/Presets/skin_presets.json` | 玩家自建预设定义 |
| `ModData/skin_framework/Textures/<预设素材目录>/<分类>/` | 玩家 PNG 和槽位说明 |
| `ModData/skin_framework/SkinPackages/` | 导入和导出的 ZIP |
| `ModConfigs/skin_framework.json` | 模组设置及按预设保存的外观调整 |
| `Mods/skin_framework/Resources/Configs/skin_presets.json` | 随包预设的定义，可作为参考 |

进阶作者可以在备份后手动编辑玩家 JSON。保留自动生成的 `id` 和 `assetFolder`，编辑名称、颜色与 `textures` 数组。用 `ASSET_LAYOUT.txt` 或随包定义确认槽 ID。例如 `{"slot":"main_menu_background","key":"my_background"}` 对应 `MainMenu/Background/my_background.png`。颜色使用 `#RRGGBB`。手动编辑 JSON 后重启游戏。

## 排查

- **缺图**：核对预设、分类目录、文件名和后缀。查看资源管理器是否隐藏了扩展名，避免把文件存成 `name.png.png`。
- **导入后画面没变化**：选中导入的副本并点「应用」。背景标记为「已隐藏」表示该槽被设置为 `False`。
- **删除后还看到目录**：设置页删除会移除预设记录和素材文件，系统可能保留空文件夹。
- **反馈问题**：附游戏版本、模组版本、操作步骤和相关日志片段。贴日志前检查是否包含本机路径或私人文件名。
