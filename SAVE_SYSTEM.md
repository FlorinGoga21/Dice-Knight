# Save system

Start fades out the Start/Exit buttons and replaces them with three matching save-slot buttons. Choosing an empty slot creates a run; choosing an occupied slot loads its saved scene. Escape returns to Start/Exit.

Occupied main-menu slots show health, gold, total playtime, location, and save date on one detail line beneath the heading. Dates use `dd.MM.yy` (for example, `18.10.26`). Their top-right red circle with a white X opens a centered confirmation that fades in and out. Cancel keeps the save; Delete permanently removes its JSON, backup, and temporary file. Deleting the active slot leaves the current session running without autosave until another slot is saved.

The pause menu in MainScene and MapScene has Resume, Save / Load, and Exit Game. Save / Load fades into the Save Game and Load Game submenu, which opens a compact three-slot dialog. The pause button steps back through the dialog and submenu before resuming. Saving over an occupied slot asks for confirmation; the pause button also cancels that confirmation.

Saving, loading, and autosaving are blocked throughout combat. Exit Game remains available during combat and exits without saving. Outside combat it saves the active slot before exiting; if no slot is selected, it asks the player to choose one. A failed save keeps the game open and displays the error.

UI is authored in the scenes. Shared assets are `Assets/Prefabs/SaveSlots.prefab`, `GamePauseMenu.prefab`, and `SaveSystem.prefab`. Edit their objects, sprites, fonts, sizing, and serialized references in Unity. The existing map controller still generates its map nodes.

Each slot Label's TMP Text Input is its runtime template. The main-menu template is `Slot {slot} - {action}` followed by `<size=70%>{details}</size>` on the next line. Edit its literal text, rich-text tags, font size, and style in the scene. Keep placeholders for dynamic values. The SaveSlotPanel component's Slot Text section controls action names (New Game / Load), empty-slot text, and occupied details. Supported values include `{health}`, `{maxHealth}`, `{gold}`, `{playtime}`, `{location}`, `{date}`, `{stats}`, `{active}`, and `{backup}`. Edit outside Play mode to keep changes.

JSON files are stored in `Application.persistentDataPath/Saves/slot_1.json` through `slot_3.json`. Writes replace files atomically and keep the previous valid save as `.json.bak`. An unreadable primary file falls back to its backup; unsupported or invalid data is rejected.

Saved state includes the exact map graph, room types and completion, current room, player health/max health/gold/potions, room position/facing/velocity/camera, enemy health/positions/defeats, combat turn and remaining block, and elapsed play time. Player stats carry between rooms and the map. Defeated enemies load without dropping rewards again.

Autosave defaults to 120 seconds of active gameplay in the selected slot. Configure Autosave Enabled and Autosave Interval on the SaveSystem prefab. It waits until combat, scene transitions, map movement, and coin pickups finish. Paused time does not count toward the timer. Older saves containing combat state remain readable.

EnemyStats instances in MainScene have unique Persistent Save Id values. Give newly placed enemies distinct IDs and keep existing IDs unchanged to preserve their identity in saves. Save schema version 1 is defined in `SaveGameData.cs`; update validation/migration when changing the format.
