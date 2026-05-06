using UnityEngine;

[ExecuteAlways]
public sealed class MarineExteriorAssetBuilder : MonoBehaviour
{
    private const string GeneratedRootName = "Generated Marine Exterior Assets";

    private static Mesh _unitCubeMesh;

    [SerializeField] private bool rebuildInEditMode = true;
    [SerializeField] private Color deckColor = new Color(0.12f, 0.18f, 0.22f, 1f);
    [SerializeField] private Color deckLineColor = new Color(0.92f, 0.86f, 0.28f, 1f);
    [SerializeField] private Color railColor = new Color(0.86f, 0.93f, 0.94f, 1f);
    [SerializeField] private Color equipmentColor = new Color(0.08f, 0.12f, 0.14f, 1f);

    private void OnEnable()
    {
        RebuildExterior();
    }

    private void OnValidate()
    {
        ScheduleEditorRebuild();
    }

    [ContextMenu("Rebuild Marine Exterior")]
    private void RebuildExterior()
    {
        if (!Application.isPlaying && !rebuildInEditMode)
        {
            return;
        }

        Transform existingRoot = transform.Find(GeneratedRootName);
        if (existingRoot != null)
        {
            DestroyAssetOrObject(existingRoot.gameObject);
}

        GameObject root = new GameObject(GeneratedRootName);
        root.hideFlags = HideFlags.DontSave;
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Material deckMaterial = CreateMaterial("Generated Deck Material", deckColor, 0.62f);
        Material deckLineMaterial = CreateMaterial("Generated Deck Line Material", deckLineColor, 0.75f);
        Material railMaterial = CreateMaterial("Generated Rail Material", railColor, 0.7f);
        Material equipmentMaterial = CreateMaterial("Generated Equipment Material", equipmentColor, 0.55f);

        CreateBowDeck(root.transform, deckMaterial);
        CreateDeckLine(root.transform, deckLineMaterial);
        CreateRails(root.transform, railMaterial);
        CreateForwardEquipment(root.transform, equipmentMaterial, railMaterial);
    }

    private void CreateBowDeck(Transform root, Material material)
    {
        Mesh mesh = new Mesh
        {
            name = "Generated Ship Bow Deck Mesh",
            hideFlags = HideFlags.DontSave
        };

        float nearZ = -21.8f;
        float farZ = -3.8f;
        float nearHalfWidth = 2.85f;
        float farHalfWidth = 0.68f;
        float topY = 0.18f;
        float bottomY = -0.22f;

        Vector3[] vertices =
        {
            new Vector3(-nearHalfWidth, topY, nearZ),
            new Vector3(nearHalfWidth, topY, nearZ),
            new Vector3(farHalfWidth, topY, farZ),
            new Vector3(-farHalfWidth, topY, farZ),
            new Vector3(-nearHalfWidth, bottomY, nearZ),
            new Vector3(nearHalfWidth, bottomY, nearZ),
            new Vector3(farHalfWidth, bottomY, farZ),
            new Vector3(-farHalfWidth, bottomY, farZ)
        };

        int[] triangles =
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject deck = new GameObject("Ship Bow Deck");
        deck.hideFlags = HideFlags.DontSave;
        deck.transform.SetParent(root, false);
        deck.transform.localPosition = Vector3.zero;
        deck.transform.localRotation = Quaternion.identity;
        MeshFilter meshFilter = deck.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = deck.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void CreateDeckLine(Transform root, Material material)
    {
        CreateBox(root, "Bow Centerline", new Vector3(0f, 0.205f, -12.8f), new Vector3(0.035f, 0.018f, 14.2f), material);
        CreateBox(root, "Bow Port Seam", new Vector3(-0.72f, 0.208f, -13.5f), new Vector3(0.02f, 0.012f, 11.2f), material);
        CreateBox(root, "Bow Starboard Seam", new Vector3(0.72f, 0.208f, -13.5f), new Vector3(0.02f, 0.012f, 11.2f), material);
    }

    private void CreateRails(Transform root, Material material)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 previousTop = Vector3.zero;
            bool hasPreviousTop = false;

            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                float z = Mathf.Lerp(-21.0f, -4.6f, t);
                float deckHalfWidth = Mathf.Lerp(2.6f, 0.75f, t);
                Vector3 postBase = new Vector3(side * deckHalfWidth, 0.22f, z);
                Vector3 postTop = postBase + Vector3.up * 0.78f;
                CreateBox(root, $"Bow Rail Post {side}_{i}", postBase + Vector3.up * 0.39f, new Vector3(0.045f, 0.78f, 0.045f), material);

                if (hasPreviousTop)
                {
                    CreateBeam(root, $"Bow Top Rail {side}_{i}", previousTop, postTop, 0.045f, material);
                    CreateBeam(root, $"Bow Mid Rail {side}_{i}", previousTop - Vector3.up * 0.34f, postTop - Vector3.up * 0.34f, 0.032f, material);
                }

                previousTop = postTop;
                hasPreviousTop = true;
            }
        }
    }

    private void CreateForwardEquipment(Transform root, Material equipmentMaterial, Material railMaterial)
    {
        CreateBox(root, "Forward Hatch", new Vector3(0f, 0.27f, -8.1f), new Vector3(1.15f, 0.08f, 1.35f), equipmentMaterial);
        CreateBox(root, "Forward Hatch Handle", new Vector3(0f, 0.35f, -8.1f), new Vector3(0.48f, 0.04f, 0.08f), railMaterial);
        CreateBox(root, "Bow Bollard Port", new Vector3(-1.35f, 0.35f, -12.4f), new Vector3(0.24f, 0.28f, 0.24f), equipmentMaterial);
        CreateBox(root, "Bow Bollard Starboard", new Vector3(1.35f, 0.35f, -12.4f), new Vector3(0.24f, 0.28f, 0.24f), equipmentMaterial);
    }

    private GameObject CreateBox(Transform root, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject box = new GameObject(name);
        box.hideFlags = HideFlags.DontSave;
        box.transform.SetParent(root, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = Quaternion.identity;
        box.transform.localScale = localScale;

        MeshFilter meshFilter = box.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = box.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = GetUnitCubeMesh();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        return box;
    }

    private void CreateBeam(Transform root, string name, Vector3 start, Vector3 end, float thickness, Material material)
    {
        Vector3 midpoint = (start + end) * 0.5f;
        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length < 0.001f)
        {
            return;
        }

        GameObject beam = CreateBox(root, name, midpoint, new Vector3(thickness, thickness, length), material);
        beam.transform.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Material CreateMaterial(string name, Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.DontSave
        };

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        return material;
    }

    private static Mesh GetUnitCubeMesh()
    {
        if (_unitCubeMesh != null)
        {
            return _unitCubeMesh;
        }

        Mesh mesh = new Mesh
        {
            name = "Generated Unit Cube Mesh",
            hideFlags = HideFlags.DontSave
        };

        const float half = 0.5f;
        Vector3[] vertices =
        {
            new Vector3(-half, -half, half), new Vector3(half, -half, half), new Vector3(half, half, half), new Vector3(-half, half, half),
            new Vector3(half, -half, -half), new Vector3(-half, -half, -half), new Vector3(-half, half, -half), new Vector3(half, half, -half),
            new Vector3(-half, -half, -half), new Vector3(-half, -half, half), new Vector3(-half, half, half), new Vector3(-half, half, -half),
            new Vector3(half, -half, half), new Vector3(half, -half, -half), new Vector3(half, half, -half), new Vector3(half, half, half),
            new Vector3(-half, half, half), new Vector3(half, half, half), new Vector3(half, half, -half), new Vector3(-half, half, -half),
            new Vector3(-half, -half, -half), new Vector3(half, -half, -half), new Vector3(half, -half, half), new Vector3(-half, -half, half)
        };

        int[] triangles =
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _unitCubeMesh = mesh;
        return _unitCubeMesh;
    }

    private static void DestroyAssetOrObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
{
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private void ScheduleEditorRebuild()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RebuildExteriorFromEditorDelay;
        UnityEditor.EditorApplication.delayCall += RebuildExteriorFromEditorDelay;
#endif
    }

#if UNITY_EDITOR
    private void RebuildExteriorFromEditorDelay()
    {
        UnityEditor.EditorApplication.delayCall -= RebuildExteriorFromEditorDelay;
        if (this == null || Application.isPlaying || !rebuildInEditMode)
        {
            return;
        }

        RebuildExterior();
    }
#endif
}
