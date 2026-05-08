using UnityEngine;
using UnityEngine.Events;
using MaritimeLMS.Bridge;

namespace MaritimeLMS.Helm
{
    /// <summary>
    /// Ship's helm (steering wheel). Multi-turn rotation around the wheel
    /// pivot's local Y axis, optional spring-back to midships, exposes
    /// rudder demand to the rest of the bridge via <see cref="IRudderInput"/>.
    /// </summary>
    /// <remarks>
    /// Lock-to-lock travel matches the typical traditional helm: ±360° from
    /// midships (720° total) with 15° rudder per wheel turn. Mapping is
    /// linear: <c>RudderNormalized = TurnAngleDegrees / maxLockDegrees</c>
    /// clamped to [-1, +1].
    ///
    /// Public grab API mirrors <c>DesktopLeverInteractable</c> so the desktop
    /// mouse-grab driver can reuse the same hook. Hand angle is measured on
    /// the wheel-pivot's parent XZ plane (world space projected to local).
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Ship Wheel")]
    public sealed class ShipWheel : MonoBehaviour, IRudderInput
    {
        [Header("Wiring")]
        [Tooltip("Empty transform whose local Y rotation drives the wheel mesh. " +
                 "From source FBX: ShipWheel_root/Wheel/wheel_pivot.")]
        [SerializeField] private Transform wheelPivot;

        [Header("Travel")]
        [Tooltip("Lock-to-lock total travel in degrees. 720 = ±360° from midships (2 full turns).")]
        [SerializeField, Range(180f, 1440f)] private float maxLockDegrees = 720f;

        [Tooltip("Smoothing speed when not grabbed. Higher = stiffer follow.")]
        [SerializeField, Range(1f, 60f)] private float followSpeed = 18f;

        [Header("Spring back")]
        [Tooltip("On release, return wheel to midships. Many real ships do NOT spring back.")]
        [SerializeField] private bool springBackToCenter = false;

        [SerializeField, Range(15f, 360f)] private float springSpeed = 90f;

        [Header("Detection thresholds (events)")]
        [Tooltip("Below this fraction of full rudder, treated as 'midships' for OnMidships.")]
        [SerializeField, Range(0f, 0.2f)] private float midshipsTolerance = 0.05f;

        [Tooltip("Above this fraction of full rudder, treated as hard-over for OnHardPort/Starboard.")]
        [SerializeField, Range(0.5f, 1f)] private float hardOverThreshold = 0.95f;

        [Header("Events")]
        public UnityEvent<float> RudderChanged;
        public UnityEvent HardPort;
        public UnityEvent HardStarboard;
        public UnityEvent Midships;

        private float _currentAngle;
        private float _targetAngle;
        private float _grabAngleOffset;
        private bool _wasMidships;
        private bool _wasHardPort;
        private bool _wasHardStarboard;
        private float _lastReportedRudder = float.NaN;

        /// <summary>Signed accumulated wheel turn in degrees. Negative = port, positive = starboard.</summary>
        public float TurnAngleDegrees => _currentAngle;

        /// <summary>Rudder demand normalized to [-1, +1]. -1 = hard a-port, +1 = hard a-starboard.</summary>
        public float RudderNormalized => Mathf.Clamp(_currentAngle / (maxLockDegrees * 0.5f), -1f, 1f);

        /// <summary>Legacy 0..1 mapping (0 = hard port, 0.5 = midships, 1 = hard starboard).</summary>
        public float NormalizedValue => RudderNormalized * 0.5f + 0.5f;

        /// <summary>Whether a driver is currently turning the wheel.</summary>
        public bool IsGrabbed { get; private set; }

        // --- Public API: programmatic ----------------------------------------

        /// <summary>Set rudder normalized [-1, +1]. Maps to wheel angle.</summary>
        public void SetRudderNormalized(float rudder)
        {
            _targetAngle = Mathf.Clamp(rudder, -1f, 1f) * (maxLockDegrees * 0.5f);
            if (!IsGrabbed)
            {
                _currentAngle = _targetAngle;
                ApplyAngle();
                RaiseEvents();
            }
        }

        /// <summary>Snap wheel back to midships immediately.</summary>
        public void SnapToMidships()
        {
            _targetAngle = 0f;
            _currentAngle = 0f;
            ApplyAngle();
            RaiseEvents();
        }

        // --- Public API: interaction (matches DesktopLeverInteractable) ------

        public void BeginGrab(Vector3 handAnchorWorldPosition)
        {
            IsGrabbed = true;
            _grabAngleOffset = _currentAngle - GetHandAngle(handAnchorWorldPosition);
        }

        public void UpdateGrab(Vector3 handAnchorWorldPosition)
        {
            float candidate = GetHandAngle(handAnchorWorldPosition) + _grabAngleOffset;
            float halfLock = maxLockDegrees * 0.5f;
            _currentAngle = Mathf.Clamp(candidate, -halfLock, halfLock);
            _targetAngle = _currentAngle;
            ApplyAngle();
            RaiseEvents();
        }

        public void EndGrab()
        {
            IsGrabbed = false;
            if (springBackToCenter) _targetAngle = 0f;
        }

        // --- Lifecycle -------------------------------------------------------

        private void Awake()
        {
            if (wheelPivot == null)
            {
                Debug.LogError($"{nameof(ShipWheel)} on '{name}' has no wheelPivot wired.", this);
                enabled = false;
                return;
            }
            _currentAngle = 0f;
            _targetAngle = 0f;
            ApplyAngle();
        }

        private void Update()
        {
            if (IsGrabbed) return;

            float follow;
            if (springBackToCenter && Mathf.Abs(_targetAngle) < 0.01f)
            {
                // Spring-back uses springSpeed for predictable timing.
                _currentAngle = Mathf.MoveTowards(_currentAngle, 0f, springSpeed * Time.deltaTime);
            }
            else
            {
                follow = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
                _currentAngle = Mathf.Lerp(_currentAngle, _targetAngle, follow);
            }
            ApplyAngle();
            RaiseEvents();
        }

        // --- Internals -------------------------------------------------------

        private void ApplyAngle()
        {
            wheelPivot.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
        }

        private void RaiseEvents()
        {
            float rudder = RudderNormalized;
            if (!Mathf.Approximately(rudder, _lastReportedRudder))
            {
                RudderChanged?.Invoke(rudder);
                _lastReportedRudder = rudder;
            }

            bool isMid = Mathf.Abs(rudder) < midshipsTolerance;
            bool isHardPort = rudder < -hardOverThreshold;
            bool isHardStarboard = rudder > hardOverThreshold;

            if (isMid && !_wasMidships) Midships?.Invoke();
            if (isHardPort && !_wasHardPort) HardPort?.Invoke();
            if (isHardStarboard && !_wasHardStarboard) HardStarboard?.Invoke();

            _wasMidships = isMid;
            _wasHardPort = isHardPort;
            _wasHardStarboard = isHardStarboard;
        }

        private float GetHandAngle(Vector3 handAnchorWorldPosition)
        {
            Transform pivotParent = wheelPivot.parent != null ? wheelPivot.parent : wheelPivot;
            Vector3 local = pivotParent.InverseTransformPoint(handAnchorWorldPosition) - wheelPivot.localPosition;
            if (local.sqrMagnitude < 0.0001f) return _currentAngle;
            // Wheel rotates around Y. Project to XZ plane and read the angle.
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            float halfLock = maxLockDegrees * 0.5f;
            _targetAngle = Mathf.Clamp(_targetAngle, -halfLock, halfLock);
        }
    }
}
