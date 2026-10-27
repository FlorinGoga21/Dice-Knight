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

Menus restore valid focus after fades or lost selection. Save-slot navigation explicitly links each slot to its Delete button; confirmations default to Cancel. Delete uses a bright gold selection outline and stronger red highlight. Reachable map rooms use explicit vertical navigation and a gold selected state. Combat buttons use direct action shortcuts rather than UI Submit navigation, preventing one press from activating two different combat actions.

Optional local verifiers under the ignored `Tools/Unity/` folder check controller flows using a virtual gamepad and isolated save fixtures. The completed one-time binding/setup scripts were retired during cleanup. These verifiers and their `output/` reports are not included in a fresh checkout or required by the game; committed input bindings and scene references are already configured.

Unity compilation succeeded during implementation. Menu, map, save-slot, and Back flows were exercised with simulated controller input. Full combat and room-exit verification remains for manual testing; automated testing was stopped at the user's request.
