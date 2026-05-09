#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Sentinel-driven companion to <see cref="BridgeReorganizerAutoHook"/>:
    /// runs the equipment tuner once per session after the scene has been
    /// scaffolded and reorganized. Picks dry-run vs execute based on the
    /// presence of the sentinel file at
    /// <c>Library/MaritimeLMS_AutoExecuteTune.flag</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class BridgeEquipmentTunerAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.TuneRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteTune.flag";

        static BridgeEquipmentTunerAutoHook()
        {
            EditorApplication.delayCall += MaybeRun;
        }

        [MenuItem("Tools/Maritime LMS/Force Tune Report On Next Compile")]
        public static void Rearm()
        {
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log("[Maritime LMS] Tune hook rearmed — next compile/load will write a fresh dry-run report.");
        }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Tune")]
        public static void ArmAutoExecute()
        {
            System.IO.File.WriteAllText(ExecuteSentinelPath, System.DateTime.UtcNow.ToString("o"));
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log($"[Maritime LMS] Auto-execute tune armed — sentinel '{ExecuteSentinelPath}' will trigger Execute on next compile/load.");
        }

        private static void MaybeRun()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += MaybeRun;
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || !active.path.EndsWith("MaritimeBridgeLMS.unity"))
            {
                EditorApplication.delayCall += MaybeRun;
                return;
            }

            // Wait until BridgeCabin is in the scene (i.e. the scaffolder has
            // run) and the equipment has been parented under its anchors
            // (i.e. the reorganizer has run).
            GameObject cabin = GameObject.Find("BridgeCabin");
            if (cabin == null)
            {
                EditorApplication.delayCall += MaybeRun;
                return;
            }
            if (cabin.transform.Find("anchor_Compass") == null
                || cabin.transform.Find("anchor_Compass").childCount == 0)
            {
                // Reorg hasn't moved the equipment yet — give it another tick.
                EditorApplication.delayCall += MaybeRun;
                return;
            }

            bool execute = System.IO.File.Exists(ExecuteSentinelPath);
            try
            {
                Debug.Log($"[Maritime LMS] Equipment tune {(execute ? "EXECUTE" : "dry-run")} starting...");
                BridgeEquipmentTunerEditor.Run(execute: execute);
                if (execute)
                {
                    try { System.IO.File.Delete(ExecuteSentinelPath); }
                    catch (System.Exception ex) { Debug.LogWarning($"[Maritime LMS] Could not delete tune sentinel: {ex.Message}"); }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Equipment tune {(execute ? "EXECUTE" : "dry-run")} threw: {ex.Message}");
            }
            finally
            {
                SessionState.SetBool(SessionStateKey, true);
            }
        }
    }
}
#endif
