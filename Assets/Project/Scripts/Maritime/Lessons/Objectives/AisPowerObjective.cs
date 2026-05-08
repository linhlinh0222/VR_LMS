using UnityEngine;
using MaritimeLMS.Ais;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Completes when the wired <see cref="AisTransceiver"/> reports
    /// <c>IsPoweredOn == true</c>. Models the "AIS Class A on" requirement
    /// from SOLAS V/19.2.4 (AIS shall be in operation at all times when
    /// the ship is underway).
    /// </summary>
    [AddComponentMenu("Maritime LMS/Lessons/AIS Power Objective")]
    public sealed class AisPowerObjective : LessonObjectiveBase
    {
        [Header("Target Equipment")]
        [SerializeField] private AisTransceiver ais;

        private void Awake()
        {
            if (ais == null) ais = FindFirstObjectByType<AisTransceiver>();
        }

        private void Update()
        {
            if (!IsActive || ais == null) return;
            if (ais.IsPoweredOn) MarkCompleted();
        }
    }
}
