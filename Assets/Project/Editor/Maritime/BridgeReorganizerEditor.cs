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
    /// Re-parents the seven bridge equipment GameObjects under the matching
    /// <c>anchor_*</c> empties inside the imported BridgeCabin FBX, then
    /// disables or destroys the legacy placeholder cabin/ocean GameObjects
    /// that the bridge no longer needs. Provides a dry-run mode that writes
    /// the planned changes to the Console and to
    /// <c>Maritime/REORG_REPORT.md</c> without touching the scene, so the
    /// team can review before committing the destructive pass.
    /// </summary>
    /// <remarks>
    /// The auto-hook side runs the dry-run automatically once per Unity
    /// session, after the Phase 12 scaffolder has finished. To execute the
    /// changes, use the <c>Execute</c> menu item or set
    /// <see cref="ApprovedToExecute"/> via the helper menu.
    /// </remarks>
    public static class BridgeReorganizerEditor
    {
        private const string MenuDryRun = "Tools/Maritime LMS/Reorganize Bridge — Dry Run";
        private const string MenuExecute = "Tools/Maritime LMS/Reorganize Bridge — Execute (DESTRUCTIVE)";
        private const string ReportPath = "Assets/Project/Maritime/REORG_REPORT.md";

        // Placeholder GameObject names that the FBX bridge cabin replaces.
        // Top-level Ship-children only — children of these are dragged along
        // when the parent is removed.
        private static readonly string[] PlaceholderTopLevelNames =
        {
            "Maritime Training Station", // procedural cabin shell + walls + windows + console
            "Generated Station Geometry",
            "OceanAmbient",
            "Open Sea Surface"
        };

        // Procedural placeholders under any depth that should be removed —
        // their FBX equivalents now hold the real geometry.
        private static readonly string[] PlaceholderDescendantNames =
        {
            "Generated Telegraph Lever Shaft",
            "Generated Telegraph Lever Knob"
        };

        public static bool ApprovedToExecute
        {
            get => SessionState.GetBool("MaritimeLMS.ReorgApproved", false);
            set => SessionState.SetBool("MaritimeLMS.ReorgApproved", value);
        }

        [MenuItem(MenuDryRun)]
        public static void DryRun() => Run(execute: false);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run(execute: true);

        public static string Run(bool execute)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[Maritime LMS] Reorganize: no active scene.");
                return null;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine($"# Bridge Reorganize {(execute ? "EXECUTE" : "Dry Run")} — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine($"Scene: `{scene.path}`");
            report.AppendLine();

            GameObject cabin = GameObject.Find("BridgeCabin");
            if (cabin == null)
            {
                report.AppendLine("**FAIL**: GameObject `BridgeCabin` not found. Run the Phase 12 scaffolder first.");
                FlushReport(report, execute);
                return report.ToString();
            }

            // Anchors map: short name -> Transform inside cabin (recursive).
            var anchorByEquipment = new Dictionary<string, Transform>
            {
                ["AIS"] = FindDeepChild(cabin.transform, "anchor_AIS4000"),
                ["VHF"] = FindDeepChild(cabin.transform, "anchor_VHF"),
                ["ECDIS"] = FindDeepChild(cabin.transform, "anchor_ECDIS"),
                ["Radar"] = FindDeepChild(cabin.transform, "anchor_Radar"),
                ["EOT"] = FindDeepChild(cabin.transform, "anchor_EOT"),
                ["Compass"] = FindDeepChild(cabin.transform, "anchor_Compass"),
                ["ShipWheel"] = FindDeepChild(cabin.transform, "anchor_ShipWheel")
            };

            report.AppendLine("## 1. Anchors detected");
            foreach (var kv in anchorByEquipment)
            {
                report.AppendLine($"- {kv.Key,-10} `{(kv.Value != null ? GetPath(kv.Value) : "MISSING")}`");
            }
            report.AppendLine();

            // Equipment lookups via component type.
            var equipPlan = new (string label, Component comp, Transform anchor)[]
            {
                ("AIS",       Object.FindFirstObjectByType<AisTransceiver>(),       anchorByEquipment["AIS"]),
                ("VHF",       Object.FindFirstObjectByType<VhfRadio>(),             anchorByEquipment["VHF"]),
                ("ECDIS",     Object.FindFirstObjectByType<ElectronicChartDisplay>(), anchorByEquipment["ECDIS"]),
                ("Radar",     Object.FindFirstObjectByType<MarineRadar>(),          anchorByEquipment["Radar"]),
                ("EOT",       Object.FindFirstObjectByType<EngineOrderTelegraph>(), anchorByEquipment["EOT"]),
                ("Compass",   Object.FindFirstObjectByType<MagneticCompass>(),      anchorByEquipment["Compass"]),
                ("ShipWheel", Object.FindFirstObjectByType<ShipWheel>(),            anchorByEquipment["ShipWheel"])
            };

            report.AppendLine("## 2. Equipment re-parent plan");
            int reparentCount = 0;
            foreach (var p in equipPlan)
            {
                if (p.comp == null)
                {
                    report.AppendLine($"- {p.label,-10} **NOT IN SCENE** (skip)");
                    continue;
                }
                Transform t = p.comp.transform;
                bool already = p.anchor != null && t.parent == p.anchor;
                string fromPath = GetPath(t);
                string toPath = p.anchor != null ? GetPath(p.anchor) : "(no anchor)";
                if (already)
                {
                    report.AppendLine($"- {p.label,-10} already under anchor — no change");
                }
                else
                {
                    report.AppendLine($"- {p.label,-10} `{fromPath}` → `{toPath}`{(p.anchor == null ? " *(no anchor — skipped)*" : "")}");
                    if (p.anchor != null) reparentCount++;
                }
            }
            report.AppendLine();

            // Placeholders to delete/disable.
            report.AppendLine("## 3. Placeholders flagged for removal");
            var placeholderGOs = new List<GameObject>();
            foreach (string n in PlaceholderTopLevelNames)
            {
                GameObject g = GameObject.Find(n);
                if (g != null)
                {
                    placeholderGOs.Add(g);
                    int childCount = g.transform.childCount;
                    report.AppendLine($"- `{GetPath(g.transform)}` ({childCount} immediate children)");
                }
            }
            foreach (string n in PlaceholderDescendantNames)
            {
                Transform t = FindDeepChildAnywhere(scene, n);
                if (t != null)
                {
                    placeholderGOs.Add(t.gameObject);
                    report.AppendLine($"- `{GetPath(t)}` (procedural placeholder)");
                }
            }
            if (placeholderGOs.Count == 0) report.AppendLine("(none — already cleaned)");
            report.AppendLine();

            report.AppendLine("## 4. Summary");
            report.AppendLine($"- Re-parent operations: **{reparentCount}**");
            report.AppendLine($"- Placeholders to remove: **{placeholderGOs.Count}**");
            if (!execute)
            {
                report.AppendLine();
                report.AppendLine("Dry-run only — nothing changed. To apply, run:");
                report.AppendLine($"`{MenuExecute}`");
            }
            report.AppendLine();

            if (!execute)
            {
                FlushReport(report, execute);
                return report.ToString();
            }

            // Execute pass.
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Reorganize Bridge");
            try
            {
                foreach (var p in equipPlan)
                {
                    if (p.comp == null || p.anchor == null) continue;
                    Transform t = p.comp.transform;
                    if (t.parent == p.anchor) continue;
                    Undo.SetTransformParent(t, p.anchor, $"Re-parent {p.label}");
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                }

                foreach (GameObject g in placeholderGOs)
                {
                    if (g == null) continue;
                    Undo.DestroyObjectImmediate(g);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.AppendLine("**Executed.** Scene saved.");
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Bridge reorganize complete: {reparentCount} re-parented, {placeholderGOs.Count} placeholders removed.");
            }
            catch (System.Exception ex)
            {
                report.AppendLine($"**FAILED**: {ex.Message}");
                Debug.LogError($"[Maritime LMS] Bridge reorganize failed: {ex}");
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            FlushReport(report, execute);
            return report.ToString();
        }

        private static void FlushReport(StringBuilder report, bool execute)
        {
            string text = report.ToString();
            try
            {
                File.WriteAllText(ReportPath, text);
                AssetDatabase.ImportAsset(ReportPath);
                Debug.Log($"[Maritime LMS] Reorganize {(execute ? "EXECUTE" : "Dry Run")} report → {ReportPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Failed to write reorg report: {ex.Message}");
            }
            // Mirror to console so it shows up in the Unity Console without
            // needing to open the markdown file.
            Debug.Log(text);
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindDeepChildAnywhere(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDeepChild(root.transform, name);
                if (found != null) return found;
            }
            return null;
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
