# Skin Framework

**ButterLab · Probably Stolen · Target game version 0.46D**

[简体中文教程](skin-guide.zh-CN.md)

Skin Framework organizes menu and shop appearance into presets. Start with the original game appearance to create your own skin, or explore the included Chiikawa preset to see how a theme is assembled. Switch presets in the game and share them as ZIP skin packs.

## Install and open the settings

1. Place the release's `skin_framework` folder in the game's `Mods` folder. The manifest and DLL should sit together at `Mods/skin_framework/manifest.xml` and `Mods/skin_framework/SkinFramework.dll`.
2. Launch the game, enable the mod in the Mods menu, and restart if prompted. Use its gear button to open Settings.
3. For English, choose **English** under **Language / 语言**, click **Apply**, and restart.

## Make your first texture

1. Open **Skin preset**, choose **＋ New Preset**, enter a name, and click **Create**. Select the new preset and click **Apply**.
2. Click **Copy Asset Folder** and paste the path into File Explorer. Open the category for the texture slot. The main menu background uses `MainMenu/Background/`, and the main menu buttons use `MainMenu/Buttons/`. A missing texture hint also shows the category.
3. Put a PNG in that category. For example, place `my_background.png` in `MainMenu/Background/`.
4. Enter `my_background.png` in **Background texture**, then choose **Apply → Save**. Return to the main menu to see the result. Restart if the game has yet to pick up a newly added or replaced PNG.

Use a comparable original or Chiikawa image as a starting point for canvas proportions. Transparent margins affect placement. For assets with multiple states, check the normal, hover, pressed, and other applicable appearances. The generated `ASSET_LAYOUT.txt` lists the slots and default file keys for its category.

| Field value | Effect |
| --- | --- |
| `my_background.png` or `my_background` | Loads that texture from the slot's category folder |
| Blank | Follows the preset's default mapping |
| `null` | Restores the game's original texture |
| `False` | Hides the image for that slot |

When you edit a preset, **Apply** offers **Save**, **Discard Changes**, **Save As**, and **Cancel**. Save updates the active preset, Discard Changes restores its saved values, Save As makes a separate copy, and Cancel returns to editing. Switching presets alone applies directly. The **Game Default** appearance is locked, so create a preset to edit it.

## Edit shop fixtures and menus

- Load into the shop and press **Ctrl+Shift+Space** to open the fixture editor. Select a fixture to see its preview, texture reference, and category. Click **Copy Folder**, create that folder if necessary, and put your PNG there.
- Enter the filename and click **Apply Preview**. The editor supports position, contrast, brightness, saturation, and opacity. Saturation at 100% preserves the original color and 0% gives grayscale. **Save & Close** saves changes to the active preset. Esc discards the current editing session.
- The **Position adjustment** and **Opacity adjustment** settings cover the main menu, New Game specialization, Load Game, credits, and game options. Open the relevant editor and follow its on-screen controls to select, adjust, and save a UI element.

## Share or import a skin pack

1. Select the preset to share and save your edits. Open **Import / Export Skin Pack** and click **Export**.
2. Click **Copy Folder** to find the `SkinPackages` folder. Share the resulting ZIP. It contains the preset, textures found during export, and the appearance adjustments saved by Skin Framework. Share assets you have permission to redistribute.
3. To install a pack, place its ZIP in your own `SkinPackages` folder. Reopen the pack window, select the ZIP, and click **Import**.
4. Select the imported copy in the preset dropdown and click **Apply**. Duplicate names receive a numbered suffix.

Import limits include a 64 MiB archive and decompression budget, 16 MiB per PNG, up to 4096 pixels per side, and 32 Mi pixels across all textures. Use letters, digits, underscores, or hyphens in asset paths for portability.

## Paths and advanced authoring

Presets are shared across save slots. On Windows, the game's user data root is typically `%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\`.

| Path | Contents |
| --- | --- |
| `ModData/skin_framework/Presets/skin_presets.json` | Player preset definitions |
| `ModData/skin_framework/Textures/<preset asset folder>/<category>/` | Player PNGs and slot notes |
| `ModData/skin_framework/SkinPackages/` | Imported and exported ZIP packs |
| `ModConfigs/skin_framework.json` | Mod settings and per-preset appearance adjustments |
| `Mods/skin_framework/Resources/Configs/skin_presets.json` | Included preset definitions for reference |

To edit a player preset directly, back up its JSON and retain its generated `id` and `assetFolder`. You can edit the name, colors, and `textures` array. Find slot IDs in `ASSET_LAYOUT.txt` or the bundled definitions. For example, `{"slot":"main_menu_background","key":"my_background"}` points to `MainMenu/Background/my_background.png`. Colors use `#RRGGBB`. Restart after editing JSON manually.

## Troubleshooting

- **Missing texture**: Check the preset, category folder, filename, and extension. File Explorer may hide extensions, so check for accidental names such as `name.png.png`.
- **The imported skin looks unchanged**: Select its new copy and click **Apply**. A **Hidden** background is a slot set to `False`.
- **A folder remains after deletion**: The settings menu removes the preset record and its asset files. Empty folders may remain.
- **Reporting a problem**: Include the game version, mod version, reproduction steps, and the relevant log excerpt. Check the excerpt for local paths or private filenames before sharing it.
