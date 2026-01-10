using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsible only for visualizing long press progress (circular UI).
/// Contains no logic, makes no decisions, knows nothing about actions.
/// </summary>
public class LongPressProgressView : MonoBehaviour
{
    [SerializeField] private Image progressImage;
    [SerializeField] private RectTransform rootRectTransform;

    private void Awake()
    {
        if (progressImage == null)
            Debug.LogError("LongPressProgressView: progressImage is not assigned in inspector");
        if (rootRectTransform == null)
            Debug.LogError("LongPressProgressView: rootRectTransform is not assigned in inspector");
    }

    /// <summary>
    /// Show the progress circle and move it under the finger/cursor.
    /// </summary>
    /// <param name="screenPosition">Position in screen coordinates</param>
    public void Show(Vector2 screenPosition)
    {
        gameObject.SetActive(true);
        progressImage.fillAmount = 0f;
        rootRectTransform.position = screenPosition;
    }

    /// <summary>
    /// Update progress (0..1).
    /// </summary>
    public void SetProgress(float progress)
    {
        if (progressImage == null) return;
        progressImage.fillAmount = Mathf.Clamp01(progress);
    }

    /// <summary>
    /// Hide the progress circle.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
