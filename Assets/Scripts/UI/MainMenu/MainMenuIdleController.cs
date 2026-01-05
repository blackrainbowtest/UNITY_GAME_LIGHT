
using UnityEngine;
using System.Collections;

public class MainMenuIdleController : MonoBehaviour
{
    [SerializeField] private TigressIdleAnimator animator;
    [SerializeField] private int userClickMax = 1;
    [SerializeField] private float clickCooldown = 2f;

    private MainMenuCharacterMotionController motionController;

    void Start()
    {
        motionController = new MainMenuCharacterMotionController(animator, userClickMax, clickCooldown, this);
        motionController.PlayWalk();
    }

    public void OnUserClick()
    {
        if (motionController != null && motionController.IsReadyForClick())
        {
            motionController.TriggerRandomAction();
        }
    }
}
