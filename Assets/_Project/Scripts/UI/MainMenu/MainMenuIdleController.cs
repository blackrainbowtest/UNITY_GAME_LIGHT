using UnityEngine;

public class MainMenuIdleController : MonoBehaviour
{
    [SerializeField] private TigressIdleAnimator animator;
    [SerializeField] private IdleAnimation[] idleAnimations;

    [SerializeField] private float minIdleTime = 2f;
    [SerializeField] private float maxIdleTime = 4f;

    private int _lastIndex = -1;
    private bool _shouldSwitchIdle = false;

    void Start()
    {
        if (animator != null)
            animator.OnCycleComplete += OnIdleCycleComplete;
        PlayRandomIdle();
        ScheduleNext();
    }

    private void PlayRandomIdle()
    {
        if (idleAnimations == null || idleAnimations.Length == 0)
            return;

        int index;
        do
        {
            index = Random.Range(0, idleAnimations.Length);
        }
        while (idleAnimations.Length > 1 && index == _lastIndex);

        _lastIndex = index;
        animator.Play(idleAnimations[index]);
    }

    private void ScheduleNext()
    {
        float delay = Random.Range(minIdleTime, maxIdleTime);
        Invoke(nameof(SetShouldSwitchIdle), delay);
    }

    private void SetShouldSwitchIdle()
    {
        _shouldSwitchIdle = true;
    }

    private void OnIdleCycleComplete()
    {
        if (_shouldSwitchIdle)
        {
            PlayRandomIdle();
            _shouldSwitchIdle = false;
            ScheduleNext();
        }
    }
    void OnDestroy()
    {
        if (animator != null)
            animator.OnCycleComplete -= OnIdleCycleComplete;
    }
}
