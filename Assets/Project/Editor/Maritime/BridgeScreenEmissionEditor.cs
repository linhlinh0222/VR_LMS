#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaritimeLMS.Ais;
using MaritimeLMS.Ecdis;
using MaritimeLMS.Radar;
using MaritimeLMS.Vhf;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 24 — switch the four console-display screens (ECDIS, Radar,
    /// AIS, VHF) to emissive material so they read as 'powered on' even
    /// before any per-device render texture is wired. Matches the
    /// reference bridge photo where every display has a visible glow.
    /// </summary>
    /// <remarks>
    /// Targets <c>Screen Display</c> mesh renderers under each device by
    /// name pattern. Clones the existing material per device so emission
    /// only applies to the screen surface, not the whole bezel. Uses a
    /// neutral cool-white emission tinted blue/green per device for
    /// quick visual differentiation; designers can replace with a
    /// proper render texture later.
    /// </remarks>
    public static class BridgeScreenEmissionEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Light Up Display Screens — Execute";
        private const string ReportPath = "Assets/Project/Maritime/SCREEN_EMISSION_REPORT.md";

        // Screen mesh name patterns + emission color per equipment.
        private static readonly (string label, System.Func<Component> finder, Color emission, string[] meshNames)[] Targets =
        {
            ("ECDIS", () => Object.FindFirstObjectByType<ElectronicChartDisplay>(),
                new Color(0.10f, 0.55f, 0.90f) * 1.5f,
                new[] { "screen", "display", "lcd" }),
            ("Radar", () => Object.FindFirstObjectByType<MarineRadar>(),
                new Color(0.20f, 0.85f, 0.35f) * 1.5f,
                new[] { "screen", "display", "ppi" }),
            ("AIS",   () => Object.FindFirstObjectByType<AisTransceiver>(),
                new Color(0.55f, 0.75f, 1.00f) * 1.5f,
                new[] { "screen", "display", "lcd" }),
            ("VHF",   () => Object.FindFirstObjectByType<VhfRadio>(),
                new Color(0.95f, 0.65f, 0.20f) * 1.5f,
                new[] { "screen", "display", "lcd" })
        };

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static string Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Screen emission: no scene."); return null; }

            StringBuilder r = new StringBuilder();
            r.AppendLine($"# Screen Emission — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            r.AppendLine();

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Light Up Display Screens");
            int touched = 0;

            try
            {
                foreach (var (label, finder, emission, names) in Targets)
                {
                    Component c = finder();
                    if (c == null) { r.AppendLine($"## {label} — NOT IN SCENE"); continue; }
                    r.AppendLine($"## {label}");

                    Renderer[] all = c.GetComponentsInChildren<Renderer>(true);
                    int matched = 0;
                    foreach (Renderer ren in all)
                    {
                        string nameLower = ren.gameObject.name.ToLowerInvariant();
                        bool match = false;
                        foreach (string n in names) if (nameLower.Contains(n)) { match = true; break; }
                        if (!match) continue;
                        Material src = ren.sharedMaterial;
                        if (src == null) continue;
                        Material clone = new Material(src);
                        clone.name = src.name + "_emissive";
                        clone.EnableKeyword("_EMISSION");
                        if (clone.HasProperty("_EmissionColor")) clone.SetColor("_EmissionColor", emission);
                        if (clone.HasProperty("_BaseColor")) clone.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.07f));
                        Undo.RecordObject(ren, $"Emit {label}");
                        ren.sharedMaterial = clone;
                        EditorUtility.SetDirty(ren);
                        matched++;
                        touched++;
                        r.AppendLine($"- `{ren.gameObject.name}` -> emission RGB({emission.r:F2},{emission.g:F2},{emission.b:F2})");
                    }
                    if (matched == 0) r.AppendLine("- (no screen mesh matched name pattern — skipped)");
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                r.AppendLine();
                r.AppendLine($"**Touched {touched} screen renderers.** Scene saved.");
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Screen emission applied to {touched} renderers.");
            }
            catch (System.Exception ex)
            {
                r.AppendLine($"**FAILED**: {ex.Message}");
                Debug.LogError($"[Maritime LMS] Screen emission failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undoGroup); }

            string text = r.ToString();
            try { File.WriteAllText(ReportPath, text); AssetDatabase.ImportAsset(ReportPath); }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] emission report write failed: {ex.Message}"); }
            Debug.Log(text);
            return text;
        }
    }

    [InitializeOnLoad]
    public static class BridgeScreenEmissionAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.ScreenEmissionRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteScreenEmission.flag";

        static BridgeScreenEmissionAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Screen Emission")]
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
            if (GameObject.Find("BridgeCabin") == null) { EditorApplication.delayCall += MaybeRun; return; }

            try
            {
                Debug.Log("[Maritime LMS] Screen-emission EXECUTE starting...");
                BridgeScreenEmissionEditor.Run();
                try { System.IO.File.Delete(ExecuteSentinelPath); }
                catch (System.Exception ex) { Debug.LogWarning($"sentinel del: {ex.Message}"); }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] screen-emission hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
