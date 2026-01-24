using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Game.Battle;

public class IntroController : MonoBehaviour
{
    // Local constant for the default save slot used in intro
    private const int DefaultIntroSaveSlot = 0;
    private enum IntroBranch { None, A, B }
    private IntroBranch selectedBranch = IntroBranch.None;
    [Header("Choice Dialog")]
    [SerializeField] private GameObject choiceDialog; // Panel with choice buttons
    [SerializeField] private Button interveneButton;
    [SerializeField] private Button turnAwayButton;
    [Header("Data")]
    [SerializeField] private IntroSequence introSequence;

    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text loreText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button textClickCatcher; // Transparent button over TextPanel

    [Header("Flow")]
    [SerializeField] private string firstFightSceneName = "FightScene";
    [SerializeField] private Game.Battle.EnemyData firstFightEnemy;

    private int currentIndex = 0;
    private Coroutine autoAdvanceCoroutine;

    private void Awake()
    {
        skipButton.onClick.AddListener(SkipIntro);
        textClickCatcher.onClick.AddListener(NextFrame);
    }

    private void Start()
    {
        if (introSequence == null || introSequence.Frames.Count == 0)
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


        if (index >= introSequence.Frames.Count)
        {
            FinishIntro();
            return;
        }

        currentIndex = index;

        // End of main branch after 14A (index == 14)
        if (selectedBranch == IntroBranch.A && index > 14)
        {
            FinishIntro();
            return;
        }
        // End of alternative branch after 13B (index == 16)
        if (selectedBranch == IntroBranch.B && index > 16)
        {
            FinishIntro();
            return;
        }

        IntroFrame frame = introSequence.Frames[currentIndex];

        // Background
        if (backgroundImage != null && frame.background != null)
            backgroundImage.sprite = frame.background;

        // Localization via component
        if (loreText != null)
        {
            // LocalizedTextSetter
            var setter = loreText.GetComponent<LocalizedTextSetter>();
            if (setter != null)
            {
                setter.key = frame.textKey;
                setter.UpdateText(); // call without parameters
            }
            // LocalizedTextComponent
            var comp = loreText.GetComponent<LocalizedTextComponent>();
            if (comp != null)
            {
                comp.textKey = frame.textKey;
                comp.UpdateText();
            }
        }

        // Auto-advance
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
        // If the 11th frame is shown, display the choice dialog instead of moving to the next frame
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
        // Save progress if needed
        // Access to GameState is required here because this controller is responsible for progressing the intro and triggering the transition to gameplay.
        // GameState.Instance is the single source of truth for current save data in the session.
        if (GameState.Instance.CurrentSave == null)
        {
            string versionPath = System.IO.Path.Combine(Application.dataPath, "..", "version.txt");
            string version = System.IO.File.Exists(versionPath)
                ? System.IO.File.ReadAllText(versionPath).Trim()
                : "0.0.1";
            GameState.Instance.CurrentSave = SaveData.CreateDefault(version);
        }

        // Null/empty check for scene name
        if (string.IsNullOrEmpty(firstFightSceneName))
        {
            Debug.LogError("IntroController: firstFightSceneName is null or empty. Cannot continue intro flow.");
            return;
        }

        // Data owner is GameState. Controller only initiates transition and records intro result.
        if (GameState.Instance.CurrentSave.player != null)
        {
            GameState.Instance.CurrentSave.player.SetSceneName(firstFightSceneName);
        }
        else
        {
            Debug.LogError("IntroController: CurrentSave.player is null. Cannot set scene name.");
        }

        // Record the result of the intro using the setter
        string introResult = "skip";
        if (selectedBranch == IntroBranch.A) introResult = "A";
        else if (selectedBranch == IntroBranch.B) introResult = "B";
        if (GameState.Instance.CurrentSave.progress != null && !string.IsNullOrEmpty(introResult))
        {
            GameState.Instance.CurrentSave.progress.SetIntroResult(introResult);
        }
        else if (GameState.Instance.CurrentSave.progress == null)
        {
            Debug.LogError("IntroController: CurrentSave.progress is null. Cannot set intro result.");
        }

        // Use a named constant for the save slot index
        SaveSlotsManager.SaveToSlot(DefaultIntroSaveSlot, GameState.Instance.CurrentSave);
        BattleEntryContext.Set(BattleMode.Tutorial);
        var enemyForFirstFight = firstFightEnemy;
        if (enemyForFirstFight != null)
            BattleEnemyContext.Set(enemyForFirstFight);
        else
            Debug.LogWarning("IntroController: firstFightEnemy is not set. BattleSceneEntryPoint will fallback to EnemySpawnTable.");
        Game.Battle.BattleExitContext.Set(new Game.Battle.BattleExitData("StartCityScene"));

        // Transition to the first fight scene using the scene loader if available
        if (UDA2.SceneFlow.SceneFlowManager.Instance != null)
        {
            UDA2.SceneFlow.SceneFlowManager.Instance.LoadScene(firstFightSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(firstFightSceneName);
        }
    }
}
