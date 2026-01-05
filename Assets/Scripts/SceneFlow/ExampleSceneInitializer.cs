using UnityEngine;
using System.Collections;
using UDA2.SceneFlow;

namespace UDA2.SceneFlow
{
    // Пример инициализатора сцены
    public class ExampleSceneInitializer : MonoBehaviour, ISceneReady
    {
        private IEnumerator Start()
        {
            // Ваша инициализация
            yield return new WaitForSeconds(1f); // пример задержки
            SignalReady();
        }

        public void SignalReady()
        {
            SceneFlowManager.Instance.NotifySceneReady();
        }
    }
}
