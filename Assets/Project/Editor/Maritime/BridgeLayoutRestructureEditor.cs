#if UNITY_EDITOR
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
    /// Phase 20 — restructure the bridge layout to match the reference
    /// photo of a modern integrated bridge: ship wheel pedestal on the
    /// player's left, engine order telegraph centred in front of the
    /// helmsman, compass binnacle overhead between the EOT and the
    /// windows, and four wide displays (ECDIS, Radar, AIS, VHF)
    /// spread along the forward console L→R.
    /// </summary>
    /// <remarks>
    /// World-space positions are chosen relative to the camera at
    /// <c>(0, 11.95, -28)</c> looking forward at +Z with 10° pitch
    /// down (Phase 18). Rotation comes from the cabin parent, no
    /// per-equipment rotation needed. DebugMarkers from Phase 19 are
    /// hidden so the cabin matches the cleaner reference look.
    /// </remarks>
    public static class BridgeLayoutRestructureEditor
    {
        private const string MenuDryRun = "Tools/Maritime LMS/Restructure Layout — Dry Run";
        private const string MenuExecute = "Tools/Maritime LMS/Restructure Layout — Execute";
        private const string ReportPath = "Assets/Project/Maritime/LAYOUT_RESTRUCTURE_REPORT.md";

        // Reference photo layout, world coordinates.
        // X axis: player left negative, right positive.
        // Y axis: world up; cabin floor sits at Y=10.23, ceiling Y=13.22.
        // Z axis: player at Z=-28 looking toward +Z; front wall at
        // approximately Z=-25.5, helm row at Z≈-27, console row Z≈-26.
        private static readonly (string label, Vector3 worldPos)[] LayoutTargets =
        {
            // Helm cluster
            ("ShipWheel", new Vector3(-0.65f, 10.50f, -27.00f)), // left pedestal
            ("EOT",       new Vector3( 0.00f, 10.50f, -26.70f)), // centre pedestal, slightly forward of wheel
            ("Compass",   new Vector3( 0.00f, 11.55f, -25.95f)), // overhead binnacle between EOT and windows
            // Forward console row, L→R
            ("ECDIS",     new Vector3(-2.10f, 11.05f, -26.00f)),
            ("Radar",     new Vector3(-1.05f, 11.05f, -25.80f)),
            ("AIS",       new Vector3( 1.05f, 11.05f, -25.80f)),
            ("VHF",       new Vector3( 2.10f, 11.05f, -26.00f))
        };

        [MenuItem(MenuDryRun)]
        public static void DryRun() => Run(execute: false);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run(execute: true);

        public static string Run(bool execute)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Layout restructure: no scene."); return null; }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Layout Restructure {(execute ? "EXECUTE" : "Dry Run")} — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            r.AppendLine("## 1. Equipment world positions");
            int repositionCount = 0;
            foreach (var (label, target) in LayoutTargets)
            {
                Transform t = ResolveEquipment(label);
                if (t == null) { r.AppendLine($"- {label,-10} NOT IN SCENE"); continue; }
                Vector3 cur = t.position;
                bool already = (cur - target).magnitude < 0.02f;
                if (already) r.AppendLine($"- {label,-10} already at {Format(cur)}");
                else { r.AppendLine($"- {label,-10} {Format(cur)} → {Format(target)}"); repositionCount++; }
            }
            r.AppendLine();

            r.AppendLine("## 2. DebugMarkers visibility");
            GameObject markers = GameObject.Find("BridgeCabin/DebugMarkers");
            bool markersWillHide = markers != null && markers.activeSelf;
            r.AppendLine(markers == null ? "- not present" : (markersWillHide ? "- set inactive" : "- already inactive"));
            r.AppendLine();

            r.AppendLine("## 3. Summary");
            r.AppendLine($"- Reposition ops: **{repositionCount}**");
            r.AppendLine($"- Hide debug markers: **{markersWillHide}**");
            if (!execute) { r.AppendLine($"\nDry-run only. To apply: `{MenuExecute}`"); FlushReport(r); return r.ToString(); }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Restructure Bridge Layout");
            try
            {
                foreach (var (label, target) in LayoutTargets)
                {
                    Transform t = ResolveEquipment(label);
                    if (t == null) continue;
                    Undo.RecordObject(t, $"Reposition {label}");
                    t.position = target;
                    EditorUtility.SetDirty(t);
                }
                if (markersWillHide && markers != null)
                {
                    Undo.RecordObject(markers, "Hide debug markers");
                    markers.SetActive(false);
                    EditorUtility.SetDirty(markers);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                r.AppendLine();
                r.AppendLine("**Executed.** Scene saved.");
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Layout restructure done: {repositionCount} repositions, markers hidden={markersWillHide}.");
            }
            catch (System.Exception ex)
            {
                r.AppendLine($"**FAILED**: {ex.Message}");
                Debug.LogError($"[Maritime LMS] Layout restructure failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undoGroup); }

            FlushReport(r);
            return r.ToString();
        }

        private static Transform ResolveEquipment(string label)
        {
            Component c = label switch
            {
                "AIS"       => Object.FindFirstObjectByType<AisTransceiver>(),
                "VHF"       => Object.FindFirstObjectByType<VhfRadio>(),
                "ECDIS"     => Object.FindFirstObjectByType<ElectronicChartDisplay>(),
                "Radar"     => Object.FindFirstObjectByType<MarineRadar>(),
                "EOT"       => Object.FindFirstObjectByType<EngineOrderTelegraph>(),
                "Compass"   => Object.FindFirstObjectByType<MagneticCompass>(),
                "ShipWheel" => Object.FindFirstObjectByType<ShipWheel>(),
                _ => null
            };
            return c != null ? c.transform : null;
        }

        private static string Format(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

        private static void FlushReport(StringBuilder r)
        {
            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] restructure report write failed: {ex.Message}"); }
            Debug.Log(text);
        }
    }

    [InitializeOnLoad]
    public static class BridgeLayoutRestructureAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.RestructureRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteRestructure.flag";

        static BridgeLayoutRestructureAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Restructure")]
        public static void ArmAutoExecute()
        {
            System.IO.File.WriteAllText(ExecuteSentinelPath, System.DateTime.UtcNow.ToString("o"));
            SessionState.SetBool(SessionStateKey, false);
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
                Debug.Log($"[Maritime LMS] Restructure {(execute ? "EXECUTE" : "dry-run")} starting...");
                BridgeLayoutRestructureEditor.Run(execute: execute);
                if (execute) { try { System.IO.File.Delete(ExecuteSentinelPath); } catch (System.Exception ex) { Debug.LogWarning($"sentinel del: {ex.Message}"); } }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] restructure hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
