# Dice Knight

A 2D pixel-art adventure prototype built in Unity, combining a branching run map with turn-based dice combat.

Choose a route, enter rooms, fight enemies, and continue your adventure across saved sessions.

![Dice Knight main menu in motion](docs/screenshots/main-menu.webp)

## Current status

**Playable prototype — actively in development.** The main menu, map, combat rooms, and local save system are connected. The focus is on building out room content and refining the gameplay loop.

### Implemented

- A branching map with room icons, connected routes, and persistent run progress.
- Cloud transitions between the main menu, map, and gameplay rooms.
- Player movement, jumping, and animated characters.
- Run-based player levels with 100 XP per enemy, a framed teal XP bar, icon-only health/Luck/damage upgrades, selection feedback, and saved progression. See [player levels](docs/PLAYER_LEVELS.md).
- Inspector-assignable Basic, Lucky, Even, and Odd Dice, with matching rolling/result sprites and saved equipped types. Lucky Dice independently use the player's Luck stat for a guaranteed-higher reroll; Even and Odd Dice favor their lower faces.
- Layered forest parallax with seamless infinite scrolling in the menu and gameplay. See [background tuning](docs/BACKGROUND_PARALLAX.md).
- Room camera limits and walk-off exits; combat exits unlock after defeating all enemies, and empty rooms stay within their initial screen. See [room boundaries](docs/COMBAT_ROOM_BOUNDARIES.md).
- Turn-based dice combat, enemy encounters, healing potions, and one Block per encounter, usable before rolling or after seeing the final results.
- Animated slimes, skeletons, zombies, and a hovering DiceDemon with fireball attacks. Combat rooms spawn enemies from an editable pool, with enemy dice matching the player's available dice.
- Random SPUM melee weapons for skeletons and zombies, preserved with the room's saved enemy lineup. See [enemy spawning](ENEMY_SPAWNING.md) for Inspector setup.
- Player health, gold, enemy health, and coin pickups.
- Three JSON save slots with manual save/load, backups, and autosave roughly every two minutes of active gameplay.
- Pause menus in both the map and gameplay scenes, with save details and confirmed save deletion.
- Pixel-style menus with fades, hover effects, and a blurred pause background.
- Controller navigation for menus, save slots, the run map, pause screens, and combat actions. See [controller support](docs/CONTROLLER_SUPPORT.md).
- Settings available from a small top-right gear on the main menu and both pause screens, with saved Music/SFX volume, resolution, and fullscreen preferences.
- A scrollable Controls page with TextMeshPro input glyphs that follow the active keyboard/mouse or controller scheme.
- A retro-fantasy audio pack: **8 looping music tracks and 55 sound effects**, combining original synthesis with approved CC0 foley replacements.
- Consistent full-strength white damage flashes for the player and every enemy, matching DiceDemon. See [damage flash](docs/DAMAGE_FLASH.md).

### Development update — 28 October 2026

- Restyled the **XP bar and upgrade choices** with dark wood, brass trim, a shaded teal fill, readable label backings, and compact plus badges. Added clover artwork and a short hop/125% size pulse on the chosen upgrade; queued choices refresh after the confirmation finishes.
- Added reusable **dice definitions** for Basic, Lucky, Even, and Odd, separating their saved identities, abilities, and visuals from the player's mutable run state. Equipped types can be assigned per slot in the Inspector and survive save/load and room changes.
- Moved the **Lucky reroll** onto green Lucky Dice. Each active Lucky Die independently checks the player's Luck once per roll and, on success, rerolls its own face to a strictly higher value. Basic, Even, Odd, and enemy dice do not receive this ability. Earned Luck still increases by 5 percentage points per upgrade, capped at 25%.
- Added turquoise/ivory **Even Dice** and copper/charcoal **Odd Dice**, preserving the normal sprite-sheet layouts, pips, alpha, sprite cuts, and rolling frames. Their low/middle/high results use matching weights of **40% / 33⅓% / 26⅔%**: 2/4/6 for Even and 1/3/5 for Odd. Basic and Lucky initial rolls remain uniform d6 rolls.
- Added an **Editor-only Luck testing slider** on PlayerProgression, from 0–100%, without changing earned upgrades or saved Luck. Large combat results now use each player's die type rather than always showing the first slot's art.
- Allowed **reactive Block** during the final result-viewing interval, after Lucky rerolls and before attack resolution. It still consumes only one use per encounter. Player dice and their total shrink/fade before the shield rises, grows to 110%, and settles; the enemy results stay visible, and the shield recoils on blocked impact.
- Fixed **pause-menu controller navigation** with explicit loops for the main pause actions and Save/Load submenu, skipping disabled Save/Load during combat and keeping focus away from gameplay controls behind the overlay.
- Reviewed alternative layered backgrounds already present in the external assets, including caverns, mountains, castles, and a night town. The game's forest background remains in place; no environment swap was implemented.
- Unity compilation passed with no errors or warnings during implementation. Checks outside Play mode covered dice behavior, 60,000 weighted rolls per Even/Odd type, original sprite-sheet parity, isolated save compatibility, Luck override isolation, Block eligibility, and pause-menu navigation links. Static UI previews were inspected. **Live controller operation, animation timing, and full gameplay testing remain with the user.**

### Development update — 26 October 2026

- Added saved **resolution selection** and refined the Settings layout, including a lower title and a Controls button beside Back.
- Completed the scrollable **Controls** page with Input Sprites for TextMesh Pro. Glyphs follow the current keyboard/mouse, Xbox/generic gamepad, PlayStation, or Nintendo device. WASD and arrow keys use familiar directional clusters; Back is centered on the Controls page.
- Added the same Settings and Controls screens to the map and gameplay pause menus. The small gear sits at the **screen's top-right corner** and matches the main menu's displayed size. Closing Settings returns to pause; Back/Cancel closes one level at a time without resuming gameplay.
- Replaced the selected coin, dice, footsteps, landing, and hurt effects after auditioning alternatives. Knight, Skeleton, and Zombie now share simple footsteps; Slime movement is silent. The approved demon hurt is retained, and Zombie hurt uses a related demonic sound.
- Installed the soft-swish **Knight attack A** and bone-tumble **Skeleton death B**. Coin pickup is restored to 100% of its approved source amplitude and plays once per cluster; softer dice roll/settle recordings are now at 225% of their approved source amplitude. Replaced clips preserve their existing asset references; superseded Slime movement clips were removed.
- Kept the current audio set at **63 clips: 8 music loops and 55 effects**. Audio sources and credits are documented in the [audio notes](ArtSource/Audio/README.md).
- Excluded local automation, generated previews, audition archives/backups, verification reports, and personal editor settings from version control. Game assets, Unity metadata, packages, project settings, documentation screenshots, source art, and audio credits remain part of the project.
- Checked Unity compilation, saved menu wiring, and imported audio levels. Final pause-menu gameplay and visual checks are left to the user; no Play mode checks were run for the pause-settings addition.

### Development update — 25 October 2026

- Added controller menu focus, explicit save-slot/Delete navigation, highlighted reachable map nodes, and Back handling through settings and pause dialogs. Combat keeps direct Roll/Heal/Block shortcuts without duplicate UI submission.
- Added the PixelUIFree-style **Settings** overlay with continuous volume sliders, percentage readouts, a fullscreen checkbox, and controller navigation. Preferences persist independently of run saves; fullscreen changes apply in a built player.
- Added independent music, gameplay SFX, and UI SFX playback. Music transitions between menu, map, room, combat, and boss contexts, while UI sounds remain available during pause.
- Generated and assigned **67 original synthesized clips** under `Assets/Audio/`, including separate character effects and four movement variations each for Knight, Skeleton, Zombie, and Slime. Zombie overrides Skeleton's inherited audio fields.
- Added a [sound assignment checklist](docs/SOUND_ASSIGNMENTS.md), an [audio file index](Assets/Audio/AudioIndex.csv), and local generation/import tools, now excluded from version control.
- Checked Unity compilation during implementation, the settings layout in a disposable preview scene, WAV file integrity, and all 67 saved audio references. Play mode and listening/balance checks remain for manual testing.

### Development update — 21 October 2026

- Added the **Zombie** enemy with **40 HP (two full hearts)**, shared SPUM melee weapons, and idle, movement, attack, hurt, and death animations. Its custom artwork, proportions, hands, feet, and painted sleeve pixels match the existing character style. Skeleton health remains 20 HP.
- Added depth-based **forest parallax** in the menu and gameplay, keeping the ground anchored and light shafts aligned with their tree layers. Infinite scrolling supports reverse movement, camera jumps, and wider views. See [background tuning](docs/BACKGROUND_PARALLAX.md).
- Added **MainScene room boundaries** with a stable invisible left wall and camera limits. Combat rooms unlock their exit after all enemies are defeated; the camera stops at the right edge while the player walks fully off screen to return to the map.
- Gave the currently empty shop, random, and treasure rooms boundaries at their initial screen edges and an immediately available walk-off exit. Their individual content and interactions are still to come. MainMenu has no room boundaries.
- Extended room saves to preserve boundary positions and restore cleared exits. Room completion waits for combat cleanup and coin rewards, and holding movement against the left wall stops the walking animation and dust without jitter. See [room boundaries](docs/COMBAT_ROOM_BOUNDARIES.md).
- Added [project contribution instructions](AGENTS.md), including Inspector header conventions, plus local Unity configuration and verification tools.

### Development update — 20 October 2026

- Split large gameplay and UI scripts into focused files, shared the floating-number visuals, and grouped save code into its own folder.
- Replaced hand-placed combat enemies with a configurable spawner. The prefab defaults to 2–3 enemies per combat room; the scene can override the pool, count, and spacing.
- Added the animated **Slime** and **DiceDemon**. DiceDemon hovers, shoots animated fireballs, has three hearts, and lands before its death animation. Its palette, outline, size, and idle eye animation have also been refined.
- Added **19 random SPUM melee weapon variants** for skeletons, excluding spears. Weapon selection changes appearance while keeping the existing attack behavior.
- Extended saves to retain the enemy lineup, weapons, health, defeated state, and DiceDemon's hover position. Fixed player targeting after loading so every enemy matches the player's current dice count.
- Improved spacing for combat roll totals, including two-digit results, and addressed input and health-display initialization issues.
- Replaced the static main-menu header image with a **60 FPS animated WebP**.

### Still in progress

- Shops, treasure chests, and distinct behavior for each room type.
- More backgrounds and room variety.
- Distinct objectives for other room types. Rooms finish when the player walks off the unlocked right screen edge; Q remains a development shortcut.
- Combat balancing, additional content, code cleanup, and visual polish.

Room types are already represented on the map, but currently lead into the shared gameplay scene rather than fully distinct room experiences.

## Screenshots and sprite previews

**New enemy: Zombie**

<img src="docs/screenshots/zombie.gif" alt="Zombie enemy preview showing idle, movement, attack, hurt, and death animations" width="240">

The zombie shares the skeleton's animation rig and melee weapon pool, with custom artwork and two full hearts. The hammer is one of its weapon variants.

**New enemies: DiceDemon and Slime**

<img src="docs/screenshots/enemy-sprites.png" alt="DiceDemon and Slime using their existing pixel-art sprite frames" width="420">

Sprite previews from the game assets. DiceDemon also uses a palette and outline material in game.

**Skeleton weapon variety**

![Skeletons equipped with all 19 currently allowed SPUM melee weapons](docs/screenshots/skeleton-weapons.png)

The current weapon pool includes swords, axes, daggers, a hammer, and a mace. Weapons are chosen on spawn and restored when loading a saved room. See [enemy spawning](ENEMY_SPAWNING.md) for setup and tuning.

**Run map**

![Branching run map and room types](docs/screenshots/run-map.png)

**Combat prototype**

![Player and enemy encounter with dice combat controls](docs/screenshots/dice-combat.png)

Screenshots show the current development build; visuals and gameplay may change.

## Dice types and Inspector setup

| Type | Appearance | Roll behavior |
| --- | --- | --- |
| Basic | White faces, dark pips | Uniform results from 1 through 6 |
| Lucky | Green faces, dark pips | Uniform d6 roll; each Lucky Die independently checks player Luck for one guaranteed-higher reroll. A six cannot reroll. |
| Even | Deep turquoise faces, ivory pips | 2: 40%; 4: 33⅓%; 6: 26⅔% |
| Odd | Copper-orange faces, charcoal pips | 1: 40%; 3: 33⅓%; 5: 26⅔% |

In **MainScene**, search the Hierarchy for `t:DiceRollUI`, then assign definitions from [`Assets/Data/Dice/`](Assets/Data/Dice/) under **Dice Types → Equipped Dice**. Keep all used definitions in **Available Dice** so saved type IDs can be resolved. Adding a new type preserves the current equipped slots. There is no dice inventory or acquisition screen yet.

To test Luck, select the gameplay player's **PlayerProgression** component, enable **Luck Testing → Override Luck For Testing**, and adjust **Test Luck Percent**. At 100%, every eligible Lucky Die rerolls; other die types stay unaffected. Disable the override to return to earned Luck. It is Editor-only and is not written into run saves.

Block works before rolling or during the existing **Roll Display Duration** after final results appear. It becomes unavailable once attack resolution starts or its encounter use is spent. See [player levels and dice](docs/PLAYER_LEVELS.md) and [controller support](docs/CONTROLLER_SUPPORT.md) for details and verification limits.

## Controls

| Input | Action |
| --- | --- |
| A / D | Move left / right |
| Space | Jump while exploring; roll dice during combat |
| Left mouse button | Attack while exploring |
| H | Use a healing potion during combat |
| B | Block before rolling or while final results are displayed (one use per encounter) |
| Mouse | Navigate menus, select connected map nodes, and use combat buttons |
| Esc | Pause, resume, or go back through pause-menu screens |
| Q | Complete the current room and return to the map (temporary development shortcut) |

Controller controls (Xbox names, with PlayStation equivalents):

| Input | Action |
| --- | --- |
| Left stick | Move while exploring; navigate menus |
| D-pad | Navigate menus and reachable map rooms |
| A / Cross | Confirm menus, jump while exploring, or roll dice in combat |
| X / Square | Attack while exploring; use a healing potion in combat |
| B / Circle | Back through menus; block during combat |
| Start / Options or View / Share | Pause or resume |
| Y / Triangle | Return to the map after clearing the room and collecting rewards |

Saving and loading are disabled during combat. Exiting outside combat saves first; exiting during combat leaves the last save unchanged.

## Settings and audio

The small **Settings gear** at the top right of the screen while paused on the map or in gameplay
uses the main menu's gear artwork and styling. It opens the same audio/display options and Controls page
as the main menu. Back returns to the pause menu while the game stays paused.

Open the top-right gear on the main menu to adjust **Music**, **SFX**, **Resolution**, and **Fullscreen**. Use the resolution arrows to cycle through available display sizes; refresh-rate duplicates appear once. Music and effects have separate volume controls; both gameplay and interface sounds follow SFX volume. Settings apply immediately and persist when closing the overlay or pausing/quitting the app. Music continues through pause while current gameplay effects pause. Display changes apply in a built player; fullscreen uses borderless fullscreen, and the Unity Editor window is unchanged. **Controls**, beside **Back**, shows exploration, combat, and menu bindings from `Assets/Settings/PlayerInputActions.inputactions`. Input Sprites for TextMesh Pro glyphs switch automatically between keyboard/mouse, Xbox/generic gamepad, PlayStation, and Nintendo layouts when those devices are used. Small stick drift and button releases do not change the displayed device. Scroll with the mouse wheel, scrollbar, keyboard navigation/Page Up/Page Down, or controller sticks/D-pad. The centered Back button or Cancel returns from Controls to Settings before closing the overlay. Reopening Controls resets the list to the top without duplicating entries.

All sound files are organized under [`Assets/Audio/`](Assets/Audio/README.md): music in `Music/`, shared effects in `SFX/UI/`, `SFX/Combat/`, and `SFX/World/`, and character effects in `SFX/Characters/`. The current pack has 8 original music loops and 55 effects, including the approved synthesis/CC0 foley replacements. Knight, Skeleton, and Zombie share the same simple footsteps; Slime movement is silent. Source credits and licenses are retained under `ArtSource/Audio/`.

To replace sounds in the Inspector:

- Open `Assets/Prefabs/GameAudio.prefab` for music and shared menu, combat, and world effects.
- Open `Assets/Art/Player/Knight.prefab` or an enemy prefab under `Assets/Prefabs/Enemies/` and edit **Character Audio** for movement, attack, hurt, death, and applicable projectile sounds.
- Follow the [field-by-field assignment checklist](docs/SOUND_ASSIGNMENTS.md) or [audio index](Assets/Audio/AudioIndex.csv). Unused character-action fields can remain empty.

The [audio notes](ArtSource/Audio/README.md) describe the installed sounds and source credits. The game uses the committed clips directly; local generation tools and audition history are not required to open or build it.

The player and all four enemies now use distinct hurt effects derived from two user-supplied retro sound references. The [hurt sound notes](ArtSource/Audio/PixelHurt20261027/README.md) record the sources and character variations.

Matching [attack effects](ArtSource/Audio/CharacterAttacks20261027/README.md) cover sword swings, Zombie's heavier arm swing, Slime's liquid flick, and Dice Demon's short charge. Its fireball launch and impact cues remain separate.

## Repository contents

Keep `Assets/` and its `.meta` files, `Packages/`, shared `ProjectSettings/`, source artwork under `ArtSource/`, and project documentation in version control. Documentation images live under `docs/screenshots/`.

`.gitignore` excludes Unity caches/builds, personal editor settings, local Pipeline connection settings, `Tools/`, `output/`, and audio auditions/backups/reports. Downloaded foley archives and decoded copies stay local; their source credits and license files remain included. These local files are not needed to open or build the game. Ignore rules do not remove files that Git already tracks.

## Running the project

1. Clone the repository with **Git LFS** installed so image, audio, and other binary assets download correctly. Run `git lfs pull` if the assets were not downloaded during cloning.
2. Add the project folder to **Unity Hub**.
3. Open it with **Unity 6000.4.3f1** and let Unity import the assets and restore packages.
4. Open `Assets/Scenes/MainMenu.unity` and press **Play**.
5. Choose **Start Game**, then select an empty slot for a new adventure or an occupied slot to continue.

Built with **C#**, **Universal Render Pipeline**, **Unity Input System**, and **uGUI / TextMeshPro**.
