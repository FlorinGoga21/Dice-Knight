# Level upgrade pixel artwork

## Approved clover replacement (28 October 2026)

`Clover-reference-pixel.png` is the approved generated source; its prompt and
provenance are recorded in `Clover-reference-pixel.txt`. The game consumes
`Assets/Art/UI/Leveling/CloverReferencePixel.png`, a 40x40 nearest-neighbor
reduction with binary alpha. Import settings are Point, uncompressed, no mipmaps,
and 64 PPU; this preserves the former clover's 0.625-unit sprite bounds for
existing Lucky combat feedback without runtime scale changes.

Only the Luck button's Icon sprite and PlayerLevelUI.luckIcon were reassigned.
The user's heart and sword choices, UI layout, and original clover PNGs remain.
The optional local installer is `Tools/Unity/InstallApprovedClover.cs`.
The upgrade appearance was inspected in a disposable Editor preview scene.

## Current wood-and-brass revision (28 October 2026)

The leveling prefab now uses new project-owned pixel sprites rather than the
original kit copies. The earlier files below remain intact for reference.

- `UpgradeWood`, `UpgradeWoodFocus`, `UpgradeWoodPressed`: 36x36 walnut panels,
  brass trim, corner pins, plank seams, and a brighter focus edge / darker press.
- `UpgradeVitality`, `UpgradeFortune`, `UpgradeMight`: 20x20 transparent icons.
  The half heart preserves the +10 HP meaning; the clover has four outlined
  leaves and a bent stem; the sword uses pale steel and a brass crossguard.
- `UpgradeBadge` (9x9) and `UpgradePlus` (7x7): small lower-right upgrade badges.
- `ExperienceWood` (104x12) and `ExperienceTeal` (96x6): framed, shaded XP track.
  The highlight is part of the filled sprite, so it also disappears at zero XP.
- `LevelCaption` (16x16, sliced): dark label backings with brass end marks for
  readability over forest light shafts. The existing Hearth bitmap font remains.

Palette roles: outline #201C25, trim #BD9458, walnut #6B4B36, pale text #FFE3AD,
teal fill #38897F / #66BBA6 / #B0E4CA. All new PNGs retain 32 PPU, Point filtering,
uncompressed alpha, no mipmaps. Sliced frame images use a 3.125 pixels-per-unit
multiplier to reconcile the Canvas's 100 reference PPU with the 32 PPU artwork.

Authored through Unity texture/importer/prefab APIs using the optional local
`Tools/Unity/RestylePlayerLevelUI.cs` recipe. Existing PNGs, GUIDs, fonts, external
assets, gameplay scripts and player-prefab wiring were preserved. The same new
clover is referenced by the existing Lucky feedback.

Static forest renders: `output/previews/leveling-forest-{0,2,3,4,5,6,7}.png`
(progress, two/three choices, focus, pressed, empty/full XP). These are preview
scenes, not captures of a completed combat sequence. Existing availability,
centering and controller-navigation checks passed, including capped Luck.

## Original artwork

Game assets live in `Assets/Art/UI/Leveling/`. All sprites use 32 PPU, Point
filtering, uncompressed alpha and no mipmaps. The 16x16 half heart and sword are
project-owned copies of the existing PixelUIFree icons. Button/hover/pressed
frames are copies of the same pack's wood theme, retaining its 4/5/4/4 borders.
Source files and the original pack remain unchanged under External Assets.

The original 16x16 clover uses four pixel-rounded leaves centered at (4,7),
(11,7), (4,12), (11,12), with a three-pixel stem beneath. Each leaf's outline is
within dx <= 3, dy <= 3, dx + dy <= 4; the green interior uses dx <= 2, dy <= 2,
dx + dy <= 3. Outline is #164A2E, base green #44B748, highlights #8BE155, stem
#267638. Coordinates start at the PNG's lower-left in the authoring recipe.

The original plus is a 7x7 pixel cross: columns or rows 2 through 4 are #F8EBB5,
with transparent corners. Pixel.png is a one-pixel white UI fill used to tint the
XP track and blue fill. Buttons compose the plus and their icon as separate
Images; icons contain no text. Copies use their existing source silhouettes.

`Tools/Unity/BuildPlayerLevelUI.cs` retains the optional Unity authoring recipe.
The bar and buttons are centered at the same position above the player's hearts
and appear in separate stages. Generated previews are in ignored
`output/previews/player-xp-progress.png` and `player-level-2.png` / `player-level-3.png`.
