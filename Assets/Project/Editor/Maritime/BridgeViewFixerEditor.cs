#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Final view-correction pass after Phase 16 orientation fix:
    /// disables the legacy <c>Maritime Training Station</c> +
    /// <c>Generated Station Geometry</c> placeholder cabin (it was kept on
    /// purpose during reorg but its forward console is now occluding the
    /// new BridgeCabin and showing the old red/orange/green status lamps
    /// the player reported), and rotates the player camera to face the new
    /// cabin's interior — Phase 16 had it pointing the wrong way around.
    /// </summary>
    /// <remarks>
    /// After Phase 16's cabin rotation <c>(-90, 0, 0)</c>, the cabin's
    /// front wall sits at MORE POSITIVE Z relative to the cabin pivot
    /// (FBX <c>-Y</c> mapped to Unity <c>+Z</c>). Equipment world Z lies
    /// between the camera and the windows, so the camera needs to face
    /// <c>+Z</c> (default Unity forward) to see them, not the
    /// <c>Y=180</c> orientation Phase 16 assumed.
    /// </remarks>
    public static class BridgeViewFixerEditor
    {
        private const string MenuDryRun = "Tools/Maritime LMS/Fix Bridge View — Dry Run";
        private const string MenuExecute = "Tools/Maritime LMS/Fix Bridge View — Execute";
        private const string ReportPath = "Assets/Project/Maritime/VIEW_FIX_REPORT.md";

        private static readonly Vector3 CameraTargetWorldPos = new Vector3(0f, 11.4f, -27.5f);
        private static readonly Vector3 CameraTargetWorldEuler = new Vector3(0f, 0f, 0f);

        private static readonly string[] LegacyDisablePaths =
        {
            "Ship/Bridge_Structure/Maritime Training Station",
            "Ship/Bridge_Structure/Maritime Training Station/Generated Station Geometry"
        };

        [MenuItem(MenuDryRun)]
        public static void DryRun() => Run(execute: false);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run(execute: true);

        public static string Run(bool execute)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[Maritime LMS] View fix: no active scene.");
                return null;
            }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Bridge View Fix {(execute ? "EXECUTE" : "Dry Run")} — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            r.AppendLine("## 1. Camera reposition + reorient");
            Camera cam = Camera.main;
            bool camNeedsFix = false;
            if (cam == null)
            {
                r.AppendLine("- **No Camera.main**");
            }
            else
            {
                Vector3 cp = cam.transform.position;
                Vector3 ce = cam.transform.eulerAngles;
                r.AppendLine($"- Current world pos: {Format(cp)}  rot {Format(ce)}");
                r.AppendLine($"- Target  world pos: {Format(CameraTargetWorldPos)}  rot {Format(CameraTargetWorldEuler)}");
                camNeedsFix = (cp - CameraTargetWorldPos).magnitude > 0.05f
                    || !ApproxEqual(Normalize(ce), CameraTargetWorldEuler);
                r.AppendLine($"- Action: {(camNeedsFix ? "reposition + rotate" : "already correct")}");
            }
            r.AppendLine();

            r.AppendLine("## 2. Legacy cabin disable");
            var disableList = new System.Collections.Generic.List<GameObject>();
            foreach (string path in LegacyDisablePaths)
            {
                GameObject g = ResolvePath(scene, path);
                if (g == null) { r.AppendLine($"- `{path}` — not found"); continue; }
                if (!g.activeSelf) { r.AppendLine($"- `{path}` — already inactive"); continue; }
                disableList.Add(g);
                r.AppendLine($"- `{path}` — set inactive");
            }
            r.AppendLine();

            r.AppendLine("## 3. Summary");
            r.AppendLine($"- Camera fix: {(camNeedsFix ? "yes" : "no")}");
            r.AppendLine($"- Legacy disables: {disableList.Count}");
            if (!execute)
            {
                r.AppendLine();
                r.AppendLine($"Dry-run only. To apply: `{MenuExecute}`");
                FlushReport(r);
                return r.ToString();
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Fix Bridge View");
            try
            {
                if (camNeedsFix && cam != null)
                {
                    Undo.RecordObject(cam.transform, "Reposition camera");
                    cam.transform.position = CameraTargetWorldPos;
                    cam.transform.eulerAngles = CameraTargetWorldEuler;
                    EditorUtility.SetDirty(cam.transform);
                }

                foreach (GameObject g in disableList)
                {
                    Undo.RecordObject(g, "Disable legacy cabin");
                    g.SetActive(false);
                    EditorUtility.SetDirty(g);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                r.AppendLine();
                r.AppendLine("**Executed.** Scene saved.");
                Debug.Log($"<color=cyan>[Maritime LMS]</color> View fix complete: cam={camNeedsFix}, legacy disabled={disableList.Count}.");
            }
            catch (System.Exception ex)
            {
                r.AppendLine($"**FAILED**: {ex.Message}");
                Debug.LogError($"[Maritime LMS] View fix failed: {ex}");
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            FlushReport(r);
            return r.ToString();
        }

        private static GameObject ResolvePath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                Transform cur = root.transform;
                bool ok = true;
                for (int i = 1; i < parts.Length; i++)
                {
                    Transform next = cur.Find(parts[i]);
                    if (next == null) { ok = false; break; }
                    cur = next;
                }
                if (ok) return cur.gameObject;
            }
            return null;
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
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] view report write failed: {ex.Message}"); }
            Debug.Log(text);
        }
    }

    [InitializeOnLoad]
    public static class BridgeViewFixerAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.ViewFixRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteViewFix.flag";

        static BridgeViewFixerAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute View Fix")]
        public static void ArmAutoExecute()
        {
            System.IO.File.WriteAllText(ExecuteSentinelPath, System.DateTime.UtcNow.ToString("o"));
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log($"[Maritime LMS] View-fix auto-execute armed — sentinel '{ExecuteSentinelPath}'.");
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
                Debug.Log($"[Maritime LMS] View-fix {(execute ? "EXECUTE" : "dry-run")} starting...");
                BridgeViewFixerEditor.Run(execute: execute);
                if (execute)
                {
                    try { System.IO.File.Delete(ExecuteSentinelPath); }
                    catch (System.Exception ex) { Debug.LogWarning($"[Maritime LMS] sentinel delete failed: {ex.Message}"); }
                }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] view-fix hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
