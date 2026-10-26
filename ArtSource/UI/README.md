# Settings icon assets

The project-owned round wood settings button and focus ring use 24x24 pixel sprites; the gear icon uses 16x16 pixels. They extend PixelUIFree's existing UI style without modifying the imported kit. Their PNGs are under `Assets/Art/UI/`, imported as point-filtered, uncompressed sprites with alpha and no mipmaps at 100 pixels per unit, matching the UI kit's art-unit convention. The local generator under `Tools/Unity/` is excluded from version control; the game uses the committed PNGs directly.

The generator leaves existing PNGs intact. Edit the existing art or explicitly manage a replacement when changing their design; normal reruns do not overwrite source assets.
