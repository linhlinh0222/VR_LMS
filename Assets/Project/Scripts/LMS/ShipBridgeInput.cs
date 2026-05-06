using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MaritimeLMS
{
    /// <summary>
    /// Bridges the gap between physical bridge interactables and the ship control logic.
    /// Now includes cinematic effects management.
    /// </summary>
    public class ShipBridgeInput : MonoBehaviour
    {
        [Header("Interactable Controls")]
        public DesktopLeverInteractable telegraph;
        public DesktopLeverInteractable helmWheel;
        
        [Header("Target Systems")]
        public ShipController ship;

        [Header("Telegraph Response")]
        [SerializeField, Range(0f, 1f)] private float telegraphDeadband = 0.18f;
        [SerializeField, Range(-0.5f, 0f)] private float asternEnginePower = -0.35f;
        [SerializeField, Range(0f, 1f)] private float aheadEnginePower = 0.65f;
        [SerializeField] private bool enableDesktopKeyboardFallback = true;
        [SerializeField] private float telegraphAsternAngle = -55f;
        [SerializeField] private float telegraphStopAngle = 0f;
        [SerializeField] private float telegraphAheadAngle = 55f;

        [Header("Cinematic Effects")]
        public Volume globalVolume;
        private ColorAdjustments _colorAdjustments;

        public float TelegraphCommand { get; private set; }

        private void Start()
        {
            if (globalVolume != null && globalVolume.profile.TryGet(out _colorAdjustments))
            {
                // Init effects
            }
        }

        private void Update()
        {
            if (ship == null) return;

            ApplyDesktopKeyboardFallback();
            ProcessTelegraphInput();
            ProcessHelmInput();
            UpdateCinematicEffects();
        }

        private void ApplyDesktopKeyboardFallback()
        {
            if (!enableDesktopKeyboardFallback || telegraph == null || telegraph.IsGrabbed)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                telegraph.SetTargetAngle(telegraphAheadAngle);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                telegraph.SetTargetAngle(telegraphAsternAngle);
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                telegraph.SetTargetAngle(telegraphStopAngle);
            }
        }

        private void ProcessTelegraphInput()
        {
            if (telegraph == null) return;
            TelegraphCommand = GetTelegraphCommand();
            ship.SetEnginePower(TelegraphCommand);
        }

        private float GetTelegraphCommand()
        {
            float signedValue = (telegraph.NormalizedValue - 0.5f) * 2f;
            if (signedValue > telegraphDeadband)
            {
                return aheadEnginePower;
            }

            if (signedValue < -telegraphDeadband)
            {
                return asternEnginePower;
            }

            return 0f;
        }

        private void ProcessHelmInput()
        {
            if (helmWheel == null) return;
            float steeringInput = (helmWheel.NormalizedValue - 0.5f) * 2f;
            ship.SetSteering(steeringInput);
        }

        private void UpdateCinematicEffects()
        {
            if (_colorAdjustments == null || ship == null) return;

            // Subtle vignette or color saturation shift based on speed for SOTA feel
            float speedRatio = Mathf.Abs(ship.CurrentSpeed) / ship.maxSpeed;
            _colorAdjustments.saturation.value = Mathf.Lerp(0, 10, speedRatio);
        }
    }
}
