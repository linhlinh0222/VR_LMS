using UnityEngine;
using System.Collections.Generic;

namespace MaritimeLMS
{
    public enum LessonStatus { Pending, InProgress, Completed }

    [System.Serializable]
    public class LessonObjective
    {
        public string description;
        public bool isCompleted;
    }

    /// <summary>
    /// Central manager for Learning Management System (LMS) lesson tracking.
    /// Follows a singleton pattern for global access to lesson state.
    /// </summary>
    public class MaritimeLMSManager : MonoBehaviour
    {
        public static MaritimeLMSManager Instance { get; private set; }

        [Header("Lesson Data")]
        public string currentLessonTitle = "Bridge Interaction Basics";
        public List<LessonObjective> objectives = new List<LessonObjective>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Returns the current lesson title.
        /// </summary>
        public string GetLessonTitle() => currentLessonTitle;

        /// <summary>
        /// Returns the list of objectives for the current lesson.
        /// </summary>
        public List<LessonObjective> GetObjectives() => objectives;

        public void EnsureObjectives(params string[] descriptions)
        {
            objectives ??= new List<LessonObjective>();
            for (int i = 0; i < descriptions.Length; i++)
            {
                if (i < objectives.Count)
                {
                    if (string.IsNullOrWhiteSpace(objectives[i].description))
                    {
                        objectives[i].description = descriptions[i];
                    }

                    continue;
                }

                objectives.Add(new LessonObjective
                {
                    description = descriptions[i],
                    isCompleted = false
                });
            }
        }

        public bool IsObjectiveCompleted(int index)
        {
            return objectives != null
                && index >= 0
                && index < objectives.Count
                && objectives[index].isCompleted;
        }

        /// <summary>
        /// Marks an objective as completed by index.
        /// </summary>
        public void CompleteObjective(int index)
        {
            if (index >= 0 && index < objectives.Count && !objectives[index].isCompleted)
            {
                objectives[index].isCompleted = true;
                Debug.Log($"Objective Completed: {objectives[index].description}");
                CheckLessonCompletion();
            }
        }

        private void CheckLessonCompletion()
        {
            bool allDone = true;
            foreach (var obj in objectives)
            {
                if (!obj.isCompleted)
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
            {
                Debug.Log($"<color=green>Lesson Fully Completed: {currentLessonTitle}</color>");
            }
        }
    }
}
