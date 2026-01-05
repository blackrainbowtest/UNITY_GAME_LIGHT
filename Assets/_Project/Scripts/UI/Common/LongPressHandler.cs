using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UDA2.UI.Common
{
    public class LongPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float holdTime = 0.7f;
        public event Action LongPressed;
        private bool isPointerDown;
        private float timer;

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            timer = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetState();
        }

        private void Update()
        {
            if (!isPointerDown) return;
            timer += Time.unscaledDeltaTime;
            if (timer >= holdTime)
            {
                ResetState();
                LongPressed?.Invoke();
            }
        }

        private void ResetState()
        {
            isPointerDown = false;
            timer = 0f;
        }
    }
}
