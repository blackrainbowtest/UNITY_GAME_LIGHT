
using UnityEngine;

public class TigressIdleAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void PlayState(string stateName)
    {
        if (animator != null)
            animator.Play(stateName);
    }

    public void TriggerAction(string triggerName)
    {
        if (animator != null)
            animator.SetTrigger(triggerName);
    }
}
