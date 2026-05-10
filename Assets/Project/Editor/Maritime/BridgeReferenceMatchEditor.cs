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
    /// Phase 29 — refine the cabin composition to match the user's
    /// reference photo of an integrated bridge: helm wheel and engine
    /// order telegraph clustered together at the centre, the compass
    /// binnacle directly behind the EOT, and the four console displays
    /// split into a left cluster (ECDIS / Radar) and a right cluster
    /// (AIS / VHF) flanking the helm. Bumps the EOT scale a little so
    /// its lever + speed-disc dial reads at the same dramatic size as
    /// the photo, and pulls the camera half a metre back so the helm
    /// cluster has room without smashing the near-clip plane.
    /// </summary>
    public static class BridgeReferenceMatchEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Reference Match — Execute";
        private const string ReportPath = "Assets/Project/Maritime/REFERENCE_MATCH_REPORT.md";

        // World-space targets, tuned for camera at (0, 11.85, -28.20).
        private static readonly (string label, Vector3 worldPos, float scale)[] LayoutTargets =
        {
            // Helm cluster: tighter, closer to player
            ("ShipWheel", new Vector3(-0.50f, 10.50f, -27.00f), 0.55f),
            ("EOT",       new Vector3( 0.00f, 10.50f, -26.85f), 0.65f), // slightly bigger for prominence
            ("Compass",   new Vector3( 0.00f, 11.50f, -26.20f), 0.55f),
            // Forward console split into two clusters
            ("ECDIS",     new Vector3(-2.00f, 11.10f, -26.10f), 1.00f),
            ("Radar",     new Vector3(-1.10f, 11.05f, -26.00f), 1.00f),
            ("AIS",       new Vector3( 1.10f, 11.05f, -26.00f), 1.00f),
            ("VHF",       new Vector3( 2.00f, 11.10f, -26.10f), 1.00f)
        };

        private static readonly Vector3 CameraTargetPos = new Vector3(0f, 11.85f, -28.20f);
        private static readonly Vector3 CameraTargetEuler = new Vector3(8f, 0f, 0f);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Reference match: no scene."); return; }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Reference Match — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Reference Match");
            int repos = 0;
            int rescaled = 0;

            try
            {
                foreach (var (label, target, scale) in LayoutTargets)
                {
                    Transform t = ResolveEquipment(label);
                    if (t == null) { r.AppendLine($"- {label,-10} NOT IN SCENE"); continue; }
                    Vector3 cur = t.position;
                    if ((cur - target).magnitude > 0.02f)
                    {
                        Undo.RecordObject(t, $"Reposition {label}");
                        t.position = target;
                        EditorUtility.SetDirty(t);
                        r.AppendLine($"- {label,-10} pos {Format(cur)} → {Format(target)}");
                        repos++;
                    }
                    Vector3 curScale = t.localScale;
                    Vector3 targetScale = Vector3.one * scale;
                    if ((curScale - targetScale).magnitude > 0.01f)
                    {
                        Undo.RecordObject(t, $"Rescale {label}");
                        t.localScale = targetScale;
                        EditorUtility.SetDirty(t);
                        r.AppendLine($"- {label,-10} scale {Format(curScale)} → {Format(targetScale)}");
                        rescaled++;
                    }
                }

                Camera cam = Camera.main;
                if (cam != null)
                {
                    bool camChanged = false;
                    if ((cam.transform.position - CameraTargetPos).magnitude > 0.05f)
                    {
                        Undo.RecordObject(cam.transform, "Reposition camera");
                        cam.transform.position = CameraTargetPos;
                        camChanged = true;
                    }
                    if (Vector3.Distance(cam.transform.eulerAngles, CameraTargetEuler) > 0.5f)
                    {
                        Undo.RecordObject(cam.transform, "Rotate camera");
                        cam.transform.eulerAngles = CameraTargetEuler;
                        camChanged = true;
                    }
                    if (camChanged)
                    {
                        EditorUtility.SetDirty(cam.transform);
                        r.AppendLine($"- Camera → pos {Format(CameraTargetPos)}, rot {Format(CameraTargetEuler)}");
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                r.AppendLine();
                r.AppendLine($"**Executed.** {repos} repositions, {rescaled} rescales. Scene saved.");
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Reference match done: {repos} repos, {rescaled} rescales.");
            }
            catch (System.Exception ex)
            {
                r.AppendLine($"**FAILED**: {ex.Message}");
                Debug.LogError($"[Maritime LMS] Reference match failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undoGroup); }

            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] reference-match report write failed: {ex.Message}"); }
            Debug.Log(text);
        }

        private static Transform ResolveEquipment(string label) => (label switch
        {
            "AIS"       => (Component)Object.FindAnyObjectByType<AisTransceiver>(FindObjectsInactive.Include),
            "VHF"       => Object.FindAnyObjectByType<VhfRadio>(FindObjectsInactive.Include),
            "ECDIS"     => Object.FindAnyObjectByType<ElectronicChartDisplay>(FindObjectsInactive.Include),
            "Radar"     => Object.FindAnyObjectByType<MarineRadar>(FindObjectsInactive.Include),
            "EOT"       => Object.FindAnyObjectByType<EngineOrderTelegraph>(FindObjectsInactive.Include),
            "Compass"   => Object.FindAnyObjectByType<MagneticCompass>(FindObjectsInactive.Include),
            "ShipWheel" => Object.FindAnyObjectByType<ShipWheel>(FindObjectsInactive.Include),
            _ => null
        })?.transform;

        private static string Format(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
    }

    [InitializeOnLoad]
    public static class BridgeReferenceMatchAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.ReferenceMatchRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteReferenceMatch.flag";

        static BridgeReferenceMatchAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Reference Match")]
        public static void Arm()
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
            if (!System.IO.File.Exists(ExecuteSentinelPath)) { SessionState.SetBool(SessionStateKey, true); return; }
            if (GameObject.Find("BridgeCabin") == null) { EditorApplication.delayCall += MaybeRun; return; }

            try
            {
                Debug.Log("[Maritime LMS] Reference-match EXECUTE starting...");
                BridgeReferenceMatchEditor.Run();
                try { System.IO.File.Delete(ExecuteSentinelPath); }
                catch (System.Exception ex) { Debug.LogWarning($"sentinel del: {ex.Message}"); }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] reference-match hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
