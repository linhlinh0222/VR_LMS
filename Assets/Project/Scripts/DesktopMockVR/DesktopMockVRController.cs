using UnityEngine;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CharacterController))]
public sealed class DesktopMockVRController : MonoBehaviour
{
    public enum DesktopHandSide
    {
        Left,
        Right,
        Both
    }

    /// <summary>Currently active hand selection. Useful for hand-aware lesson scoring.</summary>
    public DesktopHandSide ActiveHand => _activeHand;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float lookSensitivity = 2f;

    [Header("Body Collision")]
    [SerializeField] private float bodyHeight = 1.72f;
    [SerializeField] private float bodyRadius = 0.28f;
    [SerializeField] private float bodyEyeHeight = 1.46f;
    [SerializeField] private float bodySkinWidth = 0.035f;
    [SerializeField] private float bodyStepOffset = 0.18f;
    [SerializeField] private float bodySlopeLimit = 55f;
    [SerializeField] private float groundStickSpeed = 1.25f;
    [Tooltip("When true, Space lifts the body and Left Ctrl lowers it (Unity XR Device Simulator convention). " +
             "Lets the desktop tester move between bridge decks or simulate a different VR user height.")]
    [SerializeField] private bool allowVerticalBodyMove = true;

    [Header("Grab")]
    [SerializeField] private LayerMask grabbableLayers = ~0;
    [SerializeField] private LayerMask interactionBlockerLayers;
    [SerializeField] private LayerMask handCollisionLayers = ~0;
    [SerializeField] private float maxGrabDistance = 8f;
    [SerializeField] private float minHoldDistance = 0.75f;
    [SerializeField] private float maxHoldDistance = 10f;
    [SerializeField] private float scrollSensitivity = 1.5f;
    [SerializeField] private float followSpeed = 18f;
    [SerializeField] private float releaseVelocityScale = 0.35f;
    [SerializeField] private float surfaceGrabOffset = 0.1f;
    [SerializeField] private float heldBodyContactSkin = 0.015f;

    [Header("Hands")]
    [SerializeField] private Transform leftHandRoot;
    [SerializeField] private Transform rightHandRoot;
    [SerializeField] private Transform leftHandAttachPoint;
    [SerializeField] private Transform rightHandAttachPoint;
    [SerializeField] private DesktopMockVRHandVisual leftHandVisual;
    [SerializeField] private DesktopMockVRHandVisual rightHandVisual;
    [SerializeField] private DesktopMockVRHandVisibility leftHandVisibility;
    [SerializeField] private DesktopMockVRHandVisibility rightHandVisibility;
    [SerializeField] private bool trainingGhostHandsMode = true;
    [SerializeField] private DesktopHandSide defaultActiveHand = DesktopHandSide.Right;
    [SerializeField] private Vector3 leftHandIdleLocalPosition = new Vector3(-0.28f, -0.18f, 0.64f);
    [SerializeField] private Vector3 rightHandIdleLocalPosition = new Vector3(0.28f, -0.18f, 0.64f);
    [SerializeField] private Vector3 leftHandIdleLocalEuler = new Vector3(0f, 180f, 90f);
    [SerializeField] private Vector3 rightHandIdleLocalEuler = new Vector3(0f, 180f, 90f);
    [SerializeField] private Vector3 leftAttachLocalPosition = new Vector3(-0.039f, 0.167f, -0.074f);
    [SerializeField] private Vector3 rightAttachLocalPosition = new Vector3(0.035f, 0.162f, -0.07f);
    [SerializeField] private float handFollowSpeed = 16f;
    [SerializeField] private float activeHandDefaultDistance = 0.55f;
    [SerializeField, Range(0f, 0.2f)] private float aimScreenPadding = 0.04f;
    [SerializeField, Range(0f, 1f)] private float freeHandAimBlend;
    [SerializeField, Range(0f, 1f)] private float hoverHandAimBlend = 0.12f;
    [SerializeField] private float freeHandSideMinimum = 0.20f;
    [SerializeField] private float freeHandMinimumForward = 0.38f;
    [SerializeField] private float freeHandMinimumLocalHeight = -0.40f;
    [SerializeField] private float freeHandMaximumLocalHeight = 0.02f;
    [SerializeField, Range(0f, 1f)] private float idleGrip;
    [SerializeField, Range(0f, 1f)] private float bodyHoverGrip = 0.03f;
    [SerializeField, Range(0f, 1f)] private float leverHoverGrip = 0.08f;
    [SerializeField, Range(0f, 1f)] private float bodyGrabGrip = 0.32f;
    [SerializeField, Range(0f, 1f)] private float leverGrabGrip = 0.46f;
    [SerializeField, Range(0f, 1f)] private float trainingHoverGrip = 0.08f;
    [SerializeField, Range(0f, 1f)] private float trainingBodyGrabGrip = 0.25f;
    [SerializeField, Range(0f, 1f)] private float trainingLeverGrabGrip = 0.42f;
    [Tooltip("Subtle 'ready' grip applied to the currently selected hand (via 1/2/3 or modifier hold) so the user can see which hand is active without moving its position.")]
    [SerializeField, Range(0f, 1f)] private float activeHandReadyGrip = 0.10f;
    [SerializeField] private float trainingBothHandSpread = -0.16f;

    [Header("Modifier-Capture (Unity XR Interaction Simulator)")]
    [Tooltip("Hold to manipulate the RIGHT hand with the mouse. Default T per Unity XR Interaction Simulator.")]
    [SerializeField] private KeyCode rightHandManipulatorKey = KeyCode.T;
    [Tooltip("Hold to manipulate the LEFT hand with the mouse. Default Y.")]
    [SerializeField] private KeyCode leftHandManipulatorKey = KeyCode.Y;
    [Tooltip("Mouse delta -> hand local-translation (m per pixel-equivalent). Lower = finer aim.")]
    [SerializeField] private float manipulatorSensitivity = 0.0035f;
    [Tooltip("Mouse-wheel delta -> hand depth (camera-forward). Active only while a modifier is held and not currently holding an object.")]
    [SerializeField] private float manipulatorDepthSensitivity = 0.08f;
    [SerializeField] private Vector3 manipulatorMinLocal = new Vector3(-0.85f, -0.70f, 0.30f);
    [SerializeField] private Vector3 manipulatorMaxLocal = new Vector3(0.85f, 0.50f, 1.50f);

    [Header("Auto-Snap (gravity-glove)")]
    [Tooltip("Hand magnetically snaps toward any interactable within this radius (world meters).")]
    [SerializeField] private float autoSnapRadius = 0.35f;
    [Tooltip("How strongly the hand is pulled toward the snap target each frame. 0 = off, 1 = locks to target.")]
    [SerializeField, Range(0f, 1f)] private float autoSnapStrength = 0.55f;
    [SerializeField] private LayerMask autoSnapLayers = ~0;

    [Header("Body Proxy")]
    [SerializeField] private Vector3 leftShoulderLocalPosition = new Vector3(-0.22f, -0.32f, 0.04f);
    [SerializeField] private Vector3 rightShoulderLocalPosition = new Vector3(0.22f, -0.32f, 0.04f);
    [SerializeField] private Vector3 chestLocalPosition = new Vector3(0f, -0.46f, 0.05f);
    [SerializeField] private Vector3 abdomenLocalPosition = new Vector3(0f, -0.82f, 0.02f);
    [SerializeField] private float maxArmReach = 0.72f;
    [SerializeField] private float torsoRadius = 0.23f;
    [SerializeField] private float handCollisionRadius = 0.055f;
    [SerializeField] private float handContactSkin = 0.015f;

    [Header("Hand Framing")]
    [SerializeField] private bool keepFreeHandsInView = true;
    [SerializeField, Range(0f, 0.45f)] private float handViewportHorizontalPadding = 0.12f;
    [SerializeField, Range(0f, 0.45f)] private float handViewportBottomPadding = 0.18f;
    [SerializeField, Range(0.55f, 1f)] private float handViewportTopLimit = 0.86f;
    [SerializeField] private float handViewportNudgeSpeed = 30f;

    private Camera _camera;
    private CharacterController _characterController;
    private Rigidbody _heldBody;
    private DesktopLeverInteractable _heldLever;
    private Transform _heldHandRoot;
    private DesktopHandSide _activeHand;
    private bool _heldBodyWasKinematic;
    private bool _heldBodyUsedGravity;
    private RigidbodyInterpolation _heldBodyInterpolation;
    private CollisionDetectionMode _heldBodyCollisionDetectionMode;
    private float _holdDistance;
    private float _pitch;
    private float _yaw;
    private Vector3 _lastHeldPosition;
    private Vector3 _releaseVelocity;
    private Vector3 _grabPointToBodyCenter;
    private Vector3 _heldHandAttachTargetPosition;
    private bool _hasHeldHandAttachTarget;
    private bool _leftHandHoveringInteractable;
    private bool _rightHandHoveringInteractable;
    private bool _leftHandHoveringLever;
    private bool _rightHandHoveringLever;
    private bool _rightManipulatorActive;
    private bool _leftManipulatorActive;
    private Vector3 _leftManipulatorLocalTarget;
    private Vector3 _rightManipulatorLocalTarget;
    private readonly RaycastHit[] _handCastHits = new RaycastHit[16];
    private readonly RaycastHit[] _interactionRayHits = new RaycastHit[32];
    private readonly Collider[] _autoSnapColliders = new Collider[16];
    private Renderer[] _leftHandRenderers = System.Array.Empty<Renderer>();
    private Renderer[] _rightHandRenderers = System.Array.Empty<Renderer>();

    private bool IsHolding => _heldBody != null || _heldLever != null;
    private bool IsManipulatorActive => _rightManipulatorActive || _leftManipulatorActive;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _characterController = GetComponent<CharacterController>();
        ConfigureCharacterController();
        _activeHand = defaultActiveHand;
        _leftManipulatorLocalTarget = leftHandIdleLocalPosition;
        _rightManipulatorLocalTarget = rightHandIdleLocalPosition;
        EnsureHandAttachPoints();
        EnsureHandVisuals();

        Vector3 rotation = transform.eulerAngles;
        _yaw = rotation.y;
        _pitch = NormalizePitch(rotation.x);
        _targetYaw = _yaw;
        _targetPitch = _pitch;
        _currentFOV = normalFOV;
        ResetHandToIdle(leftHandRoot, leftHandIdleLocalPosition);
        ResetHandToIdle(rightHandRoot, rightHandIdleLocalPosition);
        UpdateHandGripVisuals();
    }

    private void Update()
    {
        UpdateControlMode();
        UpdateLook();
        UpdateMove();
        UpdateGrabInput();
    }

    private void FixedUpdate()
    {
        FollowHeldBody();
    }

    private void LateUpdate()
    {
        UpdateHandVisuals();
    }

    private void OnDrawGizmosSelected()
    {
        DrawBodyProxyGizmos();
    }

    private void OnDisable()
    {
        ReleaseHeldBody();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnValidate()
    {
        bodyRadius = Mathf.Max(0.05f, bodyRadius);
        bodyHeight = Mathf.Max(bodyRadius * 2f + 0.01f, bodyHeight);
        bodyEyeHeight = Mathf.Max(bodyRadius, bodyEyeHeight);
        bodySkinWidth = Mathf.Clamp(bodySkinWidth, 0.005f, bodyRadius * 0.5f);
        bodyStepOffset = Mathf.Clamp(bodyStepOffset, 0f, bodyHeight - bodyRadius * 2f);
        bodySlopeLimit = Mathf.Clamp(bodySlopeLimit, 0f, 89f);
        groundStickSpeed = Mathf.Max(0f, groundStickSpeed);
        activeHandDefaultDistance = Mathf.Max(freeHandMinimumForward + 0.01f, activeHandDefaultDistance);
        hoverHandAimBlend = Mathf.Max(hoverHandAimBlend, freeHandAimBlend);
        freeHandSideMinimum = Mathf.Max(0f, freeHandSideMinimum);
        freeHandMinimumForward = Mathf.Max(0.05f, freeHandMinimumForward);
        handViewportTopLimit = Mathf.Max(handViewportBottomPadding + 0.05f, handViewportTopLimit);
        handViewportNudgeSpeed = Mathf.Max(0f, handViewportNudgeSpeed);
        aimScreenPadding = Mathf.Clamp(aimScreenPadding, 0f, 0.2f);
        trainingBothHandSpread = Mathf.Clamp(trainingBothHandSpread, -0.5f, 0.5f);
        if (freeHandMaximumLocalHeight < freeHandMinimumLocalHeight)
        {
            float previousMinimumHeight = freeHandMinimumLocalHeight;
            freeHandMinimumLocalHeight = freeHandMaximumLocalHeight;
            freeHandMaximumLocalHeight = previousMinimumHeight;
        }

        ConfigureCharacterController();
    }

    private void ConfigureCharacterController()
    {
        CharacterController controller = _characterController != null
            ? _characterController
            : GetComponent<CharacterController>();

        if (controller == null)
        {
            return;
        }

        float radius = Mathf.Max(0.05f, bodyRadius);
        float height = Mathf.Max(radius * 2f + 0.01f, bodyHeight);

        controller.radius = radius;
        controller.height = height;
        controller.center = Vector3.down * Mathf.Max(0f, bodyEyeHeight - height * 0.5f);
        controller.skinWidth = Mathf.Clamp(bodySkinWidth, 0.005f, radius * 0.5f);
        controller.stepOffset = Mathf.Clamp(bodyStepOffset, 0f, height - radius * 2f);
        controller.slopeLimit = Mathf.Clamp(bodySlopeLimit, 0f, 89f);
        controller.minMoveDistance = 0f;
        controller.detectCollisions = true;
        controller.enableOverlapRecovery = true;
    }

    private void UpdateControlMode()
    {
        // Modifier-Capture pattern (Unity XR Interaction Simulator 3.x):
        // hold T -> mouse drives RIGHT controller, hold Y -> LEFT,
        // hold both -> both controllers, release -> mouse drives head.
        // Mouse delta accumulates into a per-hand camera-local target so the
        // hand keeps its last pose between modifier holds (XR Sim convention).
        bool wasHolding = IsHolding;
        _rightManipulatorActive = Input.GetKey(rightHandManipulatorKey);
        _leftManipulatorActive = Input.GetKey(leftHandManipulatorKey);

        if (!wasHolding)
        {
            if (_rightManipulatorActive && _leftManipulatorActive)
            {
                _activeHand = DesktopHandSide.Both;
            }
            else if (_rightManipulatorActive)
            {
                _activeHand = DesktopHandSide.Right;
            }
            else if (_leftManipulatorActive)
            {
                _activeHand = DesktopHandSide.Left;
            }
            else
            {
                // Explicit selection (Alpha + Keypad — Vietnamese IME may eat
                // top-row 2/3 diacritics, so accept numpad too). Hands stay
                // at their natural idle positions; visual feedback for the
                // active hand comes from a subtle grip "ready" pose, not by
                // moving the hand into the camera's view.
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) _activeHand = DesktopHandSide.Right;
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) _activeHand = DesktopHandSide.Left;
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) _activeHand = DesktopHandSide.Both;
            }
        }

        if (!IsManipulatorActive)
        {
            return;
        }

        float dx = Input.GetAxisRaw("Mouse X") * manipulatorSensitivity;
        float dy = Input.GetAxisRaw("Mouse Y") * manipulatorSensitivity;
        // Scroll-wheel only drives manipulator depth when not currently holding —
        // while holding, scroll keeps its existing role of adjusting hold distance.
        float dz = !wasHolding ? Input.mouseScrollDelta.y * manipulatorDepthSensitivity : 0f;
        Vector3 delta = new Vector3(dx, dy, dz);

        if (_rightManipulatorActive)
        {
            _rightManipulatorLocalTarget = ClampManipulator(_rightManipulatorLocalTarget + delta);
        }

        if (_leftManipulatorActive)
        {
            _leftManipulatorLocalTarget = ClampManipulator(_leftManipulatorLocalTarget + delta);
        }
    }

    private Vector3 ClampManipulator(Vector3 v) => new Vector3(
        Mathf.Clamp(v.x, manipulatorMinLocal.x, manipulatorMaxLocal.x),
        Mathf.Clamp(v.y, manipulatorMinLocal.y, manipulatorMaxLocal.y),
        Mathf.Clamp(v.z, manipulatorMinLocal.z, manipulatorMaxLocal.z));

    [Header("Professional Simulation")]
    [SerializeField] private bool enableSmoothing = true;
    [SerializeField] private float rotationSmoothing = 10f;
    [SerializeField] private float zoomFOV = 35f;
    [SerializeField] private float normalFOV = 65f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float leanAmount = 0.5f;
    [SerializeField] private float leanSpeed = 4f;

    private float _targetPitch;
    private float _targetYaw;
    private float _currentFOV;
    private float _leanOffset;
    private Vector3 _originalHandLocalPosLeft;
    private Vector3 _originalHandLocalPosRight;

    private bool _cursorLockInitialized;

    private void UpdateLook()
    {
        // VR-mock convention (Unity XR Device Simulator + FPS standard):
        // cursor is locked + hidden by default so mouse drives free 360° look
        // immediately; ESC unlocks for menus; clicking back into the game view
        // re-locks. No right-click hold required.
        if (!_cursorLockInitialized)
        {
            LockCursorForLook();
            _cursorLockInitialized = true;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursorForMenu();
        }
        else if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
        {
            // Player clicked back into the viewport — re-lock for look.
            LockCursorForLook();
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            HandleProfessionalInputs();
            return;
        }

        // Camera (head) rotation pauses while a hand-manipulator key is held
        // so mouse delta drives the controller instead — matches Unity XR
        // Interaction Simulator + Meta XR Simulator desktop conventions.
        if (!IsManipulatorActive)
        {
            _targetYaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
            _targetPitch -= Input.GetAxisRaw("Mouse Y") * lookSensitivity;
            _targetPitch = Mathf.Clamp(_targetPitch, -80f, 80f);

            if (enableSmoothing)
            {
                _yaw = Mathf.LerpAngle(_yaw, _targetYaw, Time.deltaTime * rotationSmoothing);
                _pitch = Mathf.LerpAngle(_pitch, _targetPitch, Time.deltaTime * rotationSmoothing);
            }
            else
            {
                _yaw = _targetYaw;
                _pitch = _targetPitch;
            }

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        HandleProfessionalInputs();
    }

    private static void LockCursorForLook()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void UnlockCursorForMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleProfessionalInputs()
    {
        // 1. Zoom Logic (Middle Mouse or Z)
        bool isZooming = Input.GetMouseButton(2) || Input.GetKey(KeyCode.Z);
        float targetFOV = isZooming ? zoomFOV : normalFOV;
        _currentFOV = Mathf.Lerp(_currentFOV, targetFOV, Time.deltaTime * zoomSpeed);
        if (_camera != null) _camera.fieldOfView = _currentFOV;

        // 2. Lean Logic (Q / E keys)
        float targetLean = 0f;
        if (Input.GetKey(KeyCode.Q)) targetLean = -leanAmount;
        if (Input.GetKey(KeyCode.E)) targetLean = leanAmount;
        _leanOffset = Mathf.Lerp(_leanOffset, targetLean, Time.deltaTime * leanSpeed);
        
        // Lean is tracked separately for now. Moving the body root here would
        // overwrite CharacterController motion when the camera is parented.
    }

    [Header("Maritime Walk Feel")]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmount = 0.02f;
    private float _walkBobTimer;

    private void UpdateMove()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = transform.forward;
        }

        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.A)) move -= right;

        // Vertical translation per Unity XR Device Simulator convention:
        // Space lifts the body, Left Ctrl lowers. Useful to inspect raised
        // controls or drop to a lower deck during desktop simulation.
        float verticalInput = 0f;
        if (allowVerticalBodyMove)
        {
            if (Input.GetKey(KeyCode.Space)) verticalInput += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) verticalInput -= 1f;
        }

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        if (move.sqrMagnitude > 0.01f || Mathf.Abs(verticalInput) > 0.01f)
        {
            _walkBobTimer += Time.deltaTime * walkBobSpeed;
            Vector3 horizontal = move.sqrMagnitude > 0.01f ? move.normalized : Vector3.zero;
            Vector3 bodyDelta = (horizontal + Vector3.up * verticalInput) * speed * Time.deltaTime;
            MoveBody(bodyDelta);
        }
        else
        {
            _walkBobTimer = 0;
            // Apply gravity/stick even when idle — only when not flying.
            MoveBody(Vector3.down * groundStickSpeed * Time.deltaTime);
        }
    }

    private void MoveBody(Vector3 delta)
    {
        if (delta.sqrMagnitude < 0.000001f)
        {
            return;
        }

        if (_characterController != null && _characterController.enabled)
        {
            _characterController.Move(delta);
            return;
        }

        transform.position += delta;
    }

    private void UpdateGrabInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryGrabBody();
        }

        if (!IsHolding)
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _holdDistance = Mathf.Clamp(
                _holdDistance + scroll * scrollSensitivity,
                minHoldDistance,
                maxHoldDistance);
        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseHeldBody();
        }
    }

    private void TryGrabBody()
    {
        Ray ray = GetAimRay();
        if (!TryRaycastSelectable(ray, out RaycastHit hit))
        {
            return;
        }

        Rigidbody hitBody = hit.rigidbody;
        DesktopLeverInteractable hitLever = hit.collider.GetComponentInParent<DesktopLeverInteractable>();
        if (hitLever != null)
        {
            BeginLeverGrab(hitLever, hit, ray);
            return;
        }

        if (hitBody == null)
        {
            return;
        }

        Transform handRoot = GetActiveHandRoot();
        if (handRoot == null)
        {
            return;
        }

        _heldBody = hitBody;
        _heldHandRoot = handRoot;
        _heldBodyWasKinematic = hitBody.isKinematic;
        _heldBodyUsedGravity = hitBody.useGravity;
        _heldBodyInterpolation = hitBody.interpolation;
        _heldBodyCollisionDetectionMode = hitBody.collisionDetectionMode;
        Vector3 handAnchorPosition = GetSurfaceAnchorPosition(hit, surfaceGrabOffset);
        _holdDistance = GetClampedDistanceAlongRay(ray, handAnchorPosition);
        _lastHeldPosition = hitBody.position;
        _releaseVelocity = Vector3.zero;
        _grabPointToBodyCenter = hitBody.position - handAnchorPosition;
        _heldHandAttachTargetPosition = handAnchorPosition;
        _hasHeldHandAttachTarget = true;

        if (!trainingGhostHandsMode)
        {
            MoveHandAttachToWorldPositionImmediate(_heldHandRoot, GetAttachPoint(_heldHandRoot), handAnchorPosition);
        }

        if (!hitBody.isKinematic)
        {
            SetLinearVelocity(hitBody, Vector3.zero);
            hitBody.angularVelocity = Vector3.zero;
        }

        hitBody.isKinematic = true;
        hitBody.useGravity = false;
        hitBody.interpolation = RigidbodyInterpolation.Interpolate;
        hitBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        UpdateHandGripVisuals();
    }

    private void BeginLeverGrab(DesktopLeverInteractable hitLever, RaycastHit hit, Ray ray)
    {
        Transform handRoot = GetActiveHandRoot();
        if (handRoot == null)
        {
            return;
        }

        _heldLever = hitLever;
        _heldHandRoot = handRoot;
        Vector3 handAnchorPosition = GetSurfaceAnchorPosition(hit, surfaceGrabOffset);
        _holdDistance = GetClampedDistanceAlongRay(ray, handAnchorPosition);
        _heldHandAttachTargetPosition = handAnchorPosition;
        _hasHeldHandAttachTarget = true;

        hitLever.BeginGrab(ray.origin + ray.direction * _holdDistance);
        if (!trainingGhostHandsMode && !MoveHeldLeverHandToGripPoseImmediate())
        {
            MoveHandAttachToWorldPositionImmediate(_heldHandRoot, GetAttachPoint(_heldHandRoot), handAnchorPosition);
        }

        UpdateHandGripVisuals();
    }

    private void FollowHeldBody()
    {
        if (!IsHolding || _heldHandRoot == null)
        {
            return;
        }

        Ray ray = GetAimRay();
        Vector3 requestedHandAttachTargetPosition = ray.origin + ray.direction * _holdDistance;
        if (!trainingGhostHandsMode)
        {
            requestedHandAttachTargetPosition = ConstrainHandAttachTarget(
                _heldHandRoot,
                requestedHandAttachTargetPosition,
                _heldBody != null);
        }

        if (_heldLever != null)
        {
            _heldHandAttachTargetPosition = requestedHandAttachTargetPosition;
            _hasHeldHandAttachTarget = true;
            _heldLever.UpdateGrab(requestedHandAttachTargetPosition);
            return;
        }

        Vector3 targetPosition = requestedHandAttachTargetPosition + _grabPointToBodyCenter;
        float followT = 1f - Mathf.Exp(-followSpeed * Time.fixedDeltaTime);
        Vector3 nextPosition = Vector3.Lerp(_heldBody.position, targetPosition, followT);
        nextPosition = ClampHeldBodyMove(_heldBody, _heldBody.position, nextPosition);

        _heldHandAttachTargetPosition = nextPosition - _grabPointToBodyCenter;
        _hasHeldHandAttachTarget = true;
        _heldBody.MovePosition(nextPosition);
        _releaseVelocity = (nextPosition - _lastHeldPosition) / Time.fixedDeltaTime;
        _lastHeldPosition = nextPosition;
    }

    private void UpdateHandVisuals()
    {
        if (trainingGhostHandsMode)
        {
            UpdateTrainingGhostHands();
            return;
        }

        Transform positionedHandRoot = _heldHandRoot;
        if (_heldLever != null && _heldHandRoot != null)
        {
            MoveHeldLeverHandToGripPose(Time.deltaTime);
        }
        else if (IsHolding && _heldHandRoot != null)
        {
            Vector3 handAttachTargetPosition;
            if (_hasHeldHandAttachTarget)
            {
                handAttachTargetPosition = _heldHandAttachTargetPosition;
            }
            else
            {
                Ray ray = GetAimRay();
                handAttachTargetPosition = ray.origin + ray.direction * _holdDistance;
            }

            MoveHandAttachToWorldPosition(_heldHandRoot, GetAttachPoint(_heldHandRoot), handAttachTargetPosition, Time.deltaTime);
        }
        else
        {
            positionedHandRoot = GetActiveHandRoot();
            UpdateFreeActiveHand();
        }

        if (leftHandRoot != positionedHandRoot)
        {
            MoveHandToLocalPosition(leftHandRoot, leftHandIdleLocalPosition, Time.deltaTime);
        }

        if (rightHandRoot != positionedHandRoot)
        {
            MoveHandToLocalPosition(rightHandRoot, rightHandIdleLocalPosition, Time.deltaTime);
        }

        if (_heldLever == null || leftHandRoot != _heldHandRoot)
        {
            SetHandRotation(leftHandRoot);
        }

        if (_heldLever == null || rightHandRoot != _heldHandRoot)
        {
            SetHandRotation(rightHandRoot);
        }

        KeepHandsFramedForDesktopView();
        UpdateHandGripVisuals();
    }

    private void UpdateTrainingGhostHands()
    {
        UpdateTrainingGhostHandHoverState();

        if (_heldLever != null)
        {
            UpdateTrainingLeverGripHands();
            return;
        }

        // A hand only leaves its natural idle pose when its modifier (T/Y) is
        // held or it is attached to a held body. Selecting via 1/2/3 only
        // changes which hand handles LMB grab; the visible switch is shown
        // through the active-hand "ready" grip in UpdateHandGripVisuals so
        // the hands do not crowd the camera view.
        bool leftActive = _leftManipulatorActive
            || (IsHolding && _heldHandRoot == leftHandRoot);
        bool rightActive = _rightManipulatorActive
            || (IsHolding && _heldHandRoot == rightHandRoot);

        if (leftActive)
        {
            MoveHandAttachToWorldPosition(
                leftHandRoot,
                GetAttachPoint(leftHandRoot),
                GetTrainingGhostHandTargetForSide(leftHand: true),
                Time.deltaTime);
        }
        else
        {
            MoveHandToLocalPosition(leftHandRoot, leftHandIdleLocalPosition, Time.deltaTime);
        }

        if (rightActive)
        {
            MoveHandAttachToWorldPosition(
                rightHandRoot,
                GetAttachPoint(rightHandRoot),
                GetTrainingGhostHandTargetForSide(leftHand: false),
                Time.deltaTime);
        }
        else
        {
            MoveHandToLocalPosition(rightHandRoot, rightHandIdleLocalPosition, Time.deltaTime);
        }

        SetHandRotation(leftHandRoot);
        SetHandRotation(rightHandRoot);
        UpdateHandGripVisuals();
    }

    private void UpdateTrainingLeverGripHands()
    {
        bool movedLeftHand = IsHandSelected(leftHandRoot)
            && MoveHandToLeverGripPose(leftHandRoot, Time.deltaTime);
        bool movedRightHand = IsHandSelected(rightHandRoot)
            && MoveHandToLeverGripPose(rightHandRoot, Time.deltaTime);

        if (!movedLeftHand)
        {
            MoveHandToLocalPosition(leftHandRoot, leftHandIdleLocalPosition, Time.deltaTime);
            SetHandRotation(leftHandRoot);
        }

        if (!movedRightHand)
        {
            MoveHandToLocalPosition(rightHandRoot, rightHandIdleLocalPosition, Time.deltaTime);
            SetHandRotation(rightHandRoot);
        }

        UpdateHandGripVisuals();
    }

    private void UpdateTrainingGhostHandHoverState()
    {
        SetHandHoveringInteractable(leftHandRoot, false, false);
        SetHandHoveringInteractable(rightHandRoot, false, false);

        if (IsHolding)
        {
            return;
        }

        if (GetActiveHandRoot() == null)
        {
            return;
        }

        if (!TryRaycastSelectable(GetAimRay(), out RaycastHit hit))
        {
            return;
        }

        bool hoveringLever = hit.collider.GetComponentInParent<DesktopLeverInteractable>() != null;
        bool hoveringBody = hit.rigidbody != null;
        if (hoveringLever || hoveringBody)
        {
            if (IsHandSelected(leftHandRoot))
            {
                SetHandHoveringInteractable(leftHandRoot, true, hoveringLever);
            }

            if (IsHandSelected(rightHandRoot))
            {
                SetHandHoveringInteractable(rightHandRoot, true, hoveringLever);
            }
        }
    }

    private Vector3 GetTrainingGhostHandTargetForSide(bool leftHand)
    {
        // While holding a body/lever: follow the held attach target (kept up
        // to date by FollowHeldBody / lever update). Both-hands mode applies
        // the existing sideways spread so the off-hand sits beside the grab.
        if (IsHolding && _hasHeldHandAttachTarget)
        {
            Vector3 heldTarget = _heldHandAttachTargetPosition;
            if (_activeHand == DesktopHandSide.Both)
            {
                float side = leftHand ? -1f : 1f;
                heldTarget += transform.right * trainingBothHandSpread * side;
            }
            return heldTarget;
        }

        // Free hand: manipulator local target -> world. Auto-snap pulls
        // toward any nearby interactable (Half-Life Alyx gravity-glove feel).
        Vector3 localTarget = leftHand ? _leftManipulatorLocalTarget : _rightManipulatorLocalTarget;
        Vector3 worldTarget = transform.TransformPoint(localTarget);

        if (TryFindAutoSnapTarget(worldTarget, out Vector3 snapPoint))
        {
            worldTarget = Vector3.Lerp(worldTarget, snapPoint, autoSnapStrength);
        }

        if (_activeHand == DesktopHandSide.Both)
        {
            float side = leftHand ? -1f : 1f;
            worldTarget += transform.right * trainingBothHandSpread * side;
        }

        return worldTarget;
    }

    private bool TryFindAutoSnapTarget(Vector3 worldPosition, out Vector3 snapPoint)
    {
        snapPoint = worldPosition;
        if (autoSnapStrength <= 0f || autoSnapRadius <= 0f) return false;

        int count = Physics.OverlapSphereNonAlloc(
            worldPosition,
            autoSnapRadius,
            _autoSnapColliders,
            autoSnapLayers,
            QueryTriggerInteraction.Ignore);

        Collider best = null;
        float bestDistSq = float.PositiveInfinity;
        Vector3 bestPoint = worldPosition;

        for (int i = 0; i < count; i++)
        {
            Collider c = _autoSnapColliders[i];
            if (c == null || !IsAutoSnapCandidate(c)) continue;
            Vector3 closest = c.ClosestPoint(worldPosition);
            float distSq = (worldPosition - closest).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = c;
                bestPoint = closest;
            }
        }

        if (best == null) return false;

        Vector3 outward = worldPosition - bestPoint;
        if (outward.sqrMagnitude < 0.0001f) outward = -transform.forward;
        snapPoint = bestPoint + outward.normalized * surfaceGrabOffset;
        return true;
    }

    private bool IsAutoSnapCandidate(Collider c)
    {
        DesktopLeverInteractable lever = c.GetComponentInParent<DesktopLeverInteractable>();
        if (lever != null)
        {
            // Don't snap onto the lever we're already holding.
            return _heldLever == null || lever != _heldLever;
        }

        if (c.attachedRigidbody != null)
        {
            if (_heldBody != null && c.attachedRigidbody == _heldBody) return false;
            return IsLayerInMask(c.gameObject.layer, grabbableLayers);
        }

        return false;
    }

    private void UpdateFreeActiveHand()
    {
        Transform activeHandRoot = GetActiveHandRoot();
        if (activeHandRoot == null)
        {
            return;
        }

        SetHandRotation(activeHandRoot);
        SetHandHoveringInteractable(activeHandRoot, false, false);
        Ray ray = GetAimRay();
        float targetDistance = activeHandDefaultDistance;
        float aimBlend = freeHandAimBlend;
        bool collideWithWorld = false;
        Vector3 aimedAttachWorldPosition = ray.origin + ray.direction * targetDistance;
        if (TryRaycastSelectable(ray, out RaycastHit hit))
        {
            DesktopLeverInteractable hoverLever = hit.collider.GetComponentInParent<DesktopLeverInteractable>();
            if (hoverLever != null)
            {
                SetHandHoveringInteractable(activeHandRoot, true, true);
            }
            else if (hit.rigidbody != null)
            {
                SetHandHoveringInteractable(activeHandRoot, true, false);
            }

            aimedAttachWorldPosition = GetSurfaceAnchorPosition(hit, surfaceGrabOffset);
            aimBlend = Mathf.Max(freeHandAimBlend, hoverHandAimBlend);
            collideWithWorld = true;
        }

        Vector3 handAttachTargetPosition = GetFreeHandPreviewAttachTarget(activeHandRoot, aimedAttachWorldPosition, aimBlend);
        handAttachTargetPosition = ConstrainHandAttachTarget(activeHandRoot, handAttachTargetPosition, collideWithWorld);
        MoveHandAttachToWorldPosition(activeHandRoot, GetAttachPoint(activeHandRoot), handAttachTargetPosition, Time.deltaTime);
    }

    private bool TryRaycastSelectable(Ray ray, out RaycastHit hit)
    {
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            _interactionRayHits,
            maxGrabDistance,
            GetInteractionQueryMask(),
            QueryTriggerInteraction.Ignore);

        float consumedDistance = -1f;
        for (int pass = 0; pass < hitCount; pass++)
        {
            int nearestIndex = -1;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                float distance = _interactionRayHits[i].distance;
                if (distance <= consumedDistance + 0.0001f || distance >= nearestDistance)
                {
                    continue;
                }

                nearestIndex = i;
                nearestDistance = distance;
            }

            if (nearestIndex < 0)
            {
                break;
            }

            hit = _interactionRayHits[nearestIndex];
            consumedDistance = nearestDistance;
            if (IsInteractionRayPassthrough(hit.collider))
            {
                continue;
            }

            if (IsSelectableHit(hit))
            {
                return true;
            }

            if (IsInteractionBlockerHit(hit.collider))
            {
                break;
            }

            break;
        }

        hit = default;
        return false;
    }

    private bool IsSelectableHit(RaycastHit hit)
    {
        if (hit.collider == null)
        {
            return false;
        }

        if (!IsLayerInMask(hit.collider.gameObject.layer, grabbableLayers))
        {
            return false;
        }

        return hit.rigidbody != null
            || hit.collider.GetComponentInParent<DesktopLeverInteractable>() != null;
    }

    private int GetInteractionQueryMask()
    {
        return grabbableLayers.value | interactionBlockerLayers.value;
    }

    private bool IsInteractionBlockerHit(Collider collider)
    {
        return collider != null && IsLayerInMask(collider.gameObject.layer, interactionBlockerLayers);
    }

    private static bool IsInteractionRayPassthrough(Collider collider)
    {
        return collider != null
            && collider.GetComponentInParent<DesktopInteractionRayPassthrough>() != null;
    }

    private Vector3 GetFreeHandPreviewAttachTarget(Transform handRoot, Vector3 aimedAttachWorldPosition, float aimBlend)
    {
        Vector3 idleRootWorldPosition = transform.TransformPoint(GetIdleLocalPosition(handRoot));
        Quaternion idleRootWorldRotation = transform.rotation * Quaternion.Euler(GetIdleLocalEuler(handRoot));
        Transform attachPoint = GetAttachPoint(handRoot);
        Vector3 idleAttachWorldPosition = idleRootWorldPosition;
        if (attachPoint != null && attachPoint != handRoot)
        {
            idleAttachWorldPosition += idleRootWorldRotation * attachPoint.localPosition;
        }

        Vector3 blendedAttachWorldPosition = Vector3.Lerp(
            idleAttachWorldPosition,
            aimedAttachWorldPosition,
            Mathf.Clamp01(aimBlend));

        return KeepFreeHandInOwnWorkspace(handRoot, blendedAttachWorldPosition);
    }

    private Vector3 KeepFreeHandInOwnWorkspace(Transform handRoot, Vector3 worldPosition)
    {
        Quaternion bodyYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 localPosition = Quaternion.Inverse(bodyYaw) * (worldPosition - transform.position);

        float sideMinimum = Mathf.Max(0f, freeHandSideMinimum);
        if (sideMinimum > 0f)
        {
            localPosition.x = handRoot == leftHandRoot
                ? Mathf.Min(localPosition.x, -sideMinimum)
                : Mathf.Max(localPosition.x, sideMinimum);
        }

        localPosition.y = Mathf.Clamp(localPosition.y, freeHandMinimumLocalHeight, freeHandMaximumLocalHeight);
        localPosition.z = Mathf.Clamp(
            localPosition.z,
            freeHandMinimumForward,
            Mathf.Max(freeHandMinimumForward + 0.01f, activeHandDefaultDistance));

        return transform.position + bodyYaw * localPosition;
    }

    private Vector3 GetIdleLocalPosition(Transform handRoot)
    {
        return handRoot == leftHandRoot ? leftHandIdleLocalPosition : rightHandIdleLocalPosition;
    }

    private void MoveHandToLocalPosition(Transform handRoot, Vector3 localPosition, float deltaTime)
    {
        if (handRoot == null)
        {
            return;
        }

        float followT = 1f - Mathf.Exp(-handFollowSpeed * deltaTime);
        handRoot.localPosition = Vector3.Lerp(handRoot.localPosition, localPosition, followT);
    }

    private void ReleaseHeldBody()
    {
        if (!IsHolding)
        {
            return;
        }

        Rigidbody releasedBody = _heldBody;
        DesktopLeverInteractable releasedLever = _heldLever;
        _heldBody = null;
        _heldLever = null;
        _heldHandRoot = null;
        _grabPointToBodyCenter = Vector3.zero;
        _hasHeldHandAttachTarget = false;

        if (releasedLever != null)
        {
            releasedLever.EndGrab();
        }

        if (releasedBody != null)
        {
            releasedBody.isKinematic = _heldBodyWasKinematic;
            releasedBody.useGravity = _heldBodyUsedGravity;
            releasedBody.interpolation = _heldBodyInterpolation;
            releasedBody.collisionDetectionMode = _heldBodyCollisionDetectionMode;

            if (!_heldBodyWasKinematic)
            {
                SetLinearVelocity(releasedBody, _releaseVelocity * releaseVelocityScale);
            }
        }

        ResetHandToIdle(leftHandRoot, leftHandIdleLocalPosition);
        ResetHandToIdle(rightHandRoot, rightHandIdleLocalPosition);
        UpdateHandGripVisuals();
    }

    private Ray GetAimRay()
    {
        // While a hand-manipulator key is held, the aim ray points from the
        // camera through the active hand's manipulator world position. This
        // means grab/hover/held-body code automatically follows the hand
        // without needing per-call branches — matches the XR Sim's "selected
        // controller is the aim source" model.
        if (IsManipulatorActive)
        {
            Transform handRoot = GetActiveHandRoot();
            Vector3 local = handRoot == leftHandRoot
                ? _leftManipulatorLocalTarget
                : _rightManipulatorLocalTarget;
            Vector3 handWorld = transform.TransformPoint(local);
            Vector3 origin = transform.position;
            Vector3 direction = handWorld - origin;
            if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
            return new Ray(origin, direction.normalized);
        }

        Vector3 screenPoint = Cursor.lockState == CursorLockMode.Locked
            ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
            : GetClampedMouseScreenPoint();
        return _camera.ScreenPointToRay(screenPoint);
    }

    private Vector3 GetClampedMouseScreenPoint()
    {
        Vector3 screenPoint = Input.mousePosition;
        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);
        float paddingX = width * aimScreenPadding;
        float paddingY = height * aimScreenPadding;
        screenPoint.x = Mathf.Clamp(screenPoint.x, paddingX, width - paddingX);
        screenPoint.y = Mathf.Clamp(screenPoint.y, paddingY, height - paddingY);
        screenPoint.z = 0f;
        return screenPoint;
    }

    private Transform GetActiveHandRoot()
    {
        Transform candidate = _activeHand == DesktopHandSide.Left ? leftHandRoot : rightHandRoot;
        if (candidate != null)
        {
            return candidate;
        }

        return rightHandRoot != null ? rightHandRoot : leftHandRoot;
    }

    private bool IsHandSelected(Transform handRoot)
    {
        if (handRoot == leftHandRoot)
        {
            return _activeHand == DesktopHandSide.Left || _activeHand == DesktopHandSide.Both;
        }

        if (handRoot == rightHandRoot)
        {
            return _activeHand == DesktopHandSide.Right || _activeHand == DesktopHandSide.Both;
        }

        return false;
    }

    private Transform GetAttachPoint(Transform handRoot)
    {
        if (handRoot == leftHandRoot && leftHandAttachPoint != null)
        {
            return leftHandAttachPoint;
        }

        if (handRoot == rightHandRoot && rightHandAttachPoint != null)
        {
            return rightHandAttachPoint;
        }

        return handRoot;
    }

    private void EnsureHandAttachPoints()
    {
        leftHandAttachPoint = EnsureAttachPoint(leftHandRoot, leftHandAttachPoint, "Left Hand Attach", leftAttachLocalPosition);
        rightHandAttachPoint = EnsureAttachPoint(rightHandRoot, rightHandAttachPoint, "Right Hand Attach", rightAttachLocalPosition);
    }

    private void EnsureHandVisuals()
    {
        if (leftHandVisual == null && leftHandRoot != null)
        {
            leftHandVisual = leftHandRoot.GetComponentInChildren<DesktopMockVRHandVisual>(true);
        }

        if (rightHandVisual == null && rightHandRoot != null)
        {
            rightHandVisual = rightHandRoot.GetComponentInChildren<DesktopMockVRHandVisual>(true);
        }

        if (leftHandVisibility == null && leftHandRoot != null)
        {
            leftHandVisibility = leftHandRoot.GetComponentInChildren<DesktopMockVRHandVisibility>(true);
        }

        if (rightHandVisibility == null && rightHandRoot != null)
        {
            rightHandVisibility = rightHandRoot.GetComponentInChildren<DesktopMockVRHandVisibility>(true);
        }

        _leftHandRenderers = CacheHandSourceRenderers(leftHandRoot);
        _rightHandRenderers = CacheHandSourceRenderers(rightHandRoot);
    }

    private static Renderer[] CacheHandSourceRenderers(Transform handRoot)
    {
        if (handRoot == null)
        {
            return System.Array.Empty<Renderer>();
        }

        Renderer[] renderers = handRoot.GetComponentsInChildren<Renderer>(true);
        System.Collections.Generic.List<Renderer> sourceRenderers = new System.Collections.Generic.List<Renderer>(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsInsidePriorityOverlay(renderer.transform))
            {
                continue;
            }

            sourceRenderers.Add(renderer);
        }

        return sourceRenderers.ToArray();
    }

    private static bool IsInsidePriorityOverlay(Transform candidate)
    {
        while (candidate != null)
        {
            if (candidate.name == "__HandPriorityOverlay")
            {
                return true;
            }

            candidate = candidate.parent;
        }

        return false;
    }

    private void KeepHandsFramedForDesktopView()
    {
        if (!keepFreeHandsInView || _camera == null)
        {
            return;
        }

        if (!IsHolding || _heldHandRoot != leftHandRoot)
        {
            KeepHandFramedForDesktopView(leftHandRoot, _leftHandRenderers, Time.deltaTime);
        }

        if (!IsHolding || _heldHandRoot != rightHandRoot)
        {
            KeepHandFramedForDesktopView(rightHandRoot, _rightHandRenderers, Time.deltaTime);
        }
    }

    private void KeepHandFramedForDesktopView(Transform handRoot, Renderer[] renderers, float deltaTime)
    {
        if (handRoot == null || renderers == null || renderers.Length == 0)
        {
            return;
        }

        if (!TryGetHandRenderBounds(renderers, out Bounds bounds))
        {
            return;
        }

        Vector3 viewportCenter = _camera.WorldToViewportPoint(bounds.center);
        if (viewportCenter.z <= _camera.nearClipPlane)
        {
            return;
        }

        float minX = handViewportHorizontalPadding;
        float maxX = 1f - handViewportHorizontalPadding;
        float targetX = Mathf.Clamp(viewportCenter.x, minX, maxX);
        float targetY = Mathf.Clamp(viewportCenter.y, handViewportBottomPadding, handViewportTopLimit);

        if (Mathf.Approximately(targetX, viewportCenter.x)
            && Mathf.Approximately(targetY, viewportCenter.y))
        {
            return;
        }

        Vector3 targetCenter = _camera.ViewportToWorldPoint(new Vector3(targetX, targetY, viewportCenter.z));
        Vector3 correction = targetCenter - bounds.center;
        float followT = 1f - Mathf.Exp(-handViewportNudgeSpeed * deltaTime);
        handRoot.position += correction * followT;
    }

    private static bool TryGetHandRenderBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static Transform EnsureAttachPoint(Transform handRoot, Transform assignedAttachPoint, string childName, Vector3 localPosition)
    {
        if (handRoot == null)
        {
            return assignedAttachPoint;
        }

        if (assignedAttachPoint != null)
        {
            assignedAttachPoint.localPosition = localPosition;
            assignedAttachPoint.localRotation = Quaternion.identity;
            assignedAttachPoint.localScale = Vector3.one;
            return assignedAttachPoint;
        }

        Transform existing = handRoot.Find(childName);
        if (existing != null)
        {
            existing.localPosition = localPosition;
            existing.localRotation = Quaternion.identity;
            existing.localScale = Vector3.one;
            return existing;
        }

        GameObject attachPointObject = new GameObject(childName);
        Transform attachPoint = attachPointObject.transform;
        attachPoint.SetParent(handRoot, false);
        attachPoint.localPosition = localPosition;
        attachPoint.localRotation = Quaternion.identity;
        attachPoint.localScale = Vector3.one;
        return attachPoint;
    }

    private void MoveHandAttachToWorldPosition(Transform handRoot, Transform attachPoint, Vector3 attachTargetPosition, float deltaTime)
    {
        if (handRoot == null)
        {
            return;
        }

        float followT = 1f - Mathf.Exp(-handFollowSpeed * deltaTime);
        Vector3 rootTargetPosition = GetRootPositionForAttachTarget(handRoot, attachPoint, attachTargetPosition);
        handRoot.position = Vector3.Lerp(handRoot.position, rootTargetPosition, followT);
    }

    private void MoveHandAttachToWorldPositionImmediate(Transform handRoot, Transform attachPoint, Vector3 attachTargetPosition)
    {
        if (handRoot == null)
        {
            return;
        }

        handRoot.position = GetRootPositionForAttachTarget(handRoot, attachPoint, attachTargetPosition);
    }

    private void MoveHandAttachToWorldPose(
        Transform handRoot,
        Transform attachPoint,
        Vector3 attachTargetPosition,
        Quaternion attachTargetRotation,
        float deltaTime)
    {
        if (handRoot == null)
        {
            return;
        }

        float followT = 1f - Mathf.Exp(-handFollowSpeed * deltaTime);
        Quaternion rootTargetRotation = GetRootRotationForAttachTarget(attachPoint, attachTargetRotation);
        Vector3 rootTargetPosition = GetRootPositionForAttachTarget(attachPoint, attachTargetPosition, rootTargetRotation);
        handRoot.rotation = Quaternion.Slerp(handRoot.rotation, rootTargetRotation, followT);
        handRoot.position = Vector3.Lerp(handRoot.position, rootTargetPosition, followT);
    }

    private void MoveHandAttachToWorldPoseImmediate(
        Transform handRoot,
        Transform attachPoint,
        Vector3 attachTargetPosition,
        Quaternion attachTargetRotation)
    {
        if (handRoot == null)
        {
            return;
        }

        Quaternion rootTargetRotation = GetRootRotationForAttachTarget(attachPoint, attachTargetRotation);
        handRoot.rotation = rootTargetRotation;
        handRoot.position = GetRootPositionForAttachTarget(attachPoint, attachTargetPosition, rootTargetRotation);
    }

    private Vector3 GetRootPositionForAttachTarget(Transform handRoot, Transform attachPoint, Vector3 attachTargetPosition)
    {
        if (attachPoint == null || attachPoint == handRoot)
        {
            return attachTargetPosition;
        }

        return attachTargetPosition - handRoot.TransformVector(attachPoint.localPosition);
    }

    private Vector3 GetRootPositionForAttachTarget(Transform attachPoint, Vector3 attachTargetPosition, Quaternion rootTargetRotation)
    {
        if (attachPoint == null)
        {
            return attachTargetPosition;
        }

        return attachTargetPosition - rootTargetRotation * attachPoint.localPosition;
    }

    private Quaternion GetRootRotationForAttachTarget(Transform attachPoint, Quaternion attachTargetRotation)
    {
        if (attachPoint == null)
        {
            return attachTargetRotation;
        }

        return attachTargetRotation * Quaternion.Inverse(attachPoint.localRotation);
    }

    private bool MoveHeldLeverHandToGripPose(float deltaTime)
    {
        return MoveHandToLeverGripPose(_heldHandRoot, deltaTime);
    }

    private bool MoveHeldLeverHandToGripPoseImmediate()
    {
        if (_heldLever == null || _heldHandRoot == null)
        {
            return false;
        }

        if (!_heldLever.TryGetGripPose(_heldHandRoot == leftHandRoot, out Vector3 gripPosition, out Quaternion gripRotation))
        {
            return false;
        }

        gripPosition = ConstrainHandAttachTarget(_heldHandRoot, gripPosition, true);
        MoveHandAttachToWorldPoseImmediate(_heldHandRoot, GetAttachPoint(_heldHandRoot), gripPosition, gripRotation);
        return true;
    }

    private bool MoveHandToLeverGripPose(Transform handRoot, float deltaTime)
    {
        if (_heldLever == null || handRoot == null)
        {
            return false;
        }

        if (!_heldLever.TryGetGripPose(handRoot == leftHandRoot, out Vector3 gripPosition, out Quaternion gripRotation))
        {
            return false;
        }

        gripPosition = ConstrainHandAttachTarget(handRoot, gripPosition, true);
        MoveHandAttachToWorldPose(handRoot, GetAttachPoint(handRoot), gripPosition, gripRotation, deltaTime);
        return true;
    }

    private void ResetHandToIdle(Transform handRoot, Vector3 idleLocalPosition)
    {
        if (handRoot == null)
        {
            return;
        }

        handRoot.localPosition = idleLocalPosition;
        SetHandRotation(handRoot);
    }

    private void UpdateHandGripVisuals()
    {
        float leftGrip = idleGrip;
        float rightGrip = idleGrip;

        if (IsHolding)
        {
            float selectedGrip = _heldLever != null
                ? (trainingGhostHandsMode ? trainingLeverGrabGrip : leverGrabGrip)
                : (trainingGhostHandsMode ? trainingBodyGrabGrip : bodyGrabGrip);
            leftGrip = _heldHandRoot == leftHandRoot || _activeHand == DesktopHandSide.Both ? selectedGrip : idleGrip;
            rightGrip = _heldHandRoot == rightHandRoot || _activeHand == DesktopHandSide.Both ? selectedGrip : idleGrip;
        }
        else if (_activeHand == DesktopHandSide.Left && _leftHandHoveringInteractable)
        {
            leftGrip = trainingGhostHandsMode
                ? trainingHoverGrip
                : (_leftHandHoveringLever ? leverHoverGrip : bodyHoverGrip);
        }
        else if (_activeHand == DesktopHandSide.Right && _rightHandHoveringInteractable)
        {
            rightGrip = trainingGhostHandsMode
                ? trainingHoverGrip
                : (_rightHandHoveringLever ? leverHoverGrip : bodyHoverGrip);
        }
        else if (_activeHand == DesktopHandSide.Both)
        {
            if (_leftHandHoveringInteractable)
            {
                leftGrip = trainingGhostHandsMode
                    ? trainingHoverGrip
                    : (_leftHandHoveringLever ? leverHoverGrip : bodyHoverGrip);
            }

            if (_rightHandHoveringInteractable)
            {
                rightGrip = trainingGhostHandsMode
                    ? trainingHoverGrip
                    : (_rightHandHoveringLever ? leverHoverGrip : bodyHoverGrip);
            }
        }

        // Active-hand "ready" grip: when not holding/hovering, the selected
        // hand closes slightly so the user can tell which side is currently
        // wired to LMB grab — without dragging the hand into the camera view.
        if (!IsHolding)
        {
            bool leftSelected = _activeHand == DesktopHandSide.Left || _activeHand == DesktopHandSide.Both;
            bool rightSelected = _activeHand == DesktopHandSide.Right || _activeHand == DesktopHandSide.Both;
            if (leftSelected) leftGrip = Mathf.Max(leftGrip, activeHandReadyGrip);
            if (rightSelected) rightGrip = Mathf.Max(rightGrip, activeHandReadyGrip);
        }

        if (leftHandVisual != null)
        {
            leftHandVisual.SetGripTarget(leftGrip);
        }

        if (rightHandVisual != null)
        {
            rightHandVisual.SetGripTarget(rightGrip);
        }

        UpdateHandVisibilityPriority(leftGrip, rightGrip);
    }

    private void UpdateHandVisibilityPriority(float leftGrip, float rightGrip)
    {
        if (leftHandVisibility != null)
        {
            float leftPriority = Mathf.Max(
                leftGrip * 0.45f,
                _leftHandHoveringInteractable ? 0.16f : 0f);
            leftHandVisibility.SetPriorityHint(leftPriority);
        }

        if (rightHandVisibility != null)
        {
            float rightPriority = Mathf.Max(
                rightGrip * 0.45f,
                _rightHandHoveringInteractable ? 0.16f : 0f);
            rightHandVisibility.SetPriorityHint(rightPriority);
        }
    }

    private void SetHandRotation(Transform handRoot)
    {
        if (handRoot != null)
        {
            handRoot.localRotation = Quaternion.Euler(GetIdleLocalEuler(handRoot));
        }
    }

    private Vector3 GetIdleLocalEuler(Transform handRoot)
    {
        return handRoot == leftHandRoot ? leftHandIdleLocalEuler : rightHandIdleLocalEuler;
    }

    private Vector3 GetSurfaceAnchorPosition(RaycastHit hit, float surfaceOffset)
    {
        return hit.point + hit.normal.normalized * surfaceOffset;
    }

    private void SetHandHoveringInteractable(Transform handRoot, bool hovering, bool hoveringLever)
    {
        if (handRoot == leftHandRoot)
        {
            _leftHandHoveringInteractable = hovering;
            _leftHandHoveringLever = hovering && hoveringLever;
        }
        else if (handRoot == rightHandRoot)
        {
            _rightHandHoveringInteractable = hovering;
            _rightHandHoveringLever = hovering && hoveringLever;
        }
    }

    private Vector3 ConstrainHandAttachTarget(Transform handRoot, Vector3 targetPosition, bool collideWithWorld)
    {
        if (handRoot == null)
        {
            return targetPosition;
        }

        bool leftHand = handRoot == leftHandRoot;
        Vector3 shoulderPosition = GetShoulderPosition(leftHand);
        Vector3 shoulderToTarget = targetPosition - shoulderPosition;
        float shoulderDistance = shoulderToTarget.magnitude;
        if (shoulderDistance > maxArmReach && shoulderDistance > 0.0001f)
        {
            targetPosition = shoulderPosition + shoulderToTarget / shoulderDistance * maxArmReach;
        }

        targetPosition = PushOutsideBodyVolume(targetPosition);

        if (collideWithWorld)
        {
            targetPosition = ClampHandTargetAgainstWorld(shoulderPosition, targetPosition, handRoot);
        }

        return targetPosition;
    }

    private Vector3 ClampHandTargetAgainstWorld(Vector3 shoulderPosition, Vector3 targetPosition, Transform handRoot)
    {
        Vector3 shoulderToTarget = targetPosition - shoulderPosition;
        float distance = shoulderToTarget.magnitude;
        if (distance < handCollisionRadius)
        {
            return targetPosition;
        }

        Vector3 direction = shoulderToTarget / distance;
        int hitCount = Physics.SphereCastNonAlloc(
            shoulderPosition,
            handCollisionRadius,
            direction,
            _handCastHits,
            distance,
            handCollisionLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _handCastHits[i];
            if (!IsValidHandContact(hit.collider, handRoot))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
            }
        }

        if (!float.IsPositiveInfinity(nearestDistance))
        {
            float clampedDistance = Mathf.Max(0f, nearestDistance - handContactSkin);
            return shoulderPosition + direction * clampedDistance;
        }

        return targetPosition;
    }

    private bool IsValidHandContact(Collider collider, Transform handRoot)
    {
        if (collider == null)
        {
            return false;
        }

        Transform colliderTransform = collider.transform;
        if (colliderTransform == transform
            || (_characterController != null && collider == _characterController))
        {
            return false;
        }

        if (IsTransformInside(colliderTransform, leftHandRoot)
            || IsTransformInside(colliderTransform, rightHandRoot))
        {
            return false;
        }

        if (_heldBody != null && collider.attachedRigidbody == _heldBody)
        {
            return false;
        }

        if (_heldLever != null && IsTransformInside(colliderTransform, _heldLever.transform))
        {
            return false;
        }

        if (handRoot != null && IsTransformInside(colliderTransform, handRoot))
        {
            return false;
        }

        return true;
    }

    private Vector3 PushOutsideBodyVolume(Vector3 targetPosition)
    {
        Vector3 chestPosition = GetBodyPoint(chestLocalPosition);
        Vector3 abdomenPosition = GetBodyPoint(abdomenLocalPosition);
        Vector3 nearestBodyPoint = ClosestPointOnSegment(chestPosition, abdomenPosition, targetPosition);
        Vector3 bodyToTarget = targetPosition - nearestBodyPoint;
        float distance = bodyToTarget.magnitude;
        if (distance >= torsoRadius)
        {
            return targetPosition;
        }

        Vector3 pushDirection = distance > 0.0001f
            ? bodyToTarget / distance
            : transform.forward;
        return nearestBodyPoint + pushDirection * torsoRadius;
    }

    private Vector3 ClampHeldBodyMove(Rigidbody body, Vector3 currentPosition, Vector3 targetPosition)
    {
        if (body == null)
        {
            return targetPosition;
        }

        Vector3 delta = targetPosition - currentPosition;
        float distance = delta.magnitude;
        if (distance < 0.0001f)
        {
            return targetPosition;
        }

        Vector3 direction = delta / distance;
        if (body.SweepTest(direction, out RaycastHit hit, distance + heldBodyContactSkin, QueryTriggerInteraction.Ignore))
        {
            float clampedDistance = Mathf.Max(0f, hit.distance - heldBodyContactSkin);
            return currentPosition + direction * clampedDistance;
        }

        return targetPosition;
    }

    private Vector3 GetShoulderPosition(bool leftHand)
    {
        return GetBodyPoint(leftHand ? leftShoulderLocalPosition : rightShoulderLocalPosition);
    }

    private Vector3 GetBodyPoint(Vector3 localPosition)
    {
        Quaternion bodyYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        return transform.position + bodyYaw * localPosition;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 segment = end - start;
        float lengthSq = segment.sqrMagnitude;
        if (lengthSq < 0.0001f)
        {
            return start;
        }

        float t = Vector3.Dot(point - start, segment) / lengthSq;
        return start + segment * Mathf.Clamp01(t);
    }

    private static bool IsTransformInside(Transform candidate, Transform root)
    {
        if (candidate == null || root == null)
        {
            return false;
        }

        return candidate == root || candidate.IsChildOf(root);
    }

    private static bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private float GetClampedDistanceAlongRay(Ray ray, Vector3 worldPosition)
    {
        float projectedDistance = Vector3.Dot(worldPosition - ray.origin, ray.direction.normalized);
        return Mathf.Clamp(projectedDistance, minHoldDistance, maxHoldDistance);
    }

    private static float NormalizePitch(float pitch)
    {
        return pitch > 180f ? pitch - 360f : pitch;
    }

    private static void SetLinearVelocity(Rigidbody body, Vector3 value)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = value;
#else
        body.velocity = value;
#endif
    }

    private void DrawBodyProxyGizmos()
    {
        if (!Application.isPlaying && _camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        Vector3 leftShoulder = GetShoulderPosition(true);
        Vector3 rightShoulder = GetShoulderPosition(false);
        Vector3 chest = GetBodyPoint(chestLocalPosition);
        Vector3 abdomen = GetBodyPoint(abdomenLocalPosition);

        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.85f);
        Gizmos.DrawWireSphere(leftShoulder, 0.045f);
        Gizmos.DrawWireSphere(rightShoulder, 0.045f);
        Gizmos.DrawLine(leftShoulder, rightShoulder);

        Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.25f);
        Gizmos.DrawWireSphere(leftShoulder, maxArmReach);
        Gizmos.DrawWireSphere(rightShoulder, maxArmReach);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
        Gizmos.DrawLine(chest, abdomen);
        Gizmos.DrawWireSphere(chest, torsoRadius);
        Gizmos.DrawWireSphere(abdomen, torsoRadius);

        float capsuleRadius = Mathf.Max(0.05f, bodyRadius);
        float capsuleHeight = Mathf.Max(capsuleRadius * 2f + 0.01f, bodyHeight);
        float capsuleHalfSegment = Mathf.Max(0f, capsuleHeight * 0.5f - capsuleRadius);
        Vector3 capsuleCenter = transform.position + Vector3.down * Mathf.Max(0f, bodyEyeHeight - capsuleHeight * 0.5f);
        Vector3 capsuleTop = capsuleCenter + Vector3.up * capsuleHalfSegment;
        Vector3 capsuleBottom = capsuleCenter - Vector3.up * capsuleHalfSegment;

        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.55f);
        Gizmos.DrawWireSphere(capsuleTop, capsuleRadius);
        Gizmos.DrawWireSphere(capsuleBottom, capsuleRadius);
        Gizmos.DrawLine(capsuleTop + Vector3.forward * capsuleRadius, capsuleBottom + Vector3.forward * capsuleRadius);
        Gizmos.DrawLine(capsuleTop - Vector3.forward * capsuleRadius, capsuleBottom - Vector3.forward * capsuleRadius);
        Gizmos.DrawLine(capsuleTop + Vector3.right * capsuleRadius, capsuleBottom + Vector3.right * capsuleRadius);
        Gizmos.DrawLine(capsuleTop - Vector3.right * capsuleRadius, capsuleBottom - Vector3.right * capsuleRadius);
    }
}
