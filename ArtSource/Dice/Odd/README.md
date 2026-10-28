# Odd Dice artwork

Game sheets: `Assets/Art/Dice/PixelDice_Copper.png` (168x168) and
`Assets/Art/Dice/dice_roll_numbers_copper.png` (168x56).

These are exact palette variants of the normal sheets, preserving all alpha
bytes, pixel positions, pip/symbol silhouettes, cells, cuts, pivots and 100 PPU.
No source art or original metadata was overwritten. Source recipe:
`Tools/Unity/BuildOddDice.cs`, using Unity texture and sprite-importer APIs.

Copper surfaces map normalized (grayscale - 48) / 207 onto #70402A to #CB8246.
Enclosed dark pips/symbols use charcoal brown #3C2824; dark pixels connected to
the outside retain the original outlines. The same flood-fill classification
used for Even Dice preserves the pattern while changing the mark color.
Sprites use Point, uncompressed alpha and no mipmaps. All six normal pip-bearing
rolling frames are assigned. The full result sheet is retained, while gameplay
only resolves to 1, 3 or 5 with weights 40%, 33.333...%, 26.666...%.

Verification: both sheets inspected; 37,632 pixel/alpha comparisons, sprite cuts,
60,000 seeded weighted rolls and isolated mixed-loadout save tests passed.
Live animation and gameplay checks are left to the user.
