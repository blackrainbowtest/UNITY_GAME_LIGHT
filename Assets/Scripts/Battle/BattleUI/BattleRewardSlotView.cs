using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Battle.UI
{
    /// <summary>
    /// UI component responsible for rendering a single reward entry.
    /// The battle result modal should only instantiate prefabs and pass rewardId + count.
    /// </summary>
    public sealed class BattleRewardSlotView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;

        [Header("Icon Sources")]
        [Tooltip("Optional. Assign ItemDatabase asset to resolve item icons by itemId. Kept as Object to avoid asmdef coupling.")]
        [SerializeField] private UnityEngine.Object itemDatabase;

        public void SetItemDatabase(UnityEngine.Object db)
        {
            if (db == null)
                return;

            if (itemDatabase == null)
                itemDatabase = db;
        }

        public string RewardId { get; private set; }
        public int Count { get; private set; }

        public void Render(string rewardId, int count)
        {
            RewardId = string.IsNullOrWhiteSpace(rewardId) ? string.Empty : rewardId.Trim();
            Count = Mathf.Max(0, count);

            var icon = ResolveIcon(RewardId);

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (countText != null)
            {
                // Keep same style as inventory slots: show number only when > 1.
                countText.text = Count > 1 ? Count.ToString() : string.Empty;
            }
        }

        private Sprite ResolveIcon(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                return null;

            return TryResolveIconFromDatabase(rewardId);
        }

        private Sprite TryResolveIconFromDatabase(string rewardId)
        {
            if (itemDatabase == null)
                return null;

            try
            {
                var dbType = itemDatabase.GetType();
                var getById = dbType.GetMethod("GetById");
                if (getById == null)
                    return null;

                var def = getById.Invoke(itemDatabase, new object[] { rewardId.Trim() });
                if (def == null)
                    return null;

                var defType = def.GetType();
                var iconProp = defType.GetProperty("Icon");
                if (iconProp == null)
                    return null;

                return iconProp.GetValue(def) as Sprite;
            }
            catch
            {
                return null;
            }
        }

    }
}
