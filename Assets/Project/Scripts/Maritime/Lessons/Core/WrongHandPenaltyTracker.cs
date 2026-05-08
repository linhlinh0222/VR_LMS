using UnityEngine;

namespace MaritimeLMS.Lessons
{
    /// <summary>
    /// Counts every <see cref="HandAwareLeverEvaluator.WrongHandUsed"/> event
    /// raised in the scene during a lesson run. The score builder reads
    /// <see cref="Count"/> at debrief time to drop the cadet's grade.
    /// </summary>
    /// <remarks>
    /// Lives as a separate component (composition) so adding more
    /// hand-aware evaluators in future lessons just requires dragging them
    /// into <c>evaluators</c> — no edit needed here. Auto-discovers all
    /// active evaluators in the scene if the array is left empty.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maritime LMS/Lessons/Wrong-Hand Penalty Tracker")]
    public sealed class WrongHandPenaltyTracker : MonoBehaviour
    {
        [Tooltip("Lever evaluators to listen on. Auto-discovered if empty.")]
        [SerializeField] private HandAwareLeverEvaluator[] evaluators;

        public int Count { get; private set; }

        private void Awake()
        {
            if (evaluators == null || evaluators.Length == 0)
            {
                evaluators = FindObjectsByType<HandAwareLeverEvaluator>(FindObjectsSortMode.None);
            }

            for (int i = 0; i < evaluators.Length; i++)
            {
                if (evaluators[i] != null)
                {
                    evaluators[i].WrongHandUsed.AddListener(OnWrongHand);
                }
            }
        }

        private void OnWrongHand() => Count++;

        public void ResetCount() => Count = 0;
    }
}
