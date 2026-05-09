#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaritimeLMS.Ais;
using MaritimeLMS.Compass;
using MaritimeLMS.Ecdis;
using MaritimeLMS.Helm;
using MaritimeLMS.Radar;
using MaritimeLMS.Telegraph;
using MaritimeLMS.Vhf;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Post-reorg pass: applies the FBX-correct local Euler angles to each
    /// re-parented equipment GameObject and disables legacy duplicates that
    /// the Phase 12 scaffold can no longer reach (e.g. the procedural
    /// MarineTelegraph still nested inside the kept-on-purpose
    /// "Maritime Training Station" placeholder).
    /// </summary>
    /// <remarks>
    /// The Phase 13 reorganizer zeroed each equipment's local rotation when
    /// re-parenting to BridgeCabin anchors. That works for the Hull v3.3
    /// (Y-up FBX) but flips every Z-up Blender export onto its back. Each
    /// of the seven device FBXes ships from the Maritime model library as
    /// Z-up, so they all need the canonical
    /// <c>(-90, 0, 0)</c> local rotation that the original Phase 1-9
    /// drag-and-drop instructions specified.
    ///
    /// MarineTelegraph (a procedural telegraph the Phase 1 prototype used
    /// before the EOT FBX shipped) is still present under Maritime Training
    /// Station. It overlaps visually with the new EngineOrderTelegraph and
    /// shouldn't accept input during the Phase 12 lesson — disable it.
    /// </remarks>
    public static class BridgeEquipmentTunerEditor
    {
        private const string MenuDryRun = "Tools/Maritime LMS/Tune Bridge Equipment — Dry Run";
        private const string MenuExecute = "Tools/Maritime LMS/Tune Bridge Equipment — Execute";
        private const string ReportPath = "Assets/Project/Maritime/EQUIPMENT_TUNE_REPORT.md";

        // (-90, 0, 0) converts Z-up Blender mesh authoring to Y-up Unity
        // when the parent transform is at identity rotation.
        private static readonly Vector3 ZUpFix = new Vector3(-90f, 0f, 0f);

        // Legacy GameObject paths to set inactive.
        private static readonly string[] LegacyDisablePaths =
        {
            "Ship/Bridge_Structure/Maritime Training Station/MarineTelegraph"
        };

        [MenuItem(MenuDryRun)]
        public static void DryRun() => Run(execute: false);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run(execute: true);

        public static string Run(bool execute)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[Maritime LMS] Tune: no active scene.");
                return null;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine($"# Equipment Tune {(execute ? "EXECUTE" : "Dry Run")} — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine($"Scene: `{scene.path}`");
            report.AppendLine();

            var plan = new (string label, Component comp, Vector3 targetEuler)[]
            {
                ("AIS",       Object.FindFirstObjectByType<AisTransceiver>(),       ZUpFix),
                ("VHF",       Object.FindFirstObjectByType<VhfRadio>(),             ZUpFix),
                ("ECDIS",     Object.FindFirstObjectByType<ElectronicChartDisplay>(), ZUpFix),
                ("Radar",     Object.FindFirstObjectByType<MarineRadar>(),          ZUpFix),
                ("EOT",       Object.FindFirstObjectByType<EngineOrderTelegraph>(), ZUpFix),
                ("Compass",   Object.FindFirstObjectByType<MagneticCompass>(),      ZUpFix),
                ("ShipWheel", Object.FindFirstObjectByType<ShipWheel>(),            ZUpFix)
            };

            report.AppendLine("## 1. Equipment rotation plan");
            int rotateCount = 0;
            foreach (var p in plan)
            {
                if (p.comp == null)
                {
                    report.AppendLine($"- {p.label,-10} **NOT IN SCENE** (skip)");
                    continue;
                }
                Transform t = p.comp.transform;
                Vector3 currentEuler = t.localEulerAngles;
                bool already = ApproxEqual(currentEuler, p.targetEuler) || ApproxEqual(NormalizeEuler(currentEuler), p.targetEuler);
                if (already)
                {
                    report.AppendLine($"- {p.label,-10} already at {p.targetEuler} — no change");
                }
                else
                {
                    report.AppendLine($"- {p.label,-10} `{GetPath(t)}`  {currentEuler}  →  {p.targetEuler}");
                    rotateCount++;
                }
            }
            report.AppendLine();

            report.AppendLine("## 2. Legacy duplicates to disable");
            var disableList = new List<GameObject>();
            foreach (string path in LegacyDisablePaths)
            {
                GameObject g = ResolvePath(scene, path);
                if (g == null)
                {
                    report.AppendLine($"- `{path}` — not found");
                    continue;
                }
                if (!g.activeSelf)
                {
                    report.AppendLine($"- `{path}` — already inactive");
                    continue;
                }
                disableList.Add(g);
                report.AppendLine($"- `{path}` — set inactive");
            }
            report.AppendLine();

            report.AppendLine("## 3. Summary");
            report.AppendLine($"- Rotation fixes: **{rotateCount}**");
            report.AppendLine($"- Legacy disables: **{disableList.Count}**");
            if (!execute)
            {
                report.AppendLine();
                report.AppendLine($"Dry-run only. To apply: `{MenuExecute}`");
            }
            report.AppendLine();

            if (!execute)
            {
                FlushReport(report);
                return report.ToString();
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Tune Bridge Equipment");
            try
            {
                foreach (var p in plan)
                {
                    if (p.comp == null) continue;
                    Transform t = p.comp.transform;
                    Vector3 cur = NormalizeEuler(t.localEulerAngles);
                    if (ApproxEqual(cur, p.targetEuler)) continue;
                    Undo.RecordObject(t, $"Tune {p.label} rotation");
                    t.localEulerAngles = p.targetEuler;
                    EditorUtility.SetDirty(t);
                }

                foreach (GameObject g in disableList)
                {
                    Undo.RecordObject(g, "Disable legacy duplicate");
                    g.SetActive(false);
                    EditorUtility.SetDirty(g);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.AppendLine($"**Executed.** {rotateCount} rotations + {disableList.Count} disables, scene saved.");
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Equipment tune complete: {rotateCount} rotations, {disableList.Count} legacy disabled.");
            }
            catch (System.Exception ex)
            {
                report.AppendLine($"**FAILED**: {ex.Message}");
                Debug.LogError($"[Maritime LMS] Equipment tune failed: {ex}");
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            FlushReport(report);
            return report.ToString();
        }

        private static GameObject ResolvePath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                Transform cur = root.transform;
                bool found = true;
                for (int i = 1; i < parts.Length; i++)
                {
                    Transform next = cur.Find(parts[i]);
                    if (next == null) { found = false; break; }
                    cur = next;
                }
                if (found) return cur.gameObject;
            }
            return null;
        }

        private static Vector3 NormalizeEuler(Vector3 e)
        {
            return new Vector3(NormAngle(e.x), NormAngle(e.y), NormAngle(e.z));
        }

        private static float NormAngle(float a)
        {
            float n = a % 360f;
            if (n > 180f) n -= 360f;
            if (n <= -180f) n += 360f;
            return n;
        }

        private static bool ApproxEqual(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(NormAngle(a.x) - NormAngle(b.x)) < 0.5f
                && Mathf.Abs(NormAngle(a.y) - NormAngle(b.y)) < 0.5f
                && Mathf.Abs(NormAngle(a.z) - NormAngle(b.z)) < 0.5f;
        }

        private static void FlushReport(StringBuilder report)
        {
            string text = report.ToString();
            try
            {
                File.WriteAllText(ReportPath, text);
                AssetDatabase.ImportAsset(ReportPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Failed to write tune report: {ex.Message}");
            }
            Debug.Log(text);
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "(null)";
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }
    }
}
#endif
