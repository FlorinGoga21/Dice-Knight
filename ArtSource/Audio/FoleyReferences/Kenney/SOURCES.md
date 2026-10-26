# Foley sources for revision 03 previews

Retrieved from the publisher on 2026-10-26:

| Pack | Publisher page | License |
| --- | --- | --- |
| Impact Sounds 1.0 | https://kenney.nl/assets/impact-sounds | CC0-1.0 |
| Casino Audio 1.1 | https://kenney.nl/assets/casino-audio | CC0-1.0 |
| RPG Audio 1.0 | https://kenney.nl/assets/rpg-audio | CC0-1.0 |

Original ZIPs and extracted recordings are preserved locally and ignored by Git.
Each pack's `License.txt` is retained in version control alongside these credits.
The local `decoded/` folder contains WAV derivatives made with Unity's local
OGG decoder; stereo sources are converted to mono and, if necessary, resampled to
44.1 kHz when the preview generator reads them.

Local audition manifests record the source filenames for edited candidates.
Edits include selecting contacts, trimming, mixing simple footfalls, removing
excess treble, controlling level, and adding short fades. Coin/liquid/vocal layers
are original synthesis; none use samples from the user's game reference.

The installed edits are committed under `Assets/Audio/`; raw source recordings
and rejected auditions are not imported into the Assets tree. Kenney's name and branding are not part of
the audio license grant; the preview set does not imply endorsement.
