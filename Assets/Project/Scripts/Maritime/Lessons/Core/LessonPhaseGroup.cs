using System;
using UnityEngine;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// A single phase in a lesson's timeline. Owns the objectives that must
    /// complete before the lesson advances and the instructional text shown
    /// to the cadet while in this phase.
    /// </summary>
    /// <remarks>
    /// Designed to be authored as a serialised array element on
    /// <see cref="LessonStateMachine"/>; not a MonoBehaviour itself. Keeping
    /// it a plain serialisable class lets the state machine remain a single
    /// inspector with the entire lesson plan visible at once.
    /// </remarks>
    [Serializable]
    public sealed class LessonPhaseGroup
    {
        public LessonPhase phase = LessonPhase.Familiarization;
        public string title = "Phase title";
        [TextArea(2, 6)] public string instructionText = "Tell the cadet what to do here.";
        public LessonObjectiveBase[] objectives = Array.Empty<LessonObjectiveBase>();

        [Tooltip("True = wait for every objective to complete. False = first one progresses the lesson.")]
        public bool requireAllComplete = true;

        [Tooltip("Minimum seconds to spend in this phase before allowing progression.")]
        [Min(0f)] public float minPhaseSeconds = 0f;

        [Tooltip("If true, this phase only advances when AdvancePhase() is called externally — used for briefing / debrief modals that wait on a UI button.")]
        public bool requiresManualAdvance = false;
    }
}
