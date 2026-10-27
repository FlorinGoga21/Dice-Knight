# Level upgrade pixel artwork

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
