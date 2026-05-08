using UnityEngine;
using UnityEngine.Events;

namespace MaritimeLMS.Ais
{
    /// <summary>
    /// AIS (Automatic Identification System) Class A transceiver per
    /// SOLAS V/19. Handles power state, alarm state, status/alarm LED
    /// visual feedback, and exposes D-pad navigation events for lesson
    /// scripts.
    /// </summary>
    /// <remarks>
    /// 3rd display device. Per the YAGNI rule recorded in the project
    /// README, common state shared with <see cref="MaritimeLMS.Radar.MarineRadar"/>
    /// and <see cref="MaritimeLMS.Ecdis.ElectronicChartDisplay"/> (power
    /// state, lifecycle) is small enough that a base class would only
    /// save ~5 lines per device. We intentionally keep the three classes
    /// flat for now; if a 4th display device introduces meaningful
    /// duplication, extract <c>BridgeInstrumentDisplay</c> at that
    /// point.
    ///
    /// LED visuals use <c>MaterialPropertyBlock</c> following the
    /// pattern of <c>MarineTelegraphLessonFeedback</c> — no per-LED
    /// material instance is created, so no shared-material leak.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/AIS Transceiver")]
    public sealed class AisTransceiver : MonoBehaviour
    {
        [Header("LED renderers (auto-discovered if null)")]
        [Tooltip("Renderer driven green when powered. From source FBX: led_status.")]
        [SerializeField] private Renderer statusLedRenderer;

        [Tooltip("Renderer driven red while alarming. From source FBX: led_alarm.")]
        [SerializeField] private Renderer alarmLedRenderer;

        [Header("LED colors")]
        [SerializeField] private Color ledOffColor = new(0.06f, 0.06f, 0.06f, 1f);
        [SerializeField] private Color ledStatusOnColor = new(0.10f, 0.85f, 0.20f, 1f);
        [SerializeField] private Color ledAlarmOnColor = new(0.90f, 0.10f, 0.05f, 1f);

        [Tooltip("Emission multiplier when LED is on. Higher = brighter glow under URP.")]
        [SerializeField, Range(0f, 6f)] private float onEmissionIntensity = 1.6f;

        [Header("Alarm pulse")]
        [SerializeField] private bool alarmPulses = true;
        [SerializeField, Range(0.5f, 6f)] private float alarmPulseHz = 2f;

        [Header("Initial state")]
        [SerializeField] private bool startPoweredOn = true;

        [Header("Events")]
        public UnityEvent<bool> PowerStateChanged;
        public UnityEvent<bool> AlarmStateChanged;

        public UnityEvent DpadUpPressed;
        public UnityEvent DpadDownPressed;
        public UnityEvent DpadLeftPressed;
        public UnityEvent DpadRightPressed;
        public UnityEvent OkPressed;
        public UnityEvent BackPressed;
        public UnityEvent MenuPressed;

        private bool _poweredOn;
        private bool _alarming;
        private float _alarmPhase;
        private MaterialPropertyBlock _block;

        // --- Public API -------------------------------------------------

        public bool IsPoweredOn => _poweredOn;
        public bool IsAlarming => _alarming;

        public void PowerOn() => SetPower(true);
        public void PowerOff() => SetPower(false);

        public void TriggerAlarm() => SetAlarm(true);
        public void ClearAlarm() => SetAlarm(false);

        public void PressDpadUp() { if (_poweredOn) DpadUpPressed?.Invoke(); }
        public void PressDpadDown() { if (_poweredOn) DpadDownPressed?.Invoke(); }
        public void PressDpadLeft() { if (_poweredOn) DpadLeftPressed?.Invoke(); }
        public void PressDpadRight() { if (_poweredOn) DpadRightPressed?.Invoke(); }
        public void PressOk() { if (_poweredOn) OkPressed?.Invoke(); }
        public void PressBack() { if (_poweredOn) BackPressed?.Invoke(); }
        public void PressMenu() { if (_poweredOn) MenuPressed?.Invoke(); }

        // --- Lifecycle --------------------------------------------------

        private void Awake()
        {
            AutoDiscoverLedRenderers();
            _block = new MaterialPropertyBlock();
            _poweredOn = startPoweredOn;
            _alarming = false;
            ApplyLeds(forceImmediate: true);
        }

        private void Update()
        {
            if (!_alarming || !alarmPulses) return;
            _alarmPhase += Time.deltaTime * alarmPulseHz * Mathf.PI * 2f;
            ApplyLeds(forceImmediate: false);
        }

        // --- Internals --------------------------------------------------

        private void SetPower(bool on)
        {
            if (on == _poweredOn) return;
            _poweredOn = on;
            if (!on) SetAlarm(false); // alarm clears when device powered down
            PowerStateChanged?.Invoke(on);
            ApplyLeds(forceImmediate: true);
        }

        private void SetAlarm(bool on)
        {
            if (on == _alarming) return;
            _alarming = on && _poweredOn; // can't alarm while off
            _alarmPhase = 0f;
            AlarmStateChanged?.Invoke(_alarming);
            ApplyLeds(forceImmediate: true);
        }

        private void ApplyLeds(bool forceImmediate)
        {
            if (statusLedRenderer != null)
            {
                Color statusColor = _poweredOn ? ledStatusOnColor : ledOffColor;
                ApplyLedColor(statusLedRenderer, statusColor, _poweredOn);
            }

            if (alarmLedRenderer != null)
            {
                if (!_alarming)
                {
                    ApplyLedColor(alarmLedRenderer, ledOffColor, false);
                }
                else
                {
                    // Pulsing alarm: 0.5..1 brightness modulation when alarmPulses, else solid
                    float intensity = alarmPulses
                        ? 0.5f + 0.5f * (Mathf.Sin(_alarmPhase) * 0.5f + 0.5f)
                        : 1f;
                    Color pulsed = ledAlarmOnColor * intensity;
                    pulsed.a = 1f;
                    ApplyLedColor(alarmLedRenderer, pulsed, true);
                }
            }
        }

        private void ApplyLedColor(Renderer r, Color color, bool emit)
        {
            r.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", color);
            _block.SetColor("_EmissionColor", emit ? color * onEmissionIntensity : Color.black);
            r.SetPropertyBlock(_block);
        }

        private void AutoDiscoverLedRenderers()
        {
            if (statusLedRenderer != null && alarmLedRenderer != null) return;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (statusLedRenderer == null && r.gameObject.name == "led_status") statusLedRenderer = r;
                else if (alarmLedRenderer == null && r.gameObject.name == "led_alarm") alarmLedRenderer = r;
            }
        }
    }
}
