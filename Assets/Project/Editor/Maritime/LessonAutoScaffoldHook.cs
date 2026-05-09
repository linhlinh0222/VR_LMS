#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// One-shot editor hook that runs the Phase 12 scaffolder automatically
    /// the next time Unity finishes compiling — but only if the
    /// MaritimeBridgeLMS scene does not yet contain
    /// <c>MaritimeLessonRoot</c>. After scaffolding, it saves the scene and
    /// disables itself for the rest of the editor session, so the user is
    /// not prompted again on subsequent recompiles.
    /// </summary>
    /// <remarks>
    /// Pivoted to this in-editor approach because the in-session MCP cloud
    /// bridge is unreliable; the hook gives a deterministic, source-controlled
    /// way to bring the scene to the runnable Phase 12 state without manual
    /// menu clicks. If you want to disable auto-run, set
    /// <see cref="SessionStateKey"/> to true via the menu, or simply delete
    /// this file.
    /// </remarks>
    [InitializeOnLoad]
    public static class LessonAutoScaffoldHook
    {
        // Bumped to v2 after the v1 run partially scaffolded but failed on
        // EnsureChild creating UI GameObjects without RectTransforms. The new
        // EnsureUIChild repairs the partial state on rerun.
        private const string SessionStateKey = "MaritimeLMS.AutoScaffoldRan.v2";
        private const string ScenePath = "Assets/Project/Scenes/MaritimeBridgeLMS.unity";
        private const string LessonRootName = "MaritimeLessonRoot";

        static LessonAutoScaffoldHook()
        {
            EditorApplication.delayCall += MaybeScaffold;
        }

        [MenuItem("Tools/Maritime LMS/Force Auto-Scaffold On Next Compile")]
        public static void ResetAutoScaffold()
        {
            SessionState.SetBool(SessionStateKey, false);
            Debug.Log("[Maritime LMS] Auto-scaffold rearmed — next compile/load will run the scaffolder.");
        }

        private static void MaybeScaffold()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += MaybeScaffold;
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            bool needsLoad = !active.IsValid()
                || string.IsNullOrEmpty(active.path)
                || !active.path.EndsWith("MaritimeBridgeLMS.unity");

            if (needsLoad)
            {
                if (!System.IO.File.Exists(ScenePath))
                {
                    Debug.LogWarning($"[Maritime LMS] Auto-scaffold skipped — scene '{ScenePath}' not found.");
                    SessionState.SetBool(SessionStateKey, true);
                    return;
                }

                Scene opened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!opened.IsValid())
                {
                    Debug.LogWarning("[Maritime LMS] Auto-scaffold could not open scene.");
                    SessionState.SetBool(SessionStateKey, true);
                    return;
                }

                active = opened;
            }

            // Skip only if the previous scaffold finished cleanly — i.e. the
            // root exists AND its LessonCanvas has a HUD RectTransform child.
            // A partially-scaffolded root from an earlier failed run is treated
            // as 'still needs work' so the new EnsureUIChild repair logic runs.
            GameObject existingRoot = GameObject.Find(LessonRootName);
            if (existingRoot != null)
            {
                Transform canvas = existingRoot.transform.Find("LessonCanvas");
                Transform hud = canvas != null ? canvas.Find("HUD") : null;
                if (hud is RectTransform)
                {
                    Debug.Log($"[Maritime LMS] '{LessonRootName}' already scaffolded cleanly in '{active.name}' — skipping.");
                    SessionState.SetBool(SessionStateKey, true);
                    return;
                }
                Debug.Log($"[Maritime LMS] '{LessonRootName}' present but UI looks broken — re-running scaffolder to repair.");
            }

            try
            {
                Debug.Log("[Maritime LMS] Auto-scaffold starting...");
                LessonScaffolderEditor.Scaffold(showCompletionDialog: false);
                EditorSceneManager.MarkSceneDirty(active);
                EditorSceneManager.SaveScene(active);
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Auto-scaffold complete. Scene saved.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Auto-scaffold failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                SessionState.SetBool(SessionStateKey, true);
            }
        }
    }
}
#endif
