using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class IntroController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private IntroSequence introSequence;

    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text loreText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button textClickCatcher; // прозрачная кнопка поверх TextPanel

    [Header("Flow")]
    [SerializeField] private string firstFightSceneName = "FirstFight";

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
        Debug.Log("wwwwwwwwwwwwwwwwwwww");
        ShowFrame(currentIndex + 1);
    }

    public void SkipIntro()
    {
        FinishIntro();
    }

    private void FinishIntro()
    {
        // Здесь переход к первому бою
        UnityEngine.SceneManagement.SceneManager.LoadScene(firstFightSceneName);
    }
}
