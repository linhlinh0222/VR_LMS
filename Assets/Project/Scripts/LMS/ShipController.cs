using UnityEngine;

namespace MaritimeLMS
{
    /// <summary>
    /// Simulates ship dynamics including propulsion and steering with momentum.
    /// </summary>
    public class ShipController : MonoBehaviour
    {
        [Header("Propulsion Settings")]
        [Tooltip("Maximum speed in knots (simulated units/sec)")]
        public float maxSpeed = 15f;
        
        [Tooltip("How fast the ship reaches target speed")]
        public float acceleration = 0.2f;

        [Tooltip("How fast the ship slows down or changes direction after a telegraph command")]
        public float deceleration = 0.7f;
        
        [Tooltip("Optional audio source for engine sound")]
        public AudioSource engineAudio;

        [Header("World Motion")]
        [Tooltip("Move this transform through the world. Keep disabled when the player rig is parented to the ship.")]
        [SerializeField] private bool moveTransform;

        [Tooltip("Rotate this transform from helm input. Keep disabled when the player rig is parented to the ship.")]
        [SerializeField] private bool rotateTransform;
        
        [Header("Steering Settings")]
        [Tooltip("Maximum turning speed in degrees per second")]
        public float turnSpeed = 5f;
        
        private float _currentSpeed;
        private float _targetSpeedPercent; 
        private float _steeringInput; 

        /// <summary>
        /// Current speed of the ship in simulated knots.
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        private void OnValidate()
        {
            maxSpeed = Mathf.Max(0f, maxSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            deceleration = Mathf.Max(0f, deceleration);
            turnSpeed = Mathf.Max(0f, turnSpeed);
        }

        private void Update()
        {
            ApplyPropulsion();
            ApplySteering();
            UpdateEngineAudio();
        }

        private void ApplyPropulsion()
        {
            float targetSpeed = _targetSpeedPercent * maxSpeed;
            bool slowingDown = Mathf.Abs(targetSpeed) < Mathf.Abs(_currentSpeed)
                || Mathf.Sign(targetSpeed) != Mathf.Sign(_currentSpeed);
            float responseRate = slowingDown ? deceleration : acceleration;

            // Apply momentum-based acceleration/deceleration.
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, responseRate * Time.deltaTime);
            
            if (moveTransform)
            {
                transform.Translate(Vector3.forward * _currentSpeed * Time.deltaTime);
            }
        }

        private void ApplySteering()
        {
            // Ships turn better when moving
            if (rotateTransform)
            {
                float effectiveTurnSpeed = _steeringInput * turnSpeed * (_currentSpeed / maxSpeed);
                transform.Rotate(Vector3.up, effectiveTurnSpeed * Time.deltaTime);
            }
        }

        private void UpdateEngineAudio()
        {
            if (engineAudio == null) return;

            // Map speed to audio pitch and volume
            float speedRatio = Mathf.Abs(_currentSpeed) / maxSpeed;
            engineAudio.pitch = Mathf.Lerp(0.6f, 1.2f, speedRatio);
            engineAudio.volume = Mathf.Lerp(0.2f, 0.6f, speedRatio);
        }

        /// <summary>
        /// Sets the engine power from -0.5 (reverse) to 1.0 (full ahead).
        /// </summary>
        public void SetEnginePower(float percent)
        {
            _targetSpeedPercent = Mathf.Clamp(percent, -0.5f, 1f);
        }

        /// <summary>
        /// Sets the steering input from -1.0 (Port) to 1.0 (Starboard).
        /// </summary>
        public void SetSteering(float input)
        {
            _steeringInput = Mathf.Clamp(input, -1f, 1f);
        }
    }
}
