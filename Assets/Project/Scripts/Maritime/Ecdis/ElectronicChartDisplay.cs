using UnityEngine;
using UnityEngine.Events;

namespace MaritimeLMS.Ecdis
{
    /// <summary>
    /// Electronic Chart Display and Information System (ECDIS) — chart
    /// display mandated by SOLAS V/19.2.10 for commercial vessels.
    /// </summary>
    /// <remarks>
    /// This pilot focuses on the operator-facing state machine: zoom level,
    /// active chart layer, and power state. Actual chart rendering (S-57 ENC
    /// data, ship marker, route waypoints) is deferred to a follow-up phase
    /// that introduces a RenderTexture + top-down chart camera.
    ///
    /// 2nd display device. After AIS lands as 3rd, common state — power /
    /// brightness — extracts to a <c>BridgeInstrumentDisplay</c> base.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Electronic Chart Display")]
    public sealed class ElectronicChartDisplay : MonoBehaviour
    {
        /// <summary>Active overlay layer on the chart display.</summary>
        public enum ChartLayer
        {
            /// <summary>Vector chart only — bathymetry, coastlines, navaids.</summary>
            Standard = 0,
            /// <summary>Standard + AIS targets overlay.</summary>
            Targets = 1,
            /// <summary>Standard + planned route waypoints + estimated time of arrival.</summary>
            Route = 2,
            /// <summary>Standard + alarm zones + restricted areas.</summary>
            Alarms = 3,
        }

        [Header("Zoom scale (smaller = closer in)")]
        [Tooltip("Orthographic 'half-height' values in meters per click. Standard ECDIS displays " +
                 "let operators step 4-8 zoom levels.")]
        [SerializeField] private float[] zoomHalfHeightsMeters =
        {
            50f, 100f, 250f, 500f, 1000f, 2500f, 5000f, 10000f,
        };

        [SerializeField, Range(0, 8)] private int initialZoomIndex = 3;

        [Header("State")]
        [SerializeField] private bool startPowered = true;
        [SerializeField] private ChartLayer initialLayer = ChartLayer.Standard;

        [Header("Events")]
        public UnityEvent<float> ZoomChanged;
        public UnityEvent<ChartLayer> LayerChanged;
        public UnityEvent<bool> PowerStateChanged;

        private int _zoomIndex;
        private ChartLayer _layer;
        private bool _powered;

        // --- Public API -------------------------------------------------

        /// <summary>Current chart half-height in meters.</summary>
        public float CurrentZoomHalfHeightMeters => zoomHalfHeightsMeters[_zoomIndex];

        /// <summary>0..length-1 zoom step.</summary>
        public int CurrentZoomIndex => _zoomIndex;

        /// <summary>Active overlay layer.</summary>
        public ChartLayer Layer => _layer;

        /// <summary>Whether the display is powered on.</summary>
        public bool IsPowered => _powered;

        public void ZoomIn() => SetZoomIndex(_zoomIndex - 1);
        public void ZoomOut() => SetZoomIndex(_zoomIndex + 1);

        public void SetZoomIndex(int index)
        {
            int clamped = Mathf.Clamp(index, 0, zoomHalfHeightsMeters.Length - 1);
            if (clamped == _zoomIndex) return;
            _zoomIndex = clamped;
            ZoomChanged?.Invoke(CurrentZoomHalfHeightMeters);
        }

        public void SetLayer(ChartLayer layer)
        {
            if (layer == _layer) return;
            _layer = layer;
            LayerChanged?.Invoke(layer);
        }

        public void PowerOn() => SetPower(true);
        public void PowerOff() => SetPower(false);

        // --- Lifecycle --------------------------------------------------

        private void Awake()
        {
            _zoomIndex = Mathf.Clamp(initialZoomIndex, 0, zoomHalfHeightsMeters.Length - 1);
            _layer = initialLayer;
            _powered = startPowered;
        }

        // --- Internals --------------------------------------------------

        private void SetPower(bool on)
        {
            if (on == _powered) return;
            _powered = on;
            PowerStateChanged?.Invoke(on);
        }

        private void OnValidate()
        {
            if (zoomHalfHeightsMeters == null || zoomHalfHeightsMeters.Length == 0)
            {
                zoomHalfHeightsMeters = new[] { 50f, 100f, 250f, 500f, 1000f, 2500f, 5000f, 10000f };
            }
            if (initialZoomIndex >= zoomHalfHeightsMeters.Length) initialZoomIndex = zoomHalfHeightsMeters.Length - 1;
            if (initialZoomIndex < 0) initialZoomIndex = 0;
        }
    }
}
