# Sound assignment checklist

A complete pack is assigned under `Assets/Audio/`, including the user-approved replacement effects. You can replace any assignment with your own AudioClip. Empty fields for unused character actions remain silent and safe. Assign clips to the fields below, rather than directly to the Audio Sources. See `Assets/Audio/AudioIndex.csv` for the file assigned to each field.

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
| Coin Pickup | First coin in a cluster arrives at the player's gold counter; plays again after a gap of at least 0.15 seconds between arrivals |
| Map Travel | Map marker begins moving to the selected room |
| Room Entered | MainScene loads |
| Room Completed | Leaving a completed room for the map |

Coin Pickup uses its original approved source amplitude (100%), scaled by the SFX setting, with no additional playback boost. Cluster timing uses gameplay time so pausing does not split a cluster. Every coin still grants its full currency value.

## Character sounds

The current five Hurt assignments use distinct variations of the two user-supplied pixel/retro references. See [source and processing notes](../ArtSource/Audio/PixelHurt20261027/README.md). Existing prefab assignments are preserved, and import normalization is disabled on the hurt clips to retain their authored balance.

The five Attack assignments use matching action-specific cues; see the [attack sound notes](../ArtSource/Audio/CharacterAttacks20261027/README.md). Knight's prefab Attack field is assigned to its attack WAV. Dice Demon's Attack is a short charge before its separate Projectile Launch effect; Projectile Launch and Projectile Impact retain their original clips.

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
| Footsteps | Player/Skeleton/Zombie use identical simple step variations. Slime's array is intentionally empty: its movement has no sound. |
| Jump | Player jump |
| Land | Player landing |
| Attack | Player and each enemy's swing/attack/casting sound |
| Hurt | Player and each enemy's nonlethal hit reaction |
| Death | Player and each enemy's defeat |
| Projectile Launch | Dice Demon fireball leaving its mouth |
| Projectile Impact | Dice Demon fireball arrival, including a blocked projectile |

Unused fields can stay empty. Zombie inherits the component from Skeleton: edit the Zombie variant's fields to override those sounds without applying them to Skeleton. `Volume` adjusts that character's clips, `Footstep Volume` adjusts only movement, and `Footstep Interval` is the delay in seconds between movement sounds. Footstep variations use a separate random generator and do not consume gameplay random numbers.

## Settings behavior

The main menu's top-right round gear opens the PixelUIFree settings overlay. Music and SFX sliders accept continuous values from 0 to 100%; controller left/right changes volume in steps. Resolution arrows cycle through available display dimensions, with duplicate refresh rates omitted. Fullscreen uses borderless fullscreen in a built player and windowed mode when unchecked. Display changes leave Unity's Editor window unchanged. Controls opens a scrollable binding reference with TMP glyphs for the most recently used keyboard/mouse or controller; Back or Cancel returns to Settings.

Preferences use `DiceKnight.Settings.*` PlayerPrefs keys, independent of run saves. Changes apply immediately and are saved when closing settings or pausing/quitting the app. Music continues through pause; current gameplay effects pause, and UI sounds remain available. Both gameplay and UI sounds follow SFX volume.

The pause menus in MainScene and MapScene include a small **Settings gear** at
the screen's top-right corner, matching the main menu's position, size, artwork, round frame,
hover tints and focus ring. The original Resume, Save/Load and Quit rows remain.
They use `Assets/Prefabs/SettingsMenu.prefab`, copied from the
authored main menu overlay and driven by the same settings and controls scripts.
Settings stays above the pause blur and keeps time paused. Back/Cancel returns
from Controls to Settings, then to the pause menu with Settings selected; Resume
returns to the game. The input that closes Settings cannot also resume the game.
Play mode verification of this addition is left to the user as requested.

The gear is a child of the pause screen Canvas, independent of the central panel.
It appears while the pause menu is open and hides during gameplay, Settings and
save-slot overlays. Both pause action groups include it in keyboard/controller
focus handling.

The current pack contains 8 music loops and 55 sound effects, with 63 active clip references. The approved revision replaces 20 WAVs while preserving their GUIDs and removes four unused Slime movement clips. Import normalization is disabled on the replacements so their auditioned gain survives import. Installation hashes, imported-level checks, and playback verification are recorded under `ArtSource/Audio/`.

Subsequent selection: Knight Attack uses A (Soft swish); Skeleton Death uses B
(Soft bone tumble). The old game clip contents were replaced at the same paths
and GUIDs. Dice Roll and Dice Settled are at 225% of the original approved source
amplitude; Coin Pickup has since been restored to its original approved level (100%). `KnightSkeletonSelection/report.json` under
`ArtSource/Audio/` verifies the four changed imports and prefab bindings.
