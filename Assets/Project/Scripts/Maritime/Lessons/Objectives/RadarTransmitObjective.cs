using UnityEngine;
using MaritimeLMS.Radar;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Completes when the wired <see cref="MarineRadar"/> reports
    /// <c>IsTransmitting == true</c> (i.e. switched out of Standby and
    /// actively sweeping). IMO/IEC 62388 mandates radar as a primary
    /// collision-avoidance aid; an off radar leaves a critical look-out gap.
    /// </summary>
    [AddComponentMenu("Maritime LMS/Lessons/Radar Transmit Objective")]
    public sealed class RadarTransmitObjective : LessonObjectiveBase
    {
        [Header("Target Equipment")]
        [SerializeField] private MarineRadar radar;

        private void Awake()
        {
            if (radar == null) radar = FindFirstObjectByType<MarineRadar>();
        }

        private void Update()
        {
            if (!IsActive || radar == null) return;
            if (radar.IsTransmitting) MarkCompleted();
        }
    }
}
