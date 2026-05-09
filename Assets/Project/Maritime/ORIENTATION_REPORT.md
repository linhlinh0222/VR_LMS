# Bridge Orientation Fix Dry Run — 2026-05-10 01:54

## 1. Cabin rotation
- Current local euler: (270.00, 0.00, 0.00)
- Target local euler:  (-90.00, 0.00, 0.00)
- Action: already correct

## 2. Equipment local rotation reset
- AIS        already at identity — no change
- VHF        already at identity — no change
- ECDIS      already at identity — no change
- Radar      already at identity — no change
- EOT        already at identity — no change
- Compass    already at identity — no change
- ShipWheel  already at identity — no change

## 3. Camera reposition
- Current world pos: (0.00, 11.95, -28.00)  rot (10.00, 0.00, 0.00)
- Target  world pos: (0.00, 11.60, -25.60)  rot (0.00, 180.00, 0.00)
- Action: reposition

## 4. Summary
- Cabin rotation fix: no
- Equipment reset count: 0
- Camera reposition: yes

Dry-run only. To apply: `Tools/Maritime LMS/Fix Bridge Orientation — Execute`
