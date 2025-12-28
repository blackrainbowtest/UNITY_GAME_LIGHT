using UnityEngine;
using System.Collections;

public class MainMenuCharacterMotionController
{
    private TigressIdleAnimator animator;
    private int userClickMax;
    private float clickCooldown;
    private bool canClick = true;
    private MonoBehaviour coroutineHost;
    private Coroutine cooldownCoroutine;

    public MainMenuCharacterMotionController(TigressIdleAnimator animator, int userClickMax, float clickCooldown, MonoBehaviour coroutineHost)
    {
        this.animator = animator;
        this.userClickMax = userClickMax;
        this.clickCooldown = clickCooldown;
        this.coroutineHost = coroutineHost;
    }

    public bool IsReadyForClick()
    {
        return canClick && animator != null;
    }

    public void TriggerRandomAction()
    {
        if (!IsReadyForClick()) return;
        canClick = false;
        int actionIndex = Random.Range(1, userClickMax + 1);
        string triggerName = $"UserClickAction_{actionIndex}";
        animator.TriggerAction(triggerName);

        // Example: external call (log, background, etc.)
        OnUserActionTriggered(triggerName);

        // Не знаем длительность action, поэтому возврат в Walk должен быть через Animator (Has Exit Time)
        // Запускаем только cooldown
        if (cooldownCoroutine != null)
            coroutineHost.StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = coroutineHost.StartCoroutine(CooldownCoroutine());

    }

    protected virtual void OnUserActionTriggered(string triggerName)
    {
        // TODO: remove if not needed
    }

    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(clickCooldown);
        canClick = true;
    }

    public void PlayWalk()
    {
        if (animator != null)
            animator.PlayState("Walk");
    }
}