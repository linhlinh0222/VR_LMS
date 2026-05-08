using UnityEngine;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Objective that completes the moment <see cref="NotifyTriggered"/> is
    /// invoked. The intended wiring is to drag the equipment's UnityEvent
    /// (e.g. <c>VhfRadio.DistressAlertSent</c>) into this objective's
    /// inspector and select <see cref="NotifyTriggered"/>.
    /// </summary>
    /// <remarks>
    /// Use this when the equipment already raises a one-shot event for the
    /// success state. For continuously-checkable conditions
    /// (heading/position/power), prefer one of the polling objectives.
    /// </remarks>
    [AddComponentMenu("Maritime LMS/Lessons/Event Objective")]
    public sealed class EventObjective : LessonObjectiveBase
    {
        public void NotifyTriggered()
        {
            if (!IsActive) return;
            MarkCompleted();
        }
    }
}
