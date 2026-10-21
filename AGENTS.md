# Dice Knight - agent instructions

These instructions apply throughout this Unity project. Follow the user's current request when it changes a preference or design decision recorded here.

## Working with the user

- Implement authorized work through verification; use reasonable judgment for routine choices. Ask only when missing information materially affects the result or an action genuinely requires permission.
- During substantial work, give occasional concise progress updates with a rough percentage. Percentages are estimates of task completion, not promises about remaining time. Explain important findings or changes in direction.
- Report what changed, what was checked, and any remaining limitations. Do not claim a successful build, test, or visual check without evidence.
- The user manages GitHub. Do not install or connect GitHub integrations, operate remote repositories, publish branches, or create pull requests unless explicitly asked. Leave staging, commits, and branch management to the user by default. Read-only local Git inspection is fine.
- Preserve unrelated edits and unsaved Editor work. Do not reset the checkout, overwrite source art, or change project-wide settings just to simplify a task. Restore temporary verification settings afterward.

## Project context and navigation

- Dice Knight is a single-player 2D pixel-art adventure with a branching run map and turn-based dice combat.
- Use the Editor version in `ProjectSettings/ProjectVersion.txt` and packages in `Packages/manifest.json`. The project currently uses Unity 6, URP 2D, C#, Unity Input System, uGUI, and TextMeshPro.
- Gameplay code lives in `Assets/Scripts/`, grouped into `Combat`, `Enemy`, `Environment`, `Interfaces`, `Player`, `SaveSystem`, and `UI`.
- Main scenes: `Assets/Scenes/MainMenu.unity`, `MapScene.unity`, and `MainScene.unity`.
- Enemy prefabs and the room spawner live in `Assets/Prefabs/Enemies/`. Project art, materials, shaders, and audio have corresponding folders under `Assets/`.
- Read relevant code and configuration before changing behavior. `README.md`, `ENEMY_SPAWNING.md`, and applicable notes under `docs/` or `ArtSource/` provide context, but may lag behind implementation. Surface meaningful discrepancies rather than silently assuming the notes are current.

## C# and Inspector style

- Use four-space indentation and opening braces on their own lines. Follow the surrounding file's conventions.
- Use PascalCase for types, methods, and properties, and camelCase for new private fields and local variables. Prefer `[SerializeField] private` fields for Inspector configuration rather than public fields, except intentionally serializable data models.
- **Inspector headers must use exactly this style:** `[Header("---Header Title---")]`. Use three hyphens on each side and no padding spaces next to them. For example:

  ```csharp
  [Header("---Movement Settings---")]
  [SerializeField, Min(0f)] private float moveSpeed = 2f;
  ```

- Apply the header style to new or modified Inspector groups. Do not reformat unrelated files merely to standardize existing headers.
- Use informative tooltips for non-obvious configuration, especially units and save identifiers. Use Inspector constraints and validation where they prevent invalid configuration.
- Preserve serialized field names and Unity event method names. If a serialized rename is necessary, preserve existing assignments with an appropriate migration such as `[FormerlySerializedAs]`, and verify the affected assets.
- Keep responsibilities focused. Reuse the existing partial-class organization for large controllers instead of adding unrelated behavior to one growing file.
- Prefer extending existing components and small interfaces over introducing general-purpose frameworks, additional global managers, or new dependencies for a local feature.
- Cache frequently used component references. Avoid repeated scene searches, allocations, and expensive work in frame loops when existing references or events suffice.
- Keep runtime code free of `UnityEditor` dependencies. Put compiled Editor-only tools in `Assets/Editor/` or an appropriate Editor assembly.
- Maintain the existing Input System and uGUI/TextMeshPro approach for related work; do not introduce a second input or UI framework without a concrete task requirement.

## Unity assets and Editor workflow

- Prefer the connected Unity Editor and Unity CLI/Pipeline for scene, prefab, animation, and importer changes. Discover available commands rather than guessing command names or parameters. Follow applicable Unity skills.
- Diagnose connection and compilation problems before changing serialized assets. Do not treat an unreachable Editor as permission to guess at scene or prefab YAML.
- Use Unity asset, prefab, and sprite importer APIs for authored asset changes. Preserve GUIDs, sprite identities, prefab references, and Inspector wiring. Do not hand-edit sprite `.meta` data or casually regenerate metadata.
- Preserve external packages and imported assets under `Assets/External Assets/` and `Assets/TextMesh Pro/`. Prefer project-owned variants, materials, and copies when customizing third-party content.
- When editing a prefab variant, change only the intended variant. Do not apply overrides to its parent or alter sibling enemies unless requested. For example, zombie tuning must not change the skeleton.
- Keep temporary rendering, sampling, and inspection objects in preview scenes or disposable Play mode state. Clean up objects, textures, and materials that tools create.
- Do not upgrade Unity, packages, render settings, or project-wide configuration as a side effect of an unrelated task.
- Do not edit generated folders or generated solution/project files as source. `Library/`, root `Temp/`, `Logs/`, `obj/`, and `UserSettings/` are disposable or machine-specific.
- Keep reusable standalone Unity automation in `Tools/Unity/`; keep compiled Editor tools in `Assets/Editor/`. Root `Temp/` is for disposable work, never the only copy of useful tooling or required game assets. `Assets/Temp/` is an ordinary asset folder and must be inspected before cleanup.

## Agreed room architecture

This is the planned direction for future room work, not a claim that every room feature already exists:

- Keep `MainMenu`, `MapScene`, and one shared `MainScene` for gameplay rooms. Reloading `MainScene` for each selected map node is acceptable.
- Configure room content with reusable prefabs and small controllers for combat, shop, treasure, event/random, and boss rooms. Avoid one controller containing every room's behavior.
- Treat room type and environment as independent choices. Forest rooms come early; dungeon environments are planned for later progression. A dungeon does not require a separate scene for every room type.
- Keep shared player, camera, HUD, pause, and room-exit behavior reusable. A substantially different room layout may justify a separate scene when a real requirement arises.
- Preserve selected environments, encounters, rewards, and shop stock in run state/saves as those features are implemented; loading must not reroll room contents.
- The intended implementation order is a shop room first, then the dungeon environment. Do not begin those features merely because they are documented here.

## Gameplay and save compatibility

- Extend existing combat, health, currency, map, and save systems rather than duplicating them for new enemies or rooms.
- A full heart represents 20 HP and a half heart represents 10 HP; use the existing `HealthHeartsUI` constants. Current approved enemy tuning includes skeleton 20 HP and zombie 40 HP. Change balance only within the user's requested scope.
- Enemy dice counts follow the player's available dice through the existing controller. Weapon variations currently change appearance, not damage or timing.
- Treat spawner IDs, enemy prefab IDs, weapon names, save IDs, and serialized enum values as persistent data. Do not rename, reorder, or repurpose them without considering existing saves. Prefer appending enum values over inserting them into an existing sequence.
- Preserve exact saved enemy lineups, health, weapons, defeated states, and room progress. An empty saved room must stay empty, and restoring defeated enemies must not award loot again.
- Keep authored definitions separate from mutable run/save state. ScriptableObjects may hold reusable configuration; do not use shared asset instances as writable player save files.
- Never overwrite the user's saves during verification. Use isolated fixtures or temporary save locations for persistence tests.

## Pixel art and animation

- Match the player's and skeleton's SPUM style: chunky silhouettes, simple hands and feet, consistent proportions, clear outlines, and readable details at actual game size.
- For sprite textures, preserve point filtering, appropriate pixels-per-unit values, alpha, pivots, and uncompressed/no-mipmap settings where used by the existing asset workflow.
- Keep source artwork and relevant generation/export notes under `ArtSource/`. Put game-consumed assets under `Assets/Art/` and previews under `output/previews/` or disposable temporary folders.
- The zombie uses the reference-inspired head and tunic with native SPUM limb proportions. Its torso has no fixed shoulder flaps. Sleeves are a few recolored pixels in zombie-specific copies of the original arm sprites, with unchanged silhouettes and no sleeve overlays.
- Check idle, movement, attack, hurt, and death after changing character art or rigging. Verify weapon grips, layering, ground contact, facing, and consistency with other characters. Use the same camera scale and pose timing for comparisons.

## Verification and completion

- Match verification to the change. Documentation-only changes need content and diff checks, not a Unity build. Art-only changes need visual inspection, not boilerplate tests.
- For C# changes, verify Unity compilation and inspect relevant console errors. For gameplay changes, exercise the affected path in Play mode when the Editor is available.
- Before trusting a live game capture, confirm that frames are advancing. Sampled animation previews verify poses; they do not prove live gameplay or combat works.
- For changes to dice rules, saves, encounter sequencing, or map progression, prefer focused, repeatable tests of behavior and important edge cases. Unity Test Framework is already installed. Avoid tests that only repeat implementation details.
- Stop and restore Play mode state after tests unless the user requests otherwise. Preserve the original scene, project settings, and unrelated console state; do not hide errors by clearing the console.
- Review the final diff for unintended assets, source changes, or generated files. Update relevant project notes when behavior or setup changes, and report any checks that could not be completed.
