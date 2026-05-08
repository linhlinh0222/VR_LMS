using UnityEngine;
using UnityEngine.Events;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Lesson-side evaluator that scores whether a lever was pulled with the
    /// correct hand. Subscribes to a <see cref="DesktopLeverInteractable"/>'s
    /// grab state and reads which hand was active on the
    /// <see cref="DesktopMockVRController"/> at the moment of grab.
    /// </summary>
    /// <remarks>
    /// Designed as a separate component (composition over modification) so
    /// neither <see cref="DesktopLeverInteractable"/> nor
    /// <see cref="DesktopMockVRController"/> needs to know about lesson
    /// scoring. Drop one of these onto any lever GameObject the lesson
    /// wants to grade. STCW 2010 BRM scenarios commonly require operating
    /// the engine telegraph with a specific hand to free the other for the
    /// helm wheel — this component fires the appropriate event so a
    /// scoring system or audio cue can react.
    ///
    /// "Both hands" mode counts as <see cref="DesktopMockVRController.DesktopHandSide.Right"/>
    /// for evaluation purposes (the dominant hand on a real bridge), but
    /// callers can override by setting <see cref="bothHandsTreatedAs"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Hand-Aware Lever Evaluator")]
    public sealed class HandAwareLeverEvaluator : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Lever to monitor. Auto-discovered on the same GameObject if null.")]
        [SerializeField] private DesktopLeverInteractable lever;

        [Tooltip("Player rig. The first DesktopMockVRController in the scene is used if null.")]
        [SerializeField] private DesktopMockVRController playerRig;

        [Header("Assessment")]
        [Tooltip("Which hand should the trainee use to operate this lever?")]
        [SerializeField] private DesktopMockVRController.DesktopHandSide correctHand =
            DesktopMockVRController.DesktopHandSide.Right;

        [Tooltip("How to score 'Both hands selected'. Defaults to Right (dominant hand on a typical bridge).")]
        [SerializeField] private DesktopMockVRController.DesktopHandSide bothHandsTreatedAs =
            DesktopMockVRController.DesktopHandSide.Right;

        [Header("Events")]
        public UnityEvent CorrectHandUsed;
        public UnityEvent WrongHandUsed;
        public UnityEvent<DesktopMockVRController.DesktopHandSide> AnyGrabEvaluated;

        private bool _wasGrabbed;
        private DesktopMockVRController.DesktopHandSide _lastHandUsed;

        /// <summary>Hand that was active the last time the lever was grabbed.</summary>
        public DesktopMockVRController.DesktopHandSide LastHandUsed => _lastHandUsed;

        /// <summary>True if the most recent grab matched <see cref="correctHand"/>.</summary>
        public bool LastGrabWasCorrect => Normalize(_lastHandUsed) == correctHand;

        private void Awake()
        {
            if (lever == null) lever = GetComponentInChildren<DesktopLeverInteractable>(true);
            if (playerRig == null) playerRig = FindFirstObjectByType<DesktopMockVRController>();
            if (lever == null)
                Debug.LogError($"{nameof(HandAwareLeverEvaluator)} on '{name}' has no DesktopLeverInteractable.", this);
            if (playerRig == null)
                Debug.LogError($"{nameof(HandAwareLeverEvaluator)} on '{name}' has no DesktopMockVRController.", this);
        }

        private void Update()
        {
            if (lever == null || playerRig == null) return;

            bool grabbed = lever.IsGrabbed;
            if (grabbed && !_wasGrabbed)
            {
                _lastHandUsed = playerRig.ActiveHand;
                var normalized = Normalize(_lastHandUsed);
                AnyGrabEvaluated?.Invoke(_lastHandUsed);
                if (normalized == correctHand) CorrectHandUsed?.Invoke();
                else WrongHandUsed?.Invoke();
            }
            _wasGrabbed = grabbed;
        }

        private DesktopMockVRController.DesktopHandSide Normalize(DesktopMockVRController.DesktopHandSide h)
            => h == DesktopMockVRController.DesktopHandSide.Both ? bothHandsTreatedAs : h;
    }
}
