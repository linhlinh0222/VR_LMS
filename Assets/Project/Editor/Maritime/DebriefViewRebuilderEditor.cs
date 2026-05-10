#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MaritimeLMS.Lessons;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 27 — focused, idempotent rebuild of <c>DebriefPanel</c> + its
    /// <c>DebriefView</c> component. The integrity check has been flagging
    /// DebriefView missing because the auto-scaffold skip predicate let a
    /// half-built canvas through; instead of relying on the full scaffolder
    /// to re-fire, this script just rebuilds the one panel.
    /// </summary>
    public static class DebriefViewRebuilderEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Rebuild DebriefView — Execute";

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] DebriefView rebuild: no scene."); return; }

            LessonStateMachine sm = Object.FindFirstObjectByType<LessonStateMachine>();
            if (sm == null) { Debug.LogError("[Maritime LMS] No LessonStateMachine in scene."); return; }

            Transform canvas = GameObject.Find("MaritimeLessonRoot/LessonCanvas")?.transform;
            if (canvas == null)
            {
                Debug.LogError("[Maritime LMS] LessonCanvas not found.");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild DebriefView");
            try
            {
                GameObject panel = EnsureUIChild(canvas, "DebriefPanel");
                panel.SetActive(false); // shown only when state machine reaches Complete
                RectTransform rt = (RectTransform)panel.transform;
                rt.anchorMin = new Vector2(0.20f, 0.20f);
                rt.anchorMax = new Vector2(0.80f, 0.80f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                EnsureBackground(panel, new Color(0.10f, 0.05f, 0.18f, 0.92f));

                TMP_Text title = EnsureChildText(rt, "TitleText", "Debrief", 40f, FontStyles.Bold,
                    new Vector2(0f, 0.80f), new Vector2(1f, 0.95f));
                TMP_Text body = EnsureChildText(rt, "BodyText", "Score…", 24f, FontStyles.Normal,
                    new Vector2(0.10f, 0.30f), new Vector2(0.65f, 0.78f));
                TMP_Text grade = EnsureChildText(rt, "GradeText", "A", 96f, FontStyles.Bold,
                    new Vector2(0.65f, 0.30f), new Vector2(0.95f, 0.78f));
                grade.alignment = TextAlignmentOptions.Center;
                grade.color = new Color(0.45f, 0.95f, 0.55f);
                Button restartButton = EnsureChildButton(rt, "RestartButton", "Restart",
                    new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.18f));

                DebriefView view = panel.GetComponent<DebriefView>();
                if (view == null) view = Undo.AddComponent<DebriefView>(panel);

                WrongHandPenaltyTracker tracker = Object.FindFirstObjectByType<WrongHandPenaltyTracker>();

                using SerializedObject so = new SerializedObject(view);
                so.FindProperty("lesson").objectReferenceValue = sm;
                so.FindProperty("penaltyTracker").objectReferenceValue = tracker;
                so.FindProperty("panel").objectReferenceValue = panel;
                so.FindProperty("titleText").objectReferenceValue = title;
                so.FindProperty("scoreBodyText").objectReferenceValue = body;
                so.FindProperty("gradeText").objectReferenceValue = grade;
                so.FindProperty("restartButton").objectReferenceValue = restartButton;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("<color=cyan>[Maritime LMS]</color> DebriefView rebuilt + wired.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] DebriefView rebuild failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undoGroup); }
        }

        private static GameObject EnsureUIChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name != name) continue;
                if (child is RectTransform) return child.gameObject;
                Undo.DestroyObjectImmediate(child.gameObject);
                break;
            }
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void EnsureBackground(GameObject panel, Color color)
        {
            Image img = panel.GetComponent<Image>();
            if (img == null) img = Undo.AddComponent<Image>(panel);
            img.color = color;
            img.raycastTarget = true;
        }

        private static TMP_Text EnsureChildText(RectTransform parent, string name, string initial,
            float fontSize, FontStyles style, Vector2 anchorMin, Vector2 anchorMax)
        {
            Transform existingT = parent.Find(name);
            GameObject go = existingT != null ? existingT.gameObject : null;
            if (go != null && !(existingT is RectTransform)) { Undo.DestroyObjectImmediate(go); go = null; }
            if (go == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                go.transform.SetParent(parent, false);
            }
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = Undo.AddComponent<TextMeshProUGUI>(go);
            tmp.text = initial;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button EnsureChildButton(RectTransform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            Transform existingT = parent.Find(name);
            GameObject go = existingT != null ? existingT.gameObject : null;
            if (go != null && !(existingT is RectTransform)) { Undo.DestroyObjectImmediate(go); go = null; }
            if (go == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                go.transform.SetParent(parent, false);
            }
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image bg = go.GetComponent<Image>();
            if (bg == null) bg = Undo.AddComponent<Image>(go);
            bg.color = new Color(0.20f, 0.45f, 0.85f, 0.95f);
            Button button = go.GetComponent<Button>();
            if (button == null) button = Undo.AddComponent<Button>(go);
            button.targetGraphic = bg;

            Transform labelT = rt.Find("Label");
            GameObject labelGo = labelT != null ? labelT.gameObject : null;
            if (labelGo != null && !(labelT is RectTransform)) { Undo.DestroyObjectImmediate(labelGo); labelGo = null; }
            if (labelGo == null)
            {
                labelGo = new GameObject("Label", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(labelGo, "Create Button Label");
                labelGo.transform.SetParent(rt, false);
            }
            RectTransform labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = Undo.AddComponent<TextMeshProUGUI>(labelGo);
            tmp.text = label;
            tmp.fontSize = 28f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            return button;
        }
    }

    [InitializeOnLoad]
    public static class DebriefViewRebuilderAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.DebriefRebuildRan.v1";

        static DebriefViewRebuilderAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Force DebriefView Rebuild")]
        public static void Rearm() { SessionState.SetBool(SessionStateKey, false); }

        private static void MaybeRun()
        {
            if (SessionState.GetBool(SessionStateKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += MaybeRun; return; }
            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.path.EndsWith("MaritimeBridgeLMS.unity")) { EditorApplication.delayCall += MaybeRun; return; }
            if (Object.FindFirstObjectByType<LessonStateMachine>() == null) { EditorApplication.delayCall += MaybeRun; return; }

            // Only rebuild if DebriefView is actually missing.
            Transform canvas = GameObject.Find("MaritimeLessonRoot/LessonCanvas")?.transform;
            Transform debrief = canvas != null ? canvas.Find("DebriefPanel") : null;
            DebriefView dv = debrief != null ? debrief.GetComponent<DebriefView>() : null;
            if (dv != null) { SessionState.SetBool(SessionStateKey, true); return; }

            try { DebriefViewRebuilderEditor.Run(); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] DebriefView rebuild hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
