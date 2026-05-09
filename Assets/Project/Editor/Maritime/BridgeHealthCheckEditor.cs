#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaritimeLMS.Ais;
using MaritimeLMS.Compass;
using MaritimeLMS.Ecdis;
using MaritimeLMS.Helm;
using MaritimeLMS.Radar;
using MaritimeLMS.Telegraph;
using MaritimeLMS.Vhf;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 22 — automated health-check that walks the scene and reports
    /// the common reasons a player would fail to interact with the bridge:
    /// equipment with no Collider on the grab path, the player camera's
    /// near-clip plane swallowing equipment that's right in front of it,
    /// missing Renderers, equipment off-screen relative to the camera
    /// frustum. Writes a markdown report so issues can be triaged without
    /// the Editor open.
    /// </summary>
    /// <remarks>
    /// Read-only — does not modify the scene. Subsequent fixers can be
    /// authored to address whatever the report flags.
    /// </remarks>
    public static class BridgeHealthCheckEditor
    {
        private const string MenuRun = "Tools/Maritime LMS/Health Check — Run";
        private const string ReportPath = "Assets/Project/Maritime/HEALTH_CHECK.md";

        [MenuItem(MenuRun)]
        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Health check: no scene."); return; }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Bridge Health Check — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            // Camera.
            r.AppendLine("## Camera");
            Camera cam = Camera.main;
            if (cam == null) r.AppendLine("- **No Camera.main**");
            else
            {
                r.AppendLine($"- pos {Format(cam.transform.position)}  rot {Format(cam.transform.eulerAngles)}");
                r.AppendLine($"- near clip: {cam.nearClipPlane:F3}  far clip: {cam.farClipPlane:F0}");
                r.AppendLine($"- FOV: {cam.fieldOfView:F1}°");
                r.AppendLine($"- culling mask: 0x{cam.cullingMask:X8}");
                if (cam.nearClipPlane > 0.05f) r.AppendLine($"- ⚠️ near clip {cam.nearClipPlane:F3} m may be too far — equipment within that distance is invisible");
            }
            r.AppendLine();

            // Equipment.
            var equips = new (string label, Component comp)[]
            {
                ("AIS",       Object.FindFirstObjectByType<AisTransceiver>()),
                ("VHF",       Object.FindFirstObjectByType<VhfRadio>()),
                ("ECDIS",     Object.FindFirstObjectByType<ElectronicChartDisplay>()),
                ("Radar",     Object.FindFirstObjectByType<MarineRadar>()),
                ("EOT",       Object.FindFirstObjectByType<EngineOrderTelegraph>()),
                ("Compass",   Object.FindFirstObjectByType<MagneticCompass>()),
                ("ShipWheel", Object.FindFirstObjectByType<ShipWheel>())
            };

            r.AppendLine("## Equipment integrity");
            int issueCount = 0;
            foreach (var (label, c) in equips)
            {
                if (c == null)
                {
                    r.AppendLine($"### {label} — **NOT IN SCENE**");
                    issueCount++;
                    continue;
                }
                Transform t = c.transform;
                r.AppendLine($"### {label}");
                r.AppendLine($"- active: {t.gameObject.activeInHierarchy}");
                Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
                int activeRens = 0;
                foreach (Renderer ren in rs)
                    if (ren.enabled && ren.gameObject.activeInHierarchy) activeRens++;
                r.AppendLine($"- renderers: {rs.Length} ({activeRens} active)");
                if (activeRens == 0)
                {
                    r.AppendLine("- ⚠️ no active renderers — invisible");
                    issueCount++;
                }
                Collider[] cs = t.GetComponentsInChildren<Collider>(true);
                int activeCols = 0;
                foreach (Collider col in cs)
                    if (col.enabled && col.gameObject.activeInHierarchy && !col.isTrigger) activeCols++;
                r.AppendLine($"- colliders: {cs.Length} ({activeCols} active non-trigger)");
                if (activeCols == 0)
                {
                    r.AppendLine("- ⚠️ no active solid colliders — LMB grab raycast cannot hit");
                    issueCount++;
                }
                if (cam != null)
                {
                    Bounds b = new Bounds(t.position, Vector3.zero);
                    bool any = false;
                    foreach (Renderer ren in rs)
                        if (ren.enabled && ren.gameObject.activeInHierarchy)
                        {
                            if (!any) { b = ren.bounds; any = true; }
                            else b.Encapsulate(ren.bounds);
                        }
                    if (any)
                    {
                        Vector3 vp = cam.WorldToViewportPoint(b.center);
                        bool inFrustum = vp.z > cam.nearClipPlane && vp.z < cam.farClipPlane
                                         && vp.x >= 0 && vp.x <= 1 && vp.y >= 0 && vp.y <= 1;
                        r.AppendLine($"- centre viewport: ({vp.x:F2}, {vp.y:F2}, z={vp.z:F2}) in-frustum: {inFrustum}");
                        if (!inFrustum) { r.AppendLine("- ⚠️ outside camera frustum"); issueCount++; }
                    }
                }
            }
            r.AppendLine();

            // Lighting.
            r.AppendLine("## Lighting");
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            r.AppendLine($"- total Lights: {lights.Length}");
            int directional = 0, enabledLights = 0;
            foreach (Light l in lights)
            {
                if (l.enabled && l.gameObject.activeInHierarchy)
                {
                    enabledLights++;
                    if (l.type == LightType.Directional) directional++;
                }
            }
            r.AppendLine($"- enabled: {enabledLights}, directional: {directional}");
            r.AppendLine($"- ambient mode: {RenderSettings.ambientMode}");
            r.AppendLine($"- ambient intensity: {RenderSettings.ambientIntensity:F2}");
            r.AppendLine($"- skybox: {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "(none)")}");
            r.AppendLine();

            // Lesson root.
            r.AppendLine("## Lesson");
            GameObject lessonRoot = GameObject.Find("MaritimeLessonRoot");
            r.AppendLine($"- MaritimeLessonRoot: {(lessonRoot != null && lessonRoot.activeInHierarchy ? "active" : "**MISSING**")}");
            GameObject lessonCanvas = GameObject.Find("MaritimeLessonRoot/LessonCanvas");
            r.AppendLine($"- LessonCanvas: {(lessonCanvas != null && lessonCanvas.activeInHierarchy ? "active" : "**MISSING**")}");
            r.AppendLine();

            r.AppendLine($"## Total issues: **{issueCount}**");

            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] health report write failed: {ex.Message}"); }
            Debug.Log(text);
        }

        private static string Format(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
    }

    [InitializeOnLoad]
    public static class BridgeHealthCheckAutoHook
    {
        // v2: re-run after Phase 23 (colliders + tighten console).
        private const string SessionStateKey = "MaritimeLMS.HealthCheckRan.v2";

        static BridgeHealthCheckAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Force Health Check On Next Compile")]
        public static void Rearm() { SessionState.SetBool(SessionStateKey, false); }

        private static void MaybeRun()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += MaybeRun; return; }
            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.path.EndsWith("MaritimeBridgeLMS.unity")) { EditorApplication.delayCall += MaybeRun; return; }
            if (GameObject.Find("BridgeCabin") == null) { EditorApplication.delayCall += MaybeRun; return; }

            try { BridgeHealthCheckEditor.Run(); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] health check threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
