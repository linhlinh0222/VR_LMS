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
    /// Final orientation pass: rotate the BridgeCabin instance by
    /// <c>(-90, 0, 0)</c> so the Z-up Blender FBX stands upright in Unity,
    /// then reset every parented equipment's local rotation back to
    /// identity so the cabin's parent rotation is the single source of
    /// truth for the up-axis. Re-positions the player camera to chest
    /// height in the middle of the cabin facing the forward console.
    /// </summary>
    /// <remarks>
    /// The Phase 14 tuner applied <c>(-90, 0, 0)</c> per equipment because
    /// the cabin was at identity rotation and each device would otherwise
    /// import on its back. The diagnostic confirmed the cabin itself
    /// (bounds Y = 5 m where the FBX's height is 2.5 m) is the one that
    /// imported sideways. Rotating the cabin fixes every child's up-axis
    /// once, so the per-equipment fix is no longer needed and turns into
    /// a double-rotation that flips them upside down.
    /// </remarks>
    public static class BridgeOrientationFixerEditor
    {
        private const string MenuDryRun = "Tools/Maritime LMS/Fix Bridge Orientation — Dry Run";
        private const string MenuExecute = "Tools/Maritime LMS/Fix Bridge Orientation — Execute";
        private const string ReportPath = "Assets/Project/Maritime/ORIENTATION_REPORT.md";

        private static readonly Vector3 CabinTargetEuler = new Vector3(-90f, 0f, 0f);
        private static readonly Vector3 EquipmentTargetEuler = Vector3.zero;
        // Player chest height inside the cabin: cabin pivot is at world Y
        // 10.23 and the cabin is 2.5 m tall, so the floor sits at y=10.23
        // and the ceiling at y=12.73. 11.6 puts the camera roughly at
        // human eye-level above the helm platform.
        private static readonly Vector3 CameraTargetWorldPos = new Vector3(0f, 11.6f, -25.6f);
        private static readonly Vector3 CameraTargetWorldEuler = new Vector3(0f, 180f, 0f);

        [MenuItem(MenuDryRun)]
        public static void DryRun() => Run(execute: false);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run(execute: true);

        public static string Run(bool execute)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[Maritime LMS] Orientation fix: no active scene.");
                return null;
            }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Bridge Orientation Fix {(execute ? "EXECUTE" : "Dry Run")} — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            // Cabin.
            GameObject cabin = GameObject.Find("BridgeCabin");
            r.AppendLine("## 1. Cabin rotation");
            if (cabin == null)
            {
                r.AppendLine("- **BridgeCabin not found** — abort.");
                FlushReport(r);
                return r.ToString();
            }
            Vector3 cabinCurrent = cabin.transform.localEulerAngles;
            r.AppendLine($"- Current local euler: {Format(cabinCurrent)}");
            r.AppendLine($"- Target local euler:  {Format(CabinTargetEuler)}");
            bool cabinNeedsFix = !ApproxEqual(Normalize(cabinCurrent), CabinTargetEuler);
            r.AppendLine($"- Action: {(cabinNeedsFix ? "rotate" : "already correct")}");
            r.AppendLine();

            // Equipment.
            r.AppendLine("## 2. Equipment local rotation reset");
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
            int equipFixCount = 0;
            foreach (var p in plan)
            {
                if (p.comp == null) { r.AppendLine($"- {p.label,-10} NOT IN SCENE"); continue; }
                Vector3 cur = p.comp.transform.localEulerAngles;
                bool already = ApproxEqual(Normalize(cur), EquipmentTargetEuler);
                if (already) r.AppendLine($"- {p.label,-10} already at identity — no change");
                else { r.AppendLine($"- {p.label,-10} {Format(cur)} → identity"); equipFixCount++; }
            }
            r.AppendLine();

            // Camera.
            r.AppendLine("## 3. Camera reposition");
            Camera cam = Camera.main;
            bool camNeedsFix = false;
            if (cam == null) r.AppendLine("- **No Camera.main**");
            else
            {
                Vector3 cp = cam.transform.position;
                Vector3 ce = cam.transform.eulerAngles;
                r.AppendLine($"- Current world pos: {Format(cp)}  rot {Format(ce)}");
                r.AppendLine($"- Target  world pos: {Format(CameraTargetWorldPos)}  rot {Format(CameraTargetWorldEuler)}");
                camNeedsFix = (cp - CameraTargetWorldPos).magnitude > 0.05f
                    || !ApproxEqual(Normalize(ce), CameraTargetWorldEuler);
                r.AppendLine($"- Action: {(camNeedsFix ? "reposition" : "already correct")}");
            }
            r.AppendLine();

            r.AppendLine("## 4. Summary");
            r.AppendLine($"- Cabin rotation fix: {(cabinNeedsFix ? "yes" : "no")}");
            r.AppendLine($"- Equipment reset count: {equipFixCount}");
            r.AppendLine($"- Camera reposition: {(camNeedsFix ? "yes" : "no")}");
            if (!execute)
            {
                r.AppendLine();
                r.AppendLine($"Dry-run only. To apply: `{MenuExecute}`");
                FlushReport(r);
                return r.ToString();
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Fix Bridge Orientation");
            try
            {
                if (cabinNeedsFix)
                {
                    Undo.RecordObject(cabin.transform, "Rotate cabin");
                    cabin.transform.localEulerAngles = CabinTargetEuler;
                    EditorUtility.SetDirty(cabin.transform);
                }

                foreach (var p in plan)
                {
                    if (p.comp == null) continue;
                    Vector3 cur = Normalize(p.comp.transform.localEulerAngles);
                    if (ApproxEqual(cur, EquipmentTargetEuler)) continue;
                    Undo.RecordObject(p.comp.transform, $"Reset {p.label} rotation");
                    p.comp.transform.localEulerAngles = EquipmentTargetEuler;
                    EditorUtility.SetDirty(p.comp.transform);
                }

                if (camNeedsFix && cam != null)
                {
                    Undo.RecordObject(cam.transform, "Reposition camera");
                    cam.transform.position = CameraTargetWorldPos;
                    cam.transform.eulerAngles = CameraTargetWorldEuler;
                    EditorUtility.SetDirty(cam.transform);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                r.AppendLine();
                r.AppendLine("**Executed.** Scene saved.");
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Orientation fix complete. Cabin={cabinNeedsFix}, equipment={equipFixCount}, camera={camNeedsFix}.");
            }
            catch (System.Exception ex)
            {
                r.AppendLine($"**FAILED**: {ex.Message}");
                Debug.LogError($"[Maritime LMS] Orientation fix failed: {ex}");
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            FlushReport(r);
            return r.ToString();
        }

        private static Vector3 Normalize(Vector3 e) => new Vector3(NormA(e.x), NormA(e.y), NormA(e.z));
        private static float NormA(float a) { float n = a % 360f; if (n > 180f) n -= 360f; if (n <= -180f) n += 360f; return n; }
        private static bool ApproxEqual(Vector3 a, Vector3 b)
            => Mathf.Abs(NormA(a.x) - NormA(b.x)) < 0.5f
            && Mathf.Abs(NormA(a.y) - NormA(b.y)) < 0.5f
            && Mathf.Abs(NormA(a.z) - NormA(b.z)) < 0.5f;
        private static string Format(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

        private static void FlushReport(StringBuilder r)
        {
            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] orientation report write failed: {ex.Message}"); }
            Debug.Log(text);
        }
    }

    [InitializeOnLoad]
    public static class BridgeOrientationFixerAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.OrientationRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteOrientation.flag";

        static BridgeOrientationFixerAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Orientation Fix")]
        public static void ArmAutoExecute()
        {
            System.IO.File.WriteAllText(ExecuteSentinelPath, System.DateTime.UtcNow.ToString("o"));
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log($"[Maritime LMS] Orientation auto-execute armed via sentinel '{ExecuteSentinelPath}'.");
        }

        private static void MaybeRun()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += MaybeRun; return; }
            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.path.EndsWith("MaritimeBridgeLMS.unity"))
            { EditorApplication.delayCall += MaybeRun; return; }
            if (GameObject.Find("BridgeCabin") == null)
            { EditorApplication.delayCall += MaybeRun; return; }

            bool execute = System.IO.File.Exists(ExecuteSentinelPath);
            try
            {
                Debug.Log($"[Maritime LMS] Orientation {(execute ? "EXECUTE" : "dry-run")} starting...");
                BridgeOrientationFixerEditor.Run(execute: execute);
                if (execute)
                {
                    try { System.IO.File.Delete(ExecuteSentinelPath); }
                    catch (System.Exception ex) { Debug.LogWarning($"[Maritime LMS] sentinel delete failed: {ex.Message}"); }
                }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] orientation hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
