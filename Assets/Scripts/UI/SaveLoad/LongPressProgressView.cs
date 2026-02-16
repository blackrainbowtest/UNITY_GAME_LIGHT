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

    [Header("Debug")]
    [SerializeField] private bool debugPositioning;

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
        if (progressImage != null)
            progressImage.fillAmount = 0f;

        // Prefer moving the actual circle image rect; in some prefabs the serialized root rect
        // is a stretched container that stays centered.
        var target = progressImage != null ? progressImage.rectTransform : rootRectTransform;
        if (target == null)
            return;

        var canvas = GetComponentInParent<Canvas>();
        var canvasRect = canvas != null ? (RectTransform)canvas.transform : null;
        var cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        if (canvasRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPosition, cam, out var worldPoint))
        {
            worldPoint.z = target.position.z;
            target.position = worldPoint;
        }
        else
        {
            // Fallback for unusual setups.
            target.position = screenPosition;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugPositioning)
        {
            Debug.Log(
                $"[LongPressProgressView] Show screen={screenPosition} -> target='{target.name}' pos={target.position} " +
                $"anchors=({target.anchorMin}->{target.anchorMax}) pivot={target.pivot} canvas={(canvas != null ? canvas.renderMode.ToString() : "null")}",
                this);
        }
#endif
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
