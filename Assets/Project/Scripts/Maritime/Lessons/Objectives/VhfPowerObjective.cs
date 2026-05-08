using UnityEngine;
using MaritimeLMS.Vhf;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Completes when the wired <see cref="VhfRadio"/> reports
    /// <c>IsPoweredOn == true</c>. SOLAS Chapter IV / GMDSS requires VHF
    /// powered up before departure with continuous Channel 16 watch.
    /// </summary>
    [AddComponentMenu("Maritime LMS/Lessons/VHF Power Objective")]
    public sealed class VhfPowerObjective : LessonObjectiveBase
    {
        [Header("Target Equipment")]
        [SerializeField] private VhfRadio vhf;

        private void Awake()
        {
            if (vhf == null) vhf = FindFirstObjectByType<VhfRadio>();
        }

        private void Update()
        {
            if (!IsActive || vhf == null) return;
            if (vhf.IsPoweredOn) MarkCompleted();
        }
    }
}
