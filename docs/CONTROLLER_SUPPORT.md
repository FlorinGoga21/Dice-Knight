# Controller support

The three main scenes use the UI action map in `Assets/Settings/PlayerInputActions.inputactions`. The original controller bindings are preserved. Start was added to Pause alongside the existing Select/View binding, and ReturnToMap was added for cleared-room exits.

| Context | Xbox-style control | PlayStation equivalent |
| --- | --- | --- |
| Menus and map | Left stick / D-pad, A to confirm | Left stick / D-pad, Cross to confirm |
| Back through menus | B | Circle |
| Pause / resume | Start or View | Options or Share |
| Move | Left stick | Left stick |
| Jump | A | Cross |
| Attack outside combat | X | Square |
| Roll in combat | A | Cross |
| Heal in combat | X | Square |
| Block in combat | B | Circle |
| Leave a cleared room | Y | Triangle |

Room exit waits for living enemies, death rewards, and coin pickups. The existing keyboard Q development shortcut retains its previous behavior. Rooms do not display control instructions.

Block remains usable once per encounter. It can be chosen before rolling or
during the existing result-viewing interval after all Lucky rerolls finish.
The button and B/Circle shortcut use the same rule. Blocking after seeing the
results replaces the player's result display with the shield, retains the
enemy's roll, and prevents that turn's enemy damage. It closes before attack
resolution begins; it is unavailable during dice animations, pause, or after
its single use is spent. The result interval remains controlled by CombatController's
Roll Display Duration (currently 1.5 seconds).

Reactive Block registers immediately, then shrinks/fades the player's dice and
total over 0.12 seconds. The result shield rises from below, fades in, grows to
110%, and settles over 0.2 seconds before attack resolution proceeds. Enemy
dice and the enemy total retain their existing objects and positions throughout.
Blocked attacks finish their projectile sequence (if any), play the block impact
sound and give the shield a brief squash/recoil that returns to its rest pose.
The transition uses game time, so pause freezes it. Fade duration, raise duration
and rise distance can be tuned under CombatController's Block Feedback group.
Compilation and non-Play-mode Block-rule checks passed; animation timing and
appearance remain for the user's Play-mode check.

Menus restore valid focus after fades or lost selection. Save-slot navigation explicitly links each slot to its Delete button; confirmations default to Cancel. Delete uses a bright gold selection outline and stronger red highlight. Reachable map rooms use explicit vertical navigation and a gold selected state. Combat buttons use direct action shortcuts rather than UI Submit navigation, preventing one press from activating two different combat actions.

Pause-menu navigation uses explicit loops for Resume / Save & Load / Quit /
Settings and for the Save / Load / Settings submenu. Disabled or inactive
buttons are skipped, including Save & Load during combat. Links refresh while
the pause menu owns focus so navigation cannot escape to gameplay controls
behind the overlay. The links were checked in disposable Editor copies of
MainScene and MapScene; live controller testing is left to the user.

Optional local verifiers under the ignored `Tools/Unity/` folder check controller flows using a virtual gamepad and isolated save fixtures. The completed one-time binding/setup scripts were retired during cleanup. These verifiers and their `output/` reports are not included in a fresh checkout or required by the game; committed input bindings and scene references are already configured.

Unity compilation succeeded during implementation. Menu, map, save-slot, and Back flows were exercised with simulated controller input. Full combat and room-exit verification remains for manual testing; automated testing was stopped at the user's request.
