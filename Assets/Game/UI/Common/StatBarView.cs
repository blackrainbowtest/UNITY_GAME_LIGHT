using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dumb visual component.
/// Displays a normalized value (0..1) as a filled bar.
/// Knows nothing about HP, Mana, Stamina or game logic.
/// </summary>
public class StatBarView : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public void SetNormalized(float value01)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(value01);
    }
}
