# Project cleanup — 27 October 2026

## Inventory

Counts are physical file lines, including comments and blank lines. Script counts
mean files, including partial-class files. Unity caches, packages, generated
solution/project files, build output, and audition output are excluded.

| Scope | Files before | Lines before | Files after | Lines after |
| --- | ---: | ---: | ---: | ---: |
| Authored game C# (`Assets/Scripts/`) | 57 | 9,248 | 57 | 9,196 |
| Third-party C# under Assets | 71 | 8,685 | 71 | 8,685 |
| Generated Input System wrapper | 1 | 1,612 | 1 | 1,612 |
| All C# under Assets | 129 | 19,545 | 129 | 19,493 |
| Optional local tools (C# and Python) | 47 | 6,156 | 20 | 3,281 |

The remaining local tools contain nine C# files (1,233 lines) and eleven Python
files (2,048 lines). Authored game C# has 8,113 nonblank lines after cleanup.

## Review and removal evidence

The review covered authored C#, Unity scene/prefab/animation method references,
script GUID references, runtime component creation, partial classes, save models,
project/input configuration, imported-code boundaries, and every local tool's
entry points and dependencies. A read-only compiler analysis resolved the 105
sources in Assembly-CSharp, using its assembly references and current defines,
with no analysis compilation errors. Symbol-reference results were cross-checked
against asset callbacks and tool/reflection references before removal.

| Removed member | Evidence and retained behavior |
| --- | --- |
| `EnemyController.IsWithinDetectionRange` | No code or asset callback callers; active targeting still uses its existing detection-range checks. |
| `EnemySpawner.SpawnedEnemies`, `SpawnerId` | No callers. The internal lineup list and persistent spawner IDs remain. `EnemyStats.SpawnerId`, used by saves, remains. |
| `CombatController.CanSave` | No callers. Save permission is owned by the actively used `SaveManager.CanSave(out ...)`. Legacy combat save fields and restoration remain. |
| `PlayerController.SetGameplayEnabled`, `gameplayEnabled` | No callers or asset callbacks; the nonserialized flag stayed true. Its always-true guards were simplified; pause, death, component enablement, and combat gates remain. |
| `PlayerStats.RollButton` | No code reads or writes and no reflection usage; the only serialized assignment was null on Knight. Unity's prefab API removed that unused serialized entry. Active dice roll button fields remain. |
| `DiceRollUI.BuyNextDie` | No code, animation-event, or persistent button callers. Current die selection, rolling, configured active dice count, and enemy dice matching remain. Future shop/progression work will need an explicitly implemented purchase path. |

DamageNumber and HealNumber had identical coroutine bodies. Both now start the
same coroutine in the existing FloatingNumberVisuals helper, retaining the caller
as coroutine owner, the elapsed-time update order, arc/fade math, lifetime,
random direction, text formatting, and destruction timing. No new manager or
framework was introduced. Runtime code shrank by 52 lines across nine files.

## Retained deliberately

- No whole gameplay class or script was verified unused. All 57 game scripts remain.
- Unity lifecycle methods, event-interface methods, and runtime initialization
  hooks remain even when ordinary C# callers are absent.
- Pause-menu callbacks, RollDice, and SelectDie remain because assets reference them.
- UIAudioFeedback, CoinPickup, and HealthHeartsUI remain because code creates them.
- Main-menu DummyPlayerWalk remains in use; its file/type naming was not changed.
- Save schema fields, legacy lineup/weapon/flight/room restoration, and enum values
  remain to preserve existing saves. Similar player/enemy code with different
  lifecycle and combat responsibilities was not merged speculatively.
- Third-party code, generated input code, packages, art, sounds, tuning, and
  project settings were not changed by this cleanup. Pre-existing local edits
  were recorded and preserved.

## Tool cleanup

Twenty-five one-time C# tools and two historical audio-installation Python tools
were removed, totaling 2,875 lines. They configured already-authored scenes,
prefabs, bindings, colors, PPU, and audio; applied superseded mix settings; decoded
the two now-preserved reference WAVs; or checked an obsolete audio installation.
No retained tool imports or calls them. Runtime assets do not depend on Tools.

Reusable source generators and their import dependency chain, disposable preview
tools, wiring inspection, and meaningful regression tools remain. The deleted
ignored sources were archived before removal, and every archive entry's SHA-256
was matched to its source. Recovery files are local and ignored:
`output/cleanup-20261027/retired-tools.zip` and `retired-tools.json`.

## Verification and limits

- Unity compilation completed with zero errors and zero warnings after code edits.
- The final diff was reviewed; the Knight prefab cleanup removes only the null
  RollButton field in addition to the pre-existing audio edit.
- Documentation was updated to stop recommending the retired setup commands.
- Play mode, gameplay, visual, and save/load tests were left to the user in
  accordance with the existing testing preference. No user saves were opened or
  written, and no scenes were saved by the cleanup.

Unused-code detection is bounded by the project's current code, assets, and
configuration. It does not prove that unrelated future content will not need a
removed API; no speculative removals were made for possible future features.
