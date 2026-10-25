# Sound assignment checklist

All clip fields start empty. Empty fields are silent and safe. Drag imported AudioClip assets into the fields below; do not assign clips to the Audio Sources directly.

## Shared music and effects

Open `Assets/Prefabs/GameAudio.prefab` and select its root **Game Audio** object. Assign clips on the **Game Audio** component. All three main scenes reference this prefab; the live instance persists across scene loads.

### Music Clips

| Inspector field | Used for |
| --- | --- |
| Main Menu Music | Main menu and settings overlay |
| Map Music | Branching run map |
| Room Music | Exploration/default room music |
| Combat Music | Normal turn-based encounters; empty keeps room music |
| Boss Music | Boss rooms and encounters; empty falls back to normal room/combat music |
| Shop Music | Optional shop room music; empty uses Room Music |
| Treasure Music | Optional treasure room music; empty uses Room Music |
| Event Music | Optional random/event room music; empty uses Room Music |

Music loops automatically. Music Fade Duration controls the fade when changing tracks. Assigning the same clip to multiple contexts keeps it playing without restarting.

### Menu Sound Effects

| Inspector field | Used for |
| --- | --- |
| UI Focus | Mouse hover/controller selection |
| UI Confirm | Button clicks/Submit |
| UI Back | Back through settings/save dialogs |
| Setting Changed | Volume adjustments/fullscreen toggle |
| Pause | Opening pause |
| Resume | Resuming gameplay |
| Save Success | Successful manual save through the slot panel |
| Load Success | Successful load through the pause slot panel |
| Delete Save | Successful confirmed save deletion |

### Combat Sound Effects

| Inspector field | Used for |
| --- | --- |
| Encounter Started | Encounter begins |
| Encounter Won | Living player finishes an encounter after defeating the enemy |
| Turn Started | New player turn |
| Dice Roll | Start of rolling the player's dice |
| Dice Settled | All player dice finish rolling |
| Roll Win | Player wins the dice comparison |
| Roll Lose | Enemy wins the dice comparison |
| Roll Tie | Equal dice totals |
| Block Raised | Player chooses block |
| Block Impact | Enemy attack is blocked |
| Healing Potion | Successful potion use |

### World Sound Effects

| Inspector field | Used for |
| --- | --- |
| Coin Pickup | Coin arrives at the player's gold counter |
| Map Travel | Map marker begins moving to the selected room |
| Room Entered | MainScene loads |
| Room Completed | Leaving a completed room for the map |

## Character sounds

Open each prefab below and select its root. Assign its **Character Audio** component. These are per-character fields so Skeleton, Zombie, Slime, Dice Demon, and Knight can sound different.

| Character | Prefab |
| --- | --- |
| Player | `Assets/Art/Player/Knight.prefab` |
| Skeleton | `Assets/Prefabs/Enemies/Skeleton.prefab` |
| Zombie | `Assets/Prefabs/Enemies/Zombie.prefab` |
| Slime | `Assets/Prefabs/Enemies/Slime.prefab` |
| Dice Demon | `Assets/Prefabs/Enemies/DiceDemon.prefab` |

| Inspector field | Assign for |
| --- | --- |
| Footsteps | Player/Skeleton/Zombie steps; optional Slime movement/squish clips. An array allows variations. |
| Jump | Player jump |
| Land | Player landing |
| Attack | Player and each enemy's swing/attack/casting sound |
| Hurt | Player and each enemy's nonlethal hit reaction |
| Death | Player and each enemy's defeat |
| Projectile Launch | Dice Demon fireball leaving its mouth |
| Projectile Impact | Dice Demon fireball arrival, including a blocked projectile |

Unused fields can stay empty. Zombie inherits the component from Skeleton: edit the Zombie variant's fields to override those sounds without applying them to Skeleton. `Volume` adjusts that character's clips, `Footstep Volume` adjusts only movement, and `Footstep Interval` is the delay in seconds between movement sounds. Footstep variations use a separate random generator and do not consume gameplay random numbers.

## Settings behavior

The main menu's top-right round gear opens the PixelUIFree settings overlay. Music and SFX sliders accept continuous values from 0 to 100%; controller left/right changes volume in steps. Fullscreen uses borderless fullscreen in a built player and windowed mode when unchecked. Unity's Editor window is not switched into fullscreen.

Preferences use `DiceKnight.Settings.*` PlayerPrefs keys, independent of run saves. Changes apply immediately and are saved when closing settings or pausing/quitting the app. Music continues through pause; current gameplay effects pause, and UI sounds remain available. Both gameplay and UI sounds follow SFX volume.

Unity compilation and the authored settings preview were checked. No Play mode or audible verification was run; clips remain unassigned for the user to choose and test.
