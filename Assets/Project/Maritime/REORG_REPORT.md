# Bridge Reorganize Dry Run — 2026-05-09 10:37
Scene: `Assets/Project/Scenes/MaritimeBridgeLMS.unity`

## 1. Anchors detected
- AIS        `BridgeCabin/anchor_AIS4000`
- VHF        `BridgeCabin/anchor_VHF`
- ECDIS      `BridgeCabin/anchor_ECDIS`
- Radar      `BridgeCabin/anchor_Radar`
- EOT        `BridgeCabin/anchor_EOT`
- Compass    `BridgeCabin/anchor_Compass`
- ShipWheel  `BridgeCabin/anchor_ShipWheel`

## 2. Equipment re-parent plan
- AIS        `Ship/Bridge_Structure/Maritime Training Station/AIS4000` → `BridgeCabin/anchor_AIS4000`
- VHF        `Ship/Bridge_Structure/Maritime Training Station/VhfRadio` → `BridgeCabin/anchor_VHF`
- ECDIS      `Ship/Bridge_Structure/Maritime Training Station/ECDIS` → `BridgeCabin/anchor_ECDIS`
- Radar      `Ship/Bridge_Structure/Maritime Training Station/MarineRadar` → `BridgeCabin/anchor_Radar`
- EOT        `Ship/Bridge_Structure/Maritime Training Station/EngineOrderTelegraph` → `BridgeCabin/anchor_EOT`
- Compass    `Ship/Bridge_Structure/Maritime Training Station/MagneticCompass` → `BridgeCabin/anchor_Compass`
- ShipWheel  `Ship/Bridge_Structure/Maritime Training Station/ShipWheel` → `BridgeCabin/anchor_ShipWheel`

## 3. Placeholders flagged for removal
- `Ship/Bridge_Structure/Maritime Training Station` (10 immediate children)
- `Ship/Bridge_Structure/Maritime Training Station/Generated Station Geometry` (34 immediate children)
- `OceanAmbient` (0 immediate children)
- `Open Sea Surface` (0 immediate children)
- `Ship/Bridge_Structure/Maritime Training Station/MarineTelegraph/Telegraph Desktop Pivot/Generated Telegraph Lever Shaft` (procedural placeholder)
- `Ship/Bridge_Structure/Maritime Training Station/MarineTelegraph/Telegraph Desktop Pivot/Generated Telegraph Lever Knob` (procedural placeholder)

## 4. Summary
- Re-parent operations: **7**
- Placeholders to remove: **6**

Dry-run only — nothing changed. To apply, run:
`Tools/Maritime LMS/Reorganize Bridge — Execute (DESTRUCTIVE)`

