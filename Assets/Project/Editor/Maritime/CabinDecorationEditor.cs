#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaritimeLMS.Compass;
using MaritimeLMS.Helm;
using MaritimeLMS.Telegraph;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 31 — fill out the bare bridge cabin with primitive-based
    /// decoration so the scene reads like the reference photo of an
    /// integrated bridge: cream wood-panel walls, dark navy carpet, a
    /// continuous console run under the displays, vertical pedestal
    /// columns under the EOT and the compass binnacle, a helm pedestal
    /// under the ship wheel, and decorative button + knob primitives
    /// flanking the displays. All decoration is parented under
    /// <c>BridgeCabin/Decoration</c> so it can be wiped + rebuilt
    /// idempotently.
    /// </summary>
    /// <remarks>
    /// Materials are URP/Lit at editor time with simple base colours.
    /// No external assets required — primitives + sharedMaterial only.
    /// Re-running the menu rebuilds the decoration from scratch.
    /// </remarks>
    public static class CabinDecorationEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Build Cabin Decoration — Execute";
        private const string DecorationRootName = "Decoration";

        private static readonly Color WallColor = new Color(0.78f, 0.71f, 0.58f);     // cream wood
        private static readonly Color CarpetColor = new Color(0.05f, 0.10f, 0.18f);   // dark navy
        private static readonly Color ConsoleColor = new Color(0.18f, 0.18f, 0.20f);  // dark gray industrial
        private static readonly Color ConsoleTopColor = new Color(0.10f, 0.10f, 0.12f); // matte rubber top
        private static readonly Color HelmPedestalColor = new Color(0.55f, 0.56f, 0.58f); // brushed steel
        private static readonly Color BrassColor = new Color(0.78f, 0.62f, 0.30f);    // brass
        private static readonly Color EotPedestalColor = new Color(0.32f, 0.32f, 0.34f); // dark gray
        private static readonly Color ButtonRedColor = new Color(0.85f, 0.15f, 0.15f);
        private static readonly Color ButtonGreenColor = new Color(0.20f, 0.75f, 0.30f);
        private static readonly Color ButtonAmberColor = new Color(0.95f, 0.75f, 0.20f);
        private static readonly Color ButtonBodyColor = new Color(0.10f, 0.10f, 0.10f);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Decoration: no scene."); return; }

            GameObject cabin = GameObject.Find("BridgeCabin");
            if (cabin == null) { Debug.LogError("[Maritime LMS] BridgeCabin not in scene."); return; }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Cabin Decoration");
            try
            {
                Transform existing = cabin.transform.Find(DecorationRootName);
                if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

                GameObject root = new GameObject(DecorationRootName);
                Undo.RegisterCreatedObjectUndo(root, "Decoration root");
                root.transform.SetParent(cabin.transform, false);

                BuildCarpet(root.transform);
                BuildWalls(root.transform);
                BuildConsoleRun(root.transform);
                BuildHelmPedestal(root.transform);
                BuildEotPedestal(root.transform);
                BuildCompassColumn(root.transform);
                BuildDecorButtons(root.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"<color=cyan>[Maritime LMS]</color> Cabin decoration rebuilt.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Decoration build failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undoGroup); }
        }

        // Floor carpet: large blue plane spanning the cabin interior.
        private static void BuildCarpet(Transform parent)
        {
            GameObject g = MakeCube("Carpet", parent, CarpetColor);
            g.transform.position = new Vector3(0f, 10.245f, -27.0f);
            g.transform.localScale = new Vector3(7.6f, 0.02f, 4.6f);
            StripCollider(g);
        }

        // Side wall trim: two long boxes lining the cabin port + starboard
        // walls just above floor level so the cream paneling reads even if
        // the cabin FBX shells are unmaterialised.
        private static void BuildWalls(Transform parent)
        {
            GameObject pw = MakeCube("WallPort", parent, WallColor);
            pw.transform.position = new Vector3(-3.85f, 11.0f, -27.0f);
            pw.transform.localScale = new Vector3(0.05f, 1.4f, 4.4f);
            StripCollider(pw);

            GameObject sw = MakeCube("WallStarboard", parent, WallColor);
            sw.transform.position = new Vector3(3.85f, 11.0f, -27.0f);
            sw.transform.localScale = new Vector3(0.05f, 1.4f, 4.4f);
            StripCollider(sw);
        }

        // Continuous front console run beneath all four displays + helm
        // station — gives the displays something to sit on instead of
        // floating in mid-air.
        private static void BuildConsoleRun(Transform parent)
        {
            GameObject body = MakeCube("ConsoleBody", parent, ConsoleColor);
            body.transform.position = new Vector3(0f, 10.55f, -25.85f);
            body.transform.localScale = new Vector3(6.0f, 0.85f, 0.55f);
            StripCollider(body);

            GameObject top = MakeCube("ConsoleTop", parent, ConsoleTopColor);
            top.transform.position = new Vector3(0f, 10.98f, -25.85f);
            top.transform.localScale = new Vector3(6.0f, 0.04f, 0.62f);
            StripCollider(top);

            // Two short side wings angling slightly inward so the run
            // matches the reference photo's L-shaped console.
            GameObject leftWing = MakeCube("ConsoleWingLeft", parent, ConsoleColor);
            leftWing.transform.position = new Vector3(-3.1f, 10.55f, -26.85f);
            leftWing.transform.localScale = new Vector3(0.55f, 0.85f, 1.6f);
            StripCollider(leftWing);

            GameObject rightWing = MakeCube("ConsoleWingRight", parent, ConsoleColor);
            rightWing.transform.position = new Vector3(3.1f, 10.55f, -26.85f);
            rightWing.transform.localScale = new Vector3(0.55f, 0.85f, 1.6f);
            StripCollider(rightWing);
        }

        // Helm wheel pedestal: short brushed-steel cylinder under the ship
        // wheel.
        private static void BuildHelmPedestal(Transform parent)
        {
            GameObject ped = MakeCylinder("HelmPedestal", parent, HelmPedestalColor);
            ped.transform.position = new Vector3(-0.50f, 10.32f, -27.00f);
            ped.transform.localScale = new Vector3(0.45f, 0.22f, 0.45f);
            StripCollider(ped);
        }

        // EOT pedestal: tall dark column rising from the deck up under the
        // EOT mount. Reference photo shows this as a slim industrial post.
        private static void BuildEotPedestal(Transform parent)
        {
            GameObject post = MakeCylinder("EotPedestal", parent, EotPedestalColor);
            post.transform.position = new Vector3(0f, 10.34f, -26.85f);
            post.transform.localScale = new Vector3(0.30f, 0.62f, 0.30f);
            StripCollider(post);
        }

        // Compass binnacle column: brass-coloured pillar from the console
        // top up to where the compass is suspended.
        private static void BuildCompassColumn(Transform parent)
        {
            GameObject col = MakeCylinder("CompassColumn", parent, BrassColor);
            col.transform.position = new Vector3(0f, 11.05f, -26.20f);
            col.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            StripCollider(col);

            GameObject ring = MakeCylinder("CompassRing", parent, BrassColor);
            ring.transform.position = new Vector3(0f, 11.42f, -26.20f);
            ring.transform.localScale = new Vector3(0.42f, 0.05f, 0.42f);
            StripCollider(ring);
        }

        // Decorative buttons + knobs flanking the displays. Pure cosmetic,
        // not interactable, so collider is stripped.
        private static void BuildDecorButtons(Transform parent)
        {
            // Console status lamps row above ConsoleTop, between each pair
            // of displays.
            float[] lampX = { -2.55f, -1.55f, -0.55f, 0.55f, 1.55f, 2.55f };
            Color[] lampC = { ButtonRedColor, ButtonGreenColor, ButtonAmberColor,
                              ButtonAmberColor, ButtonGreenColor, ButtonRedColor };
            for (int i = 0; i < lampX.Length; i++)
            {
                GameObject lamp = MakeSphere($"Lamp{i}", parent, lampC[i]);
                lamp.transform.position = new Vector3(lampX[i], 11.04f, -25.65f);
                lamp.transform.localScale = Vector3.one * 0.04f;
                StripCollider(lamp);
            }

            // Knob row along the console top forward edge.
            float[] knobX = { -2.30f, -1.85f, -1.40f, -0.95f, 0.95f, 1.40f, 1.85f, 2.30f };
            for (int i = 0; i < knobX.Length; i++)
            {
                GameObject knob = MakeCylinder($"Knob{i}", parent, ButtonBodyColor);
                knob.transform.position = new Vector3(knobX[i], 11.02f, -25.66f);
                knob.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
                StripCollider(knob);
            }
        }

        // ----- primitive factories -----

        private static GameObject MakeCube(string name, Transform parent, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(g, $"Create {name}");
            g.name = name;
            g.transform.SetParent(parent, false);
            ApplyColor(g, color);
            return g;
        }

        private static GameObject MakeCylinder(string name, Transform parent, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(g, $"Create {name}");
            g.name = name;
            g.transform.SetParent(parent, false);
            ApplyColor(g, color);
            return g;
        }

        private static GameObject MakeSphere(string name, Transform parent, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(g, $"Create {name}");
            g.name = name;
            g.transform.SetParent(parent, false);
            ApplyColor(g, color);
            return g;
        }

        private static void ApplyColor(GameObject g, Color color)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r == null) return;
            Shader urp = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(urp);
            mat.name = g.name + "_mat";
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static void StripCollider(GameObject g)
        {
            Collider col = g.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
    }

    [InitializeOnLoad]
    public static class CabinDecorationAutoHook
    {
        private const string SessionStateKey = "MaritimeLMS.CabinDecorRan.v1";
        private const string ExecuteSentinelPath = "Library/MaritimeLMS_AutoExecuteCabinDecor.flag";

        static CabinDecorationAutoHook() { EditorApplication.delayCall += MaybeRun; }

        [MenuItem("Tools/Maritime LMS/Arm Auto-Execute Cabin Decoration")]
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
                Debug.Log("[Maritime LMS] Cabin decoration EXECUTE starting...");
                CabinDecorationEditor.Run();
                try { System.IO.File.Delete(ExecuteSentinelPath); }
                catch (System.Exception ex) { Debug.LogWarning($"sentinel del: {ex.Message}"); }
            }
            catch (System.Exception ex) { Debug.LogError($"[Maritime LMS] cabin-decor hook threw: {ex.Message}"); }
            finally { SessionState.SetBool(SessionStateKey, true); }
        }
    }
}
#endif
