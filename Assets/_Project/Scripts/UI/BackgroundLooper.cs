using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    public RectTransform[] backgrounds; // Assign two backgrounds in Inspector
    public float speed = 100f;
    private float width;
    private bool isMoving = true;

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
    }

    void Update()
    {
        if (!isMoving) return;
        foreach (var bg in backgrounds)
        {
            bg.anchoredPosition += Vector2.left * speed * Time.deltaTime;
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
    public void PauseMovement() => isMoving = false;
    public void ResumeMovement() => isMoving = true;
    public bool IsMoving() => isMoving;
}
