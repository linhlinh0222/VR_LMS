# Desktop VR Simulation Knowledge

Date reviewed: 2026-05-06

This project is a Unity FirstHand/Meta XR prototype that must be testable from keyboard and mouse before a physical headset is required. The immediate goal is a desktop maritime training simulator where left/right ghost hands communicate controller/hand state, while mouse ray interaction provides deterministic selection for lesson controls such as the engine telegraph.

## Current Project Stack

- Unity 6.4.0, URP 17.4.
- Meta XR SDK packages and Meta XR Simulator are installed.
- Unity XR Interaction Toolkit is not installed in this repository right now, so the desktop sandbox should stay small and local instead of adding a second XR interaction stack prematurely.
- Unity-MCP is installed and generated local skills in `.agents/skills`; use these for scene reads, Play Mode, screenshots, script execution, and log checks.

## External References

- Unity XR Interaction Simulator: simulates headset, controllers, or hands from keyboard/mouse/controller input and drives the XR system through simulated input rather than directly moving the camera or controllers.
  Source: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/xr-interaction-simulator-overview.html

- Unity XR Device Simulator: older sample utility for keyboard/mouse simulation; Unity notes that XR Interaction Simulator is newer.
  Source: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.1/manual/xr-device-simulator-overview.html

- Unity XR Grab Interactable: grab behavior is an interaction between an Interactor and Interactable; grabbed objects follow the Interactor and can inherit velocity on release. Attach Transform defines the grab attachment point.
  Source: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/xr-grab-interactable.html
  Source: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.0/manual/xr-grab-interactable.html

- Unity CharacterController: a capsule-shaped controller moves only when `Move` is called, and that movement is constrained by collisions. Unity documents height, radius, center, skin width, step offset, and slope limit as the main tuning controls for a human-like controller.
  Source: https://docs.unity3d.com/ScriptReference/CharacterController.html
  Source: https://docs.unity.cn/560/Documentation/Manual/class-CharacterController.html
  Source: https://docs.unity3d.com/ScriptReference/CharacterController.Move.html

- Unity XR Interaction Toolkit locomotion: continuous/grab locomotion uses `CharacterController.Move` when a CharacterController is present on the Origin, instead of directly translating the Transform. This is the important architectural rule for not making an XR body ghost through the world.
  Source: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@2.5/manual/locomotion.html

- Unity XR Hands: hand tracking data is a subsystem API. Unity separates game logic updates from before-render visual updates for lower latency hand visuals.
  Source: https://docs.unity3d.com/Packages/com.unity.xr.hands@1.5/manual/hand-data/xr-hand-access-data.html

- Panoramic skyboxes are no longer the active environment direction for this sandbox. A 360 image is useful for fast look development, but it becomes visually confusing in a hands-and-controls lesson because it has no depth, no parallax, and no physical contact surface. The current scene uses real 3D bridge/deck assets plus a simple camera background instead.

- Unity HLSL shader programs: custom ShaderLab passes can contain vertex and fragment HLSL code. For this sandbox, a direct URP HLSL shader is simpler and cheaper than importing a full water package.
  Source: https://docs.unity3d.com/Manual/writing-shader-writing-shader-programs-hlsl.html

- Unity URP shader resources: URP supports custom ShaderLab/HLSL shaders when the project needs a specific performance/visual tradeoff.
  Source: https://docs.unity3d.com/Manual/urp/shaders-in-universalrp.html

- Unity water tutorial context: the referenced Unity/Ben Cloward water session focuses on realistic water building blocks such as reflection, refraction, depth fog, edge transparency, surface ripples, and wetness decals. The sandbox implements the lightweight subset appropriate for a first VR bridge prototype: vertex waves, Fresnel, specular highlights, and crest foam.
  Source: https://www.youtube.com/watch?v=-rxqKHb_4gc
  Source: https://www.gamesinprogress.com/indie-game-developers/unity/creating-stunning-water-in-unitys-shader-graph-ft-bencloward

- Meta XR Simulator: a lightweight OpenXR runtime for simulating Quest headset/controller/hand input, including keyboard/mouse mapping, hand poses, pinch, poke, grab gestures, and per-side input selection.
  Source: https://developers.meta.com/horizon/documentation/unity/xrsim-intro/

- Meta Interaction SDK Hand Grab: `HandGrabInteractable` defines whether/how an object can be grabbed, how it aligns, which fingers start/end the grab, and how movement follows the interactor.
  Source: https://developers.meta.com/horizon/documentation/unity/unity-isdk-hand-grab-interaction/

- Meta Interaction SDK hand alignment: `HandGrabPose` provides object-relative wrist anchoring and visual hand pose. `AlignOnGrab` snaps the visual hand to the interactable at grab start, while hover behaviors can progressively attract the hand or fingers before selection.
  Source: https://developers.meta.com/horizon/documentation/unity/unity-isdk-hand-grab-interaction/

- Maritime simulator realism: DNV describes STCW/DNV-ST-0033 simulator certification around appropriate physical and behavioural realism for recognised training and assessment objectives.
  Source: https://www.dnv.com/services/certification-of-maritime-simulator-systems/

- Maritime VR/synthetic environment context: DNV's 2025 revision of DNV-ST-0033 explicitly introduces terminology for VR, synthetic environments, and mixed reality, and expands training context for alternative fuels such as ammonia, hydrogen, ethanol, and electric/hybrid propulsion.
  Source: https://www.dnv.com/news/2025/dnv-revises-maritime-simulator-standard-to-support-safer-and-more-adaptable-training/

- Unity-MCP: local MCP tools and generated skills are intended for direct Unity Editor operation, code/test loops, and scene inspection.
  Source: https://github.com/IvanMurzak/Unity-MCP

- Open-source VR framework reference: UltimateXR is MIT-licensed and includes grab/manipulation, hands, body avatar, locomotion, UI, and cross-device VR/AR support. It is a useful architecture reference, but importing it into this FirstHand sandbox would be a larger stack decision.
  Source: https://www.ultimatexr.io/

- Unity official MR/XRI sample reference: Unity's Meta OpenXR sample combines OpenXR, XR Interaction Toolkit, XR Hands, AR Foundation, and Meta OpenXR. It demonstrates physics interactables, passive hand physics, object affordances, and URP Quest presets.
  Source: https://github.com/Unity-Technologies/mr-example-meta-openxr

## Asset Shortlist

- Quaternius Ships Pack: CC0, FBX/OBJ/Blend, simple low-poly ships. Best for clean-license exterior vessels or horizon dressing, not for the bridge interior itself.
  Source: https://quaternius.com/packs/ships.html

- POLYGRUNT - Low Poly Boat: free Unity Asset Store package under the Standard Unity Asset Store EULA, very small file size, useful as a quick Unity-native boat prop.
  Source: https://assetstore.unity.com/packages/3d/vehicles/sea/polygrunt-low-poly-boat-177873

- Ship engine order telegraph: free CGTrader model, FBX/OBJ/STL/PNG, PBR textures, about 7.7k polygons. This is the best candidate found for a maritime control prop, but the CGTrader royalty-free/no-AI license should be checked before importing into a distributable lesson.
  Source: https://www.cgtrader.com/free-3d-models/industrial/industrial-part/ship-engine-order-telegraph-f711d80b-2d52-4e7d-9865-65c59e7706a2

- SF bridge console on Sketchfab: downloadable, 13.4k triangles, CC Attribution. Usable only if we keep attribution. It reads more sci-fi/naval than civilian maritime bridge, so it is a possible reference or temporary prop, not the preferred final training asset.
  Source: https://sketchfab.com/3d-models/sf-bridge-console-3fca908d15254573b0a8bbec7f3a8c05

- Paid bridge interiors exist, for example ship bridge/control-room models on marketplaces, but they are often expensive, large, and license-bound. Do not import scraped copies or third-party "asset free download" mirrors. If a paid bridge interior is chosen, acquire it through the official marketplace account first.

## Design Notes For This Sandbox

1. Keep visuals subordinate to state. The ghost hand mesh should follow a left/right hand state; it should not be the interaction logic.
2. Keep desktop input separate from object interaction. Keyboard/mouse should produce head pose, hand pose, active hand, select/grip, and hold distance. The grab code should consume those states.
3. Model grab as Interactor to Interactable. Even before importing XRI, the local script should mimic the same shape: ray/direct hit, active hand attach point, select begin, selected follow, select end, release velocity.
4. Keep left and right hands symmetric. Their idle positions should be mirrored around the camera center, and runtime movement should move only the active hand unless a two-hand mode is explicitly added.
5. Use attach points. The cube should follow the selected hand attach point, not an arbitrary mesh center and not a UI-only indicator.
6. Keep the hand anchor outside the touched collider. Ray hits produce a surface point and normal; the desktop hand should target `hit.point + hit.normal * surfaceOffset`, otherwise the palm can visually sit inside the cube even while the object follows correctly.
7. Model constrained mechanisms with their own interactable, not as free rigidbody grabs. The training lever computes hand angle in stable parent space, clamps target angle between min/max, and only rotates the pivot around its hinge axis.
8. For professional desktop training controls, separate driver intent from visible contact. In the desktop simulator, the mouse ray drives the lever angle; the visible hands can remain stable ghost UI hands with a light hover/grip cue. This is more honest and usable than pretending mouse input is full hand tracking.
9. Maritime controls should be placed at human scale. Keep the camera near standing eye height, place controls on a console/table surface instead of the floor, and keep training props small enough that they do not dominate the operator's field of view.
10. Keep Play Mode verification cheap. A valid smoke test is: open `DesktopGrabSandbox`, ray-grab the cube, confirm the active hand follows the attach point, confirm the cube follows the hand, pull the lever through its min/max range, release, and confirm no new exceptions.

## Current Body And Contact Model

- Current default for the maritime lesson is `trainingGhostHandsMode = true`. In this mode the body capsule still blocks locomotion, but free hands do not chase raycast targets or collide like physical hands. The ray/cursor is the actual interactor; the hands are stable presence/affordance visuals.
- The desktop sandbox now uses a proxy body instead of treating hands as detached UI meshes. The proxy body is derived from the camera pose but uses yaw only, so looking up/down does not drag the shoulders through the world. Local shoulder, chest, and abdomen points define the operator frame.
- Each hand target is clamped to a maximum shoulder-to-hand reach. This keeps keyboard/mouse input from placing a hand at impossible distances relative to the torso.
- The torso is represented as a capsule-like chest-to-abdomen volume. Hand targets inside that volume are pushed outward, which prevents the first-person hand from passing through the operator body.
- Locomotion now has its own invisible player capsule. The camera object owns a `CharacterController` with a standing eye-height setup: height 1.72 m, radius 0.28 m, center -0.60 m from the eye pivot, skin width 0.035 m, step offset 0.18 m, and slope limit 55 degrees.
- Desktop locomotion must call `CharacterController.Move`, not directly write `transform.position`. This preserves the useful first-person camera pivot while preventing the operator from walking through bridge walls, glass, consoles, and training props.
- Vertical debug flying is disabled by default (`allowVerticalBodyMove = false`). For maritime lesson validation the operator should stand/walk inside the bridge, not move like a free camera unless a debugging pass explicitly enables it.
- The marine bridge primitives now carry colliders, including the transparent glass and ceiling. Visual-only objects such as the open sea plane should remain non-colliding unless they represent a reachable physical surface.
- The older physical-hand mode still uses `Physics.SphereCastNonAlloc` from the shoulder toward the requested hand target. This approximates the "direct interactor has a collision volume" idea from Unity XRI without importing XRI into this small sandbox yet, but it is not the default for the current desktop lesson.
- Free active hands now keep a side-specific workspace. The desktop preview target blends only slightly from the anatomical idle attach position toward the mouse ray, then clamps to the correct left/right side and a reasonable local height/depth range. This prevents the right hand from being dragged into the left wall or down into the torso when the center mouse ray is not hovering a real interactable.
- Grabbed rigidbodies use `Rigidbody.SweepTest` before `MovePosition`. This does not turn the prototype into a full haptic physics hand, but it stops the obvious training-breaker case where a held cube tunnels through another collider during desktop dragging.
- Grip state is now discrete: idle hands use a relaxed partial-open value, hover applies a light pre-grip, and grab uses a strong but not fully clenched curl. This matches the Meta ISDK distinction between hover attraction, align-on-grab, and selected hand pose while avoiding a constant "always fist" visual.
- The open/relaxed hand pose is sampled from controlled animation endpoints instead of the prefab's current scene pose. Left currently uses `Tutorial_1_HandOpenPalmUp` for open and `RepairBayGrab_L` for closed. Right uses `RepairBayGrab_R` for both open-at-time-0 and closed-at-time-2.815, avoiding the earlier mismatch where `OculusHand_R` could leave the right hand looking clenched before interaction.
- The next realism step should be per-object feedback, not necessarily per-object exact hand physics: lever hover, lever selected, objective completed, and invalid-input states should each have clear visual/audio affordances. If/when headset mode is added, then use per-object grab poses: cube grip, cylindrical lever grip, knob grip, and switch pinch.

## Current Implementation Direction

- `DesktopMockVRController` remains the minimal desktop driver for now.
- `trainingGhostHandsMode` is the current default for the maritime bridge lesson. It keeps hands stable at camera-local rest poses and uses only subtle grip values for hover/hold: hover `0.025`, body grab `0.08`, lever grab `0.14`.
- Current hand rest poses are:
  - Left: `(-0.28, -0.18, 0.64)`, rotation `(0, 0, 90)`
  - Right: `(0.28, -0.18, 0.64)`, rotation `(0, 0, 90)`
- Mouse/right-button still controls view; WASD moves the invisible body capsule. Q/E debug flying is intentionally off unless `allowVerticalBodyMove` is enabled.
- Number keys choose the active hand: `1` for left, `2` for right.
- Left mouse button starts and holds selection with the active hand.
- Mouse wheel changes hold distance.
- The held rigidbody follows the active hand attach point; on release it restores gravity/kinematic state and receives scaled release velocity.
- In training ghost-hand mode, the active hand no longer moves to a surface offset outside the hovered collider. This avoids the more distracting desktop illusion where a fake hand tries to physically reach a control using only mouse input.
- In the older physical-hand mode, the active hand is constrained by a body proxy: yaw-only shoulders, arm reach limit, torso push-out, and shoulder-to-target sphere casts against interactable/collision layers.
- Held rigidbodies are swept before movement so cube dragging respects blocking colliders instead of teleporting straight through them.
- `Training Lever` is a constrained mechanism with a base, supports, axle, handle, knob colliders, left/right grip poses, and `DesktopLeverInteractable`. It is intentionally primitive for now so the interaction math can be validated before spending time on art assets.
- The sandbox now includes a primitive-built `Marine Bridge Room`: bridge deck, bulkheads, forward/side windows, exterior rail, instrument console, screens, lamps, pushbuttons, rudder wheel placeholder, training lever, and a scaled cube training prop. These are project-local Unity objects and materials, so the prototype has no external asset-license dependency yet.
- The previous generated 360 panorama has been removed from the active scene and project assets. It was too flat for interaction validation. `MarineExteriorAssetBuilder` now generates real exterior 3D objects near the water: bow deck, seams, rails, hatch, handle, and bollards. These are visual-only, `DontSave` editor objects with shared generated meshes and intentionally have no colliders, so they add maritime context without interfering with hand/body/cube/lever physics.
- `Open Sea Surface` is now a real subdivided water mesh with `MarineWaterSurface.mat` and `FirstHand/DesktopMockVR/MarineWater`. The shader displaces vertices using layered sine waves, computes dynamic normals, adds Fresnel, specular highlights, and subtle crest foam. It remains visual-only and has no collider, so it cannot interfere with hand, body, lever, or cube physics.
- `MarineWaterSurfaceController` is the URP equivalent of the HDRP Water inspector controls from the Pirani Dev/Unity guide. It exposes time multiplier, simulation bands, distant/local wind speed and orientation, chaos, amplitude, current, foam intensity/cutoff/dimmer, shallow/deep colors, Fresnel, specular, and sun direction. Keep the default calm-sea setup for bridge-operation lessons; use the context-menu presets only when the lesson explicitly needs moderate or storm sea conditions.
- Do not convert this project to HDRP just to use the HDRP Water System. FirstHand is currently configured around URP and Meta XR packages, so a pipeline conversion would be a larger rendering migration with material/shader validation cost. The current water is a deliberate URP-compatible approximation of the HDRP tutorial concepts.
- `DesktopMockVRHandVisual` drives the FirstHand ghost-hand skeleton as a state visual. It samples only finger-bone rotations from existing FirstHand grab clips, keeps wrist/root transforms controlled by `DesktopMockVRController`, and blends open/closed pose from the current grip state.
- Hand attach points now sit near the measured palm/pinch markers from the hand prefabs instead of using arbitrary offsets. This keeps the object's follow point tied to hand anatomy while preserving the simple desktop input path.

## Later Upgrade Path

When the lecture prototype needs real XR interaction parity, add one official interaction stack deliberately:

- Meta path: build the real scene around Meta ISDK `OVRInteraction`/Comprehensive Interaction Rig, `HandGrabInteractor`, `HandGrabInteractable`, and recorded hand grab poses for lesson objects.
- Unity path: add XR Interaction Toolkit and use XR Interaction Simulator, XR Origin, Action Based Controllers, XR Direct/Ray Interactors, and XR Grab Interactable.

Do not mix both stacks in the same small sandbox unless there is a specific test that requires it.
