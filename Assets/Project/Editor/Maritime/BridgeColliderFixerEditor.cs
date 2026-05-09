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
    /// Phase 23 — fix the two issues the Phase 22 health check flagged:
    /// every equipment GameObject was missing a Collider so the LMB-grab
    /// raycast in <c>DesktopMockVRController</c> couldn't hit anything,
    /// and VHF / ECDIS were positioned outside the camera frustum at the
    /// far edges of the console.
    /// </summary>
    /// <remarks>
    /// Adds a single <c>MeshCollider</c> per <c>MeshFilter</c> child so
    /// the raycast lands on the actual visible geometry (not on a
    /// bounding box that would also include empty space). Pulls VHF and
    /// ECDIS in toward centre so all four console displays sit inside
    /// the 65° FOV at the camera's distance.
    /// </remarks>
    public static class BridgeColliderFixerEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Fix Colliders + Tighten Console — Execute";
        private const string ReportPath = "Assets/Project/Maritime/COLLIDER_FIX_REPORT.md";

        // Tightened world positions for console row so VHF + ECDIS sit
        // inside the 65° FOV at camera distance ~2 m. Phase 20 had them
        // at ±2.10 which projected to viewport.x outside [0,1].
        private static readonly (string label, Vector3 worldPos)[] TightenedPositions =
        {
            ("ECDIS", new Vector3(-1.55f, 11.05f, -25.90f)),
            ("VHF",   new Vector3( 1.55f, 11.05f, -25.90f)),
            ("Radar", new Vector3(-0.55f, 11.05f, -25.80f)),
            ("AIS",   new Vector3( 0.55f, 11.05f, -25.80f))
        };

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static string Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Collider fix: no scene."); return null; }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Collider Fix + Console Tighten — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Collider Fix");

            var allEquip = new (string label, Component comp)[]
            {
                ("AIS",       Object.FindFirstObjectByType<AisTransceiver>()),
                ("VHF",       Object.FindFirstObjectByType<VhfRadio>()),
                ("ECDIS",     Object.FindFirstObjectByType<ElectronicChartDisplay>()),
                ("Radar",     Object.FindFirstObjectByType<MarineRadar>()),
                ("EOT",       Object.FindFirstObjectByType<EngineOrderTelegraph>()),
                ("Compass",   Object.FindFirstObjectByType<MagneticCompass>()),
                ("ShipWheel", Object.FindFirstObjectByType<ShipWheel>())
            };

            r.AppendLine("## 1. MeshColliders");
            int collidersAdded = 0;
            foreach (var (label, c) in allEquip)
            {
                if (c == null) { r.AppendLine($"- {label,-10} NOT IN SCENE"); continue; }
                MeshFilter[] mfs = c.GetComponentsInChildren<MeshFilter>(true);
                int added = 0, skipped = 0;
                foreach (MeshFilter mf in mfs)
                {
                    if (mf == null || mf.sharedMesh == null) continue;
                    GameObject go = mf.gameObject;
                    if (go.GetComponent<Collider>() != null) { skipped++; continue; }
                    MeshCollider mc = Undo.AddComponent<MeshCollider>(go);
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                    added++;
                }
                collidersAdded += added;
                r.AppendLine($"- {label,-10} {mfs.Length} mesh filters → {added} new colliders, {skipped} already had one");
            }
            r.AppendLine();

            r.AppendLine("## 2. Console row tightened");
            int repositionCount = 0;
            foreach (var (label, target) in TightenedPositions)
            {
                Component c = System.Array.Find(allEquip, e => e.label == label).comp;
                if (c == null) { r.AppendLine($"- {label,-10} NOT IN SCENE"); continue; }
                Vector3 cur = c.transform.position;
                if ((cur - target).magnitude < 0.02f) { r.AppendLine($"- {label,-10} already at {Format(cur)}"); continue; }
                Undo.RecordObject(c.transform, $"Tighten {label}");
                c.transform.position = target;
                EditorUtility.SetDirty(c.transform);
                r.AppendLine($"- {label,-10} {Format(cur)} → {Format(target)}");
                repositionCount++;
            }
            r.AppendLine();

            r.AppendLine("## 3. Summary");
            r.AppendLine($"- New mesh colliders: **{collidersAdded}**");
            r.AppendLine($"- Console repositions: **{repositionCount}**");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            r.AppendLine();
            r.AppendLine("**Executed.** Scene saved.");
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"<color=cyan>[Maritime LMS]</color> Collider fix: +{collidersAdded} colliders, {repositionCount} repositions.");

            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] collider report write failed: {ex.Message}"); }
            Debug.Log(text);
            return text;
        }

        private static string Format(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
    }

    [InitializeOnLoad]
    public static class BridgeColliderFixerAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.ColliderFixRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteColliderFix.flag";

        static BridgeColliderFixerAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Collider Fix")]
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
                Debug.Log("[Maritime LMS] Collider-fix EXECUTE starting...");
                BridgeColliderFixerEditor.Run();
                try { System.IO.File.Delete(ExecuteSentinelPath); }
                catch (System.Exception ex) { Debug.LogWarning($"sentinel del: {ex.Message}"); }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] collider-fix hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
