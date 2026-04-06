using System.Collections;
using UnityEngine;

namespace UDA2.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class SceneReadyAutoSignal : MonoBehaviour, ISceneReady
    {
        [SerializeField, Min(0)] private int waitFramesBeforeSignal = 1;

        private Coroutine signalRoutine;

        private void OnEnable()
        {
            if (signalRoutine != null)
                StopCoroutine(signalRoutine);

            signalRoutine = StartCoroutine(SignalRoutine());
        }

        private void OnDisable()
        {
            if (signalRoutine != null)
            {
                StopCoroutine(signalRoutine);
                signalRoutine = null;
            }
        }

        public void SignalReady()
        {
            SceneFlowManager.Instance?.NotifySceneReady();
        }

        private IEnumerator SignalRoutine()
        {
            int frames = Mathf.Max(0, waitFramesBeforeSignal);
            while (frames > 0)
            {
                frames--;
                yield return null;
            }

            signalRoutine = null;
            SignalReady();
        }
    }
}
