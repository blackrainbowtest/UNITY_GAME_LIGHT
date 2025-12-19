using UnityEngine;
using UDA2.SceneFlow;

namespace UDA2.Bootstrap
{
    public class BootstrapLoader : MonoBehaviour
    {
        private void Start()
        {
            // Запустить корутину показа логотипа
            StartCoroutine(ShowSplashAndLoadMenu());
        }

        private System.Collections.IEnumerator ShowSplashAndLoadMenu()
        {
            // Здесь можно добавить анимацию логотипа или ожидание клика
            yield return new WaitForSeconds(2f); // Показывать логотип 2 секунды

            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.LoadScene("MainMenuScene", null, 2f);
        }
    }
}
