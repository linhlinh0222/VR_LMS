using UnityEngine;
using UnityEngine.Rendering;

public sealed class DesktopMockVRHandVisual : MonoBehaviour
{
    [SerializeField] private Transform renderRoot;
    [SerializeField] private Material materialOverride;
    [SerializeField] private bool forceRendererSetup = true;
    [SerializeField] private string renderLayerName = "Default";
    [SerializeField] private AnimationClip openPoseClip;
    [SerializeField] private AnimationClip closedPoseClip;
    [SerializeField] private string bonePrefix = "b_r_";
    [SerializeField] private float openPoseSampleTime;
    [SerializeField] private float closedPoseSampleTime = 2.8f;
    [SerializeField] private float poseFollowSpeed = 24f;
    [SerializeField] private bool disableLegacyGhostAnimation = true;

    private FingerBonePose[] _fingerBones = System.Array.Empty<FingerBonePose>();
    private float _grip;
    private float _targetGrip;

    private struct FingerBonePose
    {
        public Transform Transform;
        public Quaternion OpenRotation;
        public Quaternion ClosedRotation;
    }

    private struct TransformState
    {
        public Transform Transform;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }

    private void Awake()
    {
        if (renderRoot == null)
        {
            Transform candidate = transform.Find("RenderRoot");
            renderRoot = candidate != null ? candidate : transform;
        }

        if (disableLegacyGhostAnimation)
        {
            DisableLegacyAnimation();
        }

        ApplyRendererSetup();
        CacheFingerPoses();
        SetGripImmediate(_targetGrip);
        SetRenderersEnabled(true);
    }

    private void LateUpdate()
    {
        float followT = 1f - Mathf.Exp(-poseFollowSpeed * Time.deltaTime);
        _grip = Mathf.Lerp(_grip, _targetGrip, followT);
        ApplyGrip(_grip);
    }

    public void SetGripTarget(float normalizedGrip)
    {
        _targetGrip = Mathf.Clamp01(normalizedGrip);
    }

    public void SetGripImmediate(float normalizedGrip)
    {
        _targetGrip = Mathf.Clamp01(normalizedGrip);
        _grip = _targetGrip;
        ApplyGrip(_grip);
    }

    private void CacheFingerPoses()
    {
        if (renderRoot == null)
        {
            _fingerBones = System.Array.Empty<FingerBonePose>();
            return;
        }

        Transform[] allTransforms = renderRoot.GetComponentsInChildren<Transform>(true);
        TransformState[] originalStates = CaptureTransformStates(allTransforms);

        System.Collections.Generic.List<FingerBonePose> poses = new System.Collections.Generic.List<FingerBonePose>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform bone = allTransforms[i];
            if (!IsFingerBone(bone.name))
            {
                continue;
            }

            poses.Add(new FingerBonePose
            {
                Transform = bone,
                OpenRotation = bone.localRotation,
                ClosedRotation = bone.localRotation
            });
        }

        AnimationClip resolvedOpenPoseClip = openPoseClip != null ? openPoseClip : closedPoseClip;
        if (resolvedOpenPoseClip != null || closedPoseClip != null)
        {
            RestoreTransformStates(originalStates);
            float openSampleTime = ResolveOpenPoseSampleTime(resolvedOpenPoseClip);
            resolvedOpenPoseClip.SampleAnimation(renderRoot.gameObject, openSampleTime);

            for (int i = 0; i < poses.Count; i++)
            {
                FingerBonePose pose = poses[i];
                pose.OpenRotation = pose.Transform.localRotation;
                poses[i] = pose;
            }

            if (closedPoseClip != null)
            {
                RestoreTransformStates(originalStates);
                float closedSampleTime = Mathf.Clamp(closedPoseSampleTime, 0f, closedPoseClip.length);
                closedPoseClip.SampleAnimation(renderRoot.gameObject, closedSampleTime);

                for (int i = 0; i < poses.Count; i++)
                {
                    FingerBonePose pose = poses[i];
                    pose.ClosedRotation = pose.Transform.localRotation;
                    poses[i] = pose;
                }
            }
        }

        RestoreTransformStates(originalStates);
        _fingerBones = poses.ToArray();
    }

    private float ResolveOpenPoseSampleTime(AnimationClip clip)
    {
        if (clip == null)
        {
            return 0f;
        }

        float sampleTime = openPoseSampleTime < 0f ? 0f : openPoseSampleTime;
        return Mathf.Clamp(sampleTime, 0f, clip.length);
    }

    private TransformState[] CaptureTransformStates(Transform[] transforms)
    {
        TransformState[] states = new TransformState[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            states[i] = new TransformState
            {
                Transform = current,
                LocalPosition = current.localPosition,
                LocalRotation = current.localRotation,
                LocalScale = current.localScale
            };
        }

        return states;
    }

    private void RestoreTransformStates(TransformState[] states)
    {
        for (int i = 0; i < states.Length; i++)
        {
            TransformState state = states[i];
            if (state.Transform == null)
            {
                continue;
            }

            state.Transform.localPosition = state.LocalPosition;
            state.Transform.localRotation = state.LocalRotation;
            state.Transform.localScale = state.LocalScale;
        }
    }

    private bool IsFingerBone(string boneName)
    {
        if (!boneName.StartsWith(bonePrefix, System.StringComparison.Ordinal))
        {
            return false;
        }

        return boneName.Contains("thumb")
            || boneName.Contains("index")
            || boneName.Contains("middle")
            || boneName.Contains("ring")
            || boneName.Contains("pinky");
    }

    private void ApplyGrip(float grip)
    {
        for (int i = 0; i < _fingerBones.Length; i++)
        {
            FingerBonePose pose = _fingerBones[i];
            if (pose.Transform != null)
            {
                pose.Transform.localRotation = Quaternion.Slerp(pose.OpenRotation, pose.ClosedRotation, grip);
            }
        }
    }

    private void DisableLegacyAnimation()
    {
        Behaviour[] behaviours = GetComponents<Behaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour != this && behaviour.GetType().Name.Contains("AnimatedGhostHand"))
            {
                behaviour.enabled = false;
            }
        }

        if (renderRoot != null)
        {
            Animation legacyAnimation = renderRoot.GetComponent<Animation>();
            if (legacyAnimation != null)
            {
                legacyAnimation.enabled = false;
            }
        }
    }

    private void ApplyRendererSetup()
    {
        if (!forceRendererSetup)
        {
            return;
        }

        int renderLayer = LayerMask.NameToLayer(renderLayerName);
        if (renderLayer < 0)
        {
            renderLayer = 0;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.gameObject.layer = renderLayer;
            renderer.forceRenderingOff = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            if (materialOverride != null && !IsInPriorityOverlay(renderer.transform))
            {
                renderer.sharedMaterial = materialOverride;
            }
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = enabled;
        }
    }

    private static bool IsInPriorityOverlay(Transform candidate)
    {
        while (candidate != null)
        {
            if (candidate.name == "__HandPriorityOverlay")
            {
                return true;
            }

            candidate = candidate.parent;
        }

        return false;
    }
}
