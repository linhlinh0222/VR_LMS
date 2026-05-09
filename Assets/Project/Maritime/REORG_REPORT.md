# Bridge Reorganize Dry Run — 2026-05-10 00:17
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
- AIS        already under anchor — no change
- VHF        already under anchor — no change
- ECDIS      already under anchor — no change
- Radar      already under anchor — no change
- EOT        already under anchor — no change
- Compass    already under anchor — no change
- ShipWheel  already under anchor — no change

## 3. Placeholders flagged for removal
(none — already cleaned)

## 4. Summary
- Re-parent operations: **0**
- Placeholders to remove: **0**

Dry-run only — nothing changed. To apply, run:
`Tools/Maritime LMS/Reorganize Bridge — Execute (DESTRUCTIVE)`

