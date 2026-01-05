using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    public RectTransform[] backgrounds; // Assign two backgrounds in Inspector
    public float speed = 100f;
    private float width;
    private bool isMoving = true;
    private float currentSpeed;
    private float targetSpeed;
    public float easeDuration = 0.5f; // seconds
    private float easeTimer = 0f;
    private bool isEasing = false;
    private bool isPausing = false;

    void Start()
    {
        if (backgrounds.Length < 2)
        {
            Debug.LogError("Need at least 2 backgrounds for seamless looping!");
            enabled = false;
            return;
        }
        width = backgrounds[0].rect.width;
        // Place the first at center, second to the right
        backgrounds[0].anchoredPosition = Vector2.zero;
        backgrounds[1].anchoredPosition = new Vector2(width, 0);
        currentSpeed = speed;
        targetSpeed = speed;
    }

    void Update()
    {
        // Easing logic
        if (isEasing)
        {
            easeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(easeTimer / easeDuration);
            // EaseInCirc: 1 - sqrt(1 - t^2)
            float ease = 1f - Mathf.Sqrt(1f - t * t);
            if (isPausing)
                currentSpeed = Mathf.Lerp(speed, 0f, ease);
            else
                currentSpeed = Mathf.Lerp(0f, speed, ease);
            if (t >= 1f)
            {
                isEasing = false;
                currentSpeed = isPausing ? 0f : speed;
                isMoving = !isPausing;
            }
        }
        if (currentSpeed == 0f) return;
        foreach (var bg in backgrounds)
        {
            bg.anchoredPosition += Vector2.left * currentSpeed * Time.deltaTime;
        }
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i].anchoredPosition.x <= -width)
            {
                float rightMost = backgrounds[0].anchoredPosition.x;
                for (int j = 1; j < backgrounds.Length; j++)
                    if (backgrounds[j].anchoredPosition.x > rightMost)
                        rightMost = backgrounds[j].anchoredPosition.x;
                backgrounds[i].anchoredPosition = new Vector2(rightMost + width, backgrounds[i].anchoredPosition.y);
            }
        }
    }

    // Call from Animation Event to pause/resume movement
    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    // For manual or Animation Event control
    public void PauseMovement()
    {
        if (currentSpeed > 0f)
        {
            isEasing = true;
            isPausing = true;
            easeTimer = 0f;
        }
    }

    public void ResumeMovement()
    {
        if (currentSpeed < speed)
        {
            isEasing = true;
            isPausing = false;
            easeTimer = 0f;
            isMoving = true;
        }
    }

    public bool IsMoving() => isMoving;
}
