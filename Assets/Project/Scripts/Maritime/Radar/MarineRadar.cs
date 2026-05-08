using UnityEngine;
using UnityEngine.Events;
using MaritimeLMS.Bridge;

namespace MaritimeLMS.Radar
{
    /// <summary>
    /// X-band marine PPI radar. Continuously rotates the sweep arm at the
    /// configured RPM while transmitting, exposes range / standby state,
    /// and reads heading from the bridge IHeadingProvider so the heading
    /// line stays correct after the helm wheel turns the ship.
    /// </summary>
    /// <remarks>
    /// Range steps follow the IMO/IEC 62388 standard scale used on most
    /// marine radars: 0.5 / 1.5 / 3 / 6 / 12 / 24 nautical miles. Sweep RPM
    /// of 30 matches the typical X-band antenna speed.
    ///
    /// This is the first display device. When the second and third
    /// (ECDIS, AIS) land, common state — range knob, brightness, standby
    /// — should be extracted into a <c>BridgeInstrumentDisplay</c> base
    /// per the README roadmap.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Marine Radar")]
    public sealed class MarineRadar : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Empty transform whose local Y rotation drives the sweep arm. " +
                 "From source FBX: Radar_root/Sweep_Animation/sweep_pivot.")]
        [SerializeField] private Transform sweepPivot;

        [Tooltip("Optional heading provider — used for ARPA bearing calculations later. " +
                 "If null, the first IHeadingProvider in the scene is used.")]
        [SerializeField] private MonoBehaviour headingProviderBehaviour;

        [Header("Sweep")]
        [Tooltip("Antenna RPM. X-band radars are typically 24-30; 30 matches the FBX clip.")]
        [SerializeField, Range(6f, 60f)] private float rpm = 30f;

        [Tooltip("Sweep direction looking down at the radar. Most marine radars sweep clockwise.")]
        [SerializeField] private bool sweepClockwise = true;

        [Header("Range scale (nautical miles)")]
        [Tooltip("IMO/IEC 62388 standard range scale used by most marine radars.")]
        [SerializeField] private float[] rangesNm =
        {
            0.5f, 1.5f, 3f, 6f, 12f, 24f,
        };

        [SerializeField, Range(0, 8)] private int initialRangeIndex = 3; // = 6 nm

        [Header("State")]
        [SerializeField] private bool startTransmitting = true;

        [Header("Events")]
        public UnityEvent<float> RangeChanged;
        public UnityEvent<bool> TransmitStateChanged;

        private IHeadingProvider _headingProvider;
        private int _rangeIndex;
        private bool _transmitting;

        // --- Public API --------------------------------------------------

        /// <summary>Current range in nautical miles.</summary>
        public float CurrentRangeNm => rangesNm[_rangeIndex];

        /// <summary>0..length-1 index into the range scale.</summary>
        public int CurrentRangeIndex => _rangeIndex;

        /// <summary>Whether the antenna is rotating (true) or in standby (false).</summary>
        public bool IsTransmitting => _transmitting;

        /// <summary>Sweep arm angle in degrees (informational; LateUpdate keeps it current).</summary>
        public float SweepAngleDegrees { get; private set; }

        /// <summary>True north heading from bound provider, or 0 if none wired.</summary>
        public float ShipHeadingDegrees => _headingProvider?.HeadingDegrees ?? 0f;

        /// <summary>Step up to a longer range; clamps at the longest entry.</summary>
        public void RangeUp() => SetRangeIndex(_rangeIndex + 1);

        /// <summary>Step down to a shorter range; clamps at the shortest entry.</summary>
        public void RangeDown() => SetRangeIndex(_rangeIndex - 1);

        public void SetRangeIndex(int index)
        {
            int clamped = Mathf.Clamp(index, 0, rangesNm.Length - 1);
            if (clamped == _rangeIndex) return;
            _rangeIndex = clamped;
            RangeChanged?.Invoke(CurrentRangeNm);
        }

        public void EnterStandby() => SetTransmitting(false);
        public void EnterTransmit() => SetTransmitting(true);

        // --- Lifecycle ---------------------------------------------------

        private void Awake()
        {
            if (sweepPivot == null)
            {
                AutoDiscoverSweepPivot();
                if (sweepPivot == null)
                {
                    Debug.LogError(
                        $"{nameof(MarineRadar)} on '{name}' could not resolve sweep_pivot.", this);
                    enabled = false;
                    return;
                }
            }
            _rangeIndex = Mathf.Clamp(initialRangeIndex, 0, rangesNm.Length - 1);
            _transmitting = startTransmitting;
            _headingProvider = ResolveHeadingProvider();
        }

        private void Update()
        {
            if (!_transmitting) return;
            float degPerSec = rpm * 360f / 60f;
            float delta = (sweepClockwise ? -1f : 1f) * degPerSec * Time.deltaTime;
            SweepAngleDegrees = Mathf.Repeat(SweepAngleDegrees + delta, 360f);
            sweepPivot.localRotation = Quaternion.Euler(0f, SweepAngleDegrees, 0f);
        }

        // --- Internals ---------------------------------------------------

        private void SetTransmitting(bool on)
        {
            if (on == _transmitting) return;
            _transmitting = on;
            TransmitStateChanged?.Invoke(on);
        }

        private void AutoDiscoverSweepPivot()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "sweep_pivot") { sweepPivot = t; return; }
            }
        }

        private IHeadingProvider ResolveHeadingProvider()
        {
            if (headingProviderBehaviour is IHeadingProvider explicitProvider)
                return explicitProvider;

            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IHeadingProvider p && mb.isActiveAndEnabled) return p;
            }
            return null;
        }

        private void OnValidate()
        {
            if (rangesNm == null || rangesNm.Length == 0)
            {
                rangesNm = new[] { 0.5f, 1.5f, 3f, 6f, 12f, 24f };
            }
            if (initialRangeIndex >= rangesNm.Length) initialRangeIndex = rangesNm.Length - 1;
            if (initialRangeIndex < 0) initialRangeIndex = 0;
            if (headingProviderBehaviour != null && headingProviderBehaviour is not IHeadingProvider)
            {
                Debug.LogWarning(
                    $"{nameof(headingProviderBehaviour)} must implement {nameof(IHeadingProvider)}; clearing.", this);
                headingProviderBehaviour = null;
            }
        }
    }
}
