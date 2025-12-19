using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LogoFadeIn : MonoBehaviour
    {
        public float fadeDuration = 0.5f;
        private CanvasGroup canvasGroup;
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }
        private void Start()
        {
            StartCoroutine(FadeIn());
        }
        private System.Collections.IEnumerator FadeIn()
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }
}
