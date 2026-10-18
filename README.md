# Dice Knight

A 2D pixel-art adventure prototype built in Unity, combining a branching run map with turn-based dice combat.

Choose a route, enter rooms, fight enemies, and continue your adventure across saved sessions.

![Dice Knight main menu](docs/screenshots/main-menu.png)

## Current status

**Playable prototype — actively in development.** The main menu, map, combat rooms, and local save system are connected. The focus is on building out room content and refining the gameplay loop.

### Implemented

- A branching map with room icons, connected routes, and persistent run progress.
- Cloud transitions between the main menu, map, and gameplay rooms.
- Player movement, jumping, and animated characters.
- Turn-based dice combat, enemy encounters, blocking, and healing potions.
- Player health, gold, enemy health, and coin pickups.
- Three JSON save slots with manual save/load, backups, and autosave roughly every two minutes of active gameplay.
- Pause menus in both the map and gameplay scenes, with save details and confirmed save deletion.
- Pixel-style menus with fades, hover effects, and a blurred pause background.

### Still in progress

- Shops, treasure chests, and distinct behavior for each room type.
- More backgrounds and room variety.
- Proper room-completion objectives. **Q currently acts as a temporary shortcut to complete a room and return to the map.**
- Combat balancing, additional content, code cleanup, and visual polish.

Room types are already represented on the map, but currently lead into the shared gameplay scene rather than fully distinct room experiences.

## Screenshots

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
