# Retro hurt sound set — 27 October 2026

Created for Knight (player), Skeleton, Zombie, Slime, and Dice Demon from both user-supplied MP3 references. The original reference files are preserved here; their supplied filenames are recorded below. No external voice, foley, or music was added.

## References

- `freesound_community-pixel-sound-effect-3-82880.mp3` → `Reference.mp3`; SHA-256 `304fc6af01d66a7526549a4395958a394364d01dab11ece630e7d0e876a1e526`.
- `driken5482-retro-hurt-1-236672.mp3` → `DrikenReference.mp3`; SHA-256 `d1fa2c9e9726b88b54fdd1faa4d25caf9762369cceefe1c7d86f447fc506e2e2`.

Decoded references are mono 44.1 kHz WAVs. The first reference has approximately 0.35 seconds of active sound followed by silence; only the active sound is used. The second supplies the compact retro hurt body.

## Character variations

| Character | Duration | Design |
| --- | --- | --- |
| Knight | 0.24 s | Retained clear player hurt: the shared retro reference for the set |
| Skeleton | 0.23 s | Slightly higher and drier, with a restrained brittle rattle |
| Zombie | 0.29 s | Slightly lower and rougher, with a gentle uneven tail |
| Slime | 0.26 s | Small elastic pitch wobble and a softer rounded body |
| DiceDemon | 0.42 s | Restored previous bright revision: deep layered body with a crisp original-pitch attack |

The current character revision keeps the clear retro family with moderate variation. The Knight WAV is retained exactly from the bright revision. Skeleton uses a 1.17× body with a quiet comb-filter texture and two restrained tail grains; Zombie uses a 0.89× body with gentle 31 Hz tail roughness; Slime bends from approximately 1.17× to 0.95× with a small pitch wobble; Dice Demon alone was reverted to the previous bright revision at the user's request: a 0.64× body, a quiet 0.48× layer, and a short original-pitch attack. The other enemy bodies use their own attacks. Both supplied references remain in every recipe. Presence shaping and the gentle 14 kHz rolloff retain clarity, with limited low-mid buildup and a 0.75 peak ceiling. Outputs remain mono 44.1 kHz, 16-bit PCM with import normalization disabled.

Existing game WAV paths, GUIDs, and CharacterAudio.hurt prefab assignments are preserved. Only the five hurt sounds are replaced; coin, dice, attacks, deaths, and movement effects are unchanged by this set.

Base character generator: `Tools/Audio/vary_pixel_hurt_set.py`; Dice Demon uses the restored `pixel-hurt-20261027-bright/clips/DiceDemon_Hurt.wav` selection. Clarity-pass generator: `Tools/Audio/brighten_pixel_hurt_set.py`. First-pass generator: `Tools/Audio/create_pixel_hurt_set.py`. The completed one-time Unity installer and reference decoders were retired during cleanup; decoded source WAVs and generators remain. Previous hurt WAVs and the comparison reel are under `output/audio-previews/pixel-hurt-20261027-character/`. The previous bright pass is retained under `output/audio-previews/pixel-hurt-20261027-bright/`. The first pass is retained under `output/audio-previews/pixel-hurt-20261027/`. Source files and these notes are retained under ArtSource; the game uses the installed WAVs directly.

Archived comparison order: Knight, Skeleton, Zombie, Slime, Dice Demon. The character-revision reel predates the Dice Demon-only rollback; use the individual bright-revision Dice Demon clip for the current sound. Listening and gameplay testing are left to the user.
