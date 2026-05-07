using System;
using MaritimeLMS.Bridge;
using UnityEngine;

namespace MaritimeLMS.Compass
{
    /// <summary>
    /// Marine binnacle compass: rotates the floating card opposite to vessel heading,
    /// applies liquid-damped follow plus subtle sea-state wobble.
    /// </summary>
    /// <remarks>
    /// Card pivots on local Z axis (per the source FBX hierarchy, see
    /// <c>compass_card_pivot</c> in 03_Magnetic_Compass/README.md). Rotation is the
    /// negative of vessel heading so the card's North marker stays fixed in world
    /// space while the lubber line indicates current course. Wobble simulates the
    /// liquid-mounted card response to swell — magnitudes per typical fluxgate
    /// binnacle damping coefficients.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Magnetic Compass")]
    public sealed class MagneticCompass : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Empty transform under the bowl whose local Z rotation drives the card. " +
                 "From source FBX: MagneticCompass_root/Compass/compass_card_pivot.")]
        [SerializeField] private Transform cardPivot;

        [Tooltip("Optional explicit heading source. If null, the first IHeadingProvider " +
                 "found in the scene is used at Awake.")]
        [SerializeField] private MonoBehaviour headingProviderBehaviour;

        [Header("Damping")]
        [Tooltip("How quickly the card settles to target heading. Higher = stiffer.")]
        [SerializeField, Range(0.5f, 20f)] private float swingDamping = 5f;

        [Header("Sea-state wobble")]
        [SerializeField, Range(0f, 5f)] private float wobbleAmplitudeDegrees = 1f;
        [SerializeField, Range(0.05f, 3f)] private float wobbleFrequencyHz = 0.5f;

        private IHeadingProvider _headingProvider;
        private float _currentCardAngle;
        private float _wobblePhase;

        /// <summary>Heading the lubber line currently shows, [0, 360).</summary>
        public float HeadingDegrees => Mathf.Repeat(360f - _currentCardAngle, 360f);

        /// <summary>Eight-point compass cardinal label of <see cref="HeadingDegrees"/>.</summary>
        public string CardinalDirection => ToCardinal(HeadingDegrees);

        private void Awake()
        {
            if (cardPivot == null)
            {
                Debug.LogError($"{nameof(MagneticCompass)} on '{name}' has no cardPivot wired.", this);
                enabled = false;
                return;
            }

            _headingProvider = ResolveHeadingProvider();
            if (_headingProvider == null)
            {
                Debug.LogWarning($"{nameof(MagneticCompass)} on '{name}' could not resolve an " +
                                 $"IHeadingProvider. Compass will hold North until one is wired.", this);
            }
        }

        private void Update()
        {
            float targetAngle = _headingProvider != null
                ? -_headingProvider.HeadingDegrees
                : 0f;

            // Liquid-damped follow. LerpAngle handles the 360° wrap.
            _currentCardAngle = Mathf.LerpAngle(
                _currentCardAngle,
                targetAngle,
                Time.deltaTime * swingDamping);

            _wobblePhase += Time.deltaTime * wobbleFrequencyHz * Mathf.PI * 2f;
            float wobble = Mathf.Sin(_wobblePhase) * wobbleAmplitudeDegrees;

            cardPivot.localRotation = Quaternion.Euler(0f, 0f, _currentCardAngle + wobble);
        }

        private IHeadingProvider ResolveHeadingProvider()
        {
            if (headingProviderBehaviour is IHeadingProvider explicitProvider)
            {
                return explicitProvider;
            }

            // Auto-discover. Prefer enabled providers; fall back to any.
            var providers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in providers)
            {
                if (mb is IHeadingProvider provider && mb.isActiveAndEnabled)
                    return provider;
            }
            return null;
        }

        private static string ToCardinal(float headingDegrees)
        {
            // 8-point compass; bins span 45° centered on each cardinal.
            // N covers [337.5, 360) ∪ [0, 22.5).
            if (headingDegrees < 22.5f || headingDegrees >= 337.5f) return "N";
            if (headingDegrees < 67.5f) return "NE";
            if (headingDegrees < 112.5f) return "E";
            if (headingDegrees < 157.5f) return "SE";
            if (headingDegrees < 202.5f) return "S";
            if (headingDegrees < 247.5f) return "SW";
            if (headingDegrees < 292.5f) return "W";
            return "NW";
        }

        private void OnValidate()
        {
            if (headingProviderBehaviour != null && headingProviderBehaviour is not IHeadingProvider)
            {
                Debug.LogWarning(
                    $"{nameof(headingProviderBehaviour)} must implement {nameof(IHeadingProvider)}; clearing.",
                    this);
                headingProviderBehaviour = null;
            }
        }
    }
}
