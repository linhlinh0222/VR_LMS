#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaritimeLMS.Compass;
using MaritimeLMS.Helm;
using MaritimeLMS.Telegraph;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 35 — drop the camera to a real helmsman eye level + tilt
    /// further down so the helm wheel, engine telegraph and compass
    /// binnacle (all sitting at ~Y=10.50) actually fall inside the
    /// camera frustum. Phase 18's 11.85 / 8° put the helm cluster
    /// 1.14 m below the view centre at 1.5 m distance — outside the
    /// bottom edge of a 65° FOV. Bump helm cluster scale 0.55 → 0.70 so
    /// they read at roughly real-bridge proportion.
    /// </summary>
    public static class HelmViewFixerEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Fix Helm View — Execute";

        // Real helmsman eye height ≈ 1.27 m above the deck plate.
        // Cabin floor at Y=10.23 → camera Y ≈ 11.50.
        private static readonly Vector3 CameraTargetPos = new Vector3(0f, 11.50f, -28.20f);
        private static readonly Vector3 CameraTargetEuler = new Vector3(14f, 0f, 0f);
        private const float HelmClusterScale = 0.70f;

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] HelmView: no scene."); return; }

            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Fix Helm View");
            try
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Undo.RecordObject(cam.transform, "Camera reposition");
                    cam.transform.position = CameraTargetPos;
                    cam.transform.eulerAngles = CameraTargetEuler;
                    EditorUtility.SetDirty(cam.transform);
                }

                BumpScale<ShipWheel>("ShipWheel", HelmClusterScale);
                BumpScale<EngineOrderTelegraph>("EOT", HelmClusterScale);
                BumpScale<MagneticCompass>("Compass", HelmClusterScale);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"<color=cyan>[Maritime LMS]</color> HelmView fix done: camera {CameraTargetPos} rot {CameraTargetEuler}, helm scale {HelmClusterScale}.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] HelmView fix failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undo); }
        }

        private static void BumpScale<T>(string label, float scale) where T : Component
        {
            T comp = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (comp == null) { Debug.Log($"[Maritime LMS] HelmView: {label} not in scene"); return; }
            Undo.RecordObject(comp.transform, $"Scale {label}");
            comp.transform.localScale = Vector3.one * scale;
            EditorUtility.SetDirty(comp.transform);
        }
    }
}
#endif
