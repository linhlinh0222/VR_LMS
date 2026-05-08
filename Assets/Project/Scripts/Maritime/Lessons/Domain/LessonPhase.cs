namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Phases of a maritime LMS lesson, modelled on the IMO Model Course
    /// 1.07 Bridge Watchkeeping pedagogical flow and STCW 2010 BRM
    /// (Bridge Resource Management) competency assessment structure.
    /// </summary>
    /// <remarks>
    /// The flow mirrors how a real cadet would be taken through a bridge
    /// procedure by an instructor: <c>Briefing</c> sets the scenario,
    /// <c>Familiarization</c> walks them around the equipment,
    /// <c>Guided</c> coaches them through the procedure with hints,
    /// <c>Assessment</c> repeats it without coaching for grading, and
    /// <c>Debrief</c> reviews the score before <c>Complete</c>.
    /// </remarks>
    public enum LessonPhase
    {
        NotStarted,
        Briefing,
        Familiarization,
        Guided,
        Assessment,
        Debrief,
        Complete
    }
}
