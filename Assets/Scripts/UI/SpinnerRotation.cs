using UnityEngine;

namespace UDA2.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SpinnerRotation : MonoBehaviour
    {
        [Tooltip("Скорость вращения спиннера (градусов в секунду)")]
        public float speed = 180f;
        [Tooltip("Вращать по часовой стрелке")]
        public bool clockwise = true;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            float direction = clockwise ? -1f : 1f;
            rectTransform.Rotate(0f, 0f, direction * speed * Time.unscaledDeltaTime);
        }
    }
}
