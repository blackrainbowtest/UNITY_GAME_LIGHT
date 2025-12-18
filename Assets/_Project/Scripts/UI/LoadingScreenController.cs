using UnityEngine;

namespace UDA2.UI
{
    // Только визуализация загрузки
    public class LoadingScreenController : MonoBehaviour
    {
        public void Show()
        {
            // Показать UI загрузки
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            // Скрыть UI загрузки
            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            // Обновить прогресс-бар (реализуйте по необходимости)
        }
    }
}
