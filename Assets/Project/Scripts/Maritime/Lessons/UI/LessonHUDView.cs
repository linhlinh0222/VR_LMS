using System.Text;
using TMPro;
using UnityEngine;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Heads-up display of the active lesson — current phase title, the
    /// objective list with checkmarks, and the elapsed timer. Wires onto a
    /// world-space Canvas so it remains readable in stereo VR as well as on
    /// the desktop simulator.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Lessons/UI/Lesson HUD View")]
    public sealed class LessonHUDView : MonoBehaviour
    {
        [Header("Lesson")]
        [SerializeField] private LessonStateMachine lesson;

        [Header("UI References (Canvas children)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text objectivesText;
        [SerializeField] private TMP_Text elapsedText;

        [Header("Format")]
        [SerializeField] private string completedSymbol = "✓";
        [SerializeField] private string pendingSymbol = "•";
        [SerializeField] private Color completedColor = new Color(0.40f, 0.85f, 0.45f);
        [SerializeField] private Color pendingColor = new Color(0.95f, 0.95f, 0.95f);

        private readonly StringBuilder _objectivesBuffer = new StringBuilder(256);

        private void Awake()
        {
            if (lesson == null) lesson = FindFirstObjectByType<LessonStateMachine>();
        }

        private void Update()
        {
            if (lesson == null) return;
            if (titleText != null) titleText.text = lesson.LessonTitle;
            if (phaseText != null) phaseText.text = lesson.CurrentPhaseGroup?.title ?? lesson.CurrentPhase.ToString();
            if (elapsedText != null) elapsedText.text = FormatMinutesSeconds(Time.time - lesson.StartedAt);
            if (objectivesText != null) WriteObjectives(objectivesText);
        }

        private void WriteObjectives(TMP_Text target)
        {
            LessonPhaseGroup group = lesson.CurrentPhaseGroup;
            _objectivesBuffer.Clear();
            if (group?.objectives != null)
            {
                for (int i = 0; i < group.objectives.Length; i++)
                {
                    LessonObjectiveBase obj = group.objectives[i];
                    if (obj == null) continue;
                    bool done = obj.IsCompleted;
                    string colorHex = ColorUtility.ToHtmlStringRGB(done ? completedColor : pendingColor);
                    string symbol = done ? completedSymbol : pendingSymbol;
                    _objectivesBuffer.Append("<color=#").Append(colorHex).Append('>')
                        .Append(symbol).Append(' ').Append(obj.Description)
                        .Append("</color>\n");
                }
            }
            target.text = _objectivesBuffer.ToString();
        }

        private static string FormatMinutesSeconds(float seconds)
        {
            float clamped = Mathf.Max(0f, seconds);
            int minutes = (int)(clamped / 60f);
            int secs = (int)(clamped % 60f);
            return $"{minutes:00}:{secs:00}";
        }
    }
}
