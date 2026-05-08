using UnityEngine;
using UnityEngine.Events;

namespace MaritimeLMS.Vhf
{
    /// <summary>
    /// VHF marine radio with DSC (Digital Selective Calling) per
    /// GMDSS / SOLAS Chapter IV. Manages channel selection, PTT
    /// (push-to-talk) state, and the 3-second hold required to send a
    /// DSC distress alert.
    /// </summary>
    /// <remarks>
    /// Channel range follows the international VHF marine band: 1-88
    /// (61, 64, 76, 77, 79 reserved; channel 16 is the international
    /// distress / calling channel; channel 70 is DSC). Validation here
    /// stays simple — clamps to [1, 88]; lesson scripts can apply
    /// stricter bin filtering if needed.
    ///
    /// The 3-second distress hold matches IMO MSC.1/Circ.1364 procedure:
    /// the lift-the-cover plus 3-second hold prevents accidental
    /// distress transmissions.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/VHF Radio")]
    public sealed class VhfRadio : MonoBehaviour
    {
        public const int InternationalDistressChannel = 16;
        public const int DscDataChannel = 70;
        public const int MinChannel = 1;
        public const int MaxChannel = 88;

        [Header("Initial state")]
        [SerializeField, Range(1, 88)] private int initialChannel = InternationalDistressChannel;
        [SerializeField, Range(0f, 1f)] private float initialVolume = 0.5f;
        [SerializeField] private bool startPoweredOn = true;

        [Header("Distress hold")]
        [Tooltip("Seconds the user must hold the distress button before the alert fires (IMO MSC.1/Circ.1364).")]
        [SerializeField, Range(1f, 10f)] private float distressHoldSeconds = 3f;

        [Header("Events")]
        public UnityEvent<int> ChannelChanged;
        public UnityEvent<bool> PttStateChanged;
        public UnityEvent<bool> DualWatchStateChanged;
        public UnityEvent<bool> ScanStateChanged;
        public UnityEvent<bool> PowerStateChanged;
        public UnityEvent DistressAlertSent;
        public UnityEvent DistressHoldStarted;
        public UnityEvent DistressHoldCancelled;
        public UnityEvent<float> VolumeChanged;

        private int _channel;
        private float _volume;
        private bool _poweredOn;
        private bool _pttHeld;
        private bool _dualWatch;
        private bool _scanning;
        private bool _distressHolding;
        private float _distressHoldElapsed;

        // --- Public API: state queries -----------------------------------

        public int CurrentChannel => _channel;
        public float Volume => _volume;
        public bool IsPoweredOn => _poweredOn;
        public bool IsPttHeld => _pttHeld;
        public bool IsDualWatchActive => _dualWatch;
        public bool IsScanning => _scanning;
        public bool IsDistressHolding => _distressHolding;
        public float DistressHoldProgress01 =>
            distressHoldSeconds > 0f ? Mathf.Clamp01(_distressHoldElapsed / distressHoldSeconds) : 0f;

        // --- Public API: power -------------------------------------------

        public void PowerOn() => SetPower(true);
        public void PowerOff() => SetPower(false);

        // --- Public API: channel -----------------------------------------

        public void SetChannel(int channel)
        {
            if (!_poweredOn) return;
            int clamped = Mathf.Clamp(channel, MinChannel, MaxChannel);
            if (clamped == _channel) return;
            _channel = clamped;
            ChannelChanged?.Invoke(_channel);
        }

        public void ChannelUp() => SetChannel(_channel + 1);
        public void ChannelDown() => SetChannel(_channel - 1);

        /// <summary>SOLAS quick-key for the international distress / calling channel.</summary>
        public void QuickChannel16() => SetChannel(InternationalDistressChannel);

        // --- Public API: volume ------------------------------------------

        public void SetVolume(float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(clamped, _volume)) return;
            _volume = clamped;
            VolumeChanged?.Invoke(_volume);
        }

        // --- Public API: PTT ---------------------------------------------

        /// <summary>Driver pressed the PTT button. Transmission is allowed only while powered.</summary>
        public void BeginPtt()
        {
            if (!_poweredOn || _pttHeld) return;
            _pttHeld = true;
            PttStateChanged?.Invoke(true);
        }

        public void EndPtt()
        {
            if (!_pttHeld) return;
            _pttHeld = false;
            PttStateChanged?.Invoke(false);
        }

        // --- Public API: DSC distress (3-sec hold) -----------------------

        public void BeginDistressHold()
        {
            if (!_poweredOn || _distressHolding) return;
            _distressHolding = true;
            _distressHoldElapsed = 0f;
            DistressHoldStarted?.Invoke();
        }

        public void EndDistressHold()
        {
            if (!_distressHolding) return;
            bool reachedFullHold = _distressHoldElapsed >= distressHoldSeconds;
            _distressHolding = false;
            _distressHoldElapsed = 0f;
            if (!reachedFullHold) DistressHoldCancelled?.Invoke();
        }

        // --- Public API: dual watch / scan -------------------------------

        public void ToggleDualWatch()
        {
            if (!_poweredOn) return;
            _dualWatch = !_dualWatch;
            DualWatchStateChanged?.Invoke(_dualWatch);
        }

        public void ToggleScan()
        {
            if (!_poweredOn) return;
            _scanning = !_scanning;
            ScanStateChanged?.Invoke(_scanning);
        }

        // --- Lifecycle ---------------------------------------------------

        private void Awake()
        {
            _channel = Mathf.Clamp(initialChannel, MinChannel, MaxChannel);
            _volume = Mathf.Clamp01(initialVolume);
            _poweredOn = startPoweredOn;
        }

        private void Update()
        {
            if (!_distressHolding) return;
            _distressHoldElapsed += Time.deltaTime;
            if (_distressHoldElapsed >= distressHoldSeconds)
            {
                // Hold reached — fire the alert. Caller still needs to lift to reset.
                _distressHolding = false;
                _distressHoldElapsed = 0f;
                DistressAlertSent?.Invoke();
            }
        }

        // --- Internals ---------------------------------------------------

        private void SetPower(bool on)
        {
            if (on == _poweredOn) return;
            _poweredOn = on;
            if (!on)
            {
                // Power off cancels in-flight transmissions / holds / scans.
                if (_pttHeld) { _pttHeld = false; PttStateChanged?.Invoke(false); }
                if (_distressHolding) EndDistressHold();
                if (_scanning) { _scanning = false; ScanStateChanged?.Invoke(false); }
                if (_dualWatch) { _dualWatch = false; DualWatchStateChanged?.Invoke(false); }
            }
            PowerStateChanged?.Invoke(on);
        }
    }
}
