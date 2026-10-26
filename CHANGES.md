# CHANGES.md — branch `gamepad-alt-function` vs upstream Xeio/MassFarming

Our branch has exactly one commit not in `upstream/master`: `b81e0ee` ("WIP save 2026-10-26"),
+154/−9 across 4 files (`git diff upstream/HEAD...HEAD`).

## Gamepad modifier rework (MassFarming.cs)
- New `GamepadButton` enum (None, AlternateFunction, triggers, bumpers, D-pad, stick
  clicks) replacing the raw `KeyboardShortcut(KeyCode.JoystickButton4)` binding for
  `ControllerPickupHotkey`.
- Split pickup vs plant modifiers:
  - pickup default = AlternateFunction (follows the game's alternate-function bind;
    Left Trigger by default) — default changed from Left Bumper because the bumper opens
    the radial menu unless a build piece is up (comment in code).
  - plant default = Left Bumper — the alternate function rotates the placement ghost,
    so it can't double as the plant modifier.
- `IsPickupHotKeyPressed` / `IsPlantHotKeyPressed` centralize the modifier checks;
  MassPickup.cs and MassPlant.cs switch to them (no more direct `.Value.MainKey` reads).

## Live planting-grid resizing (MassFarming.cs + MassPlant.cs)
- `ControllerGridGrowHotkey` / `ControllerGridShrinkHotkey` (default: right stick click)
  grow/shrink the plant grid while the plant modifier is held; `TryAdjustPlantGridSize()`
  is called from the plant Update patch. Steps of ±2 keep sizes odd (3..9) so the grid
  stays centered; stick clicks were chosen because they only cycle snap points on pieces
  that have them, and plants don't.

## README
- Documents the new defaults (Alternate Function for pickup on controller) and the
  grid-resize chord.

## Notes / TBD
- Plugin version string stays 1.13 while upstream master has bumped to 1.14 — TBD:
  whether to rebase onto upstream 1.14 (which adds "Run code formatting across files"
  and "Only track placement rotation while planting") before the next release bump.
