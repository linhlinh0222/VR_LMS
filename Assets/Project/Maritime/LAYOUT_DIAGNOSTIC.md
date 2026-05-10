# Bridge Layout Diagnostic — 2026-05-10 11:26
Scene: `Assets/Project/Scenes/MaritimeBridgeLMS.unity`

## Camera (player rig)
- Name: `Desktop Mock VR Camera`
- World position: (0.00, 11.85, -28.20)
- World rotation: (8.00, 0.00, 0.00)
- Forward: (0.00, -0.14, 0.99)

## BridgeCabin
- World position: (0.00, 10.23, -28.00)
- Local euler:    (270.00, 0.00, 0.00)
- Local scale:    (1.00, 1.00, 1.00)
- Bounds (world): center (0.00, 11.53, -28.00) size (8.00, 2.60, 5.00)
- Bounds Y range: [10.23, 12.83]

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
- World position: (1.10, 11.05, -26.00)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-0.40, 0.00, -0.13)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (1.00, 1.00, 1.00)
- Active in hierarchy: True
- Bounds size:     (0.19, 0.17, 0.09)
- Distance to camera: 2.59m
- Camera dot (1=in front): 0.89
### VHF
- Parent: `anchor_VHF`
- World position: (2.00, 11.10, -26.10)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-1.50, -0.40, -0.08)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (1.00, 1.00, 1.00)
- Active in hierarchy: True
- Bounds size:     (0.18, 0.11, 0.07)
- Distance to camera: 3.00m
- Camera dot (1=in front): 0.73
### ECDIS
- Parent: `anchor_ECDIS`
- World position: (-2.00, 11.10, -26.10)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-2.00, 0.10, -0.08)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (1.00, 1.00, 1.00)
- Active in hierarchy: True
- Bounds size:     (0.62, 0.39, 0.14)
- Distance to camera: 3.00m
- Camera dot (1=in front): 0.73
### Radar
- Parent: `anchor_Radar`
- World position: (-1.10, 11.05, -26.00)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (0.40, 0.00, -0.13)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (1.00, 1.00, 1.00)
- Active in hierarchy: True
- Bounds size:     (0.38, 0.43, 0.16)
- Distance to camera: 2.59m
- Camera dot (1=in front): 0.89
### EOT
- Parent: `anchor_EOT`
- World position: (0.00, 10.50, -26.85)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-0.80, -0.55, 0.07)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (0.65, 0.65, 0.65)
- Active in hierarchy: True
- Bounds size:     (0.39, 1.50, 0.26)
- Distance to camera: 1.91m
- Camera dot (1=in front): 0.80
### Compass
- Parent: `anchor_Compass`
- World position: (0.00, 11.50, -26.20)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (0.80, -1.20, 1.07)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (0.55, 0.55, 0.55)
- Active in hierarchy: True
- Bounds size:     (0.25, 1.32, 0.24)
- Distance to camera: 2.03m
- Camera dot (1=in front): 1.00
### ShipWheel
- Parent: `anchor_ShipWheel`
- World position: (-0.50, 10.50, -27.00)
- World rotation: (270.00, 0.00, 0.00)
- Local position:  (-0.50, -0.10, 0.07)
- Local rotation:  (0.00, 0.00, 0.00)
- Local scale:     (0.55, 0.55, 0.55)
- Active in hierarchy: True
- Bounds size:     (0.42, 1.53, 0.24)
- Distance to camera: 1.87m
- Camera dot (1=in front): 0.73

## EOT lever pivot
- handle_lever_pivot world pos: (-0.07, 11.24, -26.85)
- distance to camera: 1.48m
- enabled: True
- renderers under pivot: 2 (2 active)

