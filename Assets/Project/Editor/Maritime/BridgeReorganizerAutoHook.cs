#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// One-shot hook that runs the bridge reorganizer's dry-run once per
    /// Unity session, after the scene has been auto-scaffolded. Writes the
    /// report to <c>Assets/Project/Maritime/REORG_REPORT.md</c> and the
    /// Console — gives the team a heads-up of what would change before the
    /// destructive Execute pass is invoked.
    /// </summary>
    /// <remarks>
    /// Skips if BridgeCabin is not present in the scene (i.e. the scaffolder
    /// hasn't run yet) so the dry-run runs after, not in parallel with, the
    /// scaffold flow. The
    /// <see cref="BridgeReorganizerEditor.ApprovedToExecute"/> flag can be
    /// flipped from a separate menu to convert the next session's hook from
    /// dry-run to execute, but no auto-execute happens by default.
    /// </remarks>
    [InitializeOnLoad]
    public static class BridgeReorganizerAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.ReorgReportRan.v1";

        static BridgeReorganizerAutoHook()
        {
            EditorApplication.delayCall += MaybeReport;
        }

        [MenuItem("Tools/Maritime LMS/Force Reorganize Report On Next Compile")]
        public static void ResetReorgHook()
        {
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log("[Maritime LMS] Reorganize hook rearmed — next compile/load will write a fresh dry-run report.");
        }

        private static void MaybeReport()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += MaybeReport;
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || !active.path.EndsWith("MaritimeBridgeLMS.unity"))
            {
                // Wait until the scaffolder hook has opened the right scene.
                EditorApplication.delayCall += MaybeReport;
                return;
            }

            if (GameObject.Find("BridgeCabin") == null)
            {
                // Scaffolder hasn't finished yet — try again on the next update.
                EditorApplication.delayCall += MaybeReport;
                return;
            }

            try
            {
                Debug.Log("[Maritime LMS] Reorganize dry-run starting...");
                BridgeReorganizerEditor.Run(execute: false);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Reorganize dry-run threw: {ex.Message}");
            }
            finally
            {
                SessionState.SetBool(SessionStateKey, true);
            }
        }
    }
}
#endif
