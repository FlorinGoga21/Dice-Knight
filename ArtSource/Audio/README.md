# Original Dice Knight audio generation

Generator: `Tools/Audio/generate_game_audio.py`, using Python, NumPy, and the standard library. Seed: 20261025. No recorded samples, voices, downloaded music, or external audio models are used.

Style: melodic retro fantasy. The score uses original short motifs developed over sixteen bars with chord progressions, accompaniment, bass, percussion, response phrases, and cadences. Individual instruments use additive, FM, and noise synthesis with shaped attacks/releases. Musical tails are accumulated circularly for looping. Effects use separate recipes for armor, bones, shuffling, slime bubbles, demon growls, fire, wood, metal, and UI chimes.

Output: `Assets/Audio/`, including the assignment manifest and CSV index. The default generator refuses to overwrite existing generated files. Its explicit `--replace-generated` option regenerates the pack and can replace subsequent edits to those WAVs, so inspect the files before using it.

Import/wiring: `Tools/Unity/AssignGeneratedAudio.cs`, run through the connected Unity Editor. It uses audio importer, serialized property, and prefab APIs, preserving asset GUIDs and prefab variant overrides. Inspector assignments remain freely replaceable afterward.

The source WAV pack contains 67 clips and is approximately 50.8 MiB. Music is stereo; effects are mono. Each file is normalized to its category's target level, with an individual peak ceiling of 0.82. This does not replace listening and balancing the combined game mix.
