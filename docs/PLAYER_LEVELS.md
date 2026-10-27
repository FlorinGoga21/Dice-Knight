# Player levels and run upgrades

Every newly defeated enemy awards 100 XP to a living player. Level 1 requires
100 XP to reach level 2; level 2 requires another 200 XP, level 3 another 300 XP,
and so on. Overflow carries forward. Each gained level grants one upgrade
choice. Choices are queued in earned-level order when multiple levels arrive
together, so saving or gaining more XP cannot change damage eligibility.

## Upgrade choices

- **Health:** +10 maximum HP and +10 current HP (one half heart).
- **Luck:** +5 percentage points per purchase, maximum five purchases / 25%.
- **Damage:** +10 base attack damage, offered at earned levels 3, 6, 9, etc.

Unavailable choices are hidden completely. Two, three, or one remaining choice
is centered above the player. Buttons use a plus sign with a half heart,
four-leaf clover, or sword, without text labels on the buttons. Their project-owned
sprites under `Assets/Art/UI/Leveling/` use 32 PPU, Point filtering, no mipmaps and
uncompressed alpha. The shared heart/wood/sword artwork derives from the existing
PixelUIFree assets; the clover and plus are authored pixel sprites.

The gameplay Knight prefab contains `PlayerProgression`, referencing
`Assets/Prefabs/UI/PlayerLevelUI.prefab`. After a fight, the hearts remain visible
and a small blue bar above them animates XP and displays the level and threshold.
If there is a level-up choice, the bar and its label fade out first; the upgrade
buttons then fade in at the same position. Otherwise the XP display holds briefly
and fades away. The next encounter, player movement/attacks and room exits wait
for this presentation and any required choice. Nearby enemies cannot interrupt
it. Coin collection and death animations continue normally.

Choices support mouse, keyboard and controller through the existing Input System
UI module. D-pad/left stick moves focus, A/Cross confirms, Health is focused first,
and explicit navigation links include only available choices. A single Health
choice keeps valid focus. Pause stops the presentation; the choice UI does not
steal selection from pause/settings, and the existing pause flow restores focus.

## Visible Luck

All equipped dice contribute to the current combat total. The ordinary roll
first reveals its small HUD dice and large result dice. A successful Luck trigger
then displays a green clover and `LUCKY!` above the specific large result die being
rerolled. Only its small HUD die spins again. The large result then changes and
pops in place; the other result dice retain their faces, positions and scales.
The player total and comparison update before combat resolves.

Luck targets the lowest eligible die and generates a result uniformly from its
current face + 1 through 6, guaranteeing at least +1 on every activated reroll.
An all-six roll skips Luck because no higher face exists. There are no equal,
lower, `BEST KEPT`, or `MATCH KEPT` outcomes. Matching multipliers are recalculated
normally from the final faces, so changing a matched die may change its bonus.
This replaces the initial keep-better/keep-matches implementation. Luck applies
only to player dice; the original d6 roll remains uniform. Current dice variants
retain their existing mechanics; no new
Vampire/Odd/Even/Lucky Dice mechanics are introduced by this feature.

`PlayerLevelState.GetDiceMultiplier` is shared by Luck and the combat comparison,
preserving the existing 2x doubles / 3x triples rule for one to three dice.
Purchased damage affects turn-based attacks and free attacks before those
existing multipliers are applied.

## Saves and death

Progression is mutable run data, never a shared asset. `PlayerSaveData` adds an
explicit `hasProgression` flag and a copied `PlayerLevelState`. The current level,
XP remainder, purchased upgrades, and unspent choices survive saving/loading and
room changes. Existing health/max-health fields retain the health upgrade's
actual result; restoring progression does not apply upgrades or healing again.
Older version-1 saves without the flag start at level 1 and retain their saved
health, currency and encounter state. Invalid progression is rejected.

The live defeat path grants XP once behind the enemy's existing death guard.
Restoring defeated enemies does not call that path and grants no XP or loot.
Death invalidates the active run through the existing save system; a fresh run
loads the authored level-1 Knight with no carried XP, Luck, damage or pending
choices.

## Verification

Verification deliberately stays outside Play mode. The repeatable local tool
`Tools/Unity/VerifyPlayerLevels.cs` checks thresholds, overflow, queued upgrades,
damage milestones, the Luck cap, isolated save round trips, old-save compatibility,
invalid saves and death invalidation. It also exhaustively checks all 1,548
one-to-three-die reroll combinations for bounded, strictly increasing faces and
untouched other dice. `VerifyGuaranteedLuck.cs` checks generated results and the
deferred update across 10,000 seeded attempts, restoring the original RNG state.
`VerifyLevelUI.cs` checks available/hidden choices,
controller navigation, centering, existing input-module bindings, and sprite
import settings. Fixtures use unique temporary save directories and never read
or overwrite player saves.

`PreviewPlayerLevelUI.cs` renders static XP and choice layouts in a disposable
preview scene. `BuildPlayerLevelUI.cs` authors the prefab and sprites through
Unity APIs; `UpdateLevelUILayout.cs` updates the staged bar/choice layout. These
local tools and generated reports/previews under `output/` are ignored and are
not required by the game.

Live reward timing, mouse/controller operation, health changes, the visible
Luck reroll and full save/room/death flows remain for the user's Play mode check.
