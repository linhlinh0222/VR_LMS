# Bridge Scale Fix Dry Run — 2026-05-10 01:51

## 1. Equipment scale
- AIS        no scale change
- VHF        no scale change
- ECDIS      no scale change
- Radar      no scale change
- EOT        already at 0.55
- Compass    already at 0.55
- ShipWheel  already at 0.55

## 2. Helm cluster lift (Y +0.05)
- EOT        localPos (-0.800, -0.700, 0.070) → (-0.800, 0.050, 0.070)
- Compass    localPos (0.800, -1.450, 1.120) → (0.800, 0.050, 1.120)
- ShipWheel  localPos (-0.650, -0.100, 0.070) → (-0.650, 0.050, 0.070)

## 3. Lighting
- Directional lights enabled: 1

## 4. Debug markers
- Will spawn a coloured sphere (0.10 m) above each anchor + the EOT lever pivot if missing

## 5. Summary
- Scale ops: **0** | Lift ops: **3** | Light: **ok**

Dry-run only. To apply: `Tools/Maritime LMS/Fix Bridge Scale — Execute`
