# Dice Knight audio pack

64 clips assigned to the Inspector fields: **8 looping music tracks, 1 death-music cue, and 55 sound effects**. Music and unchanged effects use original procedural synthesis. The user-approved replacement effects combine original synthesis with edited CC0 foley; sources and licenses are recorded under `ArtSource/Audio/FoleyReferences/`.

## Find a sound

| Folder | Contents |
| --- | --- |
| `Music/` | Main menu, map, forest room, combat, boss, shop, treasure, event themes, and death cue |
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

Each theme is a 16-bar original instrumental loop, approximately 26â€“46 seconds long, using plucked and bell tones, flute-like leads, soft harmony, bass, and restrained percussion. Combat and boss tracks use stronger pulse tones and drums. Note tails and echoes wrap around the loop boundary.

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

The original 16-second `MUS_Death_AshenOath.wav` cue plays once with the YOU DIED title reveal. It follows Music volume, fades naturally to silence, and hands back to looping scene music after either exit. Its synthesis recipe and mix notes are in [`ArtSource/Audio/Death20261027`](../../ArtSource/Audio/Death20261027/README.md).

## Inspector assignments

Shared clips are assigned on `Assets/Prefabs/GameAudio.prefab`. Character clips are assigned on the Knight and four enemy prefabs' **Character Audio** components. Zombie has its own overrides, so it does not use Skeleton's sounds. Slots for actions that a character does not perform remain empty.

The approved set uses coin option A, softer dice roll/settle, shared plain footsteps, Knight landing, Skeleton hurt option B, Slime hurt, the approved demon hurt, and the final Zombie hurt derived from that demon. Twenty WAVs were replaced in place to preserve their GUIDs. The four unused Slime movement WAVs and metadata were removed. Replacement import normalization is disabled to preserve the approved softer levels.

Knight attack previously used audition A (Soft swish), and Skeleton death uses audition B (Soft bone tumble), at their auditioned levels. Both old game WAV payloads were replaced in place, preserving GUIDs and Inspector references. The current attack revision described below supersedes the Knight selection.

Current mix adjustments: coin pickup is restored to 100% of its original approved amplitude, with one sound per cluster and no additional playback boost; dice roll and settle are now at 225% of their approved levels (another 50% increase from the previous 150%). The new Knight attack replaces the earlier half-volume original. Import normalization is disabled to preserve these levels. Source credits are retained under `ArtSource/Audio/`.

The current Knight, Skeleton, Zombie, Slime, and Dice Demon hurt effects are distinct variations derived from both user-supplied retro references, replacing the earlier hurt selections above. Original reference files and processing notes are under [`ArtSource/Audio/PixelHurt20261027`](../../ArtSource/Audio/PixelHurt20261027/README.md). Their existing asset paths and prefab assignments are preserved.

All five characters have matching action-specific attack cues: light sword swishes for Knight and Skeleton, a heavier swing for Zombie, a liquid flick for Slime, and a brief charge for Dice Demon. The Knight prefab's previously empty Attack field is assigned. Dice Demon's fireball launch/impact fields remain separate. Sources and processing are recorded in the [attack sound notes](../../ArtSource/Audio/CharacterAttacks20261027/README.md).

Replace any clip directly in those Inspector fields. Music imports as stereo Vorbis streaming; short effects import as mono PCM and preload. Music and SFX retain the game's independent volume controls.

The source WAVs are 44.1 kHz, 16-bit PCM. `AudioManifest.json` records the installed clips and replacement hashes. Source credits and licenses are under `ArtSource/Audio/FoleyReferences/`. Local generators, audition recordings, backups, and installation reports are excluded from version control and are not required by the game.
