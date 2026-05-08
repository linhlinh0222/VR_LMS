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
- **Ship Wheel (helm)** - traditional 8-spoke helm (~3,632 verts,
  diameter 700 mm, lock-to-lock 720°). `ShipWheel` MonoBehaviour
  rotates the FBX `wheel_pivot` empty around local Y, exposes
  `RudderNormalized` ([-1, +1]) via `IRudderInput`. Public
  `BeginGrab`/`UpdateGrab`/`EndGrab` API matches `DesktopLeverInteractable`
  so the desktop driver can hook in. Optional spring-back to midships.
  Public events `RudderChanged(float)`, `HardPort`, `HardStarboard`,
  `Midships` are SerializedField-wired so designers can attach lesson
  hooks (alarm sound, scoring, etc.) without code.
- **Marine Radar** - X-band PPI display (~700 verts, body 380×420×150mm,
  PPI 300mm round). `MarineRadar` MonoBehaviour rotates the FBX
  `sweep_pivot` empty around local Y at the configured RPM (default 30,
  matches typical X-band antenna). Range scale follows IMO/IEC 62388
  (0.5 / 1.5 / 3 / 6 / 12 / 24 nm). Public API: `RangeUp`/`RangeDown`/
  `SetRangeIndex`/`EnterStandby`/`EnterTransmit`. Reads `IHeadingProvider`
  (auto-discovered) so heading line stays correct when ship turns.
  UnityEvents: `RangeChanged(float)`, `TransmitStateChanged(bool)`.
- **ECDIS** (Electronic Chart Display) - chart display mandated by
  SOLAS V/19.2.10 (~450 verts, body 620×380×80mm). Z-up Blender — needs
  (-90, 0, 0) on instance. `ElectronicChartDisplay` MonoBehaviour
  manages zoom level (8 steps from 50m to 10km half-height), active
  chart layer enum (Standard / Targets / Route / Alarms), power state.
  Public API: `ZoomIn`/`ZoomOut`/`SetZoomIndex`/`SetLayer`/`PowerOn`/
  `PowerOff`. UnityEvents: `ZoomChanged(float)`, `LayerChanged(layer)`,
  `PowerStateChanged(bool)`. Actual chart rendering (S-57 ENC data,
  ship marker, route waypoints via RenderTexture + top-down camera) is
  deferred to a follow-up phase.
- **AIS Transceiver** (Class A SOLAS V/19) - Raymarine AIS4000 reference
  (~3,700 verts, body 178×55×128mm with mount). Z-up Blender — needs
  (-90, 0, 0). `AisTransceiver` MonoBehaviour manages power + alarm
  states with auto-discovered green status LED + red alarm LED.
  LED visuals via `MaterialPropertyBlock` (no per-LED material instance,
  no leak — same pattern as `MarineTelegraphLessonFeedback`). Alarm
  pulse rate is configurable (default 2 Hz). Public API: `PowerOn`/
  `PowerOff`/`TriggerAlarm`/`ClearAlarm` + 7 D-pad press methods that
  raise events only when powered (`PressDpadUp`/`Down`/`Left`/`Right`/
  `PressOk`/`PressBack`/`PressMenu`). Powering off auto-clears the alarm
  to match real device behavior.
  After 3 display devices, the YAGNI rule still wins: each class is
  ~140 lines and the shared surface (power state + UnityEvent) is small
  enough that extracting `BridgeInstrumentDisplay` would only save ~5
  lines per device. If a 4th display lands and starts duplicating real
  logic (range/zoom step controllers, brightness, screen-on tween),
  refactor at that point.

## Phase 5 wiring (must be done in Unity Editor; MCP automation pending)

The Phase 5 scripts and the `ShipWheel_v1.0.fbx` ship in this branch but
the scene was not auto-wired due to a transient MCP cloud auth outage.
Complete the integration manually in Unity Editor:

1. Drag `Assets/Project/Maritime/Models/Helm/ShipWheel_v1.0.fbx` into the
   scene under `Ship/Bridge_Structure/Maritime Training Station/`.
2. Rename the instance to `ShipWheel`. Set the transform to:
   - position `(0, 10.25, -23.0)` (port-of-compass, on the bridge deck)
   - rotation `(-90, 0, 0)` (Z-up Blender → Y-up Unity)
   - scale `1`.
3. Add component **Maritime LMS / Ship Wheel** to the instance.
   In the inspector set `Wheel Pivot` to the child
   `ShipWheel_root/Wheel/wheel_pivot`. Leave detents and spring-back
   defaults unless the lesson dictates otherwise.
4. Create an empty GameObject `HeadingProvider` under
   `Ship/Bridge_Structure/`. Add component
   **Maritime LMS / Heading From Rudder Provider**. Drag the new
   `ShipWheel` into `Rudder Input Behaviour`.
5. Open the existing `MagneticCompass` GameObject. Replace the
   `Heading Provider Behaviour` reference (currently the camera-based
   `CameraHeadingProvider`) with the `HeadingProvider` GameObject.

After step 5 the ports & adapters loop runs end-to-end:
`ShipWheel → IRudderInput → HeadingFromRudderProvider → IHeadingProvider →
MagneticCompass`.

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
