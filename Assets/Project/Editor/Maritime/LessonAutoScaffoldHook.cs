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
        private const string SessionStateKey = "MaritimeLMS.AutoScaffoldRan";
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

            if (GameObject.Find(LessonRootName) != null)
            {
                Debug.Log($"[Maritime LMS] '{LessonRootName}' already present in '{active.name}' — skipping auto-scaffold.");
                SessionState.SetBool(SessionStateKey, true);
                return;
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
