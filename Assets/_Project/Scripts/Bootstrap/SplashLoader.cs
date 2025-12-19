using UnityEngine;
using UDA2.SceneFlow;

namespace UDA2.Bootstrap
{
    public class SplashLoader : MonoBehaviour
    {
        public float splashDuration = 2f;
        private void Start()
        {
            StartCoroutine(ShowSplashAndLoadMenu());
        }
        private System.Collections.IEnumerator ShowSplashAndLoadMenu()
        {
            yield return new WaitForSeconds(splashDuration);
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.LoadScene("MainMenuScene", null, 2f);
        }
    }
}
