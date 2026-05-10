# Bridge Scale Fix Dry Run — 2026-05-10 11:26

## 1. Equipment scale
- AIS        no scale change
- VHF        no scale change
- ECDIS      no scale change
- Radar      no scale change
- EOT        (0.650, 0.650, 0.650) → (0.550, 0.550, 0.550)
- Compass    already at 0.55
- ShipWheel  already at 0.55

## 2. Helm cluster lift (Y +0.05)
- EOT        localPos (-0.800, -0.550, 0.070) → (-0.800, 0.050, 0.070)
- Compass    localPos (0.800, -1.200, 1.070) → (0.800, 0.050, 1.070)
- ShipWheel  localPos (-0.500, -0.100, 0.070) → (-0.500, 0.050, 0.070)

## 3. Lighting
- Directional lights enabled: 1

## 4. Debug markers
- Will spawn a coloured sphere (0.10 m) above each anchor + the EOT lever pivot if missing

## 5. Summary
- Scale ops: **1** | Lift ops: **3** | Light: **ok**

Dry-run only. To apply: `Tools/Maritime LMS/Fix Bridge Scale — Execute`
