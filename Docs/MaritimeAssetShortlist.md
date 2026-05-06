# Maritime Asset Shortlist

Date reviewed: 2026-05-05

This shortlist prioritizes legal clarity, low runtime cost, and usefulness for a desktop-simulated VR maritime lesson. The current sandbox should keep its procedural placeholder bridge until the interaction model is stable; then replace sections with imported assets one at a time.

## Recommended First Imports

| Priority | Asset | Use | License Notes | Source |
|---|---|---|---|---|
| 1 | Quaternius Ships Pack | Exterior ships, horizon dressing, deck context outside the bridge | CC0, FBX/OBJ/Blend, safe for personal/commercial prototypes | https://quaternius.com/packs/ships.html |
| 2 | Ship engine order telegraph | Primary maritime control prop for lever/throttle training | Free on CGTrader, Royalty Free No AI License. Check account/license terms before redistribution | https://www.cgtrader.com/free-3d-models/industrial/industrial-part/ship-engine-order-telegraph-f711d80b-2d52-4e7d-9865-65c59e7706a2 |
| 3 | POLYGRUNT - Low Poly Boat | Quick Unity-native boat prop for scale/context | Free Unity Asset Store package, Standard Unity Asset Store EULA | https://assetstore.unity.com/packages/3d/vehicles/sea/polygrunt-low-poly-boat-177873 |
| 4 | SF bridge console | Temporary console reference/prop if attribution is acceptable | Sketchfab CC Attribution; keep attribution in project docs/UI credits | https://sketchfab.com/3d-models/sf-bridge-console-3fca908d15254573b0a8bbec7f3a8c05 |

## Import Policy

1. Prefer CC0 or official Unity Asset Store packages for anything that may remain in the lesson.
2. Import one asset category at a time, then run Play Mode and confirm no new errors, material breakage, collider explosions, or frame spikes.
3. Keep art meshes and interaction colliders separate. Imported high-detail mesh colliders should be replaced by simple box/capsule/convex colliders for hands, body, and levers.
4. Avoid unofficial "free download" mirrors of paid assets. They are not acceptable for a training product.
5. If a CC-BY asset is used, record attribution immediately in this file and in the eventual credits surface.

## Next Asset Tasks

1. Download/import Quaternius Ships Pack for exterior silhouette testing.
2. If CGTrader license review is acceptable, import the engine order telegraph and build a real `DesktopLeverInteractable` variant around its handle.
3. Search for a civilian ship bridge interior or bridge console with CC0/Unity Asset Store licensing. If none is suitable, build a modular bridge kit locally with simple optimized meshes.
4. Replace procedural controls only after each interaction has a matching collider, grab pose, and lesson state.
