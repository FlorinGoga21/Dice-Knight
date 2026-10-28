# Lucky Dice artwork

Game files: Assets/Art/Dice/PixelDice_Green.png (168x168) and
Assets/Art/Dice/dice_roll_numbers_green.png (168x56).

These are one-to-one palette swaps of the user-supplied normal sheets.
All alpha bytes, dark pips/symbols/outlines (RGB <=48), dimensions, sprite cuts,
pivots and 100 PPU are preserved. Every other visible grayscale pixel is mapped
to the same green ramp in both sheets: #2A6831 to #B7E78B, using normalized
(grayscale - 48) / 207. Sprites use Point, uncompressed alpha, no mipmaps.
The game uses all six pip-bearing rolling frames, identical to Basic Dice.

GreenPaletteReference.png is the built-in ImageGen color reference. It is not
used as a sprite sheet: its grid was not trusted. The approved exact palette
transfer is applied to the original pixels by Tools/Unity/BuildLuckyDice.cs,
using Unity texture, sprite importer and ScriptableObject APIs.
That optional setup tool assigns a Basic starting loadout; do not rerun it over
later user-chosen loadouts. Source sheets and their .meta files are untouched.

ImageGen prompt: Recolor this pixel-art dice sprite sheet green for the Lucky
Dice variant of a 2D RPG. Preserve every cell, symbol, pip count, frame, outline
and transparent background. Change only white/gray surfaces to pale mint
highlights, medium grass green faces, darker forest green sides. Keep pips and
symbols black and preserve the grid. No clovers or added decoration.

Verification: 37,632 alpha/pixel comparisons; all 36 result-sheet cuts and 12
rolling-sheet cuts match the originals. In-engine Play-mode testing is left to
the user.
