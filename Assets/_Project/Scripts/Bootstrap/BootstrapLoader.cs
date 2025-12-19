using UnityEngine;
using UDA2.SceneFlow;

namespace UDA2.Bootstrap
{
    public class BootstrapLoader : MonoBehaviour
    {
        private void Start()
        {
            // После инициализации менеджеров загружаем SplashScene
            UnityEngine.SceneManagement.SceneManager.LoadScene("SplashScene");
        }
    }
}
