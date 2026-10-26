# Dice Knight audio sources

The installed pack has 63 assigned WAVs: 8 original music loops and 55 effects.
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

Coin pickup is at 15% of its approved source amplitude. Dice roll and settle use
soft foley at 225% of their approved source amplitudes after later mix increases.
The new Knight attack uses its auditioned level, replacing the earlier original
attack and its half-volume adjustment. Import normalization is disabled on these
replacements to retain their intended levels.

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
