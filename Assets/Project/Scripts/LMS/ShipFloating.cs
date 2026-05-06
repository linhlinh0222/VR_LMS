using UnityEngine;

namespace MaritimeLMS
{
    public class ShipFloating : MonoBehaviour
    {
        public float amplitude = 0.05f;
        public float frequency = 0.5f;
        public float rollAmplitude = 0.5f;
        public float rollFrequency = 0.3f;

        private Vector3 _startPos;
        private Quaternion _startRot;

        private void Start()
        {
            _startPos = transform.localPosition;
            _startRot = transform.localRotation;
        }

        private void Update()
        {
            float yOffset = Mathf.Sin(Time.time * frequency) * amplitude;
            transform.localPosition = _startPos + new Vector3(0, yOffset, 0);

            float roll = Mathf.Sin(Time.time * rollFrequency) * rollAmplitude;
            float pitch = Mathf.Cos(Time.time * rollFrequency * 0.7f) * rollAmplitude * 0.5f;
            transform.localRotation = _startRot * Quaternion.Euler(pitch, 0, roll);
        }
    }
}
