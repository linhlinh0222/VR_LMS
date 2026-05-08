# 🌊 Ocean Environment - Asset Documentation

**Phiên bản:** v1.1 (latest)
**Ngày tạo:** 2026-05-07
**Tác giả:** Maritime VR LMS Project
**Loại:** Environment / Skybox asset

## 🆕 What's new in v1.1
- ✅ **Preetham Sky** - atmospheric scattering for realistic horizon (replaced flat gradient)
- ✅ **Extended ocean** to 2km × 2km (no horizon black band)
- ✅ **Better atmospheric haze** at distance (real ocean look)
- ✅ Sun angle adjustable (afternoon, sunset modes documented)
- ⚠️ CloudScapes volumetric clouds **không dùng** (quá heavy cho VR Eevee runtime - documented alternative below)

---

## 1. Giới thiệu

**Ocean Environment** là asset môi trường biển dùng làm **bối cảnh nhìn ra ngoài** từ buồng lái tàu trong khóa LMS hàng hải VR.

### Use cases
- 🚢 Học viên đứng trong buồng lái → nhìn ra cửa sổ thấy biển
- 🌅 Background cho cinematic / promotional renders
- 🎬 Reference cho lighting + scale của outdoor scenes
- 📚 Lessons về navigation cần context biển

### Sea state
Hiện tại model là **Sea State 3** (Beaufort 3 - Slight):
- Wave height: 0.5-1.25m (gentle)
- Total displacement range: ~2.5m peak-to-trough
- White caps occasional
- Wind: 7-10 knots equivalent

---

## 2. Thông số kỹ thuật

### Geometry
| Component | Verts | Faces |
|---|---:|---:|
| Ocean plane | 6,561 | 6,400 |
| Sky dome | 642 | 1,280 |
| **Total** | **7,203** | **7,680** |

### Dimensions
| Element | Size |
|---|---|
| Ocean plane | 500m × 500m |
| Subdivision | 80×80 grid |
| Sky dome radius | 600m |
| Wave height (max) | 2.5m peak-to-trough |
| Wave wavelength | ~50m (main), ~8m (chop) |

### File sizes (v1.1 - latest)
| File | Size |
|---|---:|
| `source/Ocean_VR_v1.1.blend` | ~700 KB (with Preetham sky) |
| `exports/Ocean_VR_v1.1.glb` | 233 KB |
| `exports/Ocean_VR_v1.1.fbx` | 270 KB |
| `source/Ocean_VR_v1.0.blend` | 254 KB (legacy) |
| `exports/Ocean_VR_v1.0.glb` | 256 KB (legacy) |
| `exports/Ocean_VR_v1.0.fbx` | 303 KB (legacy) |

### Sky settings (Preetham, v1.1)
| Param | Value | Note |
|---|---|---|
| sun_direction | (0.5, -0.3, 0.7) | Afternoon |
| turbidity | 3.0 | 1=clear, 10=hazy |
| ground_albedo | 0.3 | Reflection from ground |
| world strength | 0.7 | Brightness multiplier |

---

## 3. Hierarchy

```
Ocean_root (Empty - root)
│
├─── 🌊 Water
│    ├── ocean_plane              [Subdivided plane with displacement]
│    └── wave_driver (Empty)       [Animation source for wave drift]
│
└─── ☁️ Sky
     └── sky_dome                  [Inverted icosphere, gradient sky]
```

---

## 4. Materials breakdown

### `mat_ocean_water` (6 layers)

```
Layer 1: Tex Coord (Object space) → Mapping
Layer 2: 3 Noise textures (big/medium/small wavelengths)
Layer 3: MixRGB nodes combining noises into height map
Layer 4: Bump node → Normal output (surface ripple detail)
Layer 5: ColorRamp → depth-aware blue gradient
Layer 6: Threshold + MixRGB → foam on wave crests (white)

→ Principled BSDF:
  - Roughness: 0.08 (very glossy water)
  - Coat: 0.8 (clear coat for reflections)
  - IOR: 1.33 (water's actual IOR)
```

### `mat_sky_dome`

```
Tex Coord (Object Z) → Multiply-Add (normalize -1..1 to 0..1)
                     → ColorRamp (5 stops):
                        0.0  - Dark below horizon (sea reflection tint)
                        0.30 - Hazy lower atmosphere
                        0.45 - Warm horizon glow
                        0.70 - Mid-sky blue
                        1.00 - Deeper zenith blue

→ Emission shader (strength 1.5)
```

---

## 5. Animation

| Clip | Frames | Loop? | Mô tả |
|---|:---:|:---:|---|
| `ocean_idle_calm` | 120 | Yes | Wave_driver moves 30m → wave pattern drifts continuously |

**Note:** Animation drives the wave displacement texture position. Waves "flow" naturally by 7.5m/sec across surface (gentle drift).

---

## 6. ⚠️ IMPORTANT - Unity Integration

### 🎯 The challenge with water in Unity

Blender's procedural water shader **DOES NOT export directly to Unity**. The exported GLB/FBX contains:
- ✅ The **geometry** (subdivided plane with baked displacement) - 1 frame static state
- ✅ **Approximate materials** (PBR with Albedo color)
- ❌ NOT animated waves
- ❌ NOT procedural noise (foam, depth gradient)
- ❌ NOT real-time reflections

→ **Unity team SHOULD replace the water material** với một solution chuyên dụng. Có 3 options:

### Option A: Unity URP Sample Asset Pack (FREE) ⭐ Recommended

Download **URP Sample Assets** từ Unity Package Manager:
```
Window → Package Manager → URP Sample Assets
  → Includes "BoatAttack Water" - production-ready water shader
```

Setup:
```csharp
// 1. Drag "Water Surface" prefab vào scene
// 2. Replace ocean_plane material với "Water_URP" material
// 3. Configure parameters:
//    - Wave height
//    - Wave count (3-5 waves typical)
//    - Wave direction
//    - Foam color, intensity
//    - Depth fade distance
```

### Option B: Crest Ocean (PAID, $99) - Photoreal

Asset Store: **Crest Ocean System** by Wave Harmonic
- Industry-grade FFT waves
- Caustics, reflections, refractions
- Used by professional studios

### Option C: Custom Shader Graph (FREE, most control)

#### Recipe: Replicating our Blender water trong Unity Shader Graph

```
1. Create new Shader Graph (Universal/PBR)
2. Add nodes:

   [Time] → [Multiply] (with wave speed)
          ↓
   [UV] → [Add] → Normal Map sampler 1 (large waves)
        ↘ Add → Normal Map sampler 2 (small ripples)
                ↓
            [Normal Blend Reoriented]
                ↓
            [Master node: Normal]

3. Color:
   [World Position Y - Camera Y] → [Normalize/Saturate]
                                   → [Lerp(deep_color, surface_color)]
                                   → [Master: Albedo]

4. Foam:
   Sample wave height texture → [Step 0.7] → [Lerp(water, white)]

5. Reflections:
   Sample [Reflection Probe]
   Combine with Fresnel for edge-based reflectivity

6. Depth fade:
   [Scene Depth - Pixel Depth] → fade alpha at edges
```

#### Preset values (ready to copy):
```
Deep color:     RGB(0.001, 0.025, 0.10)  (deep ocean blue)
Surface color:  RGB(0.05, 0.25, 0.42)   (teal-blue)
Foam color:     RGB(0.95, 0.97, 1.0)    (white)
Foam threshold: 0.62
Wave speed:     0.5 (units/sec)
Wave height:    1.2m (main) + 0.3m (chop)
Roughness:      0.08
Coat:          0.8
IOR:           1.33
```

### Bước import vào Unity

```
1. Import Ocean_VR_v1.0.fbx vào Assets/Models/Ocean/
2. Trong Hierarchy:
   - Drag prefab vào scene
   - Position: Y=0 (sea level)
   - Scale: 1 (already real-world meters)
3. Replace material với water solution chosen ở trên
4. Sky dome: keep as-is OR replace với Unity skybox
```

### Alternative: Use Unity's built-in Skybox

Nếu không cần ocean horizon trong scene chính, dùng:
```
Window → Rendering → Lighting → Environment
  → Skybox Material: Default-Skybox (procedural sky with sun)
```

Sau đó chỉ cần ocean PLANE bên dưới (không cần sky_dome của ta).

---

## 7. Recommended VR Performance Setup

### Quest 2/3 considerations

| Setting | Value | Note |
|---|---|---|
| Plane subdivision | 40×40 (LOD0) → 20×20 (LOD1) | Use Unity LOD Group |
| Reflection probes | 1 baked + 1 realtime | Static + dynamic skybox |
| Wave count (shader) | 3-4 max | Each wave = math cost |
| Foam: cubemap or noise | Cubemap (cheaper) | Pre-baked foam texture |
| Refraction | Disable for Quest | Use simple alpha blend |
| Reflection: planar reflection | OFF | Use SSR + cubemap |
| Resolution: water render texture | 256×256 | Compress aggressively |

### Performance budget
```
Total scene poly budget for Quest:    ~500K visible
Ocean asset reasonable share:         < 50K (10%)
Recommended LOD0 ocean:                10K verts (in 30m radius around ship)
LOD1 (mid distance):                   3K verts
LOD2 (far distance):                   500 verts (just colored plane)
```

### Cull distance
```
Camera Far Clip: 2000m (cho horizon visible)
Ocean LOD distances:
  LOD0: 0-50m   (full quality, all features)
  LOD1: 50-200m (reduced detail)
  LOD2: 200m+   (just flat colored plane + horizon line)
```

---

## 8. Render Previews

| File | Mô tả |
|---|---|
| `renders/Ocean_hero.png` | Hero shot - 22m altitude, 30° fov |
| `renders/Ocean_bridge_view.png` | Bridge view simulation (18m height) |
| `renders/Ocean_lowangle.png` | Low angle dramatic shot (3m altitude) |
| `renders/Ocean_topdown.png` | Top-down ortho (showing wave pattern) |
| `renders/Ocean_animated_f30.png` | Frame 30 of wave animation |

---

## 9. Suggested Lessons / Use cases

| Scene | Ocean usage |
|---|---|
| **Bridge Equipment Tour** | Background outside windows |
| **Navigation Lessons** | Visual reference for course direction |
| **COLREG Scenarios** | Other vessels visible on water |
| **Weather Recognition** | Train calm vs rough sea distinction |
| **Watchkeeping Drill** | Spot lights/objects on sea surface |
| **Emergency Procedures** | MOB scenarios with rough sea |

---

## 10. Sea State Variants (Future v1.1+)

Currently: **Calm Sea (State 3)**

### Planned variants

| Variant | Wave height | Wind | Use case |
|---|---:|---|---|
| `Ocean_calm` (current) | 0.5-1.25m | 7-10 kt | Standard lessons |
| `Ocean_moderate` | 1.25-2.5m | 11-16 kt | Maneuvering challenges |
| `Ocean_rough` | 2.5-4m | 17-21 kt | Emergency training |
| `Ocean_storm` | 4-6m+ | 22+ kt | Survival scenarios |
| `Ocean_calm_night` | 0.5m | 5 kt | Night navigation |
| `Ocean_dawn` | 1m | 8 kt | Sunrise scenes |

→ All variants will share same geometry, different shader presets.

---

## 11. Changelog

### v1.1 (2026-05-07)
- ✅ **Preetham Sky** atmospheric scattering (no horizon artifacts)
- ✅ Ocean extended to **2km × 2km**
- ✅ Sun position configurable (daytime/sunset modes)
- ✅ New renders: `Ocean_v11_hero.png`, `Ocean_v11_bridge.png`, `Ocean_v11_sunset.png`
- ⚠️ CloudScapes volumetric clouds tested but **excluded** from runtime (too heavy)

### CloudScapes (decision log)
Tested CloudScapes_Free addon với volumetric VDB clouds. Findings:
- Eevee Next has volumetric_end clamping (default 100m, must extend to 1500m+)
- Volume rendering causes timeout on high-poly volumes
- Cloud lighting requires careful sun positioning to avoid black silhouette
- **Decision:** Use Preetham Sky for atmospheric realism, skip volumetric clouds
- **For Unity:** Use Unity's volumetric clouds (URP) or Crest Ocean's sky system

### v1.0 (2026-05-07)
- ✅ Procedural water shader (6 layers: noise, depth, foam)
- ✅ Vertex displacement (2 modifiers: wave + chop)
- ✅ Subdivision surface for smooth shading
- ✅ Sky dome with 5-stop gradient
- ✅ Sun light + warm world ambient
- ✅ Wave animation (120 frames, drift loop)
- ✅ 5 render previews
- ✅ Documentation với Unity Shader Graph recipe

### Future improvements
- [ ] Additional sea states (moderate, rough, storm)
- [ ] Day/night cycle (HDRI swappable)
- [ ] Ship wake foam trail decal
- [ ] Distant land/objects (low-poly islands)
- [ ] Caustics under water (for diving lessons)
- [ ] Bake animated normal maps for Unity

---

## 12. Credits & Notes

**Inspiration:** Real ocean references at calm sea state, mid-day lighting.
**Note:** Asset designed for **mid-range VR** (Quest 2+). Photoreal water requires Unity-specific solution (Crest Ocean recommended).

---

**End of Document**
