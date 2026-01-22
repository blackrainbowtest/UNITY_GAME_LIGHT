using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Battle.UI
{
    public sealed class StatusIconView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text turnsText;

        public void Set(Sprite icon, int turnsLeft)
        {
            if (iconImage != null)
                iconImage.sprite = icon;

            if (turnsText != null)
            {
                if (turnsLeft > 0)
                {
                    turnsText.gameObject.SetActive(true);
                    turnsText.text = turnsLeft.ToString();
                }
                else
                {
                    turnsText.gameObject.SetActive(false);
                }
            }
        }
    }
}
