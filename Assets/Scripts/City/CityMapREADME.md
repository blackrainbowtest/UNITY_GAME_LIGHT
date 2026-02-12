# City Map Setup (StartCityScene)

Recommended approach: UI-based city map (Canvas) with a fixed reference resolution.

## Canvas settings
- Canvas: `Screen Space - Overlay`
- Canvas Scaler: `Scale With Screen Size`
  - Reference Resolution: `1920 x 1080`
  - Screen Match Mode: `Match Width Or Height`
  - Match: `0.5` (tweak to taste)

## Background
- Use an `Image` with the 1920x1080 sprite
- Enable `Preserve Aspect`
- Keep it centered (anchors center). Avoid `Stretch` on the background.

## Buildings
- Under `BuildingsRoot` create one object per building:
  - `Image` (sprite of building)
  - `Button`
  - `CityMapBuildingHotspot`
  - Optional child: `HighlightFrame` (Image) and assign it to `highlight`

## Inspect Mode (Eye)
- Add `CityInspectModeController` and wire:
  - `toggleButton`
  - `toggleIcon` + `iconOff/iconOn`
  - `buildingsRoot`

This will show highlight frames for all hotspots when inspect mode is ON.

## Reusable setup for Shelter/Market/Any location

If you need interaction that opens UI prefabs (not scene loading), use:
- `LocationPrefabHotspot` on each clickable object/button
- `LocationInspectModeController` on the scene root that has the Eye button

Recommended wiring:
- Keep your `BuildingsRoot` (or any root) with clickable children.
- On each child:
  - `Button`
  - `LocationPrefabHotspot`
  - assign `contentPrefab` (window/panel prefab to open)
  - assign optional highlight frame (`highlightObject` or `highlightGraphic`)
- On controller:
  - assign `toggleButton`, `toggleIcon`, `iconOff`, `iconOn`
  - assign `hotspotsRoot` (e.g. `BuildingsRoot`)

Notes:
- You can make hotspots clickable only in Eye mode via `interactableOnlyInInspectMode`.
- This setup is scene-agnostic: duplicate the scene, change background + assigned prefabs in Inspector.
