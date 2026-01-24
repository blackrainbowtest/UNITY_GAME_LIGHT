using UnityEngine;
using UDA2.SceneFlow;

namespace UDA2.UI
{
    public class LoadingScreenController : MonoBehaviour, ILoadingScreen
    {
        private void Awake()
        {
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.RegisterLoadingScreen(this);
        }

        private void OnDisable()
        {
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.UnregisterLoadingScreen(this);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            // Обновить прогресс-бар (реализуйте по необходимости)
        }
    }
}
