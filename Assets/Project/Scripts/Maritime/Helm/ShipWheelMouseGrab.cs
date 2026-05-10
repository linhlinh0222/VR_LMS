using UnityEngine;

namespace MaritimeLMS.Helm
{
    /// <summary>
    /// Bridges <see cref="ShipWheel"/> into the desktop LMB grab flow.
    /// <see cref="ShipWheel"/> does not implement
    /// <c>DesktopLeverInteractable</c> (sealed class) and is not a
    /// <see cref="Rigidbody"/>, so the cursor-locked raycast in
    /// <c>DesktopMockVRController.TryGrabBody</c> rejects it as
    /// non-selectable. Attach this component to the helm wheel and it
    /// runs its own minimal grab loop using Unity's standard LMB +
    /// camera-centre ray, calling
    /// <see cref="ShipWheel.BeginGrab"/> /
    /// <see cref="ShipWheel.UpdateGrab"/> /
    /// <see cref="ShipWheel.EndGrab"/> at the matching points.
    /// </summary>
    /// <remarks>
    /// Uses the wheel's local +Y axis (after the cabin's Z-up→Y-up
    /// rotation) as the wheel-rotation plane normal to convert the
    /// camera ray into a stable grab anchor as the player drags.
    /// Idempotent — only one is needed per ShipWheel instance, and the
    /// component bails out cleanly if the helm wheel disappears at
    /// runtime.
    /// </remarks>
    [RequireComponent(typeof(ShipWheel))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Helm/Ship Wheel Mouse Grab")]
    public sealed class ShipWheelMouseGrab : MonoBehaviour
    {
        [SerializeField] private ShipWheel target;
        [Tooltip("Maximum world distance from the camera at which an LMB click can grab the wheel.")]
        [SerializeField] private float maxGrabDistance = 6f;
        [Tooltip("Layer mask the grab raycast checks. Default = everything (-1).")]
        [SerializeField] private LayerMask grabbableLayers = ~0;

        private Camera _cam;
        private bool _grabbing;

        private void Awake()
        {
            if (target == null) target = GetComponent<ShipWheel>();
            _cam = Camera.main;
        }

        private void Update()
        {
            if (_cam == null || !_cam.gameObject.activeInHierarchy) _cam = Camera.main;
            if (_cam == null || target == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                TryBeginGrab();
            }
            else if (_grabbing && Input.GetMouseButton(0))
            {
                ContinueGrab();
            }
            else if (_grabbing && Input.GetMouseButtonUp(0))
            {
                EndGrab();
            }
        }

        private void OnDisable()
        {
            if (_grabbing) EndGrab();
        }

        private void TryBeginGrab()
        {
            Ray ray = GetCenterRay();
            if (!Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabbableLayers, QueryTriggerInteraction.Ignore))
                return;
            if (!hit.collider.transform.IsChildOf(target.transform) && hit.collider.transform != target.transform)
                return;

            target.BeginGrab(hit.point);
            _grabbing = true;
        }

        private void ContinueGrab()
        {
            Vector3 anchor = SampleWheelPlane();
            target.UpdateGrab(anchor);
        }

        private void EndGrab()
        {
            target.EndGrab();
            _grabbing = false;
        }

        // Project the centre-screen ray onto the plane containing the
        // wheel face so the hand anchor stays stable as the player drags.
        // Falls back to the ray endpoint if the ray is parallel to the
        // wheel plane.
        private Vector3 SampleWheelPlane()
        {
            Ray ray = GetCenterRay();
            Plane plane = new Plane(target.transform.up, target.transform.position);
            if (plane.Raycast(ray, out float dist))
                return ray.origin + ray.direction * dist;
            return ray.origin + ray.direction * 1.5f;
        }

        private Ray GetCenterRay()
        {
            Vector3 screen = Cursor.lockState == CursorLockMode.Locked
                ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                : Input.mousePosition;
            return _cam.ScreenPointToRay(screen);
        }
    }
}
