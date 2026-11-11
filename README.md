# 自定义皮肤 皮肤框架 [Skin Framework]

**ButterLab · Probably Stolen / 深空当铺 · 1.0**

制作和分享主菜单、游戏界面及店铺设施皮肤。
Create and share skins for menus and shop fixtures.

| 语言 / Language | 从这里开始 / Start here |
| --- | --- |
| 简体中文 | [安装、贴图制作、设施编辑、ZIP 分享与排查](docs/skin-guide.zh-CN.md) |
| English | [Installation, texture authoring, fixture editing, ZIP sharing, and troubleshooting](docs/skin-guide.en.md) |

## 从源码构建 / Build from source

需要 .NET SDK 和你已安装的目标游戏 0.46D。将 `<game folder>` 替换成游戏安装目录。
Requires the .NET SDK and a local installation of game version 0.46D. Replace `<game folder>` with your installation directory.

```powershell
dotnet build .\SkinFramework.csproj -c Release "-p:GameManagedDir=<game folder>\Probably Stolen_Data\Managed"
```

构建输出在 `bin/Release/netstandard2.1/`。运行时将该目录里的 `SkinFramework.dll`、`manifest.xml` 和 `Resources/Configs/` 放到游戏的 `Mods/skin_framework/` 对应位置。
Build output goes to `bin/Release/netstandard2.1/`. Copy the DLL, manifest, and `Resources/Configs/` into the corresponding locations under the game's `Mods/skin_framework/` folder.

## 源码和示例素材 / Source and example artwork

本仓库包含框架源码和预设配置。教程里的 Chiikawa 图片示例对应另行分发的模组包。
This repository contains the framework source and preset definitions. The Chiikawa artwork referenced by the guides is supplied with the separately distributed mod package.

