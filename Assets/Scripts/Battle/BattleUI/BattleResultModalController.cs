using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Battle.UI
{
    /// <summary>
    /// Simple modal for showing battle results.
    /// UI-only component, no combat logic.
    /// </summary>
    public sealed class BattleResultModalController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardsText;
        [SerializeField] private Button okButton;

        private Action onOk;

        private void Awake()
        {
            if (okButton != null)
                okButton.onClick.AddListener(OnOkClicked);

            Hide();
        }

        public void Show(Game.Battle.BattleResultData data, Action onOkClicked)
        {
            onOk = onOkClicked;

            if (titleText != null)
                titleText.text = data.PlayerWon ? "Victory" : "Defeat";

            if (rewardsText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Gold: {data.GoldGained}");
                sb.AppendLine($"EXP: {data.ExpGained}");

                if (data.Items != null && data.Items.Count > 0)
                {
                    sb.AppendLine("Items:");
                    foreach (var item in data.Items)
                    {
                        var label = string.IsNullOrEmpty(item.ItemId) ? "<unknown>" : item.ItemId;
                        var count = item.Count;
                        sb.AppendLine(count > 1 ? $"- {label} x{count}" : $"- {label}");
                    }
                }
                rewardsText.text = sb.ToString().TrimEnd();
            }

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnOkClicked()
        {
            var cb = onOk;
            onOk = null;
            Hide();
            cb?.Invoke();
        }
    }
}
