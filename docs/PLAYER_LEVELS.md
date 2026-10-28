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

Confirming an upgrade applies it once, then makes only the chosen button hop up
and pulse to 125% size over 0.32 seconds. Input is locked during this short
confirmation. Queued choices refresh after the button settles, so a newly capped
Luck button or completed Damage milestone stays visible for its confirmation.
The last choice finishes its pop before the presentation fades. The animation
uses game time, pauses with the game, and restores its transform and layout if
interrupted by death or the UI being disabled. Duration, scale and hop height
are configurable under PlayerLevelUI's Upgrade Feedback Inspector group.

## Visible Luck

All equipped dice contribute to the current combat total. The ordinary roll
first reveals its small HUD dice and large result dice. A successful Luck trigger
then displays a green clover and `LUCKY!` above the specific large result die being
rerolled. Only its small HUD die spins again. The large result then changes and
pops in place; the other result dice retain their faces, positions and scales.
The player total and comparison update before combat resolves.

Luck is a player statistic: +5 percentage points per Luck upgrade, capped at
25%. It does not change Basic Dice rolls. Each active Lucky Die independently
checks this statistic once per roll. On success, that same die rerolls uniformly
from its current face + 1 through 6, guaranteeing an improvement. A six skips the
check. Multiple Lucky Dice may each trigger once; ordinary dice never trigger,
including when they are the lowest face. Updated totals and matching multipliers
are used for combat resolution. Enemy dice remain Basic.

## Dice definitions and configuration

`Assets/Data/Dice/Basic.asset` and `Lucky.asset` are reusable `DiceDefinition`
assets containing a stable save ID, an ability and their face/rolling sprites.
Runtime slot assignments live on DiceRollUI; definition assets are not mutated.
New definitions can share an existing ability and supply different visuals;
future abilities should be appended to DiceAbility without changing old values.

`Assets/Data/Dice/Even.asset` adds Even Dice (ability value 2, save ID `even`).
Each roll samples 2, 4 or 6 with weights 6/15, 5/15, 4/15 respectively
(40%, 33.333...%, 26.666...%). Even Dice
do not use Luck or reroll; normal totals and matching multipliers still apply.
They use deep turquoise surfaces and ivory pips, with the normal sheet sizes,
sprite cuts, pivots, PPU and rolling-frame order. Their initial resting face is
two pips. The definition is registered in Available Dice without changing the
user's Equipped Dice; assign Even to a slot in the same way as Lucky.

`Assets/Data/Dice/Odd.asset` adds Odd Dice (appended ability value 3, save ID
`odd`). It rolls 1, 3 or 5 with the same low/middle/high weights as Even Dice:
40%, 33.333...%, 26.666...%. Odd Dice use copper-orange surfaces and charcoal
pips with the original sheet layouts and rolling frames. The initial resting
face is one pip. Odd does not trigger Lucky rerolls, and uses the ordinary
matching multipliers. It is registered in Available Dice for Inspector
assignment and save restoration, without resetting equipped slots.

In MainScene, select the DiceRollUI component (search the Hierarchy by
`t:DiceRollUI`) and change an entry under **Dice Types > Equipped Dice** from
Basic to Lucky, Even or Odd. Keep all used definitions in **Available Dice** so
saved IDs resolve. Adding new types preserves the current equipped slots.
No inventory, shop or acquisition
UI is introduced. Old per-slot visuals remain as a compatibility fallback for
unassigned definitions, with no special ability.

Lucky sprites are exact green palette swaps of both normal sheets, with their
pips, outlines, alpha, sizes, cuts, pivots and PPU preserved. Both definitions
use the same six pip-bearing rolling frames and normal rolling/impact timing.
Large player results use each slot's own face sprite, including after a reroll;
enemy results always use Basic art.

For Editor testing, select the gameplay Player's PlayerProgression component,
enable **Luck Testing > Override Luck For Testing**, and adjust **Test Luck
Percent** (0-100). At 100%, every eligible Lucky Die rerolls; Basic Dice still do
not. This Editor-only override does not change upgrades or saved progression.
Turn it off to return to earned Luck. Play-mode Inspector edits are temporary.

`PlayerLevelState.GetDiceMultiplier` is shared by Luck and the combat comparison,
preserving the existing 2x doubles / 3x triples rule for one to three dice.
Purchased damage affects turn-based attacks and free attacks before those
existing multipliers are applied.

## Saves and death

Progression is mutable run data, never a shared asset. `PlayerSaveData` adds an
explicit `hasProgression` flag and a copied `PlayerLevelState`. The current level,
XP remainder, purchased upgrades, and unspent choices survive saving/loading and
room changes. Equipped dice save stable IDs with an explicit hasDiceLoadout flag;
legacy saves without that flag keep the authored starting loadout.
Existing health/max-health fields retain the health upgrade's
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
invalid saves and death invalidation. `VerifyLuckyDice.cs` checks all 4,356
indexed reroll combinations for 1-3 dice, Basic immunity, independent Lucky
triggers over 10,000 seeded mixed-loadout rolls, exact sheet/slice parity,
loadout restoration and isolated old/new save round trips. RNG state is restored.
The old player-wide guaranteed-Luck check was replaced by this die-specific tool.
`VerifyEvenDice.cs` and `VerifyOddDice.cs` each check 60,000 weighted rolls,
the exact ticket mapping, unchanged Basic/Lucky sampling,
matching multipliers, sprite-sheet parity and mixed-loadout save round trips.
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
