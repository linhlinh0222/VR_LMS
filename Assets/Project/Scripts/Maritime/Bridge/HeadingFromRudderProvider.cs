using UnityEngine;

namespace MaritimeLMS.Bridge
{
    /// <summary>
    /// Heading provider that integrates a rudder input over time, simulating
    /// vessel turn dynamics for steering training scenarios.
    /// </summary>
    /// <remarks>
    /// Heading rate of turn (RoT) at full rudder is configurable; for a
    /// Handysize bulk carrier at sea speed ITTC Maneuvering Group reports
    /// ~0.5 deg/sec at 35° rudder. The default 5 deg/sec exaggerates this so
    /// learners see a responsive compass during desktop sim. Production
    /// scenarios should tune <see cref="maxRotationDegPerSec"/> per vessel.
    ///
    /// This adapter is the first concrete payoff of the Phase 1
    /// <see cref="IHeadingProvider"/> port: <c>MagneticCompass</c> can swap
    /// from <c>CameraHeadingProvider</c> to this without any equipment-side
    /// change.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Heading From Rudder Provider")]
    public sealed class HeadingFromRudderProvider : MonoBehaviour, IHeadingProvider
    {
        [Tooltip("MonoBehaviour implementing IRudderInput. If null, the first IRudderInput in the scene is used.")]
        [SerializeField] private MonoBehaviour rudderInputBehaviour;

        [Tooltip("Initial heading at start, degrees. 0 = North.")]
        [SerializeField, Range(0f, 360f)] private float initialHeadingDegrees = 0f;

        [Tooltip("Heading rate of turn at full rudder, degrees per second. 5 is responsive for desktop sim.")]
        [SerializeField, Range(0.1f, 20f)] private float maxRotationDegPerSec = 5f;

        private IRudderInput _rudder;
        private float _heading;

        public float HeadingDegrees => _heading;

        private void Awake()
        {
            _heading = initialHeadingDegrees;
            _rudder = ResolveRudderInput();
            if (_rudder == null)
            {
                Debug.LogWarning(
                    $"{nameof(HeadingFromRudderProvider)} on '{name}' could not find an IRudderInput. " +
                    $"Heading will hold at {_heading:F1}° until one is wired.", this);
            }
        }

        private void Update()
        {
            if (_rudder == null) return;

            float rate = Mathf.Clamp(_rudder.RudderNormalized, -1f, 1f) * maxRotationDegPerSec;
            _heading = Mathf.Repeat(_heading + rate * Time.deltaTime, 360f);
        }

        /// <summary>Force the heading (degrees) immediately. Useful for lesson resets.</summary>
        public void SetHeading(float degrees)
        {
            _heading = Mathf.Repeat(degrees, 360f);
        }

        private IRudderInput ResolveRudderInput()
        {
            if (rudderInputBehaviour is IRudderInput explicitInput)
                return explicitInput;

            // Auto-discover an enabled IRudderInput on any active MonoBehaviour.
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IRudderInput input && mb.isActiveAndEnabled)
                    return input;
            }
            return null;
        }

        private void OnValidate()
        {
            if (rudderInputBehaviour != null && rudderInputBehaviour is not IRudderInput)
            {
                Debug.LogWarning(
                    $"{nameof(rudderInputBehaviour)} must implement {nameof(IRudderInput)}; clearing.", this);
                rudderInputBehaviour = null;
            }
        }
    }
}
