using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UDA2.UI
{
    public class MainChoiceHUDController : MonoBehaviour
    {
        [Serializable]
        public class ChoiceBinding
        {
            public Button button;
            public GameObject targetHUD;
        }

        [Header("Main HUD (этот объект)")]
        [SerializeField] private GameObject mainChoiceHUD;

        [Header("Кнопки выбора -> HUD")]
        [SerializeField] private ChoiceBinding[] choices;

        [Header("Transition")]
        [SerializeField] private bool useFadeTransition = true;
        [Min(0f)]
        [SerializeField] private float fadeDuration = 0.2f;

        private UnityAction[] openActions;
        private bool isTransitioning;

        private void Awake()
        {
            WireButtons();
        }

        private void OnEnable()
        {
            if (mainChoiceHUD != null)
                mainChoiceHUD.SetActive(true);
        }

        private void WireButtons()
        {
            if (choices == null)
                return;

            if (openActions == null || openActions.Length != choices.Length)
                openActions = new UnityAction[choices.Length];

            for (int i = 0; i < choices.Length; i++)
            {
                var index = i;
                var choice = choices[i];
                if (choice == null || choice.button == null)
                    continue;

                if (openActions[i] == null)
                    openActions[i] = () => OpenChoice(index);

                choice.button.onClick.RemoveListener(openActions[i]);
                choice.button.onClick.AddListener(openActions[i]);
            }
        }

        public void OpenChoice(int index)
        {
            if (isTransitioning)
                return;

            if (choices == null || index < 0 || index >= choices.Length)
                return;

            var choice = choices[index];
            if (choice == null || choice.targetHUD == null)
                return;

            if (!useFadeTransition || fadeDuration <= 0f)
            {
                SetActiveImmediate(mainChoiceHUD, false);
                SetActiveImmediate(choice.targetHUD, true);
                return;
            }

            StartCoroutine(SwitchHudWithFade(mainChoiceHUD, choice.targetHUD));
        }

        private IEnumerator SwitchHudWithFade(GameObject fromHud, GameObject toHud)
        {
            isTransitioning = true;
            CanvasGroup fromGroup = null;

            if (fromHud != null && fromHud.activeSelf)
            {
                fromGroup = EnsureCanvasGroup(fromHud);
                fromGroup.interactable = false;
                fromGroup.blocksRaycasts = false;
                yield return Fade(fromGroup, fromGroup.alpha, 0f, fadeDuration);
            }

            if (toHud != null)
            {
                toHud.SetActive(true);
                var toGroup = EnsureCanvasGroup(toHud);
                toGroup.interactable = false;
                toGroup.blocksRaycasts = false;
                toGroup.alpha = 0f;
                yield return Fade(toGroup, 0f, 1f, fadeDuration);
                toGroup.interactable = true;
                toGroup.blocksRaycasts = true;
            }

            if (fromHud != null)
            {
                fromHud.SetActive(false);
                if (fromGroup != null)
                    fromGroup.alpha = 1f;
            }

            isTransitioning = false;
        }

        private static void SetActiveImmediate(GameObject hud, bool active)
        {
            if (hud == null)
                return;

            hud.SetActive(active);
            var group = hud.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = active ? 1f : 0f;
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject hud)
        {
            var group = hud.GetComponent<CanvasGroup>();
            if (group == null)
                group = hud.AddComponent<CanvasGroup>();
            return group;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null)
                yield break;

            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            group.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            group.alpha = to;
        }
    }
}
