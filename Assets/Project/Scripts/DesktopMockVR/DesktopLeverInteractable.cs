using UnityEngine;

public sealed class DesktopLeverInteractable : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private float minAngle = -55f;
    [SerializeField] private float maxAngle = 55f;
    [SerializeField] private float startAngle;
    [SerializeField] private float followSpeed = 18f;
    [SerializeField] private bool snapToDetentsOnRelease;
    [SerializeField] private float[] detentAngles = { -55f, 0f, 55f };
    [SerializeField] private Transform leftHandGripPose;
    [SerializeField] private Transform rightHandGripPose;

    private float _currentAngle;
    private float _targetAngle;
    private float _grabAngleOffset;

    public float CurrentAngle => _currentAngle;
    public float TargetAngle => _targetAngle;
    public float NormalizedValue => Mathf.InverseLerp(minAngle, maxAngle, _currentAngle);
    public bool IsGrabbed { get; private set; }

    private void Awake()
    {
        if (pivot == null)
        {
            pivot = transform;
        }

        _currentAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
        _targetAngle = _currentAngle;
        ApplyAngle(_currentAngle);
    }

    private void LateUpdate()
    {
        float followT = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        _currentAngle = Mathf.Lerp(_currentAngle, _targetAngle, followT);
        ApplyAngle(_currentAngle);
    }

    public void BeginGrab(Vector3 handAnchorWorldPosition)
    {
        IsGrabbed = true;
        _grabAngleOffset = _currentAngle - GetHandAngle(handAnchorWorldPosition);
    }

    public void UpdateGrab(Vector3 handAnchorWorldPosition)
    {
        _targetAngle = Mathf.Clamp(GetHandAngle(handAnchorWorldPosition) + _grabAngleOffset, minAngle, maxAngle);
    }

    public void EndGrab()
    {
        IsGrabbed = false;
        if (snapToDetentsOnRelease && detentAngles != null && detentAngles.Length > 0)
        {
            _targetAngle = GetNearestDetentAngle(_targetAngle);
        }
    }

    public bool TryGetGripPose(bool leftHand, out Vector3 position, out Quaternion rotation)
    {
        Transform gripPose = leftHand ? leftHandGripPose : rightHandGripPose;
        if (gripPose == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = gripPose.position;
        rotation = gripPose.rotation;
        return true;
    }

    public void SetTargetAngle(float angle)
    {
        _targetAngle = Mathf.Clamp(angle, minAngle, maxAngle);
    }

    public void SetAngleImmediate(float angle)
    {
        _currentAngle = Mathf.Clamp(angle, minAngle, maxAngle);
        _targetAngle = _currentAngle;
        ApplyAngle(_currentAngle);
    }

    private float GetHandAngle(Vector3 handAnchorWorldPosition)
    {
        Transform axis = pivot != null ? pivot : transform;
        Transform stableSpace = axis.parent != null ? axis.parent : axis;
        // Measure in the non-rotating parent space so the lever's own rotation
        // does not cancel out the hand angle we are trying to read.
        Vector3 localHand = stableSpace.InverseTransformPoint(handAnchorWorldPosition);
        if (pivot != null && pivot.parent != null)
        {
            localHand -= pivot.localPosition;
        }

        if (localHand.sqrMagnitude < 0.0001f)
        {
            return _currentAngle;
        }

        return Mathf.Atan2(localHand.z, localHand.y) * Mathf.Rad2Deg;
    }

    private void ApplyAngle(float angle)
    {
        if (pivot != null)
        {
            pivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }

    private float GetNearestDetentAngle(float angle)
    {
        float nearest = Mathf.Clamp(detentAngles[0], minAngle, maxAngle);
        float nearestDistance = Mathf.Abs(Mathf.DeltaAngle(angle, nearest));

        for (int i = 1; i < detentAngles.Length; i++)
        {
            float candidate = Mathf.Clamp(detentAngles[i], minAngle, maxAngle);
            float distance = Mathf.Abs(Mathf.DeltaAngle(angle, candidate));
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void OnValidate()
    {
        if (maxAngle < minAngle)
        {
            float oldMin = minAngle;
            minAngle = maxAngle;
            maxAngle = oldMin;
        }

        startAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
        if (snapToDetentsOnRelease && detentAngles != null)
        {
            for (int i = 0; i < detentAngles.Length; i++)
            {
                detentAngles[i] = Mathf.Clamp(detentAngles[i], minAngle, maxAngle);
            }
        }

        if (!Application.isPlaying && pivot != null)
        {
            ApplyAngle(startAngle);
        }
    }
}
