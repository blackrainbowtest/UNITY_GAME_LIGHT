using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ExternalLinkButton : MonoBehaviour
    {
        [SerializeField] private string url = "https://";
        [SerializeField] private bool bindOnAwake = true;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();

            if (bindOnAwake && button != null)
                button.onClick.AddListener(OpenLink);
        }

        private void OnDestroy()
        {
            if (bindOnAwake && button != null)
                button.onClick.RemoveListener(OpenLink);
        }

        public void OpenLink()
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("ExternalLinkButton: URL is empty.", this);
                return;
            }

            string normalizedUrl = NormalizeUrl(url);
            Application.OpenURL(normalizedUrl);
        }

        private static string NormalizeUrl(string rawUrl)
        {
            string trimmed = rawUrl.Trim();

            if (trimmed.StartsWith("http://") || trimmed.StartsWith("https://"))
                return trimmed;

            return "https://" + trimmed;
        }
    }
}
