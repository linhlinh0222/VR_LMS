using UnityEngine;
using UnityEngine.Events;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Abstract base for every objective the cadet must complete during a
    /// lesson. Subclasses decide *how* the objective is detected (polling a
    /// piece of equipment, waiting on a UnityEvent, etc.) but share the
    /// activation lifecycle and bookkeeping defined here.
    /// </summary>
    /// <remarks>
    /// Designed so the <see cref="LessonStateMachine"/> can drive a list of
    /// objectives via <see cref="Activate"/> and read <see cref="IsCompleted"/>
    /// without caring about each objective's detection strategy. Concrete
    /// objectives are expected to call <see cref="MarkCompleted"/> exactly
    /// once when their condition becomes true.
    /// </remarks>
    public abstract class LessonObjectiveBase : MonoBehaviour
    {
        [Header("Objective")]
        [SerializeField] private string objectiveId;
        [SerializeField, TextArea(1, 3)] private string description = "Describe the cadet's goal here.";
        [SerializeField] private LessonPhase phase = LessonPhase.Familiarization;

        [Header("Events")]
        public UnityEvent OnActivated;
        public UnityEvent OnCompleted;

        public string ObjectiveId => objectiveId;
        public string Description => description;
        public LessonPhase Phase => phase;

        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }
        public float ActivatedAt { get; private set; }
        public float CompletedAt { get; private set; }
        public float ElapsedSeconds => IsCompleted
            ? Mathf.Max(0f, CompletedAt - ActivatedAt)
            : (IsActive ? Mathf.Max(0f, Time.time - ActivatedAt) : 0f);

        /// <summary>Begin watching for completion. No-op if already completed.</summary>
        public void Activate()
        {
            if (IsCompleted || IsActive) return;
            IsActive = true;
            ActivatedAt = Time.time;
            OnObjectiveActivated();
            OnActivated?.Invoke();
        }

        /// <summary>Stop watching without resetting completion state.</summary>
        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            OnObjectiveDeactivated();
        }

        /// <summary>
        /// Mark this objective as completed. Called by subclasses the moment
        /// the success condition is met. Idempotent; later calls are ignored.
        /// </summary>
        protected void MarkCompleted()
        {
            if (IsCompleted) return;
            IsCompleted = true;
            CompletedAt = Time.time;
            IsActive = false;
            OnObjectiveCompleted();
            OnCompleted?.Invoke();
        }

        protected virtual void OnObjectiveActivated() { }
        protected virtual void OnObjectiveDeactivated() { }
        protected virtual void OnObjectiveCompleted() { }

        /// <summary>Reset to pre-activation state so the lesson can replay.</summary>
        public virtual void ResetObjective()
        {
            IsActive = false;
            IsCompleted = false;
            ActivatedAt = 0f;
            CompletedAt = 0f;
        }
    }
}
