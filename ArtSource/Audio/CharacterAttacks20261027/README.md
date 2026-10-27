# Character attack sound set — 27 October 2026

Five attack cues accompany the accepted moderate retro hurt set. They are action sounds, with no baked-in damage or hurt cue. Existing animation triggers, hit delays, damage rules, and projectile timing are unchanged.

| Character | Duration | Design |
| --- | --- | --- |
| Knight | 0.20 s | Clean light sword swish with a restrained steel edge |
| Skeleton | 0.19 s | Lean sword sweep with a quiet dry bone tick |
| Zombie | 0.24 s | Weightier, rough cloth-and-arm swing with a short scrape |
| Slime | 0.22 s | Soft elastic wet flick with a rising liquid pop |
| DiceDemon | 0.16 s | Brief arcane breath/charge ending before the separate fireball launch |

Dice Demon uses its existing 0.18-second launch delay. Its new 0.16-second attack cue is a short breath/charge, leaving the existing fireball launch and impact effects in their separate fields.

## Sources and processing

Knight, Skeleton, and Zombie combine edited copies of the existing Kenney RPG Audio/Impact Sounds sources listed below. Publisher links and retained license texts are under [the existing Kenney source notes](../FoleyReferences/Kenney/SOURCES.md). Slime and Dice Demon use original deterministic filtered-noise and chirp synthesis. No new external audio was downloaded.

- `cloth4`: `Kenney/rpg-audio/Audio/cloth4.wav` → `Sources/cloth4.wav`; SHA-256 `5edccf80e261e07a81261d3327ca6f16a282e267199aa19151a793a41f9935dc`.
- `cloth3`: `Kenney/rpg-audio/Audio/cloth3.wav` → `Sources/cloth3.wav`; SHA-256 `b9f80c51f56fc108d903658e17a507ac96708f2eb6d5d9d16b0d65b5b8266bbc`.
- `knifeSlice2`: `Kenney/rpg-audio/Audio/knifeSlice2.wav` → `Sources/knifeSlice2.wav`; SHA-256 `cec3a8fa04dc15fd28b3413001cba151b8afed9a077e4f03988f752c024d8f7a`.
- `drawKnife2`: `Kenney/rpg-audio/Audio/drawKnife2.wav` → `Sources/drawKnife2.wav`; SHA-256 `2105eb82e630f0deb377bcbd1cba6503a6396030b7506ac67e8b3b9f4e10919d`.
- `boneTap`: `Kenney/impact-sounds/Audio/impactWood_light_002.wav` → `Sources/boneTap.wav`; SHA-256 `0a2cd281a216bb64278fcfd645b3133b39f7e1d950c6809a4de25995bea92ae6`.

Foley copies retain their original decoded source data. The generator converts stereo to mono and resamples as needed. Processing selects short high-energy windows, shapes swish envelopes, adds restrained dry texture, and exports mono 44.1 kHz 16-bit PCM. Intended RMS levels are 0.043–0.050 with a 0.46 peak ceiling. Import normalization is disabled to retain this authored balance. SFX preferences and CharacterAudio volume settings still apply.

Local generator: `Tools/Audio/create_character_attack_set.py`, random seed `2026102704`. The one-time Unity installer was retired after installation; the generator remains as source tooling. Generated clips, previous attack backups, the installation report, and the comparison reel are under `output/audio-previews/character-attacks-20261027/`. Game clips remain at their existing paths under `Assets/Audio/SFX/Characters/` with their GUIDs preserved.

The Knight prefab attack field was empty; it now references its existing attack WAV path. Scene instance assignments are preserved. Enemy assignments retain their original clip references. The accepted five hurt files remain applied. The game does not depend on local tooling or generated previews.

Preview order: Knight, Skeleton, Zombie, Slime, Dice Demon. Listening and gameplay testing are left to the user.
