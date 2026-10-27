# Ashen Oath - death-screen music

Revised on 27 October 2026 after the first version felt too tragic. This
16-second cue now shares the main menu's D-minor melody, flute-like lead,
plucked accompaniment, soft pad, and rounded bass timbres. It runs at 88 BPM
instead of the menu's 96 BPM. The opening recalls Knight's Fortune, then the
notes grow sparser and resolve gently into D minor with a soft final pluck.
The first version's low death bell, bowed-string texture, and hall-like tail
have been replaced. This is a one-shot farewell with no percussion.

`generate_death_music.py` retains the original menu instrument formulas in a
standalone NumPy recipe. It writes `Assets/Audio/Music/MUS_Death_AshenOath.wav`
and `generation.json`. Existing music is protected unless explicitly run with
`--replace`. It never edits Unity metadata or regenerates the other tracks.
The composition and synthesis use no external samples or soundtrack recordings.

The WAV is stereo, 44.1 kHz, 16-bit PCM. Authored RMS is 0.085, below the menu
loop's 0.105, with peak about 0.5279 and silent endpoints. A three-second final
fade preserves the ending. Generation checks sample finiteness, headroom and
endpoints; `generation.json` records the current file hash.

The game asset's path and GUID remain unchanged, preserving the `deathMusic`
assignment on `Assets/Prefabs/GameAudio.prefab`. The previous cue and its recipe
are retained locally under ignored `output/death-music-previous/` for comparison.
The main menu's game-consumed audio file is unchanged.

The import remains preloaded Vorbis in memory with normalization disabled.
Normal music fades out during the death animation, and this cue starts with
YOU DIED after `deathAnimationDelay`. It follows Music volume, plays once while
gameplay time is frozen, and hands back to the destination scene's looping
music after either exit. An empty Death Music field leaves the screen silent.

WAV sample checks, Unity import, GUID preservation and prefab binding were
verified. In-game timing, balance and playback testing remain with the user.
No C# or death-screen timing changes were needed for this revision.
