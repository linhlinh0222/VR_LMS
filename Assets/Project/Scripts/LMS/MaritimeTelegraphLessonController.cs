using UnityEngine;

namespace MaritimeLMS
{
    public sealed class MaritimeTelegraphLessonController : MonoBehaviour
    {
        [SerializeField] private MaritimeLMSManager manager;
        [SerializeField] private ShipBridgeInput bridgeInput;
        [SerializeField] private DesktopLeverInteractable telegraph;
        [SerializeField] private ShipController ship;
        [SerializeField, Range(0f, 1f)] private float aheadThreshold = 0.55f;
        [SerializeField, Range(0f, 1f)] private float stopThreshold = 0.12f;
        [SerializeField] private float minimumAheadSpeed = 1.2f;
        [SerializeField] private bool overwriteObjectiveDescriptions = true;

        private static readonly string[] ObjectiveDescriptions =
        {
            "\u0110\u1ea9y telegraph sang Ahead",
            "T\u00e0u nh\u1eadn l\u1ec7nh ti\u1ebfn",
            "\u0110\u01b0a telegraph v\u1ec1 Stop an to\u00e0n"
        };

        private bool _aheadOrderSeen;
        private bool _shipAnsweredAhead;

        private void Start()
        {
            ResolveReferences();
            ConfigureObjectives();
        }

        private void Update()
        {
            ResolveReferences();
            if (manager == null || telegraph == null)
            {
                return;
            }

            float signedTelegraph = (telegraph.NormalizedValue - 0.5f) * 2f;
            if (!_aheadOrderSeen && signedTelegraph >= aheadThreshold)
            {
                _aheadOrderSeen = true;
                manager.CompleteObjective(0);
            }

            if (_aheadOrderSeen && !_shipAnsweredAhead && ship != null && ship.CurrentSpeed >= minimumAheadSpeed)
            {
                _shipAnsweredAhead = true;
                manager.CompleteObjective(1);
            }

            if (_shipAnsweredAhead && Mathf.Abs(signedTelegraph) <= stopThreshold)
            {
                manager.CompleteObjective(2);
            }
        }

        private void ResolveReferences()
        {
            manager = manager != null ? manager : MaritimeLMSManager.Instance;
            bridgeInput = bridgeInput != null ? bridgeInput : FindAnyObjectByType<ShipBridgeInput>();
            telegraph = telegraph != null ? telegraph : bridgeInput != null ? bridgeInput.telegraph : null;
            ship = ship != null ? ship : bridgeInput != null ? bridgeInput.ship : FindAnyObjectByType<ShipController>();
        }

        private void ConfigureObjectives()
        {
            if (manager == null)
            {
                return;
            }

            manager.EnsureObjectives(ObjectiveDescriptions);
            if (!overwriteObjectiveDescriptions || manager.objectives == null)
            {
                return;
            }

            int count = Mathf.Min(ObjectiveDescriptions.Length, manager.objectives.Count);
            for (int i = 0; i < count; i++)
            {
                manager.objectives[i].description = ObjectiveDescriptions[i];
            }
        }
    }
}
