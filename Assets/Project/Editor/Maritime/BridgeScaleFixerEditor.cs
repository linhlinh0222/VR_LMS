#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
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
    /// Phase 19 — bring oversize helm equipment down to a believable
    /// pedestal size, raise the equipment so its visible parts sit
    /// above the cabin floor, and make sure the scene actually has a
    /// light + visible debug markers at each anchor so the cadet can
    /// always tell where the equipment is supposed to be.
    /// </summary>
    /// <remarks>
    /// Diagnostic from Phase 18 reported these AABB heights:
    /// EOT 2.31 m, Compass 2.40 m, ShipWheel 2.79 m. Real bridge gear
    /// ranges 1.0–1.5 m. The FBX appears to be authored at roughly 2×
    /// real scale (or includes a tall integral pedestal that's already
    /// modelled into the mesh). Apply <c>localScale = 0.55</c> to those
    /// three so their bounds fall inside the 1.3–1.5 m human-reachable
    /// range, and lift them <c>+0.05 m</c> above the floor so they
    /// don't clip through the deck plate.
    /// </remarks>
    public static class BridgeScaleFixerEditor
    {
        private const string MenuDryRun = "Tools/Maritime LMS/Fix Bridge Scale — Dry Run";
        private const string MenuExecute = "Tools/Maritime LMS/Fix Bridge Scale — Execute";
        private const string ReportPath = "Assets/Project/Maritime/SCALE_FIX_REPORT.md";

        // Per-device scale multiplier applied to localScale. Matches the
        // ratios from the diagnostic AABB versus real-world dimensions.
        private static readonly Dictionary<string, float> ScaleByLabel = new()
        {
            ["EOT"] = 0.55f,
            ["Compass"] = 0.55f,
            ["ShipWheel"] = 0.55f
        };

        // Local Y offset (in cabin frame) lifting helm-cluster equipment
        // off the deck plate to avoid z-fighting with the cabin floor.
        private const float HelmLiftY = 0.05f;

        [MenuItem(MenuDryRun)]
        public static void DryRun() => Run(execute: false);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run(execute: true);

        public static string Run(bool execute)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Scale fix: no scene."); return null; }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Bridge Scale Fix {(execute ? "EXECUTE" : "Dry Run")} — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            var plan = new (string label, Component comp)[]
            {
                ("AIS",       Object.FindFirstObjectByType<AisTransceiver>()),
                ("VHF",       Object.FindFirstObjectByType<VhfRadio>()),
                ("ECDIS",     Object.FindFirstObjectByType<ElectronicChartDisplay>()),
                ("Radar",     Object.FindFirstObjectByType<MarineRadar>()),
                ("EOT",       Object.FindFirstObjectByType<EngineOrderTelegraph>()),
                ("Compass",   Object.FindFirstObjectByType<MagneticCompass>()),
                ("ShipWheel", Object.FindFirstObjectByType<ShipWheel>())
            };

            r.AppendLine("## 1. Equipment scale");
            int scaleChanges = 0;
            foreach (var p in plan)
            {
                if (p.comp == null) { r.AppendLine($"- {p.label,-10} NOT IN SCENE"); continue; }
                if (!ScaleByLabel.TryGetValue(p.label, out float mult)) { r.AppendLine($"- {p.label,-10} no scale change"); continue; }
                Vector3 cur = p.comp.transform.localScale;
                Vector3 target = Vector3.one * mult;
                if (Approx(cur, target)) { r.AppendLine($"- {p.label,-10} already at {mult:F2}"); continue; }
                r.AppendLine($"- {p.label,-10} {Format(cur)} → {Format(target)}");
                scaleChanges++;
            }
            r.AppendLine();

            r.AppendLine("## 2. Helm cluster lift (Y +" + HelmLiftY.ToString("F2") + ")");
            int liftChanges = 0;
            string[] liftLabels = { "EOT", "Compass", "ShipWheel" };
            foreach (string lbl in liftLabels)
            {
                Component c = System.Array.Find(plan, p => p.label == lbl).comp;
                if (c == null) { r.AppendLine($"- {lbl,-10} NOT IN SCENE"); continue; }
                Vector3 lp = c.transform.localPosition;
                Vector3 target = new Vector3(lp.x, HelmLiftY, lp.z);
                if (Mathf.Abs(lp.y - HelmLiftY) < 0.01f) { r.AppendLine($"- {lbl,-10} already lifted"); continue; }
                r.AppendLine($"- {lbl,-10} localPos {Format(lp)} → {Format(target)}");
                liftChanges++;
            }
            r.AppendLine();

            r.AppendLine("## 3. Lighting");
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            int directional = 0; foreach (Light l in lights) if (l.type == LightType.Directional && l.enabled) directional++;
            r.AppendLine($"- Directional lights enabled: {directional}");
            bool needLight = directional == 0;
            if (needLight) r.AppendLine($"- Will create a default sun light");
            r.AppendLine();

            r.AppendLine("## 4. Debug markers");
            r.AppendLine($"- Will spawn a coloured sphere (0.10 m) above each anchor + the EOT lever pivot if missing");
            r.AppendLine();

            r.AppendLine("## 5. Summary");
            r.AppendLine($"- Scale ops: **{scaleChanges}** | Lift ops: **{liftChanges}** | Light: **{(needLight ? "create" : "ok")}**");
            if (!execute) { r.AppendLine($"\nDry-run only. To apply: `{MenuExecute}`"); FlushReport(r); return r.ToString(); }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Fix Bridge Scale + Markers");
            try
            {
                foreach (var p in plan)
                {
                    if (p.comp == null) continue;
                    if (ScaleByLabel.TryGetValue(p.label, out float mult))
                    {
                        Undo.RecordObject(p.comp.transform, $"Scale {p.label}");
                        p.comp.transform.localScale = Vector3.one * mult;
                        EditorUtility.SetDirty(p.comp.transform);
                    }
                    if (System.Array.IndexOf(liftLabels, p.label) >= 0)
                    {
                        Undo.RecordObject(p.comp.transform, $"Lift {p.label}");
                        Vector3 lp = p.comp.transform.localPosition;
                        p.comp.transform.localPosition = new Vector3(lp.x, HelmLiftY, lp.z);
                        EditorUtility.SetDirty(p.comp.transform);
                    }
                }

                if (needLight)
                {
                    GameObject sun = new GameObject("Maritime Sun (auto)");
                    Undo.RegisterCreatedObjectUndo(sun, "Create sun");
                    sun.transform.SetPositionAndRotation(new Vector3(0, 20, -20), Quaternion.Euler(50, -30, 0));
                    Light l = sun.AddComponent<Light>();
                    l.type = LightType.Directional;
                    l.color = new Color(1f, 0.97f, 0.92f);
                    l.intensity = 1.2f;
                    l.shadows = LightShadows.Soft;
                }

                EnsureDebugMarkers();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                r.AppendLine();
                r.AppendLine("**Executed.** Scene saved.");
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Scale fix done: scale={scaleChanges}, lift={liftChanges}, light={needLight}.");
            }
            catch (System.Exception ex)
            {
                r.AppendLine($"**FAILED**: {ex.Message}");
                Debug.LogError($"[Maritime LMS] Scale fix failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undoGroup); }

            FlushReport(r);
            return r.ToString();
        }

        private static void EnsureDebugMarkers()
        {
            GameObject root = GameObject.Find("BridgeCabin");
            if (root == null) return;
            Transform markers = root.transform.Find("DebugMarkers");
            if (markers != null) Undo.DestroyObjectImmediate(markers.gameObject);
            GameObject markersGO = new GameObject("DebugMarkers");
            Undo.RegisterCreatedObjectUndo(markersGO, "DebugMarkers root");
            markersGO.transform.SetParent(root.transform, false);

            string[] anchors = { "anchor_AIS4000", "anchor_VHF", "anchor_ECDIS",
                                 "anchor_Radar",   "anchor_EOT", "anchor_Compass", "anchor_ShipWheel" };
            Color[] colors = {
                new Color(1f, 0.4f, 0.4f),
                new Color(1f, 0.8f, 0.2f),
                new Color(0.4f, 1f, 0.5f),
                new Color(0.4f, 0.7f, 1f),
                new Color(1f, 0.2f, 1f),
                new Color(0.2f, 1f, 1f),
                new Color(1f, 1f, 0.4f)
            };
            for (int i = 0; i < anchors.Length; i++)
            {
                Transform a = FindDeep(root.transform, anchors[i]);
                if (a == null) continue;
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(sphere, $"Marker {anchors[i]}");
                sphere.name = "M_" + anchors[i];
                sphere.transform.SetParent(markersGO.transform, false);
                sphere.transform.position = a.position + Vector3.up * 0.4f;
                sphere.transform.localScale = Vector3.one * 0.15f;
                Object.DestroyImmediate(sphere.GetComponent<Collider>());
                Renderer rn = sphere.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                mat.color = colors[i];
                rn.sharedMaterial = mat;
            }
        }

        private static Transform FindDeep(Transform t, string n)
        {
            if (t.name == n) return t;
            for (int i = 0; i < t.childCount; i++) { Transform f = FindDeep(t.GetChild(i), n); if (f != null) return f; }
            return null;
        }

        private static bool Approx(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 0.0001f;
        private static string Format(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";

        private static void FlushReport(StringBuilder r)
        {
            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] scale report write failed: {ex.Message}"); }
            Debug.Log(text);
        }
    }

    [InitializeOnLoad]
    public static class BridgeScaleFixerAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.ScaleFixRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteScaleFix.flag";

        static BridgeScaleFixerAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Scale Fix")]
        public static void ArmAutoExecute()
        {
            System.IO.File.WriteAllText(ExecuteSentinelPath, System.DateTime.UtcNow.ToString("o"));
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log($"[Maritime LMS] Scale-fix auto-execute armed.");
        }

        private static void MaybeRun()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += MaybeRun; return; }
            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.path.EndsWith("MaritimeBridgeLMS.unity")) { EditorApplication.delayCall += MaybeRun; return; }
            if (GameObject.Find("BridgeCabin") == null) { EditorApplication.delayCall += MaybeRun; return; }

            bool execute = System.IO.File.Exists(ExecuteSentinelPath);
            try
            {
                Debug.Log($"[Maritime LMS] Scale-fix {(execute ? "EXECUTE" : "dry-run")} starting...");
                BridgeScaleFixerEditor.Run(execute: execute);
                if (execute) { try { System.IO.File.Delete(ExecuteSentinelPath); } catch (System.Exception ex) { Debug.LogWarning($"sentinel del: {ex.Message}"); } }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] scale-fix hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
