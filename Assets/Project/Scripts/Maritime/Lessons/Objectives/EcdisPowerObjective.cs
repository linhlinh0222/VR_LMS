using UnityEngine;
using MaritimeLMS.Ecdis;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Completes when the wired <see cref="ElectronicChartDisplay"/> reports
    /// <c>IsPowered == true</c>. SOLAS V/19.2.10 mandates an operational
    /// ECDIS as the primary navigation chart on most ships.
    /// </summary>
    [AddComponentMenu("Maritime LMS/Lessons/ECDIS Power Objective")]
    public sealed class EcdisPowerObjective : LessonObjectiveBase
    {
        [Header("Target Equipment")]
        [SerializeField] private ElectronicChartDisplay ecdis;

        private void Awake()
        {
            if (ecdis == null) ecdis = FindFirstObjectByType<ElectronicChartDisplay>();
        }

        private void Update()
        {
            if (!IsActive || ecdis == null) return;
            if (ecdis.IsPowered) MarkCompleted();
        }
    }
}
