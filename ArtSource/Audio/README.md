# Dice Knight audio sources

The installed pack has 64 assigned clips: 8 original music loops, 1 death cue, and 55 effects.
Game-consumed clips live under `Assets/Audio/`; their assignments and current
file statistics are in `Assets/Audio/AudioIndex.csv` and `AudioManifest.json`.
The project opens and builds using these committed clips without generation tools.

On 26 October 2026, approved replacements were installed at the existing asset
paths, preserving their GUIDs and prefab references. Knight, Skeleton, and Zombie
share four plain footstep variations. Slime movement is silent, and its four old
movement clips were removed. Selected coin, dice, Knight landing, and enemy hurt
effects were replaced. Demon hurt was retained; Zombie hurt derives from that
approved demon sound. Knight attack uses Soft swish (A), and Skeleton death uses
Soft bone tumble (B).

On 27 October 2026, all five character hurt effects were replaced with distinct
retro variations derived from the two user-supplied pixel/retro hurt references.
Knight is crisp, Skeleton is sharper, Zombie is lower, Slime bends in pitch, and
Dice Demon is deeper. Source files and processing notes are retained in
[PixelHurt20261027](PixelHurt20261027/README.md). This supersedes the earlier
character hurt selections described above.

The matching [character attack set](CharacterAttacks20261027/README.md) uses short
sword swishes for Knight and Skeleton, a heavier arm swing for Zombie, a liquid
flick for Slime, and a brief charge for Dice Demon. The new Knight attack replaces
the earlier Soft swish selection. Dice Demon's separate fireball launch and impact
effects retain their existing assignments.

Coin pickup is restored to 100% of its approved source amplitude, with one sound per cluster and no additional playback boost. Dice roll and settle use
soft foley at 225% of their approved source amplitudes after later mix increases.
The current character attack set uses its authored levels, replacing the earlier
Knight attack selection and its volume adjustments. Import normalization is disabled on these
replacements to retain their intended levels.

The revised [Ashen Oath death cue](Death20261027/README.md) uses the main menu's flute, plucked accompaniment, and melody in a gentler, slower farewell arrangement. It plays once during the YOU DIED reveal. Its standalone synthesis recipe is retained with its generation notes.

## Credits and licenses

Music and unchanged effects use original procedural synthesis. Replacement
effects combine original synthesis and edited CC0 foley. Source pages, authors,
and licenses are recorded in:

- [Kenney audio sources](FoleyReferences/Kenney/SOURCES.md)
- [OpenGameArt audio sources](FoleyReferences/OpenGameArt/SOURCES.md)

Publisher license texts are retained beside these credits. Edits include trimming,
filtering, layering, pitch/speed adjustments, fades, and gain changes. No recordings
from The Binding of Isaac are included; it was a stylistic reference for the coin.

## Local working files

Automation under `Tools/`, previews under `output/`, downloaded/decoded source
audio, audition history, mix backups, and verification reports are local working
files excluded by `.gitignore`. They are not game dependencies and need not be
present in a fresh checkout. Original character source artwork elsewhere in
`ArtSource/` remains part of the repository.
