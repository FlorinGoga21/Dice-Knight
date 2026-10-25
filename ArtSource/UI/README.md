# Settings icon assets

`Tools/Unity/ConfigureSettingsGear.cs` draws the project-owned round wood button and focus ring at 24x24 pixels, and the gear icon at 16x16 pixels. They extend PixelUIFree's existing UI style without modifying the imported kit. Their PNGs are under `Assets/Art/UI/`, imported as point-filtered, uncompressed sprites with alpha and no mipmaps at 100 pixels per unit, matching the UI kit's art-unit convention.

The generator leaves existing PNGs intact. Edit the existing art or explicitly manage a replacement when changing their design; normal reruns do not overwrite source assets.
