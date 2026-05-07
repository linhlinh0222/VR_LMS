# Maritime Equipment Module

Source models live under `Assets/Project/Maritime/Models/<Equipment>/` and were
imported from <https://github.com/meiiie/model_lms> (MIT, see project root LICENSE
acknowledgements). Generated content (prefabs, materials) sits next to the source
FBX so a designer can find everything for one device in one place.

## Architecture

We follow a **ports & adapters** layout so navigation instruments do not depend
on a specific input rig. Today the desktop simulator drives heading from the
player camera; tomorrow the same interface can wrap an OVR camera, an
integrated helm steering input, or a recorded gyro feed without touching the
equipment scripts.

```
Domain          Ports                   Adapters             Concrete equipment
(values)        (interfaces)            (Scene components)   (MonoBehaviour)

float           IHeadingProvider  <---  CameraHeadingProvider
                                                              MagneticCompass
                                                              (depends on IHeadingProvider)
```

## Conventions

- Scripts live under `Assets/Project/Scripts/Maritime/<Domain>/`. They share the
  existing `ComprehensiveDemoRuntime` assembly so calling code in `MaritimeLMS`
  (the LMS lesson namespace) can reference them without an extra asmdef.
- Namespaces mirror folders: `MaritimeLMS.Bridge`, `MaritimeLMS.Compass`, etc.
- New equipment classes are `sealed` MonoBehaviours with `[AddComponentMenu]`
  for designer discoverability. Apply `[DisallowMultipleComponent]` when only
  one instance per GameObject makes sense.
- Public properties use degrees (`HeadingDegrees`) and meters (`DepthMeters`)
  explicitly. Do not return raw `Vector3` for navigation values.
- An equipment script is responsible for **its own card/dial animation only**.
  Lesson logic (objective tracking, scoring) lives in the LMS layer and
  subscribes via events or properties.

## Adding a new equipment

1. Drop the FBX into `Assets/Project/Maritime/Models/<Equipment>/`.
2. Apply Unity's standard FBX import: Generic rig, Use External Materials
   (Legacy), Optimize Game Objects.
3. If the source mesh was authored Z-up (Blender), rotate the instance by
   `(-90, 0, 0)` once placed. Compass model already needs this.
4. Place the prefab in the scene under
   `Ship/Bridge_Structure/Maritime Training Station/`.
5. Author a sealed MonoBehaviour under `Scripts/Maritime/<Equipment>/` that
   depends only on the ports it needs (e.g. `IHeadingProvider`,
   `IRudderInput`, `IEnginePower`). Wire the references in the Inspector.
6. Cross-reference the model's `README.md` for canonical part names
   (e.g. `compass_card_pivot` for the compass).

## Implemented

- `MagneticCompass` - liquid-damped binnacle. Reads `IHeadingProvider`,
  drives `compass_card_pivot` on local Z. Reference:
  `_staging/model_lms/03_Magnetic_Compass/README.md`.
- `EngineOrderTelegraph` - 7-detent EOT (-90 to +90 in 30° steps) with
  ordered/answered pointers and configurable engine acknowledgement delay.
  Pivots auto-discovered by FBX name (`handle_lever_pivot`,
  `pointer_ordered_pivot`, `pointer_answered_pivot`). Exposes
  `NormalizedValue` so existing `MaritimeTelegraphLessonController` works
  unchanged when a designer rewires it from the legacy
  `DesktopLeverInteractable`.
- **Ship Hull (exterior)** - Handysize bulk carrier (LOA 120 m, beam 20 m,
  draft 7 m, Cb 0.82, ~3,000 verts). No script: a static prop instanced
  under `Ship/ShipHullExterior`. The bundled `ocean_plane` child is
  disabled because the project ships its own marine water shader.
  Identity rotation - the v3.3 FBX exports already in Y-up, unlike the
  Blender Z-up models for compass / EOT.
  Aligned so its `anchor_bridge_cabin` empty sits exactly on
  `Ship/Bridge_Structure` (world `(0, 10.23, -28)`) - placeholder station
  geometry remains until Phase 4 polish hides it.

## Roadmap

- Hook `EngineOrderTelegraph` to a desktop/XR grab driver so the lever can be
  user-controlled (today it's programmatic only via `SetOrder`).
- Replace the legacy `MarineTelegraph` placeholder with the new EOT once
  interaction is wired; rewire `MaritimeTelegraphLessonController.telegraph`.
- Helm wheel - `IRudderInput` port, integrate steering rate to feed
  `HeadingFromRudderProvider` (replaces `CameraHeadingProvider` in Phase 2).
- Radar, ECDIS, AIS - share a `BridgeInstrumentDisplay` panel base when 3+
  display devices land (extract base then per YAGNI, not before).

## Test strategy

Until headless tests are set up, verify in Play Mode:

1. Open `Assets/Project/Scenes/MaritimeBridgeLMS.unity`.
2. Press Play. Right-click drag to look around.
3. Compass card should rotate smoothly opposite to the camera yaw and settle
   within ~1 second per `swingDamping`.
4. `MagneticCompass.HeadingDegrees` and `MagneticCompass.CardinalDirection`
   should match the camera Y rotation.
