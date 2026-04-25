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

        public static ConfirmDialog Show(
            ConfirmDialog prefab,
            Transform parent,
            string questionKey,
            Action onYes,
            Action onNo = null)
        {
            if (prefab == null)
            {
                Debug.LogError("ConfirmDialog.Show: prefab is null.");
                return null;
            }
            ConfirmDialog instance = parent != null
                ? Instantiate(prefab, parent)
                : Instantiate(prefab);
            instance.Init(questionKey, onYes, onNo);
            return instance;
        }

        private void Init(string questionKey, Action onYes, Action onNo)
        {
            this.onYes = onYes;
            this.onNo = onNo;
            ApplyQuestionLocalization(questionKey);
            // yesButton.onClick.RemoveAllListeners();
            // noButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(HandleYes);
            noButton.onClick.AddListener(HandleNo);
        }

        private void ApplyQuestionLocalization(string key)
        {
            var localized = questionText != null ? questionText.GetComponent<LocalizedGlobalComponent>() : null;
            if (localized != null)
            {
                localized.Key = key;
                localized.ClearArgs();
                localized.UpdateText();
            }
            else
                questionText.text = key;

            return;
        }

        private void HandleYes()
        {
            onYes?.Invoke();
            Close();
        }

        private void HandleNo()
        {
            onNo?.Invoke();
            Close();
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
