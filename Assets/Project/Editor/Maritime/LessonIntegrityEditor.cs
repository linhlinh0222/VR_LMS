#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaritimeLMS.Lessons;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 25 — verify the LessonStateMachine in the scene is wired up
    /// the way Phase 12 designed it: 5 phases in canonical order, each
    /// non-modal phase has at least one objective, each objective points
    /// at the correct equipment in the scene. Catches the common case
    /// where the scaffolder ran but a manual edit broke a reference.
    /// </summary>
    public static class LessonIntegrityEditor
    {
        private const string MenuRun = "Tools/Maritime LMS/Lesson Integrity Check";
        private const string ReportPath = "Assets/Project/Maritime/LESSON_INTEGRITY.md";

        [MenuItem(MenuRun)]
        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Lesson integrity: no scene."); return; }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Lesson Integrity — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            int issues = 0;
            LessonStateMachine sm = Object.FindFirstObjectByType<LessonStateMachine>();
            if (sm == null)
            {
                r.AppendLine("**FAIL**: No LessonStateMachine in scene.");
                Flush(r);
                return;
            }

            r.AppendLine($"## State machine\n- on `{GetPath(sm.transform)}`\n- title: `{sm.LessonTitle}`\n- phases configured: {sm.Phases.Count}");
            r.AppendLine();

            // Expected canonical phase order.
            LessonPhase[] expected = {
                LessonPhase.Briefing, LessonPhase.Familiarization, LessonPhase.Guided,
                LessonPhase.Assessment, LessonPhase.Debrief
            };
            if (sm.Phases.Count != expected.Length)
            {
                r.AppendLine($"⚠️ phase count {sm.Phases.Count} ≠ expected {expected.Length}");
                issues++;
            }

            r.AppendLine("## Phases");
            for (int i = 0; i < sm.Phases.Count; i++)
            {
                LessonPhaseGroup p = sm.Phases[i];
                if (p == null) { r.AppendLine($"- [{i}] **NULL**"); issues++; continue; }
                bool phaseMatchesExpected = i < expected.Length && p.phase == expected[i];
                string phaseTag = phaseMatchesExpected ? "✓" : "⚠️";
                r.AppendLine($"- [{i}] {phaseTag} {p.phase} | title `{p.title}` | manualAdvance={p.requiresManualAdvance} | objectives: {p.objectives?.Length ?? 0}");
                if (!phaseMatchesExpected && i < expected.Length)
                {
                    r.AppendLine($"     expected {expected[i]}");
                    issues++;
                }
                if (!p.requiresManualAdvance && (p.objectives == null || p.objectives.Length == 0))
                {
                    r.AppendLine($"     ⚠️ non-modal phase has no objectives — would auto-advance instantly");
                    issues++;
                }
                if (p.objectives != null)
                {
                    foreach (LessonObjectiveBase obj in p.objectives)
                    {
                        if (obj == null) { r.AppendLine($"     - **NULL objective**"); issues++; continue; }
                        r.AppendLine($"     - {obj.GetType().Name} `{obj.Description}`");
                        // Per-type integrity probe via reflection on serialized fields.
                        issues += ProbeObjective(obj, r);
                    }
                }
            }
            r.AppendLine();

            // Wrong-hand tracker.
            WrongHandPenaltyTracker tracker = Object.FindFirstObjectByType<WrongHandPenaltyTracker>();
            r.AppendLine($"## WrongHandPenaltyTracker\n- {(tracker != null ? "present" : "**MISSING**")}");
            if (tracker == null) issues++;
            r.AppendLine();

            // Canvas + views.
            r.AppendLine("## UI views");
            var hud = Object.FindFirstObjectByType<LessonHUDView>();
            var brief = Object.FindFirstObjectByType<BriefingView>();
            var deb = Object.FindFirstObjectByType<DebriefView>();
            r.AppendLine($"- LessonHUDView: {(hud != null ? "present" : "**MISSING**")}");
            r.AppendLine($"- BriefingView:  {(brief != null ? "present" : "**MISSING**")}");
            r.AppendLine($"- DebriefView:   {(deb != null ? "present" : "**MISSING**")}");
            if (hud == null) issues++;
            if (brief == null) issues++;
            if (deb == null) issues++;
            r.AppendLine();

            r.AppendLine($"## Total issues: **{issues}**");
            Flush(r);
        }

        private static int ProbeObjective(LessonObjectiveBase obj, StringBuilder r)
        {
            int issues = 0;
            using SerializedObject so = new SerializedObject(obj);
            // Common required Component-reference fields by objective type.
            string[] required = obj switch
            {
                AisPowerObjective => new[] { "ais" },
                VhfPowerObjective => new[] { "vhf" },
                EcdisPowerObjective => new[] { "ecdis" },
                RadarTransmitObjective => new[] { "radar" },
                TelegraphPositionObjective => new[] { "telegraph" },
                HelmHeadingObjective => new[] { "compass" },
                _ => System.Array.Empty<string>()
            };
            foreach (string fld in required)
            {
                SerializedProperty p = so.FindProperty(fld);
                if (p == null) continue;
                if (p.objectReferenceValue == null)
                {
                    r.AppendLine($"        ⚠️ field `{fld}` is null");
                    issues++;
                }
            }
            return issues;
        }

        private static void Flush(StringBuilder r)
        {
            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] integrity report write failed: {ex.Message}"); }
            Debug.Log(text);
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "(null)";
            string path = t.name;
            Transform p = t.parent;
            while (p != null) { path = p.name + "/" + path; p = p.parent; }
            return path;
        }
    }

    [InitializeOnLoad]
    public static class LessonIntegrityAutoHook
    {
        // v2: re-run after Phase 26 scaffolder fix.
        private const string SessionStateKey = "MaritimeLMS.LessonIntegrityRan.v2";

        static LessonIntegrityAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Force Lesson Integrity Check")]
        public static void Rearm() { SessionState.SetBool(SessionStateKey, false); }

        private static void MaybeRun()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += MaybeRun; return; }
            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.path.EndsWith("MaritimeBridgeLMS.unity")) { EditorApplication.delayCall += MaybeRun; return; }
            if (Object.FindFirstObjectByType<LessonStateMachine>() == null) { EditorApplication.delayCall += MaybeRun; return; }

            try { LessonIntegrityEditor.Run(); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] integrity hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
