using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class DesktopMockVRHandVisibility : MonoBehaviour
{
    [SerializeField] private Transform renderRoot;
    [SerializeField] private Material priorityOverlayMaterial;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask occlusionLayers = ~0;
    [SerializeField] private float cameraRayRadius = 0.025f;
    [SerializeField] private float nearSurfaceRadius = 0.08f;
    [SerializeField] private float fadeSpeed = 18f;
    [SerializeField, Range(0f, 1f)] private float occludedAlpha = 0.42f;
    [SerializeField, Range(0f, 1f)] private float nearSurfaceAlpha = 0.26f;

    private const string OverlayRootName = "__HandPriorityOverlay";
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

    private readonly List<Renderer> _sourceRenderers = new List<Renderer>();
    private readonly List<Renderer> _overlayRenderers = new List<Renderer>();
    private readonly RaycastHit[] _rayHits = new RaycastHit[16];
    private readonly Collider[] _overlapHits = new Collider[16];
    private MaterialPropertyBlock _propertyBlock;
    private Transform _overlayRoot;
    private float _alpha;
    private float _priorityHint;

    private void Awake()
    {
        if (renderRoot == null)
        {
            Transform candidate = transform.Find("RenderRoot");
            renderRoot = candidate != null ? candidate : transform;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main != null ? Camera.main : GetComponentInParent<Camera>();
        }

        _propertyBlock = new MaterialPropertyBlock();
        CacheSourceRenderers();
        BuildOverlayRenderers();
        ApplyOverlayAlpha(0f);
    }

    private void LateUpdate()
    {
        float targetAlpha = 0f;
        if (IsCameraOccluded())
        {
            targetAlpha = Mathf.Max(targetAlpha, occludedAlpha);
        }
        else if (IsNearSurface())
        {
            targetAlpha = Mathf.Max(targetAlpha, nearSurfaceAlpha);
        }

        targetAlpha = Mathf.Max(targetAlpha, _priorityHint);
        _priorityHint = 0f;

        float followT = 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime);
        _alpha = Mathf.Lerp(_alpha, targetAlpha, followT);
        if (_alpha < 0.01f)
        {
            _alpha = 0f;
        }

        ApplyOverlayAlpha(_alpha);
    }

    public void SetPriorityHint(float normalizedAlpha)
    {
        _priorityHint = Mathf.Max(_priorityHint, Mathf.Clamp01(normalizedAlpha));
    }

    private void CacheSourceRenderers()
    {
        _sourceRenderers.Clear();
        Renderer[] renderers = renderRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsInOverlayRoot(renderer.transform))
            {
                continue;
            }

            _sourceRenderers.Add(renderer);
        }
    }

    private void BuildOverlayRenderers()
    {
        _overlayRenderers.Clear();
        if (priorityOverlayMaterial == null)
        {
            return;
        }

        _overlayRoot = transform.Find(OverlayRootName);
        if (_overlayRoot == null)
        {
            _overlayRoot = new GameObject(OverlayRootName).transform;
            _overlayRoot.SetParent(transform, false);
        }

        for (int i = 0; i < _sourceRenderers.Count; i++)
        {
            Renderer source = _sourceRenderers[i];
            if (source is SkinnedMeshRenderer skinnedSource)
            {
                SkinnedMeshRenderer overlay = CreateSkinnedOverlay(skinnedSource, i);
                if (overlay != null)
                {
                    _overlayRenderers.Add(overlay);
                }
            }
        }
    }

    private SkinnedMeshRenderer CreateSkinnedOverlay(SkinnedMeshRenderer source, int index)
    {
        Transform existing = _overlayRoot.Find(source.name + "_Overlay");
        GameObject overlayObject = existing != null
            ? existing.gameObject
            : new GameObject(source.name + "_Overlay");

        overlayObject.transform.SetParent(_overlayRoot, false);
        overlayObject.layer = source.gameObject.layer;

        SkinnedMeshRenderer overlay = overlayObject.GetComponent<SkinnedMeshRenderer>();
        if (overlay == null)
        {
            overlay = overlayObject.AddComponent<SkinnedMeshRenderer>();
        }

        overlay.sharedMesh = source.sharedMesh;
        overlay.rootBone = source.rootBone;
        overlay.bones = source.bones;
        overlay.localBounds = source.localBounds;
        overlay.quality = source.quality;
        overlay.updateWhenOffscreen = true;
        overlay.sharedMaterial = priorityOverlayMaterial;
        overlay.shadowCastingMode = ShadowCastingMode.Off;
        overlay.receiveShadows = false;
        overlay.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        overlay.sortingLayerID = source.sortingLayerID;
        overlay.sortingOrder = source.sortingOrder + 20 + index;
        overlay.enabled = false;
        return overlay;
    }

    private bool IsCameraOccluded()
    {
        if (targetCamera == null || _sourceRenderers.Count == 0)
        {
            return false;
        }

        Bounds bounds = GetSourceBounds();
        Vector3 origin = targetCamera.transform.position;
        Vector3 toHand = bounds.center - origin;
        float distance = toHand.magnitude;
        if (distance <= cameraRayRadius)
        {
            return false;
        }

        Vector3 direction = toHand / distance;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            cameraRayRadius,
            direction,
            _rayHits,
            distance,
            occlusionLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            if (IsValidOccluder(_rayHits[i].collider))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNearSurface()
    {
        if (_sourceRenderers.Count == 0)
        {
            return false;
        }

        Bounds bounds = GetSourceBounds();
        int hitCount = Physics.OverlapSphereNonAlloc(
            bounds.center,
            nearSurfaceRadius,
            _overlapHits,
            occlusionLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            if (IsValidOccluder(_overlapHits[i]))
            {
                return true;
            }
        }

        return false;
    }

    private Bounds GetSourceBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        for (int i = 0; i < _sourceRenderers.Count; i++)
        {
            Renderer renderer = _sourceRenderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private bool IsValidOccluder(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        Transform hitTransform = collider.transform;
        if (hitTransform == transform || hitTransform.IsChildOf(transform))
        {
            return false;
        }

        if (targetCamera != null && (hitTransform == targetCamera.transform || hitTransform.IsChildOf(targetCamera.transform)))
        {
            return false;
        }

        if (collider.GetComponent<CharacterController>() != null)
        {
            return false;
        }

        return true;
    }

    private bool IsInOverlayRoot(Transform candidate)
    {
        return _overlayRoot != null && (candidate == _overlayRoot || candidate.IsChildOf(_overlayRoot));
    }

    private void ApplyOverlayAlpha(float alpha)
    {
        for (int i = 0; i < _overlayRenderers.Count; i++)
        {
            Renderer overlay = _overlayRenderers[i];
            if (overlay == null)
            {
                continue;
            }

            overlay.enabled = alpha > 0f;
            overlay.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(AlphaId, alpha);
            overlay.SetPropertyBlock(_propertyBlock);
        }
    }
}
