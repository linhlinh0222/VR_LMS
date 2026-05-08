using UnityEngine;
using MaritimeLMS.Telegraph;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Completes when the wired <see cref="EngineOrderTelegraph"/>'s answered
    /// position equals <see cref="requiredPosition"/>. Watching the answered
    /// (engine-room confirmed) position rather than the ordered position
    /// means the cadet has both moved the lever and waited for the simulated
    /// engine acknowledgement, matching real-bridge departure procedure.
    /// </summary>
    [AddComponentMenu("Maritime LMS/Lessons/Telegraph Position Objective")]
    public sealed class TelegraphPositionObjective : LessonObjectiveBase
    {
        [Header("Target Equipment")]
        [SerializeField] private EngineOrderTelegraph telegraph;

        [Header("Success Condition")]
        [SerializeField] private TelegraphPosition requiredPosition = TelegraphPosition.SlowAhead;

        [Tooltip("If true, completion requires the engine-acknowledged position. " +
                 "If false, completion is granted as soon as the lever order matches.")]
        [SerializeField] private bool requireEngineAcknowledgement = true;

        private void Awake()
        {
            if (telegraph == null) telegraph = FindFirstObjectByType<EngineOrderTelegraph>();
        }

        private void Update()
        {
            if (!IsActive || telegraph == null) return;
            TelegraphPosition observed = requireEngineAcknowledgement
                ? telegraph.CurrentAnswer
                : telegraph.CurrentOrder;
            if (observed == requiredPosition) MarkCompleted();
        }
    }
}
