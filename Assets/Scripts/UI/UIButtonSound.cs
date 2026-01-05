using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI
{
    public class UIButtonSound : MonoBehaviour
    {
        private void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(PlaySound);
        }

        private void PlaySound()
        {
            if (UDA2.Audio.AudioManager.Instance != null)
                UDA2.Audio.AudioManager.Instance.PlayUiClick();
        }
    }
}
