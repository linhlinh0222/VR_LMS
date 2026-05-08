using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Drives a maritime LMS lesson from briefing through debrief. Holds an
    /// ordered list of <see cref="LessonPhaseGroup"/> entries; activates the
    /// objectives in the current phase, watches for completion, and
    /// transitions when the phase's success criteria are met.
    /// </summary>
    /// <remarks>
    /// Pedagogical structure mirrors IMO Model Course 1.07 (Bridge
    /// Watchkeeping) phasing: <c>Briefing → Familiarization → Guided →
    /// Assessment → Debrief</c>. The state machine is intentionally
    /// scenario-agnostic — the actual lesson is authored entirely in the
    /// inspector by adding objective components and assembling them into
    /// <see cref="LessonPhaseGroup"/> entries, so designers can ship new
    /// lessons without writing C# code.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Lessons/Lesson State Machine")]
    public sealed class LessonStateMachine : MonoBehaviour
    {
        [Header("Lesson")]
        [SerializeField] private string lessonTitle = "Bridge Familiarization & Departure";
        [SerializeField, TextArea(3, 8)] private string briefingText =
            "You are second officer on watch. The bridge is preparing for departure. " +
            "Power on the navigation equipment, set the engine telegraph to Slow Ahead, " +
            "steer to course 045°T, and complete a VHF distress test on Channel 16.";

        [Header("Phases")]
        [SerializeField] private LessonPhaseGroup[] phases = System.Array.Empty<LessonPhaseGroup>();

        [Header("Behaviour")]
        [SerializeField] private bool autoStart = true;

        [Header("Events")]
        public UnityEvent OnLessonStarted;
        public UnityEvent<LessonPhase> OnPhaseChanged;
        public UnityEvent<LessonPhaseGroup> OnPhaseEntered;
        public UnityEvent OnLessonCompleted;

        public string LessonTitle => lessonTitle;
        public string BriefingText => briefingText;
        public LessonPhase CurrentPhase { get; private set; } = LessonPhase.NotStarted;
        public LessonPhaseGroup CurrentPhaseGroup =>
            _currentIndex < 0 || _currentIndex >= phases.Length ? null : phases[_currentIndex];
        public IReadOnlyList<LessonPhaseGroup> Phases => phases;
        public float StartedAt { get; private set; }
        public bool IsRunning => _currentIndex >= 0 && _currentIndex < phases.Length;
        public bool HasCompleted => CurrentPhase == LessonPhase.Complete;

        private int _currentIndex = -1;
        private float _phaseStartedAt;

        private void Start()
        {
            if (autoStart) StartLesson();
        }

        public void StartLesson()
        {
            StartedAt = Time.time;
            OnLessonStarted?.Invoke();
            TransitionTo(0);
        }

        public void RestartLesson()
        {
            for (int p = 0; p < phases.Length; p++)
            {
                LessonObjectiveBase[] list = phases[p]?.objectives;
                if (list == null) continue;
                for (int o = 0; o < list.Length; o++)
                {
                    if (list[o] != null) list[o].ResetObjective();
                }
            }
            _currentIndex = -1;
            CurrentPhase = LessonPhase.NotStarted;
            StartLesson();
        }

        /// <summary>
        /// Force-advance from the current phase, used by Briefing / Debrief
        /// modals where the user clicks a button to proceed.
        /// </summary>
        public void AdvancePhase()
        {
            if (!IsRunning) return;
            TransitionTo(_currentIndex + 1);
        }

        public LessonScore BuildScore(int wrongHandPenalties)
        {
            int total = 0;
            int completed = 0;
            for (int p = 0; p < phases.Length; p++)
            {
                LessonObjectiveBase[] list = phases[p]?.objectives;
                if (list == null) continue;
                for (int o = 0; o < list.Length; o++)
                {
                    if (list[o] == null) continue;
                    total++;
                    if (list[o].IsCompleted) completed++;
                }
            }
            return new LessonScore
            {
                lessonTitle = lessonTitle,
                totalSeconds = Mathf.Max(0f, Time.time - StartedAt),
                objectivesCompleted = completed,
                objectivesTotal = total,
                wrongHandPenalties = wrongHandPenalties
            };
        }

        private void Update()
        {
            LessonPhaseGroup current = CurrentPhaseGroup;
            if (current == null) return;
            if (current.requiresManualAdvance) return;
            if (Time.time - _phaseStartedAt < current.minPhaseSeconds) return;

            if (HasPhaseSuccessCondition(current))
            {
                TransitionTo(_currentIndex + 1);
            }
        }

        private static bool HasPhaseSuccessCondition(LessonPhaseGroup group)
        {
            LessonObjectiveBase[] list = group.objectives;
            if (list == null || list.Length == 0)
            {
                // Empty phase auto-progresses once minPhaseSeconds has elapsed.
                return true;
            }

            bool allDone = true;
            bool anyDone = false;
            for (int i = 0; i < list.Length; i++)
            {
                LessonObjectiveBase obj = list[i];
                if (obj == null) continue;
                if (obj.IsCompleted) anyDone = true;
                else allDone = false;
            }

            return group.requireAllComplete ? allDone : anyDone;
        }

        private void TransitionTo(int newIndex)
        {
            // Deactivate old phase's objectives.
            if (_currentIndex >= 0 && _currentIndex < phases.Length)
            {
                LessonObjectiveBase[] previous = phases[_currentIndex]?.objectives;
                if (previous != null)
                {
                    for (int i = 0; i < previous.Length; i++)
                    {
                        if (previous[i] != null) previous[i].Deactivate();
                    }
                }
            }

            _currentIndex = newIndex;
            _phaseStartedAt = Time.time;

            // End of lesson.
            if (_currentIndex < 0 || _currentIndex >= phases.Length)
            {
                CurrentPhase = LessonPhase.Complete;
                OnPhaseChanged?.Invoke(CurrentPhase);
                OnLessonCompleted?.Invoke();
                return;
            }

            // Enter new phase.
            LessonPhaseGroup next = phases[_currentIndex];
            CurrentPhase = next.phase;
            OnPhaseChanged?.Invoke(CurrentPhase);
            OnPhaseEntered?.Invoke(next);

            LessonObjectiveBase[] activate = next.objectives;
            if (activate != null)
            {
                for (int i = 0; i < activate.Length; i++)
                {
                    if (activate[i] != null) activate[i].Activate();
                }
            }
        }
    }
}
