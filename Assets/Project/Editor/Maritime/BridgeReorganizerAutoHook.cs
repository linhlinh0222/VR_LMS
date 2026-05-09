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
        // v2: bumped after the v1 hook (without sentinel logic) ran a
        // dry-run and pinned the session flag to true. v2 forces re-arm so
        // the new sentinel-aware code path actually fires.
        private const string SessionStateKey = "MaritimeLMS.ReorgReportRan.v2";
        // Sentinel file: if present, the hook runs Execute() instead of just
        // the dry-run, then deletes the file. Lets a separate process
        // (typically a `git push` of a sentinel commit, or `touch` on the
        // file from outside the editor) trigger the destructive pass without
        // needing focus to click a menu item.
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteReorg.flag";

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

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Reorg")]
        public static void ArmAutoExecute()
        {
            System.IO.File.WriteAllText(ExecuteSentinelPath, System.DateTime.UtcNow.ToString("o"));
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log($"[Maritime LMS] Auto-execute armed — sentinel at '{ExecuteSentinelPath}' will trigger Execute on next compile/load.");
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

            bool executeRequested = System.IO.File.Exists(ExecuteSentinelPath);
            try
            {
                Debug.Log($"[Maritime LMS] Reorganize {(executeRequested ? "EXECUTE" : "dry-run")} starting...");
                BridgeReorganizerEditor.Run(execute: executeRequested);
                if (executeRequested)
                {
                    try { System.IO.File.Delete(ExecuteSentinelPath); }
                    catch (System.Exception ex) { Debug.LogWarning($"[Maritime LMS] Could not delete reorg sentinel: {ex.Message}"); }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Reorganize {(executeRequested ? "EXECUTE" : "dry-run")} threw: {ex.Message}");
            }
            finally
            {
                SessionState.SetBool(SessionStateKey, true);
            }
        }
    }
}
#endif
