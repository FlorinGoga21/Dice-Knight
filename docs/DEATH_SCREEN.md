# Player death and run reset

Player death now ends the current run instead of quitting the application. The
player's `Death Screen Prefab` reference on `Assets/Art/Player/Knight.prefab` points
to `Assets/Prefabs/UI/DeathScreen.prefab`.

At lethal damage, saving and loading the ended run are blocked immediately. The
active slot's primary checkpoint, temporary checkpoint, and backup are removed.
A durable `.ended` marker prevents backup recovery from reviving the run even if
file cleanup is interrupted. Reusing that slot removes stale checkpoints before
writing a fresh run. Other save slots and volume/control preferences are preserved.

Carried player stats, elapsed run time, pending restoration, tracked enemies, and
the map state are cleared in memory. Combat input and displays stop, and room exit
and pause input cannot bypass the death screen. The existing death animation and
sound play before the world freezes.

The screen fades in a crimson Cinzel `YOU DIED` title over a darkened scene and
soft black banner. After a short hold, `NEW RUN` and `MAIN MENU` appear. Timing is
configured on `DeathScreenController` in real seconds, so fades and buttons work
while the world is frozen. Mouse, keyboard, and controller use the existing uGUI
EventSystem and Input System.

Both exits fade out the title/buttons and use the shared cloud cover/load/reveal
transition, matching the menu and room navigation. `Transition Duration` controls
each half in real seconds (default 0.8). The death Canvas is sorted immediately
below the transition Canvas so the clouds cover the full screen.

`Ashen Oath`, an original 16-second death cue, starts when the title begins its
reveal. It plays once through the existing Music source and volume setting.
Normal music fades out during the death animation; the destination scene's music
returns after choosing either exit. The clip is assigned to `Death Music` on the
GameAudio prefab. Source and generation notes are in
`ArtSource/Audio/Death20261027/`.

- **New Run:** creates a fresh run in the ended slot and opens `MapScene`. The map
  is generated again; entering a room uses the authored starting player, dice,
  equipment, potions, and enemies. A direct Editor run without an active save slot
  restarts without selecting or overwriting a slot.
- **Main Menu:** returns to `MainMenu`; the ended slot is empty in the existing
  slot picker. Other independent slots remain available.

Font: Cinzel by the Cinzel Project Authors, distributed under the SIL Open Font
License. The source font, license, and static TextMeshPro atlas are in
`Assets/Art/UI/Fonts/`. `DeathBanner.png` is a generated UI opacity gradient,
imported through Unity's sprite importer.

## Verification

Compilation, prefab references, static preview layouts at 1920x1080, 1280x1024,
and 2560x1080, and isolated save-file/run-state checks were checked outside Play
mode. Local repeatable tools are in `Tools/Unity/BuildDeathScreen.cs`,
`PreviewDeathScreen.cs`, and `VerifyDeathRunReset.cs`; generated previews and
reports are under ignored `output/` paths. Save fixtures use a unique directory
under `Temp/DeathScreenVerification/` and do not read or write player saves.

Play mode testing is intentionally left to the user. Check lethal damage during
a dice encounter, the death animation and reveal timing, keyboard/controller and
mouse button operation, both scene exits, fresh starting stats after New Run,
and reopening the menu after death to confirm the ended slot cannot continue.
Use a disposable run for this check: death intentionally erases its active slot.
