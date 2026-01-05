using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UDA2.UI.Common
{
    public class ConfirmDialog : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action onYes;
        private Action onNo;

        private const string PrefabPath = "UI/Common/ConfirmDialog";

        public static void Show(string questionKey, Action onYes, Action onNo = null)
        {
            var prefab = Resources.Load<ConfirmDialog>(PrefabPath);
            var instance = Instantiate(prefab);
            instance.Init(questionKey, onYes, onNo);
        }

        private void Init(string questionKey, Action onYes, Action onNo)
        {
            this.onYes = onYes;
            this.onNo = onNo;
            questionText.text = questionKey; // Здесь должна быть локализация
            yesButton.onClick.AddListener(OnYes);
            noButton.onClick.AddListener(OnNo);
        }

        private void OnYes()
        {
            onYes?.Invoke();
            Close();
        }

        private void OnNo()
        {
            onNo?.Invoke();
            Close();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
