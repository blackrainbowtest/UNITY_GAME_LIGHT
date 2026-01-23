using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// Opens the game menu from battle scene.
    /// </summary>
    public class BattleMenuButtonController : MonoBehaviour
    {
        [SerializeField] private GameObject gameMenuPrefab;
        [SerializeField] private Transform uiParent;

        private GameObject menuInstance;

        public void OnMenuButtonClicked()
        {
            if (menuInstance == null)
            {
                menuInstance = Instantiate(gameMenuPrefab, uiParent);
            }

            menuInstance.SetActive(true);
        }
    }
}
