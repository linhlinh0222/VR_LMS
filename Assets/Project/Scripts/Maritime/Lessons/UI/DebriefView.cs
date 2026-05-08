using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Modal debrief panel shown when the lesson reaches
    /// <see cref="LessonPhase.Complete"/>. Displays the score table
    /// (objectives completed, time taken, wrong-hand penalties) and a letter
    /// grade aligned with STCW competency assessment bands.
    /// </summary>
    /// <remarks>
    /// Pulls the wrong-hand penalty count from an optional
    /// <see cref="WrongHandPenaltyTracker"/>; if no tracker is wired the
    /// penalty count is reported as zero. Pressing Restart calls
    /// <see cref="LessonStateMachine.RestartLesson"/> which resets every
    /// objective and re-enters the briefing phase.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Lessons/UI/Debrief View")]
    public sealed class DebriefView : MonoBehaviour
    {
        [Header("Lesson")]
        [SerializeField] private LessonStateMachine lesson;
        [SerializeField] private WrongHandPenaltyTracker penaltyTracker;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreBodyText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (lesson == null) lesson = FindFirstObjectByType<LessonStateMachine>();
            if (penaltyTracker == null) penaltyTracker = FindFirstObjectByType<WrongHandPenaltyTracker>();
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        private void Update()
        {
            if (lesson == null || panel == null) return;
            bool show = lesson.CurrentPhase == LessonPhase.Complete;
            if (panel.activeSelf != show) panel.SetActive(show);
            if (!show) return;

            int wrongHand = penaltyTracker != null ? penaltyTracker.Count : 0;
            LessonScore score = lesson.BuildScore(wrongHand);

            if (titleText != null) titleText.text = $"{score.lessonTitle} — Debrief";
            if (gradeText != null) gradeText.text = score.LetterGrade();
            if (scoreBodyText != null) scoreBodyText.text = FormatScoreBody(score);
        }

        private static string FormatScoreBody(LessonScore score)
        {
            int min = (int)(score.totalSeconds / 60f);
            int sec = (int)(score.totalSeconds % 60f);
            return
                $"Time: {min:00}:{sec:00}\n" +
                $"Objectives: {score.objectivesCompleted} / {score.objectivesTotal} ({score.CompletionPercent:F0}%)\n" +
                $"Wrong-hand penalties: {score.wrongHandPenalties}";
        }

        private void OnRestartClicked()
        {
            if (penaltyTracker != null) penaltyTracker.ResetCount();
            if (lesson != null) lesson.RestartLesson();
        }
    }
}
