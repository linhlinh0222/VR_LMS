#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MaritimeLMS.Ais;
using MaritimeLMS.Compass;
using MaritimeLMS.Ecdis;
using MaritimeLMS.Helm;
using MaritimeLMS.Lessons;
using MaritimeLMS.Radar;
using MaritimeLMS.Telegraph;
using MaritimeLMS.Vhf;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// One-click scaffolder that wires Phase 12 (Bridge Familiarization &
    /// Departure) into the currently open scene. Creates the lesson
    /// GameObject, every objective component, the world-space Canvas with
    /// HUD / Briefing / Debrief panels, and assigns inspector references.
    /// </summary>
    /// <remarks>
    /// Pivoted to an editor-side scaffolder because the in-session MCP
    /// bridge has been flaky; running the menu locally is deterministic and
    /// keeps the wiring under source control review (the resulting scene
    /// diff is the artefact). Idempotent — re-running on a partially-wired
    /// scene reuses existing GameObjects rather than duplicating them.
    /// </remarks>
    public static class LessonScaffolderEditor
    {
        private const string MenuPath = "Tools/Maritime LMS/Scaffold Phase 12 Scene";

        private const string LessonRootName = "MaritimeLessonRoot";
        private const string ObjectivesContainerName = "Objectives";
        private const string CanvasName = "LessonCanvas";
        private const string HudPanelName = "HUD";
        private const string BriefingPanelName = "BriefingPanel";
        private const string DebriefPanelName = "DebriefPanel";
        private const string OceanModelGuidHint = "Ocean_v1.1";
        private const string BridgeCabinModelGuidHint = "BridgeCabin_v1.0_empty";

        [MenuItem(MenuPath)]
        public static void Scaffold()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Maritime LMS",
                    "Open a scene first (e.g. Assets/Project/Scenes/MaritimeBridgeLMS.unity).",
                    "OK");
                return;
            }

            Undo.SetCurrentGroupName("Scaffold Phase 12 Lesson");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                EnsureModelInstance(BridgeCabinModelGuidHint, "BridgeCabin",
                    new Vector3(0f, 10.23f, -28f), Quaternion.identity);
                EnsureModelInstance(OceanModelGuidHint, "Ocean",
                    Vector3.zero, Quaternion.identity);

                GameObject root = EnsureRoot();
                GameObject objectivesContainer = EnsureChild(root.transform, ObjectivesContainerName);

                AisPowerObjective aisObj = EnsureObjective<AisPowerObjective>(objectivesContainer.transform,
                    "Obj_PowerOnAIS", "Power on the AIS Class A transceiver", LessonPhase.Familiarization);
                VhfPowerObjective vhfObj = EnsureObjective<VhfPowerObjective>(objectivesContainer.transform,
                    "Obj_PowerOnVHF", "Power on the VHF radio (Channel 16 watch)", LessonPhase.Familiarization);
                EcdisPowerObjective ecdisObj = EnsureObjective<EcdisPowerObjective>(objectivesContainer.transform,
                    "Obj_PowerOnECDIS", "Power on the ECDIS chart display", LessonPhase.Familiarization);
                RadarTransmitObjective radarObj = EnsureObjective<RadarTransmitObjective>(objectivesContainer.transform,
                    "Obj_RadarTransmit", "Switch the radar to Transmit", LessonPhase.Familiarization);

                TelegraphPositionObjective telegraphObj = EnsureObjective<TelegraphPositionObjective>(
                    objectivesContainer.transform, "Obj_TelegraphSlowAhead",
                    "Set the engine telegraph to Slow Ahead (right hand)",
                    LessonPhase.Guided);
                using (SerializedObject so = new SerializedObject(telegraphObj))
                {
                    SerializedProperty pos = so.FindProperty("requiredPosition");
                    if (pos != null) pos.enumValueIndex = (int)TelegraphPosition.SlowAhead;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                HelmHeadingObjective helmObj = EnsureObjective<HelmHeadingObjective>(objectivesContainer.transform,
                    "Obj_HelmHeading045", "Steer to course 045°T (left hand)", LessonPhase.Guided);
                using (SerializedObject so = new SerializedObject(helmObj))
                {
                    SerializedProperty req = so.FindProperty("requiredHeadingDegrees");
                    SerializedProperty tol = so.FindProperty("toleranceDegrees");
                    SerializedProperty hold = so.FindProperty("requiredHoldSeconds");
                    if (req != null) req.floatValue = 45f;
                    if (tol != null) tol.floatValue = 5f;
                    if (hold != null) hold.floatValue = 2f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EventObjective vhfDistress = EnsureObjective<EventObjective>(objectivesContainer.transform,
                    "Obj_VHFDistressTest", "Hold VHF Distress on Channel 16 for 3 seconds",
                    LessonPhase.Assessment);

                AssignEquipmentReferences(aisObj, vhfObj, ecdisObj, radarObj, telegraphObj, helmObj, vhfDistress);

                LessonStateMachine stateMachine = EnsureLessonStateMachine(root,
                    aisObj, vhfObj, ecdisObj, radarObj,
                    telegraphObj, helmObj, vhfDistress);

                EnsureWrongHandTracker(root);

                GameObject canvasGo = EnsureLessonCanvas(root.transform, stateMachine);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorUtility.SetDirty(stateMachine);

                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Phase 12 lesson scaffolded into '{scene.name}'. " +
                          "Verify equipment references in Inspector, then press Play.");

                EditorUtility.DisplayDialog("Maritime LMS",
                    "Phase 12 lesson scaffolded.\n\n" +
                    "Next:\n" +
                    "1. Confirm equipment references on the objective GameObjects.\n" +
                    "2. Wire VhfRadio.DistressAlertSent → " + vhfDistress.name + ".NotifyTriggered (UnityEvent in Inspector).\n" +
                    "3. Open " + CanvasName + " and adjust UI font sizes / panel positions to taste.\n" +
                    "4. Press Play.",
                    "OK");
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ScaffoldValidate()
        {
            return SceneManager.GetActiveScene().IsValid();
        }

        private static GameObject EnsureModelInstance(string filenameHint, string objectName,
            Vector3 position, Quaternion rotation)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null) return existing;

            string[] guids = AssetDatabase.FindAssets($"{filenameHint} t:Model");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning($"[Maritime LMS] Could not find a model asset matching '{filenameHint}'. " +
                                 "Drag the FBX into the scene manually.");
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = objectName;
            instance.transform.SetPositionAndRotation(position, rotation);
            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {objectName}");
            return instance;
        }

        private static GameObject EnsureRoot()
        {
            GameObject existing = GameObject.Find(LessonRootName);
            if (existing != null) return existing;

            GameObject go = new GameObject(LessonRootName);
            Undo.RegisterCreatedObjectUndo(go, "Create Lesson Root");
            return go;
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name) return child.gameObject;
            }

            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T EnsureObjective<T>(Transform container, string objectName,
            string description, LessonPhase phase) where T : LessonObjectiveBase
        {
            GameObject child = EnsureChild(container, objectName);
            T comp = child.GetComponent<T>();
            if (comp == null)
            {
                comp = Undo.AddComponent<T>(child);
            }

            using (SerializedObject so = new SerializedObject(comp))
            {
                SerializedProperty id = so.FindProperty("objectiveId");
                SerializedProperty desc = so.FindProperty("description");
                SerializedProperty ph = so.FindProperty("phase");
                if (id != null && string.IsNullOrEmpty(id.stringValue)) id.stringValue = objectName;
                if (desc != null) desc.stringValue = description;
                if (ph != null) ph.enumValueIndex = (int)phase;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return comp;
        }

        private static void AssignEquipmentReferences(
            AisPowerObjective ais,
            VhfPowerObjective vhf,
            EcdisPowerObjective ecdis,
            RadarTransmitObjective radar,
            TelegraphPositionObjective telegraph,
            HelmHeadingObjective helm,
            EventObjective vhfDistress)
        {
            AisTransceiver aisInScene = Object.FindFirstObjectByType<AisTransceiver>();
            VhfRadio vhfInScene = Object.FindFirstObjectByType<VhfRadio>();
            ElectronicChartDisplay ecdisInScene = Object.FindFirstObjectByType<ElectronicChartDisplay>();
            MarineRadar radarInScene = Object.FindFirstObjectByType<MarineRadar>();
            EngineOrderTelegraph eotInScene = Object.FindFirstObjectByType<EngineOrderTelegraph>();
            MagneticCompass compassInScene = Object.FindFirstObjectByType<MagneticCompass>();

            AssignSerializedReference(ais, "ais", aisInScene);
            AssignSerializedReference(vhf, "vhf", vhfInScene);
            AssignSerializedReference(ecdis, "ecdis", ecdisInScene);
            AssignSerializedReference(radar, "radar", radarInScene);
            AssignSerializedReference(telegraph, "telegraph", eotInScene);
            AssignSerializedReference(helm, "compass", compassInScene);
            // VHF distress event is wired in Inspector by the user
            // (UnityEvents are not safely auto-bound from a script —
            // consumer needs to choose the right runtime/persistent target).
        }

        private static void AssignSerializedReference(Object owner, string fieldName, Object value)
        {
            if (owner == null || value == null) return;
            using SerializedObject so = new SerializedObject(owner);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LessonStateMachine EnsureLessonStateMachine(GameObject root,
            AisPowerObjective ais, VhfPowerObjective vhf, EcdisPowerObjective ecdis, RadarTransmitObjective radar,
            TelegraphPositionObjective telegraph, HelmHeadingObjective helm, EventObjective vhfDistress)
        {
            LessonStateMachine sm = root.GetComponent<LessonStateMachine>();
            if (sm == null) sm = Undo.AddComponent<LessonStateMachine>(root);

            using SerializedObject so = new SerializedObject(sm);
            SerializedProperty title = so.FindProperty("lessonTitle");
            SerializedProperty briefing = so.FindProperty("briefingText");
            SerializedProperty phases = so.FindProperty("phases");
            SerializedProperty autoStart = so.FindProperty("autoStart");

            if (title != null) title.stringValue = "Bridge Familiarization & Departure";
            if (briefing != null) briefing.stringValue =
                "You are second officer on watch. The bridge is preparing for departure.\n\n" +
                "Power on the navigation equipment, set the engine telegraph to Slow Ahead, " +
                "steer to course 045° true, and complete a VHF distress test on Channel 16.\n\n" +
                "Use 1 / 2 to switch active hand. Hold T (right) or Y (left) to manipulate hands.";
            if (autoStart != null) autoStart.boolValue = true;

            if (phases != null)
            {
                phases.arraySize = 5;
                ConfigurePhase(phases.GetArrayElementAtIndex(0),
                    LessonPhase.Briefing, "Briefing", "Read the scenario, then press Begin.",
                    new LessonObjectiveBase[0], requireAll: true, manualAdvance: true, minSeconds: 0f);
                ConfigurePhase(phases.GetArrayElementAtIndex(1),
                    LessonPhase.Familiarization, "Familiarization",
                    "Power on the four navigation systems.",
                    new LessonObjectiveBase[] { ais, vhf, ecdis, radar },
                    requireAll: true, manualAdvance: false, minSeconds: 0f);
                ConfigurePhase(phases.GetArrayElementAtIndex(2),
                    LessonPhase.Guided, "Guided procedure",
                    "Set telegraph to Slow Ahead with the right hand, then steer to 045°T with the left hand.",
                    new LessonObjectiveBase[] { telegraph, helm },
                    requireAll: true, manualAdvance: false, minSeconds: 0f);
                ConfigurePhase(phases.GetArrayElementAtIndex(3),
                    LessonPhase.Assessment, "Assessment",
                    "Complete a VHF distress test on Channel 16 (3-second hold).",
                    new LessonObjectiveBase[] { vhfDistress },
                    requireAll: true, manualAdvance: false, minSeconds: 0f);
                ConfigurePhase(phases.GetArrayElementAtIndex(4),
                    LessonPhase.Debrief, "Debrief", "Review your score, then press Restart to retry.",
                    new LessonObjectiveBase[0], requireAll: true, manualAdvance: true, minSeconds: 0f);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return sm;
        }

        private static void ConfigurePhase(SerializedProperty element, LessonPhase phase, string title, string instruction,
            LessonObjectiveBase[] objectives, bool requireAll, bool manualAdvance, float minSeconds)
        {
            element.FindPropertyRelative("phase").enumValueIndex = (int)phase;
            element.FindPropertyRelative("title").stringValue = title;
            element.FindPropertyRelative("instructionText").stringValue = instruction;
            SerializedProperty objArr = element.FindPropertyRelative("objectives");
            objArr.arraySize = objectives.Length;
            for (int i = 0; i < objectives.Length; i++)
            {
                objArr.GetArrayElementAtIndex(i).objectReferenceValue = objectives[i];
            }
            element.FindPropertyRelative("requireAllComplete").boolValue = requireAll;
            element.FindPropertyRelative("requiresManualAdvance").boolValue = manualAdvance;
            element.FindPropertyRelative("minPhaseSeconds").floatValue = minSeconds;
        }

        private static WrongHandPenaltyTracker EnsureWrongHandTracker(GameObject root)
        {
            WrongHandPenaltyTracker tracker = root.GetComponent<WrongHandPenaltyTracker>();
            if (tracker == null) tracker = Undo.AddComponent<WrongHandPenaltyTracker>(root);
            return tracker;
        }

        private static GameObject EnsureLessonCanvas(Transform parent, LessonStateMachine stateMachine)
        {
            GameObject canvasGo = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == CanvasName)
                {
                    canvasGo = child.gameObject;
                    break;
                }
            }

            if (canvasGo == null)
            {
                canvasGo = new GameObject(CanvasName);
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Lesson Canvas");
                canvasGo.transform.SetParent(parent, false);

                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 4f;
                canvasGo.AddComponent<GraphicRaycaster>();

                RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
                canvasRt.sizeDelta = new Vector2(2.4f, 1.6f);
                canvasRt.localScale = Vector3.one * 0.001f;
                canvasGo.transform.SetPositionAndRotation(new Vector3(0f, 11.2f, -25.6f), Quaternion.identity);
            }

            BuildHudPanel(canvasGo.transform, stateMachine);
            BuildBriefingPanel(canvasGo.transform, stateMachine);
            BuildDebriefPanel(canvasGo.transform, stateMachine);
            return canvasGo;
        }

        private static void BuildHudPanel(Transform canvas, LessonStateMachine stateMachine)
        {
            GameObject panel = EnsureChild(canvas, HudPanelName);
            RectTransform rt = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.65f);
            rt.anchorMax = new Vector2(0.45f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            EnsureBackground(panel, new Color(0f, 0f, 0f, 0.55f));

            TMP_Text title = EnsureChildText(rt, "TitleText", "Lesson Title", 32f, FontStyles.Bold,
                new Vector2(0f, 0.75f), new Vector2(1f, 1f));
            TMP_Text phase = EnsureChildText(rt, "PhaseText", "Phase", 24f, FontStyles.Italic,
                new Vector2(0f, 0.55f), new Vector2(1f, 0.78f));
            TMP_Text objectives = EnsureChildText(rt, "ObjectivesText", "Objectives", 20f, FontStyles.Normal,
                new Vector2(0f, 0.05f), new Vector2(1f, 0.55f));
            TMP_Text elapsed = EnsureChildText(rt, "ElapsedText", "00:00", 22f, FontStyles.Normal,
                new Vector2(0.65f, 0f), new Vector2(1f, 0.10f));

            LessonHUDView hud = panel.GetComponent<LessonHUDView>();
            if (hud == null) hud = Undo.AddComponent<LessonHUDView>(panel);

            using SerializedObject so = new SerializedObject(hud);
            so.FindProperty("lesson").objectReferenceValue = stateMachine;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("phaseText").objectReferenceValue = phase;
            so.FindProperty("objectivesText").objectReferenceValue = objectives;
            so.FindProperty("elapsedText").objectReferenceValue = elapsed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildBriefingPanel(Transform canvas, LessonStateMachine stateMachine)
        {
            GameObject panel = EnsureChild(canvas, BriefingPanelName);
            RectTransform rt = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.20f, 0.20f);
            rt.anchorMax = new Vector2(0.80f, 0.80f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            EnsureBackground(panel, new Color(0.05f, 0.10f, 0.18f, 0.92f));

            TMP_Text title = EnsureChildText(rt, "TitleText", "Briefing", 40f, FontStyles.Bold,
                new Vector2(0f, 0.78f), new Vector2(1f, 0.95f));
            TMP_Text body = EnsureChildText(rt, "BodyText", "Briefing body…", 24f, FontStyles.Normal,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.78f));
            Button beginButton = EnsureChildButton(rt, "BeginButton", "Begin",
                new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.18f));

            BriefingView view = panel.GetComponent<BriefingView>();
            if (view == null) view = Undo.AddComponent<BriefingView>(panel);

            using SerializedObject so = new SerializedObject(view);
            so.FindProperty("lesson").objectReferenceValue = stateMachine;
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("beginButton").objectReferenceValue = beginButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildDebriefPanel(Transform canvas, LessonStateMachine stateMachine)
        {
            GameObject panel = EnsureChild(canvas, DebriefPanelName);
            panel.SetActive(false);
            RectTransform rt = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
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

            using SerializedObject so = new SerializedObject(view);
            so.FindProperty("lesson").objectReferenceValue = stateMachine;
            so.FindProperty("penaltyTracker").objectReferenceValue = stateMachine.GetComponent<WrongHandPenaltyTracker>();
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("scoreBodyText").objectReferenceValue = body;
            so.FindProperty("gradeText").objectReferenceValue = grade;
            so.FindProperty("restartButton").objectReferenceValue = restartButton;
            so.ApplyModifiedPropertiesWithoutUndo();
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
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
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
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
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
            if (labelGo == null)
            {
                labelGo = new GameObject("Label");
                Undo.RegisterCreatedObjectUndo(labelGo, "Create Button Label");
                labelGo.transform.SetParent(rt, false);
            }
            RectTransform labelRt = labelGo.GetComponent<RectTransform>() ?? labelGo.AddComponent<RectTransform>();
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
}
#endif
