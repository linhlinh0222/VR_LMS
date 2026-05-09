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
        // v3: bumped after Phase 25 integrity check found DebriefView
        // missing; the v2 skip predicate only checked HUD existence which
        // let a half-scaffolded canvas slip through. v3 also requires
        // DebriefPanel + DebriefView before skipping.
        private const string SessionStateKey = "MaritimeLMS.AutoScaffoldRan.v3";
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

            // Skip only when the previous scaffold finished cleanly: root +
            // LessonCanvas + HUD/Briefing/Debrief panels (all RectTransform)
            // and the three view MonoBehaviours present. A partially-built
            // canvas (e.g. DebriefView dropped during a manual scene edit)
            // is treated as 'still needs work' so re-running the scaffolder
            // repairs it via the idempotent EnsureUIChild logic.
            GameObject existingRoot = GameObject.Find(LessonRootName);
            if (existingRoot != null)
            {
                Transform canvas = existingRoot.transform.Find("LessonCanvas");
                Transform hud = canvas != null ? canvas.Find("HUD") : null;
                Transform briefing = canvas != null ? canvas.Find("BriefingPanel") : null;
                Transform debrief = canvas != null ? canvas.Find("DebriefPanel") : null;
                bool allPanelsOk = hud is RectTransform && briefing is RectTransform && debrief is RectTransform;
                bool allViewsOk = allPanelsOk
                    && hud.GetComponent<MaritimeLMS.Lessons.LessonHUDView>() != null
                    && briefing.GetComponent<MaritimeLMS.Lessons.BriefingView>() != null
                    && debrief.GetComponent<MaritimeLMS.Lessons.DebriefView>() != null;
                if (allViewsOk)
                {
                    Debug.Log($"[Maritime LMS] '{LessonRootName}' already scaffolded cleanly in '{active.name}' — skipping.");
                    SessionState.SetBool(SessionStateKey, true);
                    return;
                }
                Debug.Log($"[Maritime LMS] '{LessonRootName}' present but UI looks incomplete (panels={allPanelsOk}, views={allViewsOk}) — re-running scaffolder to repair.");
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
