#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 21 — final polish so the in-cabin view stays clean: disable
    /// the legacy <c>LMS_UI_Canvas</c> (the world-space lesson UI from
    /// before Phase 12 added <c>MaritimeLessonRoot/LessonCanvas</c>) so it
    /// doesn't overlap the new HUD, and ensure the scene has a default
    /// skybox so the windows show sky rather than the editor's flat clear
    /// colour.
    /// </summary>
    public static class BridgeFinalPolishEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Final Polish — Execute";
        private const string ReportPath = "Assets/Project/Maritime/POLISH_REPORT.md";

        private static readonly string[] LegacyDisablePaths =
        {
            "LMS_UI_Canvas"
        };

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static string Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Polish: no scene."); return null; }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Final Polish — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Final Polish");
            int disableCount = 0;

            r.AppendLine("## Disable legacy UI");
            foreach (string n in LegacyDisablePaths)
            {
                GameObject g = GameObject.Find(n);
                if (g == null) { r.AppendLine($"- `{n}` — not found"); continue; }
                if (!g.activeSelf) { r.AppendLine($"- `{n}` — already inactive"); continue; }
                Undo.RecordObject(g, "Disable legacy UI");
                g.SetActive(false);
                EditorUtility.SetDirty(g);
                disableCount++;
                r.AppendLine($"- `{n}` — set inactive");
            }
            r.AppendLine();

            // Default skybox: if RenderSettings.skybox is null, give the
            // scene Unity's default-skybox so the cabin windows show sky.
            r.AppendLine("## Skybox");
            if (RenderSettings.skybox == null)
            {
                Material defaultSky = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
                if (defaultSky != null)
                {
                    RenderSettings.skybox = defaultSky;
                    r.AppendLine("- Set RenderSettings.skybox = Default-Skybox.mat");
                }
                else
                {
                    r.AppendLine("- (no built-in skybox available)");
                }
            }
            else
            {
                r.AppendLine($"- skybox already set to `{RenderSettings.skybox.name}`");
            }
            r.AppendLine();

            r.AppendLine($"## Summary\n- Disables: **{disableCount}**\n");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            r.AppendLine("**Executed.** Scene saved.");
            Debug.Log($"<color=cyan>[Maritime LMS]</color> Polish: disables={disableCount}.");
            Undo.CollapseUndoOperations(undoGroup);

            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] polish report write failed: {ex.Message}"); }
            Debug.Log(text);
            return text;
        }
    }

    [InitializeOnLoad]
    public static class BridgeFinalPolishAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.PolishRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecutePolish.flag";

        static BridgeFinalPolishAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Final Polish")]
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

            try
            {
                Debug.Log("[Maritime LMS] Polish EXECUTE starting...");
                BridgeFinalPolishEditor.Run();
                try { System.IO.File.Delete(ExecuteSentinelPath); }
                catch (System.Exception ex) { Debug.LogWarning($"sentinel del: {ex.Message}"); }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] polish hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
