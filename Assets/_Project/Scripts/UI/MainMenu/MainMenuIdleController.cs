
using UnityEngine;

public class MainMenuIdleController : MonoBehaviour
{
    [SerializeField] private TigressIdleAnimator animator;

    void Start()
    {
        if (animator != null)
            animator.PlayState("Walk");
    }

    public void OnUserClick()
    {
        if (animator != null)
            animator.TriggerAction("UserClickAction");
    }
}
