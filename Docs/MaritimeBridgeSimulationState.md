# Maritime Bridge VR Simulation State

Last updated: 2026-05-06

## Current Goal

Build a mouse-and-keyboard VR development simulator for a maritime training lesson. The immediate lesson is engine telegraph familiarization: the learner should stand at a bridge/control station, see both virtual hands, manipulate the telegraph, observe vessel response, then return the telegraph to Stop.

## Current Architecture Decisions

- Keep desktop simulation separate from real headset support. Real Quest/VR hand logic can still use Meta/OpenXR later; desktop development uses `DesktopMockVRController`.
- Desktop mode now uses **training ghost hands**: the visible hands are stable, camera-relative feedback meshes; the actual interaction is driven by a ray/cursor interactor. This intentionally favors a reliable training interface over fake full hand physics while we are using mouse and keyboard.
- Use a physics layer contract for interaction: `desktop.body`, `desktop.hand`, `desktop.interactable`, `desktop.world`.
- Use a separate render-layer contract for hands: hand colliders/logic can stay on the desktop layers, but hand mesh renderers are forced to `Default` because the current Game View/URP setup was not reliably drawing custom-layer hand meshes.
- Use object-specific interactables for mechanisms. The telegraph is a constrained lever, not a free rigidbody grab.
- Keep HUD as Screen Space Overlay for now, but treat it as a compact training instrument rather than a large tutorial card.

## Interaction Strategy Notes

- Unity XR Interaction Toolkit treats `Hover`, `Select`, and `Activate` as interaction states between an Interactor and an Interactable. That maps cleanly to our custom controller: mouse/ray hover is intent, left-click select is grip/hold, release exits select.
- Unity's XR Device Simulator is a runtime utility for keyboard/mouse-driven simulated XR input, and its docs note that it drives XR devices indirectly through simulated input rather than directly manipulating the camera/controller objects. This supports keeping our desktop input layer separate from final headset rig logic.
- Unity's XR Ray Interactor is explicitly for distance interaction via raycasts, with closest-hit target selection and optional UI behavior. This is the right mental model for the desktop build: precision comes from raycast selection, not from forcing a mesh hand to physically reach every target.
- Meta's Interaction SDK supports grab, UI, poke, raycasting, custom hand models, pose detection, and real hand/controller inputs. The future Quest path should migrate telegraph interaction to Meta ISDK/OpenXR hands while keeping this desktop ghost-hand mode as an editor/development simulator.
- For this lesson, "magic hands" are acceptable because the pedagogical target is telegraph familiarization and vessel response, not hand anatomy assessment. Visual hands should communicate presence and active state, while the interaction contract must remain deterministic.

## Screenshot Assessment

- The view was too much like an exterior bow/deck camera and not enough like a bridge operation station.
- The two hand GameObjects existed, but their custom render layer made them disappear in Game View. The old idle depth also placed them behind the console face.
- The right hand drifted when selected because active-hand attach-point math ignored the hand's idle rotation.
- The HUD panel was too narrow at the current Game View width; objective text, title, compass, and speed readout overlapped.
- The imported bridge console mesh is currently a risk: it can visually dominate the lower frame and appears to write/occlude depth in ways that hide training props behind it. Prefer a controlled training console mesh for the core lesson until the imported bridge assets are cleaned.

## Fixes Applied

- `DesktopMockVRHandVisual` now forces hand mesh renderers to a visible render layer and optional material override.
- Hand visuals use `Assets/Project/Materials/DesktopMockVR/DesktopSDKGhostHand.mat`, the softer classic SDK ghost-hand look. This keeps the earlier translucent hand aesthetic while preserving the renderer-layer fix that prevents disappearing hands.
- `DesktopMockVRController` now computes active-hand idle attach targets using the hand's idle rotation. This fixes the "selected hand 2 moved away" behavior.
- `DesktopMockVRController` now has `trainingGhostHandsMode`, which keeps both ghost hands in stable camera-local rest poses and uses ray/cursor logic for actual hover/select/hold. This removes the previous unnatural hand drift and object-penetration feel in mouse/keyboard mode.
- Training ghost-hand grip is intentionally subtle:
  - Hover grip: `0.025`
  - Body grab grip: `0.08`
  - Lever grab grip: `0.14`
- Both hands are now framed in front of the player without cropping at the Game View edges:
  - Left idle local position: `(-0.28, -0.18, 0.64)`
  - Right idle local position: `(0.28, -0.18, 0.64)`
  - Both idle rotations: `(0, 0, 90)`
- The HUD layout is currently compact top-left, with wrapped/auto-sized TextMeshPro fields and separated compass/speed zones.
- Runtime verification showed hands visible around viewport left `0.26`, right `0.76`, with no compile/runtime errors from these changes.
- A controlled `Maritime Training Station` was added under `Ship/Bridge_Structure`. It replaces the visually noisy imported foreground console with stable console geometry, panels, lamps, bridge glass, and world colliders.
- The downloaded FBX telegraph asset at `Assets/Project/Models/Maritime/Telegraph.fbx` is now used as the telegraph reference visual. A smaller generated lever shaft/knob remains the actual moving interactable so the physics/control logic stays predictable.
- Non-lesson props such as the old helm and cube/plinth training objects are hidden for the engine telegraph lesson to reduce clutter and visual ambiguity.
- `ShipController` now separates engine-speed simulation from transform motion. In the bridge lesson, `moveTransform` and `rotateTransform` are disabled so the root that contains the player rig does not drift while the ship speed/LMS logic still updates.
- Transparent bridge glass material is configured for the station window so the sea remains visible without a solid cyan panel blocking the view.
- `Bridge Interior Shell` exists as world-space bridge-room geometry under `Ship/Bridge_Structure`; this is preferred over camera-attached frame overlays because it can later carry proper colliders/materials. It still needs an art/layout pass because the current Game View remains too deck-forward.

## Remaining Technical Risks

- The imported console/bridge assets need a cleanup pass: separate render mesh from colliders, remove over-large/hidden depth surfaces, and assign stable materials.
- Core lesson props must stay visually prominent in Game View, not only present in the hierarchy. Render meshes should remain on `Default`; colliders should remain on `desktop.interactable` or `desktop.world`.
- The first-person station is now usable, but the bridge still needs an art pass: better window framing, less bow-deck dominance, cleaner instrument proportions, and stronger maritime material language.
- A future real-headset path should switch from the desktop mock hand driver to Meta ISDK or XR Interaction Toolkit without mixing both stacks in the same scene.

## Next Pass Checklist

1. Improve the visual art pass for the station: slimmer HUD, stronger bridge-window frame, better horizon/deck composition, and less toy-like lamp spacing.
2. Add a focused mouse/ray QA pass on the telegraph: verify hover, click-hold, detents, Ahead/Stop objective transitions, and cursor bounds.
3. Build a dedicated telegraph mesh split into fixed base and moving handle, replacing the temporary FBX-plus-generated-lever hybrid.
4. Add a compact in-scene body/reach debug toggle for developers so arm limits and body capsule can be inspected without cluttering normal play.
5. Keep the real-headset path separate: desktop simulation stays mouse/keyboard, headset mode should use Meta/OpenXR hand tracking/controller input.

## Verification Notes

- Play Mode after the station pass showed both hands visible in front of the player and the telegraph visible on the right console.
- Play Mode after the magic ghost-hand pass showed both hands stable in front of the player at local `(-0.28, -0.18, 0.64)` and `(0.28, -0.18, 0.64)`. `trainingGhostHandsMode=True`.
- Runtime LMS test:
  - Setting telegraph to Ahead completed objective 0.
  - Ship speed rose to `3.37`, completing objective 1.
  - Returning telegraph to Stop completed objective 2.
  - `Ship` stayed at `(0, 0, 0)` and the camera stayed near local `(0, 1.59, 2.35)` during the test.
  - Error log after verification: none.

## References Checked

- Unity XR Interaction Toolkit Architecture: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.0/manual/architecture.html
- Unity XR Device Simulator Overview: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.0/manual/xr-device-simulator-overview.html
- Unity XR Ray Interactor: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.0/manual/xr-ray-interactor.html
- Unity XR Hands: https://docs.unity3d.com/Packages/com.unity.xr.hands@latest
- Meta Interaction SDK Overview: https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview
