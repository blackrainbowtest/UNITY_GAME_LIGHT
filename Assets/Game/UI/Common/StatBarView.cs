using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dumb visual component.
/// Displays a normalized value (0..1) as a filled bar.
/// Knows nothing about HP, Mana, Stamina or game logic.
/// </summary>
public class StatBarView : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    [Header("Optional Labels")]
    [SerializeField] private TMP_Text valueText;
    [Tooltip("Optional floating delta text, e.g. +5 / -10. Will be animated if assigned.")]
    [SerializeField] private TMP_Text deltaText;

    [Header("Delta Animation")]
    [SerializeField] private float deltaDurationSeconds = 0.75f;
    [SerializeField] private float deltaMoveUpPixels = 24f;
    [SerializeField] private Color deltaPositiveColor = new Color(0.25f, 1f, 0.25f, 1f);
    [SerializeField] private Color deltaNegativeColor = new Color(1f, 0.35f, 0.35f, 1f);

    private Coroutine deltaRoutine;
    private Vector2 deltaStartAnchoredPos;

    private void Awake()
    {
        if (deltaText != null)
        {
            var rt = deltaText.rectTransform;
            deltaStartAnchoredPos = rt.anchoredPosition;
            deltaText.gameObject.SetActive(false);
        }
    }

    public void SetNormalized(float value01)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(value01);
    }

    public void SetValue(int current, int max)
    {
        if (valueText == null)
            return;

        if (max < 0) max = 0;
        if (current < 0) current = 0;
        if (current > max && max > 0) current = max;

        valueText.text = $"{current}/{max}";
    }

    public void ShowDelta(int delta)
    {
        if (deltaText == null)
            return;

        if (delta == 0)
            return;

        if (deltaRoutine != null)
            StopCoroutine(deltaRoutine);

        deltaRoutine = StartCoroutine(DeltaRoutine(delta));
    }

    private System.Collections.IEnumerator DeltaRoutine(int delta)
    {
        deltaText.gameObject.SetActive(true);

        deltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();
        deltaText.color = delta > 0 ? deltaPositiveColor : deltaNegativeColor;

        var rt = deltaText.rectTransform;
        rt.anchoredPosition = deltaStartAnchoredPos;

        var startColor = deltaText.color;
        var t = 0f;
        while (t < deltaDurationSeconds)
        {
            t += Time.deltaTime;
            var p = deltaDurationSeconds <= 0f ? 1f : Mathf.Clamp01(t / deltaDurationSeconds);

            // Move up and fade out.
            rt.anchoredPosition = deltaStartAnchoredPos + Vector2.up * (deltaMoveUpPixels * p);
            var c = startColor;
            c.a = 1f - p;
            deltaText.color = c;

            yield return null;
        }

        deltaText.gameObject.SetActive(false);
        deltaText.color = startColor;
        rt.anchoredPosition = deltaStartAnchoredPos;
        deltaRoutine = null;
    }
}
