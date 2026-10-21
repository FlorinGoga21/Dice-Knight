# MainScene room boundaries

`CombatRoomBounds` is attached to **Room Flow** in **MainScene only**. Enemy and boss encounters use the enemy lineup to establish the far edge. The currently empty shop, treasure, and random rooms use the initial camera viewport for both edges and allow immediate departure. MainMenu and MapScene do not contain this component.

- The invisible left wall sits at the starting camera's left screen edge. The camera cannot reveal scenery beyond it, including while walking, jumping, or shaking from damage.
- Leftward velocity stops just before wall contact, so holding the direction does not repeatedly push against the physics solver. The player's walking animation and movement dust stop while blocked. Jumping and walking away remain available.
- The combat-room exit unlocks when every enemy is defeated. Its edge is three world units beyond the farthest enemy position recorded for the encounter, with at least one screen of room width. Select **Room Flow → Combat Room Bounds → Exit Padding** to tune that distance. Empty room types unlock immediately at the starting screen's right edge.
- The camera stops at the right edge, while the player can keep walking. Once the player's collider and all visible character sprites have fully passed the edge, the room completes through `RoomSceneController.CompleteRoom()`, marks map progress, preserves player stats, and uses the existing cloud transition back to MapScene. There is no solid right wall.
- If combat cleanup, a flying enemy's landing/reward spawn, or coin collection is still running, completion waits until it finishes. The player does not have to return to the edge and cross it again.
- The left wall extends above and below the spawn height to prevent jumping around it. **Wall Height** and **Wall Thickness** are Inspector settings. It has a collider and no visible artwork.
- Enlarging the viewport expands the right edge if needed to keep a full camera view within the room. The left edge stays fixed.

Room saves include `hasCombatBounds`, `combatLeftEdge`, and `combatRightEdge`. Loading preserves the room's edges, even if enemies moved or were already defeated. Exit availability is derived from the restored enemy lineup. Older saves without these fields derive boundaries from the authored starting view and restored enemies. Invalid saved boundary coordinates are rejected by the existing save validator.

Q remains available as a development shortcut and retains its previous behavior. Normal room completion uses the unlocked walk-off exit.

## Verification tools

With the Unity Editor connected, from the project root:

```powershell
# Edit mode, with no unsaved scene edits. Configures MainScene only.
unity command run_script --caller plugin --skill unity-cli --format json -- --file Tools/Unity/ConfigureCombatRoomBounds.cs --entry ConfigureCombatRoomBounds.Main

# Start a fresh Play session directly in MainScene, then run the live regression check.
unity command run_script --caller plugin --skill unity-cli --format json -- --file Tools/Unity/VerifyCombatRoomBounds.cs --entry VerifyCombatRoomBounds.Main

# In Play mode between transitions, also check the three placeholder room types.
unity command run_script --caller plugin --skill unity-cli --format json -- --file Tools/Unity/VerifyEmptyRoomBounds.cs --entry VerifyEmptyRoomBounds.Main
```

The combat check drives the real player movement/physics code without holding keyboard keys, checks left-wall stability and jumping, idle animation while blocked, camera limits, locked/cleared exits, partial and cleared save/load, pending rewards, and fully off-screen departure through the map transition. It temporarily disables automatic encounter input and uses a unique directory under `Temp/BoundaryVerification/` for saves. It never writes to the user's save directory. Results are written to `Temp/combat-room-boundary-verification.json`. The placeholder check verifies a fixed initial screen, no enemies, and off-screen completion for shop, random, and treasure rooms, with results in `Temp/empty-room-boundary-verification.json`. Stop Play mode afterward to discard the fixtures.
