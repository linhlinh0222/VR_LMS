#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaritimeLMS.LessonsEditor
{
    /// <summary>
    /// Phase 34 — build a fully enclosed primitive bridge cabin so the
    /// player feels they are inside an actual room: solid floor + carpet,
    /// solid ceiling, solid back + side walls, lower front bulkhead
    /// (under the windows), upper front bulkhead (above the windows),
    /// and a window-glass plane behind the existing window mullions.
    /// </summary>
    /// <remarks>
    /// Uses the Phase 18 cabin bounds (Y=10.23 floor, Y=13.22 ceiling,
    /// X=±4, Z=-30.5..-25.5). All shell parts are parented under
    /// <c>BridgeCabin/CabinShell</c> so the layer is wipe-and-rebuild
    /// safe. Walls have stripped colliders so they don't fight the
    /// player CharacterController; the camera collider walks around the
    /// cabin floor freely.
    /// </remarks>
    public static class CabinShellEditor
    {
        private const string MenuExecute = "Tools/Maritime LMS/Build Cabin Shell — Execute";
        private const string ShellRootName = "CabinShell";

        // Cabin dimensions (world space).
        private const float FloorY = 10.23f;
        private const float CeilingY = 13.22f;
        private const float HalfWidth = 4.0f;     // X = ±4
        private const float BackZ = -30.50f;      // back wall Z
        private const float FrontZ = -25.50f;     // front wall Z (windows)
        private const float WindowBottomY = 11.65f;
        private const float WindowTopY = 12.95f;

        private static readonly Color FloorColor = new Color(0.16f, 0.13f, 0.10f);   // dark wood
        private static readonly Color CeilingColor = new Color(0.85f, 0.83f, 0.80f); // off-white
        private static readonly Color WallCream = new Color(0.78f, 0.71f, 0.58f);    // cream wood panel
        private static readonly Color BulkheadGray = new Color(0.30f, 0.30f, 0.32f); // industrial gray under windows
        private static readonly Color WindowGlass = new Color(0.50f, 0.65f, 0.80f, 0.20f); // tinted glass

        [MenuItem(MenuExecute)]
        public static void Execute() => Run();

        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogError("[Maritime LMS] Cabin shell: no scene."); return; }

            GameObject cabin = GameObject.Find("BridgeCabin");
            if (cabin == null) { Debug.LogError("[Maritime LMS] BridgeCabin not in scene."); return; }

            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Cabin Shell");
            try
            {
                Transform existing = cabin.transform.Find(ShellRootName);
                if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

                GameObject root = new GameObject(ShellRootName);
                Undo.RegisterCreatedObjectUndo(root, "CabinShell root");
                root.transform.SetParent(cabin.transform, false);

                BuildFloor(root.transform);
                BuildCeiling(root.transform);
                BuildBackWall(root.transform);
                BuildSideWalls(root.transform);
                BuildFrontBulkheads(root.transform);
                BuildWindowGlass(root.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("<color=cyan>[Maritime LMS]</color> Cabin shell built.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Maritime LMS] Cabin shell failed: {ex}");
            }
            finally { Undo.CollapseUndoOperations(undo); }
        }

        // 8 m × 5 m floor at Y = 10.23. The carpet plane from Phase 31
        // sits 0.02 m above this so it reads as carpet on top of deck.
        private static void BuildFloor(Transform parent)
        {
            GameObject g = MakeCube("ShellFloor", parent, FloorColor);
            g.transform.position = new Vector3(0f, FloorY - 0.05f, (BackZ + FrontZ) * 0.5f);
            g.transform.localScale = new Vector3(HalfWidth * 2f, 0.10f, FrontZ - BackZ);
            StripCollider(g);
        }

        // Ceiling at Y = 13.22.
        private static void BuildCeiling(Transform parent)
        {
            GameObject g = MakeCube("ShellCeiling", parent, CeilingColor);
            g.transform.position = new Vector3(0f, CeilingY + 0.05f, (BackZ + FrontZ) * 0.5f);
            g.transform.localScale = new Vector3(HalfWidth * 2f, 0.10f, FrontZ - BackZ);
            StripCollider(g);
        }

        // Back wall at Z = -30.5.
        private static void BuildBackWall(Transform parent)
        {
            GameObject g = MakeCube("ShellWallBack", parent, WallCream);
            g.transform.position = new Vector3(0f, (FloorY + CeilingY) * 0.5f, BackZ - 0.05f);
            g.transform.localScale = new Vector3(HalfWidth * 2f, CeilingY - FloorY, 0.10f);
            StripCollider(g);
        }

        // Port + starboard side walls.
        private static void BuildSideWalls(Transform parent)
        {
            GameObject port = MakeCube("ShellWallPort", parent, WallCream);
            port.transform.position = new Vector3(-HalfWidth - 0.05f, (FloorY + CeilingY) * 0.5f, (BackZ + FrontZ) * 0.5f);
            port.transform.localScale = new Vector3(0.10f, CeilingY - FloorY, FrontZ - BackZ);
            StripCollider(port);

            GameObject sb = MakeCube("ShellWallStarboard", parent, WallCream);
            sb.transform.position = new Vector3(HalfWidth + 0.05f, (FloorY + CeilingY) * 0.5f, (BackZ + FrontZ) * 0.5f);
            sb.transform.localScale = new Vector3(0.10f, CeilingY - FloorY, FrontZ - BackZ);
            StripCollider(sb);
        }

        // Lower bulkhead beneath the front windows + upper bulkhead above
        // the windows. Together with the windowed strip in between, these
        // cap the front of the cabin.
        private static void BuildFrontBulkheads(Transform parent)
        {
            // Lower (floor → window bottom)
            GameObject lower = MakeCube("ShellFrontLower", parent, BulkheadGray);
            float lowerCenterY = (FloorY + WindowBottomY) * 0.5f;
            float lowerHeight = WindowBottomY - FloorY;
            lower.transform.position = new Vector3(0f, lowerCenterY, FrontZ + 0.05f);
            lower.transform.localScale = new Vector3(HalfWidth * 2f, lowerHeight, 0.10f);
            StripCollider(lower);

            // Upper (window top → ceiling)
            GameObject upper = MakeCube("ShellFrontUpper", parent, WallCream);
            float upperCenterY = (WindowTopY + CeilingY) * 0.5f;
            float upperHeight = CeilingY - WindowTopY;
            upper.transform.position = new Vector3(0f, upperCenterY, FrontZ + 0.05f);
            upper.transform.localScale = new Vector3(HalfWidth * 2f, upperHeight, 0.10f);
            StripCollider(upper);
        }

        // Tinted glass plane covering the window opening. Mostly
        // transparent so the ocean shows through; shaded slightly blue.
        private static void BuildWindowGlass(Transform parent)
        {
            GameObject g = MakeCube("ShellWindowGlass", parent, WindowGlass);
            float winCenterY = (WindowBottomY + WindowTopY) * 0.5f;
            float winHeight = WindowTopY - WindowBottomY;
            g.transform.position = new Vector3(0f, winCenterY, FrontZ + 0.02f);
            g.transform.localScale = new Vector3(HalfWidth * 2f - 0.20f, winHeight, 0.02f);
            StripCollider(g);

            Renderer r = g.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                Material m = r.sharedMaterial;
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // transparent
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
            }
        }

        private static GameObject MakeCube(string name, Transform parent, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.20f);
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }

        private static void StripCollider(GameObject g)
        {
            Collider col = g.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
    }
}
#endif
