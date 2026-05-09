#if UNITY_EDITOR
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
    /// Diagnostic dump of the bridge layout: world position / rotation /
    /// bounds for the camera, the BridgeCabin, every anchor, and every
    /// equipment GameObject. Writes to
    /// <c>Assets/Project/Maritime/LAYOUT_DIAGNOSTIC.md</c> so a teammate
    /// can read it without running the editor themselves.
    /// </summary>
    /// <remarks>
    /// Used to spot misplaced equipment (e.g. cabin imported with wrong
    /// up-axis, equipment buried under the floor, or camera sitting
    /// outside the cabin shell). The diagnostic is the input the layout
    /// tuner needs before it can decide how to reposition things.
    /// </remarks>
    public static class BridgeLayoutDiagnosticEditor
    {
        private const string ReportPath = "Assets/Project/Maritime/LAYOUT_DIAGNOSTIC.md";

        [MenuItem("Tools/Maritime LMS/Layout Diagnostic — Dump")]
        public static void Dump()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[Maritime LMS] Layout diagnostic: no active scene.");
                return;
            }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Bridge Layout Diagnostic — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine($"Scene: `{scene.path}`");
            r.AppendLine();

            // Camera.
            r.AppendLine("## Camera (player rig)");
            Camera cam = Camera.main;
            if (cam != null)
            {
                Transform ct = cam.transform;
                r.AppendLine($"- Name: `{ct.name}`");
                r.AppendLine($"- World position: {Format(ct.position)}");
                r.AppendLine($"- World rotation: {Format(ct.eulerAngles)}");
                r.AppendLine($"- Forward: {Format(ct.forward)}");
            }
            else
            {
                r.AppendLine("- **No Camera.main in scene!**");
            }
            r.AppendLine();

            // BridgeCabin.
            r.AppendLine("## BridgeCabin");
            GameObject cabin = GameObject.Find("BridgeCabin");
            if (cabin != null)
            {
                Transform ct = cabin.transform;
                r.AppendLine($"- World position: {Format(ct.position)}");
                r.AppendLine($"- Local euler:    {Format(ct.localEulerAngles)}");
                r.AppendLine($"- Local scale:    {Format(ct.localScale)}");
                Bounds bb = GetWorldBounds(cabin);
                r.AppendLine($"- Bounds (world): center {Format(bb.center)} size {Format(bb.size)}");
                r.AppendLine($"- Bounds Y range: [{bb.min.y:F2}, {bb.max.y:F2}]");
                r.AppendLine();
                r.AppendLine("### Anchor world positions");
                string[] anchors = { "anchor_AIS4000", "anchor_VHF", "anchor_ECDIS",
                                     "anchor_Radar", "anchor_EOT", "anchor_Compass",
                                     "anchor_ShipWheel" };
                foreach (string a in anchors)
                {
                    Transform t = FindDeep(ct, a);
                    if (t == null) { r.AppendLine($"- {a,-20} **MISSING**"); continue; }
                    r.AppendLine($"- {a,-20} world {Format(t.position)} rot {Format(t.eulerAngles)}");
                }
            }
            else
            {
                r.AppendLine("- **BridgeCabin not in scene.**");
            }
            r.AppendLine();

            // Equipment.
            r.AppendLine("## Equipment");
            DumpEquipment(r, "AIS",       Object.FindFirstObjectByType<AisTransceiver>(),       cam);
            DumpEquipment(r, "VHF",       Object.FindFirstObjectByType<VhfRadio>(),             cam);
            DumpEquipment(r, "ECDIS",     Object.FindFirstObjectByType<ElectronicChartDisplay>(), cam);
            DumpEquipment(r, "Radar",     Object.FindFirstObjectByType<MarineRadar>(),          cam);
            DumpEquipment(r, "EOT",       Object.FindFirstObjectByType<EngineOrderTelegraph>(), cam);
            DumpEquipment(r, "Compass",   Object.FindFirstObjectByType<MagneticCompass>(),      cam);
            DumpEquipment(r, "ShipWheel", Object.FindFirstObjectByType<ShipWheel>(),            cam);
            r.AppendLine();

            // EOT lever pivot specifically (the user reported "không nhìn thấy cần gạt").
            r.AppendLine("## EOT lever pivot");
            EngineOrderTelegraph eot = Object.FindFirstObjectByType<EngineOrderTelegraph>();
            if (eot != null)
            {
                Transform handle = FindDeep(eot.transform, "handle_lever_pivot");
                if (handle != null)
                {
                    r.AppendLine($"- handle_lever_pivot world pos: {Format(handle.position)}");
                    if (cam != null)
                        r.AppendLine($"- distance to camera: {Vector3.Distance(handle.position, cam.transform.position):F2}m");
                    r.AppendLine($"- enabled: {handle.gameObject.activeInHierarchy}");
                    Renderer[] rs = handle.GetComponentsInChildren<Renderer>(true);
                    int activeRenderers = 0;
                    foreach (var ren in rs) if (ren.enabled && ren.gameObject.activeInHierarchy) activeRenderers++;
                    r.AppendLine($"- renderers under pivot: {rs.Length} ({activeRenderers} active)");
                }
                else
                {
                    r.AppendLine("- **handle_lever_pivot not found** under EOT — FBX hierarchy mismatch.");
                    r.AppendLine("- EOT children:");
                    for (int i = 0; i < eot.transform.childCount; i++)
                    {
                        Transform c = eot.transform.GetChild(i);
                        r.AppendLine($"    - `{c.name}` (active={c.gameObject.activeInHierarchy})");
                    }
                }
            }
            r.AppendLine();

            string text = r.ToString();
            try
            {
                File.WriteAllText(ReportPath, text);
                AssetDatabase.ImportAsset(ReportPath);
                Debug.Log($"[Maritime LMS] Layout diagnostic written → {ReportPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Failed to write diagnostic: {ex.Message}");
            }
            Debug.Log(text);
        }

        private static void DumpEquipment(StringBuilder r, string label, Component c, Camera cam)
        {
            if (c == null) { r.AppendLine($"### {label} — NOT IN SCENE"); return; }
            Transform t = c.transform;
            string parentPath = t.parent != null ? t.parent.name : "(root)";
            r.AppendLine($"### {label}");
            r.AppendLine($"- Parent: `{parentPath}`");
            r.AppendLine($"- World position: {Format(t.position)}");
            r.AppendLine($"- World rotation: {Format(t.eulerAngles)}");
            r.AppendLine($"- Local position:  {Format(t.localPosition)}");
            r.AppendLine($"- Local rotation:  {Format(t.localEulerAngles)}");
            r.AppendLine($"- Local scale:     {Format(t.localScale)}");
            r.AppendLine($"- Active in hierarchy: {t.gameObject.activeInHierarchy}");
            Bounds bb = GetWorldBounds(t.gameObject);
            r.AppendLine($"- Bounds size:     {Format(bb.size)}");
            if (cam != null)
            {
                r.AppendLine($"- Distance to camera: {Vector3.Distance(t.position, cam.transform.position):F2}m");
                Vector3 toEq = (t.position - cam.transform.position).normalized;
                float dot = Vector3.Dot(cam.transform.forward, toEq);
                r.AppendLine($"- Camera dot (1=in front): {dot:F2}");
            }
        }

        private static Bounds GetWorldBounds(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeep(root.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        private static string Format(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
    }

    [InitializeOnLoad]
    public static class BridgeLayoutDiagnosticAutoHook
    {
        // v5: bumped after Phase 20 layout restructure so the diagnostic
        // captures the new world positions matching the reference photo.
        private const string SessionStateKey = "MaritimeLMS.LayoutDiagRan.v5";

        static BridgeLayoutDiagnosticAutoHook()
        {
            EditorApplication.delayCall += MaybeRun;
        }

        [MenuItem("Tools/Maritime LMS/Force Layout Diagnostic On Next Compile")]
        public static void Rearm()
        {
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log("[Maritime LMS] Layout diagnostic rearmed.");
        }

        private static void MaybeRun()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += MaybeRun;
                return;
            }

            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.path.EndsWith("MaritimeBridgeLMS.unity"))
            {
                EditorApplication.delayCall += MaybeRun;
                return;
            }
            if (GameObject.Find("BridgeCabin") == null)
            {
                EditorApplication.delayCall += MaybeRun;
                return;
            }

            try { BridgeLayoutDiagnosticEditor.Dump(); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] Layout diagnostic threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
