# Even Dice artwork

Game sheets: `Assets/Art/Dice/PixelDice_Turquoise.png` (168x168) and
`Assets/Art/Dice/dice_roll_numbers_turquoise.png` (168x56).

These retain the normal dice sheets' exact alpha bytes, pixel positions, pips,
symbols, cells and sprite cuts. The reusable Unity authoring recipe is the local
`Tools/Unity/BuildEvenDice.cs`. Source PNGs and their metadata remain untouched.

Visible grayscale surfaces use a deep turquoise ramp #093C46 to #2A9293, mapped
with normalized (grayscale - 48) / 207. Dark pixels connected to the outside are
preserved as outlines. Enclosed dark marks become ivory #F8E7BE, preserving the
pip and symbol silhouettes. Transparency remains exact. This is an exact palette
variant of the existing artwork, with no generated or resampled pixels.

Sprite importer settings: Point, uncompressed alpha, no mipmaps, original 100 PPU.
All 36 result-sheet and 12 rolling-sheet sprite cuts retain their rects and pivots.
Even.asset uses the same first six pip-bearing rolling frames as Basic/Lucky.
All six result sprites are retained; gameplay only produces faces 2, 4 and 6.

Verification: both sheets inspected, 37,632 pixel/alpha checks, matching cuts,
60,000 seeded rolls with only 2/4/6 outcomes, and isolated save fixtures.
Play-mode testing is left to the user.
