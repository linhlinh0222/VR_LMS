using UnityEngine;

namespace MaritimeLMS
{
    public sealed class MaritimePerformanceBootstrap : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 72;
        [SerializeField] private float fixedTimeStep = 0.02f;
        [SerializeField] private float maximumDeltaTime = 0.05f;
        [SerializeField] private bool disableVSync = true;
        [SerializeField] private bool disableAutoSyncTransforms = true;
        [SerializeField] private bool runInBackground = true;

        private void Awake()
        {
            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }

            if (targetFrameRate > 0)
            {
                Application.targetFrameRate = targetFrameRate;
            }

            if (fixedTimeStep > 0f)
            {
                Time.fixedDeltaTime = fixedTimeStep;
            }

            if (maximumDeltaTime > 0f)
            {
                Time.maximumDeltaTime = maximumDeltaTime;
            }

            if (disableAutoSyncTransforms)
            {
#pragma warning disable CS0618
                Physics.autoSyncTransforms = false;
#pragma warning restore CS0618
            }

            Application.runInBackground = runInBackground;
            Application.backgroundLoadingPriority = ThreadPriority.Low;
        }
    }
}
