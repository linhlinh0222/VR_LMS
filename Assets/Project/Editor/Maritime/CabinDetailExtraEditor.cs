#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 33 — extra primitive-based detail on top of Phase 31's
    /// Decoration root: vertical window mullion bars, an overhead
    /// ceiling light strip, button keypads flanking the EOT, a captain's
    /// chair primitive behind the player, and a low ship-deck plane
    /// extending forward of the cabin so the ocean view has a horizon
    /// reference rather than being a flat skybox. All parented under
    /// <c>BridgeCabin/DecorationExtra</c> so they can be wiped + rebuilt
    /// without touching the Phase 31 Decoration root.
    /// </summary>
    public static class CabinDetailExtraEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Build Cabin Extra Detail — Execute";
        private const string ExtraRootName = "DecorationExtra";

        private static readonly Color SteelDark = new Color(0.20f, 0.20f, 0.22f);
        private static readonly Color SteelLight = new Color(0.55f, 0.56f, 0.58f);
        private static readonly Color CeilingLight = new Color(0.95f, 0.93f, 0.85f);
        private static readonly Color ChairLeather = new Color(0.10f, 0.08f, 0.06f);
        private static readonly Color ButtonRed = new Color(0.85f, 0.20f, 0.20f);
        private static readonly Color ButtonAmber = new Color(0.95f, 0.65f, 0.10f);
        private static readonly Color ShipDeckColor = new Color(0.55f, 0.50f, 0.45f);
        private static readonly Color RailColor = new Color(0.85f, 0.85f, 0.85f);

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Extra detail: no scene."); return; }

            GameObject cabin = GameObject.Find("BridgeCabin");
            if (cabin == null) { Debug.LogError("[Maritime LMS] No BridgeCabin."); return; }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Cabin Extra Detail");
            try
            {
                Transform existing = cabin.transform.Find(ExtraRootName);
                if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

                GameObject root = new GameObject(ExtraRootName);
                Undo.RegisterCreatedObjectUndo(root, "Extra root");
                root.transform.SetParent(cabin.transform, false);

                BuildWindowMullions(root.transform);
                BuildCeilingLight(root.transform);
                BuildEotKeypads(root.transform);
                BuildCaptainsChair(root.transform);
                BuildShipBow(root.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("<color=cyan>[Maritime LMS]</color> Cabin extra detail built.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Extra detail failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undoGroup); }
        }

        // 5 thin vertical mullion bars across the front window opening so
        // the windows don't read as one big sheet of sky.
        private static void BuildWindowMullions(Transform parent)
        {
            float[] mullionsX = { -2.5f, -1.25f, 0f, 1.25f, 2.5f };
            for (int i = 0; i < mullionsX.Length; i++)
            {
                GameObject m = MakeCube($"Mullion{i}", parent, SteelDark);
                m.transform.position = new Vector3(mullionsX[i], 12.30f, -25.50f);
                m.transform.localScale = new Vector3(0.06f, 1.30f, 0.06f);
                StripCollider(m);
            }
            // Top + bottom horizontal frame
            GameObject top = MakeCube("WindowFrameTop", parent, SteelDark);
            top.transform.position = new Vector3(0f, 12.95f, -25.50f);
            top.transform.localScale = new Vector3(6.5f, 0.10f, 0.08f);
            StripCollider(top);
            GameObject bot = MakeCube("WindowFrameBottom", parent, SteelDark);
            bot.transform.position = new Vector3(0f, 11.65f, -25.50f);
            bot.transform.localScale = new Vector3(6.5f, 0.10f, 0.08f);
            StripCollider(bot);
        }

        // Long horizontal emissive cuboid running across the cabin
        // ceiling — fakes the under-bridge fluorescent lighting strip.
        private static void BuildCeilingLight(Transform parent)
        {
            GameObject strip = MakeCube("CeilingStrip", parent, CeilingLight);
            strip.transform.position = new Vector3(0f, 13.10f, -27.20f);
            strip.transform.localScale = new Vector3(5.5f, 0.05f, 0.18f);
            StripCollider(strip);
            EnableEmission(strip, CeilingLight * 0.8f);

            GameObject point = new GameObject("CeilingPointLight");
            Undo.RegisterCreatedObjectUndo(point, "Ceiling point light");
            point.transform.SetParent(parent, false);
            point.transform.position = new Vector3(0f, 13.05f, -27.20f);
            Light l = point.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 6f;
            l.intensity = 0.9f;
            l.color = new Color(1f, 0.95f, 0.85f);
            l.shadows = LightShadows.None;
        }

        // Small button cluster next to the EOT — visual filler matching
        // the reference photo where a control panel sits beside the
        // telegraph pedestal.
        private static void BuildEotKeypads(Transform parent)
        {
            GameObject panel = MakeCube("EotKeypadPanel", parent, SteelDark);
            panel.transform.position = new Vector3(0.55f, 11.06f, -25.85f);
            panel.transform.localScale = new Vector3(0.30f, 0.06f, 0.20f);
            StripCollider(panel);

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    GameObject btn = MakeCylinder($"EotBtn_{x}_{y}", parent, y == 0 ? ButtonRed : ButtonAmber);
                    btn.transform.position = new Vector3(0.50f + x * 0.05f, 11.10f, -25.90f + y * 0.06f);
                    btn.transform.localScale = new Vector3(0.025f, 0.015f, 0.025f);
                    StripCollider(btn);
                }
            }

            // Mirror panel on the left side too
            GameObject panelL = MakeCube("EotKeypadPanelLeft", parent, SteelDark);
            panelL.transform.position = new Vector3(-0.55f, 11.06f, -25.85f);
            panelL.transform.localScale = new Vector3(0.30f, 0.06f, 0.20f);
            StripCollider(panelL);
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    GameObject btn = MakeCylinder($"HelmBtn_{x}_{y}", parent, y == 0 ? ButtonAmber : ButtonRed);
                    btn.transform.position = new Vector3(-0.60f + x * 0.05f, 11.10f, -25.90f + y * 0.06f);
                    btn.transform.localScale = new Vector3(0.025f, 0.015f, 0.025f);
                    StripCollider(btn);
                }
            }
        }

        // Captain's swivel chair primitive behind the player POV — adds
        // peripheral context.
        private static void BuildCaptainsChair(Transform parent)
        {
            GameObject baseCol = MakeCylinder("ChairBase", parent, SteelLight);
            baseCol.transform.position = new Vector3(2.5f, 10.34f, -28.70f);
            baseCol.transform.localScale = new Vector3(0.30f, 0.04f, 0.30f);
            StripCollider(baseCol);

            GameObject post = MakeCylinder("ChairPost", parent, SteelLight);
            post.transform.position = new Vector3(2.5f, 10.55f, -28.70f);
            post.transform.localScale = new Vector3(0.06f, 0.32f, 0.06f);
            StripCollider(post);

            GameObject seat = MakeCube("ChairSeat", parent, ChairLeather);
            seat.transform.position = new Vector3(2.5f, 10.95f, -28.70f);
            seat.transform.localScale = new Vector3(0.50f, 0.10f, 0.50f);
            StripCollider(seat);

            GameObject back = MakeCube("ChairBack", parent, ChairLeather);
            back.transform.position = new Vector3(2.5f, 11.32f, -28.95f);
            back.transform.localScale = new Vector3(0.50f, 0.65f, 0.10f);
            StripCollider(back);
        }

        // Long flat plane forward of the cabin representing the ship's
        // foredeck so the ocean view has scale + a horizon reference.
        // Plus side rails.
        private static void BuildShipBow(Transform parent)
        {
            GameObject deck = MakeCube("ShipBowDeck", parent, ShipDeckColor);
            deck.transform.position = new Vector3(0f, 9.70f, -19.0f);
            deck.transform.localScale = new Vector3(8.0f, 0.10f, 12.0f);
            StripCollider(deck);

            GameObject railL = MakeCube("BowRailLeft", parent, RailColor);
            railL.transform.position = new Vector3(-3.95f, 10.0f, -19.0f);
            railL.transform.localScale = new Vector3(0.05f, 0.50f, 12.0f);
            StripCollider(railL);

            GameObject railR = MakeCube("BowRailRight", parent, RailColor);
            railR.transform.position = new Vector3(3.95f, 10.0f, -19.0f);
            railR.transform.localScale = new Vector3(0.05f, 0.50f, 12.0f);
            StripCollider(railR);

            GameObject mast = MakeCube("BowMast", parent, RailColor);
            mast.transform.position = new Vector3(0f, 11.5f, -15.0f);
            mast.transform.localScale = new Vector3(0.10f, 3.0f, 0.10f);
            StripCollider(mast);
        }

        // ----- factories -----

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

        private static void ApplyColor(GameObject g, Color color)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r == null) return;
            Shader urp = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(urp) { name = g.name + "_mat" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.30f);
            r.sharedMaterial = mat;
        }

        private static void EnableEmission(GameObject g, Color emission)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r == null || r.sharedMaterial == null) return;
            Material m = r.sharedMaterial;
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
        }

        private static void StripCollider(GameObject g)
        {
            Collider col = g.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
    }
}
#endif
