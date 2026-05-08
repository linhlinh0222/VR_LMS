using System;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Result of a completed lesson — what the cadet did well, where they
    /// burned time, and how often the wrong hand was used. Pure POCO so it
    /// can be serialised by xAPI / cmi5 telemetry layers later without
    /// dragging Unity types into the data model.
    /// </summary>
    [Serializable]
    public sealed class LessonScore
    {
        public string lessonTitle;
        public float totalSeconds;
        public int objectivesCompleted;
        public int objectivesTotal;
        public int wrongHandPenalties;

        public float CompletionPercent => objectivesTotal > 0
            ? 100f * objectivesCompleted / objectivesTotal
            : 0f;

        /// <summary>
        /// Letter grade following STCW competency assessment bands —
        /// A = exemplary, B = competent, C = needs supervised practice,
        /// D = re-train. Penalties from <see cref="wrongHandPenalties"/>
        /// drop the grade by one band each.
        /// </summary>
        public string LetterGrade()
        {
            int rank = 0; // A
            if (CompletionPercent < 100f) rank++;
            if (CompletionPercent < 80f) rank++;
            if (CompletionPercent < 60f) rank++;
            rank += wrongHandPenalties;
            rank = System.Math.Min(rank, 3);
            return rank switch
            {
                0 => "A",
                1 => "B",
                2 => "C",
                _ => "D"
            };
        }
    }
}
