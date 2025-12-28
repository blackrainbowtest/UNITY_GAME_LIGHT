using UnityEngine;

public class PlayerActionController : MonoBehaviour
{
    public Animator playerAnimator;

    // Эти методы вызываются из Animation Event на нужных кадрах
    public void PauseBackground()
    {
        var looper = FindFirstObjectByType<BackgroundLooper>();
        if (looper != null) looper.PauseMovement();
    }

    public void ResumeBackground()
    {
        var looper = FindFirstObjectByType<BackgroundLooper>();
        if (looper != null) looper.ResumeMovement();
    }
}
