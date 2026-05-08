# 🚢 Bridge Cabin (Wheelhouse) - Asset Documentation

**Phiên bản:** v1.0
**Ngày tạo:** 2026-05-07
**Tác giả:** Maritime VR LMS Project
**Loại:** Architecture / Container scene

---

## 1. Giới thiệu

**Bridge Cabin (Wheelhouse)** là không gian buồng lái tàu - nơi học viên VR sẽ đứng để vận hành các thiết bị hàng hải.

### Use cases
- 🚢 **Container chính** cho khóa LMS - học viên spawn và làm bài tập trong này
- 🪟 **Tầm nhìn ra biển** qua 5 windows panoramic (kết hợp với Ocean asset)
- 🎯 **Anchor points** để Unity drop equipment prefabs vào vị trí đúng
- 📚 **Bối cảnh thực tế** cho mọi lessons về navigation, helm operations

### Real-world reference
Modern commercial ship bridge với layout chuẩn IMO:
- Helmsman station ở giữa (đứng vận hành ship wheel)
- Front console với equipment mounted (AIS, ECDIS, Radar)
- Captain's chair behind helmsman
- Chart table for paper navigation
- Side wing access doors

---

## 2. Thông số kỹ thuật

### Dimensions (real-world)
| Mục | Giá trị |
|---|---|
| **Width** (X) | 8.0 m |
| **Depth** (Y) | 5.0 m |
| **Height** (Z) | 2.5 m |
| **Wall thickness** | 100 mm |
| **Floor thickness** | 100 mm |
| **Window size** | 1.4 × 1.0 m × 5 windows |
| **Window bottom from floor** | 1.3 m (chest height) |
| **Side door** | 0.8 × 2.0 m × 2 doors |

### Geometry stats
| Component | Verts | Approximate |
|---|---:|---|
| Floor + Ceiling | 16 | 2 boxes |
| 4 walls (with cutouts) | ~150 | with window/door holes |
| 5 window glass panels | 40 | 8 verts each |
| Front console + top inset | ~100 | with bevels |
| Helm platform | 24 | raised step |
| Captain's chair | ~250 | base, pole, seat, back, armrests |
| Chart table | 30 | simple box |
| **TOTAL** | **~600 verts** | Very VR-friendly! |

### File sizes
| File | Size | Use |
|---|---:|---|
| `source/Bridge_v1.0_empty.blend` | 204 KB | Empty bridge (no equipment) |
| `source/Bridge_FullScene_v1.0.blend` | 1.3 MB | Bridge + 4 equipment composed |
| `exports/Bridge_v1.0_empty.glb` | 47 KB | Empty bridge for Unity |
| `exports/Bridge_v1.0_empty.fbx` | 91 KB | Empty bridge FBX |
| `exports/Bridge_FullScene_v1.0.glb` | 1.4 MB | Full scene with all equipment |

---

## 3. Hierarchy

```
Bridge_root (Empty - root pivot)
│
├─── 🏗️ Cabin_Shell (static architecture)
│    ├── cabin_floor                 [Wood floor 8×5m]
│    ├── cabin_ceiling                [Painted ceiling]
│    ├── wall_front                   [With 5 window cutouts]
│    ├── wall_back                    [Solid back wall]
│    ├── wall_left                    [With door cutout]
│    ├── wall_right                   [With door cutout]
│    └── window_glass_1..5            [Transparent panels]
│
├─── 🪑 Furniture (static interior)
│    ├── front_console                [Full-width desk for equipment]
│    ├── console_top_inset             [Equipment-mount surface]
│    ├── helm_platform                 [Raised step for helmsman]
│    ├── captain_chair_base/_pole/_seat/_back/_armrest_left/_right
│    └── chart_table                   [Navigation desk - back-left]
│
└─── 🎯 Equipment_Anchors (Empty markers)
     ├── anchor_AIS4000                [Position for AIS device]
     ├── anchor_ECDIS                  [Future ECDIS position]
     ├── anchor_Radar                  [Future Radar position]
     ├── anchor_ShipWheel              [Helm wheel position]
     ├── anchor_Compass                [Compass binnacle position]
     ├── anchor_EOT                    [Engine telegraph position]
     └── anchor_VHF                    [Future VHF radio position]
```

---

## 4. 🎯 Equipment Anchors

Mỗi anchor là một **Empty** với metadata để Unity script tự động drop equipment prefab vào vị trí đúng.

### Anchor positions (relative to Bridge_root)

| Anchor | Position (X, Y, Z) | Equipment |
|---|---|---|
| `anchor_AIS4000` | (-1.5, -2.4, 0.95) | AIS4000 mounted on console |
| `anchor_ECDIS` | (0.0, -2.4, 0.95) | ECDIS center console (future) |
| `anchor_Radar` | (1.5, -2.4, 0.95) | Marine Radar (future) |
| `anchor_ShipWheel` | (0.0, -0.9, 0.20) | Ship Wheel at helm |
| `anchor_Compass` | (0.8, -0.6, 0.20) | Compass binnacle - right of helm |
| `anchor_EOT` | (-0.8, -0.6, 0.20) | Engine Telegraph - left of helm |
| `anchor_VHF` | (-3.5, -1.5, 0.95) | VHF Radio (future) |

### Anchor metadata (custom properties)
Mỗi anchor có:
```json
{
  "equipment_info": "Mount [Equipment] here - description",
  "interactive_zone": true
}
```

### Unity usage
```csharp
// Find all anchors
GameObject bridgeRoot = GameObject.Find("Bridge_root");
foreach (Transform child in bridgeRoot.GetComponentsInChildren<Transform>()) {
    if (child.name.StartsWith("anchor_")) {
        // Check what equipment to instantiate
        string equipmentName = child.name.Replace("anchor_", "");

        // Instantiate the corresponding prefab
        var prefab = Resources.Load<GameObject>($"Prefabs/{equipmentName}_VR");
        if (prefab) {
            var instance = Instantiate(prefab, child.position, child.rotation, child.parent);
        }
    }
}
```

---

## 5. Materials

### `mat_bridge_floor`
- Wood-like dark teak (anti-slip flooring)
- Base: (0.10, 0.07, 0.04), Roughness 0.6, Coat 0.1

### `mat_bridge_wall`
- Off-white industrial paint
- Base: (0.85, 0.83, 0.80), Roughness 0.7

### `mat_bridge_ceiling`
- Lighter white painted ceiling
- Base: (0.92, 0.92, 0.90), Roughness 0.85

### `mat_bridge_console`
- Dark gray industrial console
- Base: (0.18, 0.18, 0.20), Roughness 0.5, Metallic 0.1

### `mat_console_top` (rubberized work surface)
- Base: (0.10, 0.10, 0.12), Roughness 0.65

### `mat_helm_platform` (wooden helm step)
- Base: (0.15, 0.10, 0.05), Roughness 0.55

### `mat_chair_leather` (captain's chair upholstery)
- Base: (0.08, 0.06, 0.04), Roughness 0.4, Coat 0.2

### `mat_chair_metal` (chair base + pole)
- Base: (0.30, 0.30, 0.32), Roughness 0.4, Metallic 0.7

### `mat_window_glass`
- Transparent glass with subtle tint
- Roughness 0.0, IOR 1.05, Alpha 0.15
- Surface render method: BLENDED

---

## 6. Lighting

### Interior lights (4 ceiling area lights)
- Position: 4 corners of ceiling (raised 5cm down)
- Energy: 80W each
- Size: 0.8m
- Color: Warm white (1.0, 0.95, 0.88)

### Sun (outdoor light through windows)
- Type: SUN
- Energy: 4.0
- Color: Warm afternoon (1.0, 0.97, 0.92)
- Angle: 0.5° (sharp shadows)

### World background
- Preetham Sky (afternoon)
- Direction: (0.5, -0.3, 0.7)
- Turbidity: 3.0

---

## 7. Unity Integration Guide

### Strategy: Modular workflow

The bridge is exported in **2 versions**:
1. **`Bridge_v1.0_empty.glb`** - Just the cabin shell + furniture (no equipment)
2. **`Bridge_FullScene_v1.0.glb`** - Bridge with all 4 equipment positioned

→ **Khuyến nghị Unity:** Dùng EMPTY bridge + drop equipment prefabs at runtime
→ **Lợi ích:** Dễ swap equipment, customize từng scenario

### Bước 1: Import Bridge empty

```
Drag Bridge_v1.0_empty.fbx → Assets/Models/Bridge/
Inspector → Materials → Extract Materials
```

### Bước 2: Setup as VR Player Container

```csharp
// On bridge prefab
public class BridgeManager : MonoBehaviour
{
    [Header("Equipment Prefabs (assigned in Inspector)")]
    public GameObject ais4000Prefab;
    public GameObject shipWheelPrefab;
    public GameObject compassPrefab;
    public GameObject eotPrefab;

    void Start() {
        // Find anchors and instantiate equipment
        Transform[] anchors = GetComponentsInChildren<Transform>()
            .Where(t => t.name.StartsWith("anchor_")).ToArray();

        foreach (Transform anchor in anchors) {
            GameObject prefab = GetPrefabForAnchor(anchor.name);
            if (prefab != null) {
                Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            }
        }
    }

    GameObject GetPrefabForAnchor(string anchorName) {
        return anchorName switch {
            "anchor_AIS4000" => ais4000Prefab,
            "anchor_ShipWheel" => shipWheelPrefab,
            "anchor_Compass" => compassPrefab,
            "anchor_EOT" => eotPrefab,
            _ => null
        };
    }
}
```

### Bước 3: Setup XR Player

```
1. Add XR Origin (XR Interaction Toolkit)
2. Position XR Origin at helm position:
   - Y: floor level (0.1m above ground)
   - X: 0
   - Z: -0.3 (slight in front of helm platform)
3. Add Continuous Movement Provider for ship-deck-rocking effect
4. Add Teleport zones around bridge for navigation
```

### Bước 4: Add windows + Ocean

The 5 window panels are already glass material. Outside the windows, you can:

**Option A:** Use Bridge_FullScene which includes all equipment but NOT ocean
**Option B:** Add Ocean asset separately:
```csharp
// Add Ocean prefab outside the bridge
GameObject ocean = Instantiate(oceanPrefab, new Vector3(0, -3, 0), Quaternion.identity);
ocean.transform.localScale = Vector3.one * 4; // 2km × 2km
```

### Bước 5: Lighting setup

For VR performance, replace Blender lights with Unity baked lighting:
1. Mark all bridge objects as Static
2. Window > Rendering > Lighting > Generate Lighting
3. Bake interior lighting (less than 1 minute for this small scene)
4. Add 1-2 realtime lights for dynamic effects (alarm flash, menu UI)

---

## 8. ⚠️ Known limitations

### 8.1. No ceiling lights mesh
- Lights là Blender point lights, không có ceiling fixture geometry
- Unity team có thể add light fixtures (lampshades) sau

### 8.2. Doors are cutouts only
- Side doors là HOLE trong wall, không có door geometry
- Add door meshes nếu cần animated doors

### 8.3. No bridge wing exteriors
- Outside the side doors là void
- Cần add wing platforms hoặc đóng door với glass panels

### 8.4. Equipment animations in FullScene
- 58 transform animations từ 4 equipment đều export OK trong GLB
- FBX FullScene file lớn (>10MB) có thể cần re-export với Blender directly

### 8.5. Procedural materials
- Materials có procedural noise (wood grain, etc.) không transfer Unity
- Sau import, cần re-create với Unity Shader Graph hoặc bake textures

---

## 9. Render Previews

| File | Mô tả |
|---|---|
| `renders/Bridge_hero.png` | 3/4 hero shot - showing entire interior |
| `renders/Bridge_helmsman_POV.png` | View from helm position looking forward at console |
| `renders/Bridge_helmsman_close.png` | Close-up of windows from helm |
| `renders/Bridge_interior_3_4.png` | Empty bridge 3/4 (no equipment) |
| `renders/Bridge_full_3_4.png` | Full scene với equipment placed |
| `renders/Bridge_topdown.png` | Top-down with ceiling visible |
| `renders/Bridge_topdown_layout.png` | Top-down without ceiling (showing layout) |

---

## 10. Suggested LMS Lessons

| Lesson | Required equipment |
|---|---|
| **Bridge Familiarization** | Empty bridge - tour each station |
| **Helm Operations** | Ship Wheel + Compass + EOT |
| **AIS Operations** | AIS4000 only |
| **Watchkeeping** | All 4 equipment + ocean view |
| **Emergency Drills** | Full scene + audio alerts |
| **Compass Course Steering** | Wheel + Compass (focus station) |
| **Engine Communication** | EOT + bridge intercom |

---

## 11. Future improvements (v1.1+)

- [ ] **Bridge wings** (outside platforms accessible via side doors)
- [ ] **Door geometry** với open/close animations
- [ ] **Light fixtures** (ceiling lamps as visible meshes)
- [ ] **Cable management** (visible cabling on consoles)
- [ ] **Day/Night mode** (red lights for night navigation)
- [ ] **Captain's quarters** (door at back leading to private cabin)
- [ ] **Detailed chart table** with paper charts + dividers
- [ ] **Coffee maker, telephone, intercom** (small details for atmosphere)
- [ ] **Window wipers + visibility effects** (rain/spray)
- [ ] **Fire extinguishers, emergency exits** (safety features)

---

## 12. Changelog

### v1.0 (2026-05-07)
- ✅ 8m × 5m × 2.5m commercial bridge
- ✅ 5 panoramic front windows + 2 side door cutouts
- ✅ Front console with equipment positions
- ✅ Helm platform raised 100mm
- ✅ Captain's chair (greybox)
- ✅ Chart table
- ✅ 7 equipment anchor empties (4 used + 3 future)
- ✅ Interior + sun lighting setup
- ✅ Composed FullScene với 4 thiết bị (AIS, Wheel, Compass, EOT)
- ✅ Both empty + full scene exported
- ✅ 7 render previews

---

**End of Document**
