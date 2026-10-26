# Forest background parallax

`Background.cs` handles horizontal camera parallax and infinite repetition in both `MainMenu` and `MainScene`. The settings are saved on `Assets/Prefabs/Background.prefab` and the existing scene instances.

## Layer settings

`Parallax Factor` is the fraction of horizontal camera movement that a layer follows: **0** stays in world space; **1** follows the camera completely. Larger values make scenery appear farther away.

| Sprite | Role | Factor |
| --- | --- | --- |
| `Layer_0000_9` | Ground silhouette and extended foreground masks | 0 |
| `Layer_0001_8` | Grass, anchored to the walking surface | 0 |
| `Layer_0002_7`, `Layer_0003_6` | Near canopy and trunks | 0.12 |
| `Layer_0004_Lights`, `Layer_0005_5` | Midground light shafts and trees | 0.38 |
| `Layer_0006_4`, `Layer_0007_Lights` | Deeper trees and matching light shafts | 0.58 |
| `Layer_0008_3` | Distant trunks | 0.78 |
| `Layer_0009_2` | Farthest forest silhouettes | 0.90 |
| `Layer_0011_0` | Flat distant backdrop | 1 |

The unused `Layer_0010_1` sky asset would use 1; it has not been added to either scene. Matching light and tree layers use the same factor to keep their composition together.

## Infinite scrolling

- Keep **Use Sprite Width** enabled for these layers. It uses the scaled sprite bounds so repeated edges meet. **Chunk Width** remains available for art that deliberately uses custom repeat spacing.
- Matching sibling sprites share one owner. Authored copies are reused, and only enough extra tiles are created to cover the camera plus a margin. Containers without a sprite do not clone the forest hierarchy.
- Tile placement uses camera position directly, supporting reverse scrolling and large jumps. Widening the camera adds coverage once; repeated scrolling does not continually add tiles. Generated tiles are removed with their owner.
- Parallax is horizontal only, preserving authored heights and vertical camera behavior. The camera must be orthographic. An unassigned camera falls back to `Camera.main`.
- To tune a layer, give all of its authored copies the same factor. Matching also includes sprite, material, color, sorting, transform scale/rotation, and vertical position.

## Local Editor tools

The optional scripts below live in the local, ignored `Tools/` folder and are not included in a fresh checkout. Existing scenes already contain the configuration. If those local tools are available, run them from the project root with the Unity Editor connected:

```powershell
# Edit mode, with no unsaved scene edits. Reapply the table above to prefab and scenes.
unity command script run --caller plugin --skill unity-cli --format json --file Tools/Unity/ConfigureForestParallax.cs --entry ConfigureForestParallax.Main

# Play mode, after frames have advanced. Temporarily moves the camera and restores it afterward.
unity command script run --caller plugin --skill unity-cli --format json --file Tools/Unity/VerifyBackgroundScrolling.cs --entry VerifyBackgroundScrolling.Main
```

Verification passed in both scenes: 12 independently repeated layers, 360 coverage checks per scene, correct parallax ratios, reverse scrolling, camera jumps up to 5,000 units in either direction, and a wider orthographic view. Tile count stayed stable after viewport expansion. Unity compilation and console checks were clean.
