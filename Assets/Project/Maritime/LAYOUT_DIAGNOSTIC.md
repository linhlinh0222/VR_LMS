# Bridge Layout Diagnostic — 2026-05-10 01:54
Scene: `Assets/Project/Scenes/MaritimeBridgeLMS.unity`

## Camera (player rig)
- Name: `Desktop Mock VR Camera`
- World position: (0.00, 11.95, -28.00)
- World rotation: (10.00, 0.00, 0.00)
- Forward: (0.00, -0.17, 0.98)

## BridgeCabin
- World position: (0.00, 10.23, -28.00)
- Local euler:    (270.00, 0.00, 0.00)
- Local scale:    (1.00, 1.00, 1.00)
- Bounds (world): center (0.00, 11.55, -28.00) size (8.00, 2.64, 5.00)
- Bounds Y range: [10.23, 12.87]

### Anchor world positions
- anchor_AIS4000       world (1.50, 11.18, -26.00) rot (270.00, 0.00, 0.00)
- anchor_VHF           world (3.50, 11.18, -26.50) rot (270.00, 0.00, 0.00)
- anchor_ECDIS         world (0.00, 11.18, -26.00) rot (270.00, 0.00, 0.00)
- anchor_Radar         world (-1.50, 11.18, -26.00) rot (270.00, 0.00, 0.00)
- anchor_EOT           world (0.80, 10.43, -27.40) rot (270.00, 0.00, 0.00)
- anchor_Compass       world (-0.80, 10.43, -27.40) rot (270.00, 0.00, 0.00)
- anchor_ShipWheel     world (0.00, 10.43, -27.10) rot (270.00, 0.00, 0.00)

## Equipment
### AIS
- Parent: `anchor_AIS4000`
- World position: (1.05, 11.05, -25.80)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-0.45, -0.20, -0.13)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (1.00, 1.00, 1.00)
- Active in hierarchy: True
- Bounds size:     (0.19, 0.17, 0.09)
- Distance to camera: 2.60m
- Camera dot (1=in front): 0.89
### VHF
- Parent: `anchor_VHF`
- World position: (2.10, 11.05, -26.00)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-1.40, -0.50, -0.13)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (1.00, 1.00, 1.00)
- Active in hierarchy: True
- Bounds size:     (0.18, 0.11, 0.07)
- Distance to camera: 3.04m
- Camera dot (1=in front): 0.70
### ECDIS
- Parent: `anchor_ECDIS`
- World position: (-2.10, 11.05, -26.00)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-2.10, 0.00, -0.13)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (1.00, 1.00, 1.00)
- Active in hierarchy: True
- Bounds size:     (0.62, 0.39, 0.14)
- Distance to camera: 3.04m
- Camera dot (1=in front): 0.70
### Radar
- Parent: `anchor_Radar`
- World position: (-1.05, 11.05, -25.80)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (0.45, -0.20, -0.13)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (1.00, 1.00, 1.00)
- Active in hierarchy: True
- Bounds size:     (0.38, 0.43, 0.16)
- Distance to camera: 2.60m
- Camera dot (1=in front): 0.89
### EOT
- Parent: `anchor_EOT`
- World position: (0.00, 10.50, -26.70)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-0.80, -0.70, 0.07)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (0.55, 0.55, 0.55)
- Active in hierarchy: True
- Bounds size:     (0.33, 1.27, 0.22)
- Distance to camera: 1.95m
- Camera dot (1=in front): 0.79
### Compass
- Parent: `anchor_Compass`
- World position: (0.00, 11.55, -25.95)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (0.80, -1.45, 1.12)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (0.55, 0.55, 0.55)
- Active in hierarchy: True
- Bounds size:     (0.25, 1.32, 0.24)
- Distance to camera: 2.09m
- Camera dot (1=in front): 1.00
### ShipWheel
- Parent: `anchor_ShipWheel`
- World position: (-0.65, 10.50, -27.00)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-0.65, -0.10, 0.07)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (0.55, 0.55, 0.55)
- Active in hierarchy: True
- Bounds size:     (0.42, 1.53, 0.24)
- Distance to camera: 1.88m
- Camera dot (1=in front): 0.66

## EOT lever pivot
- handle_lever_pivot world pos: (-0.06, 11.13, -26.70)
- distance to camera: 1.54m
- enabled: True
- renderers under pivot: 2 (2 active)

