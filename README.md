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
- Layered forest parallax with seamless infinite scrolling in the menu and gameplay. See [background tuning](docs/BACKGROUND_PARALLAX.md).
- Room camera limits and walk-off exits; combat exits unlock after defeating all enemies, and empty rooms stay within their initial screen. See [room boundaries](docs/COMBAT_ROOM_BOUNDARIES.md).
- Turn-based dice combat, enemy encounters, blocking, and healing potions.
- Animated slimes, skeletons, zombies, and a hovering DiceDemon with fireball attacks. Combat rooms spawn enemies from an editable pool, with enemy dice matching the player's available dice.
- Random SPUM melee weapons for skeletons and zombies, preserved with the room's saved enemy lineup. See [enemy spawning](ENEMY_SPAWNING.md) for Inspector setup.
- Player health, gold, enemy health, and coin pickups.
- Three JSON save slots with manual save/load, backups, and autosave roughly every two minutes of active gameplay.
- Pause menus in both the map and gameplay scenes, with save details and confirmed save deletion.
- Pixel-style menus with fades, hover effects, and a blurred pause background.

### Development update — 21 October 2026

- Added the **Zombie** enemy with **40 HP (two full hearts)**, shared SPUM melee weapons, and idle, movement, attack, hurt, and death animations. Its custom artwork, proportions, hands, feet, and painted sleeve pixels match the existing character style. Skeleton health remains 20 HP.
- Added depth-based **forest parallax** in the menu and gameplay, keeping the ground anchored and light shafts aligned with their tree layers. Infinite scrolling supports reverse movement, camera jumps, and wider views. See [background tuning](docs/BACKGROUND_PARALLAX.md).
- Added **MainScene room boundaries** with a stable invisible left wall and camera limits. Combat rooms unlock their exit after all enemies are defeated; the camera stops at the right edge while the player walks fully off screen to return to the map.
- Gave the currently empty shop, random, and treasure rooms boundaries at their initial screen edges and an immediately available walk-off exit. Their individual content and interactions are still to come. MainMenu has no room boundaries.
- Extended room saves to preserve boundary positions and restore cleared exits. Room completion waits for combat cleanup and coin rewards, and holding movement against the left wall stops the walking animation and dust without jitter. See [room boundaries](docs/COMBAT_ROOM_BOUNDARIES.md).
- Added [project contribution instructions](AGENTS.md), including Inspector header conventions, plus reusable Unity configuration and verification tools under `Tools/Unity/`.

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

## Controls

| Input | Action |
| --- | --- |
| A / D | Move left / right |
| Space | Jump |
| Mouse | Navigate menus, select connected map nodes, and use combat buttons |
| Esc | Pause, resume, or go back through pause-menu screens |
| Q | Complete the current room and return to the map (temporary development shortcut) |

Saving and loading are disabled during combat. Exiting outside combat saves first; exiting during combat leaves the last save unchanged.

## Running the project

1. Clone the repository with **Git LFS** installed so image, audio, and other binary assets download correctly. Run `git lfs pull` if the assets were not downloaded during cloning.
2. Add the project folder to **Unity Hub**.
3. Open it with **Unity 6000.4.3f1** and let Unity import the assets and restore packages.
4. Open `Assets/Scenes/MainMenu.unity` and press **Play**.
5. Choose **Start Game**, then select an empty slot for a new adventure or an occupied slot to continue.

Built with **C#**, **Universal Render Pipeline**, **Unity Input System**, and **uGUI / TextMeshPro**.
