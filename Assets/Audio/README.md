# Dice Knight audio pack

63 clips assigned to the existing Inspector fields: **8 looping music tracks and 55 sound effects**. Music and unchanged effects use original procedural synthesis. The user-approved replacement effects combine original synthesis with edited CC0 foley; sources and licenses are recorded under `ArtSource/Audio/FoleyReferences/`.

## Find a sound

| Folder | Contents |
| --- | --- |
| `Music/` | Main menu, map, forest room, combat, boss, shop, treasure, and event themes |
| `SFX/UI/` | Selection, confirmation, Back, settings, pause/resume, save/load/delete |
| `SFX/Combat/` | Encounter/turn cues, wooden dice, result jingles, shields, potion |
| `SFX/World/` | Coin pickup, map travel, room entry/completion |
| `SFX/Characters/Knight/` | Four simple shared footsteps, jump, soft landing, sword swing, hurt, death |
| `SFX/Characters/Skeleton/` | Same four footsteps as Knight/Zombie, sword/bone attack, approved short fracture hurt, death |
| `SFX/Characters/Zombie/` | Same four footsteps as Knight/Skeleton, attack, approved demon-like hurt, death |
| `SFX/Characters/Slime/` | Attack, hurt, death; movement intentionally silent with empty footstep slots |
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

The approved set uses coin option A, softer dice roll/settle, shared plain footsteps, Knight landing, Skeleton hurt option B, Slime hurt, the approved demon hurt, and the final Zombie hurt derived from that demon. Twenty WAVs were replaced in place to preserve their GUIDs. The four unused Slime movement WAVs and metadata were removed. Replacement import normalization is disabled to preserve the approved softer levels.

Knight attack now uses audition A (Soft swish), and Skeleton death uses audition B (Soft bone tumble), at their auditioned levels. Both old game WAV payloads were replaced in place, preserving GUIDs and Inspector references.

Current mix adjustments: coin pickup stays at 15% of its original approved amplitude; dice roll and settle are now at 225% of their approved levels (another 50% increase from the previous 150%). The new Knight attack replaces the earlier half-volume original. Import normalization is disabled to preserve these levels. Source credits are retained under `ArtSource/Audio/`.

Replace any clip directly in those Inspector fields. Music imports as stereo Vorbis streaming; short effects import as mono PCM and preload. Music and SFX retain the game's independent volume controls.

The source WAVs are 44.1 kHz, 16-bit PCM. `AudioManifest.json` records the installed clips and replacement hashes. Source credits and licenses are under `ArtSource/Audio/FoleyReferences/`. Local generators, audition recordings, backups, and installation reports are excluded from version control and are not required by the game.
