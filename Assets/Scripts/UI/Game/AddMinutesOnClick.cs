using UnityEngine;
using UnityEngine.UI;
using UDA2.GameTime;

namespace UDA2.UI.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class AddMinutesOnClick : MonoBehaviour
    {
        [SerializeField] private int minutesDelta = 15;

        private void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            var button = GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            EnsureDefaultSaveExists();
            GameTimeAPI.AddMinutes(minutesDelta);
        }

        private static void EnsureDefaultSaveExists()
        {
            if (global::GameState.Instance == null)
                return;

            if (global::GameState.Instance.CurrentSave != null)
                return;

            global::GameState.Instance.CurrentSave = SaveData.CreateDefault(Application.version);
        }
    }
}
