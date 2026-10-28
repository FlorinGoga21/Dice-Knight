# Matching result dice fire

The fire uses all 61 frames, in numeric order, from
`Assets/External Assets/PossibleAdditions/Fx_pack/2/1_0.png` through `1_60.png`.
The external source images and their sprite imports remain unchanged.

`Tools/Unity/BuildDiceMatchFire.cs` packs the original 100 x 100 frames into an
800 x 800 transparent atlas at `Assets/Art/Effects/DiceMatchFire.png`. Run it
through Unity Pipeline's `script run` outside Play mode after scripts import.
It uses Unity's sprite importer APIs and preserves existing generated sprite IDs
when rerun. Tools is locally ignored by this project's Git configuration.

The atlas uses point filtering, 40 pixels per unit, no mipmaps, and no
compression. Each frame's pivot is at (60, 46) pixels, near the bright core.
The resource prefab at `Assets/Resources/Combat/DiceMatchFire.prefab` rotates
the animation 90 degrees clockwise so the source's left-facing trail rises.
Its `DiceMatchFire` component exposes playback speed (30 FPS by default) and
size relative to the die width. The size is 2.2, making the flames wrap around
the die. Ignition waits 0.15 seconds, then fades in and grows from 35% to full
size over 0.5 seconds. Both ignition timings can be adjusted on the prefab.

`CombatController` creates the effect only on the side with the higher final
roll total, after all Lucky rerolls finish. Only dice with another equal face
in that winning roll ignite: a pair lights those two dice; a triple lights all
three. Losing rolls, ties, and turns blocked before rolling show no fire.
Flames inherit their
die's position and scale, copy its alpha and visibility, and render one sorting
order behind it. Destroying the result die also destroys its fire. A reactive
Block removes the player's dice and fire with the existing transition.

The effect is cosmetic; damage multipliers, dice probabilities, saves, and the
small rolling HUD dice are unchanged. Gameplay and visual testing are left to
the user as requested.
