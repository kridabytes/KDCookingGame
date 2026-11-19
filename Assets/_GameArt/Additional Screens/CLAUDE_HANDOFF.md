# Additional Screens Sprite Update Handoff

Update the existing Unity cooking game screens using the visual references in `Demos/demo/`. These assets are implementation references for the existing game. Do not build a prototype, replace gameplay, or change current screen navigation, economy, timers, localization, rewards, purchase flows, or other game logic.

## References and asset folders

| Existing screen | Reference | Sprite asset |
|---|---|---|
| Language selection | `Demos/demo/language-selection-screen.png` | `Language Selection/Language_Selection_Sprite_Sheet.png` |
| Shop | `Demos/demo/shop-screen.png` | `Shop/Shop_Sprite_Sheet.png` |
| Daily tasks | `Demos/demo/daily-tasks-screen.png` | `Daily Tasks/Daily_Tasks_Sprite_Sheet.png` |
| Achievements | `Demos/demo/achievements-screen.png` | `Achievements/Achievements_Sprite_Sheet.png` |
| Free gift | `Demos/demo/free-gift-screen.png` | `Free Gift/Free_Gift_Sprite_Sheet.png` |
| Shared top currency HUD | Any of the above | `Common UI/Currency_HUD_Sprite_Sheet.png` |
| Shared chef guide | Any of the above | `Common UI/Chef_Character.png` |
| Coastal scene background | Screens using this common setting | `Common UI/Coastal_Game_Background.png` |

The existing special-offer screen is already documented and has assets in `Demos/Extracted Sprites/Special Offer Screen/CLAUDE_HANDOFF.md`; reuse those rather than rebuilding that screen from this batch.

## Integration requirements

1. Inspect the current scene, prefabs, and screen controllers first. Update the existing screens and preserve all current bindings and logic.
2. Keep every label, amount, timer, progress value, localized language name, price, and button copy as live Unity UI text. The sprite sheets intentionally use blank UI surfaces where practical.
3. Keep interactive button/container backgrounds separate from their text and runtime states. Use the small standalone icons as image layers when they need independent animation.
4. Import the PNGs as UI sprites with alpha where available, suitable filtering, no compression artifacts, and no mipmaps for screen-space UI. Slice sprite sheets manually where automatic slicing would cut into outlines, glows, or drop shadows. Preserve the original aspect ratios and transparent padding.
5. Build responsive landscape layouts. Keep interactive controls within safe areas and scale panels, cards, and spacing to the existing reference composition without forcing a new aspect ratio.
6. Do not move these demo source files into the live Unity asset tree. Copy the selected sprites into the project's established runtime asset location and keep their names and source folder mapping traceable.

## Source quality note

These PNGs are visual sprite-sheet drafts. Several sheets currently have a flat colored backdrop baked into the image rather than transparent pixels. `Currency_HUD_Sprite_Sheet.png` has transparency; the other generated composite sheets and `Chef_Character.png` currently do not. Before using those in production, isolate the sprite elements onto transparent layers or use the reference images to rebuild/slice the UI surfaces in the existing UI system. Do not treat a checker pattern in a preview as proof of alpha; verify the PNG alpha channel in the import inspector.

The full-scene background is intentionally opaque and should be used as a background layer, not sliced as transparent sprites.
