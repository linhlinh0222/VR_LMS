# Bridge Scale Fix EXECUTE — 2026-05-10 01:37

## 1. Equipment scale
- AIS        no scale change
- VHF        no scale change
- ECDIS      no scale change
- Radar      no scale change
- EOT        (1.000, 1.000, 1.000) → (0.550, 0.550, 0.550)
- Compass    (1.000, 1.000, 1.000) → (0.550, 0.550, 0.550)
- ShipWheel  (1.000, 1.000, 1.000) → (0.550, 0.550, 0.550)

## 2. Helm cluster lift (Y +0.05)
- EOT        localPos (0.000, 0.000, 0.000) → (0.000, 0.050, 0.000)
- Compass    localPos (0.000, 0.000, 0.000) → (0.000, 0.050, 0.000)
- ShipWheel  localPos (0.000, 0.000, 0.000) → (0.000, 0.050, 0.000)

## 3. Lighting
- Directional lights enabled: 1

## 4. Debug markers
- Will spawn a coloured sphere (0.10 m) above each anchor + the EOT lever pivot if missing

## 5. Summary
- Scale ops: **3** | Lift ops: **3** | Light: **ok**

**Executed.** Scene saved.
