#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaritimeLMS.Helm;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 32 — robust unified auto-applier. The earlier per-phase
    /// <c>[InitializeOnLoad]</c> + <c>EditorApplication.delayCall</c>
    /// pattern stopped firing in this Unity install for reasons that
    /// don't show up in Editor.log, so this fallback uses
    /// <c>EditorApplication.update</c> polling (which fires reliably
    /// every editor frame) and runs every pending fix in one place.
    /// </summary>
    /// <remarks>
    /// Runs after a settle delay (60 frames ≈ 1 s) and once compilation +
    /// asset import are quiet. Each fix is gated by a SessionState flag
    /// so the apply pass is idempotent within a session, and rearmed by
    /// the per-phase Force menus or a Unity restart.
    /// </remarks>
    [InitializeOnLoad]
    public static class BridgeAutoApplyAllEditor
    {
        private const string SessionStateKey = "MaritimeLMS.AutoApplyAll.v1";
        private const int SettleFrames = 60;

        private static int _frameCount;

        static BridgeAutoApplyAllEditor()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Maritime LMS/Force Apply All Pending Fixes")]
        public static void ForceApply()
        {
            SessionState.SetBool(SessionStateKey, false);
            _frameCount = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Maritime LMS] AutoApplyAll rearmed.");
        }

        private static void Tick()
        {
            if (SessionState.GetBool(SessionStateKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                _frameCount = 0;
                return;
            }

            // Wait for the editor to settle before touching the scene so
            // the auto-scaffold + reorganizer have already run.
            if (++_frameCount < SettleFrames) return;

            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.path.EndsWith("MaritimeBridgeLMS.unity"))
            {
                _frameCount = 0;
                return;
            }
            if (GameObject.Find("BridgeCabin") == null)
            {
                _frameCount = 0;
                return;
            }

            try
            {
                Debug.Log("[Maritime LMS] AutoApplyAll starting...");
                ApplyShipWheelGrab();
                ApplyCabinDecoration();
                Debug.Log("[Maritime LMS] AutoApplyAll complete.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Maritime LMS] AutoApplyAll threw: {ex}");
            }
            finally
            {
                SessionState.SetBool(SessionStateKey, true);
                EditorApplication.update -= Tick;
            }
        }

        private static void ApplyShipWheelGrab()
        {
            ShipWheel wheel = UnityEngine.Object.FindAnyObjectByType<ShipWheel>(FindObjectsInactive.Include);
            if (wheel == null) { Debug.Log("[Maritime LMS] AutoApplyAll: no ShipWheel"); return; }
            if (wheel.GetComponent<ShipWheelMouseGrab>() == null)
            {
                Undo.AddComponent<ShipWheelMouseGrab>(wheel.gameObject);
                EditorUtility.SetDirty(wheel.gameObject);
                Debug.Log("[Maritime LMS] AutoApplyAll: added ShipWheelMouseGrab");
            }
        }

        private static void ApplyCabinDecoration()
        {
            try
            {
                CabinDecorationEditor.Run();
                Debug.Log("[Maritime LMS] AutoApplyAll: cabin decoration built");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Maritime LMS] AutoApplyAll: decoration build failed: {ex}");
            }

            Scene s = SceneManager.GetActiveScene();
            if (s.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
            }
        }
    }
}
#endif
