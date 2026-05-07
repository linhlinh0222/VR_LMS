using UnityEngine;

namespace MaritimeLMS.Bridge
{
    /// <summary>
    /// Default heading provider that reads world-space yaw of a Camera.
    /// </summary>
    /// <remarks>
    /// Suitable for desktop simulation where the player camera also represents the
    /// vessel heading. For helm-driven steering, replace with an adapter that
    /// integrates ShipBridgeInput.helmWheel over time.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CameraHeadingProvider : MonoBehaviour, IHeadingProvider
    {
        [SerializeField] private Camera sourceCamera;

        public float HeadingDegrees =>
            sourceCamera != null ? Mathf.Repeat(sourceCamera.transform.eulerAngles.y, 360f) : 0f;

        private void Awake()
        {
            if (sourceCamera == null)
            {
                sourceCamera = GetComponent<Camera>();
                if (sourceCamera == null) sourceCamera = Camera.main;
            }
        }
    }
}
