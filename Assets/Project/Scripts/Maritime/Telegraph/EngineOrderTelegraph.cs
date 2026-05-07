using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MaritimeLMS.Telegraph
{
    /// <summary>
    /// Engine Order Telegraph: bridge-to-engine-room mechanical communication
    /// device with two pointers (red = ordered, green = answered) and a bell
    /// that rings on order change.
    /// </summary>
    /// <remarks>
    /// User grabs the lever; lever angle drives the ordered (red) pointer.
    /// On release, the order snaps to the nearest discrete detent. The engine
    /// room then "acknowledges" by animating the answered (green) pointer to
    /// match the order after a configurable delay (simulating real reaction
    /// time, per STCW 2010 BRM scenario design).
    ///
    /// The 7 detents are spaced 30° apart on the lever pivot's local Y axis,
    /// per the EOT FBX authored under <c>04_Engine_Telegraph/</c> in
    /// <see href="https://github.com/meiiie/model_lms"/>.
    ///
    /// This component intentionally exposes <see cref="NormalizedValue"/> with
    /// the same shape as <c>DesktopLeverInteractable</c> so existing lesson
    /// controllers (e.g. <c>MaritimeTelegraphLessonController</c>) keep working
    /// without modification.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Engine Order Telegraph")]
    public sealed class EngineOrderTelegraph : MonoBehaviour
    {
        // Detent angles in lever-local Y degrees, indexed by (int)TelegraphPosition.
        // Astern is negative, ahead is positive — opposite of clockwise screen
        // direction but matches the brass-dial print convention worldwide.
        public static readonly float[] PositionAngles =
        {
            -90f, // FullAstern
            -60f, // HalfAstern
            -30f, // SlowAstern
              0f, // Stop
             30f, // SlowAhead
             60f, // HalfAhead
             90f, // FullAhead
        };

        private const float MinAngle = -90f;
        private const float MaxAngle = 90f;
        private const float DetentTolerance = 15f; // half of 30° spacing

        [Header("Pivots (auto-discovered by name when null)")]
        [Tooltip("Empty transform whose local Y rotation is the ordered (RED) pointer angle.")]
        [SerializeField] private Transform orderedPivot;

        [Tooltip("Empty transform whose local Y rotation is the answered (GREEN) pointer angle.")]
        [SerializeField] private Transform answeredPivot;

        [Tooltip("Lever handle pivot. Mirrors orderedPivot in real bridges; here we drive both " +
                 "from a single user-facing 'target angle'.")]
        [SerializeField] private Transform leverPivot;

        [Header("Audio")]
        [SerializeField] private AudioSource bellAudio;

        [Header("Engine room response")]
        [Tooltip("Seconds before answered pointer starts following an order change.")]
        [SerializeField, Range(0f, 5f)] private float engineResponseDelaySeconds = 1.5f;

        [Tooltip("How fast the answered pointer rotates toward the order (degrees / second).")]
        [SerializeField, Range(15f, 360f)] private float answeredRotationSpeed = 90f;

        [Header("Lever follow")]
        [Tooltip("Lever exponential follow speed when not grabbed. Higher = stiffer.")]
        [SerializeField, Range(1f, 60f)] private float leverFollowSpeed = 18f;

        [Header("Events")]
        public UnityEvent<TelegraphPosition> OrderChanged;
        public UnityEvent<TelegraphPosition> EngineAcknowledged;

        private TelegraphPosition _currentOrder = TelegraphPosition.Stop;
        private TelegraphPosition _currentAnswer = TelegraphPosition.Stop;
        private float _leverCurrentAngle;
        private float _leverTargetAngle;
        private float _grabAngleOffset;
        private Coroutine _ackCoroutine;

        /// <summary>Bridge order currently posted to engine room.</summary>
        public TelegraphPosition CurrentOrder => _currentOrder;

        /// <summary>Last position the engine room acknowledged.</summary>
        public TelegraphPosition CurrentAnswer => _currentAnswer;

        /// <summary>True when ordered and answered match (engine has caught up).</summary>
        public bool IsAcknowledged => _currentOrder == _currentAnswer;

        /// <summary>Lever angle in degrees, [-90, 90].</summary>
        public float LeverAngle => _leverCurrentAngle;

        /// <summary>Lever angle remapped to [0, 1] so legacy lesson controllers can read this.</summary>
        public float NormalizedValue => Mathf.InverseLerp(MinAngle, MaxAngle, _leverCurrentAngle);

        /// <summary>Whether a user (or scripted driver) is currently moving the lever.</summary>
        public bool IsGrabbed { get; private set; }

        // --- Public API: programmatic / lesson scripts ---------------------

        /// <summary>
        /// Post an order to the engine room. Rings the bell, snaps the lever
        /// to the detent, and schedules engine acknowledgement.
        /// </summary>
        public void SetOrder(TelegraphPosition position)
        {
            float targetAngle = PositionAngles[(int)position];
            _leverTargetAngle = targetAngle;
            _leverCurrentAngle = targetAngle; // hard-snap when set programmatically
            ApplyLeverAngle(_leverCurrentAngle);
            CommitOrder(position);
        }

        /// <summary>Set the lever target angle directly (legacy API parity).</summary>
        public void SetTargetAngle(float angle)
        {
            _leverTargetAngle = Mathf.Clamp(angle, MinAngle, MaxAngle);
        }

        // --- Public API: interaction -----------------------------------------

        /// <summary>Driver grabbed the lever. Stops auto-follow until <see cref="EndGrab"/>.</summary>
        public void BeginGrab(Vector3 handAnchorWorldPosition)
        {
            IsGrabbed = true;
            _grabAngleOffset = _leverCurrentAngle - GetHandAngle(handAnchorWorldPosition);
        }

        /// <summary>Driver moves the lever during a grab.</summary>
        public void UpdateGrab(Vector3 handAnchorWorldPosition)
        {
            float candidate = GetHandAngle(handAnchorWorldPosition) + _grabAngleOffset;
            _leverCurrentAngle = Mathf.Clamp(candidate, MinAngle, MaxAngle);
            ApplyLeverAngle(_leverCurrentAngle);
            // Update target so post-release follow does not snap backward.
            _leverTargetAngle = _leverCurrentAngle;
        }

        /// <summary>Driver released the lever; snap to nearest detent and post the order.</summary>
        public void EndGrab()
        {
            IsGrabbed = false;
            if (TryFindNearestPosition(_leverCurrentAngle, out var snapped))
            {
                _leverTargetAngle = PositionAngles[(int)snapped];
                CommitOrder(snapped);
            }
        }

        // --- Lifecycle -------------------------------------------------------

        private void Awake()
        {
            AutoDiscoverPivots();
            if (leverPivot == null || orderedPivot == null || answeredPivot == null)
            {
                Debug.LogError(
                    $"{nameof(EngineOrderTelegraph)} on '{name}' could not resolve all required " +
                    $"pivots. Check that the FBX exposes 'handle_lever_pivot', " +
                    $"'pointer_ordered_pivot', and 'pointer_answered_pivot'.", this);
                enabled = false;
                return;
            }

            _leverCurrentAngle = 0f;
            _leverTargetAngle = 0f;
            ApplyLeverAngle(0f);
            ApplyAnsweredAngle(0f);
        }

        private void Update()
        {
            if (!IsGrabbed)
            {
                float follow = 1f - Mathf.Exp(-leverFollowSpeed * Time.deltaTime);
                _leverCurrentAngle = Mathf.Lerp(_leverCurrentAngle, _leverTargetAngle, follow);
                ApplyLeverAngle(_leverCurrentAngle);
            }
        }

        // --- Internals -------------------------------------------------------

        private void CommitOrder(TelegraphPosition order)
        {
            if (order == _currentOrder) return;

            _currentOrder = order;
            if (bellAudio != null) bellAudio.Play();
            OrderChanged?.Invoke(order);

            if (_ackCoroutine != null) StopCoroutine(_ackCoroutine);
            _ackCoroutine = StartCoroutine(AnswerEngineRoom(order));
        }

        private IEnumerator AnswerEngineRoom(TelegraphPosition order)
        {
            yield return new WaitForSeconds(engineResponseDelaySeconds);

            float targetAngle = PositionAngles[(int)order];
            float startAngle = ReadAnsweredAngle();
            float duration = Mathf.Max(0.05f, Mathf.Abs(targetAngle - startAngle) / answeredRotationSpeed);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ApplyAnsweredAngle(Mathf.LerpAngle(startAngle, targetAngle, t));
                yield return null;
            }
            ApplyAnsweredAngle(targetAngle);

            _currentAnswer = order;
            EngineAcknowledged?.Invoke(order);
            _ackCoroutine = null;
        }

        private void ApplyLeverAngle(float angleY)
        {
            // Lever and ordered pointer are mechanically the same shaft on a real
            // EOT, so they share an angle here. Pivots rotate on local Y.
            var rot = Quaternion.Euler(0f, angleY, 0f);
            if (leverPivot != null) leverPivot.localRotation = rot;
            if (orderedPivot != null) orderedPivot.localRotation = rot;
        }

        private void ApplyAnsweredAngle(float angleY)
        {
            if (answeredPivot != null)
                answeredPivot.localRotation = Quaternion.Euler(0f, angleY, 0f);
        }

        private float ReadAnsweredAngle()
        {
            if (answeredPivot == null) return 0f;
            float y = answeredPivot.localEulerAngles.y;
            return y > 180f ? y - 360f : y;
        }

        private float GetHandAngle(Vector3 handAnchorWorldPosition)
        {
            if (leverPivot == null) return _leverCurrentAngle;
            Transform stableSpace = leverPivot.parent != null ? leverPivot.parent : leverPivot;
            Vector3 local = stableSpace.InverseTransformPoint(handAnchorWorldPosition) - leverPivot.localPosition;
            if (local.sqrMagnitude < 0.0001f) return _leverCurrentAngle;
            // Lever rotates around Y; project to XZ plane.
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        private static bool TryFindNearestPosition(float angle, out TelegraphPosition position)
        {
            float bestDist = float.PositiveInfinity;
            int bestIdx = -1;
            for (int i = 0; i < PositionAngles.Length; i++)
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(angle, PositionAngles[i]));
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIdx = i;
                }
            }
            if (bestIdx < 0 || bestDist > DetentTolerance)
            {
                position = TelegraphPosition.Stop;
                return false;
            }
            position = (TelegraphPosition)bestIdx;
            return true;
        }

        private void AutoDiscoverPivots()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "pointer_ordered_pivot":
                        if (orderedPivot == null) orderedPivot = t; break;
                    case "pointer_answered_pivot":
                        if (answeredPivot == null) answeredPivot = t; break;
                    case "handle_lever_pivot":
                        if (leverPivot == null) leverPivot = t; break;
                }
            }
        }

        private void OnValidate()
        {
            // Snap inspector-edited target to detent so the model rests at a
            // known position when the designer sets initial values.
            if (Application.isPlaying) return;
            if (TryFindNearestPosition(_leverTargetAngle, out var snapped))
            {
                _leverTargetAngle = PositionAngles[(int)snapped];
            }
        }
    }
}
