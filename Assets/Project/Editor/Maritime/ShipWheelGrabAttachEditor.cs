#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaritimeLMS.Helm;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 30 — attach <see cref="ShipWheelMouseGrab"/> to the ship
    /// wheel GameObject so the player can grab it with LMB. Without
    /// this bridge the wheel was invisible to
    /// <c>DesktopMockVRController.TryGrabBody</c>: it neither implements
    /// <c>DesktopLeverInteractable</c> (sealed class) nor has a
    /// <see cref="Rigidbody"/>, so the raycast skipped it.
    /// </summary>
    public static class ShipWheelGrabAttachEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Attach Ship Wheel Grab — Execute";

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Wheel grab attach: no scene."); return; }

            ShipWheel wheel = Object.FindAnyObjectByType<ShipWheel>(FindObjectsInactive.Include);
            if (wheel == null)
            {
                Debug.LogError("[Maritime LMS] No ShipWheel in scene.");
                return;
            }

            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Attach Ship Wheel Grab");
            try
            {
                ShipWheelMouseGrab existing = wheel.GetComponent<ShipWheelMouseGrab>();
                if (existing == null)
                {
                    Undo.AddComponent<ShipWheelMouseGrab>(wheel.gameObject);
                    Debug.Log("<color=cyan>[Maritime LMS]</color> Added ShipWheelMouseGrab to ShipWheel.");
                }
                else
                {
                    Debug.Log("[Maritime LMS] ShipWheelMouseGrab already present — no change.");
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Wheel grab attach failed: {ex.Message}");
            }
            finally { Undo.CollapseUndoOperations(undo); }
        }
    }

    [InitializeOnLoad]
    public static class ShipWheelGrabAttachAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.WheelGrabAttachRan.v1";

        static ShipWheelGrabAttachAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Force Wheel Grab Attach")]
        public static void Rearm() { SessionState.SetBool(SessionStateKey, false); }

        private static void MaybeRun()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += MaybeRun; return; }
            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.path.EndsWith("MaritimeBridgeLMS.unity")) { EditorApplication.delayCall += MaybeRun; return; }
            ShipWheel wheel = Object.FindAnyObjectByType<ShipWheel>(FindObjectsInactive.Include);
            if (wheel == null) { EditorApplication.delayCall += MaybeRun; return; }
            if (wheel.GetComponent<ShipWheelMouseGrab>() != null)
            {
                SessionState.SetBool(SessionStateKey, true);
                return;
            }

            try { ShipWheelGrabAttachEditor.Run(); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] wheel-grab-attach hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
