# Dice Knight original audio pack

67 generated clips, assigned to the existing Inspector fields: **8 looping music tracks and 59 sound effects**. All sound assets live under this folder. They are original procedural synthesis, with no downloaded recordings or external samples.

## Find a sound

| Folder | Contents |
| --- | --- |
| `Music/` | Main menu, map, forest room, combat, boss, shop, treasure, and event themes |
| `SFX/UI/` | Selection, confirmation, Back, settings, pause/resume, save/load/delete |
| `SFX/Combat/` | Encounter/turn cues, wooden dice, result jingles, shields, potion |
| `SFX/World/` | Coin pickup, map travel, room entry/completion |
| `SFX/Characters/Knight/` | Four armored footsteps, jump, landing, sword swing, hurt, death |
| `SFX/Characters/Skeleton/` | Four bone footsteps, sword/bone attack, rattle hurt/death |
| `SFX/Characters/Zombie/` | Four heavy shuffles, attack, low synthesized hurt/death growls |
| `SFX/Characters/Slime/` | Four squishy movement variations, attack, hurt, death |
| `SFX/Characters/DiceDemon/` | Attack, hurt, death, fireball launch and impact |

`AudioIndex.csv` maps every file to its Inspector field. `AudioManifest.json` is the machine-readable source of the assignments and file statistics.

## Music

Each theme is a 16-bar original instrumental loop, approximately 26–46 seconds long, using plucked and bell tones, flute-like leads, soft harmony, bass, and restrained percussion. Combat and boss tracks use stronger pulse tones and drums. Note tails and echoes wrap around the loop boundary.

| Theme | Filename | BPM |
| --- | --- | ---: |
| Menu | `MUS_MainMenu_KnightsFortune.wav` | 96 |
| Map | `MUS_Map_ForkedPaths.wav` | 104 |
| Forest | `MUS_Forest_WhisperingBoughs.wav` | 84 |
| Combat | `MUS_Combat_RollOfSteel.wav` | 136 |
| Boss | `MUS_Boss_DemonsWager.wav` | 148 |
| Shop | `MUS_Shop_CopperAndCloth.wav` | 112 |
| Treasure | `MUS_Treasure_GlimmeringChest.wav` | 88 |
| Event | `MUS_Event_StrangeCrossroads.wav` | 108 |

## Inspector assignments

Shared clips are assigned on `Assets/Prefabs/GameAudio.prefab`. Character clips are assigned on the Knight and four enemy prefabs' **Character Audio** components. Zombie has its own overrides, so it does not use Skeleton's sounds. Slots for actions that a character does not perform remain empty.

Replace any clip directly in those Inspector fields. Music imports as stereo Vorbis streaming; short effects import as mono PCM and preload. Music and SFX retain the game's independent volume controls.

The source WAVs are 44.1 kHz, 16-bit PCM. File checks found no empty, silent, invalid, or individually clipped files. Play mode and audible balancing are left to the user.
