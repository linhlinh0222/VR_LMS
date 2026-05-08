using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Modal briefing panel shown while the lesson is in
    /// <see cref="LessonPhase.Briefing"/>. The cadet reads the scenario,
    /// then presses the Begin button which calls
    /// <see cref="LessonStateMachine.AdvancePhase"/>.
    /// </summary>
    /// <remarks>
    /// Decoupled from <see cref="LessonStateMachine"/> via inspector
    /// references so the same view component can target any lesson without
    /// code changes. Set the briefing phase's <c>requiresManualAdvance</c>
    /// flag so the state machine waits on the button click rather than
    /// auto-progressing.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Lessons/UI/Briefing View")]
    public sealed class BriefingView : MonoBehaviour
    {
        [Header("Lesson")]
        [SerializeField] private LessonStateMachine lesson;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button beginButton;

        private void Awake()
        {
            if (lesson == null) lesson = FindFirstObjectByType<LessonStateMachine>();
            if (beginButton != null) beginButton.onClick.AddListener(OnBeginClicked);
        }

        private void OnDestroy()
        {
            if (beginButton != null) beginButton.onClick.RemoveListener(OnBeginClicked);
        }

        private void Update()
        {
            if (lesson == null || panel == null) return;
            bool show = lesson.CurrentPhase == LessonPhase.Briefing;
            if (panel.activeSelf != show) panel.SetActive(show);
            if (!show) return;
            if (titleText != null) titleText.text = lesson.LessonTitle;
            if (bodyText != null) bodyText.text = lesson.BriefingText;
        }

        private void OnBeginClicked()
        {
            if (lesson != null) lesson.AdvancePhase();
        }
    }
}
