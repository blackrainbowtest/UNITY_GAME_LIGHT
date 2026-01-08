using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class IntroController : MonoBehaviour
{
    private enum IntroBranch { None, A, B }
    private IntroBranch selectedBranch = IntroBranch.None;
    [Header("Choice Dialog")]
    [SerializeField] private GameObject choiceDialog; // Панель с кнопками выбора
    [SerializeField] private Button interveneButton;
    [SerializeField] private Button turnAwayButton;
    [Header("Data")]
    [SerializeField] private IntroSequence introSequence;

    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text loreText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button textClickCatcher; // прозрачная кнопка поверх TextPanel

    [Header("Flow")]
    [SerializeField] private string firstFightSceneName = "FightScene";

    private int currentIndex = 0;
    private Coroutine autoAdvanceCoroutine;

    private void Awake()
    {
        skipButton.onClick.AddListener(SkipIntro);
        textClickCatcher.onClick.AddListener(NextFrame);
    }

    private void Start()
    {
        if (introSequence == null || introSequence.frames.Count == 0)
        {
            Debug.LogError("IntroController: IntroSequence is empty or missing.");
            return;
        }

        ShowFrame(0);
    }

    private void ShowFrame(int index)
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }


        if (index >= introSequence.frames.Count)
        {
            FinishIntro();
            return;
        }

        currentIndex = index;

        // Завершение основной ветки после 14A (index == 14)
        if (selectedBranch == IntroBranch.A && index > 14)
        {
            FinishIntro();
            return;
        }
        // Завершение альтернативной ветки после 13B (index == 16)
        if (selectedBranch == IntroBranch.B && index > 16)
        {
            FinishIntro();
            return;
        }

        IntroFrame frame = introSequence.frames[currentIndex];

        // Background
        if (backgroundImage != null && frame.background != null)
            backgroundImage.sprite = frame.background;

        // Локализация через компонент
        if (loreText != null)
        {
            // LocalizedTextSetter
            var setter = loreText.GetComponent<LocalizedTextSetter>();
            if (setter != null)
            {
                setter.key = frame.textKey;
                setter.UpdateText(); // вызываем без параметров
            }
            // LocalizedTextComponent
            var comp = loreText.GetComponent<LocalizedTextComponent>();
            if (comp != null)
            {
                comp.textKey = frame.textKey;
                comp.UpdateText();
            }
        }

        // Автопереход
        if (!frame.waitForClick && frame.autoDelay > 0f)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvance(frame.autoDelay));
        }
    }

    private IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextFrame();
    }

    public void NextFrame()
    {
        // Если сейчас показан 11-й фрейм — показываем диалог вместо перехода к следующему фрейму
        if (currentIndex == 11)
        {
            if (choiceDialog != null)
            {
                choiceDialog.SetActive(true);
                interveneButton.onClick.RemoveAllListeners();
                turnAwayButton.onClick.RemoveAllListeners();
                turnAwayButton.onClick.AddListener(() => {
                    choiceDialog.SetActive(false);
                    selectedBranch = IntroBranch.A;
                    ShowFrame(12); // 12A
                });
                interveneButton.onClick.AddListener(() => {
                    choiceDialog.SetActive(false);
                    selectedBranch = IntroBranch.B;
                    ShowFrame(15); // 12B
                });
            }
            return;
        }
        ShowFrame(currentIndex + 1);
    }

    public void SkipIntro()
    {
        FinishIntro();
    }

    private void FinishIntro()
    {
        // Сохраняем прогресс, если нужно
        if (GameState.Instance.CurrentSave == null)
            GameState.Instance.CurrentSave = new SaveData();
        GameState.Instance.CurrentSave.player.sceneName = firstFightSceneName;

        // Записываем результат интро
        string introResult = "skip";
        if (selectedBranch == IntroBranch.A) introResult = "A";
        else if (selectedBranch == IntroBranch.B) introResult = "B";
        if (GameState.Instance.CurrentSave.progress != null)
            GameState.Instance.CurrentSave.progress.introResult = introResult;

        SaveSlotsManager.SaveToSlot(0, GameState.Instance.CurrentSave);

        // Переход к первому бою через загрузчик
        if (UDA2.SceneFlow.SceneFlowManager.Instance != null)
            UDA2.SceneFlow.SceneFlowManager.Instance.LoadScene(firstFightSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(firstFightSceneName);
    }
}
