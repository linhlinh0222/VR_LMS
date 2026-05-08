using UnityEngine;
using MaritimeLMS.Compass;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Completes when the magnetic compass reads within
    /// <see cref="toleranceDegrees"/> of <see cref="requiredHeadingDegrees"/>
    /// for at least <see cref="requiredHoldSeconds"/> uninterrupted. The
    /// hold-time prevents accidental fly-throughs from counting as steady
    /// course-keeping (per IMO Model Course 1.07 helm-coaching guidance).
    /// </summary>
    [AddComponentMenu("Maritime LMS/Lessons/Helm Heading Objective")]
    public sealed class HelmHeadingObjective : LessonObjectiveBase
    {
        [Header("Target Equipment")]
        [SerializeField] private MagneticCompass compass;

        [Header("Success Condition")]
        [Tooltip("Required heading in degrees true [0, 360).")]
        [SerializeField, Range(0f, 360f)] private float requiredHeadingDegrees = 45f;
        [Tooltip("Acceptable absolute heading error.")]
        [SerializeField, Range(0.5f, 30f)] private float toleranceDegrees = 5f;
        [Tooltip("Cadet must hold within tolerance this long for the objective to count.")]
        [SerializeField, Range(0f, 10f)] private float requiredHoldSeconds = 2f;

        private float _withinSince = -1f;

        private void Awake()
        {
            if (compass == null) compass = FindFirstObjectByType<MagneticCompass>();
        }

        private void Update()
        {
            if (!IsActive || compass == null) return;

            float diff = Mathf.Abs(Mathf.DeltaAngle(compass.HeadingDegrees, requiredHeadingDegrees));
            if (diff <= toleranceDegrees)
            {
                if (_withinSince < 0f) _withinSince = Time.time;
                if (Time.time - _withinSince >= requiredHoldSeconds) MarkCompleted();
            }
            else
            {
                _withinSince = -1f;
            }
        }

        public override void ResetObjective()
        {
            base.ResetObjective();
            _withinSince = -1f;
        }
    }
}
